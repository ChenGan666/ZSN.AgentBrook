using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Entity.Model;
using ZSN.AI.Node.Claw.Utils;

namespace ZSN.AI.Node.Services
{
    /// <summary>
    /// LLM增强的知识图谱服务
    /// 对重要知识使用大模型进行深度语义分析来构建知识图谱
    /// </summary>
    public class KnowledgeGraphLLMService
    {
        private readonly ILogger<KnowledgeGraphLLMService> _logger;
        private readonly IChatService _chatService;

        public KnowledgeGraphLLMService(
            ILogger<KnowledgeGraphLLMService> logger,
            IChatService chatService)
        {
            _logger = logger;
            _chatService = chatService;
        }

        /// <summary>
        /// 使用LLM发现知识关系（主要方法）
        /// </summary>
        public async Task<List<KnowledgeRelationInfo>> DiscoverRelationsWithLLMAsync(
            string memoryId,
            string appId,
            LargeModelInfo modelInfo,
            string memberId = null,
            string clawId = null,
            int maxRelations = 10)
        {
            var relations = new List<KnowledgeRelationInfo>();

            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] 开始分析知识 {memoryId}");

                // 1. 获取源知识
                var sourceMemory = LongTermMemoryBusiness.GetModel(memoryId);
                if (sourceMemory == null)
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.MEMORY,
                        $"[LLM知识图谱] 未找到知识 {memoryId}");
                    return relations;
                }

                // 2. 查找候选相关知识（使用语义搜索缩小范围）
                var candidateMemories = LongTermMemoryBusiness.SearchBySimilarity(
                    sourceMemory.Summary,
                    appId,
                    memberId,
                    clawId,
                    topK: 20,
                    minSimilarity: 0.5f);

                // 过滤掉源知识本身
                candidateMemories = candidateMemories
                    .Where(m => m.MemoryID != memoryId)
                    .Take(maxRelations * 2)
                    .ToList();

                if (candidateMemories.Count == 0)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                        $"[LLM知识图谱] 未找到候选知识");
                    return relations;
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] 找到 {candidateMemories.Count} 个候选知识");

                // 3. 使用LLM深度分析关系
                var llmRelations = await AnalyzeRelationsWithLLMAsync(
                    sourceMemory,
                    candidateMemories,
                    modelInfo);

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] LLM分析了 {llmRelations.Count} 个关系");

                // 4. 创建LLM发现的关系
                foreach (var llmRel in llmRelations)
                {
                    var existingStrength = KnowledgeRelationBusiness.GetStrength(
                        memoryId, llmRel.TargetMemoryId, llmRel.RelationType);

                    if (existingStrength > 0)
                    {
                        LoggerHelper.LogDebug(_logger, ClawLogModules.MEMORY,
                            $"[LLM知识图谱] 关系已存在，跳过");
                        continue;
                    }

                    var relationId = KnowledgeRelationBusiness.CreateRelation(
                        appId,
                        memoryId,
                        llmRel.TargetMemoryId,
                        llmRel.RelationType,
                        llmRel.Strength,
                        llmRel.Metadata);

                    var relation = KnowledgeRelationBusiness.GetModel(relationId);
                    if (relation != null)
                    {
                        relations.Add(relation);
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                            $"[LLM知识图谱] ✓ 创建关系: {llmRel.RelationType} (强度={llmRel.Strength:F3}) - {llmRel.Reason}");
                    }
                }

                // 5. 如果LLM发现的关系较少，补充基于规则的关系
                if (relations.Count < maxRelations)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                        $"[LLM知识图谱] LLM发现关系较少，补充规则方法");

                    var ruleBasedRelations = KnowledgeRelationBusiness.DiscoverRelations(
                        memoryId,
                        appId,
                        memberId,
                        clawId,
                        similarityThreshold: 0.7f,
                        maxRelations: maxRelations - relations.Count);

                    foreach (var ruleRel in ruleBasedRelations)
                    {
                        bool exists = relations.Any(r =>
                            r.TargetMemoryID == ruleRel.TargetMemoryID &&
                            r.RelationType == ruleRel.RelationType);

                        if (!exists)
                        {
                            relations.Add(ruleRel);
                        }
                    }
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] ✓ 完成，共创建 {relations.Count} 个关系");

                return relations;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] LLM调用失败，回退到规则方法: {ex.Message}");

                // 回退到基于规则的方法
                var ruleRelations = KnowledgeRelationBusiness.DiscoverRelations(
                    memoryId,
                    appId,
                    memberId,
                    clawId,
                    0.7f,
                    maxRelations);

                return ruleRelations.ToList();
            }
        }

        /// <summary>
        /// 使用LLM分析知识之间的关系
        /// </summary>
        private async Task<List<LLMDiscoveredRelation>> AnalyzeRelationsWithLLMAsync(
            LongTermMemoryInfo sourceMemory,
            List<LongTermMemoryInfo> candidateMemories,
            LargeModelInfo modelInfo)
        {
            var discoveredRelations = new List<LLMDiscoveredRelation>();

            try
            {
                // 1. 构建提示词
                string prompt = BuildRelationAnalysisPrompt(sourceMemory, candidateMemories);

                LoggerHelper.LogDebug(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] 提示词长度: {prompt.Length} 字符");

                // 2. 转换为 LargeModelConfig
                var modelConfig = new LargeModelConfig
                {
                    Model = modelInfo,
                    Temperature = 0.3,
                    AnswerTokens = 2000,
                    TopPCoefficient = 0.95
                };

                // 3. 构建聊天历史
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] 调用模型: {modelInfo.ModelName}");

                // 4. 流式调用LLM
                var chatResult = _chatService.SendChatAsync(
                    modelConfig,
                    chatHistory,
                    Function: null,
                    responseFormat: "text",
                    enableStreamingObservation: false,
                    progress: null,
                    ct: CancellationToken.None
                );

                // 5. 流式读取响应
                var rawContent = new StringBuilder();
                await foreach (var content in chatResult)
                {
                    rawContent.Append(content);
                }
                var resultText = rawContent.ToString();

                if (string.IsNullOrEmpty(resultText))
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.MEMORY,
                        "[LLM知识图谱] LLM返回空结果");
                    return discoveredRelations;
                }

                LoggerHelper.LogDebug(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] LLM响应长度: {resultText.Length} 字符");

                // 6. 解析响应
                discoveredRelations = ParseLLMRelationResponse(resultText, candidateMemories);

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] 解析到 {discoveredRelations.Count} 个有效关系");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] 分析失败: {ex.Message}");
            }

            return discoveredRelations;
        }

        /// <summary>
        /// 构建关系分析提示词
        /// </summary>
        private string BuildRelationAnalysisPrompt(
            LongTermMemoryInfo sourceMemory,
            List<LongTermMemoryInfo> candidateMemories)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# 知识关系分析任务");
            sb.AppendLine();
            sb.AppendLine("你是一个知识图谱专家，需要分析源知识与候选知识之间的关系。");
            sb.AppendLine();

            // 源知识信息
            sb.AppendLine("## 源知识");
            sb.AppendLine();
            sb.AppendLine($"- **主题**: {sourceMemory.Topic}");
            sb.AppendLine($"- **类型**: {sourceMemory.KnowledgeType}");
            sb.AppendLine($"- **摘要**: {sourceMemory.Summary}");
            sb.AppendLine($"- **内容**: {TruncateContent(sourceMemory.Content, 200)}");
            sb.AppendLine();

            // 候选知识信息
            sb.AppendLine("## 候选知识");
            sb.AppendLine();
            for (int i = 0; i < Math.Min(candidateMemories.Count, 10); i++)
            {
                var candidate = candidateMemories[i];
                sb.AppendLine($"### 候选{i + 1}");
                sb.AppendLine($"- **主题**: {candidate.Topic}");
                sb.AppendLine($"- **类型**: {candidate.KnowledgeType}");
                sb.AppendLine($"- **摘要**: {candidate.Summary}");
                sb.AppendLine($"- **内容**: {TruncateContent(candidate.Content, 150)}");
                sb.AppendLine();
            }

            // 关系类型定义
            sb.AppendLine("## 关系类型定义");
            sb.AppendLine();
            sb.AppendLine("| 类型代码 | 类型名称 | 说明 | 示例 |");
            sb.AppendLine("|---------|---------|------|------|");
            sb.AppendLine("| `related` | 相关知识 | 内容相关但没有明显从属关系 | \"函数\" ↔ \"方法\" |");
            sb.AppendLine("| `prerequisite` | 前置知识 | 源知识是理解目标知识的基础（源→目标）| \"变量\" → \"函数\" |");
            sb.AppendLine("| `derived` | 派生知识 | 目标知识是从源知识派生的（源←目标）| \"面向对象\" → \"类\" |");
            sb.AppendLine("| `conflict` | 冲突知识 | 两个知识相互矛盾 | \"Python2\" ↔ \"Python3\" |");
            sb.AppendLine("| `example` | 示例关系 | 目标是源的具体示例 | \"编程语言\" → \"Python示例\" |");
            sb.AppendLine("| `category` | 分类关系 | 目标属于源的某个类别 | \"数据库\" → \"PostgreSQL\" |");
            sb.AppendLine();

            // 分析要求
            sb.AppendLine("## 分析要求");
            sb.AppendLine();
            sb.AppendLine("1. **深度语义理解**: 不仅要看关键词匹配，还要理解深层含义");
            sb.AppendLine("2. **领域知识**: 利用你的领域知识来判断关系");
            sb.AppendLine("3. **因果关系**: 识别知识之间的因果、依赖、先后关系");
            sb.AppendLine("4. **关系强度**: 评估关系的强度（0-1之间的浮点数）");
            sb.AppendLine("5. **严格筛选**: 只创建确实有关系且关系强度>=0.6的关系");
            sb.AppendLine();

            // 输出格式
            sb.AppendLine("## 输出格式");
            sb.AppendLine();
            sb.AppendLine("请以JSON数组格式输出分析结果，只包含确实存在关系（强度>=0.6）的知识对：");
            sb.AppendLine();
            sb.AppendLine("```json");
            sb.AppendLine("[");
            sb.AppendLine("  {");
            sb.AppendLine("    \"candidate_index\": 1,");
            sb.AppendLine("    \"relation_type\": \"prerequisite\",");
            sb.AppendLine("    \"strength\": 0.85,");
            sb.AppendLine("    \"reason\": \"源知识'变量'是理解目标知识'函数'的基础，函数需要使用变量来存储数据\"");
            sb.AppendLine("  }");
            sb.AppendLine("]");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("**注意**: 如果没有发现任何关系（强度<0.6），请返回空数组 `[]`");

            return sb.ToString();
        }

        /// <summary>
        /// 解析LLM的关系分析响应
        /// </summary>
        private List<LLMDiscoveredRelation> ParseLLMRelationResponse(
            string response,
            List<LongTermMemoryInfo> candidateMemories)
        {
            var relations = new List<LLMDiscoveredRelation>();

            try
            {
                string jsonStr = response;

                // 1. 提取JSON部分（处理markdown代码块）
                if (response.Contains("```json"))
                {
                    int startIdx = response.IndexOf("```json") + 7;
                    int endIdx = response.IndexOf("```", startIdx);
                    if (endIdx > startIdx)
                    {
                        jsonStr = response.Substring(startIdx, endIdx - startIdx).Trim();
                    }
                }
                else if (response.Contains("```"))
                {
                    int startIdx = response.IndexOf("```") + 3;
                    int endIdx = response.IndexOf("```", startIdx);
                    if (endIdx > startIdx)
                    {
                        jsonStr = response.Substring(startIdx, endIdx - startIdx).Trim();
                    }
                }

                // 2. 查找JSON数组
                int bracketStart = jsonStr.IndexOf('[');
                int bracketEnd = jsonStr.LastIndexOf(']');
                if (bracketStart >= 0 && bracketEnd > bracketStart)
                {
                    jsonStr = jsonStr.Substring(bracketStart, bracketEnd - bracketStart + 1);
                }

                // 3. 解析JSON
                var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonStr);
                if (jsonArray != null)
                {
                    foreach (var item in jsonArray)
                    {
                        if (item == null) continue;

                        // 解析候选索引
                        if (!int.TryParse(item["candidate_index"]?.ToString(), out int candidateIndex) ||
                            candidateIndex < 1 || candidateIndex > candidateMemories.Count)
                        {
                            continue;
                        }

                        // 解析关系类型
                        string relationType = item["relation_type"]?.ToString()?.ToLower() ?? "related";
                        string[] validTypes = { "related", "prerequisite", "derived", "conflict", "example", "category" };
                        if (!validTypes.Contains(relationType))
                        {
                            relationType = "related";
                        }

                        // 解析关系强度
                        if (!float.TryParse(item["strength"]?.ToString(), out float strength) || strength < 0.6f)
                        {
                            continue;  // 跳过低质量关系
                        }

                        // 解析原因
                        string reason = item["reason"]?.ToString() ?? "LLM发现的关系";

                        // 创建关系对象
                        relations.Add(new LLMDiscoveredRelation
                        {
                            TargetMemoryId = candidateMemories[candidateIndex - 1].MemoryID,
                            RelationType = relationType,
                            Strength = strength,
                            Reason = reason,
                            Metadata = JsonConvert.SerializeObject(new
                            {
                                discovered_by = "llm",
                                reason = reason,
                                confidence = strength,
                                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            })
                        });

                        LoggerHelper.LogDebug(_logger, ClawLogModules.MEMORY,
                            $"[LLM知识图谱] 解析关系: {relationType} (强度={strength:F3}) - {reason}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] 解析LLM响应失败: {ex.Message}");
                LoggerHelper.LogDebug(_logger, ClawLogModules.MEMORY,
                    $"[LLM知识图谱] 响应内容: {response.Substring(0, Math.Min(500, response.Length))}...");
            }

            return relations;
        }

        /// <summary>
        /// 截断内容到指定长度
        /// </summary>
        private string TruncateContent(string content, int maxLength)
        {
            if (string.IsNullOrEmpty(content)) return "";
            return content.Length <= maxLength ? content : content.Substring(0, maxLength) + "...";
        }
    }

    /// <summary>
    /// LLM发现的关系
    /// </summary>
    internal class LLMDiscoveredRelation
    {
        public string TargetMemoryId { get; set; }
        public string RelationType { get; set; }
        public float Strength { get; set; }
        public string Reason { get; set; }
        public string Metadata { get; set; }
    }
}
