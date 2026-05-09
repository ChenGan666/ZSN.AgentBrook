using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Models;
using ZSN.AI.Node.Utils.Pipeline;
using ZSN.AI.Node.Claw.Services;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Node.Helpers;
using ZSN.AI.Node.Services;
using ZSN.AI.Node.Utils;
using ZSN.AI.Service.Helpers;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using ZSN.AI.Node.Claw.Pipeline;

namespace ZSN.AI.Node
{
    /// <summary>
    /// Claw AI 节点执行器
    /// 实现任务规划、Agent编排、循环反思的高级AI节点
    /// </summary>
    public class ExecutionClaw: BaseExecution
    {
        // Claw 核心服务
        private readonly ITaskPlanningService _taskPlanningService;
        private readonly IMemoryService _memoryService;
        private readonly IReflectionService _reflectionService;
        private readonly IAgentOrchestrationService _agentOrchestrationService;
        private readonly IPersonalityService _personalityService;
        private readonly IMasterControlService _masterControlService;

        // Pipeline 处理器
        private readonly ModelInitializer _modelInitializer;
        private readonly ContextLoader _contextLoader;
        private readonly GreetingFastPath _greetingFastPath;
        private readonly PlanningHandler _planningHandler;

        /// <summary>
        /// 执行循环结果
        /// </summary>
        private class ExecutionLoopResult
        {
            public string FinalResult { get; set; }
            public int TotalIterations { get; set; }

            /// <summary>
            /// 是否已异步触发子 WorkFlow，主流程应退出
            /// </summary>
            public bool IsAsyncTriggered { get; set; }
        }

        /// <summary>
        /// 统一规划结果 (P0优化: 合并主控判断+任务规划)
        /// </summary>
        private class UnifiedPlanResult
        {
            public bool IsDirectResponse { get; set; }
            public string DirectResponseContent { get; set; }
            public string Reason { get; set; }
            public int Confidence { get; set; }
            public TaskPlanning TaskPlanning { get; set; }
            public string LlmRawResponse { get; set; }
            /// <summary>
            /// 已替换好参数的完整提示词（用于日志记录）
            /// </summary>
            public string ResolvedPrompt { get; set; }
        }

        private readonly IBackgroundPostProcessingQueue _postProcessingQueue;

        public ExecutionClaw(
            IChatService chatService,
            IServiceProvider provider,
            ILogger<ExecutionClaw> logger,
            ITaskPlanningService taskPlanningService,
            IMemoryService memoryService,
            IReflectionService reflectionService,
            IAgentOrchestrationService agentOrchestrationService,
            IPersonalityService personalityService,
            IMasterControlService masterControlService,
            IBackgroundPostProcessingQueue postProcessingQueue)
            : base(chatService, provider, logger)
        {
            _taskPlanningService = taskPlanningService;
            _memoryService = memoryService;
            _reflectionService = reflectionService;
            _agentOrchestrationService = agentOrchestrationService;
            _personalityService = personalityService;
            _masterControlService = masterControlService;
            _postProcessingQueue = postProcessingQueue;

            // 初始化 Pipeline 处理器
            _modelInitializer = new ModelInitializer(logger);
            _contextLoader = new ContextLoader(memoryService, personalityService, agentOrchestrationService, logger);
            _greetingFastPath = new GreetingFastPath(chatService, logger);
            _planningHandler = new PlanningHandler(taskPlanningService, logger);
        }

        /// <summary>
        /// Claw AI 节点异步执行方法
        /// </summary>
        public async Task<string> ClawAINodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            ConcurrentQueue<string> Logs = new ConcurrentQueue<string>();
            ExecutionRecordStatus ExecutionRecordStatus = ExecutionRecordStatus.Success;

            string AppID = data.AppID;
            string TaskID = data.TaskID;
            string SessionID = data.SessionID;
            string ProcessesID = data.ProcessesID.IsNullOrEmpty() ? Guid.NewGuid().ToString() : data.ProcessesID;
            string MemberID = data.MemberID.IsNullOrEmpty() ? "system" : data.MemberID;  // 如果MemberID为空,使用默认值
            string FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;

            // 从上游节点获取 attachments 和 additionalOptions
            Inputs attachmentInput = inputs.FirstOrDefault(i => i.varname == "attachments");
            Inputs additionalOptionsInput = inputs.FirstOrDefault(i => i.varname == "additionalOptions");

            if (attachmentInput != null)
            {
                data.AttachmentItems = JsonConvert.DeserializeObject<List<AttachmentItem>>(attachmentInput.value);
            }
            if (additionalOptionsInput != null)
            {
                data.AdditionalOptions = JsonConvert.DeserializeObject(additionalOptionsInput.value);
            }

            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            // P1优化: 创建记录更新节流器，替代逐条 updateExcutionRecord
            using var throttler = new RecordUpdateThrottler(
                RecordID, outputs, Logs,
                (rid, status, outs, logs) => ZSN.AI.Node.Utils.Utils.updateExcutionRecord(rid, status, outs, logs),
                intervalMs: 500);
            
            // 流式输出Key - 使用标准格式
            string streamKey = StreamKey.Build(SessionID, ProcessesID);

            // P2优化: 创建流式批量写入器，减少Redis网络往返
            using var batchWriter = new StreamBatchWriter(
                _streamSync, streamKey, SessionID, ProcessesID, TaskID, config.id, intervalMs: 200);

            // 创建统一的Progress对象用于流式输出
            var progress = new Progress<string>(delta => {
                batchWriter.Append(delta);
            });

            // 初始化执行日志记录器（在 try 外声明，确保 catch 也能访问）
            ClawAIExecutionLogger execLogger = null;
            var execStartTime = DateTime.Now;

            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.CLAW, $"开始执行 - SessionID: {SessionID}, TaskID: {TaskID}");
                Logs.Enqueue("=== Claw AI 节点开始执行 ===");

                // 解析节点配置
                ClawAIData nodeData = JsonConvert.DeserializeObject<ClawAIData>(config.data.ToString());
                if (nodeData == null)
                {
                    throw new Exception("节点配置解析失败");
                }

                // P1优化: 一次性构建替换缓存，后续复用
                var promptCache = this.BuildPromptReplaceCache(inputs, config.fromNodeId, SessionID, AppID, ProcessesID);

                nodeData.prompt = await this.ReplacePromptValueCached(nodeData.prompt, promptCache, SessionID, AppID, ProcessesID);

                // === 阶段 0: 初始化模型配置 ===
                var initModelMsg = "\n=== 阶段 0: 初始化模型配置 ===";

                // 初始化执行日志记录器
                execLogger = new ClawAIExecutionLogger(TaskID);
                execStartTime = DateTime.Now;
                Logs.Enqueue(initModelMsg);
                batchWriter.Append(initModelMsg);
                throttler.MarkDirty(); // P1: 节流更新

                // 使用 ModelInitializer 初始化所有模型
                var modelInitResult = _modelInitializer.InitializeAllModels(nodeData);
                foreach (var log in modelInitResult.Logs) { Logs.Enqueue(log); }
                throttler.MarkDirty(); // P1: 节流更新

                // 提取模型配置，供后续使用
                var mainModelConfig = modelInitResult.MainModelConfig;
                var planningModelConfig = modelInitResult.PlanningModelConfig;
                var reflectionModelConfig = modelInitResult.ReflectionModelConfig;
                var memoryModelConfig = modelInitResult.MemoryModelConfig;
                var profileModelConfig = modelInitResult.ProfileModelConfig;
                var personalityModelConfig = modelInitResult.PersonalityModelConfig;

                // === 阶段 1: 初始化上下文 ===
                var initMsg = "\n=== 阶段 1: 初始化上下文 ===";
                Logs.Enqueue(initMsg);
                batchWriter.Append(initMsg);
                throttler.MarkDirty(); // P1: 节流更新

                // 使用 ContextLoader 并行加载上下文
                var contextResult = await _contextLoader.LoadContextAsync(
                    AppID, SessionID, MemberID, inputs, nodeData, config.id);

                foreach (var log in contextResult.Logs) { Logs.Enqueue(log); }
                batchWriter.Append(contextResult.WorkflowLogMessage);
                throttler.MarkDirty(); // P1: 节流更新

                // 提取上下文数据，供后续使用
                var originalTask = contextResult.OriginalTask;
                var memoryContext = contextResult.MemoryContext;
                var availableWorkflows = contextResult.AvailableWorkflows;

                // 记录输入到执行日志
                execLogger.LogInput(originalTask, nodeData.prompt, availableWorkflows);

                // === P0优化: 统一规划阶段 (合并主控判断+任务规划为单次LLM调用) ===
                TaskPlanning taskPlanning = null;
                bool shouldDirectResponse = false;
                string directResponseContent = "";

                // 检查是否使用统一规划 (优先使用新模板)
                bool useUnifiedPlan = !string.IsNullOrEmpty(nodeData.taskPlanningConfig.unifiedPlanPromptTemplate);

                // 传递 attachments 和 additionalOptions 到下游节点
                var _attachmentsString = JsonConvert.SerializeObject(data.AttachmentItems ?? new List<AttachmentItem>());
                var _additionalOptionsString = data.AdditionalOptions != null ? JsonConvert.SerializeObject(data.AdditionalOptions) : null;


                if (useUnifiedPlan)
                {
                    var unifiedMsg = "\n=== 阶段 2: 统一规划 (判断+规划合并) ===";
                    Logs.Enqueue(unifiedMsg);
                    batchWriter.Append(unifiedMsg);
                    throttler.MarkDirty(); // P1: 节流更新

                    try
                    {
                        var unifiedResult = await UnifiedPlanOrRespondAsync(
                            nodeData, planningModelConfig, originalTask, availableWorkflows,
                            memoryContext, promptCache, AppID, SessionID, MemberID, config,
                            ProcessesID, progress, Logs);

                        if (unifiedResult.IsDirectResponse)
                        {
                            shouldDirectResponse = true;
                            directResponseContent = unifiedResult.DirectResponseContent;

                            var directMsg = $"⚡ 统一规划决策: 直接回复 (置信度: {unifiedResult.Confidence}%)\n   理由: {unifiedResult.Reason}";
                            Logs.Enqueue(directMsg);
                            batchWriter.Append(directMsg);
                        }
                        else
                        {
                            taskPlanning = unifiedResult.TaskPlanning;

                            var planMsg = $"✓ 统一规划决策: 任务规划 (置信度: {unifiedResult.Confidence}%)\n   理由: {unifiedResult.Reason}";
                            Logs.Enqueue(planMsg);
                            batchWriter.Append(planMsg);

                            // 记录规划结果到执行日志（使用已替换好参数的提示词）
                            execLogger.LogPlanning(
                                unifiedResult.ResolvedPrompt ?? nodeData.taskPlanningConfig.unifiedPlanPromptTemplate,
                                unifiedResult.LlmRawResponse ?? "",
                                "",
                                null, // 校验错误已在内部记录
                                Newtonsoft.Json.JsonConvert.SerializeObject(taskPlanning, Formatting.Indented));

                            // 推送规划步骤给前端
                            if (taskPlanning != null)
                            {
                                var stepsJson = JsonConvert.SerializeObject(new
                                {
                                    type = "planning_steps",
                                    planningId = taskPlanning.PlanningID,
                                    totalSteps = taskPlanning.TotalSteps,
                                    steps = taskPlanning.Steps.Select(s => new
                                    {
                                        stepId = s.StepID, stepIndex = s.StepIndex,
                                        description = s.StepDescription, type = s.StepType.ToString(),
                                        assignedWorkflowIds = s.AssignedWorkflowIds,
                                        dependencies = s.DependsOnStepIds,
                                        status = s.StepStatus.ToString()
                                    }).ToList()
                                });
                                batchWriter.Append($"\n[PLANNING_STEPS]\n{stepsJson}\n[/PLANNING_STEPS]");
                            }
                        }
                        throttler.MarkDirty(); // P1: 节流更新
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError(_logger, ClawLogModules.CLAW, $"统一规划失败，回退到传统路径: {ex.Message}", ex);
                        Logs.Enqueue($"⚠ 统一规划失败，回退到传统路径: {ex.Message}");

                        // 回退: 使用 GreetingDetector + CreateSimplePlan
                        shouldDirectResponse = GreetingDetector.IsSimpleGreeting(originalTask);
                        if (!shouldDirectResponse)
                        {
                            taskPlanning = _taskPlanningService.CreateSimplePlan(
                                originalTask, availableWorkflows, AppID, SessionID, MemberID, config.id, ProcessesID);
                        }
                    }
                }
                else
                {
                    // === 传统路径: 保留原有的阶段1.5(主控判断) + 阶段2(任务规划) ===
                    string masterControlReason = "";
                    MasterControlResult masterControlResult = null;

                    if (nodeData.masterControlConfig != null && nodeData.masterControlConfig.enabled)
                    {
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MASTER_CONTROL, "启用智能主控判断(传统路径)");

                        var masterControlService = _masterControlService;
                        if (masterControlService == null)
                        {
                            var masterLogger = _provider.GetService<ILogger<MasterControlService>>() ??
                                              _provider.GetService<ILoggerFactory>()?.CreateLogger<MasterControlService>();
                            var tempOptions = Options.Create(new ClawAIOptions());
                            masterControlService = new MasterControlService(
                                _chatService, masterLogger, tempOptions);
                        }

                        var masterControlContext = new ZSN.AI.Node.Claw.Models.MasterControlContext
                        {
                            UserInput = originalTask,
                            SystemPrompt = nodeData.prompt,
                            ChatHistory = FormatChatHistoryForMasterControl(memoryContext.WorkingMemory),
                            AvailableWorkflows = FormatWorkflowsForMasterControl(availableWorkflows),
                            UserProfileSummary = memoryContext.UserProfile?.PreferencesSummary ?? "",
                            AppID = AppID, SessionID = SessionID, MemberID = MemberID,
                            ModelConfig = mainModelConfig,
                            PromptTemplate = await this.ReplacePromptValueCached(nodeData.masterControlConfig?.promptTemplate, promptCache, SessionID, AppID, ProcessesID)
                        };

                        try
                        {
                            masterControlResult = await masterControlService.DecideAsync(masterControlContext);
                            shouldDirectResponse = (masterControlResult.Decision == ZSN.AI.Node.Claw.Models.MasterControlDecision.DirectResponse);
                            masterControlReason = masterControlResult.Reason;

                            var decisionMsg = $"\n🎯 主控决策: {masterControlResult.Decision} (置信度: {masterControlResult.Confidence}%)\n   理由: {masterControlReason}";
                            Logs.Enqueue(decisionMsg);
                            batchWriter.Append(decisionMsg);
                            throttler.MarkDirty(); // P1: 节流更新
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.LogError(_logger, ClawLogModules.MASTER_CONTROL, $"主控判断失败: {ex.Message}", ex);
                            shouldDirectResponse = GreetingDetector.IsSimpleGreeting(originalTask);
                            masterControlReason = "主控判断失败，使用回退策略";
                        }
                    }
                    else
                    {
                        shouldDirectResponse = GreetingDetector.IsSimpleGreeting(originalTask);
                        masterControlReason = $"使用关键词匹配: {GreetingDetector.GetGreetingType(originalTask)}";
                    }

