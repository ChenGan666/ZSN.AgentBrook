using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Exceptions;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.Service.Helpers;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// 任务规划服务实现
    /// </summary>
    public class TaskPlanningService : ITaskPlanningService
    {
        private readonly IChatService _chatService;
        private readonly ILogger<TaskPlanningService> _logger;
        private readonly IResultParserService _resultParserService;
        private readonly ClawAIOptions _options;

        public TaskPlanningService(
            IChatService chatService,
            ILogger<TaskPlanningService> logger,
            IResultParserService resultParserService,
            IOptions<ClawAIOptions> options)
        {
            _chatService = chatService;
            _logger = logger;
            _resultParserService = resultParserService;
            _options = options?.Value ?? new ClawAIOptions();
        }

        public async Task<TaskPlanning> CreatePlanningAsync(
            ClawAIData nodeData,
            LargeModelConfig planningModelConfig,
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            MemoryContext memoryContext,
            string AppID,
            string SessionID,
            string MemberID,
            string NodeID,
            string ProcessesID,
            IProgress<string> progress)
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 开始创建规划 - Task: {originalTask}");
            
            try
            {
                // 分析任务复杂度并动态调整最大步骤数
                var complexity = AnalyzeTaskComplexity(originalTask, availableWorkflows);
                int dynamicMaxSteps = GetDynamicMaxSteps(complexity, nodeData.taskPlanningConfig.maxSteps);
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 任务复杂度: {complexity}, 动态最大步骤数: {dynamicMaxSteps}");
                
                // ===== 优化1: 问候语直接创建简单计划,不调用LLM规划 =====
                if (complexity == TaskComplexity.Greeting)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, " 检测到问候语/简单对话,跳过LLM规划,使用LLM直接回答");
                    var greetingPlan = CreateSimplePlan(originalTask, availableWorkflows, AppID, SessionID, MemberID, NodeID, ProcessesID);
                    await SavePlanningAsync(greetingPlan);
                    return greetingPlan;
                }
                
                // ===== 优化2: 基于记忆的快速响应路径 =====
                // 检查是否可以从记忆中直接回答,避免调用WorkFlow
                var memoryBasedPlan = TryCreateMemoryBasedPlan(
                    originalTask, memoryContext, availableWorkflows, complexity, 
                    AppID, SessionID, MemberID, NodeID, ProcessesID);
                
                if (memoryBasedPlan != null)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 基于记忆快速响应 - 跳过LLM规划,使用{memoryBasedPlan.Metadata.Strategy}策略");
                    await SavePlanningAsync(memoryBasedPlan);
                    return memoryBasedPlan;
                }
                
                // 使用传入的已初始化的规划模型配置
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 使用模型: {planningModelConfig.Model.ModelName} (ID: {planningModelConfig.Id})");
                
                // 1. 构建规划提示词
                string planningPrompt = await BuildPlanningPromptAsync(
                    originalTask, availableWorkflows, memoryContext, nodeData, AppID, MemberID, dynamicMaxSteps, complexity);

                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 规划提示词长度: {planningPrompt.Length} 字符");
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 完整提示词内容:\n{planningPrompt}");

                // 2. 调用 LLM 生成规划(流式输出)
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("你是一个专业的任务规划助手,擅长将复杂任务分解为可执行的步骤。");
                chatHistory.AddUserMessage(planningPrompt);

                var responseBuilder = new System.Text.StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    planningModelConfig, 
                    chatHistory,
                    Function: null,
                    responseFormat: "text",
                    enableStreamingObservation: true,
                    progress: progress,
                    ct: System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }
                var response = responseBuilder.ToString();

                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" LLM 响应长度: {response?.Length ?? 0} 字符");
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" LLM 完整响应:\n{response}");

                // 3. 解析规划结果
                var planning = ParsePlanningResponse(
                    response, originalTask, availableWorkflows, AppID, SessionID, MemberID, NodeID, ProcessesID);

                // 4. 验证和优化规划(使用动态步骤数)
                ValidateAndOptimizePlanning(planning, availableWorkflows, nodeData, dynamicMaxSteps);

                LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                    $"[TaskPlanning] 规划创建成功 - Steps: {planning.TotalSteps}, Strategy: {planning.Metadata.Strategy}");

                // 5. 保存到数据库
                await SavePlanningAsync(planning);

                return planning;
            }
            catch (LLMException llmEx) when (llmEx.IsFatal)
            {
                // 致命 LLM 错误（403/欠费等）：不降级为简单计划——
                // 降级产生的 workflow_call 仍会调用同一个坏掉的 LLM，必然失败并卡死。
                LoggerHelper.LogError(_logger, ClawLogModules.TASK_PLANNING,
                    $" 规划创建失败（LLM 致命错误），终止: {llmEx.Message}", llmEx);
                throw;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.TASK_PLANNING, " 规划创建失败,回退到简单计划");
                
                // 失败时回退到简单计划
                var planning = CreateSimplePlan(originalTask, availableWorkflows, AppID, SessionID, MemberID, NodeID, ProcessesID);
                await SavePlanningAsync(planning);
                return planning;
            }
        }

        public TaskPlanning CreateSimplePlan(
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            string AppID,
            string SessionID,
            string MemberID,
            string NodeID,
            string ProcessesID)
        {
            var planning = new TaskPlanning
            {
                AppID = AppID,
                SessionID = SessionID,
                MemberID = MemberID,
                NodeID = NodeID,
                ProcessesID = ProcessesID,
                OriginalTask = originalTask,
                PlanningStatus = PlanningStatus.Planning,
                TotalSteps = 1,
                Metadata = new PlanningMetadata
                {
                    Strategy = "sequential",
                    Confidence = 80,
                    EstimatedDuration = 60
                }
            };

            // ===== 新增: 判断是否为问候语/简单对话 =====
            var complexity = AnalyzeTaskComplexity(originalTask, availableWorkflows);
            bool isGreeting = complexity == TaskComplexity.Greeting;
            
            // 创建单个步骤
            var step = new TaskStep
            {
                PlanningID = planning.PlanningID,
                StepIndex = 1,
                StepDescription = isGreeting ? "直接回答用户" : "执行任务",
                // 问候语使用LLMReasoning,不调用WorkFlow
                StepType = isGreeting ? StepType.LLMReasoning : 
                           (availableWorkflows.Count > 0 ? StepType.WorkflowCall : StepType.LLMReasoning),
                // 问候语不分配WorkFlow
                AssignedWorkflowIds = isGreeting ? new List<string>() : 
                                     (availableWorkflows.Count > 0 ? new List<string> { availableWorkflows[0].workflowId } : new List<string>()),
                ExpectedOutput = isGreeting ? "友好的回复" : "任务执行结果",
                StepStatus = StepStatus.Pending
            };

            planning.Steps.Add(step);
            
            LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $"创建简单计划 - 任务类型: {(isGreeting ? "问候语" : "普通任务")}, 步骤类型: {step.StepType}");

            return planning;
        }

        public async Task<TaskPlanning> ReplanAsync(
            ClawAIData nodeData,
            LargeModelConfig planningModelConfig,
            TaskPlanning currentPlanning,
            ExecutionResult executionResult,
            ReflectionResult reflectionResult,
            List<WorkflowConfigInfo> availableWorkflows,
            MemoryContext memoryContext,
            IProgress<string> progress)
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 开始重新规划 - PlanningID: {currentPlanning.PlanningID}");

            try
            {
                // 1. 保存修订历史
                await SaveRevisionHistoryAsync(currentPlanning, reflectionResult.Reason);

                // 2. 分析执行情况
                var analysis = AnalyzeExecutionStatus(currentPlanning, executionResult);

                LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                    $"[TaskPlanning] 执行分析 - 已完成: {analysis.CompletedSteps.Count}, 失败: {analysis.FailedSteps.Count}, 未执行: {analysis.PendingSteps.Count}");

                // 3. 构建重新规划提示词
                string replanPrompt = BuildReplanningPrompt(
                    currentPlanning, analysis, reflectionResult, availableWorkflows, nodeData);

                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 重新规划提示词长度: {replanPrompt.Length} 字符");

                // 4. 调用 LLM 生成新规划(流式输出)
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 使用模型: {planningModelConfig.Model.ModelName} (ID: {planningModelConfig.Id})");
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("你是一个专业的任务重新规划助手,擅长根据执行反馈优化计划。");
                chatHistory.AddUserMessage(replanPrompt);

                var responseBuilder = new System.Text.StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    planningModelConfig, 
                    chatHistory,
                    Function: null,
                    responseFormat: "text",
                    enableStreamingObservation: true,
                    progress: progress,
                    ct: System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }
                var response = responseBuilder.ToString();

                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" LLM 响应长度: {response?.Length ?? 0} 字符");

                // 5. 解析新规划
                var newSteps = ParseReplanningResponse(response, currentPlanning, analysis);

                // 6. 合并已完成步骤和新步骤
                MergeSteps(currentPlanning, analysis.CompletedSteps, newSteps);

                // 7. 更新元数据
                currentPlanning.Metadata.RevisionCount++;
                currentPlanning.TotalSteps = currentPlanning.Steps.Count;
                currentPlanning.LastUpdateTime = DateTime.Now;

                LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                    $"[TaskPlanning] 重新规划完成 - 新步骤数: {currentPlanning.TotalSteps}, 修订次数: {currentPlanning.Metadata.RevisionCount}");

                // 8. 保存到数据库
                await SavePlanningAsync(currentPlanning);

                return currentPlanning;
            }
            catch (LLMException llmEx) when (llmEx.IsFatal)
            {
                // 致命 LLM 错误（403/欠费等）：不继续重规划循环，向上抛出。
                LoggerHelper.LogError(_logger, ClawLogModules.TASK_PLANNING,
                    $" 重新规划失败（LLM 致命错误） - PlanningID: {currentPlanning.PlanningID}: {llmEx.Message}", llmEx);
                throw;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.TASK_PLANNING, $" 重新规划失败 - PlanningID: {currentPlanning.PlanningID}");
                
                // 失败时简单增加修订计数
                currentPlanning.Metadata.RevisionCount++;
                currentPlanning.LastUpdateTime = DateTime.Now;
                await SavePlanningAsync(currentPlanning);
                
                return currentPlanning;
            }
        }

        public async Task UpdatePlanningStatusAsync(TaskPlanning taskPlanning)
        {
            taskPlanning.LastUpdateTime = DateTime.Now;
            
            // 更新数据库
            TaskPlanningBusiness.UpdateStatus(taskPlanning.PlanningID, taskPlanning.PlanningStatus.ToString());
            
            LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 更新状态 - PlanningID: {taskPlanning.PlanningID}, Status: {taskPlanning.PlanningStatus}");
        }

        public async Task SavePlanningAsync(TaskPlanning taskPlanning)
        {
            try
            {
                // 优化：使用CreateTime判断是否为新规划，避免额外的数据库查询
                bool isUpdate = taskPlanning.CreateTime < DateTime.Now.AddSeconds(-1);

                // 1. 转换为实体模型
                var planningEntity = new TaskPlanningInfo
                {
                    PlanningID = taskPlanning.PlanningID,
                    AppID = taskPlanning.AppID,
                    SessionID = taskPlanning.SessionID,
                    MemberID = taskPlanning.MemberID,
                    NodeID = taskPlanning.NodeID,
                    ProcessesID = taskPlanning.ProcessesID,
                    OriginalTask = taskPlanning.OriginalTask,
                    PlanningStatus = taskPlanning.PlanningStatus.ToString(),
                    CurrentStepIndex = taskPlanning.CurrentStepIndex,
                    TotalSteps = taskPlanning.TotalSteps,
                    Strategy = taskPlanning.Metadata.Strategy,
                    Confidence = taskPlanning.Metadata.Confidence,
                    EstimatedDuration = taskPlanning.Metadata.EstimatedDuration,
                    ActualDuration = taskPlanning.Metadata.ActualDuration,
                    RevisionCount = taskPlanning.Metadata.RevisionCount,
                    CreateTime = taskPlanning.CreateTime,
                    LastUpdateTime = taskPlanning.LastUpdateTime
                };

                // 2. 预先转换步骤实体（避免在条件分支中重复转换）
                List<TaskStepInfo> stepEntities = null;
                if (taskPlanning.Steps != null && taskPlanning.Steps.Count > 0)
                {
                    stepEntities = taskPlanning.Steps.Select(step => new TaskStepInfo
                    {
                        StepID = step.StepID,
                        PlanningID = taskPlanning.PlanningID,
                        StepIndex = step.StepIndex,
                        StepDescription = step.StepDescription,
                        StepType = step.StepType.ToString(),
                        AssignedWorkflowIds = JsonConvert.SerializeObject(step.AssignedWorkflowIds),
                        StepStatus = step.StepStatus.ToString(),
                        DependsOnStepIds = JsonConvert.SerializeObject(step.DependsOnStepIds),
                        StepInputs = step.StepInputs != null && step.StepInputs.Count > 0 
                            ? JsonConvert.SerializeObject(step.StepInputs) 
                            : null,
                        ExpectedOutput = step.ExpectedOutput,
                        ActualOutput = step.ActualOutput,
                        ExecutionResult = step.ExecutionResult,
                        QualityScore = step.QualityScore ?? 0,
                        RetryCount = step.RetryCount,
                        ErrorMessage = step.ErrorMessage ?? "",
                        StartTime = step.StartTime,
                        EndTime = step.EndTime,
                        CreateTime = step.CreateTime
                    }).ToList();
                }

                if (isUpdate)
                {
                    // 优化：批量操作 - 更新规划 + 删除旧步骤 + 插入新步骤
                    TaskPlanningBusiness.Update(planningEntity);
                    
                    if (stepEntities != null && stepEntities.Count > 0)
                    {
                        // 删除该规划下的所有旧步骤
                        TaskStepBusiness.DeleteByPlanningID(taskPlanning.PlanningID);
                        // 批量插入新步骤
                        TaskStepBusiness.AddBatch(stepEntities);
                    }
                    
                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 更新规划 - PlanningID: {taskPlanning.PlanningID}, Steps: {stepEntities?.Count ?? 0}");
                }
                else
                {
                    // 优化：批量操作 - 新增规划 + 批量插入步骤
                    TaskPlanningBusiness.Add(planningEntity);
                    
                    if (stepEntities != null && stepEntities.Count > 0)
                    {
                        TaskStepBusiness.AddBatch(stepEntities);
                    }
                    
                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 新增规划 - PlanningID: {taskPlanning.PlanningID}, Steps: {stepEntities?.Count ?? 0}");
                }

                _logger.LogDebug($"[TaskPlanning] 保存规划成功 - PlanningID: {taskPlanning.PlanningID}, 操作: {(isUpdate ? "更新" : "新增")}");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.TASK_PLANNING, $" 保存规划失败 - PlanningID: {taskPlanning.PlanningID}");
                throw;
            }
            
        }

        public async Task<List<TaskPlanning>> GetHistoricalPlansAsync(string AppID, string MemberID, int limit)
        {
            try
            {
                // 从数据库查询历史规划
                var entities = TaskPlanningBusiness.GetHistoricalPlans(MemberID, AppID, limit);
                
                // 转换为业务模型
                var plans = new List<TaskPlanning>();
                foreach (var entity in entities)
                {
                    var plan = new TaskPlanning
                    {
                        PlanningID = entity.PlanningID,
                        AppID = entity.AppID,
                        SessionID = entity.SessionID,
                        MemberID = entity.MemberID,
                        NodeID = entity.NodeID,
                        ProcessesID = entity.ProcessesID,
                        OriginalTask = entity.OriginalTask,
                        PlanningStatus = Enum.Parse<PlanningStatus>(entity.PlanningStatus),
                        CurrentStepIndex = entity.CurrentStepIndex,
                        TotalSteps = entity.TotalSteps,
                        Metadata = new PlanningMetadata
                        {
                            Strategy = entity.Strategy,
                            Confidence = entity.Confidence,
                            EstimatedDuration = entity.EstimatedDuration,
                            ActualDuration = entity.ActualDuration,
                            RevisionCount = entity.RevisionCount
                        },
                        CreateTime = entity.CreateTime,
                        LastUpdateTime = entity.LastUpdateTime
                    };
                    
                    // 查询步骤
                    var stepEntities = TaskStepBusiness.GetByPlanningID(entity.PlanningID);
                    plan.Steps = stepEntities.Select(s => new TaskStep
                    {
                        StepID = s.StepID,
                        PlanningID = s.PlanningID,
                        StepIndex = s.StepIndex,
                        StepDescription = s.StepDescription,
                        StepType = Enum.Parse<StepType>(s.StepType),
                        AssignedWorkflowIds = JsonConvert.DeserializeObject<List<string>>(s.AssignedWorkflowIds ?? "[]"),
                        StepStatus = Enum.Parse<StepStatus>(s.StepStatus),
                        DependsOnStepIds = JsonConvert.DeserializeObject<List<string>>(s.DependsOnStepIds ?? "[]"),
                        StepInputs = !string.IsNullOrEmpty(s.StepInputs) 
                            ? JsonConvert.DeserializeObject<List<Inputs>>(s.StepInputs) 
                            : new List<Inputs>(),
                        ExpectedOutput = s.ExpectedOutput,
                        ActualOutput = s.ActualOutput,
                        ExecutionResult = s.ExecutionResult,
                        QualityScore = s.QualityScore > 0 ? s.QualityScore : (int?)null,
                        RetryCount = s.RetryCount,
                        ErrorMessage = s.ErrorMessage ?? "",
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        CreateTime = s.CreateTime
                    }).ToList();
                    
                    plans.Add(plan);
                }
                
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 查询历史规划 - MemberID: {MemberID}, Count: {plans.Count}");
                
                return plans;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.TASK_PLANNING, $" 查询历史规划失败 - MemberID: {MemberID}");
                return new List<TaskPlanning>();
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 尝试基于记忆创建快速响应计划
        /// </summary>
        private TaskPlanning TryCreateMemoryBasedPlan(
            string originalTask,
            MemoryContext memoryContext,
            List<WorkflowConfigInfo> availableWorkflows,
            TaskComplexity complexity,
            string AppID,
            string SessionID,
            string MemberID,
            string NodeID,
            string ProcessesID)
        {
            // 只对简单和中等复杂度任务尝试记忆快速响应
            if (complexity == TaskComplexity.Complex)
                return null;

            var taskLower = originalTask.ToLower();

            // 1. 检查短期记忆(ChatHistory) - 最近对话中是否已经讨论过类似问题
            if (memoryContext.WorkingMemory != null && memoryContext.WorkingMemory.Count > 0)
            {
                var recentMessages = memoryContext.WorkingMemory.OrderByDescending(m => m.CreateTime).Take(_options.Memory.WorkingMemoryLimit);
                foreach (var msg in recentMessages)
                {
                    // 如果最近的对话中AI已经回答过类似问题,直接用LLM总结即可
                    if (msg.Role == "assistant")
                    {
                        var similarity = CalculateSimpleTextSimilarity(taskLower, JsonConvert.SerializeObject(msg.Content));
                        if (similarity > _options.SimilarityThresholds.MemoryFastPath)
                        {
                            _logger.LogInformation($"[MemoryFastPath] 短期记忆命中 - 相似度: {similarity:F2}, 使用LLM总结");
                            return CreateLLMReasoningPlan(originalTask, "短期记忆", AppID, SessionID, MemberID, NodeID, ProcessesID);
                        }
                    }
                }
            }

            // 2. 检查情景记忆 - 是否有相似的历史任务经验
            if (memoryContext.RelevantMemories != null && memoryContext.RelevantMemories.Count > 0)
            {
                foreach (var memory in memoryContext.RelevantMemories.OrderByDescending(m => m.Importance))
                {
                    if (!string.IsNullOrEmpty(memory.Summary))
                    {
                        var similarity = CalculateSimpleTextSimilarity(taskLower, memory.Summary.ToLower());
                        if (similarity > _options.SimilarityThresholds.EpisodicMemory && memory.Importance >= _options.Memory.ArchiveImportanceThreshold)
                        {
                            _logger.LogInformation($"[MemoryFastPath] 情景记忆命中 - 相似度: {similarity:F2}, 重要性: {memory.Importance}");
                            return CreateLLMReasoningPlan(originalTask, "情景记忆", AppID, SessionID, MemberID, NodeID, ProcessesID);
                        }
                    }
                }
            }

            // 3. 检查用户画像 - 判断是否为用户常见问题类型
            if (memoryContext.UserProfile != null && !string.IsNullOrEmpty(memoryContext.UserProfile.PreferencesSummary))
            {
                var preferences = memoryContext.UserProfile.PreferencesSummary.ToLower();

                // 如果用户偏好包含当前任务类型,说明是熟悉的场景
                var taskKeywords = ClawAIRegexPatterns.ActionVerbs;
                foreach (var keyword in taskKeywords)
                {
                    if (taskLower.Contains(keyword) && preferences.Contains(keyword))
                    {
                        _logger.LogInformation($"[MemoryFastPath] 用户画像命中 - 偏好类型: {keyword}");
                        return CreateLLMReasoningPlan(originalTask, "用户画像", AppID, SessionID, MemberID, NodeID, ProcessesID);
                    }
                }
            }

            // 4. 检查是否为纯知识问答类任务(不需要WorkFlow)
            if (_options.Planning.KnowledgeQueryPatterns.Any(p => taskLower.Contains(p)) &&
                originalTask.Length < _options.TaskComplexity.KnowledgeQueryMaxLength)
            {
                _logger.LogInformation($"[MemoryFastPath] 知识问答任务 - 无需WorkFlow,使用LLM直接回答");
                return CreateLLMReasoningPlan(originalTask, "知识问答", AppID, SessionID, MemberID, NodeID, ProcessesID);
            }

            return null;
        }
        
        /// <summary>
        /// 创建基于LLM推理的简单计划(不调用WorkFlow)
        /// </summary>
        private TaskPlanning CreateLLMReasoningPlan(
            string originalTask,
            string source,
            string AppID,
            string SessionID,
            string MemberID,
            string NodeID,
            string ProcessesID)
        {
            var planning = new TaskPlanning
            {
                AppID = AppID,
                SessionID = SessionID,
                MemberID = MemberID,
                NodeID = NodeID,
                ProcessesID = ProcessesID,
                OriginalTask = originalTask,
                PlanningStatus = PlanningStatus.Planning,
                TotalSteps = 1,
                Metadata = new PlanningMetadata
                {
                    Strategy = $"memory_fast_path_{source}",
                    Confidence = 85,
                    EstimatedDuration = 30
                }
            };
            
            var step = new TaskStep
            {
                PlanningID = planning.PlanningID,
                StepIndex = 1,
                StepDescription = $"基于{source}直接回答用户问题",
                StepType = StepType.LLMReasoning,
                AssignedWorkflowIds = new List<string>(),
                ExpectedOutput = "基于记忆和知识的直接回答",
                StepStatus = StepStatus.Pending
            };
            
            planning.Steps.Add(step);
            
            return planning;
        }
        
        /// <summary>
        /// 计算简单文本相似度(快速版本)
        /// </summary>
        private double CalculateSimpleTextSimilarity(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return 0;
            
            // 提取关键词(长度>1的词)
            var words1 = text1.Split(new[] { ' ', ',', '.', '!', '?', '、', '。', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 1)
                .Select(w => w.Trim())
                .ToHashSet();
            
            var words2 = text2.Split(new[] { ' ', ',', '.', '!', '?', '、', '。', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 1)
                .Select(w => w.Trim())
                .ToHashSet();
            
            if (words1.Count == 0 || words2.Count == 0)
                return 0;
            
            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();
            
            return union > 0 ? (double)intersection / union : 0;
        }
        
        /// <summary>
        /// 分析任务复杂度
        /// </summary>
        private TaskComplexity AnalyzeTaskComplexity(string task, List<WorkflowConfigInfo> availableWorkflows)
        {
            if (string.IsNullOrWhiteSpace(task))
                return TaskComplexity.Simple;

            var taskLower = task.ToLower().Trim();

            // ===== 优化: 问候语和简单对话检测（增强版）=====
            // 使用统一的GreetingDetector工具类
            if (GreetingDetector.ShouldUseGreetingFastPath(task))
            {
                _logger.LogInformation($"[TaskComplexity] 识别为{GreetingDetector.GetGreetingType(task)},启用超快速路径: {task}");
                return TaskComplexity.Greeting;
            }

            // ===== 优化: 知识问答类任务检测(不需要WorkFlow) =====
            bool isKnowledgeQuery = _options.Planning.KnowledgeQueryPatterns.Any(p => taskLower.Contains(p));
            if (isKnowledgeQuery && task.Length < _options.TaskComplexity.KnowledgeQueryMaxLength)
            {
                _logger.LogDebug($"[TaskComplexity] 识别为知识问答,无需WorkFlow: {task}");
                return TaskComplexity.Simple;
            }

            // 简单任务特征:单一查询、显示类(可能需要WorkFlow)
            bool isSimple = _options.Planning.SimpleTaskPatterns.Any(p => taskLower.Contains(p));
            if (isSimple && task.Length < _options.TaskComplexity.SimpleTaskMaxLength &&
                !_options.TaskComplexity.TaskConnectors.Any(c => taskLower.Contains(c)))
            {
                _logger.LogDebug($"[TaskComplexity] 识别为简单任务: {task}");
                return TaskComplexity.Simple;
            }

            // 复杂任务特征:多步骤、分析、生成、处理类(必须需要WorkFlow)
            bool isComplex = _options.Planning.ComplexTaskPatterns.Any(p => ClawAIRegexPatterns.ComplexTaskPattern.IsMatch(taskLower));
            if (isComplex || task.Length > _options.TaskComplexity.ComplexTaskMinLength ||
                taskLower.Split(_options.TaskComplexity.TaskConnectors.ToArray(), StringSplitOptions.RemoveEmptyEntries).Length > 3)
            {
                _logger.LogDebug($"[TaskComplexity] 识别为复杂任务: {task}");
                return TaskComplexity.Complex;
            }

            // 默认为中等复杂度
            _logger.LogDebug($"[TaskComplexity] 识别为中等任务: {task}");
            return TaskComplexity.Medium;
        }
        
        /// <summary>
        /// 根据复杂度获取动态最大步骤数
        /// </summary>
        private int GetDynamicMaxSteps(TaskComplexity complexity, int configMaxSteps)
        {
            switch (complexity)
            {
                case TaskComplexity.Greeting:
                    return 0; // 问候语不需要规划步骤,直接LLM回答
                case TaskComplexity.Simple:
                    return Math.Min(_options.Planning.SimpleTaskMaxSteps, configMaxSteps); // 简单任务最多2步
                case TaskComplexity.Medium:
                    return Math.Min(_options.Planning.MediumTaskMaxSteps, configMaxSteps); // 中等任务最多5步
                case TaskComplexity.Complex:
                    return configMaxSteps; // 复杂任务使用配置值
                default:
                    return configMaxSteps;
            }
        }

        /// <summary>
        /// 构建规划提示词
        /// </summary>
        private async Task<string> BuildPlanningPromptAsync(
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            MemoryContext memoryContext,
            ClawAIData nodeData,
            string AppID,
            string MemberID,
            int dynamicMaxSteps,
            TaskComplexity complexity)
        {
            var promptTemplate = nodeData.taskPlanningConfig.planningPromptTemplate;
            
            // 根据复杂度调整提示词
            if (complexity == TaskComplexity.Simple)
            {
                promptTemplate = promptTemplate.Replace(
                    "禁止规划多个步骤! 禁止添加分析、验证、整理等步骤!",
                    $"这是简单任务,最多{dynamicMaxSteps}步! 直接调用WorkFlow获取结果,不要添加额外步骤!");
            }

            // 替换原始任务
            promptTemplate = promptTemplate.Replace("{{originalTask}}", originalTask);

            // 构建可用 WorkFlow 列表 - 增强展示,添加能力关键词和智能匹配提示
            var workflowsInfo = new StringBuilder();
            if (availableWorkflows != null && availableWorkflows.Count > 0)
            {
                workflowsInfo.AppendLine($"**共有 {availableWorkflows.Count} 个可用 WorkFlow,这是你唯一可以使用的工具!**");
                workflowsInfo.AppendLine();
                workflowsInfo.AppendLine("**⚠️ 重要规则**:");
                workflowsInfo.AppendLine("1. 只能使用下面列出的WorkFlow ID，禁止臆造或假设其他WorkFlow存在");
                workflowsInfo.AppendLine("2. 仔细阅读每个WorkFlow的能力描述，选择最匹配的");
                workflowsInfo.AppendLine("3. 如果任务关键词与WorkFlow的能力/适用场景匹配，优先选择它");
                workflowsInfo.AppendLine("4. 每个步骤必须提供inputs参数，至少包含prompt字段");
                workflowsInfo.AppendLine();
                
                // 提取任务关键词用于智能匹配提示
                var taskKeywords = ExtractTaskKeywords(originalTask);
                
                foreach (var workflow in availableWorkflows)
                {
                    var workflowIndex = availableWorkflows.IndexOf(workflow) + 1;
                    workflowsInfo.AppendLine($"### WorkFlow {workflowIndex}: {workflow.name}");
                    workflowsInfo.AppendLine($"- **ID**: `{workflow.workflowId}` ⚠️ 必须使用此ID，不能修改");
                    workflowsInfo.AppendLine($"- **描述**: {workflow.description}");
                    
                    // 提取能力关键词
                    if (workflow.capabilities != null && workflow.capabilities.Count > 0)
                    {
                        workflowsInfo.AppendLine($"- **能力标签**: {string.Join("、", workflow.capabilities)}");
                    }
                    
                    // 从描述中提取关键词
                    var keywords = ExtractWorkflowKeywords(workflow);
                    if (keywords.Count > 0)
                    {
                        workflowsInfo.AppendLine($"- **适用场景**: {string.Join("、", keywords)}");
                    }
                    
                    // 智能匹配提示 - 如果任务关键词与WorkFlow能力匹配
                    var matchScore = CalculateWorkflowMatchScore(taskKeywords, workflow);
                    if (matchScore > 0.3)
                    {
                        workflowsInfo.AppendLine($"- 💡 **匹配度**: {matchScore:P0} - 此WorkFlow可能适合当前任务");
                    }
                    
                    workflowsInfo.AppendLine();
                }
                
                workflowsInfo.AppendLine("---");
                workflowsInfo.AppendLine("**🔴 严禁臆造**: 以上是全部可用的WorkFlow，不存在其他WorkFlow！");
                workflowsInfo.AppendLine("**✅ 正确做法**: 从上面的列表中选择最匹配的WorkFlow ID");
                workflowsInfo.AppendLine("**❌ 错误做法**: 编造不存在的WorkFlow ID，或假设存在某个功能的WorkFlow");
            }
            else
            {
                workflowsInfo.AppendLine("⚠️ **无可用 WorkFlow**");
                workflowsInfo.AppendLine("由于没有可用的WorkFlow工具,所有步骤只能使用 `llm_reasoning` 类型。");
                workflowsInfo.AppendLine("**严禁**在 assignedWorkflowIds 中填写任何ID!");
            }

            promptTemplate = promptTemplate.Replace("{{availableWorkFlows}}", workflowsInfo.ToString());

            // 获取历史规划经验 - 增强展示，包含成功案例的WorkFlow使用情况
            var historicalInfo = new StringBuilder();
            try
            {
                var historicalPlans = await GetHistoricalPlansAsync(AppID, MemberID, 5);
                if (historicalPlans != null && historicalPlans.Count > 0)
                {
                    // 只展示成功完成的规划
                    var successfulPlans = historicalPlans
                        .Where(p => p.PlanningStatus == PlanningStatus.Completed)
                        .OrderByDescending(p => p.Metadata.Confidence)
                        .Take(3)
                        .ToList();
                    
                    if (successfulPlans.Count > 0)
                    {
                        historicalInfo.AppendLine("**📚 历史成功案例** (可以参考类似任务的WorkFlow选择):");
                        historicalInfo.AppendLine();
                        
                        foreach (var plan in successfulPlans)
                        {
                            historicalInfo.AppendLine($"### 案例: {plan.OriginalTask}");
                            historicalInfo.AppendLine($"- **策略**: {plan.Metadata.Strategy}");
                            historicalInfo.AppendLine($"- **步骤数**: {plan.TotalSteps}");
                            historicalInfo.AppendLine($"- **置信度**: {plan.Metadata.Confidence}%");
                            
                            // 展示使用的WorkFlow
                            if (plan.Steps != null && plan.Steps.Count > 0)
                            {
                                var usedWorkflows = plan.Steps
                                    .Where(s => s.AssignedWorkflowIds != null && s.AssignedWorkflowIds.Count > 0)
                                    .SelectMany(s => s.AssignedWorkflowIds)
                                    .Distinct()
                                    .ToList();
                                
                                if (usedWorkflows.Count > 0)
                                {
                                    historicalInfo.AppendLine($"- **使用的WorkFlow**: {string.Join(", ", usedWorkflows)}");
                                    
                                    // 展示步骤详情
                                    historicalInfo.AppendLine("- **步骤详情**:");
                                    foreach (var step in plan.Steps.OrderBy(s => s.StepIndex))
                                    {
                                        var workflowIds = step.AssignedWorkflowIds != null && step.AssignedWorkflowIds.Count > 0
                                            ? string.Join(", ", step.AssignedWorkflowIds)
                                            : "无";
                                        historicalInfo.AppendLine($"  {step.StepIndex}. {step.StepDescription} (WorkFlow: {workflowIds})");
                                    }
                                }
                            }
                            
                            historicalInfo.AppendLine();
                        }
                        
                        historicalInfo.AppendLine("💡 **参考建议**: 如果当前任务与上述案例相似，可以参考其WorkFlow选择和步骤设计");
                    }
                    else
                    {
                        historicalInfo.AppendLine("暂无成功完成的历史规划案例");
                    }
                }
                else
                {
                    historicalInfo.AppendLine("暂无历史规划经验");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TaskPlanning] 获取历史规划失败");
                historicalInfo.AppendLine("暂无历史规划经验");
            }
            promptTemplate = promptTemplate.Replace("{{historicalPlans}}", historicalInfo.ToString());

            // 添加历史对话上下文
            var chatHistoryInfo = new StringBuilder();
            if (memoryContext.WorkingMemory != null && memoryContext.WorkingMemory.Count > 0)
            {
                chatHistoryInfo.AppendLine("## 历史对话上下文");
                chatHistoryInfo.AppendLine("以下是本次会话的历史对话,请基于此上下文理解用户的当前需求:");
                chatHistoryInfo.AppendLine();
                
                foreach (var msg in memoryContext.WorkingMemory.OrderBy(m => m.CreateTime))
                {
                    chatHistoryInfo.AppendLine($"**{msg.Role}**: {msg.Content}");
                    chatHistoryInfo.AppendLine();
                }
                chatHistoryInfo.AppendLine("---");
                chatHistoryInfo.AppendLine("**重要**: 请结合以上历史对话理解用户当前的任务需求,避免重复相同的规划!");
            }
            else
            {
                chatHistoryInfo.AppendLine("(这是本次会话的第一轮对话)");
            }
            
            promptTemplate = promptTemplate.Replace("{{chatHistory}}", chatHistoryInfo.ToString());

            // 添加明确的 JSON 格式要求和示例
            var jsonFormatGuide = new StringBuilder();
            jsonFormatGuide.AppendLine("\n\n## 🔴 重要：JSON 格式要求");
            jsonFormatGuide.AppendLine("\n**每个步骤必须包含以下字段**：");
            jsonFormatGuide.AppendLine("1. `dependsOnStepIds`: 依赖的前置步骤ID列表（如果步骤需要使用前面步骤的结果，必须填写）");
            jsonFormatGuide.AppendLine("2. `inputs`: 步骤的输入参数列表（每个步骤都应该有明确的输入参数）");
            jsonFormatGuide.AppendLine("\n**示例**：");
            jsonFormatGuide.AppendLine("```json");
            jsonFormatGuide.AppendLine("{");
            jsonFormatGuide.AppendLine("  \"steps\": [");
            jsonFormatGuide.AppendLine("    {");
            jsonFormatGuide.AppendLine("      \"stepIndex\": 1,");
            jsonFormatGuide.AppendLine("      \"stepDescription\": \"从知识库检索AgentBrook的资料\",");
            jsonFormatGuide.AppendLine("      \"stepType\": \"workflow_call\",");
            jsonFormatGuide.AppendLine("      \"assignedWorkflowIds\": [\"workflow-id-1\"],");
            jsonFormatGuide.AppendLine("      \"dependsOnStepIds\": [],");
            jsonFormatGuide.AppendLine("      \"inputs\": [");
            jsonFormatGuide.AppendLine("        {\"varname\": \"prompt\", \"value\": \"检索AgentBrook的部署、使用、功能等信息\"}");
            jsonFormatGuide.AppendLine("      ],");
            jsonFormatGuide.AppendLine("      \"expectedOutput\": \"AgentBrook的详细资料\"");
            jsonFormatGuide.AppendLine("    },");
            jsonFormatGuide.AppendLine("    {");
            jsonFormatGuide.AppendLine("      \"stepIndex\": 2,");
            jsonFormatGuide.AppendLine("      \"stepDescription\": \"基于步骤1的结果进行闲聊\",");
            jsonFormatGuide.AppendLine("      \"stepType\": \"workflow_call\",");
            jsonFormatGuide.AppendLine("      \"assignedWorkflowIds\": [\"workflow-id-2\"],");
            jsonFormatGuide.AppendLine("      \"dependsOnStepIds\": [1],  // ⚠️ 填写依赖的步骤索引（stepIndex）");
            jsonFormatGuide.AppendLine("      \"inputs\": [");
            jsonFormatGuide.AppendLine("        {\"varname\": \"prompt\", \"value\": \"基于上一步的AgentBrook资料进行友好对话\"}");
            jsonFormatGuide.AppendLine("      ],");
            jsonFormatGuide.AppendLine("      \"expectedOutput\": \"关于AgentBrook的友好对话\"");
            jsonFormatGuide.AppendLine("    }");
            jsonFormatGuide.AppendLine("  ]");
            jsonFormatGuide.AppendLine("}");
            jsonFormatGuide.AppendLine("```");
            jsonFormatGuide.AppendLine("\n**关键规则**：");
            jsonFormatGuide.AppendLine("- 如果步骤2需要使用步骤1的结果，**必须**在 `dependsOnStepIds` 中填写步骤1的索引（如 [1]）");
            jsonFormatGuide.AppendLine("- 每个步骤都应该有 `inputs` 数组，至少包含一个 `prompt` 参数");
            jsonFormatGuide.AppendLine("- `dependsOnStepIds` 填写步骤索引（stepIndex），系统会自动转换为实际的 StepID");
            jsonFormatGuide.AppendLine("- 系统会自动将前置步骤的结果作为 `context` 参数传递给依赖步骤");
            jsonFormatGuide.AppendLine("- **重要**：如果步骤描述中提到\"基于前面步骤\"、\"使用上一步结果\"等，必须设置 dependsOnStepIds！");
            
            promptTemplate += jsonFormatGuide.ToString();

            return promptTemplate;
        }

        /// <summary>
        /// 解析 LLM 返回的规划结果
        /// </summary>
        private TaskPlanning ParsePlanningResponse(
            string response,
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            string AppID,
            string SessionID,
            string MemberID,
            string NodeID,
            string ProcessesID)
        {
            try
            {
                // 提取 JSON 部分
                string jsonContent = ExtractJsonFromResponse(response);
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 提取的 JSON 内容:\n{jsonContent}");
                
                // 解析 JSON
                var planningData = JsonConvert.DeserializeObject<PlanningResponseData>(jsonContent);
                
                if (planningData == null || planningData.steps == null || planningData.steps.Count == 0)
                {
                    throw new Exception("规划数据解析失败或步骤为空");
                }
                
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 解析到 {planningData.steps.Count} 个步骤");

                // 创建规划对象
                var planning = new TaskPlanning
                {
                    AppID = AppID,
                    SessionID = SessionID,
                    MemberID = MemberID,
                    NodeID = NodeID,
                    ProcessesID = ProcessesID,
                    OriginalTask = originalTask,
                    PlanningStatus = PlanningStatus.Planning,
                    TotalSteps = planningData.steps.Count,
                    Metadata = new PlanningMetadata
                    {
                        Strategy = planningData.strategy ?? "sequential",
                        Confidence = planningData.confidence > 0 ? planningData.confidence : 80,
                        EstimatedDuration = planningData.estimatedDuration > 0 ? planningData.estimatedDuration : 60
                    }
                };

                // 第一遍：创建所有步骤（先不处理依赖关系）
                var stepIndexToIdMap = new Dictionary<int, string>();
                
                foreach (var stepData in planningData.steps)
                {
                    var step = new TaskStep
                    {
                        PlanningID = planning.PlanningID,
                        StepIndex = stepData.stepIndex,
                        StepDescription = stepData.stepDescription,
                        StepType = ParseStepType(stepData.stepType),
                        AssignedWorkflowIds = stepData.assignedWorkflowIds ?? new List<string>(),
                        DependsOnStepIds = new List<string>(), // 暂时为空，第二遍处理
                        ExpectedOutput = stepData.expectedOutput,
                        StepStatus = StepStatus.Pending,
                        StepInputs = ExtractStepInputs(stepData)
                    };

                    planning.Steps.Add(step);
                    stepIndexToIdMap[step.StepIndex] = step.StepID;
                    
                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 创建步骤 {step.StepIndex}: {step.StepID}");
                }
                
                // 第二遍：处理依赖关系（将 stepIndex 转换为 StepID）
                for (int i = 0; i < planningData.steps.Count; i++)
                {
                    var stepData = planningData.steps[i];
                    var step = planning.Steps[i];
                    
                    if (stepData.dependsOnStepIds != null && stepData.dependsOnStepIds.Count > 0)
                    {
                        foreach (var depId in stepData.dependsOnStepIds)
                        {
                            // 尝试将依赖ID解析为整数（stepIndex）
                            if (int.TryParse(depId, out int depStepIndex))
                            {
                                // 转换为实际的 StepID
                                if (stepIndexToIdMap.ContainsKey(depStepIndex))
                                {
                                    step.DependsOnStepIds.Add(stepIndexToIdMap[depStepIndex]);
                                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 步骤 {step.StepIndex} 依赖步骤 {depStepIndex} (ID: {stepIndexToIdMap[depStepIndex]})");
                                }
                                else
                                {
                                    LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING, $" 步骤 {step.StepIndex} 依赖的步骤索引 {depStepIndex} 不存在");
                                }
                            }
                            else
                            {
                                // 如果已经是 GUID 格式，直接使用
                                step.DependsOnStepIds.Add(depId);
                                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 步骤 {step.StepIndex} 依赖步骤 ID: {depId}");
                            }
                        }
                    }
                }

                // 第三遍：规范化 StepInputs 中的占位符为 {output_<StepID>} 格式
                NormalizeInputPlaceholders(planning.Steps, stepIndexToIdMap);

                // 第四遍：检测并替换 StepInputs 中的内联嵌入文本（LLM直接粘贴前置步骤的预期输出）
                DetectAndReplaceInlineEmbeddings(planning.Steps);

                return planning;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TaskPlanning] 解析规划响应失败");
                throw;
            }
        }

        /// <summary>
        /// 从响应中提取 JSON 内容
        /// </summary>
        private string ExtractJsonFromResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                throw new Exception("响应内容为空");
            }

            // 尝试提取 ```json ... ``` 中的内容
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                response, @"```json\s*(\{[\s\S]*?\})\s*```", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (jsonMatch.Success)
            {
                return jsonMatch.Groups[1].Value.Trim();
            }

            // 尝试提取 ``` ... ``` 中的内容
            jsonMatch = System.Text.RegularExpressions.Regex.Match(
                response, @"```\s*(\{[\s\S]*?\})\s*```");
            
            if (jsonMatch.Success)
            {
                return jsonMatch.Groups[1].Value.Trim();
            }

            // 尝试直接查找 JSON 对象
            jsonMatch = System.Text.RegularExpressions.Regex.Match(
                response, @"(\{[\s\S]*\})");
            
            if (jsonMatch.Success)
            {
                return jsonMatch.Groups[1].Value.Trim();
            }

            throw new Exception("无法从响应中提取 JSON 内容");
        }

        /// <summary>
        /// 解析步骤类型
        /// </summary>
        private StepType ParseStepType(string stepType)
        {
            if (string.IsNullOrEmpty(stepType))
            {
                return StepType.LLMReasoning;
            }

            stepType = stepType.ToLower().Replace("_", "").Replace("-", "");

            if (stepType.Contains("workflow"))
                return StepType.WorkflowCall;
            if (stepType.Contains("llm") || stepType.Contains("reasoning"))
                return StepType.LLMReasoning;
            if (stepType.Contains("data") || stepType.Contains("collection"))
                return StepType.DataCollection;
            if (stepType.Contains("valid"))
                return StepType.Validation;
            if (stepType.Contains("synth"))
                return StepType.Synthesis;

            return StepType.LLMReasoning;
        }

        /// <summary>
        /// 规范化步骤输入中的占位符为 {output_<StepID>} 格式
        /// 将 LLM 生成的任意占位符名称统一替换为确定的 {output_<StepID>} 格式
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
                            var idPart = placeholder.Substring(7);
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
                                return match.Value;
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

                            LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING,
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
        /// 验证和优化规划 - 增强WorkFlow匹配逻辑
        /// </summary>
        private void ValidateAndOptimizePlanning(
            TaskPlanning planning,
            List<WorkflowConfigInfo> availableWorkflows,
            ClawAIData nodeData,
            int dynamicMaxSteps)
        {
            // 1. 限制步骤数量(使用动态步骤数)
            int maxSteps = dynamicMaxSteps;
            if (planning.TotalSteps > maxSteps)
            {
                LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING, $" 步骤数 {planning.TotalSteps} 超过最大限制 {maxSteps},进行裁剪");
                planning.Steps = planning.Steps.Take(maxSteps).ToList();
                planning.TotalSteps = planning.Steps.Count;
            }

            // 2. 验证 WorkFlow 分配并智能匹配
            var availableWorkflowIds = new HashSet<string>(availableWorkflows.Select(w => w.workflowId));
            int autoMatchedCount = 0;
            
            foreach (var step in planning.Steps)
            {
                // 记录原始分配的 WorkFlow
                var originalWorkflowIds = new List<string>(step.AssignedWorkflowIds);
                
                // 移除不存在的 WorkFlow
                step.AssignedWorkflowIds = step.AssignedWorkflowIds
                    .Where(id => availableWorkflowIds.Contains(id))
                    .ToList();

                // 如果有无效的 WorkFlow 被移除,记录警告
                var removedWorkflowIds = originalWorkflowIds.Except(step.AssignedWorkflowIds).ToList();
                if (removedWorkflowIds.Count > 0)
                {
                    _logger.LogWarning(
                        $"[TaskPlanning] 步骤 {step.StepIndex} 包含无效的 WorkFlow ID,已移除: {string.Join(", ", removedWorkflowIds)}");
                }

                // ===== P1修复: 强制单 WorkFlow 校验 =====
                // 当前版本不支持一个步骤分配多个 WorkFlow,如果检测到多个,抛出异常
                if (step.AssignedWorkflowIds.Count > 1)
                {
                    var workflowNames = string.Join(", ", 
                        step.AssignedWorkflowIds.Select(id => 
                            availableWorkflows.FirstOrDefault(w => w.workflowId == id)?.name ?? id));
                    
                    throw new InvalidOperationException(
                        $"步骤 {step.StepIndex} 分配了 {step.AssignedWorkflowIds.Count} 个 WorkFlow ({workflowNames})，" +
                        $"当前版本仅支持单 WorkFlow 步骤。请拆分为多个步骤，每个步骤调用一个 WorkFlow。");
                }

                // ===== 核心优化: 智能WorkFlow匹配 =====
                // 如果步骤没有分配WorkFlow,但有可用WorkFlow,尝试自动匹配
                if (step.AssignedWorkflowIds.Count == 0 && availableWorkflows.Count > 0)
                {
                    var matchedWorkflows = TryMatchWorkflowsForStep(step, availableWorkflows);
                    if (matchedWorkflows.Count > 0)
                    {
                        step.AssignedWorkflowIds = matchedWorkflows;
                        step.StepType = StepType.WorkflowCall;
                        autoMatchedCount++;
                        
                        var workflowNames = string.Join(", ", 
                            matchedWorkflows.Select(id => availableWorkflows.FirstOrDefault(w => w.workflowId == id)?.name ?? id));
                        
                        LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                            $"[TaskPlanning] ✓ 步骤 {step.StepIndex} 自动匹配到WorkFlow: {workflowNames}");
                    }
                }

                // 自动纠正步骤类型
                if (step.AssignedWorkflowIds.Count > 0)
                {
                    // 如果分配了有效的 WorkFlow,但类型不是 WorkflowCall,自动修正为 WorkflowCall
                    if (step.StepType != StepType.WorkflowCall)
                    {
                        LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 步骤 {step.StepIndex} 已分配 WorkFlow,自动修正类型: {step.StepType} -> WorkflowCall");
                        step.StepType = StepType.WorkflowCall;
                    }
                }
                else
                {
                    // 如果步骤类型是 WorkflowCall 但没有分配 WorkFlow,改为 LLMReasoning
                    if (step.StepType == StepType.WorkflowCall)
                    {
                        LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 步骤 {step.StepIndex} 未分配 WorkFlow,自动修正类型: WorkflowCall -> LLMReasoning");
                        step.StepType = StepType.LLMReasoning;
                    }
                }
            }
            
            if (autoMatchedCount > 0)
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" ✓ 自动匹配成功: {autoMatchedCount} 个步骤");
            }

            // 3. 验证依赖关系
            var validStepIds = new HashSet<string>(planning.Steps.Select(s => s.StepID));
            foreach (var step in planning.Steps)
            {
                // 移除无效的依赖
                step.DependsOnStepIds = step.DependsOnStepIds
                    .Where(id => validStepIds.Contains(id))
                    .ToList();

                // 移除循环依赖(简单检查:不能依赖自己)
                step.DependsOnStepIds = step.DependsOnStepIds
                    .Where(id => id != step.StepID)
                    .ToList();
            }

            // 4. 确保步骤索引连续
            for (int i = 0; i < planning.Steps.Count; i++)
            {
                planning.Steps[i].StepIndex = i + 1;
            }

            LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 规划验证完成 - 最终步骤数: {planning.TotalSteps}, 使用WorkFlow的步骤: {planning.Steps.Count(s => s.StepType == StepType.WorkflowCall)}");
        }

        /// <summary>
        /// 尝试为步骤匹配合适的WorkFlow - 使用文本相似度算法
        /// </summary>
        private List<string> TryMatchWorkflowsForStep(TaskStep step, List<WorkflowConfigInfo> availableWorkflows)
        {
            var matchedWorkflows = new List<string>();
            var stepText = $"{step.StepDescription} {step.ExpectedOutput}".ToLower();

            // 只过滤明显的纯分析步骤(不包含任何操作动词)
            bool isPureAnalysis = _options.Planning.PureAnalysisPatterns.Any(pattern => stepText.Contains(pattern));

            if (isPureAnalysis)
            {
                _logger.LogDebug($"[WorkflowMatch] 步骤 '{step.StepDescription}' 是纯分析步骤,不分配WorkFlow");
                return matchedWorkflows;
            }

            // 计算文本相似度
            var workflowScores = new List<(WorkflowConfigInfo workflow, double similarity)>();

            foreach (var workflow in availableWorkflows)
            {
                var workflowText = $"{workflow.name} {workflow.description} {string.Join(" ", workflow.capabilities ?? new List<string>())}";
                double similarity = CalculateTextSimilarity(stepText, workflowText);

                if (similarity > 0)
                {
                    workflowScores.Add((workflow, similarity));
                }
            }

            // 选择相似度最高的WorkFlow
            if (workflowScores.Count > 0)
            {
                var bestMatch = workflowScores.OrderByDescending(x => x.similarity).First();

                // 降低阈值,只要有一点相关性就分配
                if (bestMatch.similarity > _options.Planning.WorkflowMatchThreshold)
                {
                    matchedWorkflows.Add(bestMatch.workflow.workflowId);
                    _logger.LogDebug($"[WorkflowMatch] 步骤 '{step.StepDescription}' 匹配到 '{bestMatch.workflow.name}' (相似度: {bestMatch.similarity:F3})");
                }
                else
                {
                    _logger.LogDebug($"[WorkflowMatch] 步骤 '{step.StepDescription}' 相似度太低({bestMatch.similarity:F3}),不分配WorkFlow");
                }
            }

            return matchedWorkflows;
        }

        /// <summary>
        /// 计算两段文本的相似度 - 使用Jaccard相似度算法
        /// </summary>
        private double CalculateTextSimilarity(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return 0;
            
            // 分词:提取所有有意义的词(长度>1)
            var words1 = ExtractWords(text1.ToLower());
            var words2 = ExtractWords(text2.ToLower());
            
            if (words1.Count == 0 || words2.Count == 0)
                return 0;
            
            // 计算Jaccard相似度: 交集大小 / 并集大小
            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();
            
            return union > 0 ? (double)intersection / union : 0;
        }
        
        /// <summary>
        /// 从文本中提取有意义的词
        /// </summary>
        private HashSet<string> ExtractWords(string text)
        {
            var words = new HashSet<string>();
            
            // 分隔符:空格、标点、特殊字符
            var separators = new[] { ' ', ',', '.', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', 
                                    '、', '。', '!', '?', ';', ':', '"', '"', '\u2018', '\u2019', '《', '》', 
                                    '-', '_', '/', '\\', '|', '\n', '\r', '\t' };
            
            var tokens = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var token in tokens)
            {
                var trimmed = token.Trim();
                // 只保留长度>1的词,过滤单字符和纯数字
                if (trimmed.Length > 1 && !IsAllDigits(trimmed))
                {
                    words.Add(trimmed);
                }
            }
            
            return words;
        }
        
        /// <summary>
        /// 判断字符串是否全是数字
        /// </summary>
        private bool IsAllDigits(string str)
        {
            return str.All(char.IsDigit);
        }

        /// <summary>
        /// 从WorkFlow信息中提取关键词 - 用于提示词展示
        /// </summary>
        private List<string> ExtractWorkflowKeywords(WorkflowConfigInfo workflow)
        {
            var keywords = new HashSet<string>();
            
            // 从名称提取
            var nameWords = workflow.name.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in nameWords)
            {
                if (word.Length > 1)
                {
                    keywords.Add(word);
                }
            }
            
            // 从Capabilities提取
            if (workflow.capabilities != null && workflow.capabilities.Count > 0)
            {
                foreach (var cap in workflow.capabilities)
                {
                    if (!string.IsNullOrEmpty(cap) && cap.Length > 1)
                    {
                        keywords.Add(cap.Trim());
                    }
                }
            }
            
            // 从描述中提取关键词
            if (!string.IsNullOrEmpty(workflow.description))
            {
                var descWords = workflow.description.Split(new[] { ',', ';', '、', ' ', '，' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in descWords.Take(5)) // 只取前5个词
                {
                    var trimmed = word.Trim();
                    if (trimmed.Length > 1)
                    {
                        keywords.Add(trimmed);
                    }
                }
            }
            
            return keywords.ToList();
        }

        /// <summary>
        /// 从任务中提取关键词
        /// </summary>
        private HashSet<string> ExtractTaskKeywords(string task)
        {
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrWhiteSpace(task))
                return keywords;
            
            // 提取中文关键词(长度>=2的词)
            var chineseWords = System.Text.RegularExpressions.Regex.Matches(task, @"[\u4e00-\u9fa5]{2,}");
            foreach (System.Text.RegularExpressions.Match match in chineseWords)
            {
                keywords.Add(match.Value);
            }
            
            // 提取英文关键词
            var englishWords = System.Text.RegularExpressions.Regex.Matches(task, @"[a-zA-Z]{2,}");
            foreach (System.Text.RegularExpressions.Match match in englishWords)
            {
                keywords.Add(match.Value);
            }
            
            // 提取常见动词
            var actionVerbs = new[] { 
                "查询", "搜索", "检索", "查找", "获取",
                "生成", "创建", "制作", "编写", "撰写",
                "分析", "统计", "计算", "处理", "转换",
                "介绍", "说明", "解释", "描述"
            };
            
            foreach (var verb in actionVerbs)
            {
                if (task.Contains(verb))
                {
                    keywords.Add(verb);
                }
            }
            
            return keywords;
        }

        /// <summary>
        /// 计算WorkFlow与任务的匹配度
        /// </summary>
        private double CalculateWorkflowMatchScore(HashSet<string> taskKeywords, WorkflowConfigInfo workflow)
        {
            if (taskKeywords == null || taskKeywords.Count == 0)
                return 0;
            
            var workflowKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // 收集WorkFlow的所有关键词
            if (!string.IsNullOrEmpty(workflow.name))
            {
                foreach (var word in workflow.name.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (word.Length > 1)
                        workflowKeywords.Add(word);
                }
            }
            
            if (!string.IsNullOrEmpty(workflow.description))
            {
                // 提取描述中的中文关键词
                var chineseWords = System.Text.RegularExpressions.Regex.Matches(workflow.description, @"[\u4e00-\u9fa5]{2,}");
                foreach (System.Text.RegularExpressions.Match match in chineseWords)
                {
                    workflowKeywords.Add(match.Value);
                }
            }
            
            if (workflow.capabilities != null)
            {
                foreach (var cap in workflow.capabilities)
                {
                    if (!string.IsNullOrEmpty(cap))
                        workflowKeywords.Add(cap.Trim());
                }
            }
            
            // 计算交集
            var intersection = taskKeywords.Intersect(workflowKeywords, StringComparer.OrdinalIgnoreCase).Count();
            
            if (intersection == 0)
                return 0;
            
            // 匹配度 = 交集数量 / 任务关键词数量
            return (double)intersection / taskKeywords.Count;
        }

        /// <summary>
        /// 保存修订历史
        /// </summary>
        private async Task SaveRevisionHistoryAsync(TaskPlanning planning, string reason)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 开始保存修订历史 - PlanningID: {planning.PlanningID}, Reason: {reason}");

                // 1. 获取当前规划的最新版本号
                var existingRevisions = PlanningRevisionBusiness.GetByPlanningID(planning.PlanningID);
                int newVersion = 1;
                if (existingRevisions != null && existingRevisions.Count > 0)
                {
                    newVersion = existingRevisions.Max(r => r.RevisionVersion) + 1;
                }

                // 2. 序列化当前规划状态为JSON(作为修订前内容)
                var planningSnapshot = new
                {
                    planning.PlanningID,
                    planning.OriginalTask,
                    planning.PlanningStatus,
                    planning.CurrentStepIndex,
                    planning.TotalSteps,
                    Steps = planning.Steps,
                    Metadata = planning.Metadata,
                    planning.CreateTime,
                    planning.LastUpdateTime
                };
                string contentBefore = JsonConvert.SerializeObject(planningSnapshot, Formatting.Indented);

                // 3. 创建修订历史记录
                var revision = new PlanningRevisionInfo
                {
                    RevisionID = Guid.NewGuid().ToString(),
                    PlanningID = planning.PlanningID,
                    RevisionVersion = newVersion,
                    RevisionReason = reason ?? "未指定原因",
                    ContentBefore = contentBefore,
                    ContentAfter = null, // 修订后内容将在重新规划完成后更新
                    CreateTime = DateTime.Now
                };

                // 4. 保存到数据库
                string revisionId = PlanningRevisionBusiness.Add(revision);

                if (!string.IsNullOrEmpty(revisionId))
                {
                    LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                        $"[TaskPlanning] 修订历史保存成功 - RevisionID: {revisionId}, Version: {newVersion}, ContentLength: {contentBefore.Length}");
                }
                else
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING, $" 修订历史保存返回空ID - PlanningID: {planning.PlanningID}");
                }

            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.TASK_PLANNING, $" 保存修订历史失败 - PlanningID: {planning.PlanningID}, Reason: {reason}");
            }
        }

        /// <summary>
        /// 分析执行状态
        /// </summary>
        private ExecutionAnalysis AnalyzeExecutionStatus(TaskPlanning planning, ExecutionResult executionResult)
        {
            var analysis = new ExecutionAnalysis
            {
                CompletedSteps = new List<TaskStep>(),
                FailedSteps = new List<TaskStep>(),
                PendingSteps = new List<TaskStep>()
            };

            foreach (var step in planning.Steps)
            {
                if (step.StepStatus == StepStatus.Completed)
                {
                    analysis.CompletedSteps.Add(step);
                }
                else if (step.StepStatus == StepStatus.Failed)
                {
                    analysis.FailedSteps.Add(step);
                }
                else
                {
                    analysis.PendingSteps.Add(step);
                }
            }

            return analysis;
        }

        /// <summary>
        /// 构建重新规划提示词
        /// </summary>
        private string BuildReplanningPrompt(
            TaskPlanning currentPlanning,
            ExecutionAnalysis analysis,
            ReflectionResult reflectionResult,
            List<WorkflowConfigInfo> availableWorkflows,
            ClawAIData nodeData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# 任务重新规划");
            prompt.AppendLine();
            prompt.AppendLine("## 原始任务");
            prompt.AppendLine(currentPlanning.OriginalTask);
            prompt.AppendLine();

            if (!string.IsNullOrEmpty(currentPlanning.Goal))
            {
                prompt.AppendLine("## 任务目标");
                prompt.AppendLine(currentPlanning.Goal);
                prompt.AppendLine("请确保重新规划的步骤能够达成以上目标。");
                prompt.AppendLine();
            }

            // 完成度检查提示
            prompt.AppendLine("## ⚠ 完成度检查（重新规划前必须分析）");
            prompt.AppendLine("请对照原始任务分析当前产出的完成度：");
            prompt.AppendLine("- 原始任务是否要求生成N个结果？实际生成了几个？");
            prompt.AppendLine("- 原始任务要求的功能是否全部覆盖？");
            prompt.AppendLine("- 如果完成度不足，新规划必须补足缺失的部分");
            prompt.AppendLine();

            prompt.AppendLine("## 执行引擎约束（必须理解）");
            prompt.AppendLine("1. **一个步骤 = 一次调用**: 每个 step 只会被执行引擎执行一次，调用指定的 WorkFlow 一次");
            prompt.AppendLine("2. **不存在隐式循环**: 不存在\"循环\"、\"逐一\"、\"批量\"等多次执行语义");
            prompt.AppendLine("3. **多项任务必须展开为独立步骤**: 如果需要处理N个项，必须为每个项创建一个独立的步骤，每个步骤的 input 明确指定处理哪一项");
            prompt.AppendLine("4. **数据传递**: 步骤间通过 dependsOnStepIds 传递依赖，前置步骤输出自动作为 context 注入");
            prompt.AppendLine();

            prompt.AppendLine("## 当前执行情况");
            prompt.AppendLine($"- 总步骤: {currentPlanning.TotalSteps}");
            prompt.AppendLine($"- 已完成: {analysis.CompletedSteps.Count}");
            prompt.AppendLine($"- 失败: {analysis.FailedSteps.Count}");
            prompt.AppendLine($"- 未执行: {analysis.PendingSteps.Count}");
            prompt.AppendLine();

            // 已完成步骤 - 输出完整内容以便评估完成度
            if (analysis.CompletedSteps.Count > 0)
            {
                prompt.AppendLine("## 已完成步骤(保留)");
                foreach (var step in analysis.CompletedSteps)
                {
                    prompt.AppendLine($"### 步骤 {step.StepIndex}: {step.StepDescription}");
                    prompt.AppendLine($"- 预期输出: {step.ExpectedOutput ?? "未指定"}");
                    if (!string.IsNullOrEmpty(step.ActualOutput))
                    {
                        // 输出更完整的内容以便评估完成度，限制在500字符
                        var output = step.ActualOutput.Length > 500
                            ? step.ActualOutput.Substring(0, 500) + "..."
                            : step.ActualOutput;
                        prompt.AppendLine($"- 实际输出({step.ActualOutput.Length}字符): {output}");
                    }
                    else
                    {
                        prompt.AppendLine($"- 实际输出: (空)");
                    }
                    prompt.AppendLine($"- 完成度自评: 该步骤的预期输出是否真正达成？");
                    prompt.AppendLine();
                }
            }

            // 失败步骤
            if (analysis.FailedSteps.Count > 0)
            {
                prompt.AppendLine("## 失败步骤(需要重新规划)");
                foreach (var step in analysis.FailedSteps)
                {
                    prompt.AppendLine($"- 步骤 {step.StepIndex}: {step.StepDescription}");
                    if (!string.IsNullOrEmpty(step.ErrorMessage))
                    {
                        prompt.AppendLine($"  错误: {step.ErrorMessage}");
                    }
                }
                prompt.AppendLine();
            }

            // 反思建议
            prompt.AppendLine("## 反思评估");
            prompt.AppendLine($"- 原因: {reflectionResult.Reason}");
            prompt.AppendLine($"- 推理: {reflectionResult.Reasoning}");
            if (!string.IsNullOrEmpty(reflectionResult.RefinedPrompt))
            {
                prompt.AppendLine($"- 改进建议: {reflectionResult.RefinedPrompt}");
            }
            if (reflectionResult.TaskAnalysis?.MissingCapabilities != null && reflectionResult.TaskAnalysis.MissingCapabilities.Count > 0)
            {
                prompt.AppendLine($"- 缺失项: {string.Join(", ", reflectionResult.TaskAnalysis.MissingCapabilities)}");
            }
            prompt.AppendLine();

            // 可用 WorkFlow
            prompt.AppendLine("## 可用的 WorkFlow 工具");
            foreach (var workFlow in availableWorkflows)
            {
                prompt.AppendLine($"- **{workFlow.name}** (ID: {workFlow.workflowId})");
                prompt.AppendLine($"  描述: {workFlow.description}");
            }
            prompt.AppendLine();

            prompt.AppendLine("## 你的任务");
            prompt.AppendLine("请基于以上信息重新规划剩余任务:");
            prompt.AppendLine("1. **先评估完成度**: 对照原始任务，检查已完成步骤的产出是否真正满足了原始任务的量化要求");
            prompt.AppendLine("2. 保留所有已完成的步骤");
            prompt.AppendLine("3. 针对失败步骤,分析原因并调整策略");
            prompt.AppendLine("4. 为未完成的任务生成新的执行步骤（每项任务必须创建独立步骤，不要合并多项到一个步骤）");
            prompt.AppendLine("5. 确保新步骤能够避免之前的错误，并补足缺失的产出");
            prompt.AppendLine();
            prompt.AppendLine("### 输出格式 (JSON):");
            prompt.AppendLine("```json");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"newSteps\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"stepDescription\": \"步骤描述\",");
            prompt.AppendLine("      \"stepType\": \"workflow_call\" | \"llm_reasoning\",");
            prompt.AppendLine("      \"assignedWorkflowIds\": [\"workflow_id\"],");
            prompt.AppendLine("      \"dependsOnStepIds\": [已完成的步骤索引],");
            prompt.AppendLine("      \"inputs\": [{\"varname\": \"prompt\", \"value\": \"具体指令\"}],");
            prompt.AppendLine("      \"expectedOutput\": \"预期输出\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");
            prompt.AppendLine("```");

            return prompt.ToString();
        }

        /// <summary>
        /// 解析重新规划响应
        /// </summary>
        private List<TaskStep> ParseReplanningResponse(
            string response,
            TaskPlanning currentPlanning,
            ExecutionAnalysis analysis)
        {
            try
            {
                string jsonContent = ExtractJsonFromResponse(response);
                var replanData = JsonConvert.DeserializeObject<ReplanningResponseData>(jsonContent);
                
                if (replanData == null || replanData.newSteps == null || replanData.newSteps.Count == 0)
                {
                    _logger.LogWarning("[TaskPlanning] 重新规划响应为空,返回空列表");
                    return new List<TaskStep>();
                }

                var newSteps = new List<TaskStep>();
                int startIndex = analysis.CompletedSteps.Count + 1;
                
                for (int i = 0; i < replanData.newSteps.Count; i++)
                {
                    var stepData = replanData.newSteps[i];
                    var step = new TaskStep
                    {
                        PlanningID = currentPlanning.PlanningID,
                        StepIndex = startIndex + i,
                        StepDescription = stepData.stepDescription,
                        StepType = ParseStepType(stepData.stepType),
                        AssignedWorkflowIds = stepData.assignedWorkflowIds ?? new List<string>(),
                        ExpectedOutput = stepData.expectedOutput,
                        StepStatus = StepStatus.Pending,
                        StepInputs = stepData.inputs?.Select(inp => new Inputs { varname = inp.varname ?? inp.name, value = inp.value }).ToList() ?? new List<Inputs>()
                    };
                    
                    newSteps.Add(step);
                }

                return newSteps;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TaskPlanning] 解析重新规划响应失败");
                return new List<TaskStep>();
            }
        }

        /// <summary>
        /// 合并已完成步骤和新步骤
        /// </summary>
        private void MergeSteps(
            TaskPlanning planning,
            List<TaskStep> completedSteps,
            List<TaskStep> newSteps)
        {
            // 清空现有步骤
            planning.Steps.Clear();
            
            // 添加已完成步骤
            planning.Steps.AddRange(completedSteps);
            
            // 添加新步骤
            planning.Steps.AddRange(newSteps);
            
            // 重新编号
            for (int i = 0; i < planning.Steps.Count; i++)
            {
                planning.Steps[i].StepIndex = i + 1;
            }
        }

        #endregion

        #region 内部数据类
        
        /// <summary>
        /// 任务复杂度枚举
        /// </summary>
        private enum TaskComplexity
        {
            Greeting, // 问候语/简单对话:无需WorkFlow,直接LLM回答
            Simple,   // 简单任务:单一查询、介绍类
            Medium,   // 中等任务:需要2-5步
            Complex   // 复杂任务:需要多步骤协调
        }

        /// <summary>
        /// 规划响应数据结构
        /// </summary>
        private class PlanningResponseData
        {
            public string strategy { get; set; }
            public int confidence { get; set; }
            public int estimatedDuration { get; set; }
            public List<StepResponseData> steps { get; set; }
        }

        private class StepResponseData
        {
            public int stepIndex { get; set; }
            public string stepDescription { get; set; }
            public string stepType { get; set; }
            public List<string> assignedWorkflowIds { get; set; }
            public List<string> dependsOnStepIds { get; set; }
            public string expectedOutput { get; set; }
            public List<InputData> inputs { get; set; }
        }

        /// <summary>
        /// 输入参数数据结构
        /// </summary>
        private class InputData
        {
            public string varname { get; set; }
            public string name { get; set; }
            public string value { get; set; }
        }

        /// <summary>
        /// 重新规划响应数据结构
        /// </summary>
        private class ReplanningResponseData
        {
            public List<StepResponseData> newSteps { get; set; }
        }

        /// <summary>
        /// 执行分析结果
        /// </summary>
        private class ExecutionAnalysis
        {
            public List<TaskStep> CompletedSteps { get; set; }
            public List<TaskStep> FailedSteps { get; set; }
            public List<TaskStep> PendingSteps { get; set; }
        }

        /// <summary>
        /// 从步骤数据中提取输入参数
        /// </summary>
        private List<Inputs> ExtractStepInputs(StepResponseData stepData)
        {
            var inputs = new List<Inputs>();
            
            try
            {
                // 从 stepData 中提取 inputs 字段
                if (stepData.inputs != null && stepData.inputs.Count > 0)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 步骤 {stepData.stepIndex} 提取到 {stepData.inputs.Count} 个输入参数");
                    
                    foreach (var input in stepData.inputs)
                    {
                        var varname = input.varname ?? input.name ?? "";
                        var value = input.value ?? "";
                        
                        inputs.Add(new Inputs
                        {
                            varname = varname,
                            value = value
                        });
                        
                        _logger.LogDebug($"[TaskPlanning]   - {varname}: {(value.Length > 50 ? value.Substring(0, 50) + "..." : value)}");
                    }
                }
                else
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING, $" 步骤 {stepData.stepIndex} 没有输入参数 (inputs 字段为 null 或空)");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING, $" 步骤 {stepData.stepIndex} 提取输入参数失败");
            }
            
            return inputs;
        }

        #endregion

        #region 动态步骤应用

        /// <summary>
        /// 应用建议的步骤到任务规划（动态添加步骤）
        /// </summary>
        public async Task ApplySuggestedStepsAsync(
            TaskPlanning taskPlanning,
            List<SuggestedStep> suggestedSteps,
            List<WorkflowConfigInfo> availableWorkflows,
            ConcurrentQueue<string> Logs)
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 应用建议步骤 - 数量: {suggestedSteps.Count}");

            if (suggestedSteps == null || suggestedSteps.Count == 0)
            {
                Logs.Enqueue("  没有建议的步骤需要添加");
                return;
            }

            int addedCount = 0;

            // 按优先级排序
            foreach (var suggested in suggestedSteps.OrderByDescending(s => s.Priority))
            {
                try
                {
                    // 1. 匹配最合适的 WorkFlow
                    var matchedWorkflow = MatchBestWorkflow(
                        suggested.SuggestedWorkflowIds,
                        availableWorkflows);

                    if (matchedWorkflow == null)
                    {
                        Logs.Enqueue($"  ⚠ 跳过步骤：{suggested.StepDescription}");
                        Logs.Enqueue($"     原因：未找到合适的 WorkFlow");
                        LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING, $" 未找到匹配的 WorkFlow - 建议ID: {string.Join(", ", suggested.SuggestedWorkflowIds)}");
                        continue;
                    }

                    // 2. 创建新步骤
                    var newStep = new TaskStep
                    {
                        PlanningID = taskPlanning.PlanningID,
                        StepIndex = taskPlanning.Steps.Count + 1,
                        StepDescription = suggested.StepDescription,
                        StepType = suggested.StepType,
                        AssignedWorkflowIds = new List<string> { matchedWorkflow.workflowId },
                        StepStatus = StepStatus.Pending,
                        ExpectedOutput = suggested.ExpectedOutput ?? ""
                    };

                    // 3. 处理依赖关系
                    if (suggested.DependsOnStepIndices != null && suggested.DependsOnStepIndices.Count > 0)
                    {
                        foreach (var depIndex in suggested.DependsOnStepIndices)
                        {
                            var depStep = taskPlanning.Steps.FirstOrDefault(s => s.StepIndex == depIndex);
                            if (depStep != null)
                            {
                                newStep.DependsOnStepIds.Add(depStep.StepID);
                            }
                        }
                    }

                    // 4. 处理输入参数
                    if (suggested.InputSuggestions != null && suggested.InputSuggestions.Count > 0)
                    {
                        foreach (var inputSuggestion in suggested.InputSuggestions)
                        {
                            if (inputSuggestion.ExtractFromPreviousStep && inputSuggestion.SourceStepIndex.HasValue)
                            {
                                // 从前置步骤提取参数
                                var sourceStep = taskPlanning.Steps.FirstOrDefault(
                                    s => s.StepIndex == inputSuggestion.SourceStepIndex.Value);

                                if (sourceStep != null && !string.IsNullOrEmpty(sourceStep.ActualOutput))
                                {
                                    try
                                    {
                                        // 使用 ResultParserService 提取数据
                                        var extractedValue = await _resultParserService.ExtractJsonAsync(
                                            sourceStep.ActualOutput,
                                            inputSuggestion.Value);

                                        newStep.StepInputs.Add(new Inputs
                                        {
                                            varname = inputSuggestion.VarName,
                                            value = extractedValue?.ToString() ?? inputSuggestion.Value
                                        });

                                        LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 从步骤 {inputSuggestion.SourceStepIndex} 提取参数: {inputSuggestion.VarName}");
                                    }
                                    catch (Exception ex)
                                    {
                                        LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING, " 参数提取失败，使用原始值");
                                        newStep.StepInputs.Add(new Inputs
                                        {
                                            varname = inputSuggestion.VarName,
                                            value = inputSuggestion.Value
                                        });
                                    }
                                }
                                else
                                {
                                    // 源步骤未完成或无输出，使用建议的值
                                    newStep.StepInputs.Add(new Inputs
                                    {
                                        varname = inputSuggestion.VarName,
                                        value = inputSuggestion.Value
                                    });
                                }
                            }
                            else
                            {
                                // 直接使用建议的值
                                newStep.StepInputs.Add(new Inputs
                                {
                                    varname = inputSuggestion.VarName,
                                    value = inputSuggestion.Value
                                });
                            }
                        }
                    }

                    // 5. 添加到规划
                    taskPlanning.Steps.Add(newStep);
                    taskPlanning.TotalSteps++;
                    addedCount++;

                    Logs.Enqueue($"  ✓ 添加新步骤 {newStep.StepIndex}: {newStep.StepDescription}");
                    Logs.Enqueue($"     WorkFlow: {matchedWorkflow.name}");
                    Logs.Enqueue($"     优先级: {suggested.Priority}");
                    if (!string.IsNullOrEmpty(suggested.Reason))
                    {
                        Logs.Enqueue($"     原因: {suggested.Reason}");
                    }
                    if (newStep.DependsOnStepIds.Count > 0)
                    {
                        Logs.Enqueue($"     依赖步骤: {string.Join(", ", suggested.DependsOnStepIndices)}");
                    }

                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 成功添加步骤 {newStep.StepIndex}: {newStep.StepDescription}");
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError(_logger, ClawLogModules.TASK_PLANNING, $" 添加建议步骤失败: {suggested.StepDescription}");
                    Logs.Enqueue($"  ✗ 添加步骤失败: {suggested.StepDescription}");
                    Logs.Enqueue($"     错误: {ex.Message}");
                }
            }

            // 6. 保存修订历史
            if (addedCount > 0)
            {
                try
                {
                    await SavePlanningRevisionAsync(
                        taskPlanning,
                        $"动态添加 {addedCount} 个步骤");

                    Logs.Enqueue($"  ✓ 已保存规划修订历史");
                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 成功添加 {addedCount} 个步骤到规划");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TaskPlanning] 保存规划修订失败");
                }
            }
        }

        /// <summary>
        /// 匹配最合适的 WorkFlow
        /// </summary>
        private WorkflowConfigInfo MatchBestWorkflow(
            List<string> suggestedIds,
            List<WorkflowConfigInfo> availableWorkflows)
        {
            if (suggestedIds == null || suggestedIds.Count == 0)
            {
                return null;
            }

            // 1. 优先使用建议的 WorkFlow ID（精确匹配）
            foreach (var id in suggestedIds)
            {
                var workflow = availableWorkflows.FirstOrDefault(
                    w => w.workflowId == id && w.enabled);
                if (workflow != null)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 精确匹配 WorkFlow: {workflow.name} ({workflow.workflowId})");
                    return workflow;
                }
            }

            // 2. 如果没有精确匹配，尝试模糊匹配（基于名称）
            foreach (var id in suggestedIds)
            {
                var workflow = availableWorkflows.FirstOrDefault(
                    w => w.enabled && (w.name?.Contains(id, StringComparison.OrdinalIgnoreCase) == true ||
                                       w.description?.Contains(id, StringComparison.OrdinalIgnoreCase) == true));
                if (workflow != null)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 模糊匹配 WorkFlow: {workflow.name} ({workflow.workflowId})");
                    return workflow;
                }
            }

            // 3. 未找到匹配
            return null;
        }

        /// <summary>
        /// 保存规划修订历史
        /// </summary>
        private async Task SavePlanningRevisionAsync(TaskPlanning taskPlanning, string revisionReason)
        {
            try
            {
                // 增加修订次数
                taskPlanning.Metadata.RevisionCount++;
                taskPlanning.LastUpdateTime = DateTime.Now;

                // 转换为 TaskPlanningInfo
                var planningInfo = ConvertToTaskPlanningInfo(taskPlanning);

                // 保存到数据库
                bool success = TaskPlanningBusiness.Update(planningInfo);

                if (success)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_PLANNING, $" 保存规划修订成功 - 原因: {revisionReason}, 修订次数: {taskPlanning.Metadata.RevisionCount}");
                }
                else
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.TASK_PLANNING, $" 保存规划修订失败 - 原因: {revisionReason}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TaskPlanning] 保存规划修订异常");
                // 不抛出异常，避免中断流程
            }

        }

        /// <summary>
        /// 将 TaskPlanning 转换为 TaskPlanningInfo
        /// </summary>
        private TaskPlanningInfo ConvertToTaskPlanningInfo(TaskPlanning taskPlanning)
        {
            var planningInfo = new TaskPlanningInfo
            {
                PlanningID = taskPlanning.PlanningID,
                AppID = taskPlanning.AppID,
                SessionID = taskPlanning.SessionID,
                MemberID = taskPlanning.MemberID,
                NodeID = taskPlanning.NodeID,
                ProcessesID = taskPlanning.ProcessesID,
                OriginalTask = taskPlanning.OriginalTask,
                PlanningStatus = taskPlanning.PlanningStatus.ToString(),
                CurrentStepIndex = taskPlanning.CurrentStepIndex,
                TotalSteps = taskPlanning.TotalSteps,
                Strategy = taskPlanning.Metadata.Strategy,
                Confidence = taskPlanning.Metadata.Confidence,
                EstimatedDuration = taskPlanning.Metadata.EstimatedDuration,
                ActualDuration = taskPlanning.Metadata.ActualDuration,
                RevisionCount = taskPlanning.Metadata.RevisionCount,
                CreateTime = taskPlanning.CreateTime,
                LastUpdateTime = taskPlanning.LastUpdateTime
            };

            return planningInfo;
        }

        #endregion
    }
}
