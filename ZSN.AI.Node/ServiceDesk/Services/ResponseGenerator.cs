using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Node.ServiceDesk.Interfaces;
using ZSN.AI.Node.ServiceDesk.Models;
using ZSN.AI.Node.Utils;
using ZSN.AI.Node.Utils.Pipeline;

namespace ZSN.AI.Node.ServiceDesk.Services
{
    /// <summary>
    /// 响应生成器 — 根据检索结果和策略生成流式回复
    /// </summary>
    public class ResponseGenerator : IResponseGenerator
    {
        private readonly IChatService _chatService;
        private readonly IHybridSearchService _hybridSearchService;
        private readonly ILogger<ResponseGenerator> _logger;

        public ResponseGenerator(
            IChatService chatService,
            IHybridSearchService hybridSearchService,
            ILogger<ResponseGenerator> logger)
        {
            _chatService = chatService;
            _hybridSearchService = hybridSearchService;
            _logger = logger;
        }

        /// <summary>
        /// 生成用户回复
        /// </summary>
        public async Task<ServiceDeskResponse> GenerateResponseAsync(
            string userQuery,
            ClassificationResult classification,
            KnowledgeRetrievalResult retrievalResult,
            MemoryContext memoryContext,
            ServiceDeskData config,
            StreamBatchWriter streamWriter)
        {
            var stopwatch = Stopwatch.StartNew();

            ServiceDeskResponse response = classification.Strategy switch
            {
                ProcessingStrategy.DirectReply =>
                    GenerateDirectReply(userQuery, classification, streamWriter),

                ProcessingStrategy.KnowledgeRetrieval =>
                    await GenerateKnowledgeBasedReplyAsync(userQuery, retrievalResult, config, streamWriter),

                ProcessingStrategy.RAGEnhanced =>
                    await GenerateRAGEnhancedReplyAsync(userQuery, retrievalResult, memoryContext, config, streamWriter),

                ProcessingStrategy.EscalateToClawAI =>
                    GenerateEscalationResponse(userQuery, classification),

                _ => await GenerateFallbackReplyAsync(userQuery, config, streamWriter)
            };

            stopwatch.Stop();
            response.ElapsedMs = stopwatch.ElapsedMilliseconds;
            return response;
        }

        /// <summary>直接回复（问候语、闲聊）</summary>
        public ServiceDeskResponse GenerateDirectReply(
            string userQuery,
            ClassificationResult classification,
            StreamBatchWriter streamWriter)
        {
            string reply = classification.Type switch
            {
                MessageType.Greeting => GetGreetingReply(),
                MessageType.SmallTalk => GetSmallTalkReply(userQuery),
                _ => "您好，有什么可以帮助您的吗？"
            };

            streamWriter.Append(reply);
            streamWriter.Flush();

            return new ServiceDeskResponse
            {
                Content = reply,
                Strategy = ProcessingStrategy.DirectReply,
                Confidence = 1.0,
                Sources = new List<KnowledgeSource>()
            };
        }

