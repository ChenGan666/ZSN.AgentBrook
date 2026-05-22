

using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;

//using ZSN.AI.Core.Common.Bge;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using OpenAI.Chat;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ZSN.AI.BLL;
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model;
using ZSN.AI.Entity.Model.Constant;
using ZSN.AI.MCPClient;
using ZSN.Utils.Core.Extensions;
using System.Threading;
using System.Diagnostics;
using ZSN.AI.BLL;
using AuthorRole = Microsoft.SemanticKernel.ChatCompletion.AuthorRole;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;
using Document = Microsoft.KernelMemory.Document;


namespace ZSN.AI.Core.Service
{
    [ServiceDescription(typeof(IChatService), ServiceLifetime.Scoped)]
    public class ChatService(
        IKernelService _kernelService,
        IKMService _kMService,
        IOperationLogService _logService
        ) : IChatService
    {
        private const int LLMLogMarkId = 309;


        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="ModelConfig"></param>
        /// <param name="history"></param>
        /// <param name="Function"></param>
        /// <param name="responseFormat">json_object,text</param>
        /// <param name="enableStreamingObservation"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<string> SendChatAsync(LargeModelConfig ModelConfig, ChatHistory history, CallFunction? Function = null, string responseFormat = "text", bool enableStreamingObservation = false, IProgress<string>? progress = null, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var _kernel = _kernelService.GetKernel(ModelConfig.Model);
            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var temperature = ModelConfig.Temperature / 100;
            var topP = ModelConfig.TopPCoefficient / 100;
            responseFormat = ModelConfig.ResponseFormat.IsNullOrEmpty() ? responseFormat : ModelConfig.ResponseFormat;

            UnifiedChatSettings settings = PromptExecutionSettingsFactory.Create(ModelConfig);
            /*
            OpenAIPromptExecutionSettings settings = new() { 
                Temperature = temperature, 
                TopP = topP, 
                FrequencyPenalty = 1, 
                ResponseFormat = responseFormat, 
                //ReasoningEffort = (ModelConfig.Thinking ? "high" : "low") 
            };
            */

            List<string> completionList = new List<string>();
            string _re = "";
            MCPClient.MCPClient mcpClient = null;
            bool hasFunctions = false;
            bool isClientCallMCP = false;

            //MCP客户端配置
            if (ModelConfig.Mcp != null&& ModelConfig.Mcp.Config!=null)
            {
                MCPConfig mcpConfig = null;
                try
                {
                    mcpConfig = JsonConvert.DeserializeObject<MCPConfig>(ModelConfig.Mcp.Config);
                }
                catch
                {
                    _re = "MCP配置错误";
                    mcpConfig = null;
                }
                
                if (mcpConfig != null)
                {
                    mcpClient = new MCPClient.MCPClient(mcpConfig);
                   
                    IList<McpClientTool> tools = await GetMcpClientToolsAsync(mcpConfig);

                    if (tools?.Count > 0)
                    {
                        if(mcpConfig.Info.RunHost == RunHostType.Client)//客户端调用MCP,返回整理后的参数，由客户端执行具体函数
                        {
                            for (int i = 0; i < tools.Count; i++)
                            {
                                if (tools[i] is McpClientTool item)
                                {
                                    tools[i] = tools[i].WithDescription(item.Description + "\n\r #不执行，只返回 JSON 指令#"); // 安全地修改属性
                                    Console.WriteLine("MCP Tool: " + item.Name);
                                }
                            }
                            isClientCallMCP = true;
                        }
                        _kernelService.ImportFunctions(_kernel, tools);
                        hasFunctions = true;
                    }

                }
                else
                {
                   yield return _re;
                }
            }

            if (ModelConfig.SemanticFunction.Count > 0 || ModelConfig.NativeFunction.Count > 0 || Function != null)
            {
                _kernelService.ImportFunctions(ModelConfig, _kernel);

                if (Function != null)
                {
                    _kernelService.ImportFunctions(_kernel, Function.FunctionClass, Function.FunctionName);
                }

                //插件加载检查
                foreach (var plugin in _kernel.Plugins)
                {
                    Console.WriteLine("plugin: " + plugin.Name);
                    foreach (var function in plugin)
                    {
                        Console.WriteLine("  - prompt function: " + function.Name);
                    }
                }
                hasFunctions = true;
            }

            if (hasFunctions)
            {
                if (isClientCallMCP)
                {
                    settings.ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions;
                    history.AddUserMessage("注意!这里不用执行,只返回客户端可执行的MCP调用 JSON 指令!");
                }
                else
                {
                    settings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;
                }
            }

            if (enableStreamingObservation)
            {
                var sb = new StringBuilder();
                Exception streamError = null;
                try
                {
                    await foreach (var item in chat.GetStreamingChatMessageContentsAsync(history, settings, _kernel, ct))
                    {
                        if (item?.Content != null && item.Content.Length > 0)
                        {
                            var delta = item.Content.ConvertToString();
                            if (!string.IsNullOrEmpty(delta))
                            {
                                progress?.Report(delta);
                                sb.Append(delta);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    streamError = ex;
                }

                if (streamError != null)
                {
                    LogLLMCall("SendChatAsync", ModelConfig, history, null, sw.ElapsedMilliseconds, "error", streamError.Message);
                    yield return $"生成回答时发生错误：{streamError.Message}";
                    yield break;
                }

                var final = sb.ToString();
                LogLLMCall("SendChatAsync", ModelConfig, history, final, sw.ElapsedMilliseconds, "success");
                if (!string.IsNullOrEmpty(final))
                {
                    yield return final;
                }
                yield break;
            }
            else
            {

                Microsoft.SemanticKernel.ChatMessageContent result = null;
                Exception callError = null;

                try
                {
                    result = await chat.GetChatMessageContentAsync(history, settings, _kernel);
                }
                catch (Exception ex)
                {
                    callError = ex;
                }

                if (callError != null)
                {
                    LogLLMCall("SendChatAsync", ModelConfig, history, null, sw.ElapsedMilliseconds, "error", callError.Message);
                    yield return $"生成回答时发生错误：{callError.Message}";
                    yield break;
                }

                if (result?.Content != null && result.Content.Length > 0)
                {
                    string chunkCompletion = result.Content.ConvertToString();
                    LogLLMCall("SendChatAsync", ModelConfig, history, chunkCompletion, sw.ElapsedMilliseconds, "success");
                    completionList.Add(chunkCompletion);
                    foreach (var content in completionList)
                    {
                        yield return content.ConvertToString();
                    }
                }
            }
        }


        public static McpClientTool CloneWithDescription(McpClientTool original, string newDescription)
        {
            var clone = JsonConvert.DeserializeObject<JsonObject>( JsonConvert.SerializeObject(original));
            clone["description"] = newDescription;

            return JsonConvert.DeserializeObject<McpClientTool>( JsonConvert.SerializeObject(clone));
        }
        private async Task<IEnumerable<string>> GenerateResponsesAsync(Kernel _kernel, KernelFunction func, KernelArguments arguments)
        {
            try
            {
                // 执行函数
                FunctionResult chatResult = await func.InvokeAsync(_kernel, arguments);

                return new[] { chatResult.ToString() };
            }
            catch (Exception ex)
            {
                // 异常时返回错误信息
                return new[] { $"生成回答时发生错误：{ex.Message}" };
            }
        }

        /// <summary>
        /// 通过多个知识库获取提问的相关内容，并根据对话记录重新组织返回
        /// </summary>
        /// <param name="KnowledgeBaseUnits"></param>
        /// <param name="ChatModel"></param>
        /// <param name="questions"></param>
        /// <param name="history"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<string> SendKmsAsync(List<KnowledgeBaseUnit> KnowledgeBaseUnits, LargeModelConfig ChatModel, string questions, ChatHistory history)
        {
            var sw = Stopwatch.StartNew();
            var dataMsg = new StringBuilder();
            foreach (var kms in KnowledgeBaseUnits)
            {
                var relevantSourceList = await _kMService.GetRelevantSourceList(kms.LargeModelUnit, questions, kms.KnowledgeBaseInfo.KnowledgeBaseID);

                if (relevantSourceList.Count > 0)
                {
                    dataMsg.AppendLine("#知识库名称:");
                    dataMsg.AppendLine($"{kms.KnowledgeBaseInfo.Name}");
                    dataMsg.AppendLine("#知识库的作用以及知识范围:");
                    dataMsg.AppendLine($"{kms.KnowledgeBaseInfo.Description}");
                    dataMsg.AppendLine("#找到相关内容如下：");
                    foreach (var item in relevantSourceList)
                    {
                        dataMsg.AppendLine(item.ToString());
                    }
                    dataMsg.AppendLine("");
                }
            }
            var kmsResult = dataMsg.ToString();
            LogLLMCall("SendKmsAsync", ChatModel, questions, kmsResult, sw.ElapsedMilliseconds, "success");
            yield return kmsResult;
            /*
            var _kernel = _kernelService.GetKernelByAIModelID(ChatModel.Model.LargeModelID);

            var temperature = ChatModel.Temperature / 100;
            var topP = ChatModel.TopPCoefficient / 100;

            OpenAIPromptExecutionSettings settings = new() { Temperature = temperature, TopP = topP };

            string prompt = @"
# 系统提示词：知识库问答助手

你是一名专业的知识库问答助手。  
你的目标是根据 **用户提问**、**知识库检索内容** 和 **对话历史**，为用户生成最贴近需求的精准回答。  

## 规则
1. **优先结合知识库检索内容**（{{$doc}}）回答用户问题,有先采用 **Relevance** 值高的内容。  
2. 如果用户明确要求“直接输出知识库内容”，请逐字输出检索到的内容，不做任何改写或总结。  
3. 如果知识库未完全覆盖问题，可结合对话历史（{{$history}}）和你已有知识补充说明，但请明确标注哪些内容来自知识库，哪些为推理补充。  
4. 保持回答简洁准确，避免冗余和重复。

---

## 输入变量
- **用户提问**：`{{$input}}`  
- **知识库检索内容**：`{{$doc}}`  
- **对话历史**：`{{$history}}`

---

## 输出要求
请生成一个最接近用户需求的高质量回答。
- 优先引用知识库内容
- 可结合上下文和推理补充信息
- 用户要求“直接输出”时，不得改动内容
";
            KernelFunction func = _kernel.CreateFunctionFromPrompt(
                prompt,
                settings
                );
            var recentHistory = history.TakeLast(5);
            var arguments = new KernelArguments()
            {
                ["doc"] = dataMsg.ToString(),
                ["history"] = string.Join("\n", recentHistory.Select(x => $"{x.Role}: {x.Content}")),
                ["input"] = questions
            };
            
            var responses = await GenerateResponsesAsync(_kernel,func, arguments);
            foreach (var response in responses)
            {
                yield return response;
            }
            */
        }


        /// <summary>
        /// 组织会话记录
        /// </summary>
        /// <param name="MessageList">消息列表</param>
        /// <param name="history">记录历史</param>
        /// <returns>处理后的对话历史</returns>
        public async Task<ChatHistory> GetChatHistory(List<AppChatLogInfo> MessageList, ChatHistory history)
        {
            for(int i = 0;i<MessageList.Count;i++)
            {
                var item = MessageList[i];
                GptMsg msg = JsonConvert.DeserializeObject<GptMsg>(item.Content.ConvertToString());

                switch (item.Direction) {

                    case 0:
                        if(msg.Attachments?.Count > 0){

                            var _ChatMessage = new ChatMessageContentItemCollection();
                            _ChatMessage.Add(new Microsoft.SemanticKernel.TextContent(msg.content));
                            foreach (var attachment in msg.Attachments)
                            {
                                // 智能选择文件来源：优先使用本地文件，不存在则从URI获取
                                byte[] bytes;
                                if (File.Exists(attachment.FilePath))
                                {
                                    bytes = File.ReadAllBytes(attachment.FilePath);
                                }
                                else if (!string.IsNullOrEmpty(attachment.FileURI))
                                {
                                    using (var httpClient = new HttpClient())
                                    {
                                        bytes = await httpClient.GetByteArrayAsync(attachment.FileURI);
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                                if (FilesExtension.ImageExtensionMimeTypes.ContainsKey(attachment.Type.ToLower()))
                                {
                                    _ChatMessage.Add(new ImageContent(bytes, FilesExtension.ImageExtensionMimeTypes[attachment.Type.ToLower()]));
                                }
                                else
                                {
#pragma warning disable SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                                    _ChatMessage.Add(new BinaryContent(bytes, FilesExtension.FilesExtensionMimeTypes[attachment.Type.ToLower()]));
#pragma warning restore SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                                }
                            }
                            history.AddUserMessage(_ChatMessage);
                        }
                        else
                        {
                            history.AddUserMessage(msg.content);
                        }
                        break;
                    case 1:
                        history.AddAssistantMessage(msg.content);
                        break;
                    case 2:
                        history.AddMessage(AuthorRole.Tool, msg.content);
                        break;
                }
                
            }
            return history;
        }
        public async Task<ChatHistory> GetChatHistory(List<AppChatSummaryInfo> MessageList, ChatHistory history)
        {
            for (int i = 0; i < MessageList.Count; i++)
            {
                var item = MessageList[i];
                string _content = JsonConvert.DeserializeObject<GptMsg>(item.Content.ConvertToString()).content;

                history.AddMessage(AuthorRole.Assistant, _content);

            }
            return history;
        }

        public async IAsyncEnumerable<string> HistorySummarize(LargeModelConfig ModelConfig, ChatHistory history)
        {
            var sw = Stopwatch.StartNew();
            var _kernel = _kernelService.GetKernel(ModelConfig.Model);

            var result = await _kernelService.HistorySummarize(_kernel, history);
            LogLLMCall("HistorySummarize", ModelConfig, history, result, sw.ElapsedMilliseconds, "success");
            yield return result;
        }

        public async IAsyncEnumerable<string> FunctionCall(LargeModelConfig ModelConfig, CallFunction callFunction, KernelArguments kernelArguments = null, string responseFormat = "text") {

            var sw = Stopwatch.StartNew();
            var _kernel = _kernelService.GetKernel(ModelConfig.Model);
            _kernel.Plugins.AddFromObject(callFunction.FunctionClass, callFunction.FunctionClassName);

            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var temperature = ModelConfig.Temperature / 100;
            var topP = ModelConfig.TopPCoefficient / 100;

            OpenAIPromptExecutionSettings settings = new() { Temperature = temperature, TopP = topP, ResponseFormat = responseFormat };
            List<string> completionList = new List<string>();
            settings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;// ToolCallBehavior.EnableKernelFunctions;

            var history = new ChatHistory();
            history.AddSystemMessage(callFunction.Prompt);
            history.AddUserMessage(callFunction.Input);

            while (true)
            {
                Microsoft.SemanticKernel.ChatMessageContent result = await chat.GetChatMessageContentAsync(history, settings, _kernel);
                if (result.Content != null && result.Content.Length > 0)
                {
                    string chunkCompletion = result.Content.ConvertToString();
                    completionList.Add(chunkCompletion);
                    LogLLMCall("FunctionCall", ModelConfig, history, chunkCompletion, sw.ElapsedMilliseconds, "success");
                    foreach (var content in completionList)
                    {
                        yield return content.ConvertToString();
                    }
                    break;
                }

                history.Add(result);
            }

        }
    
        public async IAsyncEnumerable<string> PromptFunctionCall(LargeModelConfig ModelConfig, CallFunction callFunction, KernelArguments kernelArguments = null, string responseFormat = "text")
        {
            var sw = Stopwatch.StartNew();
            var _kernel = _kernelService.GetKernel(ModelConfig.Model);

            var temperature = ModelConfig.Temperature / 100;
            var topP = ModelConfig.TopPCoefficient / 100;

            OpenAIPromptExecutionSettings settings = new() { Temperature = temperature, TopP = topP, ResponseFormat = responseFormat };
            List<string> completionList = new List<string>();

            string prompt = callFunction.Prompt;

            prompt = ModelConfig.Prompt.IsNullOrEmpty() ? prompt : ModelConfig.Prompt + "\n" + prompt;

            if (kernelArguments == null)
            {
                kernelArguments = new KernelArguments()
                {
                    ["input"] = callFunction.Input
                };
            }

            KernelFunction func = _kernel.CreateFunctionFromPrompt(
                prompt,
                settings
                );

            var responses = await GenerateResponsesAsync(_kernel, func, kernelArguments);
            var responseText = string.Join("", responses);
            LogLLMCall("PromptFunctionCall", ModelConfig, $"prompt:{prompt}\ninput:{callFunction.Input}", responseText, sw.ElapsedMilliseconds, "success");
            foreach (var response in responses)
            {
                yield return response;
            }
        }

        public async Task<IList<McpClientTool>> GetMcpClientToolsAsync(MCPConfig mcpConfig)
        {

            if (mcpConfig == null)
            {
                return new List<McpClientTool>();
            }

            var mcpClient = new MCPClient.MCPClient(mcpConfig);

            var mcpTools = await mcpClient.GetToolsAsync();

            return mcpTools;
        }

        private void LogLLMCall(string methodName, LargeModelConfig modelConfig, ChatHistory history, string output, long durationMs, string status, string error = null)
        {
            try
            {
                var historySummary = new StringBuilder();
                foreach (var msg in history)
                {
                    var content = msg.Content ?? "";
                    if (content.Length > 200) content = content.Substring(0, 200) + "...";
                    historySummary.AppendLine($"{msg.Role}: {content}");
                }
                LogLLMCall(methodName, modelConfig, historySummary.ToString(), output, durationMs, status, error);
            }
            catch { }
        }

        private void LogLLMCall(string methodName, LargeModelConfig modelConfig, string input, string output, long durationMs, string status, string error = null)
        {
            try
            {
                var model = modelConfig.Model;
                var logDetail = JsonConvert.SerializeObject(new
                {
                    serviceName = "ChatService",
                    methodName,
                    model = new
                    {
                        modelId = model.LargeModelID,
                        modelName = model.ModelName,
                        typeName = model.TypeName,
                        organization = model.ModelOrganizationName,
                        endPoint = model.EndPoint
                    },
                    parameters = new
                    {
                        temperature = modelConfig.Temperature,
                        topP = modelConfig.TopPCoefficient,
                        responseFormat = modelConfig.ResponseFormat,
                        thinking = modelConfig.Thinking,
                        answerTokens = modelConfig.AnswerTokens
                    },
                    input = TruncateLog(input),
                    output = TruncateLog(output),
                    timing = new { durationMs },
                    status,
                    error
                }, Formatting.None);
                _logService.AddOperationLog(LLMLogMarkId, logDetail);
            }
            catch { }
        }

        private static string TruncateLog(string text, int maxLength = 10000)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length > maxLength ? text.Substring(0, maxLength) + "...[truncated]" : text;
        }

    }
}
