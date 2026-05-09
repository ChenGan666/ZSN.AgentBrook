using Microsoft.Extensions.Logging;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Services;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Node.Claw.Pipeline
{
    /// <summary>
    /// 上下文加载器 - 负责并行加载记忆上下文、AI状态和WorkFlow配置
    /// </summary>
    public class ContextLoader
    {
        private readonly IMemoryService _memoryService;
        private readonly IPersonalityService _personalityService;
        private readonly IAgentOrchestrationService _agentOrchestrationService;
        private readonly ILogger _logger;

        public ContextLoader(
            IMemoryService memoryService,
            IPersonalityService personalityService,
            IAgentOrchestrationService agentOrchestrationService,
            ILogger logger)
        {
            _memoryService = memoryService;
            _personalityService = personalityService;
            _agentOrchestrationService = agentOrchestrationService;
            _logger = logger;
        }

        /// <summary>
        /// 加载执行上下文
        /// </summary>
        public async Task<ContextLoadingResult> LoadContextAsync(
            string appId,
            string sessionId,
            string memberId,
            List<Inputs> inputs,
            ClawAIData nodeData,
            string nodeId)
        {
            var result = new ContextLoadingResult
            {
                Logs = new List<string>()
            };

            result.Logs.Add("⚡ 并行加载记忆上下文、AI状态、WorkFlow配置...");

            // 获取用户输入
            result.UserInput = inputs.FirstOrDefault(input => input.varname == "prompt");
            result.OriginalTask = result.UserInput?.value ?? "";

            if (string.IsNullOrEmpty(result.OriginalTask))
            {
                throw new Exception("用户任务不能为空");
            }

            result.Logs.Add($"用户任务: {result.OriginalTask}");

            // 并行加载：记忆上下文 + AI个性状态 + 可用WorkFlow
            var memoryTask = _memoryService.BuildMemoryContextAsync(
                appId, sessionId, memberId, inputs, nodeData.memoryConfig, nodeId);

            var personalityTask = nodeData.personalityConfig.enabled
                ? _personalityService.InitializePersonalityAsync(sessionId, appId, nodeData.personalityConfig)
                : Task.FromResult<AIPersonalityState>(null);

            var workflowTask = _agentOrchestrationService.GetAvailableWorkflowsAsync(nodeData);

            // 等待所有任务完成
            await Task.WhenAll(memoryTask, personalityTask, workflowTask);

            // 获取结果
            result.MemoryContext = await memoryTask;
            result.AvailableWorkflows = await workflowTask;

            // 如果启用了个性状态，更新到记忆上下文
            if (nodeData.personalityConfig.enabled)
            {
                result.MemoryContext.AIState = await personalityTask;
            }

            // ==================== P1优化: 并行检索长期记忆 ====================
            result.Logs.Add("⚡ 正在并行检索长期记忆...");

            // 阶段A: 关键词检索 + 语义检索(并行)
            var keywordTask = Task.Run(() =>
            {
                try
                {
                    return LongTermMemoryBusiness.SearchByKeywords(appId, result.OriginalTask, limit: 5);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, $"关键词检索失败: {ex.Message}");
                    return new List<LongTermMemoryInfo>();
                }
            });

            // 语义检索（当前已跳过，但结构上并行）
            var semanticTask = Task.Run(() =>
            {
                // TODO: 启用语义搜索需要配置Embedding API
                return new List<LongTermMemoryInfo>();
            });

            await Task.WhenAll(keywordTask, semanticTask);

            result.MemoryContext.LongTermMemories = keywordTask.Result;
            result.MemoryContext.SemanticMemories = semanticTask.Result;

            result.Logs.Add($"✓ 关键词检索: 找到 {result.MemoryContext.LongTermMemories.Count} 条长期记忆");
            result.Logs.Add("✓ 语义检索: 已跳过（需要配置Embedding API）");

            // 阶段B: 知识图谱关联检索 (依赖关键词检索结果)
            try
            {
                if (result.MemoryContext.LongTermMemories.Count > 0)
                {
                    // P1优化: 对前3条记忆并行检索关联知识
                    var memoriesToSearch = result.MemoryContext.LongTermMemories.Take(3).ToList();
                    var allRelated = new System.Collections.Concurrent.ConcurrentBag<LongTermMemoryInfo>();
                    var visitedMemoryIds = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();

                    var graphTasks = memoriesToSearch.Select(memory => Task.Run(() =>
                    {
                        try
                        {
                            var related = KnowledgeRelationBusiness.GetRelatedKnowledge(
                                memory.MemoryID, maxDepth: 2, maxResults: 10);

                            foreach (var r in related)
                            {
                                if (visitedMemoryIds.TryAdd(r.MemoryID, true))
                                {
                                    allRelated.Add(r);
                                }
                            }
                        }
                        catch { /* 单条记忆图谱检索失败不影响其他 */ }
                    }));

                    await Task.WhenAll(graphTasks);

                    result.MemoryContext.RelatedKnowledge = allRelated.ToList();
                    result.Logs.Add($"✓ 知识图谱: 找到 {result.MemoryContext.RelatedKnowledge.Count} 条关联知识");
                }
                else
                {
                    result.MemoryContext.RelatedKnowledge = new List<LongTermMemoryInfo>();
                    result.Logs.Add("✓ 知识图谱: 无相关记忆");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, $"知识图谱检索失败: {ex.Message}");
                result.Logs.Add($"⚠ 知识图谱检索失败: {ex.Message}");
            }

            // 生成日志消息
            var memoryMsg = $"✓ 并行加载完成:\n" +
                $"  - 用户画像: {(result.MemoryContext.UserProfile != null ? "已加载" : "未加载")}\n" +
                $"  - AI 状态: {(result.MemoryContext.AIState != null ? "已加载" : "未加载")}\n" +
                $"  - 短期记忆: {result.MemoryContext.WorkingMemoryCount} 条\n" +
                $"  - 相关记忆: {result.MemoryContext.RelevantMemories?.Count ?? 0} 条\n" +
                $"  - 长期记忆(关键词): {result.MemoryContext.LongTermMemories.Count} 条\n" +
                $"  - 长期记忆(语义): {result.MemoryContext.SemanticMemories.Count} 条\n" +
                $"  - 知识图谱关联: {result.MemoryContext.RelatedKnowledge.Count} 条";

            result.Logs.Add("✓ 并行加载完成:");
            result.Logs.Add($"  - 用户画像: {(result.MemoryContext.UserProfile != null ? "已加载" : "未加载")}");
            result.Logs.Add($"  - AI 状态: {(result.MemoryContext.AIState != null ? "已加载" : "未加载")}");
            result.Logs.Add($"  - 短期记忆: {result.MemoryContext.WorkingMemoryCount} 条");
            result.Logs.Add($"  - 相关记忆: {result.MemoryContext.RelevantMemories?.Count ?? 0} 条");
            result.Logs.Add($"  - 长期记忆(关键词): {result.MemoryContext.LongTermMemories.Count} 条");
            result.Logs.Add($"  - 长期记忆(语义): {result.MemoryContext.SemanticMemories.Count} 条");
            result.Logs.Add($"  - 知识图谱关联: {result.MemoryContext.RelatedKnowledge.Count} 条");

            if (nodeData.personalityConfig.enabled && result.MemoryContext.AIState != null)
            {
                if (nodeData.personalityConfig.enableEmotionalState)
                {
                    var confidence = result.MemoryContext.AIState.EmotionalState.ContainsKey("confidence")
                        ? result.MemoryContext.AIState.EmotionalState["confidence"] : 70;
                    result.Logs.Add($"  - AI自信度: {confidence}%");
                }
            }

            var workflowMsg = new StringBuilder($"可用 WorkFlow: {result.AvailableWorkflows.Count} 个\n");
            result.Logs.Add($"可用 WorkFlow: {result.AvailableWorkflows.Count} 个");
            foreach (var workflow in result.AvailableWorkflows)
            {
                var workflowLine = $"  - {workflow.name}: {workflow.description}";
                result.Logs.Add(workflowLine);
                workflowMsg.AppendLine(workflowLine);
            }

            result.WorkflowLogMessage = workflowMsg.ToString();

            LoggerHelper.LogInfo(_logger, ClawLogModules.CONTEXT_LOADER, $"上下文加载完成 - SessionID: {sessionId}");
            return result;
        }
    }

    /// <summary>
    /// 上下文加载结果
    /// </summary>
    public class ContextLoadingResult
    {
        public Inputs UserInput { get; set; }
        public string OriginalTask { get; set; }
        public MemoryContext MemoryContext { get; set; }
        public List<WorkflowConfigInfo> AvailableWorkflows { get; set; }
        public List<string> Logs { get; set; }
        public string WorkflowLogMessage { get; set; }
    }
}