        /// <summary>基于知识库的回复（检索结果经 LLM 加工后输出）</summary>
        internal async Task<ServiceDeskResponse> GenerateKnowledgeBasedReplyAsync(
            string userQuery,
            KnowledgeRetrievalResult retrievalResult,
            ServiceDeskData config,
            StreamBatchWriter streamWriter)
        {
            if (retrievalResult?.Items == null || retrievalResult.Items.Count == 0)
            {
                return await GenerateFallbackReplyAsync(userQuery, config, streamWriter);
            }

            var modelConfig = BuildModelConfig(config);
            if (modelConfig == null)
            {
                _logger.LogWarning("[ResponseGenerator] 模型配置无效,降级到兜底回复");
                return await GenerateFallbackReplyAsync(userQuery, config, streamWriter);
            }

            // 构建知识库内容上下文
            var knowledgeContent = new StringBuilder();
            int count = Math.Min(config?.MaxContextChunks ?? 5, retrievalResult.Items.Count);
            for (int i = 0; i < count; i++)
            {
                var item = retrievalResult.Items[i];
                knowledgeContent.AppendLine($"--- [文档{i + 1}] ---");
                knowledgeContent.AppendLine(item.Content);
            }

            // 使用 system + user 分离的结构，确保模型清晰区分指令和用户问题
            var history = new ChatHistory();
            history.AddSystemMessage(
                "你是一个专业的客服助手。请根据提供的知识库内容回答用户问题。\n" +
                "要求：\n" +
                "1. 只使用提供的知识库内容回答，不要编造信息\n" +
                "2. 回答要准确、简洁、友好\n" +
                "3. 不要直接复制原文，用自己的语言组织回答\n" +
                "4. 如果知识库内容不足以回答，请诚实告知");

            history.AddUserMessage(
                $"知识库内容：\n{knowledgeContent}\n\n" +
                $"用户问题：{userQuery}");

            // 调用 LLM 流式生成
            var generatedContent = new StringBuilder();
            var progress = new Progress<string>(delta => streamWriter.Append(delta));
            await foreach (var chunk in _chatService.SendChatAsync(
                modelConfig, history, enableStreamingObservation: true, progress: progress))
            {
                generatedContent.Append(chunk);
            }

            // 添加引用标注
            var citations = BuildCitations(retrievalResult.Items.Take(3).ToList());
            if (!string.IsNullOrEmpty(citations))
            {
                streamWriter.Append("\n\n" + citations);
                generatedContent.Append("\n\n" + citations);
            }

            streamWriter.Flush();

            return new ServiceDeskResponse
            {
                Content = generatedContent.ToString(),
                Strategy = ProcessingStrategy.KnowledgeRetrieval,
                Confidence = retrievalResult.Confidence,
                Sources = retrievalResult.Items.Take(3).Select(i => i.Source).ToList(),
                RetrievalCount = retrievalResult.Items.Count,
                Citations = citations
            };
        }

        /// <summary>RAG 增强回复（检索 + LLM 流式生成）</summary>
        internal async Task<ServiceDeskResponse> GenerateRAGEnhancedReplyAsync(
            string userQuery,
            KnowledgeRetrievalResult retrievalResult,
            MemoryContext memoryContext,
            ServiceDeskData config,
            StreamBatchWriter streamWriter)
        {
            var modelConfig = BuildModelConfig(config);
            if (modelConfig == null)
            {
                _logger.LogWarning("[ResponseGenerator] 模型配置无效,降级到知识库回复");
                return await GenerateKnowledgeBasedReplyAsync(userQuery, retrievalResult, config, streamWriter);
            }

            // 构建知识库内容上下文
            var knowledgeContent = new StringBuilder();
            if (retrievalResult?.Items?.Count > 0)
            {
                int count = Math.Min(config?.MaxContextChunks ?? 5, retrievalResult.Items.Count);
                for (int i = 0; i < count; i++)
                {
                    var item = retrievalResult.Items[i];
                    knowledgeContent.AppendLine($"--- [文档{i + 1}] ---");
                    knowledgeContent.AppendLine(item.Content);
                }
            }

            // 构建对话历史
            var chatHistory = new StringBuilder();
            if (memoryContext?.ShortTermMemory?.Count > 0)
            {
                foreach (var msg in memoryContext.ShortTermMemory.TakeLast(3))
                {
                    chatHistory.AppendLine($"{msg.Role}: {msg.Content}");
                }
            }

            // 使用 system + user 分离结构
            var history = new ChatHistory();

            string systemPrompt = config?.PersonaPrompt;
            if (string.IsNullOrEmpty(systemPrompt))
            {
                systemPrompt = Utils.Utils.LoadPromptTemplate("ServiceDeskRAGResponsePrompt");
            }
            if (string.IsNullOrEmpty(systemPrompt))
            {
                systemPrompt = "你是一个专业的客服助手。请根据提供的知识库内容回答用户问题。\n" +
                    "回答要求：\n1. 只使用提供的知识库内容，不要编造信息\n2. 回答要准确、简洁、友好\n3. 如果知识库中没有相关信息，请诚实告知\n4. 可以适当添加相关建议或补充信息";
            }
            else
            {
                // 如果模板包含变量占位符，替换知识内容和查询以外的部分
                systemPrompt = systemPrompt
                    .Replace("{{knowledgeContent}}", "")
                    .Replace("{{userQuery}}", "")
                    .Replace("{{chatHistory}}", chatHistory.ToString());
            }

            history.AddSystemMessage(systemPrompt);

            var userPrompt = new StringBuilder();
            if (chatHistory.Length > 0)
            {
                userPrompt.AppendLine($"对话历史：\n{chatHistory}");
            }
            userPrompt.AppendLine($"知识库内容：\n{knowledgeContent}");
            userPrompt.AppendLine($"用户问题：{userQuery}");

            history.AddUserMessage(userPrompt.ToString());

            // 调用 LLM 流式生成
            var generatedContent = new StringBuilder();
            var progress = new Progress<string>(delta => streamWriter.Append(delta));
            await foreach (var chunk in _chatService.SendChatAsync(
                modelConfig, history, enableStreamingObservation: true, progress: progress))
            {
                generatedContent.Append(chunk);
            }

            // 添加引用标注
            var citations = BuildCitations(retrievalResult?.Items?.Take(3).ToList());
            if (!string.IsNullOrEmpty(citations))
            {
                streamWriter.Append("\n\n" + citations);
                generatedContent.Append("\n\n" + citations);
            }

            streamWriter.Flush();

            return new ServiceDeskResponse
            {
                Content = generatedContent.ToString(),
                Strategy = ProcessingStrategy.RAGEnhanced,
                Confidence = retrievalResult?.Confidence ?? 0,
                Sources = retrievalResult?.Items?.Take(3).Select(i => i.Source).ToList() ?? new List<KnowledgeSource>(),
                RetrievalCount = retrievalResult?.Items?.Count ?? 0,
                Citations = citations
            };
        }

