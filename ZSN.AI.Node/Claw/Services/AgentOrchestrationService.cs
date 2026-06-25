using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Exceptions;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Plugins;
using ZSN.AI.Service.Helpers;
using ZSN.Utils.Core.Helpers;
using StackExchange.Redis;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// Agent 编排服务实现
    /// </summary>
    public class AgentOrchestrationService : IAgentOrchestrationService
    {
        private readonly ILogger<AgentOrchestrationService> _logger;
        private readonly IReflectionService _reflectionService;
        private readonly IKernelService _kernelService;
        private readonly IChatService _chatService;

        public AgentOrchestrationService(
            ILogger<AgentOrchestrationService> logger,
            IReflectionService reflectionService,
            IKernelService kernelService,
            IChatService chatService)
        {
            _logger = logger;
            _reflectionService = reflectionService;
            _kernelService = kernelService;
            _chatService = chatService;
        }

        public async Task<List<WorkflowConfigInfo>> GetAvailableWorkflowsAsync(
            ClawAIData nodeData)
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.AGENT_ORCHESTRATION, " 获取可用 WorkFlow");

            var workflows = new List<WorkflowConfigInfo>();

            try
            {
                // 从配置中获取WorkFlow列表
                if (nodeData.workflowConfigs != null && nodeData.workflowConfigs.Count > 0)
                {
                    foreach (var config in nodeData.workflowConfigs)
                    {
                        if (config.enabled)
                        {
                            // 验证WorkFlow是否存在
                            var workflowInfo = WorkflowInfoBussiness.GetModel(config.workflowId);
                            if (workflowInfo != null)
                            {
                                config.name = workflowInfo.WorkflowName;
                                config.description = workflowInfo.Description;

                                workflows.Add(config);
                                LoggerHelper.LogInfo(_logger, ClawLogModules.AGENT_ORCHESTRATION, $" 添加 WorkFlow: {config.name} ({config.workflowId})");
                            }
                            else
                            {
                                LoggerHelper.LogWarning(_logger, ClawLogModules.AGENT_ORCHESTRATION, $" WorkFlow不存在: {config.workflowId}");
                            }
                        }
                    }
                }
                
                LoggerHelper.LogInfo(_logger, ClawLogModules.AGENT_ORCHESTRATION, $" 找到 {workflows.Count} 个可用 WorkFlow");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AgentOrchestration] 获取 WorkFlow 失败");
            }

            await Task.CompletedTask;
            return workflows;
        }

        public async Task<List<AgentNodeInfo>> GetAvailableAgentNodesAsync(
            string nodeId,
            WorkFlowLoopConfig config)
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.AGENT_ORCHESTRATION, $" 获取可用 Agent - NodeID: {nodeId}");

            var agents = new List<AgentNodeInfo>();

            try
            {
                // 获取连接到 AgentLoop 端点的边 - 只获取 sourceHandle=output_to_agent 的边
                List<WorkflowEdgeInfo> edgeList = WorkflowEdgeInfoBussiness.GetListBySourceNodeId(nodeId);
                
                if (edgeList != null && edgeList.Count > 0)
                {
                    // 筛选 sourceHandle=output_to_agent 的边
                    List<string> agentNodeIds = new();
                    foreach (var edge in edgeList)
                    {
                        var cfg = edge.Config as Newtonsoft.Json.Linq.JObject ?? Newtonsoft.Json.Linq.JObject.FromObject(edge.Config);
                        if ((string?)cfg["sourceHandle"] == "output_to_agent")
                        {
                            agentNodeIds.Add(edge.TargetNodeId);
                        }
                    }

                    // 获取筛选后的目标节点
                    if (agentNodeIds.Count > 0)
                    {
                        List<WorkflowNodeInfo> targetNodeList = WorkflowNodeInfoBussiness.GetListByNodeID(
                            string.Join(",", agentNodeIds.Select(id => $"'{id}'")));

                        if (targetNodeList != null)
                        {
                            foreach (WorkflowNodeInfo node in targetNodeList)
                            {
                                if (node != null && node.NodeType == NodeType.Agent)
                                {
                                    NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(node.Config.ToString());
                                    if (nodeConfig != null)
                                    {
                                        AgentData agentData = JsonConvert.DeserializeObject<AgentData>(nodeConfig.data.ToString());
                                        if (agentData != null)
                                        {
                                            agents.Add(new AgentNodeInfo
                                            {
                                                NodeID = node.NodeID,
                                                Name = agentData.label,
                                                Description = agentData.agent?.Description ?? "",
                                                Capabilities = agentData.agent?.Description ?? "",
                                                WorkflowID = node.WorkflowID
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        LoggerHelper.LogInfo(_logger, ClawLogModules.AGENT_ORCHESTRATION, $" 未找到 output_to_agent 连接的 Agent 节点");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AgentOrchestration] 获取 Agent 节点失败");
            }

            await Task.CompletedTask;
            
            return agents;
        }

        public async Task<ExecutionResult> ExecuteStepsAsync(
            NodeConfig config,
            TaskPlanning taskPlanning,
            string AppID,
            string SessionID,
            string ProcessesID,
            string TaskID,
            string originalTask,
            List<Inputs> inputs,
            ClawAIData nodeData,
            LargeModelConfig reflectionModelConfig,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress,
            ClawAIExecutionLogger execLogger = null,
            string MemberID = "system",
            string FromMainTaskID = "")
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.AGENT_ORCHESTRATION, $" 执行步骤 - PlanningID: {taskPlanning.PlanningID}");

            var result = new ExecutionResult
            {
                CompletedSteps = 0,
                FailedSteps = 0,
                SkippedSteps = 0
            };

            // P1改进: 使用智能并发执行
            await ExecuteStepsIntelligentlyAsync(
                config, taskPlanning, AppID, SessionID, ProcessesID, TaskID,
                originalTask, inputs, nodeData, reflectionModelConfig,
                Logs, progress, result, execLogger, MemberID, FromMainTaskID);

            // 检查是否所有步骤都完成 - 严格模式：只有 Completed 才算完成
            // Skipped 步骤需要有明确的跳过原因，且不影响任务完成度
            var completedSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Completed);
            var skippedSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Skipped);
            var pendingSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Pending);
            var failedSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Failed);
            
            // 所有步骤完成 = (已完成 + 已跳过) == 总步骤 且 待执行 == 0
            result.AllStepsCompleted = (completedSteps + skippedSteps) == taskPlanning.TotalSteps && pendingSteps == 0;
            
            Logs.Enqueue($"步骤状态统计: 已完成={completedSteps}, 已跳过={skippedSteps}, 待执行={pendingSteps}, 失败={failedSteps}, 总数={taskPlanning.TotalSteps}");
            Logs.Enqueue($"AllStepsCompleted={result.AllStepsCompleted} (严格模式: 需要所有步骤都处理完毕)");

            return result;
        }

        /// <summary>
        /// P1改进: 智能执行步骤 - 根据依赖关系决定串行/并行
        /// </summary>
        private async Task ExecuteStepsIntelligentlyAsync(
            NodeConfig config,
            TaskPlanning taskPlanning,
            string AppID,
            string SessionID,
            string ProcessesID,
            string TaskID,
            string originalTask,
            List<Inputs> inputs,
            ClawAIData nodeData,
            LargeModelConfig reflectionModelConfig,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress,
            ExecutionResult result,
            ClawAIExecutionLogger execLogger = null,
            string MemberID = "system",
            string FromMainTaskID = "")
        {
            // 构建依赖图
            var dependencyGraph = BuildDependencyGraph(taskPlanning.Steps);
            
            // 按层级分组(同一层级的步骤可以并行)
            var layers = TopologicalSort(taskPlanning.Steps, dependencyGraph);
            
            _logger.LogInformation(
                "[ParallelExecution] 任务分层完成 - 总步骤: {TotalSteps}, 层数: {Layers}",
                taskPlanning.Steps.Count, layers.Count);
            
            Logs.Enqueue($"并发执行分析: 总步骤={taskPlanning.Steps.Count}, 分为{layers.Count}层");
            
            // 按层执行
            var skippedLayers = new List<int>();
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                var layer = layers[layerIndex];
                
                // 过滤出可执行的步骤(依赖已完成)
                var executableSteps = layer.Where(step => 
                    step.StepStatus == StepStatus.Pending &&
                    step.DependsOnStepIds.All(depId =>
                        taskPlanning.Steps.Any(ds => ds.StepID == depId && ds.StepStatus == StepStatus.Completed))
                ).ToList();
                
                if (executableSteps.Count == 0)
                {
                    // 记录跳过的层级，稍后检查
                    skippedLayers.Add(layerIndex);
                    var pendingInLayer = layer.Count(s => s.StepStatus == StepStatus.Pending);
                    if (pendingInLayer > 0)
                    {
                        Logs.Enqueue($"[层{layerIndex + 1}] 跳过: 有{pendingInLayer}个待执行步骤，但依赖未满足");
                    }
                    continue;
                }
                
                if (executableSteps.Count == 1)
                {
                    // 单个步骤,串行执行
                    var step = executableSteps[0];

                    Logs.Enqueue($"[层{layerIndex + 1}] 串行执行步骤 {step.StepIndex}: {step.StepDescription}");

                    var stepResult = await ExecuteSingleStepAsync(
                        config, step, taskPlanning, AppID, SessionID, ProcessesID, TaskID,
                        originalTask, inputs, nodeData, reflectionModelConfig,
                        Logs, progress, execLogger, MemberID, FromMainTaskID, layerIndex, layers.Count);

                    // 异步触发：步骤已触发子 WorkFlow，不再阻塞等待
                    if (stepResult.IsAsyncTriggered)
                    {
                        Logs.Enqueue($"[AsyncTrigger] 步骤 {step.StepIndex} 串行异步触发，等待子 WorkFlow 完成后回调");
                        result.IsAsyncTriggered = true; // 通知上层退出反思循环
                        return; // 跳出层级循环，方法返回，线程释放
                    }

                    if (stepResult.Success)
                    {
                        result.CompletedSteps++;
                    }
                    else
                    {
                        result.FailedSteps++;
                        if (!nodeData.workFlowLoopConfig.continueOnWorkFlowFailure)
                        {
                            Logs.Enqueue($"步骤 {step.StepIndex} 失败,中断执行");
                            return;
                        }
                    }
                }
                else
                {
                    // 多个步骤,并行执行
                    var stepIds = string.Join(", ", executableSteps.Select(s => s.StepIndex));
                    Logs.Enqueue($"[层{layerIndex + 1}] 并行执行 {executableSteps.Count} 个步骤: {stepIds}");

                    _logger.LogInformation(
                        "[ParallelExecution] 并行执行 {Count} 个步骤: {StepIds}",
                        executableSteps.Count,
                        stepIds);

                    var startTime = DateTime.UtcNow;

                    // 识别异步候选步骤：只有 workflow_call 且分配了 WorkFlow 的步骤才会异步触发
                    // llm_reasoning 等同步步骤会立即完成，不会产生 AgentEnd 回调
                    var asyncCandidateSteps = executableSteps
                        .Where(s => s.StepType == StepType.WorkflowCall
                                    && s.AssignedWorkflowIds != null
                                    && s.AssignedWorkflowIds.Count > 0)
                        .ToList();
                    int asyncStepCount = asyncCandidateSteps.Count;

                    // 注册 Redis 原子计数器（仅用于异步步骤汇聚）
                    string layerCounterKey = $"clawai:layer:{ProcessesID}:{layerIndex}";
                    string layerContextKey = $"clawai:ctx:{ProcessesID}:{layerIndex}";
                    bool counterRegistered = false;

                    Logs.Enqueue($"[并发控制] ProcessesID: {ProcessesID}");
                    Logs.Enqueue($"[并发控制] LayerIndex: {layerIndex}");
                    Logs.Enqueue($"[并发控制] 总并行步骤: {executableSteps.Count}, 异步候选步骤: {asyncStepCount}");

                    if (asyncStepCount == 0)
                    {
                        // 全部是同步步骤，无需 Redis 计数器
                        Logs.Enqueue($"[并发控制] 本层全部为同步步骤，跳过 Redis 计数器注册");
                        counterRegistered = false;
                    }
                    else
                    {
                        Logs.Enqueue($"[并发控制] 计数器 Key: {layerCounterKey}");
                        Logs.Enqueue($"[并发控制] 准备注册 Redis 计数器（仅计异步步骤）...");

                        try
                        {
                            var redis = new RedisHelper().GetConnectionRedisMultiplexer().GetDatabase();

                            if (redis == null)
                            {
                                Logs.Enqueue($"[并发控制] ❌ Redis 连接失败（redis = null），回退到同步模式");
                                Console.WriteLine($"[并发控制] ❌ Redis 连接失败 - ProcessesID: {ProcessesID}, LayerIndex: {layerIndex}");
                                counterRegistered = false;
                            }
                            else
                            {
                                TimeSpan redisTTL = TimeSpan.FromHours(2);

                                Logs.Enqueue($"[并发控制] 尝试 SETNX: {layerCounterKey} = {asyncStepCount}");
                                Console.WriteLine($"[并发控制] 尝试 SETNX: {layerCounterKey} = {asyncStepCount}");

                                // 仅计数异步步骤，不是全部并行步骤
                                counterRegistered = redis.StringSet(layerCounterKey, asyncStepCount, redisTTL, When.NotExists);

                                Logs.Enqueue($"[并发控制] SETNX 结果: {counterRegistered}");
                                Console.WriteLine($"[并发控制] SETNX 结果: {counterRegistered} - Key: {layerCounterKey}");

                                if (counterRegistered)
                                {
                                    // 保存层级上下文（仅包含异步步骤的 ID）
                                    var layerCtx = new ClawAILayerContext
                                    {
                                        TotalStepCount = asyncStepCount,
                                        StepIds = asyncCandidateSteps.Select(s => s.StepID).ToList(),
                                        LayerIndex = layerIndex,
                                        TotalLayers = layers.Count,
                                        ContinueOnFailure = nodeData.workFlowLoopConfig.continueOnWorkFlowFailure,
                                        ProcessesID = ProcessesID,
                                        ClawAINodeId = config.id
                                    };
                                    redis.StringSet(layerContextKey, JsonConvert.SerializeObject(layerCtx), redisTTL);

                                    Logs.Enqueue($"[并发控制] ✅ 注册层级计数器成功: {layerCounterKey} = {asyncStepCount} (异步步骤数)");
                                    Console.WriteLine($"[并发控制] ✅ 注册成功 - Key: {layerCounterKey}, Value: {asyncStepCount}, StepIDs: {string.Join(",", layerCtx.StepIds)}");
                                }
                                else
                                {
                                    Logs.Enqueue($"[并发控制] ⚠️ 计数器已存在（可能是重复触发或恢复场景）");
                                    Console.WriteLine($"[并发控制] ⚠️ 计数器已存在 - Key: {layerCounterKey}");

                                    var existingValue = redis.StringGet(layerCounterKey);
                                    Logs.Enqueue($"[并发控制] 现有计数器值: {existingValue}");
                                    Console.WriteLine($"[并发控制] 现有计数器值: {existingValue}");
                                }
                            }
                        }
                        catch (Exception redisEx)
                        {
                            Logs.Enqueue($"[并发控制] ❌ Redis 注册计数器异常（回退到同步模式）: {redisEx.Message}");
                            Console.WriteLine($"[并发控制] ❌ Redis 异常 - ProcessesID: {ProcessesID}, LayerIndex: {layerIndex}");
                            Console.WriteLine($"[并发控制] 异常详情: {redisEx.Message}");
                            counterRegistered = false;
                        }
                    }

                    var tasks = executableSteps.Select(step =>
                        ExecuteSingleStepAsync(
                            config, step, taskPlanning, AppID, SessionID, ProcessesID, TaskID,
                            originalTask, inputs, nodeData, reflectionModelConfig,
                            Logs, progress, execLogger, MemberID, FromMainTaskID, layerIndex, layers.Count)
                    ).ToList();

                    var results = await Task.WhenAll(tasks);

                    var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

                    // 检查是否有异步触发的步骤
                    var asyncTriggeredCount = results.Count(r => r.IsAsyncTriggered);
                    if (asyncTriggeredCount > 0)
                    {
                        Logs.Enqueue($"[并发控制] 本层 {executableSteps.Count} 个步骤已全部异步触发，" +
                                 $"等待子 WorkFlow 完成后回调恢复");
                        Logs.Enqueue($"[并发控制] 异步触发: {asyncTriggeredCount}, 同步完成: {results.Length - asyncTriggeredCount}");
                        result.IsAsyncTriggered = true; // 通知上层退出反思循环
                        return; // 跳出层级循环，方法返回，线程释放
                    }

                    // 统计结果（同步完成路径）
                    var successCount = results.Count(r => r.Success);
                    var failCount = results.Count(r => !r.Success);

                    result.CompletedSteps += successCount;
                    result.FailedSteps += failCount;

                    Logs.Enqueue($"[层{layerIndex + 1}] 并行执行完成 - 成功: {successCount}, 失败: {failCount}, 耗时: {elapsed:F2}秒");

                    _logger.LogInformation(
                        "[ParallelExecution] 并行执行完成 - 成功: {Success}, 失败: {Failed}, 耗时: {Duration}秒",
                        successCount, failCount, elapsed);

                    // 如果有失败且不允许继续,则中断
                    if (failCount > 0 && !nodeData.workFlowLoopConfig.continueOnWorkFlowFailure)
                    {
                        Logs.Enqueue($"有 {failCount} 个步骤失败,中断执行");
                        return;
                    }
                }
            }

            // 检查是否有未执行的Pending步骤
            var remainingPendingSteps = taskPlanning.Steps
                .Where(s => s.StepStatus == StepStatus.Pending)
                .ToList();

            if (remainingPendingSteps.Count > 0)
            {
                var warningMsg = $"⚠ 警告: 有{remainingPendingSteps.Count}个步骤未执行 (可能因依赖未满足): " +
                                 string.Join(", ", remainingPendingSteps.Select(s => $"步骤{s.StepIndex}"));
                Logs.Enqueue(warningMsg);
                _logger.LogWarning("[ParallelExecution] {Warning}", warningMsg);

                // 列出每个未执行步骤的依赖情况
                foreach (var step in remainingPendingSteps)
                {
                    var missingDeps = step.DependsOnStepIds
                        .Where(depId => !taskPlanning.Steps.Any(s => s.StepID == depId && s.StepStatus == StepStatus.Completed))
                        .ToList();
                    
                    if (missingDeps.Count > 0)
                    {
                        Logs.Enqueue($"  步骤{step.StepIndex} 缺失依赖: {string.Join(", ", missingDeps)}");
                    }
                }
            }
        }

        /// <summary>
        /// P1改进: 构建依赖图
        /// </summary>
        private Dictionary<string, List<string>> BuildDependencyGraph(List<TaskStep> steps)
        {
            var graph = new Dictionary<string, List<string>>();
            
            foreach (var step in steps)
            {
                graph[step.StepID] = step.DependsOnStepIds ?? new List<string>();
            }
            
            return graph;
        }

        /// <summary>
        /// P1改进: 拓扑排序 - 按依赖关系分层
        /// </summary>
        private List<List<TaskStep>> TopologicalSort(
            List<TaskStep> steps,
            Dictionary<string, List<string>> graph)
        {
            var layers = new List<List<TaskStep>>();
            var processed = new HashSet<string>();
            var remaining = new HashSet<string>(graph.Keys);
            
            while (remaining.Count > 0)
            {
                // 找出当前可以执行的步骤(依赖都已完成)
                var currentLayer = remaining
                    .Where(stepId => graph[stepId].All(dep => processed.Contains(dep)))
                    .Select(stepId => steps.First(s => s.StepID == stepId))
                    .ToList();
                
                if (currentLayer.Count == 0)
                {
                    // 检测到循环依赖
                    _logger.LogWarning(
                        "[DependencyGraph] 检测到循环依赖,剩余步骤: {Steps}",
                        string.Join(", ", remaining));
                    
                    // 将剩余步骤全部加入最后一层
                    var remainingSteps = remaining
                        .Select(stepId => steps.First(s => s.StepID == stepId))
                        .ToList();
                    
                    if (remainingSteps.Count > 0)
                    {
                        layers.Add(remainingSteps);
                    }
                    
                    break;
                }
                
                layers.Add(currentLayer);
                
                foreach (var step in currentLayer)
                {
                    processed.Add(step.StepID);
                    remaining.Remove(step.StepID);
                }
            }
            
            return layers;
        }

        public async Task RetryStepAsync(
            TaskPlanning taskPlanning,
            int stepIndex,
            string refinedPrompt)
        {
            var step = taskPlanning.Steps.FirstOrDefault(s => s.StepIndex == stepIndex);
            if (step != null)
            {
                step.StepStatus = StepStatus.Pending;
                step.RetryCount++;
                
                if (!string.IsNullOrEmpty(refinedPrompt))
                {
                    step.StepDescription = refinedPrompt;
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取最终结果 - 只返回终端步骤(没有其他步骤依赖它的步骤)的输出
        /// 对于串行链式任务(1→2→3→4),只返回步骤4的结果
        /// 对于并行分支任务(1→[2,3]),返回步骤2和3的合并结果
        /// </summary>
        public string CombineStepResults(TaskPlanning taskPlanning)
        {
            var completedSteps = taskPlanning.Steps
                .Where(s => s.StepStatus == StepStatus.Completed && !string.IsNullOrEmpty(s.ActualOutput))
                .ToList();

            if (completedSteps.Count == 0)
                return "";

            if (completedSteps.Count == 1)
                return completedSteps[0].ActualOutput;

            // 找出被其他步骤依赖的步骤ID
            var completedStepIds = new HashSet<string>(completedSteps.Select(s => s.StepID));
            var dependedUponIds = new HashSet<string>();
            foreach (var step in completedSteps)
            {
                foreach (var depId in step.DependsOnStepIds ?? new List<string>())
                {
                    if (completedStepIds.Contains(depId))
                        dependedUponIds.Add(depId);
                }
            }

            // 按步骤序号排列，输出所有步骤结果
            var parts = new List<string>();
            foreach (var step in completedSteps.OrderBy(s => s.StepIndex))
            {
                var isTerminal = !dependedUponIds.Contains(step.StepID);
                var output = step.ActualOutput ?? "";

                if (isTerminal)
                {
                    // 终端步骤：完整输出
                    parts.Add(output);
                }
                else
                {
                    // 中间步骤：跳过纯图片链接（避免和终端步骤重复），文本内容截断输出
                    if (!output.TrimStart().StartsWith("!["))
                    {
                        var truncated = output.Length > 500
                            ? output.Substring(0, 500) + $"\n... (共 {output.Length} 字符)"
                            : output;
                        parts.Add(truncated);
                    }
                }
            }

            return string.Join("\n\n", parts);
        }

        private List<TaskStep> GetExecutableSteps(TaskPlanning taskPlanning)
        {
            return taskPlanning.Steps
                .Where(s => s.StepStatus == StepStatus.Pending &&
                           s.DependsOnStepIds.All(depId =>
                               taskPlanning.Steps.Any(ds => ds.StepID == depId && ds.StepStatus == StepStatus.Completed)))
                .ToList();
        }

        private async Task<StepExecutionResult> ExecuteSingleStepAsync(
            NodeConfig config,
            TaskStep step,
            TaskPlanning taskPlanning,
            string AppID,
            string SessionID,
            string ProcessesID,
            string TaskID,
            string originalTask,
            List<Inputs> inputs,
            ClawAIData nodeData,
            LargeModelConfig reflectionModelConfig,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress,
            ClawAIExecutionLogger execLogger = null,
            string MemberID = "system",
            string FromMainTaskID = "",
            int currentLayerIndex = 0,
            int totalLayers = 0)
        {
            Logs.Enqueue($"执行步骤 {step.StepIndex}: {step.StepDescription}");

            step.StepStatus = StepStatus.Executing;
            step.StartTime = DateTime.Now;

            try
            {
                string result = "";

                // ===== 单次执行模式 =====
                result = await ExecuteStepCoreAsync(
                    config, step, taskPlanning, AppID, SessionID, ProcessesID, TaskID,
                    originalTask, inputs, nodeData, Logs, progress, execLogger, MemberID, FromMainTaskID,
                    currentLayerIndex, totalLayers);

                // 检测异步触发标记：步骤已触发子 WorkFlow，不阻塞等待
                if (result != null && result.StartsWith("__ASYNC_TRIGGERED__"))
                {
                    step.StepStatus = StepStatus.Executing; // 保持执行中状态
                    Logs.Enqueue($"步骤 {step.StepIndex} 已异步触发，等待子 WorkFlow 完成后回调");
                    LoggerHelper.LogInfo(_logger, ClawLogModules.AGENT_ORCHESTRATION,
                        $"[AsyncTrigger] 步骤 {step.StepIndex} 已异步触发 - StepID: {step.StepID}, Result: {result}");

                    return new StepExecutionResult
                    {
                        Success = true,
                        Result = result,
                        IsAsyncTriggered = true
                    };
                }

                step.ExecutionResult = result;
                step.ActualOutput = result;
                step.StepStatus = StepStatus.Completed;
                step.EndTime = DateTime.Now;

                Logs.Enqueue($"✓ 步骤 {step.StepIndex} 完成");
                Logs.Enqueue($"  结果长度: {result?.Length ?? 0} 字符");
                Logs.Enqueue($"  结果预览: {(result != null && result.Length > 100 ? result.Substring(0, 100) + "..." : result)}");

                // 使用默认质量分，主流程立即继续
                int defaultQualityScore = 75;
                step.QualityScore = defaultQualityScore;

                // 保存步骤执行结果到数据库（使用默认质量分）
                TaskStepBusiness.UpdateExecutionResult(
                    step.StepID,
                    result,
                    result,
                    defaultQualityScore);

                Logs.Enqueue($"  ✓ 步骤结果已保存到数据库（质量分: {defaultQualityScore}，后台评估中）");

                // 启动后台任务进行实际质量评估（异步非阻塞）
                var stepIdForEval = step.StepID;
                var stepForEval = step; // 捕获当前步骤引用
                _ = Task.Run(async () =>
                {
                    try
                    {
                        LoggerHelper.LogDebug(_logger, ClawLogModules.AGENT_ORCHESTRATION, " 开始后台质量评估 - StepID: {stepIdForEval}");

                        int actualQuality = await _reflectionService.EvaluateStepQualityAsync(
                            stepForEval, nodeData, reflectionModelConfig, null);

                        // 更新步骤对象的质量分
                        stepForEval.QualityScore = actualQuality;

                        // 更新数据库中的质量分
                        TaskStepBusiness.UpdateExecutionResult(
                            stepIdForEval,
                            result,
                            result,
                            actualQuality);

                        LoggerHelper.LogInfo(_logger, ClawLogModules.AGENT_ORCHESTRATION, $" 后台质量评估完成 - StepID: {stepIdForEval}, 质量分: {actualQuality}/100");
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogWarning(_logger, ClawLogModules.AGENT_ORCHESTRATION, $" 后台质量评估失败 - StepID: {stepIdForEval}，保持默认值");
                    }
                });

                return new StepExecutionResult { Success = true, Result = result };
            }
            catch (Exception ex)
            {
                step.StepStatus = StepStatus.Failed;
                step.EndTime = DateTime.Now;
                step.ExecutionResult = $"执行失败: {ex.Message}";
                step.RetryCount++;

                Logs.Enqueue($"✗ 步骤 {step.StepIndex} 失败: {ex.Message}");

                return new StepExecutionResult { Success = false, Result = ex.Message };
            }
        }

        /// <summary>
        /// 步骤核心执行逻辑（从 ExecuteSingleStepAsync 提取，支持循环调用）
        /// </summary>
        private async Task<string> ExecuteStepCoreAsync(
            NodeConfig config,
            TaskStep step,
            TaskPlanning taskPlanning,
            string AppID,
            string SessionID,
            string ProcessesID,
            string TaskID,
            string originalTask,
            List<Inputs> inputs,
            ClawAIData nodeData,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress,
            ClawAIExecutionLogger execLogger = null,
            string MemberID = "system",
            string FromMainTaskID = "",
            int currentLayerIndex = 0,
            int totalLayers = 0)
        {
            string result = "";

            switch (step.StepType)
            {
                case StepType.WorkflowCall:
                    if (step.AssignedWorkflowIds != null && step.AssignedWorkflowIds.Count > 0)
                    {
                        result = await ExecuteWorkflowStepAsync(config, step, taskPlanning, AppID, SessionID, ProcessesID, TaskID, originalTask, inputs, nodeData, Logs, execLogger, MemberID, FromMainTaskID, currentLayerIndex, totalLayers);
                    }
                    else
                    {
                        result = "未分配 WorkFlow 或 Agent";
                        Logs.Enqueue($"  警告: 步骤未分配 WorkFlow 或 Agent");
                    }
                    break;

                case StepType.LLMReasoning:
                    result = await ExecuteLLMReasoningStepAsync(step, taskPlanning, originalTask, nodeData, Logs, progress);
                    break;

                case StepType.DataCollection:
                    result = await ExecuteDataCollectionStepAsync(step, taskPlanning, originalTask, nodeData, Logs, progress);
                    break;

                case StepType.Validation:
                    result = await ExecuteValidationStepAsync(step, taskPlanning, originalTask, nodeData, Logs, progress);
                    break;

                case StepType.Synthesis:
                    result = await ExecuteSynthesisStepAsync(step, taskPlanning, originalTask, nodeData, Logs, progress);
                    break;

                default:
                    result = $"未知步骤类型: {step.StepType}";
                    Logs.Enqueue($"  警告: 未实现的步骤类型 {step.StepType}");
                    break;
            }

            return result;
        }

        private async Task<string> ExecuteWorkflowStepAsync(
            NodeConfig config,
            TaskStep step,
            TaskPlanning taskPlanning,
            string AppID,
            string SessionID,
            string ProcessesID,
            string TaskID,
            string originalTask,
            List<Inputs> inputs,
            ClawAIData nodeData,
            ConcurrentQueue<string> Logs,
            ClawAIExecutionLogger execLogger = null,
            string MemberID = "system",
            string FromMainTaskID = "",
            int currentLayerIndex = 0,
            int totalLayers = 0)
        {
            Logs.Enqueue($"  开始执行WorkFlow步骤...");

            // 检查是否分配了WorkFlow
            if (step.AssignedWorkflowIds == null || step.AssignedWorkflowIds.Count == 0)
            {
                Logs.Enqueue($"  警告: 未分配WorkFlow,无法执行");
                return "未分配WorkFlow";
            }

            Logs.Enqueue($"  已分配 {step.AssignedWorkflowIds.Count} 个WorkFlow");
            var results = new List<string>();

            foreach (var workflowId in step.AssignedWorkflowIds)
            {
                try
                {
                    // 获取 WorkFlow 名称用于调试
                    var workflowInfo = WorkflowInfoBussiness.GetModel(workflowId);
                    string workflowName = workflowInfo?.WorkflowName ?? "未知WorkFlow";

                    Logs.Enqueue($"  → 准备调用WorkFlow: {workflowId}");
                    Logs.Enqueue($"     WorkFlow名称: {workflowName}");

                    // 构建传递给 WorkFlow 的输入参数
                    var workflowInputs = BuildWorkflowInputs(
                        step, taskPlanning, originalTask, inputs, Logs);

                    Logs.Enqueue($"  → WorkFlow调用参数:");
                    Logs.Enqueue($"     - AppID: {AppID}");
                    Logs.Enqueue($"     - SessionID: {SessionID}");
                    Logs.Enqueue($"     - ProcessesID: {ProcessesID}");
                    Logs.Enqueue($"     - TaskID: {TaskID}");
                    Logs.Enqueue($"     - MemberID: {MemberID}");
                    Logs.Enqueue($"     - WorkflowID: {workflowId}");
                    Logs.Enqueue($"     - WorkflowName: {workflowName}");
                    Logs.Enqueue($"     - NodeID: {config.id}");
                    Logs.Enqueue($"     - StepID: {step.StepID}");
                    Logs.Enqueue($"     - StepIndex: {step.StepIndex}");
                    Logs.Enqueue($"     - FromMainTaskID: {FromMainTaskID}");
                    Logs.Enqueue($"     - 输入参数数: {workflowInputs.Count}");

                    // 详细记录每个输入参数
                    foreach (var input in workflowInputs)
                    {
                        Logs.Enqueue($"       • {input.varname} = {(input.value?.Length > 100 ? input.value.Substring(0, 100) + "..." : input.value)}");
                    }

                    // 记录到专用日志文件（用于排查 WorkFlow 输入输出问题）
                    execLogger?.LogStepExecution(
                        step, null, null,
                        workflowInputs,
                        null, // 执行结果在步骤完成后记录
                        0);

                    // ===== 异步触发模式 =====
                    // 构建 ClawAI 步骤执行上下文，保存完整状态用于回调恢复
                    var stepContext = new ClawAIStepContext
                    {
                        ClawAINodeId = config.id,
                        NodeConfigJson = JsonConvert.SerializeObject(config),
                        NodeDataJson = JsonConvert.SerializeObject(nodeData),
                        TaskPlanningJson = JsonConvert.SerializeObject(taskPlanning),
                        TriggeredStepId = step.StepID,
                        TriggeredStepIndex = step.StepIndex,
                        AppID = AppID,
                        SessionID = SessionID,
                        ProcessesID = ProcessesID,
                        TaskID = TaskID,
                        FromMainTaskID = FromMainTaskID,
                        MemberID = MemberID,
                        OriginalTask = originalTask,
                        InputsJson = JsonConvert.SerializeObject(inputs),
                        LogsJson = JsonConvert.SerializeObject(Logs),
                        // 保存已完成步骤结果
                        CompletedStepResultsJson = JsonConvert.SerializeObject(
                            taskPlanning.Steps
                                .Where(s => s.StepStatus == StepStatus.Completed
                                            && !string.IsNullOrEmpty(s.ExecutionResult))
                                .ToDictionary(s => s.StepID, s => s.ExecutionResult)),
                        // 保存剩余步骤
                        PendingStepIdsJson = JsonConvert.SerializeObject(
                            taskPlanning.Steps
                                .Where(s => s.StepStatus == StepStatus.Pending)
                                .Select(s => s.StepID).ToList()),
                        // 当前层信息
                        CurrentLayerStepIdsJson = JsonConvert.SerializeObject(
                            taskPlanning.Steps
                                .Where(s => s.StepStatus == StepStatus.Pending || s.StepStatus == StepStatus.Executing)
                                .Select(s => s.StepID).ToList()),
                        CurrentLayerIndex = currentLayerIndex,
                        TotalLayers = totalLayers,
                        RecordID = "",
                        MaxAsyncWaitMinutes = nodeData.workFlowLoopConfig.asyncTriggerMaxWaitMinutes > 0
                            ? nodeData.workFlowLoopConfig.asyncTriggerMaxWaitMinutes : 120
                    };

                    // 创建异步等待 TaskInfo
                    string stepTaskID = Guid.NewGuid().ToString();
                    var asyncTaskInfo = new TaskInfo
                    {
                        TaskID = stepTaskID,
                        TaskType = NodeType.ClawAIWorkflowStep,
                        TaskConfig = new TaskConfig
                        {
                            NodeConfig = config,
                            NotNodeConfig = stepContext,
                            Data = new TaskData
                            {
                                AppID = AppID,
                                SessionID = SessionID,
                                ProcessesID = ProcessesID,
                                TaskID = stepTaskID,
                                FromMainTaskID = TaskID
                            }
                        },
                        State = TaskState.Waiting,
                        LoopType = LoopType.NOLoop,
                        RepeatValue = 1,
                        RedoCount = 0,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        FromTaskID = TaskID,
                        FromMainTaskID = TaskID,
                        SessionID = SessionID,
                        ProcessesID = ProcessesID
                    };
                    TaskInfoBussiness.Add(asyncTaskInfo);

                    // 触发子 WorkFlow（不等待）
                    BasePlugin basePlugin = new BasePlugin();
                    string stepProcessesID = $"{ProcessesID}_{step.StepID}";
                    string newTaskID = basePlugin.excution_agent_workflow(config,
                        AppID, SessionID,
                        stepProcessesID, workflowId, workflowInputs,
                        FromMainTaskID: stepTaskID  // 指向异步等待 TaskInfo
                    );

                    // 保存子 WorkFlow 关联信息到上下文
                    stepContext.SubWorkflowSessionID = SessionID;
                    stepContext.SubWorkflowProcessesID = stepProcessesID;
                    stepContext.SubWorkflowTaskID = newTaskID;

                    // 更新上下文
                    asyncTaskInfo.TaskConfig.NotNodeConfig = stepContext;
                    TaskInfoBussiness.Update(asyncTaskInfo);

                    Logs.Enqueue($"  → 子 WorkFlow 已异步触发（非阻塞模式）");
                    Logs.Enqueue($"     - 异步等待 TaskID: {stepTaskID}");
                    Logs.Enqueue($"     - 子 WorkFlow TaskID: {newTaskID}");

                    Console.WriteLine($"[AsyncTrigger] 子 WorkFlow 已创建 - StepID: {step.StepID}, AsyncTaskID: {stepTaskID}, SubWorkflowTaskID: {newTaskID}, ProcessesID: {stepProcessesID}");
                    Console.WriteLine($"[AsyncTrigger] 等待 NodeJob 下轮轮询捡起子 WorkFlow TaskID: {newTaskID}");

                    LoggerHelper.LogInfo(_logger, ClawLogModules.AGENT_ORCHESTRATION,
                        $"[AsyncTrigger] 步骤已异步触发 - StepID: {step.StepID}, " +
                        $"AsyncTaskID: {stepTaskID}, SubWorkflowTaskID: {newTaskID}");

                    // 返回异步触发标记
                    return $"__ASYNC_TRIGGERED__:{stepTaskID}";
                }
                catch (Exception ex)
                {
                    var workflowInfo = WorkflowInfoBussiness.GetModel(workflowId);
                    string workflowName = workflowInfo?.WorkflowName ?? "未知WorkFlow";

                    Logs.Enqueue($"  ✗ WorkFlow [{workflowName}] 执行失败");
                    Logs.Enqueue($"     - WorkflowID: {workflowId}");
                    Logs.Enqueue($"     - 错误信息: {ex.Message}");
                    Logs.Enqueue($"     - 堆栈: {ex.StackTrace}");

                    if (!nodeData.workFlowLoopConfig.continueOnWorkFlowFailure)
                    {
                        throw;
                    }
                }
            }

            await Task.CompletedTask;
            return string.Join("\n\n", results);
        }

        private List<Inputs> BuildWorkflowInputs(
            TaskStep step,
            TaskPlanning taskPlanning,
            string originalTask,
            List<Inputs> originalInputs,
            ConcurrentQueue<string> Logs)
        {
            var inputs = new List<Inputs>();
            
            // 优先使用步骤特定的输入参数（创建副本，避免修改原始 StepInputs）
            if (step.StepInputs != null && step.StepInputs.Count > 0)
            {
                foreach (var si in step.StepInputs)
                {
                    inputs.Add(new Inputs
                    {
                        id = si.id,
                        sourceId = si.sourceId,
                        varname = si.varname,
                        value = si.value,
                        type = si.type,
                        txt = si.txt,
                        paramName = si.paramName,
                        paramType = si.paramType,
                        defaultValue = si.defaultValue
                    });
                }
            }
            else
            {
                // 如果步骤没有特定参数，则使用默认的prompt
                inputs.Add(new Inputs
                {
                    varname = "prompt",
                    value = $"{step.StepDescription}\n\n原始任务: {originalTask}"
                });
            }
            
            // 添加前置步骤的结果作为上下文
            // 优化：如果 prompt 中已通过 {output_xxx} 占位符显式引用了前置步骤输出，
            // 则不再注入冗余的 context，避免 context 淹没 prompt 导致 WorkFlow 忽略实际输入
            bool promptHasExplicitReference = inputs.Any(i =>
                i.varname == "prompt" &&
                !string.IsNullOrEmpty(i.value) &&
                i.value.Contains("{output_"));

            if (promptHasExplicitReference)
            {
                Logs.Enqueue($"  → 步骤 {step.StepIndex} 的 prompt 已包含显式占位符引用，跳过 context 注入");
            }
            else if (step.DependsOnStepIds != null && step.DependsOnStepIds.Count > 0)
            {
                Logs.Enqueue($"  → 步骤 {step.StepIndex} 依赖 {step.DependsOnStepIds.Count} 个前置步骤");
                
                var contextBuilder = new StringBuilder();
                foreach (var depStepId in step.DependsOnStepIds)
                {
                    var depStep = taskPlanning.Steps.FirstOrDefault(s => s.StepID == depStepId);
                    if (depStep != null)
                    {
                        Logs.Enqueue($"     - 前置步骤: {depStep.StepDescription}");
                        Logs.Enqueue($"       状态: {depStep.StepStatus}");
                        Logs.Enqueue($"       结果长度: {depStep.ExecutionResult?.Length ?? 0} 字符");
                        
                        if (!string.IsNullOrEmpty(depStep.ExecutionResult))
                        {
                            contextBuilder.AppendLine($"- {depStep.StepDescription}: {depStep.ExecutionResult}");
                        }
                        else
                        {
                            Logs.Enqueue($"       ⚠️ 警告: 前置步骤结果为空!");
                        }
                    }
                    else
                    {
                        Logs.Enqueue($"     - ⚠️ 警告: 找不到依赖步骤 {depStepId}");
                    }
                }
                
                if (contextBuilder.Length > 0)
                {
                    // 查找现有的 context 参数
                    var existingContext = inputs.FirstOrDefault(i => i.varname == "context");
                    if (existingContext != null)
                    {
                        // 如果已有 context 参数，替换为实际的前置步骤结果
                        Logs.Enqueue($"  → 替换现有 context 参数（原值: {existingContext.value?.Substring(0, Math.Min(50, existingContext.value?.Length ?? 0))}...）");
                        existingContext.value = contextBuilder.ToString();
                        Logs.Enqueue($"  ✓ 已用前置步骤结果更新 context 参数 ({contextBuilder.Length} 字符)");
                    }
                    else
                    {
                        // 如果没有 context 参数，添加新的
                        inputs.Add(new Inputs
                        {
                            varname = "context",
                            value = contextBuilder.ToString()
                        });
                        Logs.Enqueue($"  ✓ 添加了 context 参数 ({contextBuilder.Length} 字符)");
                    }
                }
                else
                {
                    Logs.Enqueue($"  ⚠️ 警告: 没有可用的前置步骤结果");
                }
            }
            
            // 传递原始输入（避免重复）
            foreach (var input in originalInputs)
            {
                if (!inputs.Any(i => i.varname == input.varname))
                {
                    inputs.Add(input);
                }
            }

            // 解析输入值中的占位符: 将 {xxx} 格式的占位符替换为对应前置步骤的输出
            // LLM规划可能生成如 {knowledge_output}, {chat_output_1}, {step_2_output} 等占位符
            inputs = ResolveInputPlaceholders(inputs, step, taskPlanning, Logs);

            return inputs;
        }

        /// <summary>
        /// 解析输入值中的占位符，将 {output_<StepID>} 替换为前置步骤的实际输出
        /// 标准格式: {output_<StepID>} — 由规划阶段的 NormalizeInputPlaceholders 生成
        /// 兼容格式: 含步骤索引数字的任意占位符 (如 {chat_output_1}) 作为回退
        /// </summary>
        private List<Inputs> ResolveInputPlaceholders(
            List<Inputs> inputs,
            TaskStep currentStep,
            TaskPlanning taskPlanning,
            ConcurrentQueue<string> Logs)
        {
            // 检查是否有需要解析的占位符
            bool hasPlaceholders = inputs.Any(i => !string.IsNullOrEmpty(i.value) && i.value.Contains("{"));
            if (!hasPlaceholders) return inputs;

            // 构建 StepID→输出 映射 (所有已完成步骤)
            var stepOutputById = new Dictionary<string, string>();
            foreach (var s in taskPlanning.Steps.Where(s => s.StepStatus == StepStatus.Completed))
            {
                if (!string.IsNullOrEmpty(s.ExecutionResult))
                {
                    stepOutputById[s.StepID] = s.ExecutionResult;
                }
            }

            // 构建步骤索引→输出 映射 (兼容回退)
            var stepOutputByIndex = new Dictionary<int, string>();
            foreach (var s in taskPlanning.Steps.Where(s => s.StepStatus == StepStatus.Completed))
            {
                if (!string.IsNullOrEmpty(s.ExecutionResult))
                {
                    stepOutputByIndex[s.StepIndex] = s.ExecutionResult;
                }
            }

            if (stepOutputById.Count == 0) return inputs;

            // 匹配 {xxx} 以及紧跟其后的可选数组索引 [N]
            var placeholderPattern = new Regex(@"\{([^}]+)\}(\[(\d+)\])?");

            foreach (var input in inputs)
            {
                if (string.IsNullOrEmpty(input.value) || !input.value.Contains("{"))
                    continue;

                string originalValue = input.value;

                input.value = placeholderPattern.Replace(input.value, match =>
                {
                    string placeholder = match.Groups[1].Value.Trim();
                    // 提取花括号外紧跟的数组索引后缀，如 [0]
                    string outerArraySuffix = match.Groups[2].Success ? match.Groups[2].Value : "";
                    int outerArrayIndex = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : -1;

                    // 检测数组索引后缀 (如 [0], [1]) — 优先检查 placeholder 内部的
                    int arrayIndex = -1;
                    string arraySuffix = null;
                    var arrayIdxMatch = Regex.Match(placeholder, @"\[(\d+)\]$");
                    if (arrayIdxMatch.Success)
                    {
                        arrayIndex = int.Parse(arrayIdxMatch.Groups[1].Value);
                        arraySuffix = arrayIdxMatch.Value;
                        placeholder = placeholder.Substring(0, arrayIdxMatch.Index);
                    }
                    else if (outerArrayIndex >= 0)
                    {
                        // 花括号外部紧跟的数组索引，如 {output_xxx}[0]
                        arrayIndex = outerArrayIndex;
                        arraySuffix = outerArraySuffix;
                    }

                    // 优先: 精确匹配 {output_<StepID>} 或 {output_<StepID>[N]} 格式
                    if (placeholder.StartsWith("output_"))
                    {
                        var stepId = placeholder.Substring(7);
                        // 去除可能存在于 stepId 中的数组索引
                        var idArrayMatch = Regex.Match(stepId, @"\[\d+\]$");
                        if (idArrayMatch.Success)
                        {
                            stepId = stepId.Substring(0, idArrayMatch.Index);
                        }
                        if (stepOutputById.ContainsKey(stepId))
                        {
                            string output = stepOutputById[stepId];
                            output = ExtractArrayElementIfNeeded(output, arrayIndex, placeholder, arraySuffix, Logs);
                            Logs.Enqueue($"  ✓ 占位符 → 步骤 {stepId} 的输出 ({output.Length} 字符, arrayIndex={arrayIndex})");
                            return output;
                        }
                    }

                    // 回退: 从占位符中提取步骤索引数字
                    var numberMatch = Regex.Match(placeholder, @"(\d+)");
                    if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out int stepIdx))
                    {
                        if (stepOutputByIndex.ContainsKey(stepIdx))
                        {
                            string output = stepOutputByIndex[stepIdx];
                            output = ExtractArrayElementIfNeeded(output, arrayIndex, placeholder, arraySuffix, Logs);
                            Logs.Enqueue($"  ✓ 占位符 → 步骤 {stepIdx} 的输出 ({output.Length} 字符, arrayIndex={arrayIndex})");
                            return output;
                        }
                    }

                    // 回退: 无数字 + 单前置步骤
                    if (currentStep.DependsOnStepIds != null && currentStep.DependsOnStepIds.Count == 1)
                    {
                        var depStep = taskPlanning.Steps.FirstOrDefault(s => s.StepID == currentStep.DependsOnStepIds[0]);
                        if (depStep != null && stepOutputByIndex.ContainsKey(depStep.StepIndex))
                        {
                            string output = stepOutputByIndex[depStep.StepIndex];
                            output = ExtractArrayElementIfNeeded(output, arrayIndex, placeholder, arraySuffix, Logs);
                            Logs.Enqueue($"  ✓ 占位符 → 前置步骤 {depStep.StepIndex} 的输出 ({output.Length} 字符, arrayIndex={arrayIndex})");
                            return output;
                        }
                    }

                    // 未能解析,保持原样
                    Logs.Enqueue($"  ⚠ 占位符 {{{match.Groups[1].Value.Trim()}}}{outerArraySuffix} 未能解析,保持原样");
                    return match.Value;
                });

                if (input.value != originalValue)
                {
                    Logs.Enqueue($"  ✓ 输入 [{input.varname}] 占位符解析完成 ({originalValue.Length} → {input.value.Length} 字符)");
                }
            }

            return inputs;
        }

        /// <summary>
        /// 当占位符包含数组索引 [N] 时，从输出中提取 JSON 数组的第 N 个元素。
        /// 如果数组索引无效或输出不是 JSON 数组，返回原始输出。
        /// </summary>
        private string ExtractArrayElementIfNeeded(
            string output, int arrayIndex, string placeholder, string arraySuffix, ConcurrentQueue<string> Logs)
        {
            if (arrayIndex < 0 || string.IsNullOrEmpty(output))
                return output;

            try
            {
                // 尝试直接解析为 JArray
                var token = JsonConvert.DeserializeObject(output);
                if (token is Newtonsoft.Json.Linq.JArray arr && arrayIndex < arr.Count)
                {
                    var element = arr[arrayIndex];
                    string result = element.ToString(Formatting.None);
                    Logs.Enqueue($"    ↳ 数组索引 [{arrayIndex}] 提取成功，数组共 {arr.Count} 个元素，提取第 {arrayIndex} 个 ({result.Length} 字符)");
                    return result;
                }

                // 尝试从 markdown 代码块中提取 JSON 数组
                var jsonBlockMatch = Regex.Match(output, @"```(?:json)?\s*\n?([\s\S]*?)\n?```");
                if (jsonBlockMatch.Success)
                {
                    var innerToken = JsonConvert.DeserializeObject(jsonBlockMatch.Groups[1].Value.Trim());
                    if (innerToken is Newtonsoft.Json.Linq.JArray innerArr && arrayIndex < innerArr.Count)
                    {
                        var element = innerArr[arrayIndex];
                        string result = element.ToString(Formatting.None);
                        Logs.Enqueue($"    ↳ 从代码块中提取数组索引 [{arrayIndex}]，数组共 {innerArr.Count} 个元素 ({result.Length} 字符)");
                        return result;
                    }
                }

                Logs.Enqueue($"    ⚠ 数组索引 [{arrayIndex}] 提取失败，输出不是有效的 JSON 数组或索引越界，返回完整输出");
            }
            catch (Exception ex)
            {
                Logs.Enqueue($"    ⚠ 数组索引 [{arrayIndex}] 提取异常: {ex.Message}，返回完整输出");
            }

            return output;
        }

        private async Task<string> WaitForWorkflowCompletionAsync(
            string newTaskID,
            string workflowId,
            WorkFlowLoopConfig config,
            ConcurrentQueue<string> Logs,
            WorkflowConfigInfo workflowConfig = null)
        {
            try
            {
                // 获取任务信息
                var taskInfo = TaskInfoBussiness.GetModel(newTaskID);
                if (taskInfo == null)
                {
                    throw new Exception($"无法找到任务: {newTaskID}");
                }

                string workflowSessionID = taskInfo.SessionID;
                string workflowProcessesID = taskInfo.ProcessesID;

                Logs.Enqueue($"    等待 WorkFlow 完成 - SessionID: {workflowSessionID}, ProcessesID: {workflowProcessesID}");
                
                // P1改进: 动态超时机制
                var timeout = CalculateWorkflowTimeout(workflowId, workflowConfig, config);
                
                _logger.LogInformation(
                    "[WorkflowStart] 开始等待WorkFlow完成 - WorkflowID: {WorkflowID}, SessionID: {SessionID}, ProcessesID: {ProcessesID}, 超时时间: {Timeout}秒",
                    workflowId, workflowSessionID, workflowProcessesID, timeout.TotalSeconds);
                var startTime = DateTime.UtcNow;
                bool isCompleted = false;
                string workflowResult = "";
                int pollCount = 0;
                
                // P2改进: 使用配置参数替换硬编码值
                int fastPollingMs = config.fastPollingIntervalMs;
                int slowPollingMs = config.slowPollingIntervalMs;
                int fastPollingDurationSec = config.fastPollingDurationSeconds;
                int logFrequency = config.logOutputFrequency;

                // 轮询等待 AgentEnd 节点完成
                while (!isCompleted && (DateTime.UtcNow - startTime) < timeout)
                {
                    pollCount++;
                    var elapsed = DateTime.UtcNow - startTime;
                    
                    // P0改进: 使用增强的状态检测逻辑
                    var completionResult = await IsWorkflowCompletedAsync(
                        workflowId, workflowSessionID, workflowProcessesID, startTime);
                    
                    if (completionResult.IsCompleted)
                    {
                        isCompleted = true;
                        workflowResult = completionResult.Result;
                        
                        // P0改进: 详细的完成日志
                        _logger.LogInformation(
                            "[WorkflowComplete] WorkFlow执行完成 - WorkflowID: {WorkflowID}, " +
                            "实际耗时: {Duration}秒, 轮询次数: {PollCount}, " +
                            "输出长度: {OutputLength}字符, 检测方式: {DetectionMethod}",
                            workflowId,
                            elapsed.TotalSeconds,
                            pollCount,
                            workflowResult?.Length ?? 0,
                            completionResult.DetectionMethod);
                        
                        Logs.Enqueue($"    WorkFlow 成功完成,耗时: {elapsed.TotalSeconds:F2}秒, 轮询: {pollCount}次");
                    }
                    else if (completionResult.IsFailed)
                    {
                        var errorMsg = completionResult.ErrorMessage ?? "未知错误";
                        _logger.LogError(
                            "[WorkflowFailed] WorkFlow执行失败 - WorkflowID: {WorkflowID}, " +
                            "耗时: {Duration}秒, 错误: {Error}",
                            workflowId, elapsed.TotalSeconds, errorMsg);
                        throw new Exception($"WorkFlow {workflowId} 执行失败: {errorMsg}");
                    }

                    if (!isCompleted)
                    {
                        // 动态调整轮询间隔
                        var elapsedSeconds = elapsed.TotalSeconds;
                        int currentPollingInterval = elapsedSeconds < fastPollingDurationSec 
                            ? fastPollingMs 
                            : slowPollingMs;
                        
                        // P2改进: 使用配置的日志输出频率
                        if (pollCount % logFrequency == 0)
                        {
                            _logger.LogInformation(
                                "[WorkflowWait] 轮询检查 - WorkflowID: {WorkflowID}, " +
                                "已等待: {Elapsed}秒, 轮询次数: {Count}, " +
                                "当前间隔: {Interval}ms",
                                workflowId,
                                elapsedSeconds,
                                pollCount,
                                currentPollingInterval);
                        }
                        
                        await Task.Delay(currentPollingInterval);
                    }
                }

                if (!isCompleted)
                {
                    _logger.LogError(
                        "[WorkflowTimeout] WorkFlow执行超时 - WorkflowID: {WorkflowID}, " +
                        "超时时间: {Timeout}分钟, 轮询次数: {PollCount}",
                        workflowId, config.workflowExecutionTimeoutMinutes, pollCount);
                    throw new TimeoutException(
                        $"WorkFlow {workflowId} 执行超时 (超过 {config.workflowExecutionTimeoutMinutes} 分钟, 轮询 {pollCount} 次)");
                }

                return workflowResult;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.AGENT_ORCHESTRATION, $"等待 WorkFlow 完成时发生错误: {workflowId}");
                throw;
            }
        }

        

        private class StepExecutionResult
        {
            public bool Success { get; set; }
            public string Result { get; set; }
            /// <summary>
            /// 是否为异步触发（步骤已触发但未完成，等待子 WorkFlow 回调）
            /// </summary>
            public bool IsAsyncTriggered { get; set; }
        }

        /// <summary>
        /// P1改进: 计算WorkFlow动态超时时间
        /// P2改进: 使用配置参数替换硬编码值
        /// 根据WorkFlow配置的预估执行时间动态调整超时
        /// </summary>
        private TimeSpan CalculateWorkflowTimeout(
            string workflowId,
            WorkflowConfigInfo workflowConfig,
            WorkFlowLoopConfig config)
        {
            try
            {
                // P2改进: 使用配置的缓冲倍数和最小超时
                double bufferMultiplier = config.timeoutBufferMultiplier;
                int minTimeoutSec = config.minTimeoutSeconds;
                
                // 如果WorkFlow配置了预估执行时间,使用预估时间 + 缓冲
                if (workflowConfig != null && workflowConfig.estimatedDurationSeconds > 0)
                {
                    var estimatedSeconds = workflowConfig.estimatedDurationSeconds;
                    var timeoutSeconds = (int)(estimatedSeconds * bufferMultiplier);
                    
                    // P2改进: 使用配置的最小超时时间
                    timeoutSeconds = Math.Max(minTimeoutSec, timeoutSeconds);
                    
                    // 确保不超过配置的最大超时时间
                    var maxTimeoutSeconds = config.workflowExecutionTimeoutMinutes * 60;
                    timeoutSeconds = Math.Min(timeoutSeconds, maxTimeoutSeconds);
                    
                    _logger.LogDebug(
                        "[DynamicTimeout] WorkFlow {WorkflowID} 使用预估超时: {Estimated}秒 × {Multiplier} = {Timeout}秒 (最小{Min}秒)",
                        workflowId, estimatedSeconds, bufferMultiplier, timeoutSeconds, minTimeoutSec);
                    
                    return TimeSpan.FromSeconds(timeoutSeconds);
                }
                
                // 如果没有配置预估时间,使用默认超时
                var defaultTimeout = TimeSpan.FromMinutes(config.workflowExecutionTimeoutMinutes);
                
                _logger.LogDebug(
                    "[DynamicTimeout] WorkFlow {WorkflowID} 使用默认超时: {Timeout}分钟",
                    workflowId, config.workflowExecutionTimeoutMinutes);
                
                return defaultTimeout;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DynamicTimeout] 计算超时时间异常,使用默认值");
                return TimeSpan.FromMinutes(config.workflowExecutionTimeoutMinutes);
            }
        }

        /// <summary>
        /// P0改进: 增强的WorkFlow完成状态检测
        /// 使用多重检测机制提高可靠性
        /// </summary>
        private async Task<WorkflowCompletionResult> IsWorkflowCompletedAsync(
            string workflowId,
            string sessionId,
            string processesId,
            DateTime startTime)
        {
            try
            {
                // 方案1: 检查TaskInfo的AgentEnd节点状态
                var tasks = TaskInfoBussiness.GetListBySessionIDProcessesID(sessionId, processesId);
                if (tasks != null && tasks.Count > 0)
                {
                    var endTask = tasks.FirstOrDefault(t => t.TaskType == NodeType.AgentEnd);
                    if (endTask != null)
                    {
                        // 检查失败状态
                        if (endTask.State == TaskState.Failure)
                        {
                            var errorMsg = endTask.Results?.Data?.ToString() ?? "未知错误";
                            return new WorkflowCompletionResult
                            {
                                IsCompleted = false,
                                IsFailed = true,
                                ErrorMessage = errorMsg,
                                DetectionMethod = "TaskState.Failure"
                            };
                        }
                        
                        // 检查完成状态
                        if (endTask.State == TaskState.Completed)
                        {
                            string output = "";

                            // 尝试从 ExecutionRecord 获取输出（最佳路径）
                            var records = WorkflowNodeExecutionRecordInfoBussiness.GetListBySessionIDProcessesID(
                                sessionId, processesId);

                            if (records != null && records.Count > 0)
                            {
                                var endRecord = records.FirstOrDefault(r => r.NodeName != null
                                    && r.NodeName.StartsWith(NodeType.AgentEnd.ToString()));
                                if (endRecord != null && endRecord.Status == ExecutionRecordStatus.Success)
                                {
                                    output = endRecord.Outputs?.ToString() ?? "";
                                    try
                                    {
                                        var outputList = JsonConvert.DeserializeObject<List<Output>>(output);
                                        if (outputList != null && outputList.Count > 0)
                                            output = outputList[0].value ?? output;
                                    }
                                    catch { }
                                }
                            }

                            // 无论 ExecutionRecord 是否找到，TaskState.Completed 即代表完成
                            return new WorkflowCompletionResult
                            {
                                IsCompleted = true,
                                Result = output,
                                DetectionMethod = "TaskState.Completed"
                            };
                        }
                    }
                }
                
                // 未完成
                return new WorkflowCompletionResult
                {
                    IsCompleted = false,
                    IsFailed = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WorkflowDetection] 状态检测异常,返回未完成");
                return new WorkflowCompletionResult
                {
                    IsCompleted = false,
                    IsFailed = false
                };
            }
        }

        /// <summary>
        /// WorkFlow完成状态检测结果
        /// </summary>
        private class WorkflowCompletionResult
        {
            /// <summary>
            /// 是否已完成
            /// </summary>
            public bool IsCompleted { get; set; }
            
            /// <summary>
            /// 是否失败
            /// </summary>
            public bool IsFailed { get; set; }
            
            /// <summary>
            /// 执行结果
            /// </summary>
            public string Result { get; set; }
            
            /// <summary>
            /// 错误信息
            /// </summary>
            public string ErrorMessage { get; set; }
            
            /// <summary>
            /// 检测方式 (用于日志分析)
            /// </summary>
            public string DetectionMethod { get; set; }
        }

        /// <summary>
        /// 执行 LLM 推理步骤
        /// </summary>
        private async Task<string> ExecuteLLMReasoningStepAsync(
            TaskStep step,
            TaskPlanning taskPlanning,
            string originalTask,
            ClawAIData nodeData,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress)
        {
            try
            {
                Logs.Enqueue($"  开始 LLM 推理...");

                // 1. 获取主模型
                var model = ModelSelector.GetMainModel(nodeData);
                if (model == null)
                {
                    throw new InvalidOperationException("未配置主模型,无法执行 LLM 推理");
                }

                // 2. 构建推理提示词
                var promptBuilder = new StringBuilder();
                
                // ✅ 优先使用 StepInputs 中的 prompt 参数
                string userPrompt = null;
                if (step.StepInputs != null && step.StepInputs.Count > 0)
                {
                    var promptInput = step.StepInputs.FirstOrDefault(i => i.varname == "prompt");
                    if (promptInput != null && !string.IsNullOrEmpty(promptInput.value))
                    {
                        userPrompt = promptInput.value;
                        
                        // 解析占位符 {output_xxx}
                        userPrompt = ResolvePromptPlaceholdersForLLM(userPrompt, step, taskPlanning, Logs);
                        
                        Logs.Enqueue($"  使用步骤自定义 prompt ({userPrompt.Length} 字符)");
                    }
                }
                
                // ✅ 如果有自定义 prompt,使用它;否则使用默认模板
                if (!string.IsNullOrEmpty(userPrompt))
                {
                    promptBuilder.AppendLine(userPrompt);
                }
                else
                {
                    // 默认模板
                    promptBuilder.AppendLine("# LLM 推理任务");
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("## 原始任务");
                    promptBuilder.AppendLine(originalTask);
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("## 当前步骤");
                    promptBuilder.AppendLine(step.StepDescription);
                    promptBuilder.AppendLine();

                    // 3. 添加前置步骤的结果作为上下文
                    if (step.DependsOnStepIds != null && step.DependsOnStepIds.Count > 0)
                    {
                        promptBuilder.AppendLine("## 前置步骤结果");
                        foreach (var depStepId in step.DependsOnStepIds)
                        {
                            var depStep = taskPlanning.Steps.FirstOrDefault(s => s.StepID == depStepId);
                            if (depStep != null && !string.IsNullOrEmpty(depStep.ExecutionResult))
                            {
                                promptBuilder.AppendLine($"### {depStep.StepDescription}");
                                promptBuilder.AppendLine(depStep.ExecutionResult);
                                promptBuilder.AppendLine();
                            }
                        }
                    }

                    // 4. 添加预期输出说明
                    if (!string.IsNullOrEmpty(step.ExpectedOutput))
                    {
                        promptBuilder.AppendLine("## 预期输出");
                        promptBuilder.AppendLine(step.ExpectedOutput);
                        promptBuilder.AppendLine();
                    }

                    promptBuilder.AppendLine("## 要求");
                    promptBuilder.AppendLine("请基于以上信息进行推理分析,并给出详细的结果。");
                }

                string prompt = promptBuilder.ToString();

                Logs.Enqueue($"  推理提示词长度: {prompt.Length} 字符");

                // 5. 创建 Kernel 并获取 ChatCompletion 服务
                var kernel = _kernelService.GetKernel(model);
                var chatService = kernel.GetRequiredService<IChatCompletionService>();

                // 6. 构建 ChatHistory
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);

                // 7. 配置执行设置
                var settings = new PromptExecutionSettings
                {
                    ExtensionData = new Dictionary<string, object>
                    {
                        ["temperature"] = 0.7,
                        ["max_tokens"] = 2000
                    }
                };

                // 8. 执行推理(流式输出)
                Logs.Enqueue($"  调用模型: {model.ModelName}");
                
                var responseBuilder = new StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    new LargeModelConfig { Model = model },
                    chatHistory,
                    Function: null,
                    responseFormat: "text",
                    enableStreamingObservation: true,
                    progress: progress,
                    ct: System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }
                string reasoningResult = responseBuilder.ToString();

                if (string.IsNullOrEmpty(reasoningResult))
                {
                    throw new InvalidOperationException("LLM 推理返回空结果");
                }

                Logs.Enqueue($"  推理完成,结果长度: {reasoningResult.Length} 字符");

                return reasoningResult;
            }
            catch (Exception ex)
            {
                // LLMException（含致命 403/欠费等）保持原类型向上传播，避免被包装成普通 Exception
                // 导致上层无法区分可恢复错误与 LLM 不可用。
                if (ex is LLMException) throw;
                LoggerHelper.LogError(_logger, ClawLogModules.AGENT_ORCHESTRATION, " LLM 推理失败");
                throw new Exception($"LLM 推理失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行数据收集步骤
        /// </summary>
        private async Task<string> ExecuteDataCollectionStepAsync(
            TaskStep step,
            TaskPlanning taskPlanning,
            string originalTask,
            ClawAIData nodeData,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress)
        {
            try
            {
                Logs.Enqueue($"  开始数据收集...");

                // 1. 获取主模型
                var model = ModelSelector.GetMainModel(nodeData);
                if (model == null)
                {
                    throw new InvalidOperationException("未配置主模型,无法执行数据收集");
                }

                // 2. 构建数据收集提示词
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("# 数据收集任务");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("## 原始任务");
                promptBuilder.AppendLine(originalTask);
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("## 数据收集要求");
                promptBuilder.AppendLine(step.StepDescription);
                promptBuilder.AppendLine();

                // 3. 添加前置步骤的结果作为参考
                if (step.DependsOnStepIds != null && step.DependsOnStepIds.Count > 0)
                {
                    promptBuilder.AppendLine("## 已有信息");
                    foreach (var depStepId in step.DependsOnStepIds)
                    {
                        var depStep = taskPlanning.Steps.FirstOrDefault(s => s.StepID == depStepId);
                        if (depStep != null && !string.IsNullOrEmpty(depStep.ExecutionResult))
                        {
                            promptBuilder.AppendLine($"### {depStep.StepDescription}");
                            promptBuilder.AppendLine(depStep.ExecutionResult);
                            promptBuilder.AppendLine();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(step.ExpectedOutput))
                {
                    promptBuilder.AppendLine("## 期望的数据格式");
                    promptBuilder.AppendLine(step.ExpectedOutput);
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("## 要求");
                promptBuilder.AppendLine("请根据以上要求收集和整理相关数据,以结构化的方式呈现。");

                string prompt = promptBuilder.ToString();
                Logs.Enqueue($"  数据收集提示词长度: {prompt.Length} 字符");

                // 4. 调用LLM
                var kernel = _kernelService.GetKernel(model);
                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);

                var settings = new PromptExecutionSettings
                {
                    ExtensionData = new Dictionary<string, object>
                    {
                        ["temperature"] = 0.5,
                        ["max_tokens"] = 2000
                    }
                };

                Logs.Enqueue($"  调用模型: {model.ModelName}");
                
                var responseBuilder = new StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    new LargeModelConfig { Model = model },
                    chatHistory,
                    Function: null,
                    responseFormat: "text",
                    enableStreamingObservation: true,
                    progress: progress,
                    ct: System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }
                string collectionResult = responseBuilder.ToString();

                if (string.IsNullOrEmpty(collectionResult))
                {
                    throw new InvalidOperationException("数据收集返回空结果");
                }

                Logs.Enqueue($"  数据收集完成,结果长度: {collectionResult.Length} 字符");

                return collectionResult;
            }
            catch (Exception ex)
            {
                if (ex is LLMException) throw;
                LoggerHelper.LogError(_logger, ClawLogModules.AGENT_ORCHESTRATION, " 数据收集失败");
                throw new Exception($"数据收集失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行验证步骤
        /// </summary>
        private async Task<string> ExecuteValidationStepAsync(
            TaskStep step,
            TaskPlanning taskPlanning,
            string originalTask,
            ClawAIData nodeData,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress)
        {
            try
            {
                Logs.Enqueue($"  开始验证...");

                // 1. 获取主模型
                var model = ModelSelector.GetMainModel(nodeData);
                if (model == null)
                {
                    throw new InvalidOperationException("未配置主模型,无法执行验证");
                }

                // 2. 构建验证提示词
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("# 验证任务");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("## 原始任务");
                promptBuilder.AppendLine(originalTask);
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("## 验证要求");
                promptBuilder.AppendLine(step.StepDescription);
                promptBuilder.AppendLine();

                // 3. 添加需要验证的内容
                if (step.DependsOnStepIds != null && step.DependsOnStepIds.Count > 0)
                {
                    promptBuilder.AppendLine("## 待验证内容");
                    foreach (var depStepId in step.DependsOnStepIds)
                    {
                        var depStep = taskPlanning.Steps.FirstOrDefault(s => s.StepID == depStepId);
                        if (depStep != null && !string.IsNullOrEmpty(depStep.ExecutionResult))
                        {
                            promptBuilder.AppendLine($"### {depStep.StepDescription}");
                            promptBuilder.AppendLine(depStep.ExecutionResult);
                            promptBuilder.AppendLine();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(step.ExpectedOutput))
                {
                    promptBuilder.AppendLine("## 验证标准");
                    promptBuilder.AppendLine(step.ExpectedOutput);
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("## 要求");
                promptBuilder.AppendLine("请仔细验证以上内容,指出问题并给出改进建议。格式:");
                promptBuilder.AppendLine("- 验证结果: 通过/不通过");
                promptBuilder.AppendLine("- 发现的问题: (如有)");
                promptBuilder.AppendLine("- 改进建议: (如有)");

                string prompt = promptBuilder.ToString();
                Logs.Enqueue($"  验证提示词长度: {prompt.Length} 字符");

                // 4. 调用LLM
                var kernel = _kernelService.GetKernel(model);
                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);

                var settings = new PromptExecutionSettings
                {
                    ExtensionData = new Dictionary<string, object>
                    {
                        ["temperature"] = 0.3,
                        ["max_tokens"] = 1500
                    }
                };

                Logs.Enqueue($"  调用模型: {model.ModelName}");
                
                var responseBuilder = new StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    new LargeModelConfig { Model = model },
                    chatHistory,
                    Function: null,
                    responseFormat: "text",
                    enableStreamingObservation: true,
                    progress: progress,
                    ct: System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }
                string validationResult = responseBuilder.ToString();

                if (string.IsNullOrEmpty(validationResult))
                {
                    throw new InvalidOperationException("验证返回空结果");
                }

                Logs.Enqueue($"  验证完成,结果长度: {validationResult.Length} 字符");

                return validationResult;
            }
            catch (Exception ex)
            {
                if (ex is LLMException) throw;
                LoggerHelper.LogError(_logger, ClawLogModules.AGENT_ORCHESTRATION, " 验证失败");
                throw new Exception($"验证失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行综合步骤
        /// </summary>
        private async Task<string> ExecuteSynthesisStepAsync(
            TaskStep step,
            TaskPlanning taskPlanning,
            string originalTask,
            ClawAIData nodeData,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress)
        {
            try
            {
                Logs.Enqueue($"  开始综合...");

                // 1. 获取主模型
                var model = ModelSelector.GetMainModel(nodeData);
                if (model == null)
                {
                    throw new InvalidOperationException("未配置主模型,无法执行综合");
                }

                // 2. 构建综合提示词
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("# 综合任务");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("## 原始任务");
                promptBuilder.AppendLine(originalTask);
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("## 综合要求");
                promptBuilder.AppendLine(step.StepDescription);
                promptBuilder.AppendLine();

                // 3. 收集所有前置步骤的结果进行综合
                if (step.DependsOnStepIds != null && step.DependsOnStepIds.Count > 0)
                {
                    promptBuilder.AppendLine("## 需要综合的内容");
                    foreach (var depStepId in step.DependsOnStepIds)
                    {
                        var depStep = taskPlanning.Steps.FirstOrDefault(s => s.StepID == depStepId);
                        if (depStep != null && !string.IsNullOrEmpty(depStep.ExecutionResult))
                        {
                            promptBuilder.AppendLine($"### {depStep.StepDescription}");
                            promptBuilder.AppendLine(depStep.ExecutionResult);
                            promptBuilder.AppendLine();
                        }
                    }
                }
                else
                {
                    // 如果没有指定依赖,则综合所有已完成的步骤
                    promptBuilder.AppendLine("## 已完成的步骤结果");
                    foreach (var completedStep in taskPlanning.Steps.Where(s => s.StepStatus == StepStatus.Completed && s.StepID != step.StepID))
                    {
                        if (!string.IsNullOrEmpty(completedStep.ExecutionResult))
                        {
                            promptBuilder.AppendLine($"### {completedStep.StepDescription}");
                            promptBuilder.AppendLine(completedStep.ExecutionResult);
                            promptBuilder.AppendLine();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(step.ExpectedOutput))
                {
                    promptBuilder.AppendLine("## 期望的综合结果");
                    promptBuilder.AppendLine(step.ExpectedOutput);
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("## 要求");
                promptBuilder.AppendLine("请将以上所有信息进行综合分析和整理,形成一个完整、连贯的结果。");

                string prompt = promptBuilder.ToString();
                Logs.Enqueue($"  综合提示词长度: {prompt.Length} 字符");

                // 4. 调用LLM
                var kernel = _kernelService.GetKernel(model);
                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);

                var settings = new PromptExecutionSettings
                {
                    ExtensionData = new Dictionary<string, object>
                    {
                        ["temperature"] = 0.6,
                        ["max_tokens"] = 3000
                    }
                };

                Logs.Enqueue($"  调用模型: {model.ModelName}");
                
                var responseBuilder = new StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    new LargeModelConfig { Model = model },
                    chatHistory,
                    Function: null,
                    responseFormat: "text",
                    enableStreamingObservation: true,
                    progress: progress,
                    ct: System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }
                string synthesisResult = responseBuilder.ToString();

                if (string.IsNullOrEmpty(synthesisResult))
                {
                    throw new InvalidOperationException("综合返回空结果");
                }

                Logs.Enqueue($"  综合完成,结果长度: {synthesisResult.Length} 字符");

                return synthesisResult;
            }
            catch (Exception ex)
            {
                if (ex is LLMException) throw;
                LoggerHelper.LogError(_logger, ClawLogModules.AGENT_ORCHESTRATION, " 综合失败");
                throw new Exception($"综合失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解析LLM推理步骤的prompt中的占位符，将 {output_<StepID>} 或 {output_<StepID>[N]} 替换为前置步骤的实际输出
        /// </summary>
        private string ResolvePromptPlaceholdersForLLM(
            string prompt,
            TaskStep currentStep,
            TaskPlanning taskPlanning,
            ConcurrentQueue<string> Logs)
        {
            if (string.IsNullOrEmpty(prompt) || !prompt.Contains("{"))
                return prompt;

            // 构建 StepID→输出 映射 (所有已完成步骤)
            var stepOutputById = new Dictionary<string, string>();
            foreach (var s in taskPlanning.Steps.Where(s => s.StepStatus == StepStatus.Completed))
            {
                if (!string.IsNullOrEmpty(s.ExecutionResult))
                {
                    stepOutputById[s.StepID] = s.ExecutionResult;
                }
            }

            if (stepOutputById.Count == 0)
            {
                Logs.Enqueue($"  ⚠️ 警告: 没有已完成的前置步骤,无法替换占位符");
                return prompt;
            }

            // 替换 {output_<StepID>} 或 {output_<StepID>[N]} 占位符
            var placeholderPattern = new System.Text.RegularExpressions.Regex(@"\{output_([^}\]]+)(?:\[(\d+)\])?\}");
            var result = placeholderPattern.Replace(prompt, match =>
            {
                string stepId = match.Groups[1].Value;
                int? arrayIndex = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : (int?)null;

                if (stepOutputById.TryGetValue(stepId, out string output))
                {
                    // 如果有数组索引，提取对应元素
                    if (arrayIndex.HasValue)
                    {
                        output = ExtractArrayElementIfNeeded(output, arrayIndex.Value, stepId, $"[{arrayIndex.Value}]", Logs);
                    }
                    Logs.Enqueue($"  ✓ 替换占位符 {match.Value} → 步骤输出 ({output.Length} 字符)");
                    return output;
                }
                else
                {
                    Logs.Enqueue($"  ⚠️ 警告: 找不到步骤 {stepId} 的输出,保留占位符");
                    return match.Value;
                }
            });

            return result;
        }
    }
}
