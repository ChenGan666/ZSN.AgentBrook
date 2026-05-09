using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Core.Interface;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Utils;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// 知识提炼服务实现
    /// 自动从对话中提取知识并更新到长期记忆
    /// </summary>
    public class KnowledgeExtractionService : IKnowledgeExtractionService
    {
        private readonly ILogger<KnowledgeExtractionService> _logger;
        private readonly IKernelService _kernelService;
        private readonly ClawAIOptions _options;

        public KnowledgeExtractionService(
            ILogger<KnowledgeExtractionService> logger,
            IKernelService kernelService,
            IOptions<ClawAIOptions> options)
        {
            _logger = logger;
            _kernelService = kernelService;
            _options = options?.Value ?? new ClawAIOptions();
        }

        /// <summary>
        /// 从对话中提取知识点
        /// </summary>
        public async Task<List<ExtractedKnowledge>> ExtractKnowledgeFromDialogueAsync(
            string userQuestion,
            string aiAnswer,
            DialogueContext context)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $"[KnowledgeExtraction] 开始提取知识 - 问题长度: {userQuestion?.Length}, 回答长度: {aiAnswer?.Length}");

                var extractedKnowledge = new List<ExtractedKnowledge>();

                // 1. 快速规则提取 - 不依赖LLM
                var ruleBasedKnowledge = ExtractByRules(userQuestion, aiAnswer);
                if (ruleBasedKnowledge != null)
                {
                    extractedKnowledge.Add(ruleBasedKnowledge);
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, "[KnowledgeExtraction] 规则提取成功");
                }

                // 2. LLM深度提取 - 仅对重要对话使用
                if (ShouldUseLLMExtraction(userQuestion, aiAnswer, context))
                {
                    var llmKnowledge = await ExtractByLLMAsync(userQuestion, aiAnswer, context);
                    if (llmKnowledge != null && llmKnowledge.Count > 0)
                    {
                        extractedKnowledge.AddRange(llmKnowledge);
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $"[KnowledgeExtraction] LLM提取成功 - 数量: {llmKnowledge.Count}");
                    }
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $"[KnowledgeExtraction] 提取完成 - 总数: {extractedKnowledge.Count}");
                return extractedKnowledge;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KnowledgeExtraction] 提取知识失败");
                return new List<ExtractedKnowledge>();
            }
        }

        /// <summary>
        /// 基于规则的快速提取
        /// </summary>
        private ExtractedKnowledge ExtractByRules(string userQuestion, string aiAnswer)
        {
            try
            {
                // 检测问答对模式
                if (IsQuestionAnswerPair(userQuestion, aiAnswer))
                {
                    return new ExtractedKnowledge
                    {
                        Type = KnowledgeType.QA,
                        Topic = ExtractTopic(userQuestion),
                        Summary = $"Q: {TruncateText(userQuestion, 100)}\nA: {TruncateText(aiAnswer, 200)}",
                        Content = $"问题: {userQuestion}\n\n回答: {aiAnswer}",
                        Keywords = ExtractKeywords(userQuestion + " " + aiAnswer),
                        Importance = CalculateImportance(userQuestion, aiAnswer),
                        Confidence = 0.9,
                        SourceDialogue = $"{userQuestion}\n---\n{aiAnswer}"
                    };
                }

                // 检测概念定义模式
                if (IsConceptDefinition(userQuestion, aiAnswer))
                {
                    return new ExtractedKnowledge
                    {
                        Type = KnowledgeType.Concept,
                        Topic = ExtractConceptName(userQuestion),
                        Summary = ExtractDefinition(aiAnswer),
                        Content = aiAnswer,
                        Keywords = ExtractKeywords(userQuestion + " " + aiAnswer),
                        Importance = 80,
                        Confidence = 0.85,
                        SourceDialogue = $"{userQuestion}\n---\n{aiAnswer}"
                    };
                }

                // 检测流程步骤模式
                if (IsProcedureDescription(aiAnswer))
                {
                    return new ExtractedKnowledge
                    {
                        Type = KnowledgeType.Procedure,
                        Topic = ExtractTopic(userQuestion),
                        Summary = $"如何{TruncateText(userQuestion, 50)}",
                        Content = aiAnswer,
                        Keywords = ExtractKeywords(userQuestion + " " + aiAnswer),
                        Importance = 75,
                        Confidence = 0.8,
                        SourceDialogue = $"{userQuestion}\n---\n{aiAnswer}"
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[KnowledgeExtraction] 规则提取异常");
                return null;
            }
        }

        /// <summary>
        /// 基于LLM的深度提取
        /// TODO: 后续集成LLM深度提取
        /// </summary>
        private async Task<List<ExtractedKnowledge>> ExtractByLLMAsync(
            string userQuestion,
            string aiAnswer,
            DialogueContext context)
        {
            // 暂时返回空列表,后续集成LLM提取
            await Task.CompletedTask;
            return new List<ExtractedKnowledge>();
        }

        /// <summary>
        /// 构建提取提示词
        /// </summary>
        private string BuildExtractionPrompt(string userQuestion, string aiAnswer, DialogueContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 知识提取任务");
            sb.AppendLine();
            sb.AppendLine("从以下对话中提取可以长期保存的知识点:");
            sb.AppendLine();
            sb.AppendLine($"**用户问题**: {userQuestion}");
            sb.AppendLine();
            sb.AppendLine($"**AI回答**: {aiAnswer}");
            sb.AppendLine();

            // 添加上下文
            if (context?.UserProfile != null && !string.IsNullOrEmpty(context.UserProfile.PreferencesSummary))
            {
                sb.AppendLine($"**用户背景**: {context.UserProfile.PreferencesSummary}");
                sb.AppendLine();
            }

            sb.AppendLine("## 提取要求");
            sb.AppendLine();
            sb.AppendLine("1. 识别知识类型: concept(概念), fact(事实), procedure(流程), experience(经验), qa(问答), preference(偏好)");
            sb.AppendLine("2. 提取主题/标签(简短关键词)");
            sb.AppendLine("3. 生成知识摘要(50-100字)");
            sb.AppendLine("4. 提取关键词(3-5个)");
            sb.AppendLine("5. 评估重要性(0-100分)");
            sb.AppendLine();
            sb.AppendLine("## 输出格式(JSON数组)");
            sb.AppendLine();
            sb.AppendLine("```json");
            sb.AppendLine("[");
            sb.AppendLine("  {");
            sb.AppendLine("    \"type\": \"qa\",");
            sb.AppendLine("    \"topic\": \"主题\",");
            sb.AppendLine("    \"summary\": \"摘要\",");
            sb.AppendLine("    \"keywords\": [\"关键词1\", \"关键词2\"],");
            sb.AppendLine("    \"importance\": 75");
            sb.AppendLine("  }");
            sb.AppendLine("]");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("**注意**: 只提取有价值的知识,不要提取无意义的闲聊内容。如果没有可提取的知识,返回空数组[]。");

            return sb.ToString();
        }

        /// <summary>
        /// 解析LLM响应
        /// </summary>
        private List<ExtractedKnowledge> ParseLLMResponse(string response, string userQuestion, string aiAnswer)
        {
            try
            {
                // 提取JSON部分
                var jsonMatch = Regex.Match(response, @"\[[\s\S]*?\]", RegexOptions.Multiline);
                if (!jsonMatch.Success)
                {
                    return new List<ExtractedKnowledge>();
                }

                var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonMatch.Value);
                if (jsonArray == null || jsonArray.Count == 0)
                {
                    return new List<ExtractedKnowledge>();
                }

                var result = new List<ExtractedKnowledge>();
                foreach (var item in jsonArray)
                {
                    var knowledge = new ExtractedKnowledge
                    {
                        Type = ParseKnowledgeType(item.GetValueOrDefault("type")?.ToString()),
                        Topic = item.GetValueOrDefault("topic")?.ToString() ?? "",
                        Summary = item.GetValueOrDefault("summary")?.ToString() ?? "",
                        Content = aiAnswer,
                        Keywords = ParseKeywords(item.GetValueOrDefault("keywords")?.ToString() ?? ""),
                        Importance = ParseInt(item.GetValueOrDefault("importance"), 50),
                        Confidence = 0.75,
                        SourceDialogue = $"{userQuestion}\n---\n{aiAnswer}"
                    };

                    result.Add(knowledge);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[KnowledgeExtraction] 解析LLM响应失败");
                return new List<ExtractedKnowledge>();
            }
        }

        /// <summary>
        /// 将提取的知识更新到长期记忆
        /// </summary>
        public async Task<string> UpdateLongTermMemoryAsync(
            ExtractedKnowledge knowledge,
            string AppID,
            string ClawID,
            string SessionID,
            string MemberID,
            LargeModelConfig embeddingModelConfig = null)
        {
            try
            {
                // 检查是否已存在相似知识
                var existingMemories = LongTermMemoryBusiness.GetByTopicAndMember(
                    AppID, MemberID, knowledge.Topic, 5);

                // 如果存在相似知识,更新而不是创建新的
                var similarMemory = FindSimilarMemory(existingMemories, knowledge);
                if (similarMemory != null)
                {
                    // 更新现有记忆
                    similarMemory.Content = MergeContent(similarMemory.Content, knowledge.Content);
                    similarMemory.Summary = knowledge.Summary; // 使用最新摘要
                    similarMemory.Importance = Math.Max(similarMemory.Importance, knowledge.Importance);
                    similarMemory.AccessCount++;
                    similarMemory.LastAccessTime = DateTime.Now;
                    similarMemory.LastUpdateTime = DateTime.Now;

                    // 如果提供了向量模型，重新生成 embedding
                    if (embeddingModelConfig != null && embeddingModelConfig.Model != null)
                    {
                        try
                        {
                            var embeddingVector = await _kernelService.GenerateEmbeddingAsync(
                                embeddingModelConfig.Model,
                                knowledge.Summary);

                            similarMemory.Embedding = JsonConvert.SerializeObject(embeddingVector);
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.LogWarning(_logger, ClawLogModules.MEMORY,
                                $"[KnowledgeExtraction] 重新生成向量嵌入失败: {ex.Message}");
                        }
                    }

                    LongTermMemoryBusiness.Update(similarMemory);
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                        $"[KnowledgeExtraction] 更新现有记忆 - MemoryID: {similarMemory.MemoryID}, Topic: {knowledge.Topic}");

                    return similarMemory.MemoryID;
                }

                // 生成向量嵌入
                string embeddingJson = string.Empty;
                if (embeddingModelConfig != null && embeddingModelConfig.Model != null)
                {
                    try
                    {
                        var embeddingVector = await _kernelService.GenerateEmbeddingAsync(
                            embeddingModelConfig.Model,
                            knowledge.Summary);

                        embeddingJson = JsonConvert.SerializeObject(embeddingVector);
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, "[KnowledgeExtraction] 向量嵌入已生成");
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogWarning(_logger, ClawLogModules.MEMORY,
                            $"[KnowledgeExtraction] 生成向量嵌入失败: {ex.Message}，将继续保存但不包含向量");
                    }
                }

                // 创建新记忆
                var newMemory = new LongTermMemoryInfo
                {
                    MemoryID = Guid.NewGuid().ToString(),
                    AppID = AppID,
                    ClawID = ClawID,
                    SessionID = SessionID,
                    MemberID = MemberID,
                    KnowledgeType = knowledge.Type.ToString().ToLower(),
                    Topic = knowledge.Topic,
                    Summary = knowledge.Summary,
                    Content = knowledge.Content,
                    Embedding = embeddingJson,
                    Importance = knowledge.Importance,
                    AccessCount = 0,
                    SourceType = "dialogue",
                    SourceID = SessionID,
                    Metadata = JsonConvert.SerializeObject(new
                    {
                        Keywords = knowledge.Keywords,
                        Confidence = knowledge.Confidence,
                        ExtractedAt = DateTime.Now
                    }),
                    CreateTime = DateTime.Now,
                    LastUpdateTime = DateTime.Now
                };

                string memoryId = LongTermMemoryBusiness.Add(newMemory);
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, 
                    $"[KnowledgeExtraction] 创建新记忆 - MemoryID: {memoryId}, Type: {knowledge.Type}, Topic: {knowledge.Topic}");

                return memoryId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KnowledgeExtraction] 更新长期记忆失败");
                return string.Empty;
            }
        }

        /// <summary>
        /// 批量处理对话历史
        /// </summary>
        public async Task<int> ProcessChatHistoryAsync(
            List<AppChatLogInfo> chatHistory,
            string AppID,
            string ClawID,
            string SessionID,
            string MemberID,
            LargeModelConfig embeddingModelConfig = null)
        {
            try
            {
                int extractedCount = 0;

                // 按对话对处理
                for (int i = 0; i < chatHistory.Count - 1; i++)
                {
                    var userMsg = chatHistory[i];
                    var aiMsg = chatHistory[i + 1];

                    if (userMsg.Role == "user" && aiMsg.Role == "assistant")
                    {
                        var context = new DialogueContext
                        {
                            ChatHistory = chatHistory.Take(i + 2).ToList()
                        };

                        var knowledgeList = await ExtractKnowledgeFromDialogueAsync(
                            userMsg.Content?.ToString() ?? "",
                            aiMsg.Content?.ToString() ?? "",
                            context);

                        foreach (var knowledge in knowledgeList)
                        {
                            var memoryId = await UpdateLongTermMemoryAsync(
                                knowledge, AppID, ClawID, SessionID, MemberID, embeddingModelConfig);

                            if (!string.IsNullOrEmpty(memoryId))
                            {
                                extractedCount++;
                            }
                        }

                        i++; // 跳过AI消息
                    }
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                    $"[KnowledgeExtraction] 批量处理完成 - 提取数量: {extractedCount}");

                return extractedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KnowledgeExtraction] 批量处理失败");
                return 0;
            }
        }

        /// <summary>
        /// 合并和去重相似知识
        /// </summary>
        public async Task<int> MergeAndDeduplicateKnowledgeAsync(string AppID, string MemberID)
        {
            try
            {
                // 获取该用户的所有长期记忆
                var allMemories = LongTermMemoryBusiness.GetByMemberAndApp(MemberID, AppID, 1000);
                
                int mergedCount = 0;
                var processedIds = new HashSet<string>();

                // 按主题分组
                var groupedByTopic = allMemories.GroupBy(m => m.Topic);

                foreach (var group in groupedByTopic)
                {
                    var memories = group.ToList();
                    if (memories.Count < 2) continue;

                    // 找出相似的记忆对
                    for (int i = 0; i < memories.Count - 1; i++)
                    {
                        if (processedIds.Contains(memories[i].MemoryID)) continue;

                        for (int j = i + 1; j < memories.Count; j++)
                        {
                            if (processedIds.Contains(memories[j].MemoryID)) continue;

                            // 计算相似度
                            var similarity = CalculateSimilarity(
                                memories[i].Summary, memories[j].Summary);

                            if (similarity > 0.7) // 相似度阈值
                            {
                                // 合并两个记忆
                                MergeMemories(memories[i], memories[j]);
                                processedIds.Add(memories[j].MemoryID);
                                
                                // 删除被合并的记忆
                                LongTermMemoryBusiness.Delete(memories[j].MemoryID);
                                mergedCount++;
                            }
                        }
                    }
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, 
                    $"[KnowledgeExtraction] 去重完成 - 合并数量: {mergedCount}");

                return mergedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KnowledgeExtraction] 去重失败");
                return 0;
            }
        }

        #region 辅助方法

        private bool ShouldUseLLMExtraction(string userQuestion, string aiAnswer, DialogueContext context)
        {
            // 对话长度足够
            if (userQuestion.Length < 10 || aiAnswer.Length < 50)
                return false;

            // 不是简单问候
            var greetings = new[] { "你好", "hi", "hello", "谢谢", "再见" };
            if (greetings.Any(g => userQuestion.ToLower().Contains(g)))
                return false;

            // 回答包含实质内容
            if (aiAnswer.Length > 200)
                return true;

            // 任务规划复杂
            if (context?.TaskPlanning != null && context.TaskPlanning.TotalSteps >= 3)
                return true;

            return false;
        }

        private bool IsQuestionAnswerPair(string question, string answer)
        {
            var questionPatterns = new[] { "什么", "如何", "怎么", "为什么", "是否", "能否", "?", "？" };
            return questionPatterns.Any(p => question.Contains(p)) && answer.Length > 20;
        }

        private bool IsConceptDefinition(string question, string answer)
        {
            var patterns = new[] { "是什么", "什么是", "定义", "解释", "介绍" };
            return patterns.Any(p => question.Contains(p));
        }

        private bool IsProcedureDescription(string answer)
        {
            var patterns = new[] { "步骤", "第一", "第二", "首先", "然后", "最后", "1.", "2.", "3." };
            return patterns.Any(p => answer.Contains(p));
        }

        private string ExtractTopic(string text)
        {
            // 提取前20个字符作为主题
            return TruncateText(text.Replace("?", "").Replace("？", ""), 20);
        }

        private string ExtractConceptName(string question)
        {
            // 提取"什么是XXX"中的XXX
            var match = Regex.Match(question, @"(?:什么是|是什么|定义|解释)(.{1,20})");
            return match.Success ? match.Groups[1].Value.Trim() : ExtractTopic(question);
        }

        private string ExtractDefinition(string answer)
        {
            // 提取前100个字符作为定义
            return TruncateText(answer, 100);
        }

        private List<string> ExtractKeywords(string text)
        {
            // 简单的关键词提取(可以后续优化为TF-IDF或其他算法)
            var words = Regex.Split(text, @"[,，。.!！?？\s]+")
                .Where(w => w.Length >= 2 && w.Length <= 10)
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            return words;
        }

        private int CalculateImportance(string question, string answer)
        {
            int importance = 50;

            // 问题长度
            if (question.Length > 50) importance += 10;

            // 回答长度
            if (answer.Length > 200) importance += 10;
            if (answer.Length > 500) importance += 10;

            // 包含关键词
            var importantKeywords = new[] { "重要", "关键", "核心", "必须", "注意" };
            if (importantKeywords.Any(k => answer.Contains(k))) importance += 15;

            return Math.Min(100, importance);
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        private KnowledgeType ParseKnowledgeType(string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr)) return KnowledgeType.QA;

            return typeStr.ToLower() switch
            {
                "concept" => KnowledgeType.Concept,
                "fact" => KnowledgeType.Fact,
                "procedure" => KnowledgeType.Procedure,
                "experience" => KnowledgeType.Experience,
                "qa" => KnowledgeType.QA,
                "preference" => KnowledgeType.Preference,
                _ => KnowledgeType.QA
            };
        }

        private List<string> ParseKeywords(string keywordsStr)
        {
            try
            {
                if (string.IsNullOrEmpty(keywordsStr))
                    return new List<string>();
                    
                var jArray = JsonConvert.DeserializeObject<List<string>>(keywordsStr);
                return jArray ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private int ParseInt(object obj, int defaultValue)
        {
            if (obj == null) return defaultValue;
            if (int.TryParse(obj.ToString(), out int result))
                return result;
            return defaultValue;
        }

        private LongTermMemoryInfo FindSimilarMemory(
            List<LongTermMemoryInfo> memories,
            ExtractedKnowledge knowledge)
        {
            foreach (var memory in memories)
            {
                var similarity = CalculateSimilarity(memory.Summary, knowledge.Summary);
                if (similarity > 0.7) // 相似度阈值70%
                {
                    return memory;
                }
            }
            return null;
        }

        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0;

            // 简单的Jaccard相似度
            var words1 = new HashSet<string>(text1.Split(new[] { ' ', ',', '，', '。', '.' }, 
                StringSplitOptions.RemoveEmptyEntries));
            var words2 = new HashSet<string>(text2.Split(new[] { ' ', ',', '，', '。', '.' }, 
                StringSplitOptions.RemoveEmptyEntries));

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union > 0 ? (double)intersection / union : 0;
        }

        private string MergeContent(string content1, string content2)
        {
            // 简单合并,避免重复
            if (content1.Contains(content2))
                return content1;
            if (content2.Contains(content1))
                return content2;

            return $"{content1}\n\n---\n\n{content2}";
        }

        private void MergeMemories(LongTermMemoryInfo target, LongTermMemoryInfo source)
        {
            // 合并内容
            target.Content = MergeContent(target.Content ?? "", source.Content ?? "");
            
            // 取更高的重要性
            target.Importance = Math.Max(target.Importance, source.Importance);
            
            // 累加访问次数
            target.AccessCount += source.AccessCount;
            
            // 更新时间
            target.LastUpdateTime = DateTime.Now;
            
            // 保存
            LongTermMemoryBusiness.Update(target);
        }

        #endregion
    }
}