        /// <summary>升级到 ClawAI 的响应</summary>
        internal ServiceDeskResponse GenerateEscalationResponse(
            string userQuery,
            ClassificationResult classification)
        {
            var content = @"您的问题比较复杂，我将为您转接到高级智能助手进行处理。

正在为您规划最佳解决方案，请稍候...";

            return new ServiceDeskResponse
            {
                Content = content,
                Strategy = ProcessingStrategy.EscalateToClawAI,
                Confidence = 0,
                NeedsEscalation = true,
                EscalationReason = $"复杂度: {classification.Complexity}, 置信度: {classification.Confidence:F2}"
            };
        }

        /// <summary>兜底回复</summary>
        internal async Task<ServiceDeskResponse> GenerateFallbackReplyAsync(
            string userQuery,
            ServiceDeskData config,
            StreamBatchWriter streamWriter)
        {
            var content = config?.FallbackMessage ?? @"抱歉，我暂时没有找到相关信息来回答您的问题。

您可以：
1. 换个方式描述您的问题
2. 联系人工客服获取帮助

请问还有什么可以帮助您的吗？";

            streamWriter.Append(content);
            streamWriter.Flush();

            await Task.CompletedTask;

            return new ServiceDeskResponse
            {
                Content = content,
                Strategy = ProcessingStrategy.RAGEnhanced,
                Confidence = 0,
                Sources = new List<KnowledgeSource>(),
                IsFallback = true
            };
        }

