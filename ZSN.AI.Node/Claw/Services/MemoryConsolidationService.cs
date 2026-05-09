using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Models;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Node.Services;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// 记忆整理服务实现
    /// 定时对ClawAI的记忆进行重新组织和优化，支持三级记忆层级
    /// 不影响现有的实时记忆处理逻辑
    /// </summary>
    public class MemoryConsolidationService : IMemoryConsolidationService
    {
        private readonly ILogger<MemoryConsolidationService> _logger;
        private readonly IChatService _chatService;
        private readonly IKernelService _kernelService;
        private readonly IKnowledgeExtractionService _knowledgeExtractionService;
        private readonly KnowledgeGraphLLMService _knowledgeGraphLLMService;
        private readonly MemoryConsolidationOptions _options;

        private const string LOG_MODULE = "MemoryConsolidation";

        public MemoryConsolidationService(
            ILogger<MemoryConsolidationService> logger,
            IChatService chatService,
            IKernelService kernelService,
            IKnowledgeExtractionService knowledgeExtractionService,
            KnowledgeGraphLLMService knowledgeGraphLLMService,
            IOptions<ClawAIOptions> clawAIOptions)
        {
            _logger = logger;
            _chatService = chatService;
            _kernelService = kernelService;
            _knowledgeExtractionService = knowledgeExtractionService;
            _knowledgeGraphLLMService = knowledgeGraphLLMService;
            _options = clawAIOptions.Value?.MemoryConsolidation ?? new MemoryConsolidationOptions();
        }

        #region 入口方法

        /// <summary>
        /// 执行 ClawAI 级记忆整理
        /// </summary>
        public async Task<MemoryConsolidationResult> ConsolidateClawAIAsync(
            string appId,
            string clawId,
            DateTime cutoffTime,
            LargeModelInfo modelInfo,
            LargeModelInfo embeddingModelInfo)
        {
            var result = new MemoryConsolidationResult
            {
                Scope = MemoryScope.ClawAI,
                ScopeLabel = $"APP:{appId}/Claw:{clawId}"
            };

            var scope = new MemoryScopeContext
            {
                Scope = MemoryScope.ClawAI,
                AppID = appId,
                ClawID = clawId
            };

            LoggerHelper.LogInfo(_logger, LOG_MODULE,
                $"=== 开始 ClawAI 级整理 {result.ScopeLabel}，时间窗口: {cutoffTime:yyyy-MM-dd HH:mm:ss} ===");

            try
            {
                // Step 1: 清理低价值记忆
                result.CleanedMemories = await CleanupLowValueMemoriesAsync(scope);
                LoggerHelper.LogInfo(_logger, LOG_MODULE, $"Step1 清理完成: {result.CleanedMemories} 条");

                // Step 2: 深度知识提炼
                result.ExtractedKnowledge = await DeepKnowledgeExtractionAsync(
                    scope, modelInfo, _options.KnowledgeExtractionBatchSize, cutoffTime);
                LoggerHelper.LogInfo(_logger, LOG_MODULE, $"Step2 知识提炼完成: {result.ExtractedKnowledge} 条");

                // Step 3: 知识图谱构建
                result.GraphRelationsBuilt = await BuildScopedKnowledgeGraphAsync(
                    scope, modelInfo, _options.GraphBuildBatchSize);
                LoggerHelper.LogInfo(_logger, LOG_MODULE, $"Step3 图谱构建完成: {result.GraphRelationsBuilt} 条");

                // Step 4: 规划经验整理
                result.PlanningExperienceConsolidated = await ConsolidatePlanningExperienceAsync(scope, modelInfo);
                LoggerHelper.LogInfo(_logger, LOG_MODULE, $"Step4 规划整理完成: {result.PlanningExperienceConsolidated} 条");

                // Step 6: 合并重评分
                result.MergedMemories = await MergeAndRescoreMemoriesAsync(scope);
                LoggerHelper.LogInfo(_logger, LOG_MODULE, $"Step6 合并重评分完成: {result.MergedMemories} 条");

                result.Summary = $"ClawAI级整理完成: 清理{result.CleanedMemories}条, " +
                    $"提炼{result.ExtractedKnowledge}条, 图谱{result.GraphRelationsBuilt}条, " +
                    $"规划{result.PlanningExperienceConsolidated}条, 合并{result.MergedMemories}条";
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"ClawAI级整理异常: {ex.Message}", ex);
                result.Summary = $"ClawAI级整理异常: {ex.Message}";
            }

            LoggerHelper.LogInfo(_logger, LOG_MODULE, result.Summary);
            return result;
        }

        /// <summary>
        /// 执行 APP 级记忆整理
        /// </summary>
        public async Task<MemoryConsolidationResult> ConsolidateAppAsync(
            string appId,
            DateTime cutoffTime,
            LargeModelInfo modelInfo,
            LargeModelInfo embeddingModelInfo)
        {
            var result = new MemoryConsolidationResult
            {
                Scope = MemoryScope.App,
                ScopeLabel = $"APP:{appId}"
            };

            var scope = new MemoryScopeContext
            {
                Scope = MemoryScope.App,
                AppID = appId
            };

            LoggerHelper.LogInfo(_logger, LOG_MODULE,
                $"=== 开始 APP 级整理 {result.ScopeLabel}，时间窗口: {cutoffTime:yyyy-MM-dd HH:mm:ss} ===");

            try
            {
                // Step 1: APP级清理
                result.CleanedMemories = await CleanupLowValueMemoriesAsync(scope);
                LoggerHelper.LogInfo(_logger, LOG_MODULE, $"Step1 APP级清理完成: {result.CleanedMemories} 条");

                // Step 3: APP级知识图谱
                result.GraphRelationsBuilt = await BuildScopedKnowledgeGraphAsync(
                    scope, modelInfo, _options.GraphBuildBatchSize);
                LoggerHelper.LogInfo(_logger, LOG_MODULE, $"Step3 APP级图谱完成: {result.GraphRelationsBuilt} 条");

                // Step 5: 知识层级提升
                result.PromotedToAppLevel = await PromoteKnowledgeToAppLevelAsync(
                    appId, modelInfo, embeddingModelInfo, null, _options.PromotionGeneralityThreshold);
                LoggerHelper.LogInfo(_logger, LOG_MODULE, $"Step5 知识提升完成: {result.PromotedToAppLevel} 条");

                // Step 6: APP级合并重评分
                result.MergedMemories = await MergeAndRescoreMemoriesAsync(scope);
                LoggerHelper.LogInfo(_logger, LOG_MODULE, $"Step6 APP级合并完成: {result.MergedMemories} 条");

                result.Summary = $"APP级整理完成: 清理{result.CleanedMemories}条, " +
                    $"图谱{result.GraphRelationsBuilt}条, 提升{result.PromotedToAppLevel}条, " +
                    $"合并{result.MergedMemories}条";
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"APP级整理异常: {ex.Message}", ex);
                result.Summary = $"APP级整理异常: {ex.Message}";
            }

            LoggerHelper.LogInfo(_logger, LOG_MODULE, result.Summary);
            return result;
        }

        #endregion

        #region Step 1: 清理低价值记忆

        public async Task<int> CleanupLowValueMemoriesAsync(MemoryScopeContext scope)
        {
            int cleaned = 0;

            try
            {
                switch (scope.Scope)
                {
                    case MemoryScope.Session:
                    case MemoryScope.ClawAI:
                        cleaned += CleanupClawAILevel(scope);
                        break;
                    case MemoryScope.App:
                        cleaned += CleanupAppLevel(scope);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"清理低价值记忆异常: {ex.Message}", ex);
            }

            return await Task.FromResult(cleaned);
        }

        /// <summary>
        /// ClawAI级清理：清理该ClawID下的问候语、低重要性、长期未访问记忆
        /// </summary>
        private int CleanupClawAILevel(MemoryScopeContext scope)
        {
            int cleaned = 0;

            // 获取该ClawID下的长期记忆
            var memories = LongTermMemoryBusiness.GetByClawID(scope.AppID, scope.ClawID, 1000);

            foreach (var memory in memories)
            {
                bool shouldClean = false;

                // 1. 问候语/简单对话检测
                if (IsTrivialContent(memory.Content) || IsTrivialContent(memory.Summary))
                {
                    if (memory.Importance < 50)
                        shouldClean = true;
                }

                // 2. 低重要性 + 零访问 + 长期未用
                if (!shouldClean &&
                    memory.Importance < _options.LowImportanceThresholdClawAI &&
                    memory.AccessCount == 0 &&
                    memory.LastAccessTime.HasValue &&
                    (DateTime.Now - memory.LastAccessTime.Value).TotalDays > _options.LongUnusedDaysClawAI)
                {
                    shouldClean = true;
                }

                // 3. 内容过短 + 低重要性
                if (!shouldClean &&
                    (memory.Content?.Length ?? 0) < _options.MinContentLength &&
                    memory.Importance < 30)
                {
                    shouldClean = true;
                }

                if (shouldClean)
                {
                    // 保留失败经验（避免丢失教训）
                    if (memory.KnowledgeType == "experience" && memory.Importance >= 20)
                        continue;

                    LongTermMemoryBusiness.Delete(memory.MemoryID);
                    cleaned++;
                    LoggerHelper.LogDebug(_logger, LOG_MODULE,
                        $"清理记忆: {memory.MemoryID} (类型={memory.KnowledgeType}, 重要性={memory.Importance})");
                }
            }

            return cleaned;
        }

        /// <summary>
        /// APP级清理：更保守，仅清理极低价值记忆
        /// </summary>
        private int CleanupAppLevel(MemoryScopeContext scope)
        {
            int cleaned = 0;

            // APP级知识：ClawID为空
            var memories = LongTermMemoryBusiness.GetList(
                $" app_id='{scope.AppID}' AND (claw_id IS NULL OR claw_id='') ");

            foreach (var memory in memories)
            {
                bool shouldClean = false;

                // APP级更保守：仅清理极低重要性 + 长期未访问
                if (memory.Importance < _options.LowImportanceThresholdApp &&
                    memory.AccessCount == 0 &&
                    memory.LastAccessTime.HasValue &&
                    (DateTime.Now - memory.LastAccessTime.Value).TotalDays > _options.LongUnusedDaysApp)
                {
                    shouldClean = true;
                }

                if (shouldClean)
                {
                    LongTermMemoryBusiness.Delete(memory.MemoryID);
                    cleaned++;
                }
            }

            return cleaned;
        }

        /// <summary>
        /// 判断内容是否为简单问候/感谢等无价值内容
        /// </summary>
        private bool IsTrivialContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;

            var allPatterns = _options.GreetingPatterns.Concat(_options.TrivialPatterns);
            foreach (var pattern in allPatterns)
            {
                if (content.Trim().Equals(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        #endregion

        #region Step 2: 深度知识提炼

        public async Task<int> DeepKnowledgeExtractionAsync(
            MemoryScopeContext scope,
            LargeModelInfo modelInfo,
            int batchSize = 20,
            DateTime? cutoffTime = null)
        {
            if (scope.Scope != MemoryScope.ClawAI || string.IsNullOrEmpty(scope.ClawID))
            {
                LoggerHelper.LogWarning(_logger, LOG_MODULE, "深度知识提炼需要 ClawAI 级上下文");
                return 0;
            }

            int extracted = 0;

            try
            {
                // 获取该AppID下的情景记忆
                var episodicMemories = EpisodicMemoryBusiness.GetByMemberAndApp("", scope.AppID, 5000);

                // 过滤：按时间窗口（增量）+ 排除已归档的
                if (cutoffTime.HasValue)
                {
                    episodicMemories = episodicMemories
                        .Where(m => m.CreateTime >= cutoffTime.Value)
                        .ToList();
                }

                // 过滤已归档到长期记忆的（Metadata中有 consolidated=true）
                episodicMemories = episodicMemories
                    .Where(m =>
                    {
                        // 简单判断：如果Importance太低则跳过
                        if (m.Importance < 20) return false;
                        return true;
                    })
                    .Take(batchSize)
                    .ToList();

                if (episodicMemories.Count == 0)
                {
                    LoggerHelper.LogInfo(_logger, LOG_MODULE, "未找到需要提炼的情景记忆");
                    return 0;
                }

                LoggerHelper.LogInfo(_logger, LOG_MODULE,
                    $"找到 {episodicMemories.Count} 条待提炼情景记忆");

                // 分批处理
                for (int i = 0; i < episodicMemories.Count; i += batchSize)
                {
                    var batch = episodicMemories.Skip(i).Take(batchSize).ToList();

                    foreach (var epMemory in batch)
                    {
                        try
                        {
                            var knowledge = await ExtractKnowledgeWithLLMAsync(epMemory, modelInfo);
                            if (knowledge != null)
                            {
                                // 保存到长期记忆，ClawID设置为当前节点
                                var memoryId = await _knowledgeExtractionService.UpdateLongTermMemoryAsync(
                                    knowledge,
                                    scope.AppID,
                                    scope.ClawID,
                                    epMemory.SessionID ?? "",
                                    epMemory.MemberID ?? "");

                                if (!string.IsNullOrEmpty(memoryId))
                                {
                                    extracted++;
                                    LoggerHelper.LogInfo(_logger, LOG_MODULE,
                                        $"提炼知识: {knowledge.Topic} (重要性={knowledge.Importance})");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.LogError(_logger, LOG_MODULE,
                                $"提炼情景记忆 {epMemory.MemoryID} 失败: {ex.Message}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"深度知识提炼异常: {ex.Message}", ex);
            }

            return extracted;
        }

        /// <summary>
        /// 使用LLM从情景记忆中提取结构化知识
        /// </summary>
        private async Task<ExtractedKnowledge> ExtractKnowledgeWithLLMAsync(
            EpisodicMemoryInfo episodicMemory,
            LargeModelInfo modelInfo)
        {
            var prompt = $@"你是一个知识管理专家。请从以下记忆内容中提取可以长期保存的结构化知识。

记忆类型：{episodicMemory.EventType}
记忆内容：{TruncateContent(episodicMemory.EventResult, 500)}
记忆上下文：{TruncateContent(episodicMemory.EventContext, 300)}
记忆摘要：{episodicMemory.Summary}

请提取以下信息：
1. 知识类型（concept/fact/procedure/experience/qa/preference）
2. 主题（简短关键词，10字以内）
3. 摘要（50-100字，核心知识点）
4. 详细内容（结构化整理后的知识）
5. 关键词（3-5个）
6. 重要性评分（0-100）
7. 通用性评分（0-100）：该知识是否具有跨ClawAI节点的通用价值？
   - 100=完全通用（如""什么是数据库""）
   - 50=部分通用（如""该项目的API设计规范""）
   - 0=完全特定（如""我的邮箱是xxx""）

以JSON格式输出。如果记忆内容没有可提取的知识价值，输出空数组[]。";

            var resultText = await CallLLMAsync(modelInfo, prompt, 2000);
            if (string.IsNullOrEmpty(resultText))
                return null;

            try
            {
                // 提取JSON
                string jsonStr = ExtractJsonArray(resultText);
                if (string.IsNullOrEmpty(jsonStr) || jsonStr.Trim() == "[]")
                    return null;

                var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonStr);
                if (items == null || items.Count == 0)
                    return null;

                var item = items[0];

                var knowledge = new ExtractedKnowledge
                {
                    Type = ParseEnum<KnowledgeType>(item, "知识类型") ?? KnowledgeType.Fact,
                    Topic = GetStringValue(item, "主题") ?? "未命名",
                    Summary = GetStringValue(item, "摘要") ?? "",
                    Content = GetStringValue(item, "详细内容") ?? "",
                    Importance = GetIntValue(item, "重要性评分", 50),
                    Confidence = 0.8
                };

                // 解析关键词
                var keywords = GetStringValue(item, "关键词") ?? "";
                if (!string.IsNullOrEmpty(keywords))
                {
                    knowledge.Keywords = keywords.Split(new[] { '、', ',', '，', ';' },
                        StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                // 保存通用性评分到SourceDialogue字段（临时借用）
                int generalityScore = GetIntValue(item, "通用性评分", 50);
                knowledge.SourceDialogue = $"generality_score:{generalityScore}";

                return knowledge;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"解析LLM知识提取结果失败: {ex.Message}", ex);
                return null;
            }
        }

        #endregion

        #region Step 3: 层级化知识图谱构建

        public async Task<int> BuildScopedKnowledgeGraphAsync(
            MemoryScopeContext scope,
            LargeModelInfo modelInfo,
            int batchSize = 20)
        {
            int relationsBuilt = 0;

            try
            {
                List<LongTermMemoryInfo> memories;

                if (scope.Scope == MemoryScope.ClawAI)
                {
                    // ClawAI级：同ClawID下所有长期记忆
                    memories = LongTermMemoryBusiness.GetByClawID(scope.AppID, scope.ClawID, 200);
                }
                else
                {
                    // APP级：同AppID下所有长期记忆
                    memories = LongTermMemoryBusiness.GetByApp(scope.AppID, 500);
                }

                // 过滤：只处理没有图谱关系的记忆
                var unprocessed = new List<LongTermMemoryInfo>();
                foreach (var memory in memories.Take(batchSize))
                {
                    var existingRelations = KnowledgeRelationBusiness.GetBySourceId(memory.MemoryID);
                    if (existingRelations == null || existingRelations.Count == 0)
                    {
                        unprocessed.Add(memory);
                    }
                }

                LoggerHelper.LogInfo(_logger, LOG_MODULE,
                    $"图谱构建: {scope.ScopeLabel}, 待处理 {unprocessed.Count} 条");

                foreach (var memory in unprocessed.Take(batchSize))
                {
                    try
                    {
                        var relations = await _knowledgeGraphLLMService.DiscoverRelationsWithLLMAsync(
                            memory.MemoryID,
                            scope.AppID,
                            modelInfo,
                            clawId: scope.Scope == MemoryScope.ClawAI ? scope.ClawID : null,
                            maxRelations: 5);

                        relationsBuilt += relations.Count;
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError(_logger, LOG_MODULE,
                            $"图谱构建失败 {memory.MemoryID}: {ex.Message}", ex);
                    }
                }

                // 清理孤立图谱关系
                CleanOrphanedRelations(scope);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"图谱构建异常: {ex.Message}", ex);
            }

            return relationsBuilt;
        }

        /// <summary>
        /// 清理关联的记忆已被删除的孤立图谱关系
        /// </summary>
        private void CleanOrphanedRelations(MemoryScopeContext scope)
        {
            try
            {
                var relations = KnowledgeRelationBusiness.GetList(
                    $" app_id='{scope.AppID}' ");

                foreach (var relation in relations)
                {
                    var source = LongTermMemoryBusiness.GetModel(relation.SourceMemoryID);
                    var target = LongTermMemoryBusiness.GetModel(relation.TargetMemoryID);

                    if (source == null || target == null)
                    {
                        KnowledgeRelationBusiness.Delete(relation.RelationID);
                        LoggerHelper.LogDebug(_logger, LOG_MODULE,
                            $"清理孤立关系: {relation.RelationID}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"清理孤立关系异常: {ex.Message}", ex);
            }
        }

        #endregion

        #region Step 4: 任务规划经验整理

        public async Task<int> ConsolidatePlanningExperienceAsync(
            MemoryScopeContext scope,
            LargeModelInfo modelInfo)
        {
            int consolidated = 0;

            try
            {
                if (scope.Scope != MemoryScope.ClawAI)
                {
                    LoggerHelper.LogWarning(_logger, LOG_MODULE, "规划经验整理需要 ClawAI 级上下文");
                    return 0;
                }

                // 获取该AppID下已完成的任务规划（通过NodeID关联ClawID）
                var completedPlans = TaskPlanningBusiness.GetList(
                    $" app_id='{scope.AppID}' AND node_id='{scope.ClawID}' AND planning_status='Completed' ");

                if (completedPlans == null || completedPlans.Count == 0)
                {
                    LoggerHelper.LogInfo(_logger, LOG_MODULE, "未找到已完成的任务规划");
                    return 0;
                }

                LoggerHelper.LogInfo(_logger, LOG_MODULE,
                    $"找到 {completedPlans.Count} 个已完成规划");

                // 使用LLM总结规划模式
                var planDescriptions = new StringBuilder();
                for (int i = 0; i < Math.Min(completedPlans.Count, 20); i++)
                {
                    var plan = completedPlans[i];
                    planDescriptions.AppendLine($"规划{i + 1}: 任务={plan.OriginalTask}, " +
                        $"策略={plan.Strategy}, 步骤数={plan.TotalSteps}, " +
                        $"置信度={plan.Confidence}, 修订次数={plan.RevisionCount}");
                }

                var prompt = $@"你是一个任务规划专家。请分析以下已完成的任务规划记录，提取成功的规划模式。

已完成规划列表：
{planDescriptions}

请总结以下信息：
1. 规划模板名称（简短描述）
2. 适用任务类型
3. 推荐步骤序列（JSON数组）
4. 适用条件
5. 历史成功率评估（0-100）
6. 通用性评分（0-100）

以JSON格式输出。";

                var resultText = await CallLLMAsync(modelInfo, prompt, 2000);
                if (string.IsNullOrEmpty(resultText))
                    return 0;

                // 保存为 planning_template 类型的长期记忆
                var templateKnowledge = new ExtractedKnowledge
                {
                    Type = KnowledgeType.Procedure,
                    Topic = "任务规划经验模板",
                    Summary = $"基于{completedPlans.Count}个已完成规划总结",
                    Content = resultText,
                    Importance = 70,
                    Confidence = 0.7
                };

                var memoryId = await _knowledgeExtractionService.UpdateLongTermMemoryAsync(
                    templateKnowledge,
                    scope.AppID,
                    scope.ClawID,
                    "", "");

                if (!string.IsNullOrEmpty(memoryId))
                {
                    consolidated = 1;
                    LoggerHelper.LogInfo(_logger, LOG_MODULE, "规划经验模板已保存");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"规划经验整理异常: {ex.Message}", ex);
            }

            return consolidated;
        }

        #endregion

        #region Step 5: 知识层级提升

        public async Task<int> PromoteKnowledgeToAppLevelAsync(
            string appId,
            LargeModelInfo modelInfo,
            LargeModelInfo embeddingModelInfo,
            string sourceClawId = null,
            int generalityThreshold = 70)
        {
            int promoted = 0;

            try
            {
                // 获取候选知识：ClawAI级（ClawID不为空）、高通用性、尚未被提升
                var allMemories = LongTermMemoryBusiness.GetByApp(appId, 5000);
                var candidates = allMemories
                    .Where(m => !string.IsNullOrEmpty(m.ClawID))
                    .Where(m => m.Importance >= _options.PromotionMinImportance)
                    .Where(m => m.SourceType != "promoted_from_clawai") // 排除自身就是提升来的
                    .Where(m =>
                    {
                        // 排除已提升的（Metadata中有 promoted_to_app）
                        if (string.IsNullOrEmpty(m.Metadata)) return true;
                        return !m.Metadata.Contains("\"promoted_to_app\":true");
                    })
                    .Take(_options.PromotionMaxPerRun)
                    .ToList();

                if (!string.IsNullOrEmpty(sourceClawId))
                {
                    candidates = candidates.Where(m => m.ClawID == sourceClawId).ToList();
                }

                LoggerHelper.LogInfo(_logger, LOG_MODULE,
                    $"知识提升候选: {candidates.Count} 条 (阈值={generalityThreshold})");

                foreach (var candidate in candidates)
                {
                    if (promoted >= _options.PromotionMaxPerRun)
                        break;

                    try
                    {
                        // 使用LLM评估通用性
                        var evaluation = await EvaluateGeneralityAsync(candidate, modelInfo);
                        if (evaluation == null || evaluation.Score < generalityThreshold)
                            continue;

                        // 创建APP级知识副本
                        var appMemory = new LongTermMemoryInfo
                        {
                            AppID = appId,
                            ClawID = "", // 空表示APP级
                            SessionID = "",
                            MemberID = candidate.MemberID,
                            KnowledgeType = candidate.KnowledgeType,
                            Topic = candidate.Topic,
                            Summary = evaluation.GeneralizedSummary ?? candidate.Summary,
                            Content = evaluation.GeneralizedContent ?? candidate.Content,
                            Importance = candidate.Importance,
                            SourceType = "promoted_from_clawai",
                            SourceID = candidate.MemoryID,
                            Metadata = JsonConvert.SerializeObject(new
                            {
                                source_claw_id = candidate.ClawID,
                                source_memory_id = candidate.MemoryID,
                                generality_score = evaluation.Score,
                                promoted_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                scope_level = "app"
                            })
                        };

                        var newMemoryId = LongTermMemoryBusiness.Add(appMemory);

                        if (!string.IsNullOrEmpty(newMemoryId))
                        {
                            // 生成向量嵌入
                            try
                            {
                                if (embeddingModelInfo != null)
                                {
                                    var embedding = await _kernelService.GenerateEmbeddingAsync(
                                        embeddingModelInfo, appMemory.Summary);
                                    if (embedding != null && embedding.Length > 0)
                                    {
                                        LongTermMemoryBusiness.UpdateEmbedding(newMemoryId,
                                            JsonConvert.SerializeObject(embedding));
                                    }
                                }
                            }
                            catch (Exception embEx)
                            {
                                LoggerHelper.LogWarning(_logger, LOG_MODULE,
                                    $"提升知识嵌入生成失败: {embEx.Message}");
                            }

                            // 标记源记忆已提升
                            var sourceMetadata = string.IsNullOrEmpty(candidate.Metadata)
                                ? new Dictionary<string, object>()
                                : JsonConvert.DeserializeObject<Dictionary<string, object>>(candidate.Metadata) ?? new Dictionary<string, object>();
                            sourceMetadata["promoted_to_app"] = true;
                            sourceMetadata["promoted_memory_id"] = newMemoryId;
                            candidate.Metadata = JsonConvert.SerializeObject(sourceMetadata);
                            LongTermMemoryBusiness.Update(candidate);

                            // 建立跨层关系
                            KnowledgeRelationBusiness.CreateRelation(
                                appId,
                                candidate.MemoryID,
                                newMemoryId,
                                "derived",
                                0.9f,
                                JsonConvert.SerializeObject(new
                                {
                                    cross_scope = true,
                                    from = "clawai",
                                    to = "app",
                                    reason = evaluation.Reason
                                }));

                            promoted++;
                            LoggerHelper.LogInfo(_logger, LOG_MODULE,
                                $"提升知识: {candidate.Topic} → APP级 (通用性={evaluation.Score})");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError(_logger, LOG_MODULE,
                            $"提升知识 {candidate.MemoryID} 失败: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"知识层级提升异常: {ex.Message}", ex);
            }

            return promoted;
        }

        /// <summary>
        /// 使用LLM评估知识的通用性
        /// </summary>
        private async Task<GeneralityEvaluation> EvaluateGeneralityAsync(
            LongTermMemoryInfo memory,
            LargeModelInfo modelInfo)
        {
            var prompt = $@"你是一个知识评估专家。请评估以下知识的通用性。

知识主题：{memory.Topic}
知识类型：{memory.KnowledgeType}
知识摘要：{memory.Summary}
知识详细内容：{TruncateContent(memory.Content, 300)}
来源ClawAI节点：{memory.ClawID}

请评估：
1. 通用性评分（0-100）：
   - 100：该知识是领域通用知识，任何同类型AI都可以使用
   - 70：该知识适用于多数同类场景，但可能需要微调
   - 40：该知识部分通用，但包含特定的上下文
   - 0：该知识完全特定于来源ClawAI节点，不具有通用价值
2. 泛化后的摘要（移除特定节点/用户信息后的版本，如果需要脱敏）
3. 泛化后的内容（移除特定节点/用户信息后的版本，如果需要脱敏）
4. 提升建议（直接提升/泛化后提升/不建议提升）

以JSON格式输出，字段: generality_score, generalized_summary, generalized_content, suggestion, reason";

            var resultText = await CallLLMAsync(modelInfo, prompt, 1500);
            if (string.IsNullOrEmpty(resultText))
                return null;

            try
            {
                string jsonStr = ExtractJsonObject(resultText);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStr);

                int score = GetIntValue(dict, "generality_score", 0);
                string suggestion = GetStringValue(dict, "suggestion") ?? "不建议提升";

                if (suggestion.Contains("不建议"))
                    return null;

                return new GeneralityEvaluation
                {
                    Score = score,
                    Reason = GetStringValue(dict, "reason") ?? "",
                    GeneralizedSummary = GetStringValue(dict, "generalized_summary"),
                    GeneralizedContent = GetStringValue(dict, "generalized_content")
                };
            }
            catch
            {
                return null;
            }
        }

        private class GeneralityEvaluation
        {
            public int Score { get; set; }
            public string Reason { get; set; }
            public string GeneralizedSummary { get; set; }
            public string GeneralizedContent { get; set; }
        }

        #endregion

        #region Step 6: 记忆合并与重评分

        public async Task<int> MergeAndRescoreMemoriesAsync(MemoryScopeContext scope)
        {
            int merged = 0;

            try
            {
                List<LongTermMemoryInfo> memories;

                if (scope.Scope == MemoryScope.ClawAI)
                {
                    memories = LongTermMemoryBusiness.GetByClawID(scope.AppID, scope.ClawID, 1000);
                }
                else if (scope.Scope == MemoryScope.App)
                {
                    memories = LongTermMemoryBusiness.GetList(
                        $" app_id='{scope.AppID}' AND (claw_id IS NULL OR claw_id='') ");
                }
                else
                {
                    return 0; // 会话级不处理
                }

                // 时间衰减重评分
                foreach (var memory in memories)
                {
                    try
                    {
                        int originalImportance = memory.Importance;
                        int newImportance = originalImportance;

                        // 时间衰减
                        var daysSinceUpdate = (DateTime.Now - memory.LastUpdateTime).TotalDays;
                        int decayCycles = (int)(daysSinceUpdate / _options.TimeDecayDays);
                        if (decayCycles > 0)
                        {
                            int decayScore = scope.Scope == MemoryScope.App
                                ? _options.TimeDecayScoreApp
                                : _options.TimeDecayScoreClawAI;
                            newImportance -= decayCycles * decayScore;
                        }

                        // 访问加权
                        if (memory.AccessCount > _options.HighAccessThreshold)
                        {
                            int accessBonus = scope.Scope == MemoryScope.App ? 15 : _options.HighAccessBonus;
                            newImportance += accessBonus;
                        }

                        // 边界约束
                        newImportance = Math.Max(0, Math.Min(100, newImportance));

                        // 仅在有变化时更新
                        if (newImportance != originalImportance)
                        {
                            memory.Importance = newImportance;
                            LongTermMemoryBusiness.Update(memory);
                            merged++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError(_logger, LOG_MODULE,
                            $"重评分失败 {memory.MemoryID}: {ex.Message}", ex);
                    }
                }

                // 去重合并（复用现有方法）
                if (scope.Scope == MemoryScope.ClawAI)
                {
                    try
                    {
                        await _knowledgeExtractionService.MergeAndDeduplicateKnowledgeAsync(
                            scope.AppID, "");
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError(_logger, LOG_MODULE, $"去重合并失败: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"合并重评分异常: {ex.Message}", ex);
            }

            return merged;
        }

        #endregion

        #region LLM 辅助方法

        /// <summary>
        /// 调用LLM获取文本响应
        /// </summary>
        private async Task<string> CallLLMAsync(LargeModelInfo modelInfo, string prompt, int maxTokens = 2000)
        {
            try
            {
                var modelConfig = new LargeModelConfig
                {
                    Model = modelInfo,
                    Temperature = 0.3,
                    AnswerTokens = maxTokens,
                    TopPCoefficient = 0.95
                };

                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);

                var chatResult = _chatService.SendChatAsync(
                    modelConfig,
                    chatHistory,
                    Function: null,
                    responseFormat: "text",
                    enableStreamingObservation: false,
                    progress: null,
                    ct: CancellationToken.None
                );

                var rawContent = new StringBuilder();
                await foreach (var content in chatResult)
                {
                    rawContent.Append(content);
                }

                return rawContent.ToString();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, LOG_MODULE, $"LLM调用失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 从响应文本中提取JSON数组
        /// </summary>
        private string ExtractJsonArray(string text)
        {
            return ExtractJson(text, '[', ']');
        }

        /// <summary>
        /// 从响应文本中提取JSON对象
        /// </summary>
        private string ExtractJsonObject(string text)
        {
            return ExtractJson(text, '{', '}');
        }

        private string ExtractJson(string text, char startChar, char endChar)
        {
            if (string.IsNullOrEmpty(text)) return null;

            // 处理markdown代码块
            if (text.Contains("```json"))
            {
                int startIdx = text.IndexOf("```json") + 7;
                int endIdx = text.IndexOf("```", startIdx);
                if (endIdx > startIdx)
                    text = text.Substring(startIdx, endIdx - startIdx).Trim();
            }
            else if (text.Contains("```"))
            {
                int startIdx = text.IndexOf("```") + 3;
                int endIdx = text.IndexOf("```", startIdx);
                if (endIdx > startIdx)
                    text = text.Substring(startIdx, endIdx - startIdx).Trim();
            }

            int bracketStart = text.IndexOf(startChar);
            int bracketEnd = text.LastIndexOf(endChar);
            if (bracketStart >= 0 && bracketEnd > bracketStart)
            {
                return text.Substring(bracketStart, bracketEnd - bracketStart + 1);
            }

            return null;
        }

        private string TruncateContent(string content, int maxLength)
        {
            if (string.IsNullOrEmpty(content)) return "";
            return content.Length <= maxLength ? content : content.Substring(0, maxLength) + "...";
        }

        private T? ParseEnum<T>(Dictionary<string, object> dict, string key) where T : struct
        {
            var str = GetStringValue(dict, key);
            if (string.IsNullOrEmpty(str)) return null;
            if (Enum.TryParse<T>(str, true, out var result)) return result;

            // 尝试中文到枚举的映射
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "概念", "Concept" }, { "事实", "Fact" }, { "流程", "Procedure" },
                { "经验", "Experience" }, { "问答", "QA" }, { "偏好", "Preference" }
            };
            if (mapping.TryGetValue(str, out var mapped) && Enum.TryParse<T>(mapped, true, out var mappedResult))
                return mappedResult;

            return null;
        }

        private string GetStringValue(Dictionary<string, object> dict, string key)
        {
            if (dict == null) return null;

            // 尝试精确匹配
            if (dict.TryGetValue(key, out var val) && val != null)
                return val.ToString();

            // 尝试忽略大小写匹配
            foreach (var kvp in dict)
            {
                if (kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                    return kvp.Value.ToString();
            }

            return null;
        }

        private int GetIntValue(Dictionary<string, object> dict, string key, int defaultValue)
        {
            var str = GetStringValue(dict, key);
            return int.TryParse(str, out var val) ? val : defaultValue;
        }

        #endregion
    }
}