                    // 统一获取 directResponseContent (兼容两种路径)
                    directResponseContent = masterControlResult?.DirectResponseContent ?? "";

                    // 传统路径的任务规划
                    if (!shouldDirectResponse)
                    {
                        var planningMsg = "\n=== 阶段 2: 任务规划(传统) ===";
                        Logs.Enqueue(planningMsg);
                        batchWriter.Append(planningMsg);
                        throttler.MarkDirty(); // P1: 节流更新

                        if (nodeData.taskPlanningConfig.enabled && availableWorkflows.Count > 0)
                        {
                            taskPlanning = await _taskPlanningService.CreatePlanningAsync(
                                nodeData, planningModelConfig, originalTask, availableWorkflows,
                                memoryContext, AppID, SessionID, MemberID, config.id, ProcessesID, progress);

                            var planResultMsg = $"✓ 规划完成: 总步骤={taskPlanning.TotalSteps}, 策略={taskPlanning.Metadata.Strategy}, 置信度={taskPlanning.Metadata.Confidence}%";
                            Logs.Enqueue(planResultMsg);
                            batchWriter.Append(planResultMsg);
                            throttler.MarkDirty(); // P1: 节流更新
                        }
                        else
                        {
                            taskPlanning = _taskPlanningService.CreateSimplePlan(
                                originalTask, availableWorkflows, AppID, SessionID, MemberID, config.id, ProcessesID);
                            Logs.Enqueue("创建简单执行计划(未启用规划或无可用WorkFlow)");
                            batchWriter.Append("创建简单执行计划");
                            throttler.MarkDirty(); // P1: 节流更新
                        }
                    }
                }

                // === 直接响应路径 (统一入口) ===
                if (shouldDirectResponse)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.GREETING_FAST_PATH, "启用直接响应路径");

                    var fastPathMsg = "\n⚡ 启用直接响应路径";
                    Logs.Enqueue(fastPathMsg);
                    batchWriter.Append(fastPathMsg);
                    throttler.MarkDirty(); // P1: 节流更新

                    string greetingResult;

                    if (!string.IsNullOrEmpty(directResponseContent))
                    {
                        greetingResult = directResponseContent;
                        Logs.Enqueue("✓ 使用LLM直接生成的回复内容");
                    }
                    else
                    {
                        var chatHistory = new ChatHistory();
                        chatHistory.AddSystemMessage(nodeData.prompt);

                        // 注入历史对话上下文，确保直接回复路径也能感知之前的对话内容
                        if (memoryContext?.WorkingMemory != null && memoryContext.WorkingMemory.Count > 0)
                        {
                            foreach (var msg in memoryContext.WorkingMemory.OrderBy(m => m.CreateTime))
                            {
                                if (msg.Role == "user")
                                    chatHistory.AddUserMessage(msg.Content?.ToString() ?? "");
                                else if (msg.Role == "assistant")
                                    chatHistory.AddAssistantMessage(msg.Content?.ToString() ?? "");
                            }
                        }

                        chatHistory.AddUserMessage(originalTask);

                        var responseBuilder = new StringBuilder();
                        await foreach (var chunk in _chatService.SendChatAsync(
                            mainModelConfig, chatHistory, Function: null,
                            responseFormat: "text", enableStreamingObservation: true,
                            progress: progress, ct: System.Threading.CancellationToken.None))
                        {
                            responseBuilder.Append(chunk);
                        }
                        greetingResult = responseBuilder.ToString();
                    }

                    // ChatHistory 由 EndNodeAsync 统一保存，此处不再重复写入

                    outputs.Add(new Output { varname = "results", value = greetingResult, nodeId = config.id, sourceId = $"{config.id}_results" });
                    outputs.Add(new Output { varname = "totalSteps", value = "0", nodeId = config.id, sourceId = $"{config.id}_totalSteps" });
                    outputs.Add(new Output { varname = "completedSteps", value = "0", nodeId = config.id, sourceId = $"{config.id}_completedSteps" });
                    outputs.Add(new Output { varname = "iterations", value = "0", nodeId = config.id, sourceId = $"{config.id}_iterations" });
                    outputs.Add(new Output { varname = "planningStatus", value = "FastPath", nodeId = config.id, sourceId = $"{config.id}_planningStatus" });

                    
                    outputs.Add(new Output { varname = "attachments", type = "List<AttachmentItem>", value = _attachmentsString, nodeId = config.id, sourceId = $"{config.id}_attachments" });
                    if (_additionalOptionsString != null)
                        outputs.Add(new Output { varname = "additionalOptions", type = "dynamic", value = _additionalOptionsString, nodeId = config.id, sourceId = $"{config.id}_additionalOptions" });

                    // 触发下游节点
                    TriggerDownstreamNodes(config, outputs, AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, Logs);

                    Logs.Enqueue("=== 直接响应路径执行完成 ===");
                    throttler.FlushWithStatus(ExecutionRecordStatus.Success); // P1: 最终状态刷新
                    return RecordID;
                }

                // === 阶段 3: 执行与反思循环 ===
                var executeMsg = "\n=== 阶段 3: 执行与反思循环 ===";
                Logs.Enqueue(executeMsg);
                batchWriter.Append(executeMsg);
                throttler.MarkDirty(); // P1: 节流更新

                var executionResult = await ExecuteWithReflectionLoopAsync(
                    config,
                    taskPlanning,
                    nodeData,
                    planningModelConfig,
                    reflectionModelConfig,
                    originalTask,
                    inputs,
                    memoryContext,
                    availableWorkflows,
                    promptCache,
                    AppID,
                    SessionID,
                    ProcessesID,
                    TaskID,
                    Logs,
                    progress,
                    execLogger,
                    MemberID,
                    FromMainTaskID
                );

                string finalResult = executionResult.FinalResult;
                int totalIterations = executionResult.TotalIterations;

                // ===== 异步触发检测：子 WorkFlow 已异步触发，主流程提前退出 =====
                if (executionResult.IsAsyncTriggered)
                {
                    Logs.Enqueue("[AsyncTrigger] 子 WorkFlow 已异步触发，跳过后处理，线程释放");
                    batchWriter.Append("[AsyncTrigger] 等待子 WorkFlow 回调恢复...");
                    throttler.FlushWithStatus(ExecutionRecordStatus.Running);

                    execLogger?.LogFinalResult("[AsyncTrigger] 等待回调恢复", 0, taskPlanning);
                    execLogger?.Dispose();

                    return RecordID; // 返回 RecordID，状态保持 Running
                }

                // === 阶段 4: 后处理(异步) ===
                var postMsg = "\n=== 阶段 4: 后处理(异步启动) ===";
                Logs.Enqueue(postMsg);
                batchWriter.Append(postMsg);
                throttler.MarkDirty(); // P1: 节流更新

                // P1修复: 使用后台队列代替 Task.Run,避免作用域服务共享
                // 创建不可变快照,避免共享可变状态
                var postProcessingParams = new PostProcessingParams
                {
                    MemoryContext = memoryContext,
                    OriginalTask = originalTask,
                    FinalResult = finalResult,
                    TaskPlanning = taskPlanning,
                    AppID = AppID,
                    SessionID = SessionID,
                    MemberID = MemberID,
                    ClawID = config.id,
                    PersonalityConfig = nodeData.personalityConfig,
                    StreamKey = streamKey,
                    ProcessesID = ProcessesID,
                    TaskID = TaskID,
                    NodeID = config.id,
                    RecordID = RecordID,
                    Outputs = outputs,
                    Logs = Logs.ToList(), // P1修复: 转换 ConcurrentQueue 为 List
                    EmbeddingModelConfig = modelInitResult.EmbeddingModelConfig
                };

                // 创建不可变快照并加入后台队列
                var snapshot = PostProcessingSnapshot.CreateFrom(postProcessingParams);
                _postProcessingQueue.QueuePostProcessing(snapshot);
                
                var asyncMsg = "✓ 后处理已在后台启动(记忆更新、ChatHistory保存、AI状态更新)";
                Logs.Enqueue(asyncMsg);
                batchWriter.Append(asyncMsg);
                throttler.MarkDirty(); // P1: 节流更新

                // === 阶段 5: 输出结果 ===
                var outputMsg = "\n=== 阶段 5: 输出结果 ===";
                Logs.Enqueue(outputMsg);
                batchWriter.Append(outputMsg);
                throttler.MarkDirty(); // P1: 节流更新

                outputs.Add(new Output
                {
                    varname = "results",
                    value = finalResult,
                    nodeId = config.id,
                    sourceId = $"{config.id}_results"
                });
                
                var resultMsg = $"✓ 最终结果 ({finalResult.Length} 字符):\n{finalResult}";
                Logs.Enqueue(resultMsg);
                batchWriter.Append(resultMsg);
                throttler.MarkDirty(); // P1: 节流更新

                // 记录最终结果到执行日志
                execLogger.LogFinalResult(finalResult, (DateTime.Now - execStartTime).TotalSeconds, taskPlanning);
                execLogger.Dispose();

                outputs.Add(new Output
                {
                    varname = "taskPlanning",
                    value = JsonConvert.SerializeObject(taskPlanning),
                    nodeId = config.id,
                    sourceId = $"{config.id}_taskPlanning"
                });

                outputs.Add(new Output
                {
                    varname = "totalSteps",
                    value = taskPlanning.TotalSteps.ToString(),
                    nodeId = config.id,
                    sourceId = $"{config.id}_totalSteps"
                });

                outputs.Add(new Output
                {
                    varname = "completedSteps",
                    value = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Completed).ToString(),
                    nodeId = config.id,
                    sourceId = $"{config.id}_completedSteps"
                });

                outputs.Add(new Output
                {
                    varname = "iterations",
                    value = totalIterations.ToString(),
                    nodeId = config.id,
                    sourceId = $"{config.id}_iterations"
                });

                outputs.Add(new Output
                {
                    varname = "planningStatus",
                    value = taskPlanning.PlanningStatus.ToString(),
                    nodeId = config.id,
                    sourceId = $"{config.id}_planningStatus"
                });

                // 传递 attachments 和 additionalOptions 到下游节点
                
                outputs.Add(new Output { varname = "attachments", type = "List<AttachmentItem>", value = _attachmentsString, nodeId = config.id, sourceId = $"{config.id}_attachments" });
                if (_additionalOptionsString != null)
                    outputs.Add(new Output { varname = "additionalOptions", type = "dynamic", value = _additionalOptionsString, nodeId = config.id, sourceId = $"{config.id}_additionalOptions" });

                var statsMsg = $"✓ 统计信息: 总步骤={taskPlanning.TotalSteps}, 已完成={taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Completed)}, 迭代次数={totalIterations}, 状态={taskPlanning.PlanningStatus}";
                Logs.Enqueue(statsMsg);
                batchWriter.Append(statsMsg);
                throttler.MarkDirty(); // P1: 节流更新

                // 触发下游节点 (P3优化: 使用共用方法)
                TriggerDownstreamNodes(config, outputs, AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, Logs);

                Logs.Enqueue("\n=== 执行完成 ===");
                Logs.Enqueue($"总耗时: {taskPlanning.Metadata.ActualDuration} 秒");
                Logs.Enqueue($"总迭代: {totalIterations} 次");
                Logs.Enqueue($"规划修订: {taskPlanning.Metadata.RevisionCount} 次");
                Logs.Enqueue($"最终状态: {taskPlanning.PlanningStatus}");

                LoggerHelper.LogInfo(_logger, ClawLogModules.EXECUTION, $"执行完成 - SessionID: {SessionID}, 状态: {taskPlanning.PlanningStatus}");
                
                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.CLAW, $"执行失败 - SessionID: {SessionID}", ex);
                Logs.Enqueue($"\n✗ 执行失败: {ex.Message}");
                Logs.Enqueue($"堆栈: {ex.StackTrace}");
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;

                // 记录执行失败到日志
                try { execLogger?.LogRaw($"\n✗ 执行失败: {ex.Message}\n堆栈: {ex.StackTrace}"); execLogger?.Dispose(); }
                catch { /* 日志写入失败不影响主流程 */ }
            }

            // 确保最终状态写入数据库（成功/失败都需要显式刷新，不能依赖 Dispose 中的 Running 状态）
            throttler.FlushWithStatus(ExecutionRecordStatus);
            return RecordID;
        }

        /// <summary>
        /// 执行与反思循环
        /// </summary>
        private async Task<ExecutionLoopResult> ExecuteWithReflectionLoopAsync(
            NodeConfig config,
            TaskPlanning taskPlanning,
            ClawAIData nodeData,
            LargeModelConfig planningModelConfig,
            LargeModelConfig reflectionModelConfig,
            string originalTask,
            List<Inputs> inputs,
            MemoryContext memoryContext,
            List<WorkflowConfigInfo> availableWorkflows,
            PromptReplaceCache promptCache,
            string AppID,
            string SessionID,
            string ProcessesID,
            string TaskID,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress,
            ClawAIExecutionLogger execLogger = null,
            string MemberID = "system",
            string FromMainTaskID = "")
        {
            int maxIterations = nodeData.workFlowLoopConfig.maxIterations;
            int iteration = 0;
            string finalResult = "";
            bool shouldContinue = true;
            int lastReflectionQuality = 0; // 追踪最后一轮反思质量

            // 更新规划状态为执行中
            taskPlanning.PlanningStatus = PlanningStatus.Executing;
            await _taskPlanningService.UpdatePlanningStatusAsync(taskPlanning);

            var startTime = DateTime.Now;

            // P2优化: 循环前一次性替换 Prompt 模板，避免循环内重复替换+覆盖原始配置
            string resolvedReflectionPrompt = nodeData.reflectionConfig.reflectionPromptTemplate;
            if (nodeData.reflectionConfig.enabled && !string.IsNullOrEmpty(resolvedReflectionPrompt))
            {
                resolvedReflectionPrompt = await this.ReplacePromptValueCached(resolvedReflectionPrompt, promptCache, SessionID, AppID, ProcessesID);
            }
            string resolvedPlanningPrompt = nodeData.taskPlanningConfig.planningPromptTemplate;
            if (!string.IsNullOrEmpty(resolvedPlanningPrompt))
            {
                resolvedPlanningPrompt = await this.ReplacePromptValueCached(resolvedPlanningPrompt, promptCache, SessionID, AppID, ProcessesID);
            }

            while (shouldContinue && iteration < maxIterations)
            {
                iteration++;
                var iterMsg = $"\n--- 迭代 {iteration}/{maxIterations} ---";
                Logs.Enqueue(iterMsg);
                progress?.Report(iterMsg);
                //_ = _streamSync.AppendDeltaAsync(streamKey, SessionID, ProcessesID, TaskID, "", iterMsg + "\n");

                // 执行当前步骤
                var executionResult = await _agentOrchestrationService.ExecuteStepsAsync(
                    config,
                    taskPlanning,
                    AppID,
                    SessionID,
                    ProcessesID,
                    TaskID,
                    originalTask,
                    inputs,
                    nodeData,
                    reflectionModelConfig,
                    Logs,
                    progress,
                    execLogger,
                    MemberID,
                    FromMainTaskID
                );

                // 注意: ConcurrentQueue 的日志已通过 progress 实时推送,无需再次遍历
                
                // 推送步骤状态更新
                foreach (var step in taskPlanning.Steps)
                {
                    // 只推送状态有变化的步骤(Executing, Completed, Failed)
                    if (step.StepStatus == StepStatus.Executing || 
                        step.StepStatus == StepStatus.Completed || 
                        step.StepStatus == StepStatus.Failed)
                    {
                        await PushStepStatusUpdateAsync(step, progress);
                    }
                }

                var execMsg = $"执行完成: {executionResult.CompletedSteps}/{taskPlanning.TotalSteps} 步骤\n";
                Logs.Enqueue(execMsg);
                progress?.Report(execMsg);

                // 记录每步执行的详细参数到专用日志（用于排查 WorkFlow 输入输出问题）
                if (execLogger != null)
                {
                    foreach (var step in taskPlanning.Steps)
                    {
                        if (step.StepStatus == StepStatus.Completed || step.StepStatus == StepStatus.Failed)
                        {
                            var duration = (step.EndTime.HasValue && step.StartTime.HasValue)
                                ? (step.EndTime.Value - step.StartTime.Value).TotalSeconds
                                : 0;

                            // 构建解析后的输入参数快照
                            // StepInputs 中是占位符格式，需要尝试从步骤上下文中获取实际传给 WorkFlow 的解析值
                            var resolvedInputs = ResolveStepInputsForLogging(step, taskPlanning, Logs);

                            execLogger.LogStepExecution(
                                step, null, null,
                                resolvedInputs,
                                step.ExecutionResult,
                                duration);
                        }
                    }
                }

                // ===== 异步触发检测：步骤已触发子 WorkFlow，退出反思循环 =====
                if (executionResult.IsAsyncTriggered)
                {
                    Logs.Enqueue("[AsyncTrigger] 步骤已异步触发子 WorkFlow，退出反思循环，线程释放");
                    LoggerHelper.LogInfo(_logger, ClawLogModules.CLAW,
                        $"[AsyncTrigger] 异步触发退出 - SessionID: {SessionID}, 等待子 WorkFlow 回调恢复");

                    return new ExecutionLoopResult
                    {
                        FinalResult = "",
                        TotalIterations = iteration,
                        IsAsyncTriggered = true
                    };
                }

                // 反思评估
                if (nodeData.reflectionConfig.enabled)
                {
                    // P2: 使用循环外预替换的模板
                    nodeData.reflectionConfig.reflectionPromptTemplate = resolvedReflectionPrompt;

                    // 使用动态任务分析（如果启用）
                    ReflectionResult reflectionResult;
                    if (nodeData.reflectionConfig.enableDynamicTaskAnalysis)
                    {
                        reflectionResult = await _reflectionService.AnalyzeTaskDynamicallyAsync(
                            nodeData,
                            reflectionModelConfig,
                            taskPlanning,
                            executionResult,
                            originalTask,
                            availableWorkflows,
                            progress
                        );
                    }
                    else
                    {
                        reflectionResult = await _reflectionService.ReflectOnExecutionAsync(
                            nodeData,
                            reflectionModelConfig,
                            taskPlanning,
                            executionResult,
                            originalTask,
                            iteration,
                            progress
                        );
                    }

                    var reflectMsg = $"反思评估:\n" +
                        $"  - 整体质量: {reflectionResult.OverallQuality}/100\n" +
                        $"  - 完成度: {reflectionResult.CompletenessScore}/100\n" +
                        $"  - 准确性: {reflectionResult.AccuracyScore}/100\n" +
                        $"  - 建议行动: {reflectionResult.Action}";
                    Logs.Enqueue(reflectMsg);
                    lastReflectionQuality = reflectionResult.OverallQuality;

                    progress?.Report(reflectMsg);
                    //_ = _streamSync.AppendDeltaAsync(streamKey, SessionID, ProcessesID, TaskID, "", reflectMsg + "\n");

                    // 更新 AI 情绪状态 (基于反思结果)
                    if (nodeData.personalityConfig.enabled && nodeData.personalityConfig.enableEmotionalState)
                    {
                        bool interactionSuccess = reflectionResult.OverallQuality >= 70;
                        memoryContext.AIState = await _personalityService.UpdateEmotionalStateAsync(
                            memoryContext.AIState,
                            interactionSuccess,
                            null,
                            nodeData.personalityConfig);
                        
                        LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $"AI情绪状态已更新 - Quality: {reflectionResult.OverallQuality}");
                    }

                    // 根据反思结果决定下一步
                    switch (reflectionResult.Action)
                    {
                        case ReflectionAction.Complete:
                            finalResult = reflectionResult.FinalAnswer;
                            taskPlanning.PlanningStatus = PlanningStatus.Completed;
                            shouldContinue = false;
                            var completeMsg = "✓ 任务完成";
                            Logs.Enqueue(completeMsg);
                            progress?.Report(completeMsg);
                            //_ = _streamSync.AppendDeltaAsync(streamKey, SessionID, ProcessesID, TaskID, "", completeMsg + "\n");

                            // ==================== P3优化: 保存反思到记忆 (优化点3) ====================
                            if (reflectionResult.OverallQuality >= 80)
                            {
                                await MemoryPersistenceService.SaveReflectionToMemoryAsync(
                                    AppID, SessionID, MemberID, config.id,
                                    originalTask, reflectionResult, taskPlanning, _logger);
                            }
                            break;

                        case ReflectionAction.ContinueExecution:
                            var continueMsg = "→ 继续执行下一步";
                            Logs.Enqueue(continueMsg);

                            progress?.Report(continueMsg);
                            //_ = _streamSync.AppendDeltaAsync(streamKey, SessionID, ProcessesID, TaskID, "", continueMsg + "\n");
                            break;

                        case ReflectionAction.RetryStep:
                            var retryMsg = $"↻ 重试步骤: {reflectionResult.RetryStepIndex}";
                            Logs.Enqueue(retryMsg);
                            progress?.Report(retryMsg);
                            //_ = _streamSync.AppendDeltaAsync(streamKey, SessionID, ProcessesID, TaskID, "", retryMsg + "\n");
                            await _agentOrchestrationService.RetryStepAsync(
                                taskPlanning, reflectionResult.RetryStepIndex, reflectionResult.RefinedPrompt);
                            break;

                        case ReflectionAction.Replan:
                            if (nodeData.taskPlanningConfig.allowDynamicReplanning)
                            {
                                var replanMsg = "⚡ 触发动态重新规划";
                                Logs.Enqueue(replanMsg);
                                progress?.Report(replanMsg);

                                // 显示任务分析结果
                                if (reflectionResult.TaskAnalysis != null)
                                {
                                    Logs.Enqueue($"  任务完成度: {reflectionResult.TaskAnalysis.CompletionPercentage}%");
                                    Logs.Enqueue($"  分析摘要: {reflectionResult.TaskAnalysis.Summary}");
                                    if (reflectionResult.TaskAnalysis.MissingCapabilities != null && reflectionResult.TaskAnalysis.MissingCapabilities.Count > 0)
                                    {
                                        Logs.Enqueue($"  缺失能力: {string.Join(", ", reflectionResult.TaskAnalysis.MissingCapabilities)}");
                                    }
                                }

                                // 应用建议的步骤（动态添加）
                                if (reflectionResult.SuggestedSteps != null && reflectionResult.SuggestedSteps.Count > 0)
                                {
                                    Logs.Enqueue($"  建议添加 {reflectionResult.SuggestedSteps.Count} 个新步骤:");
                                    
                                    await _taskPlanningService.ApplySuggestedStepsAsync(
                                        taskPlanning,
                                        reflectionResult.SuggestedSteps,
                                        availableWorkflows,
                                        Logs
                                    );

                                    var replanDoneMsg = $"✓ 动态规划完成: 总共 {taskPlanning.TotalSteps} 个步骤";
                                    Logs.Enqueue(replanDoneMsg);
                                    progress?.Report(replanDoneMsg);
                                }
                                else
                                {
                                    // 如果没有建议步骤，使用传统的重新规划
                                    Logs.Enqueue("  未提供建议步骤，使用传统重新规划");

                                    // P2: 使用循环外预替换的模板
                                    nodeData.taskPlanningConfig.planningPromptTemplate = resolvedPlanningPrompt;

                                    taskPlanning = await _taskPlanningService.ReplanAsync(
                                        nodeData,
                                        planningModelConfig,
                                        taskPlanning,
                                        executionResult,
                                        reflectionResult,
                                        availableWorkflows,
                                        memoryContext,
                                        progress
                                    );
                                    
                                    var replanDoneMsg = $"✓ 重新规划完成: {taskPlanning.TotalSteps} 个步骤";
                                    Logs.Enqueue(replanDoneMsg);
                                    progress?.Report(replanDoneMsg);
                                }
                            }
                            else
                            {
                                var noReplanMsg = "⚠ 需要重新规划,但未启用动态规划";
                                Logs.Enqueue(noReplanMsg);
                                progress?.Report(noReplanMsg);
                                //_ = _streamSync.AppendDeltaAsync(streamKey, SessionID, ProcessesID, TaskID, "", noReplanMsg + "\n");
                            }
                            break;

                        case ReflectionAction.Fail:
                            var failMsg = $"✗ 任务失败: {reflectionResult.Reason}";
                            Logs.Enqueue(failMsg);
                            progress?.Report(failMsg);
                            //_ = _streamSync.AppendDeltaAsync(streamKey, SessionID, ProcessesID, TaskID, "", failMsg + "\n");
                            taskPlanning.PlanningStatus = PlanningStatus.Failed;
                            finalResult = $"任务执行失败: {reflectionResult.Reason}";
                            shouldContinue = false;
                            break;
                    }
                }
                else
                {
                    // 不启用反思,检查是否所有步骤都完成
                    if (executionResult.AllStepsCompleted)
                    {
                        finalResult = _agentOrchestrationService.CombineStepResults(taskPlanning);
                        taskPlanning.PlanningStatus = PlanningStatus.Completed;
                        shouldContinue = false;
                        var allDoneMsg = "✓ 所有步骤执行完成";
                        Logs.Enqueue(allDoneMsg);
                        progress?.Report(allDoneMsg);
                        //_ = _streamSync.AppendDeltaAsync(streamKey, SessionID, ProcessesID, TaskID, "", allDoneMsg + "\n");
                    }
                }

                // 注意: 不在此处重复检查步骤完成状态
                // 完成判断已由反思服务统一处理，避免逻辑冲突
            }

            // 达到最大迭代次数
            if (iteration >= maxIterations && shouldContinue)
            {
                Logs.Enqueue($"⚠ 达到最大迭代次数 {maxIterations}");
                if (string.IsNullOrEmpty(finalResult))
                {
                    finalResult = _agentOrchestrationService.CombineStepResults(taskPlanning);
                }
                taskPlanning.PlanningStatus = PlanningStatus.Completed;
            }

            // 计算实际耗时
            var endTime = DateTime.Now;
            taskPlanning.Metadata.ActualDuration = (int)(endTime - startTime).TotalSeconds;

            // 更新规划状态
            await _taskPlanningService.UpdatePlanningStatusAsync(taskPlanning);

            // 使用主模型对最终结果进行优化整合
            if (!string.IsNullOrEmpty(finalResult))
            {
                // 判断是否需要优化：多步骤任务的结果包含中间步骤输出，需要整合优化
                bool shouldOptimize = taskPlanning.TotalSteps > 3 &&
                                      finalResult.Length > 500;
                
                if (shouldOptimize)
                {
                    try
                    {
                        var optimizeMsg = "\n优化最终结果...";
                        Logs.Enqueue(optimizeMsg);
                        progress?.Report(optimizeMsg);

                        finalResult = await OptimizeFinalResultAsync(
                            finalResult,
                            originalTask,
                            taskPlanning,
                            memoryContext,
                            new LargeModelConfig { Model = nodeData.model },
                            Logs,
                            progress
                        );

                        var optimizedMsg = "✓ 结果优化完成";
                        Logs.Enqueue(optimizedMsg);
                        progress?.Report(optimizedMsg);
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError(_logger, ClawLogModules.EXECUTION, "优化最终结果失败,使用原始结果", ex);
                        var errorMsg = $"⚠ 结果优化失败: {ex.Message},使用原始结果";
                        Logs.Enqueue(errorMsg);

                        progress?.Report(errorMsg);
                        //_ = _streamSync.AppendDeltaAsync(streamKey, SessionID, ProcessesID, TaskID, "", errorMsg + "\n");
                    }
                }
                else
                {
                    Logs.Enqueue($"✓ 跳过结果优化 (Steps: {taskPlanning.TotalSteps}, Length: {finalResult.Length})");
                    LoggerHelper.LogInfo(_logger, ClawLogModules.EXECUTION, $"跳过结果优化 - Steps: {taskPlanning.TotalSteps}, Length: {finalResult.Length}");
                }
            }

            return new ExecutionLoopResult
            {
                FinalResult = finalResult,
                TotalIterations = iteration
            };
        }

        /// <summary>
        /// 优化最终结果
        /// </summary>
        private async Task<string> OptimizeFinalResultAsync(
            string rawResult,
            string originalTask,
            TaskPlanning taskPlanning,
            MemoryContext memoryContext,
            LargeModelConfig mainModelConfig,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress)
        {
            try
            {
                var optimizationPrompt = new StringBuilder();
                optimizationPrompt.AppendLine("# 任务: 优化和整合执行结果");
                optimizationPrompt.AppendLine();
                
                // 添加历史对话上下文
                if (memoryContext.WorkingMemory != null && memoryContext.WorkingMemory.Count > 0)
                {
                    optimizationPrompt.AppendLine("## 历史对话");
                    foreach (var msg in memoryContext.WorkingMemory.OrderBy(m => m.CreateTime))
                    {
                        optimizationPrompt.AppendLine($"{msg.Role}: {msg.Content}");
                    }
                    optimizationPrompt.AppendLine();
                }
                
                optimizationPrompt.AppendLine("## 用户当前问题");
                optimizationPrompt.AppendLine(originalTask);
                optimizationPrompt.AppendLine();
                
                optimizationPrompt.AppendLine("## WorkFlow执行结果");
                optimizationPrompt.AppendLine(rawResult);
                optimizationPrompt.AppendLine();
                
                optimizationPrompt.AppendLine("## 要求");
                optimizationPrompt.AppendLine("请基于用户的原始问题和历史对话上下文,对以上WorkFlow执行结果进行:");
                optimizationPrompt.AppendLine("1. 智能整合和去重");
                optimizationPrompt.AppendLine("2. 格式优化和美化");
                optimizationPrompt.AppendLine("3. 补充必要的解释说明");
                optimizationPrompt.AppendLine("4. 确保回答完整且符合用户意图");
                optimizationPrompt.AppendLine();
                optimizationPrompt.AppendLine("直接输出优化后的最终答案,不要添加额外的说明。");
                
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(optimizationPrompt.ToString());
                
                var responseBuilder = new StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    mainModelConfig, 
                    chatHistory,
                    Function: null,
                    responseFormat: "text",
                    enableStreamingObservation: true,
                    progress: progress,
                    ct: System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }
                
                string optimizedResult = responseBuilder.ToString();
                Logs.Enqueue($"结果优化完成,长度: {optimizedResult.Length} 字符");
                
                return optimizedResult;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.EXECUTION, "优化最终结果失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 推送步骤状态更新到前端
        /// </summary>
        private async Task PushStepStatusUpdateAsync(
            TaskStep step, 
            IProgress<string> progress)
        {
            try
            {
                var stepUpdateJson = JsonConvert.SerializeObject(new
                {
                    type = "step_status_update",
                    stepId = step.StepID,
                    stepIndex = step.StepIndex,
                    status = step.StepStatus.ToString(),
                    description = step.StepDescription,
                    actualOutput = step.ActualOutput,
                    qualityScore = step.QualityScore,
                    errorMessage = step.ErrorMessage,
                    retryCount = step.RetryCount,
                    startTime = step.StartTime,
                    endTime = step.EndTime
                });
                
                progress?.Report($"[STEP_UPDATE]{stepUpdateJson}[/STEP_UPDATE]");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning(_logger, ClawLogModules.EXECUTION, $"推送步骤状态更新失败 - StepID: {step.StepID}", ex);
            }
        }

        /// <summary>
        /// 安全获取模型信息 - 带重试机制
        /// </summary>
        private LargeModelInfo GetModelWithRetry(int modelId, string modelType = "模型")
        {
            return RetryPolicy.Execute(
                () =>
                {
                    var model = LargeModelInfoBussiness.GetModel(modelId);
                    if (model == null)
                    {
                        throw new Exception($"{modelType}不存在: ModelID={modelId}");
                    }
                    return model;
                },
                maxRetries: 3,
                delayMs: 500,
                onRetry: (ex, attempt) =>
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.MODEL_INIT, $"获取{modelType}失败，第 {attempt} 次重试 - ModelID: {modelId}", ex);
                }
            );
        }

        
        /// <summary>
        /// 格式化对话历史供主控判断使用
        /// </summary>
        private string FormatChatHistoryForMasterControl(List<AppChatLogInfo> workingMemory)
        {
            if (workingMemory == null || workingMemory.Count == 0)
            {
                return "无对话历史";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"最近 {workingMemory.Count} 条对话:");
            
            foreach (var msg in workingMemory.TakeLast(10)) // 只取最近10条
            {
                var role = msg.Role == "user" ? "用户" : "助手";
                var contentStr = msg.Content?.ToString() ?? "";
                sb.AppendLine($"- {role}: {contentStr}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 格式化WorkFlow列表供主控判断使用
        /// </summary>
        private string FormatWorkflowsForMasterControl(List<WorkflowConfigInfo> workflows)
        {
            if (workflows == null || workflows.Count == 0)
            {
                return "无可用WorkFlow";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"共 {workflows.Count} 个可用WorkFlow:");
            
            foreach (var workflow in workflows)
            {
                sb.AppendLine($"- {workflow.name}: {workflow.description}");
                if (workflow.capabilities != null && workflow.capabilities.Count > 0)
                {
                    sb.AppendLine($"  能力: {string.Join(", ", workflow.capabilities)}");
                }
            }

            return sb.ToString();
        }

        // [已删除] PostProcessingAsync 方法
        // 逻辑已完全移到 BackgroundPostProcessingQueue 服务中
        // 使用后台队列系统代替 Task.Run,确保线程安全和作用域隔离

        #region P0优化: 统一规划 (合并主控判断+任务规划)

        /// <summary>
        /// 统一规划: 单次LLM调用同时完成主控判断和任务规划 (P0优化)
        /// </summary>
        private async Task<UnifiedPlanResult> UnifiedPlanOrRespondAsync(
            ClawAIData nodeData,
            LargeModelConfig planningModelConfig,
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            MemoryContext memoryContext,
            PromptReplaceCache promptCache,
            string AppID,
            string SessionID,
            string MemberID,
            NodeConfig config,
            string ProcessesID,
            IProgress<string> progress,
            ConcurrentQueue<string> Logs)
        {
            // 构建统一 prompt
            var promptTemplate = nodeData.taskPlanningConfig.unifiedPlanPromptTemplate;
            promptTemplate = await this.ReplacePromptValueCached(promptTemplate, promptCache, SessionID, AppID, ProcessesID);

            // 替换基础变量
            promptTemplate = promptTemplate.Replace("{{originalTask}}", originalTask);

            // 替换对话历史
            var chatHistory = new StringBuilder();
            if (memoryContext.WorkingMemory != null && memoryContext.WorkingMemory.Count > 0)
            {
                foreach (var msg in memoryContext.WorkingMemory.OrderBy(m => m.CreateTime))
                {
                    chatHistory.AppendLine($"**{msg.Role}**: {msg.Content}");
                }
            }
            promptTemplate = promptTemplate.Replace("{{chatHistory}}", chatHistory.ToString());

            // 替换系统提示词
            promptTemplate = promptTemplate.Replace("{{SystemPrompt}}", nodeData.prompt ?? "");

            // 替换可用 WorkFlow 列表
            var workflowsInfo = new StringBuilder();
            if (availableWorkflows != null && availableWorkflows.Count > 0)
            {
                workflowsInfo.AppendLine($"共有 {availableWorkflows.Count} 个可用 WorkFlow:");
                foreach (var wf in availableWorkflows)
                {
                    workflowsInfo.AppendLine($"- ID: {wf.workflowId}, 名称: {wf.name}, 描述: {wf.description}");
                }
            }
            else
            {
                workflowsInfo.AppendLine("无可用 WorkFlow");
            }
            promptTemplate = promptTemplate.Replace("{{availableWorkFlows}}", workflowsInfo.ToString());

            // 替换历史规划
            var historicalInfo = new StringBuilder("暂无历史规划经验");
            try
            {
                var historicalPlans = await _taskPlanningService.GetHistoricalPlansAsync(AppID, MemberID, 3);
                if (historicalPlans != null && historicalPlans.Count > 0)
                {
                    historicalInfo.Clear();
                    foreach (var plan in historicalPlans.Where(p => p.PlanningStatus == PlanningStatus.Completed).Take(3))
                    {
                        historicalInfo.AppendLine($"- 任务: {plan.OriginalTask}, 步骤数: {plan.TotalSteps}, 策略: {plan.Metadata?.Strategy}");
                    }
                }
            }
            catch { /* 历史规划获取失败不影响主流程 */ }
            promptTemplate = promptTemplate.Replace("{{historicalPlans}}", historicalInfo.ToString());

            // 调用 LLM（带重试机制）
            int maxRetries = nodeData.taskPlanningConfig?.maxParseRetries > 0
                ? nodeData.taskPlanningConfig.maxParseRetries : 2;
            string lastError = "";
            UnifiedPlanResponseData unifiedData = null;
            string responseText = "";

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var chatMsgs = new ChatHistory();
                chatMsgs.AddSystemMessage(promptTemplate);
                chatMsgs.AddUserMessage(originalTask);

                // 重试时追加纠错提示，让 LLM 修正上次的错误
                if (attempt > 0 && !string.IsNullOrEmpty(lastError))
                {
                    Logs.Enqueue($"[统一规划] 第 {attempt} 次重试（共 {maxRetries} 次），上次错误: {lastError}");
                    chatMsgs.AddAssistantMessage(responseText);
                    chatMsgs.AddUserMessage($@"你上次的输出存在格式错误，请修正后重新输出完整 JSON。
错误信息: {lastError}

格式要求（简化版）:
1. 所有字段必须使用正确的数据类型
2. dep 必须是整数数组（如 [1, 2]），引用依赖的步骤索引
3. wf 为空字符串时表示 llm_reasoning 步骤，否则填写可用 WorkFlow 清单中的 WorkFlow ID
4. prompt 中使用 {{N}} 引用第N步的输出
5. 不要输出任何 JSON 之外的内容（不要 markdown 代码块标记、不要注释）
6. 确保所有引号、括号、逗号正确配对，JSON 结构完整可解析");
                }

                var responseBuilder = new StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    planningModelConfig, chatMsgs, Function: null,
                    responseFormat: "json_object",
                    enableStreamingObservation: false,
                    progress: null,
                    ct: System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }

                responseText = responseBuilder.ToString().Trim();
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $"统一规划LLM响应 (尝试 {attempt + 1}/{maxRetries + 1}, 长度: {responseText.Length} 字符)");

                // 输出LLM原始响应到日志，便于排查字段映射问题
                Logs.Enqueue($"[统一规划] LLM原始响应 (尝试 {attempt + 1}, 长度: {responseText.Length}):\n{responseText}");

                // 尝试解析 JSON（优先使用简化版格式，回退到旧格式）
                try
                {
                    string jsonContent = ExtractJsonFromResponse(responseText);
                    Logs.Enqueue($"[统一规划] 提取的JSON内容:\n{jsonContent}");

                    // 优先尝试简化版格式
                    var simplified = Newtonsoft.Json.JsonConvert.DeserializeObject<SimplifiedPlanData>(jsonContent);
                    bool isSimplifiedFormat = simplified != null
                        && simplified.steps != null
                        && simplified.steps.Count > 0
                        && simplified.steps.Any(s => s.i > 0 && !string.IsNullOrEmpty(s.desc));

                    if (isSimplifiedFormat)
                    {
                        // 将简化版数据适配到旧格式（保持后续逻辑兼容）
                        unifiedData = AdaptSimplifiedToUnified(simplified);
                        Logs.Enqueue($"[统一规划] 使用简化版格式解析成功，共 {simplified.steps.Count} 个步骤");
                    }
                    else
                    {
                        // 回退到旧格式
                        Logs.Enqueue($"[统一规划] 简化版格式检测不通过（i/desc 为空），使用旧格式解析");
                        unifiedData = Newtonsoft.Json.JsonConvert.DeserializeObject<UnifiedPlanResponseData>(jsonContent);
                    }
                }
                catch (Exception parseEx)
                {
                    lastError = parseEx.Message;
                    LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING, $"统一规划 JSON 解析失败 (尝试 {attempt + 1}): {parseEx.Message}");

                    if (attempt < maxRetries)
                    {
                        continue; // 重试
                    }

                    // 最后一次重试也失败，抛出异常
                    throw new Exception($"JSON解析失败（已重试 {maxRetries} 次）: {parseEx.Message}\n--- LLM原始响应 (长度: {responseText.Length}) ---\n{responseText}\n--- 响应结束 ---", parseEx);
                }

                if (unifiedData == null)
                {
                    lastError = "JSON反序列化结果为null";
                    if (attempt < maxRetries)
                    {
                        continue;
                    }
                    throw new Exception($"统一规划响应解析失败 (反序列化结果为null，已重试 {maxRetries} 次)\n--- LLM原始响应 (长度: {responseText.Length}) ---\n{responseText}\n--- 响应结束 ---");
                }

                // 规划后校验（仅 taskPlanning 类型）
                if (!string.Equals(unifiedData.decision, "directResponse", StringComparison.OrdinalIgnoreCase))
                {
                    // 优先使用简化版校验
                    var simplifiedForValidation = TryDeserializeSimplified(ExtractJsonFromResponse(responseText));
                    bool useSimplifiedValidation = simplifiedForValidation != null
                        && simplifiedForValidation.steps != null
                        && simplifiedForValidation.steps.Count > 0
                        && simplifiedForValidation.steps.Any(s => s.i > 0 && !string.IsNullOrEmpty(s.desc));

                    List<string> validationErrors;
                    if (useSimplifiedValidation)
                    {
                        validationErrors = SimplifiedPlanConverter.Validate(simplifiedForValidation, availableWorkflows);
                    }
                    else
                    {
                        validationErrors = ValidatePlanning(unifiedData, availableWorkflows);
                    }

                    if (validationErrors.Count > 0)
                    {
                        lastError = "规划校验失败:\n" + string.Join("\n", validationErrors);
                        Logs.Enqueue($"[统一规划] 校验失败: {lastError}");
                        LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING, $"规划校验失败 (尝试 {attempt + 1}): {lastError}");

                        if (attempt < maxRetries)
                        {
                            continue; // 校验失败，重试
                        }

                        // 最后一次仍失败，记录警告但继续执行（降级）
                        Logs.Enqueue($"[统一规划] 校验失败但已达最大重试次数，降级继续执行");
                    }
                    else
                    {
                        Logs.Enqueue($"[统一规划] 规划校验通过");
                    }
                }

                // 解析+校验成功，跳出重试循环
                break;
            }

            // 输出解析后的字段值，便于排查字段名不匹配问题
            Logs.Enqueue($"[统一规划] 解析结果: decision={unifiedData.decision ?? "null"}, reason={unifiedData.reason ?? "null"}, confidence={unifiedData.confidence}, strategy={unifiedData.strategy ?? "null"}, goal={unifiedData.goal ?? "null"}, directResponse={unifiedData.directResponse ?? "null"}, steps.Count={unifiedData.steps?.Count ?? 0}");
            if (unifiedData.steps != null)
            {
                foreach (var s in unifiedData.steps)
                {
                    Logs.Enqueue($"  步骤{s.stepIndex}: dependsOn=[{string.Join(",", s.dependsOnStepIds ?? new List<int>())}], workflow=[{string.Join(",", s.assignedWorkflowIds ?? new List<string>())}]");
                }
            }

            // 判断结果
            bool isDirect = string.Equals(unifiedData.decision, "directResponse", StringComparison.OrdinalIgnoreCase);

            if (isDirect)
            {
                return new UnifiedPlanResult
                {
                    IsDirectResponse = true,
                    DirectResponseContent = unifiedData.directResponse ?? "",
                    Reason = unifiedData.reason ?? "",
                    Confidence = unifiedData.confidence,
                    LlmRawResponse = responseText,
                    ResolvedPrompt = promptTemplate  // 已替换好参数的提示词
                };
            }
            else
            {
                // 尝试使用简化版格式转换
                var simplified = TryDeserializeSimplified(ExtractJsonFromResponse(responseText));
                bool useSimplifiedConversion = simplified != null
                    && simplified.steps != null
                    && simplified.steps.Count > 0
                    && simplified.steps.Any(s => s.i > 0 && !string.IsNullOrEmpty(s.desc))
                    && !string.Equals(simplified.decision, "directResponse", StringComparison.OrdinalIgnoreCase);

                TaskPlanning planning;

                if (useSimplifiedConversion)
                {
                    // 使用 SimplifiedPlanConverter 转换
                    planning = SimplifiedPlanConverter.Convert(
                        simplified, originalTask, availableWorkflows,
                        AppID, SessionID, MemberID, config.id, ProcessesID, Logs);

                    Logs.Enqueue($"[统一规划] 使用 SimplifiedPlanConverter 转换成功");
                }
                else
                {
                    // 回退到旧的手动映射逻辑
                    planning = _taskPlanningService.CreateSimplePlan(
                        originalTask, availableWorkflows, AppID, SessionID, MemberID, config.id, ProcessesID);

                    if (unifiedData.steps != null && unifiedData.steps.Count > 0)
                    {
                        planning.TotalSteps = unifiedData.steps.Count;
                        planning.Steps = unifiedData.steps.Select((s, i) => new TaskStep
                        {
                            StepID = Guid.NewGuid().ToString("N").Substring(0, 8),
                            StepIndex = i + 1,
                            StepDescription = s.stepDescription ?? "",
                            StepType = ParseStepType(s.stepType),
                            AssignedWorkflowIds = s.assignedWorkflowIds ?? new List<string>(),
                            DependsOnStepIds = new List<string>(),
                            StepInputs = s.inputs?.Select(inp => new Inputs { varname = inp.varname, value = inp.value }).ToList() ?? new List<Inputs>(),
                            ExpectedOutput = s.expectedOutput ?? "",
                            StepStatus = StepStatus.Pending
                        }).ToList();

                        // 映射依赖
                        var stepIndexToIdMap = planning.Steps.ToDictionary(s => s.StepIndex, s => s.StepID);
                        for (int i = 0; i < unifiedData.steps.Count; i++)
                        {
                            var stepData = unifiedData.steps[i];
                            var step = planning.Steps[i];
                            if (stepData.dependsOnStepIds != null && stepData.dependsOnStepIds.Count > 0)
                            {
                                foreach (var depIndex in stepData.dependsOnStepIds)
                                {
                                    if (stepIndexToIdMap.ContainsKey(depIndex))
                                    {
                                        step.DependsOnStepIds.Add(stepIndexToIdMap[depIndex]);
                                    }
                                }
                            }
                        }

                        NormalizeInputPlaceholders(planning.Steps, stepIndexToIdMap);
                        DetectAndReplaceInlineEmbeddings(planning.Steps);

                        planning.Metadata = new PlanningMetadata
                        {
                            Strategy = unifiedData.strategy ?? "sequential",
                            Confidence = unifiedData.confidence,
                            EstimatedDuration = unifiedData.steps.Count * 15
                        };

                        planning.Goal = unifiedData.goal ?? originalTask;
                    }
                }

                return new UnifiedPlanResult
                {
                    IsDirectResponse = false,
                    TaskPlanning = planning,
                    Reason = unifiedData.reason ?? "",
                    Confidence = unifiedData.confidence,
                    LlmRawResponse = responseText,
                    ResolvedPrompt = promptTemplate  // 已替换好参数的提示词
                };
            }
        }

        /// <summary>
        /// 将简化版数据适配为旧的 UnifiedPlanResponseData 格式（保持后续日志等逻辑兼容）
        /// </summary>
        private UnifiedPlanResponseData AdaptSimplifiedToUnified(SimplifiedPlanData simplified)
        {
            return new UnifiedPlanResponseData
            {
                decision = simplified.decision,
                reason = simplified.reason,
                confidence = simplified.confidence,
                goal = simplified.goal,
                directResponse = simplified.directResponse,
                strategy = null, // 由 SimplifiedPlanConverter 推断
                steps = simplified.steps?.Select(s => new UnifiedStepData
                {
                    stepIndex = s.i,
                    stepDescription = s.desc,
                    stepType = string.IsNullOrEmpty(s.wf) ? "llm_reasoning" : "workflow_call",
                    assignedWorkflowIds = string.IsNullOrEmpty(s.wf) ? new List<string>() : new List<string> { s.wf },
                    dependsOnStepIds = s.dep ?? new List<int>(),
                    inputs = new List<UnifiedInputData>
                    {
                        new UnifiedInputData { varname = "prompt", value = s.prompt ?? "" }
                    },
                    expectedOutput = ""
                }).ToList()
            };
        }

        /// <summary>
        /// 尝试将 JSON 文本反序列化为 SimplifiedPlanData
        /// </summary>
        private SimplifiedPlanData TryDeserializeSimplified(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json)) return null;
                return Newtonsoft.Json.JsonConvert.DeserializeObject<SimplifiedPlanData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 为日志记录解析步骤输入参数中的 {output_xxx}[N] 占位符
        /// 返回包含实际解析值的 Inputs 列表（不影响原始 StepInputs）
        /// </summary>
        private List<Inputs> ResolveStepInputsForLogging(TaskStep step, TaskPlanning taskPlanning, ConcurrentQueue<string> Logs)
        {
            if (step.StepInputs == null || step.StepInputs.Count == 0)
                return step.StepInputs ?? new List<Inputs>();

            // 构建已完成步骤的 StepID → ExecutionResult 映射
            var stepOutputMap = new Dictionary<string, string>();
            foreach (var s in taskPlanning.Steps)
            {
                if (!string.IsNullOrEmpty(s.ExecutionResult))
                {
                    stepOutputMap[s.StepID] = s.ExecutionResult;
                }
            }

            var resolvedInputs = new List<Inputs>();
            foreach (var si in step.StepInputs)
            {
                var resolvedValue = si.value ?? "";

                // 匹配 {output_xxx} 以及可选的数组索引 [N]
                var placeholderPattern = new System.Text.RegularExpressions.Regex(@"\{output_([^}]+)\}(\[(\d+)\])?");

                if (!string.IsNullOrEmpty(resolvedValue) && resolvedValue.Contains("{output_"))
                {
                    resolvedValue = placeholderPattern.Replace(resolvedValue, match =>
                    {
                        string refStepId = match.Groups[1].Value;
                        int arrayIndex = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : -1;

                        if (stepOutputMap.TryGetValue(refStepId, out string output))
                        {
                            if (arrayIndex >= 0)
                            {
                                // 尝试从 JSON 数组中提取指定索引的元素
                                try
                                {
                                    var token = Newtonsoft.Json.JsonConvert.DeserializeObject(output);
                                    if (token is Newtonsoft.Json.Linq.JArray arr && arrayIndex < arr.Count)
                                    {
                                        var element = arr[arrayIndex];
                                        string extracted = element.ToString(Newtonsoft.Json.Formatting.None);
                                        return $"[数组提取 [{arrayIndex}]/[{arr.Count}]] {extracted}";
                                    }
                                }
                                catch { }
                                return $"[数组提取失败 [{arrayIndex}], 原始输出前200字] {TruncateForLog(output, 200)}";
                            }

                            // 非数组索引，返回截断后的完整输出
                            return $"[完整输出, {output.Length}字符] {TruncateForLog(output, 500)}";
                        }

                        return $"[未找到步骤 {refStepId} 的输出]";
                    });
                }

                resolvedInputs.Add(new Inputs
                {
                    varname = si.varname,
                    value = resolvedValue
                });
            }

            return resolvedInputs;
        }

        /// <summary>
        /// 截断文本用于日志输出
        /// </summary>
        private static string TruncateForLog(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + $"...(共 {text.Length} 字符)";
        }

        /// <summary>
        /// 从响应文本中提取 JSON
        /// </summary>
        /// <summary>
        /// 校验 LLM 生成的规划是否合理，返回错误列表。空列表表示校验通过。
        /// </summary>
        private List<string> ValidatePlanning(UnifiedPlanResponseData data, List<WorkflowConfigInfo> availableWorkflows)
        {
            var errors = new List<string>();

            // 1. steps 不能为空
            if (data.steps == null || data.steps.Count == 0)
            {
                errors.Add("decision=taskPlanning 但 steps 为空，必须至少包含一个步骤");
                return errors;
            }

            // 2. goal 不能为空
            if (string.IsNullOrEmpty(data.goal))
            {
                errors.Add("缺少 goal 字段，必须声明任务最终目标");
            }

            // 构建可用 Workflow ID 集合
            var validWorkflowIds = new HashSet<string>();
            if (availableWorkflows != null)
            {
                foreach (var wf in availableWorkflows)
                {
                    if (!string.IsNullOrEmpty(wf.workflowId))
                        validWorkflowIds.Add(wf.workflowId);
                }
            }

            // 收集所有有效的步骤索引
            var validStepIndices = new HashSet<int>();
            foreach (var s in data.steps)
            {
                validStepIndices.Add(s.stepIndex);
            }

            for (int i = 0; i < data.steps.Count; i++)
            {
                var step = data.steps[i];
                string prefix = $"步骤 {step.stepIndex}";

                // 3. assignedWorkflowIds 必须在可用清单中
                if (step.assignedWorkflowIds != null && validWorkflowIds.Count > 0)
                {
                    foreach (var wfId in step.assignedWorkflowIds)
                    {
                        if (!string.IsNullOrEmpty(wfId) && !validWorkflowIds.Contains(wfId))
                        {
                            errors.Add($"{prefix}: assignedWorkflowId '{wfId}' 不在可用 WorkFlow 清单中");
                        }
                    }
                }

                // 4. inputs 完整性
                if (step.inputs == null || step.inputs.Count == 0)
                {
                    errors.Add($"{prefix}: 缺少 inputs，每个步骤至少需要一个输入参数");
                }

                // 5. dependsOnStepIds 引用有效
                if (step.dependsOnStepIds != null)
                {
                    foreach (var depIndex in step.dependsOnStepIds)
                    {
                        if (!validStepIndices.Contains(depIndex))
                        {
                            errors.Add($"{prefix}: dependsOnStepIds 引用了不存在的步骤索引 {depIndex}");
                        }
                    }
                }

            }

            return errors;
        }

        private string ExtractJsonFromResponse(string response)
        {
            // 尝试直接解析
            if (response.TrimStart().StartsWith("{"))
            {
                var depth = 0;
                for (int i = 0; i < response.Length; i++)
                {
                    if (response[i] == '{') depth++;
                    if (response[i] == '}') depth--;
                    if (depth == 0) return response.Substring(0, i + 1);
                }
            }

            // 从 markdown code block 中提取
            var jsonStart = response.IndexOf("```json");
            if (jsonStart >= 0)
            {
                var jsonBody = response.Substring(jsonStart + 7);
                var jsonEnd = jsonBody.IndexOf("```");
                if (jsonEnd > 0) return jsonBody.Substring(0, jsonEnd).Trim();
            }

            // 从普通 code block 中提取
            jsonStart = response.IndexOf("```");
            if (jsonStart >= 0)
            {
                var jsonBody = response.Substring(jsonStart + 3);
                var jsonEnd = jsonBody.IndexOf("```");
                if (jsonEnd > 0) return jsonBody.Substring(0, jsonEnd).Trim();
            }

            return response;
        }

        /// <summary>
        /// 解析步骤类型
        /// </summary>
        private StepType ParseStepType(string stepType)
        {
            if (string.IsNullOrEmpty(stepType)) return StepType.WorkflowCall;
            switch (stepType.ToLower())
            {
                case "llm_reasoning": return StepType.LLMReasoning;
                case "workflow_call": return StepType.WorkflowCall;
                default: return StepType.WorkflowCall;
            }
        }

        /// <summary>
        /// 规范化步骤输入中的占位符为 {output_<StepID>} 格式
        /// 将 LLM 生成的任意占位符名称(如 {knowledge_output}, {chat_output_1})
        /// 统一替换为确定的 {output_<StepID>} 格式
        /// 规则: 从占位符中提取步骤索引数字,映射为实际 StepID
        /// </summary>
        private void NormalizeInputPlaceholders(
            List<TaskStep> steps,
            Dictionary<int, string> stepIndexToIdMap)
        {
            // 匹配 {xxx} 以及紧跟其后的可选数组索引 [N]
            // 例如: {output_1}[0] → 匹配整个 "{output_1}[0]"，placeholder="output_1"，arraySuffix="[0]"
            var placeholderPattern = new System.Text.RegularExpressions.Regex(@"\{([^}]+)\}(\[(\d+)\])?");

            foreach (var step in steps)
            {
                if (step.StepInputs == null || step.StepInputs.Count == 0) continue;

                foreach (var input in step.StepInputs)
                {
                    if (string.IsNullOrEmpty(input.value) || !input.value.Contains("{"))
                        continue;

                    input.value = placeholderPattern.Replace(input.value, match =>
                    {
                        string placeholder = match.Groups[1].Value.Trim();
                        // 提取花括号外紧跟的数组索引后缀，如 [0]
                        string outerArraySuffix = match.Groups[2].Success ? match.Groups[2].Value : "";

                        // 已经是标准格式 {output_<StepID>} 的不需要处理
                        if (placeholder.StartsWith("output_"))
                        {
                            var idPart = placeholder.Substring(7); // "output_" 长度7
                            // 去除可能已存在于 placeholder 内部的数组索引后缀
                            var innerArrayMatch = System.Text.RegularExpressions.Regex.Match(idPart, @"\[\d+\]$");
                            if (innerArrayMatch.Success)
                            {
                                idPart = idPart.Substring(0, innerArrayMatch.Index);
                            }
                            if (steps.Any(s => s.StepID == idPart))
                            {
                                // 将外部的数组索引合并到花括号内部
                                if (!string.IsNullOrEmpty(outerArraySuffix))
                                    return $"{{output_{idPart}{outerArraySuffix}}}";
                                return match.Value; // 已经是正确的格式
                            }
                        }

                        // 尝试从占位符中提取步骤索引数字
                        var numberMatch = System.Text.RegularExpressions.Regex.Match(placeholder, @"(\d+)");
                        if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out int stepIdx))
                        {
                            if (stepIndexToIdMap.ContainsKey(stepIdx))
                            {
                                // 保留数组索引后缀 (如 [0], [1])，LLM 可能使用 {output_1[0]} 引用数组中的特定元素
                                string innerSuffix = placeholder.Substring(numberMatch.Index + numberMatch.Length);
                                // 合并内部和外部数组索引（优先使用内部，若无则使用外部）
                                string finalSuffix = !string.IsNullOrEmpty(innerSuffix) ? innerSuffix : outerArraySuffix;
                                return $"{{output_{stepIndexToIdMap[stepIdx]}{finalSuffix}}}";
                            }
                        }

                        // 无数字的占位符: 如果该步骤只有一个前置步骤,替换为该步骤的 output
                        if (step.DependsOnStepIds != null && step.DependsOnStepIds.Count == 1)
                        {
                            if (!string.IsNullOrEmpty(outerArraySuffix))
                                return $"{{output_{step.DependsOnStepIds[0]}{outerArraySuffix}}}";
                            return $"{{output_{step.DependsOnStepIds[0]}}}";
                        }

                        // 无法确定,保持原样
                        return match.Value;
                    });
                }
            }
        }

        /// <summary>
        /// 检测并替换 StepInputs 中的内联嵌入文本。
        /// LLM 可能将前置步骤的 ExpectedOutput 直接粘贴到下游步骤的 input.value 中，
        /// 而不是使用 {output_<StepID>} 占位符。此方法检测此类嵌入并替换为正确的占位符。
        /// </summary>
        private void DetectAndReplaceInlineEmbeddings(List<TaskStep> steps)
        {
            const int MIN_EMBED_LENGTH = 50;

            // Phase 1: 构建前置步骤的参考文本库（StepID → ExpectedOutput）
            var references = new Dictionary<string, (string text, int index)>();
            foreach (var step in steps)
            {
                if (!string.IsNullOrEmpty(step.ExpectedOutput) && step.ExpectedOutput.Length >= MIN_EMBED_LENGTH)
                {
                    references[step.StepID] = (step.ExpectedOutput, step.StepIndex);
                }
            }

            if (references.Count == 0) return;

            // Phase 2: 检查每个下游步骤的 StepInputs
            foreach (var step in steps)
            {
                if (step.StepInputs == null || step.StepInputs.Count == 0) continue;
                if (step.DependsOnStepIds == null || step.DependsOnStepIds.Count == 0) continue;

                foreach (var input in step.StepInputs)
                {
                    if (string.IsNullOrEmpty(input.value) || input.value.Length < MIN_EMBED_LENGTH)
                        continue;

                    // 已包含 {output_ 占位符的跳过（已被 NormalizeInputPlaceholders 处理）
                    if (input.value.Contains("{output_"))
                        continue;

                    foreach (var depStepId in step.DependsOnStepIds)
                    {
                        if (!references.ContainsKey(depStepId)) continue;

                        var (refText, refIndex) = references[depStepId];
                        var (isEmbed, prefix, suffix) = CheckInlineEmbedding(input.value, refText);

                        if (isEmbed)
                        {
                            var oldValue = input.value;
                            string placeholder;

                            // 尝试检测嵌入的是 JSON 数组中的某个元素
                            string arrayIndexSuffix = TryDetectArrayElementIndex(input.value, refText);
                            placeholder = $"{{output_{depStepId}{arrayIndexSuffix}}}";

                            var sb = new StringBuilder();
                            if (!string.IsNullOrWhiteSpace(prefix))
                                sb.AppendLine(prefix.Trim());
                            sb.AppendLine(placeholder);
                            if (!string.IsNullOrWhiteSpace(suffix))
                                sb.AppendLine(suffix.Trim());

                            input.value = sb.ToString().Trim();

                            LoggerHelper.LogWarning(_logger, ClawLogModules.CLAW,
                                $" 步骤 {step.StepIndex} 的输入 [{input.varname}] 检测到内联嵌入" +
                                $" (前置步骤 {refIndex} 的预期输出, {refText.Length} 字符)。" +
                                $" 已替换为占位符 {placeholder}。" +
                                $" 原长度: {oldValue.Length} → 新长度: {input.value.Length}");
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查 inputValue 是否嵌入了 referenceText（仅基于子串精确匹配）
        /// 注意：不使用模糊匹配，避免不同语义但词汇重叠的文本被误判
        /// </summary>
        private (bool isEmbed, string prefix, string suffix) CheckInlineEmbedding(
            string inputValue, string referenceText)
        {
            if (string.IsNullOrEmpty(inputValue) || string.IsNullOrEmpty(referenceText))
                return (false, null, null);

            string normalizedInput = NormalizeForComparison(inputValue);
            string normalizedRef = NormalizeForComparison(referenceText);

            // Case 1: 精确匹配
            if (string.Equals(normalizedInput, normalizedRef, StringComparison.OrdinalIgnoreCase))
                return (true, "", "");

            // Case 2: 参考文本是输入的前缀（最常见的模式: LLM粘贴完整输出 + 追加指令）
            if (normalizedInput.StartsWith(normalizedRef, StringComparison.OrdinalIgnoreCase))
            {
                int estEnd = EstimateOriginalIndex(inputValue, normalizedRef.Length);
                string prefix = "";
                string suffix = estEnd < inputValue.Length ? inputValue.Substring(estEnd) : "";
                return (true, prefix, suffix);
            }

            // Case 3: 参考文本出现在输入中间
            int matchPos = normalizedInput.IndexOf(normalizedRef, StringComparison.OrdinalIgnoreCase);
            if (matchPos >= 0)
            {
                int estStart = EstimateOriginalIndex(inputValue, matchPos);
                int estEnd = EstimateOriginalIndex(inputValue, matchPos + normalizedRef.Length);
                string prefix = estStart > 0 ? inputValue.Substring(0, estStart) : "";
                string suffix = estEnd < inputValue.Length ? inputValue.Substring(estEnd) : "";
                return (true, prefix, suffix);
            }

            return (false, null, null);
        }

        /// <summary>
        /// 文本标准化用于比较：去除空白差异、大小写差异
        /// </summary>
        private string NormalizeForComparison(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length);
            bool lastWasSpace = false;
            foreach (char c in text.Trim())
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace) { sb.Append(' '); lastWasSpace = true; }
                }
                else
                {
                    sb.Append(char.ToLowerInvariant(c));
                    lastWasSpace = false;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 估算标准化文本位置对应的原始文本位置
        /// </summary>
        private int EstimateOriginalIndex(string original, int normalizedIndex)
        {
            if (string.IsNullOrEmpty(original)) return 0;
            string normalized = NormalizeForComparison(original);
            if (normalized.Length == 0) return 0;
            double ratio = (double)original.Length / normalized.Length;
            int estIndex = (int)(normalizedIndex * ratio);
            return Math.Min(estIndex, original.Length);
        }

        /// <summary>
        /// 尝试检测嵌入文本对应的是 JSON 数组中的哪个元素。
        /// 返回 "[N]" 格式的后缀，如果无法确定则返回空字符串。
        /// </summary>
        private string TryDetectArrayElementIndex(string inputValue, string referenceText)
        {
            try
            {
                // 尝试将参考文本解析为 JSON 数组
                var token = JsonConvert.DeserializeObject(referenceText);
                if (!(token is Newtonsoft.Json.Linq.JArray arr))
                    return "";

                string normalizedInput = NormalizeForComparison(inputValue);

                // 遍历数组元素，检查 inputValue 是否包含某个元素
                int bestMatch = -1;
                int bestLength = 0;
                for (int i = 0; i < arr.Count; i++)
                {
                    string elementText = arr[i].ToString(Formatting.None);
                    string normalizedElement = NormalizeForComparison(elementText);

                    if (normalizedElement.Length > 0 && normalizedInput.Contains(normalizedElement))
                    {
                        // 选择最长的匹配（避免短元素误匹配）
                        if (normalizedElement.Length > bestLength)
                        {
                            bestMatch = i;
                            bestLength = normalizedElement.Length;
                        }
                    }
                }

                if (bestMatch >= 0)
                {
                    return $"[{bestMatch}]";
                }
            }
            catch
            {
                // 解析失败，忽略
            }

            return "";
        }

        /// <summary>
        /// 触发下游节点 (P3优化: 抽取共用逻辑)
        /// </summary>
        private void TriggerDownstreamNodes(
            NodeConfig config, List<Output> outputs,
            string AppID, string SessionID, string ProcessesID,
            string TaskID, string FromMainTaskID, ConcurrentQueue<string> Logs)
        {
            List<WorkflowEdgeInfo> edgeList = WorkflowEdgeInfoBussiness.GetListBySourceNodeId(config.id);
            if (edgeList == null || edgeList.Count == 0) return;

            var targetNodeIds = new List<string>();
            foreach (var edge in edgeList)
            {
                var cfg = edge.Config as Newtonsoft.Json.Linq.JObject
                    ?? Newtonsoft.Json.Linq.JObject.FromObject(edge.Config);
                if ((string?)cfg["sourceHandle"] == "output_to_next")
                {
                    targetNodeIds.Add(edge.TargetNodeId);
                }
            }

            if (targetNodeIds.Count == 0) return;

            var targetNodes = WorkflowNodeInfoBussiness.GetListByNodeID(
                string.Join(",", targetNodeIds.Select(id => $"'{id}'")));

            if (targetNodes == null) return;

            foreach (var node in targetNodes)
            {
                NodeConfig targetNode = new NodeConfig()
                {
                    id = node.NodeID,
                    mainid = config.mainid,
                    workflowid = node.WorkflowID,
                    type = node.NodeType,
                    data = node.Config
                };

                string newTaskID = TaskInfoBussiness.toTask(
                    config, outputs, targetNode, AppID, SessionID,
                    ProcessesID, TaskID, FromMainTaskID, "");

                Logs.Enqueue($"触发下游节点: {newTaskID}");
            }
        }

        /// <summary>
        /// 异步恢复：子 WorkFlow 完成后回调恢复 ClawAI 执行
        /// 从保存的 ClawAIStepContext 中恢复状态，继续执行剩余步骤
        /// </summary>
        public async Task<string> ContinueFromStepAsync(
            string asyncTaskID,
            string stepResult,
            Dictionary<string, string> allStepResults)
        {
            ConcurrentQueue<string> Logs = new ConcurrentQueue<string>();
            List<Output> outputs = new List<Output>();
            ExecutionRecordStatus ExecutionRecordStatus = ExecutionRecordStatus.Success;
            ClawAIExecutionLogger resumeLogger = null;

            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.CLAW,
                    $"[ContinueFromStep] 开始恢复 - AsyncTaskID: {asyncTaskID}");

                // 1. 从 TaskInfo 恢复上下文
                var asyncTaskInfo = TaskInfoBussiness.GetModel(asyncTaskID);
                if (asyncTaskInfo == null)
                {
                    throw new Exception($"异步任务不存在: {asyncTaskID}");
                }

                // 状态检查：仅排除已明确失败的任务（TryResumeClawAIStep 已做前置验证，
                // 但 Task.Run 异步执行期间状态可能被 GetList 等机制改变，因此放宽检查）
                if (asyncTaskInfo.State == TaskState.Failure)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.CLAW,
                        $"[ContinueFromStep] 任务已失败 - TaskID: {asyncTaskID}, State: {asyncTaskInfo.State}");
                    return asyncTaskID;
                }

                // 类型检查：仅 ClawAIWorkflowStep 类型可恢复
                if (asyncTaskInfo.TaskType != NodeType.ClawAIWorkflowStep)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.CLAW,
                        $"[ContinueFromStep] 任务类型不匹配 - TaskID: {asyncTaskID}, Type: {asyncTaskInfo.TaskType}");
                    return asyncTaskID;
                }

                var context = JsonConvert.DeserializeObject<ClawAIStepContext>(
                    JsonConvert.SerializeObject(asyncTaskInfo.TaskConfig.NotNodeConfig));
                if (context == null)
                {
                    throw new Exception("步骤上下文恢复失败");
                }

                // 2. 重建配置对象
                var config = JsonConvert.DeserializeObject<NodeConfig>(context.NodeConfigJson);
                var nodeData = JsonConvert.DeserializeObject<ClawAIData>(context.NodeDataJson);
                var taskPlanning = JsonConvert.DeserializeObject<TaskPlanning>(context.TaskPlanningJson);
                var inputs = JsonConvert.DeserializeObject<List<Inputs>>(context.InputsJson);
                var prevLogs = JsonConvert.DeserializeObject<List<string>>(context.LogsJson) ?? new List<string>();
                var completedResults = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    context.CompletedStepResultsJson) ?? new Dictionary<string, string>();

                // 合入本次完成的步骤结果
                foreach (var kv in allStepResults)
                {
                    completedResults[kv.Key] = kv.Value;
                }

                // 并行层恢复时，更新所有并行步骤的状态
                foreach (var kv in allStepResults)
                {
                    if (kv.Key == context.TriggeredStepId) continue; // 已在上方处理
                    var parallelStep = taskPlanning.Steps.FirstOrDefault(s => s.StepID == kv.Key);
                    if (parallelStep != null && parallelStep.StepStatus != StepStatus.Completed)
                    {
                        bool isFailed = !string.IsNullOrEmpty(kv.Value) && kv.Value.StartsWith("__FAILED__");
                        parallelStep.StepStatus = isFailed ? StepStatus.Failed : StepStatus.Completed;
                        parallelStep.ExecutionResult = kv.Value;
                        parallelStep.ActualOutput = kv.Value;
                        parallelStep.EndTime = DateTime.Now;
                    }
                }

                string AppID = context.AppID;
                string SessionID = context.SessionID;
                string ProcessesID = context.ProcessesID;
                string TaskID = context.TaskID;
                string MemberID = context.MemberID;
                string FromMainTaskID = context.FromMainTaskID;
                string originalTask = context.OriginalTask;

                foreach (var log in prevLogs) { Logs.Enqueue(log); }
                Logs.Enqueue($"\n=== [异步恢复] 子 WorkFlow 完成，恢复 ClawAI 执行 ===");
                Logs.Enqueue($"\n[异步恢复] 恢复步骤: {context.TriggeredStepIndex}, 结果长度: {stepResult?.Length ?? 0}");

                // 创建专用日志记录器（追加到同一天目录下的日志文件）
                try { resumeLogger = new ClawAIExecutionLogger(TaskID); resumeLogger.LogRaw($"\n[异步恢复] 恢复步骤: {context.TriggeredStepIndex}"); } catch { }

                // 3. 更新步骤状态（处理 __FAILED__ 前缀的超时/错误结果）
                var triggeredStep = taskPlanning.Steps.FirstOrDefault(s => s.StepID == context.TriggeredStepId);
                if (triggeredStep != null)
                {
                    // 优先使用 allStepResults 中该步骤的独立结果，避免并行恢复时 stepResult 为 mergedResult
                    string triggeredStepResult = allStepResults != null && allStepResults.ContainsKey(context.TriggeredStepId)
                        ? allStepResults[context.TriggeredStepId]
                        : stepResult;
                    bool isFailed = !string.IsNullOrEmpty(triggeredStepResult) && triggeredStepResult.StartsWith("__FAILED__");
                    triggeredStep.StepStatus = isFailed ? StepStatus.Failed : StepStatus.Completed;
                    triggeredStep.ExecutionResult = triggeredStepResult;
                    triggeredStep.ActualOutput = triggeredStepResult;
                    triggeredStep.EndTime = DateTime.Now;

                    if (isFailed)
                    {
                        Logs.Enqueue($"\n[异步恢复] 步骤 {context.TriggeredStepIndex} 执行失败: {triggeredStepResult}");
                    }
                }

                // 更新已完成的结果到后续步骤可用的上下文
                foreach (var step in taskPlanning.Steps)
                {
                    if (completedResults.ContainsKey(step.StepID) && step.StepStatus == StepStatus.Pending)
                    {
                        // 后续步骤可通过 {output_<StepID>} 引用已完成步骤的结果
                    }
                }

                // 4. 重建 RecordID 和流式输出
                string RecordID;
                if (!string.IsNullOrEmpty(context.RecordID))
                {
                    RecordID = context.RecordID;
                }
                else
                {
                    // 异步恢复场景：查找原始 ClawAI 节点的执行记录（状态为 Running）
                    // DAL 层 NodeID 参数用于 IN 子句，需要加单引号
                    string quotedNodeId = $"'{config.id}'";
                    var runningRecords = ZSN.AI.BLL.WorkflowNodeExecutionRecordInfoBussiness
                        .GetListByNodeId(SessionID, quotedNodeId, ExecutionRecordStatus.Running, ProcessesID);
                    RecordID = runningRecords?.FirstOrDefault()?.RecordID;
                    if (!string.IsNullOrEmpty(RecordID))
                    {
                        Logs.Enqueue($"\n[异步恢复] 复用原始执行记录: {RecordID}");
                    }
                    else
                    {
                        // 兜底：未找到 Running 记录时创建新记录
                        RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(
                            SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);
                        Logs.Enqueue($"\n[异步恢复] 未找到原始记录，创建新执行记录: {RecordID}");
                    }
                }

                string streamKey = StreamKey.Build(SessionID, ProcessesID);
                using var batchWriter = new StreamBatchWriter(
                    _streamSync, streamKey, SessionID, ProcessesID, TaskID, config.id, intervalMs: 200);
                using var throttler = new RecordUpdateThrottler(
                    RecordID, outputs, Logs,
                    (rid, status, outs, logs) => ZSN.AI.Node.Utils.Utils.updateExcutionRecord(rid, status, outs, logs),
                    intervalMs: 500);

                var progress = new Progress<string>(delta => { batchWriter.Append(delta); });

                var resumeMsg = $"\n[异步恢复] 步骤 {context.TriggeredStepIndex} 已完成，继续执行剩余步骤";
                Logs.Enqueue(resumeMsg);
                batchWriter.Append(resumeMsg);
                throttler.MarkDirty();

                // 5. 恢复模型配置
                var modelInitResult = _modelInitializer.InitializeAllModels(nodeData);

                // 6. 继续执行剩余步骤 (✅ 修改: 循环执行直到所有步骤完成或再次异步触发)
                bool hasMoreSteps = true;
                int resumeIteration = 0;
                const int maxResumeIterations = 10; // 防止无限循环
                ExecutionResult executionResult = null;

                while (hasMoreSteps && resumeIteration < maxResumeIterations)
                {
                    resumeIteration++;
                    Logs.Enqueue($"\n[异步恢复] 恢复迭代 {resumeIteration}/{maxResumeIterations} - 执行剩余步骤");
                    batchWriter.Append($"\n[异步恢复] 恢复迭代 {resumeIteration}...\n");
                    throttler.MarkDirty();
                    
                    executionResult = await _agentOrchestrationService.ExecuteStepsAsync(
                        config, taskPlanning,
                        AppID, SessionID, ProcessesID, TaskID,
                        originalTask, inputs, nodeData,
                        modelInitResult.ReflectionModelConfig,
                        Logs, progress, resumeLogger, MemberID, FromMainTaskID);

                    // 检查是否还有待执行步骤
                    var pendingSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Pending);
                    var executingSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Executing);
                    var completedSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Completed);
                    var failedSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Failed);
                    
                    Logs.Enqueue($"\n[异步恢复] 执行结果 - 完成: {executionResult.CompletedSteps}, " +
                                 $"失败: {executionResult.FailedSteps}, 待执行: {pendingSteps}, 执行中: {executingSteps}");
                    Logs.Enqueue($"\n[异步恢复] 步骤状态统计 - 已完成: {completedSteps}, 失败: {failedSteps}, " +
                                 $"待执行: {pendingSteps}, 执行中: {executingSteps}, 总数: {taskPlanning.TotalSteps}");

                    // 如果有步骤被异步触发，保存上下文并退出
                    if (executionResult.IsAsyncTriggered)
                    {
                        Logs.Enqueue("\n[异步恢复] 检测到新的异步触发，等待子 WorkFlow 回调恢复");
                        batchWriter.Append("\n[异步恢复] 检测到新的异步触发，等待回调...\n");
                        throttler.MarkDirty();

                        // ✅ 更新异步等待任务状态为 Completed（本次回调已完成）
                        asyncTaskInfo.State = TaskState.Completed;
                        asyncTaskInfo.UpdateTime = DateTime.Now;
                        TaskInfoBussiness.Update(asyncTaskInfo);
                        
                        return asyncTaskID;
                    }

                    // 检查是否所有步骤都已完成
                    if (pendingSteps == 0 && executingSteps == 0)
                    {
                        Logs.Enqueue($"\n[异步恢复] 所有步骤已完成，退出恢复循环 (迭代: {resumeIteration})");
                        batchWriter.Append($"\n[异步恢复] 所有步骤已完成\n");
                        throttler.MarkDirty();
                        hasMoreSteps = false;
                        break;
                    }

                    // 如果有失败步骤且不允许继续，退出循环
                    if (executionResult.FailedSteps > 0 && !nodeData.workFlowLoopConfig.continueOnWorkFlowFailure)
                    {
                        Logs.Enqueue($"\n[异步恢复] 有 {executionResult.FailedSteps} 个步骤失败，中断执行");
                        batchWriter.Append($"\n[异步恢复] 步骤失败，中断执行\n");
                        throttler.MarkDirty();
                        hasMoreSteps = false;
                        break;
                    }

                    // 如果没有新的完成步骤，可能陷入死循环，退出
                    if (executionResult.CompletedSteps == 0 && executionResult.FailedSteps == 0)
                    {
                        Logs.Enqueue($"\n[异步恢复] 警告: 本轮无步骤完成，可能存在依赖死锁，退出循环");
                        batchWriter.Append($"\n[异步恢复] 警告: 无进展，退出循环\n");
                        throttler.MarkDirty();
                        hasMoreSteps = false;
                        break;
                    }
                }

                if (resumeIteration >= maxResumeIterations)
                {
                    Logs.Enqueue($"\n[异步恢复] 警告: 达到最大恢复迭代次数 ({maxResumeIterations})，强制退出");
                    batchWriter.Append($"\n[异步恢复] 达到最大迭代次数\n");
                    throttler.MarkDirty();
                }

                // 如果 executionResult 为 null (循环未执行)，创建默认结果
                if (executionResult == null)
                {
                    executionResult = new ExecutionResult
                    {
                        CompletedSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Completed),
                        FailedSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Failed)
                    };
                }

                // 7. 反思评估 + 是否继续循环（只有所有步骤同步完成时才走这条路径）
                bool needMoreIterations = false;
                if (nodeData.reflectionConfig.enabled && executionResult.FailedSteps == 0)
                {
                    var reflectionResult = await _reflectionService.ReflectOnExecutionAsync(
                        nodeData,
                        modelInitResult.ReflectionModelConfig,
                        taskPlanning,
                        executionResult,
                        originalTask,
                        context.CurrentIteration,
                        progress
                    );

                    needMoreIterations = reflectionResult.Action == ReflectionAction.ContinueExecution
                        && context.CurrentIteration < context.MaxIterations;

                    Logs.Enqueue($"\n[异步恢复] 反思评分: {reflectionResult.OverallQuality}/100, " +
                             $"动作: {reflectionResult.Action}, 需要继续迭代: {needMoreIterations}");
                }

                // 检查是否还有未完成步骤
                bool hasPendingSteps = taskPlanning.Steps.Any(s =>
                    s.StepStatus == StepStatus.Pending || s.StepStatus == StepStatus.Executing);

                if (hasPendingSteps || needMoreIterations)
                {
                    // 还有剩余步骤或需要更多迭代 → 再次执行循环
                    if (needMoreIterations)
                    {
                        // 重新规划并执行新一轮
                        executionResult = await _agentOrchestrationService.ExecuteStepsAsync(
                            config, taskPlanning,
                            AppID, SessionID, ProcessesID, TaskID,
                            originalTask, inputs, nodeData,
                            modelInitResult.ReflectionModelConfig,
                            Logs, progress, resumeLogger, MemberID, FromMainTaskID);
                    }
                }

                // 8. 所有步骤完成 → 生成最终结果并触发下游
                await FinalizeExecutionAsync(
                    config, taskPlanning, nodeData, inputs,
                    AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, MemberID,
                    originalTask, modelInitResult, executionResult,
                    outputs, Logs, progress, batchWriter, throttler, RecordID);

                ExecutionRecordStatus = ExecutionRecordStatus.Success;

                // 更新异步等待任务状态为 Completed，避免 ClawAIStepTimeoutJob 重复捡起
                asyncTaskInfo.State = TaskState.Completed;
                asyncTaskInfo.UpdateTime = DateTime.Now;
                TaskInfoBussiness.Update(asyncTaskInfo);
                
                // ✅ 修复: 更新所有相关的 ClawAIWorkflowStep 任务状态为 Completed
                // 这包括初始异步任务和所有中间层的并行任务
                try
                {
                    string strWhere = $" TaskType={((int)NodeType.ClawAIWorkflowStep)} " +
                                      $"AND State IN (0, 1) " +  // Waiting 或 Processing
                                      $"AND ProcessesID LIKE '{ProcessesID}%'";  // 同一个 ProcessesID 下的所有异步任务
                    
                    var relatedAsyncTasks = TaskInfoBussiness.GetList(strWhere);
                    int updatedCount = 0;
                    foreach (var task in relatedAsyncTasks)
                    {
                        task.State = TaskState.Completed;
                        task.UpdateTime = DateTime.Now;
                        TaskInfoBussiness.Update(task);
                        updatedCount++;
                    }
                }
                catch (Exception updateEx)
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.CLAW,
                        $"[ContinueFromStep] 更新相关异步任务失败 - ProcessesID: {ProcessesID}", updateEx);
                }

                // 释放恢复日志记录器
                try { resumeLogger?.LogRaw($"\n[异步恢复] 执行完成"); resumeLogger?.Dispose(); } catch { }
            }
            catch (Exception ex)
            {
                try { resumeLogger?.LogRaw($"\n✗ 恢复执行失败: {ex.Message}"); resumeLogger?.Dispose(); } catch { }
                LoggerHelper.LogError(_logger, ClawLogModules.CLAW,
                    $"[ContinueFromStep] 恢复执行失败 - AsyncTaskID: {asyncTaskID}", ex);
                Logs.Enqueue($"\n✗ 恢复执行失败: {ex.Message}");
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;

                // 标记异步任务失败
                try
                {
                    var failedTask = TaskInfoBussiness.GetModel(asyncTaskID);
                    if (failedTask != null && failedTask.State != TaskState.Completed)
                    {
                        failedTask.State = TaskState.Failure;
                        failedTask.UpdateTime = DateTime.Now;
                        TaskInfoBussiness.Update(failedTask);
                    }
                }
                catch { /* 忽略状态更新失败 */ }
            }

            return asyncTaskID;
        }

        /// <summary>
        /// 最终化执行：生成输出、触发下游节点
        /// </summary>
        private async Task FinalizeExecutionAsync(
            NodeConfig config,
            TaskPlanning taskPlanning,
            ClawAIData nodeData,
            List<Inputs> inputs,
            string AppID, string SessionID, string ProcessesID,
            string TaskID, string FromMainTaskID, string MemberID,
            string originalTask,
            ModelInitializationResult modelInitResult,
            ExecutionResult executionResult,
            List<Output> outputs,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress,
            StreamBatchWriter batchWriter,
            RecordUpdateThrottler throttler,
            string RecordID)
        {
            // 生成最终结果 - 只输出终端步骤（未被其他步骤作为依赖的步骤）的结果
            // 避免中间步骤和下游步骤的内容重复
            var terminalSteps = taskPlanning.Steps
                .Where(s => s.StepStatus == StepStatus.Completed && !string.IsNullOrEmpty(s.ExecutionResult))
                .Where(s => !taskPlanning.Steps.Any(other =>
                    other.DependsOnStepIds != null && other.DependsOnStepIds.Contains(s.StepID)))
                .ToList();

            // 如果没有终端步骤（所有步骤都是中间步骤的边界情况），回退到全部输出
            if (terminalSteps.Count == 0)
            {
                terminalSteps = taskPlanning.Steps
                    .Where(s => s.StepStatus == StepStatus.Completed && !string.IsNullOrEmpty(s.ExecutionResult))
                    .ToList();
            }

            string finalResult = string.Join("\n\n", terminalSteps.Select(s => s.ExecutionResult));

            if (string.IsNullOrEmpty(finalResult))
            {
                finalResult = "执行完成，但未生成有效结果";
            }

            var outputMsg = $"\n=== [异步恢复] 输出最终结果 ===";
            Logs.Enqueue(outputMsg);
            batchWriter.Append(outputMsg);
            throttler.MarkDirty();

            outputs.Add(new Output
            {
                varname = "results",
                value = finalResult,
                nodeId = config.id,
                sourceId = $"{config.id}_results"
            });

            outputs.Add(new Output
            {
                varname = "taskPlanning",
                value = JsonConvert.SerializeObject(taskPlanning),
                nodeId = config.id,
                sourceId = $"{config.id}_taskPlanning"
            });

            outputs.Add(new Output
            {
                varname = "totalSteps",
                value = taskPlanning.TotalSteps.ToString(),
                nodeId = config.id,
                sourceId = $"{config.id}_totalSteps"
            });

            outputs.Add(new Output
            {
                varname = "completedSteps",
                value = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Completed).ToString(),
                nodeId = config.id,
                sourceId = $"{config.id}_completedSteps"
            });

            outputs.Add(new Output
            {
                varname = "iterations",
                value = "1",
                nodeId = config.id,
                sourceId = $"{config.id}_iterations"
            });

            outputs.Add(new Output
            {
                varname = "planningStatus",
                value = taskPlanning.PlanningStatus.ToString(),
                nodeId = config.id,
                sourceId = $"{config.id}_planningStatus"
            });

            // 触发下游节点
            TriggerDownstreamNodes(config, outputs, AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, Logs);

            var doneMsg = "=== [异步恢复] 执行完成 ===";
            Logs.Enqueue(doneMsg);
            batchWriter.Append(doneMsg);
            throttler.MarkDirty();

            throttler.FlushWithStatus(ExecutionRecordStatus.Success);

            LoggerHelper.LogInfo(_logger, ClawLogModules.EXECUTION,
                $"\n[异步恢复] 完成 - SessionID: {SessionID}, 状态: {taskPlanning.PlanningStatus}");

            await Task.CompletedTask;
        }

        /// <summary>
        /// 统一规划响应数据结构
        /// </summary>
        private class UnifiedPlanResponseData
        {
            public string decision { get; set; }
            public string reason { get; set; }
            public int confidence { get; set; }
            public string directResponse { get; set; }
            public string strategy { get; set; }
            public string goal { get; set; }
            public List<UnifiedStepData> steps { get; set; }
        }

        private class UnifiedStepData
        {
            public int stepIndex { get; set; }
            public string stepDescription { get; set; }
            public string stepType { get; set; }
            public List<string> assignedWorkflowIds { get; set; }
            public List<int> dependsOnStepIds { get; set; }
            public List<UnifiedInputData> inputs { get; set; }
            public string expectedOutput { get; set; }
        }

        private class UnifiedInputData
        {
            public string varname { get; set; }
            public string value { get; set; }
        }

        #endregion
    }
}