        /// <summary>构建 RAG Prompt</summary>
        private string BuildRAGPrompt(
            string userQuery,
            KnowledgeRetrievalResult retrievalResult,
            MemoryContext memoryContext,
            ServiceDeskData config)
        {
            // 优先使用自定义 PersonaPrompt，否则从模板文件加载
            string template = config?.PersonaPrompt;
            if (string.IsNullOrEmpty(template))
            {
                template = Utils.Utils.LoadPromptTemplate("ServiceDeskRAGResponsePrompt");
            }
            if (string.IsNullOrEmpty(template))
            {
                template = "你是一个专业的客服助手。请根据提供的知识库内容回答用户问题。\n\n回答要求：\n1. 只使用提供的知识库内容，不要编造信息\n2. 回答要准确、简洁、友好\n3. 如果知识库中没有相关信息，请诚实告知";
            }

            // 构建对话历史
            var chatHistory = new StringBuilder();
            if (memoryContext?.ShortTermMemory?.Count > 0)
            {
                foreach (var msg in memoryContext.ShortTermMemory.TakeLast(3))
                {
                    chatHistory.AppendLine($"{msg.Role}: {msg.Content}");
                }
            }

            // 构建知识库内容
            var knowledgeContent = new StringBuilder();
            if (retrievalResult?.Items?.Count > 0)
            {
                int count = Math.Min(config?.MaxContextChunks ?? 5, retrievalResult.Items.Count);
                for (int i = 0; i < count; i++)
                {
                    var item = retrievalResult.Items[i];
                    knowledgeContent.AppendLine($"\n[文档{i + 1}] {item.Source.DocumentTitle ?? "知识条目"}");
                    knowledgeContent.AppendLine(item.Content);
                }
            }

            // 替换模板变量
            template = template.Replace("{{chatHistory}}", chatHistory.ToString())
                               .Replace("{{knowledgeContent}}", knowledgeContent.ToString())
                               .Replace("{{userQuery}}", userQuery);

            return template;
        }

        /// <summary>
        /// 通过 FunctionCall 生成回复 — LLM 自主调用知识库检索工具
        /// </summary>
        public async Task<ServiceDeskResponse> GenerateFunctionCallResponseAsync(
            string userQuery,
            ServiceDeskData config,
            StreamBatchWriter streamWriter,
            List<ChatMessageRecord> chatHistory = null)
        {
            var stopwatch = Stopwatch.StartNew();

            var modelConfig = BuildModelConfig(config);
            if (modelConfig == null)
            {
                var fallback = "抱歉，系统暂时无法处理您的请求，请稍后再试。";
                streamWriter.Append(fallback);
                streamWriter.Flush();
                return new ServiceDeskResponse
                {
                    Content = fallback,
                    Strategy = ProcessingStrategy.RAGEnhanced,
                    Confidence = 0,
                    IsFallback = true
                };
            }

            // 创建知识库检索插件（每次请求实例化，携带当前节点的知识库配置）
            var plugin = new ServiceDeskKnowledgePlugin(
                _hybridSearchService, config, _logger);

            var callFunction = new CallFunction
            {
                FunctionClass = plugin,
                FunctionClassName = "ServiceDeskKB",
                FunctionName = "SearchKnowledgeBase"
            };

            // 构建聊天上下文
            var history = new ChatHistory();
            history.AddSystemMessage(
                "你是一个专业的智能客服助手。\n\n" +
                "## 工具使用规则\n" +
                "你拥有一个 search_knowledge_base 工具，仅在以下情况调用它：\n" +
                "- 用户询问具体的事实性问题（产品功能、操作步骤、技术细节、业务流程等）\n" +
                "- 需要从文档中查找准确信息才能回答\n\n" +
                "以下情况**不要**调用工具，直接回答即可：\n" +
                "- 问候、闲聊、感谢\n" +
                "- 回顾对话历史（\"我们聊了什么\"、\"刚才说了什么\"）\n" +
                "- 对你已生成回答的追问或澄清\n" +
                "- 基于上下文就能回答的问题\n\n" +
                "## 回答要求\n" +
                "- 根据对话上下文理解用户意图，特别是追问和省略指代\n" +
                "- 使用检索内容时，用简洁友好的语言组织回答，不要照搬原文\n" +
                "- 信息不足时如实告知，不要编造");

            // 添加对话历史（多轮对话上下文）
            if (chatHistory != null)
            {
                foreach (var msg in chatHistory.TakeLast(10))
                {
                    if (msg.Role == "user")
                        history.AddUserMessage(msg.Content);
                    else if (msg.Role == "assistant")
                        history.AddAssistantMessage(msg.Content);
                }
            }

            history.AddUserMessage(userQuery);

            // 调用 LLM（SK 自动处理 FunctionCall 周期）
            var generatedContent = new StringBuilder();
            var progress = new Progress<string>(delta => streamWriter.Append(delta));
            await foreach (var chunk in _chatService.SendChatAsync(
                modelConfig, history, Function: callFunction,
                enableStreamingObservation: true, progress: progress))
            {
                generatedContent.Append(chunk);
            }

            // 从插件提取引用信息
            var sources = plugin.LastSearchResults.Take(3).ToList();
            var citations = BuildCitations(sources);
            if (!string.IsNullOrEmpty(citations))
            {
                streamWriter.Append("\n\n" + citations);
                generatedContent.Append("\n\n" + citations);
            }

            streamWriter.Flush();
            stopwatch.Stop();

            return new ServiceDeskResponse
            {
                Content = generatedContent.ToString(),
                Strategy = ProcessingStrategy.RAGEnhanced,
                Confidence = sources.Count > 0 ? sources.Max(s => s.FinalScore) : 0,
                Sources = sources.Select(s => s.Source).ToList(),
                RetrievalCount = plugin.LastSearchResults.Count,
                Citations = citations,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }

        /// <summary>构建引用标注</summary>
        private string BuildCitations(List<RetrievalItem> items)
        {
            if (items == null || items.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine("参考来源：");

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var fileName = item.Source.DocumentTitle ?? "未知文件";
                var kbName = item.Source.KnowledgeBaseName ?? "知识库";
                var page = item.Metadata?.ContainsKey("page_number") == true
                    ? $" 第{item.Metadata["page_number"]}页"
                    : "";
                sb.AppendLine($"{i + 1}. [{kbName}] {fileName}{page}");
            }

            return sb.ToString();
        }

        private string GetGreetingReply()
        {
            var greetings = new[]
            {
                "您好！我是智能客服助手，很高兴为您服务。请问有什么可以帮助您的吗？",
                "您好！有什么问题我可以帮您解答吗？",
                "欢迎！请问有什么需要咨询的吗？"
            };
            return greetings[Random.Shared.Next(greetings.Length)];
        }

        private string GetSmallTalkReply(string userQuery)
        {
            if (userQuery.Contains("谢谢") || userQuery.Contains("感谢"))
                return "不客气！很高兴能帮到您。还有其他问题吗？";
            if (userQuery.Contains("再见") || userQuery.Contains("拜拜"))
                return "再见！祝您生活愉快，有问题随时联系我。";
            return "好的，还有什么可以帮您的吗？";
        }

        /// <summary>构建模型配置,优先使用节点配置的模型,否则回退到系统默认模型</summary>
        private LargeModelConfig BuildModelConfig(ServiceDeskData config)
        {
            LargeModelInfo modelInfo = null;

            // 优先使用节点配置的模型
            if (config?.model?.LargeModelID > 0)
            {
                modelInfo = LargeModelInfoBussiness.GetModel(config.model.LargeModelID);
            }

            // 回退到系统默认模型
            if (modelInfo == null)
            {
                modelInfo = LargeModelInfoBussiness.GetDefaultModel();
                if (modelInfo != null)
                {
                    _logger.LogInformation("[ResponseGenerator] 使用系统默认模型: {ModelName} (ID: {ModelID})",
                        modelInfo.ModelName, modelInfo.LargeModelID);
                }
            }

            // 最终验证
            if (modelInfo == null ||
                string.IsNullOrWhiteSpace(modelInfo.ModelName) ||
                string.IsNullOrWhiteSpace(modelInfo.ModelKey) ||
                string.IsNullOrWhiteSpace(modelInfo.EndPoint))
            {
                _logger.LogWarning("[ResponseGenerator] 无可用模型: 节点模型ID={NodeModelID}, 默认模型={DefaultModel}",
                    config?.model?.LargeModelID ?? 0,
                    modelInfo?.ModelName ?? "null");
                return null;
            }

            return new LargeModelConfig
            {
                Model = modelInfo,
                Temperature = config?.temperature ?? 30,
                TopPCoefficient = config?.topp ?? 80,
            };
        }
    }
}
