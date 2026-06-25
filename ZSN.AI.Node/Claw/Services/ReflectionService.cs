using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Linq;
using System.Threading.Tasks;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Exceptions;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Service.Helpers;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// 反思服务实现
    /// </summary>
    public class ReflectionService : IReflectionService
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ReflectionService> _logger;
        private readonly ClawAIOptions _options;

        public ReflectionService(
            IChatService chatService,
            ILogger<ReflectionService> logger,
            IOptions<ClawAIOptions> options)
        {
            _chatService = chatService;
            _logger = logger;
            _options = options?.Value ?? new ClawAIOptions();
        }

        public async Task<ReflectionResult> ReflectOnExecutionAsync(
            ClawAIData nodeData,
            LargeModelConfig reflectionModelConfig,
            TaskPlanning taskPlanning,
            ExecutionResult executionResult,
            string originalTask,
            int iteration,
            IProgress<string> progress)
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" 开始反思 - Iteration: {iteration}");
            
            try
            {
                // ===== 快速完成: 所有步骤成功完成 + 无失败 + 所有步骤都有有效结果 =====
                // 异步触发的 WorkFlow 依赖反射快速返回才能流转到下一个节点
                if (executionResult.AllStepsCompleted && executionResult.FailedSteps == 0)
                {
                    var completedSteps = taskPlanning.Steps
                        .Where(s => s.StepStatus == StepStatus.Completed)
                        .ToList();

                    // 严格验证: 所有已完成步骤都必须有有效结果
                    bool allStepsHaveResults = completedSteps.Count > 0 && 
                                               completedSteps.All(s => !string.IsNullOrEmpty(s.ExecutionResult) && s.ExecutionResult.Length > 10);
                    
                    if (allStepsHaveResults)
                    {
                        // 计算平均质量分(如果有)
                        int avgQuality = completedSteps.Any(s => s.QualityScore.HasValue)
                            ? (int)completedSteps.Where(s => s.QualityScore.HasValue)
                                .Average(s => s.QualityScore.Value)
                            : 85;

                        LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" 所有步骤已完成且都有有效结果({completedSteps.Count}个) - 跳过LLM反思");

                        return new ReflectionResult
                        {
                            OverallQuality = avgQuality,
                            CompletenessScore = 95,
                            AccuracyScore = avgQuality,
                            Reasoning = "所有步骤已成功完成且都有有效结果,基于步骤质量分计算",
                            Action = ReflectionAction.Complete,
                            Reason = "所有步骤已完成且无失败,所有步骤都有有效结果",
                            FinalAnswer = CombineStepResults(taskPlanning),
                            ResolvedPrompt = null  // 快速路径，未使用LLM
                        };
                    }
                    else
                    {
                        LoggerHelper.LogWarning(_logger, ClawLogModules.REFLECTION, 
                            $" 所有步骤已完成但部分步骤无有效结果 - 继续LLM反思验证");
                    }
                }

                // ===== 迭代次数保护: 达到最大迭代次数时强制完成 =====
                if (iteration >= nodeData.workFlowLoopConfig.maxIterations)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, " 已达最大迭代次数 - 强制完成");

                    return new ReflectionResult
                    {
                        OverallQuality = _options.Reflection.MaxIterationOverallQuality,
                        CompletenessScore = _options.Reflection.MaxIterationCompletenessScore,
                        AccuracyScore = _options.Reflection.MaxIterationAccuracyScore,
                        Reasoning = "已达最大迭代次数限制",
                        Action = ReflectionAction.Complete,
                        Reason = $"迭代次数已达最大值{nodeData.workFlowLoopConfig.maxIterations},强制完成",
                        FinalAnswer = CombineStepResults(taskPlanning),
                        ResolvedPrompt = null  // 快速路径，未使用LLM
                    };
                }

                // ===== 存在失败步骤 或 步骤未全部完成: 使用 LLM 进行反思评估 =====
                // LLM 会根据 ReflectionPrompt 评估完成度、决定是否 replan/retry
                
                // 使用传入的已初始化的反思模型配置
                LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" 使用模型: {reflectionModelConfig.Model.ModelName} (ID: {reflectionModelConfig.Id})");
                
                // 1. 构建反思提示词
                string reflectionPrompt = BuildReflectionPrompt(
                    nodeData, taskPlanning, executionResult, originalTask, iteration);

                LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" 反思提示词长度: {reflectionPrompt.Length} 字符");

                // 2. 调用 LLM 进行评估(流式输出)
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("你是一个专业的任务执行评估员,擅长分析任务执行质量并给出改进建议。");
                chatHistory.AddUserMessage(reflectionPrompt);

                var responseBuilder = new System.Text.StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    reflectionModelConfig, 
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

                LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" LLM 响应长度: {response?.Length ?? 0} 字符");

                // 3. 解析反思结果
                var result = ParseReflectionResponse(response, taskPlanning, executionResult);
                
                // 设置已替换的提示词（用于日志记录）
                result.ResolvedPrompt = reflectionPrompt;

                // 4. 硬编码保护逻辑:防止过度重新规划
                if (result.Action == ReflectionAction.Replan)
                {
                    bool shouldBlockReplan = false;
                    string blockReason = "";

                    // 计算任务完成度
                    var totalWorkflowSteps = taskPlanning.Steps.Count(s => s.StepType == StepType.WorkflowCall);
                    var completedWorkflowSteps = taskPlanning.Steps.Count(s => s.StepType == StepType.WorkflowCall && 
                                                                                s.StepStatus == StepStatus.Completed &&
                                                                                !string.IsNullOrEmpty(s.ExecutionResult));
                    
                    // 规则0: 所有WorkflowCall步骤都完成且有结果 + 完成度>=80% 时禁止重新规划
                    if (totalWorkflowSteps > 0 && completedWorkflowSteps == totalWorkflowSteps)
                    {
                        var completionRate = (double)completedWorkflowSteps / totalWorkflowSteps * 100;
                        if (completionRate >= 80)
                        {
                            shouldBlockReplan = true;
                            blockReason = $"所有WorkflowCall步骤已完成({completedWorkflowSteps}/{totalWorkflowSteps}),完成度{completionRate:F0}%,禁止重新规划";
                        }
                    }
                    // 规则1: 简单任务(步骤数 <= 配置值)且所有步骤都完成时禁止重新规划
                    else if (taskPlanning.TotalSteps <= _options.Reflection.NoReplanStepThreshold &&
                             taskPlanning.Steps.All(s => s.StepStatus == StepStatus.Completed || s.StepStatus == StepStatus.Skipped))
                    {
                        shouldBlockReplan = true;
                        blockReason = "简单任务所有步骤已完成,禁止重新规划";
                    }
                    // 规则2: 迭代次数 >= 配置值 时禁止重新规划
                    else if (iteration >= _options.Reflection.NoReplanIterationThreshold)
                    {
                        shouldBlockReplan = true;
                        blockReason = $"迭代次数已达 {iteration},禁止继续重新规划";
                    }

                    if (shouldBlockReplan)
                    {
                        LoggerHelper.LogWarning(_logger, ClawLogModules.REFLECTION, " 拦截重新规划请求 - 原因: {blockReason}");
                        
                        // 强制改为 Complete
                        result.Action = ReflectionAction.Complete;
                        result.Reason = $"[系统拦截] {blockReason}。{result.Reason}";
                        
                        // 如果没有最终答案,从已完成的步骤中获取执行结果
                        if (string.IsNullOrEmpty(result.FinalAnswer))
                        {
                            var completedSteps = taskPlanning.Steps
                                .Where(s => s.StepStatus == StepStatus.Completed && !string.IsNullOrEmpty(s.ExecutionResult))
                                .ToList();
                            
                            if (completedSteps.Count > 0)
                            {
                                // 使用最后一个完成步骤的结果
                                result.FinalAnswer = completedSteps.Last().ExecutionResult;
                            }
                            else
                            {
                                result.FinalAnswer = "任务已完成";
                            }
                        }
                    }
                }

                LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                    $"[Reflection] 反思完成 - Action: {result.Action}, Quality: {result.OverallQuality}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Reflection] 反思失败,使用默认策略");
                
                // 失败时使用简单判断
                return CreateFallbackReflectionResult(taskPlanning, executionResult);
            }
        }

        public async Task<int> EvaluateStepQualityAsync(TaskStep step, ClawAIData nodeData, LargeModelConfig reflectionModelConfig, IProgress<string> progress)
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" 评估步骤质量 - StepIndex: {step.StepIndex}");

            try
            {
                // ===== 新增: 基于规则的快速评估 =====
                // 1. WorkflowCall类型步骤,如果完成且有输出,直接给高分
                if (step.StepType == StepType.WorkflowCall && 
                    step.StepStatus == StepStatus.Completed && 
                    !string.IsNullOrEmpty(step.ActualOutput))
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, " WorkflowCall步骤快速评估 - 给予高分90");
                    return 90; // WorkFlow是成熟工作流,信任其输出质量
                }
                
                // 2. 简单步骤(无依赖)且完成,快速评估
                if ((step.DependsOnStepIds == null || step.DependsOnStepIds.Count == 0) &&
                    step.StepStatus == StepStatus.Completed &&
                    !string.IsNullOrEmpty(step.ActualOutput) &&
                    step.ActualOutput.Length > 50) // 有足够长度的输出
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, " 简单步骤快速评估 - 给予中高分85");
                    return 85;
                }
                
                // 3. 失败步骤,直接低分
                if (step.StepStatus == StepStatus.Failed)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, " 失败步骤快速评估 - 给予低分30");
                    return 30;
                }
                
                // 4. 无输出步骤,直接中低分
                if (string.IsNullOrEmpty(step.ActualOutput))
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, " 无输出步骤快速评估 - 给予中低分40");
                    return 40;
                }
                
                // ===== 新增: 仅对关键步骤使用LLM评估 =====
                // 只有以下情况才调用LLM:
                // - LLMReasoning类型(需要评估推理质量)
                // - Synthesis类型(需要评估综合质量)
                // - Validation类型(需要评估验证准确性)
                bool needsLLMEvaluation = step.StepType == StepType.LLMReasoning ||
                                          step.StepType == StepType.Synthesis ||
                                          step.StepType == StepType.Validation;
                
                if (!needsLLMEvaluation)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, " 步骤类型{step.StepType}使用规则评估");
                    return CalculateFallbackQualityScore(step);
                }
                
                // 原有的LLM评估逻辑...
                LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" 关键步骤使用LLM评估 - StepType: {step.StepType}");
                
                // 1. 构建评估提示词
                string evaluationPrompt = BuildStepEvaluationPrompt(step);

                // 2. 调用 LLM 进行评估(流式输出)
                LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" 使用模型: {reflectionModelConfig.Model.ModelName} (ID: {reflectionModelConfig.Id})");
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("你是一个专业的任务步骤质量评估员,擅长评估执行结果的完整性和准确性。");
                chatHistory.AddUserMessage(evaluationPrompt);

                var responseBuilder = new System.Text.StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    reflectionModelConfig, 
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

                // 3. 解析质量分数
                int qualityScore = ParseQualityScore(response);

                LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" 步骤质量评分: {qualityScore}");

                return qualityScore;
            }
            catch (LLMException llmEx) when (llmEx.IsFatal)
            {
                // 致命 LLM 错误：不降级到启发式判断，向上抛出避免后续继续调用坏掉的 LLM。
                _logger.LogError(llmEx, $"[Reflection] 评估步骤质量失败（LLM 致命错误） - StepIndex: {step.StepIndex}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Reflection] 评估步骤质量失败 - StepIndex: {step.StepIndex}");
                
                // 失败时使用简单规则判断
                return CalculateFallbackQualityScore(step);
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 构建反思提示词
        /// </summary>
        private string BuildReflectionPrompt(
            ClawAIData nodeData,
            TaskPlanning taskPlanning,
            ExecutionResult executionResult,
            string originalTask,
            int iteration)
        {
            var promptTemplate = nodeData.reflectionConfig.reflectionPromptTemplate;

            // 替换原始任务
            promptTemplate = promptTemplate.Replace("{{originalTask}}", originalTask);

            // 替换任务目标（从规划中提取，若无则使用原始任务）
            string goal = !string.IsNullOrEmpty(taskPlanning.Goal) ? taskPlanning.Goal : originalTask;
            promptTemplate = promptTemplate.Replace("{{goal}}", goal);

            // 替换迭代次数
            promptTemplate = promptTemplate.Replace("{{iteration}}", iteration.ToString());

            // 构建当前进度
            var currentProgress = new System.Text.StringBuilder();
            currentProgress.AppendLine($"迭代次数: {iteration}");
            currentProgress.AppendLine($"总步骤: {taskPlanning.TotalSteps}");
            currentProgress.AppendLine($"已完成: {executionResult.CompletedSteps}");
            currentProgress.AppendLine($"失败: {executionResult.FailedSteps}");
            currentProgress.AppendLine($"跳过: {executionResult.SkippedSteps}");
            promptTemplate = promptTemplate.Replace("{{currentProgress}}", currentProgress.ToString());

            // 构建步骤详情
            var stepDetails = new System.Text.StringBuilder();
            foreach (var step in taskPlanning.Steps)
            {
                stepDetails.AppendLine($"\n步骤 {step.StepIndex}: {step.StepDescription}");
                stepDetails.AppendLine($"  状态: {step.StepStatus}");
                stepDetails.AppendLine($"  类型: {step.StepType}");
                
                if (!string.IsNullOrEmpty(step.ActualOutput))
                {
                    var output = step.ActualOutput.Length > 500
                        ? step.ActualOutput.Substring(0, 500) + "..."
                        : step.ActualOutput;
                    stepDetails.AppendLine($"  输出: {output}");
                }
                
                if (!string.IsNullOrEmpty(step.ErrorMessage))
                {
                    stepDetails.AppendLine($"  错误: {step.ErrorMessage}");
                }
                
                if (step.QualityScore.HasValue)
                {
                    stepDetails.AppendLine($"  质量分: {step.QualityScore.Value}");
                }
            }
            promptTemplate = promptTemplate.Replace("{{stepDetails}}", stepDetails.ToString());

            // 添加明确的完成条件检查
            var completionCheck = new System.Text.StringBuilder();
            completionCheck.AppendLine("\n## ⚠️ 重要：完成条件检查");
            completionCheck.AppendLine($"- 总步骤数: {taskPlanning.TotalSteps}");
            completionCheck.AppendLine($"- 已完成步骤: {executionResult.CompletedSteps}");
            completionCheck.AppendLine($"- 待执行步骤: {taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Pending)}");
            completionCheck.AppendLine($"- 所有步骤完成: {(executionResult.AllStepsCompleted ? "是" : "否")}");
            completionCheck.AppendLine("\n**关键规则**:");
            completionCheck.AppendLine("- 只有当「所有步骤完成」为「是」时，才能返回 action=\"Complete\"");
            completionCheck.AppendLine("- 如果还有待执行步骤，必须返回 action=\"ContinueExecution\"");
            completionCheck.AppendLine("- 不要因为部分步骤完成就提前结束任务！");
            
            promptTemplate += completionCheck.ToString();

            return promptTemplate;
        }

        /// <summary>
        /// 解析反思响应
        /// </summary>
        private ReflectionResult ParseReflectionResponse(
            string response,
            TaskPlanning taskPlanning,
            ExecutionResult executionResult)
        {
            try
            {
                // 提取 JSON 部分
                string jsonContent = ExtractJsonFromResponse(response);
                
                // 解析 JSON
                var reflectionData = Newtonsoft.Json.JsonConvert.DeserializeObject<ReflectionResponseData>(jsonContent);
                
                if (reflectionData == null)
                {
                    throw new Exception("反思数据解析失败");
                }

                var result = new ReflectionResult
                {
                    OverallQuality = reflectionData.overallQuality,
                    CompletenessScore = reflectionData.completenessScore,
                    AccuracyScore = reflectionData.accuracyScore,
                    Reasoning = reflectionData.reasoning ?? "",
                    Action = ParseReflectionAction(reflectionData.action),
                    Reason = reflectionData.reason ?? "",
                    FinalAnswer = reflectionData.finalAnswer ?? ""
                };

                // 当 action=Complete 时，直接使用步骤原始输出作为 FinalAnswer，
                // 避免LLM在finalAnswer中添加多余的总结性标题（如"## 使用工作流查询..."）
                if (result.Action == ReflectionAction.Complete)
                {
                    result.FinalAnswer = CombineStepResults(taskPlanning);
                }

                // 解析重试步骤索引
                if (result.Action == ReflectionAction.RetryStep && reflectionData.retryStepIndex.HasValue && reflectionData.retryStepIndex.Value > 0)
                {
                    result.RetryStepIndex = reflectionData.retryStepIndex.Value;
                }

                // 解析优化提示词
                if (!string.IsNullOrEmpty(reflectionData.refinedPrompt))
                {
                    result.RefinedPrompt = reflectionData.refinedPrompt;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reflection] 解析反思响应失败");
                return CreateFallbackReflectionResult(taskPlanning, executionResult);
            }
        }

        /// <summary>
        /// 从响应中提取 JSON
        /// </summary>
        private string ExtractJsonFromResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                throw new Exception("响应内容为空");
            }

            string jsonContent = null;

            // 尝试提取 ```json ... ``` 中的内容
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                response, @"```json\s*(\{[\s\S]*?\})\s*```",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (jsonMatch.Success)
            {
                jsonContent = jsonMatch.Groups[1].Value.Trim();
            }
            else
            {
                // 尝试提取 ``` ... ``` 中的内容
                jsonMatch = System.Text.RegularExpressions.Regex.Match(
                    response, @"```\s*(\{[\s\S]*?\})\s*```");

                if (jsonMatch.Success)
                {
                    jsonContent = jsonMatch.Groups[1].Value.Trim();
                }
                else
                {
                    // 尝试直接查找 JSON 对象
                    jsonMatch = System.Text.RegularExpressions.Regex.Match(
                        response, @"(\{[\s\S]*\})");

                    if (jsonMatch.Success)
                    {
                        jsonContent = jsonMatch.Groups[1].Value.Trim();
                    }
                }
            }

            if (string.IsNullOrEmpty(jsonContent))
            {
                throw new Exception("无法从响应中提取 JSON 内容");
            }

            // 清理 JSON 内容
            jsonContent = CleanJsonContent(jsonContent);

            return jsonContent;
        }

        /// <summary>
        /// 清理 JSON 内容，移除常见的 LLM 生成问题
        /// </summary>
        private string CleanJsonContent(string json)
        {
            // 移除单行注释 //（但要小心不要移除 URL 中的 //）
            json = System.Text.RegularExpressions.Regex.Replace(json, @"^\s*//.*$", "", System.Text.RegularExpressions.RegexOptions.Multiline);

            // 移除多行注释 /* */
            json = System.Text.RegularExpressions.Regex.Replace(json, @"/\*[\s\S]*?\*/", "");

            // 移除尾随逗号（如 "item": 1, 变成 "item": 1）
            json = System.Text.RegularExpressions.Regex.Replace(json, @",\s*([}\]])", "$1");

            // 移除属性值中的 markdown 加粗标记 **text**
            json = System.Text.RegularExpressions.Regex.Replace(json, @"\*\*([^*]+)\*\*", "$1");

            // 处理字符串值中的未转义换行符
            // 这是一个简单的方法，更健壮的方法需要逐字符解析
            // 暂时不处理，因为可能会破坏已正确转义的内容

            return json.Trim();
        }

        /// <summary>
        /// 解析反思行动
        /// </summary>
        private ReflectionAction ParseReflectionAction(string action)
        {
            if (string.IsNullOrEmpty(action))
            {
                return ReflectionAction.ContinueExecution;
            }

            action = action.ToLower().Replace("_", "").Replace("-", "");

            if (action.Contains("complete"))
                return ReflectionAction.Complete;
            if (action.Contains("retry"))
                return ReflectionAction.RetryStep;
            if (action.Contains("replan"))
                return ReflectionAction.Replan;
            if (action.Contains("fail"))
                return ReflectionAction.Fail;
            if (action.Contains("continue"))
                return ReflectionAction.ContinueExecution;

            return ReflectionAction.ContinueExecution;
        }

        /// <summary>
        /// 创建回退反思结果(简单规则判断)
        /// </summary>
        private ReflectionResult CreateFallbackReflectionResult(
            TaskPlanning taskPlanning,
            ExecutionResult executionResult)
        {
            var completedSteps = taskPlanning.Steps.Count(s => s.StepStatus == StepStatus.Completed);
            var totalSteps = taskPlanning.TotalSteps;

            var result = new ReflectionResult
            {
                OverallQuality = totalSteps > 0 ? completedSteps * 100 / totalSteps : 0,
                CompletenessScore = totalSteps > 0 ? completedSteps * 100 / totalSteps : 0,
                AccuracyScore = 80,
                Reasoning = "基于步骤完成度的简单评估(LLM评估失败)"
            };

            if (executionResult.AllStepsCompleted)
            {
                result.Action = ReflectionAction.Complete;
                result.FinalAnswer = CombineStepResults(taskPlanning);
            }
            else if (executionResult.FailedSteps > 0)
            {
                result.Action = ReflectionAction.RetryStep;
                result.RetryStepIndex = taskPlanning.Steps.FirstOrDefault(s => s.StepStatus == StepStatus.Failed)?.StepIndex ?? 1;
                result.Reason = "存在失败的步骤";
            }
            else
            {
                result.Action = ReflectionAction.ContinueExecution;
            }

            return result;
        }

        /// <summary>
        /// 构建步骤评估提示词
        /// </summary>
        private string BuildStepEvaluationPrompt(TaskStep step)
        {
            var prompt = new System.Text.StringBuilder();
            
            prompt.AppendLine("# 步骤质量评估");
            prompt.AppendLine();
            prompt.AppendLine($"## 步骤信息");
            prompt.AppendLine($"- 步骤序号: {step.StepIndex}");
            prompt.AppendLine($"- 步骤描述: {step.StepDescription}");
            prompt.AppendLine($"- 步骤类型: {step.StepType}");
            prompt.AppendLine($"- 预期输出: {step.ExpectedOutput}");
            prompt.AppendLine();
            
            prompt.AppendLine($"## 实际执行结果");
            if (!string.IsNullOrEmpty(step.ActualOutput))
            {
                prompt.AppendLine($"输出内容:");
                prompt.AppendLine(step.ActualOutput);
            }
            else
            {
                prompt.AppendLine("(无输出)");
            }
            prompt.AppendLine();
            
            if (!string.IsNullOrEmpty(step.ErrorMessage))
            {
                prompt.AppendLine($"## 错误信息");
                prompt.AppendLine(step.ErrorMessage);
                prompt.AppendLine();
            }
            
            prompt.AppendLine("## 评估要求");
            prompt.AppendLine("请从以下三个维度评估步骤质量,并给出综合评分(0-100):");
            prompt.AppendLine("1. **完整性** (0-40分): 输出是否完整,是否包含所有必要信息");
            prompt.AppendLine("2. **准确性** (0-40分): 输出是否准确,是否符合预期");
            prompt.AppendLine("3. **相关性** (0-20分): 输出是否与任务相关,是否有用");
            prompt.AppendLine();
            prompt.AppendLine("请直接返回一个0-100的整数分数。");
            
            return prompt.ToString();
        }

        /// <summary>
        /// 解析质量分数
        /// </summary>
        private int ParseQualityScore(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return 50; // 默认中等分数
            }

            // 尝试提取数字
            var match = System.Text.RegularExpressions.Regex.Match(response, @"\b(\d{1,3})\b");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int score))
            {
                // 限制范围 0-100
                return Math.Min(100, Math.Max(0, score));
            }

            // 如果无法解析,返回默认值
            return 50;
        }

        /// <summary>
        /// 计算回退质量分数(基于规则)
        /// </summary>
        private int CalculateFallbackQualityScore(TaskStep step)
        {
            int score = 50; // 基础分数

            // 根据步骤状态调整
            if (step.StepStatus == StepStatus.Completed)
            {
                score += 20; // 完成状态加分
            }
            else if (step.StepStatus == StepStatus.Failed)
            {
                score -= 30; // 失败状态扣分
            }

            // 根据是否有输出调整
            if (!string.IsNullOrEmpty(step.ActualOutput))
            {
                score += 15; // 有输出加分
                
                // 根据输出长度调整
                if (step.ActualOutput.Length > 100)
                {
                    score += 10; // 输出较长加分
                }
            }
            else
            {
                score -= 20; // 无输出扣分
            }

            // 根据是否有错误调整
            if (!string.IsNullOrEmpty(step.ErrorMessage))
            {
                score -= 15; // 有错误扣分
            }

            // 根据重试次数调整
            if (step.RetryCount > 0)
            {
                score -= step.RetryCount * 5; // 每次重试扣5分
            }

            // 限制范围 0-100
            return Math.Min(100, Math.Max(0, score));
        }
        
        /// <summary>
        /// 获取最终结果 - 只返回终端步骤的输出
        /// </summary>
        private string CombineStepResults(TaskPlanning taskPlanning)
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
                    parts.Add(output);
                }
                else
                {
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

        #endregion

        #region 内部数据类

        /// <summary>
        /// 反思响应数据结构
        /// </summary>
        private class ReflectionResponseData
        {
            public int overallQuality { get; set; }
            public int completenessScore { get; set; }
            public int accuracyScore { get; set; }
            public string reasoning { get; set; }
            public string action { get; set; }
            public string reason { get; set; }
            public string finalAnswer { get; set; }
            public int? retryStepIndex { get; set; }  // 可为空，因为只有 action=retry_step 时才需要
            public string refinedPrompt { get; set; }
        }

        #endregion

        #region 动态任务分析

        /// <summary>
        /// 动态分析任务，建议下一步行动
        /// </summary>
        public async Task<ReflectionResult> AnalyzeTaskDynamicallyAsync(
            ClawAIData nodeData,
            LargeModelConfig reflectionModelConfig,
            TaskPlanning taskPlanning,
            ExecutionResult executionResult,
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            IProgress<string> progress)
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, " 动态任务分析");

            try
            {
                // 检查是否启用动态分析
                if (!nodeData.reflectionConfig.enableDynamicTaskAnalysis)
                {
                    _logger.LogInformation("[Reflection] 动态任务分析未启用，使用标准反思");
                    return await ReflectOnExecutionAsync(
                        nodeData, reflectionModelConfig, taskPlanning, 
                        executionResult, originalTask, 1, progress);
                }

                // 快速路径检查：高质量步骤直接完成
                var completedSteps = taskPlanning.Steps.Where(s => s.StepStatus == StepStatus.Completed).ToList();
                if (completedSteps.Count > 0)
                {
                    var avgQuality = completedSteps.Where(s => s.QualityScore.HasValue)
                        .Select(s => s.QualityScore.Value)
                        .DefaultIfEmpty(0)
                        .Average();

                    // 如果平均质量 >= 80，且最后一个步骤是 WorkflowCall，直接完成
                    var lastStep = completedSteps.OrderByDescending(s => s.StepIndex).FirstOrDefault();
                    if (avgQuality >= 80 && lastStep?.StepType == StepType.WorkflowCall && !string.IsNullOrEmpty(lastStep.ActualOutput))
                    {
                        LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" 快速路径 - 高质量 WorkFlow 步骤（平均质量: {avgQuality:F0}），直接完成");
                        return new ReflectionResult
                        {
                            Action = ReflectionAction.Complete,
                            OverallQuality = (int)avgQuality,
                            CompletenessScore = 95,
                            AccuracyScore = (int)avgQuality,
                            Reasoning = $"WorkFlow 已成功执行并返回高质量结果（质量分: {avgQuality:F0}），任务完成。",
                            FinalAnswer = CombineStepResults(taskPlanning)
                        };
                    }
                }

                // 构建动态分析提示词
                var prompt = BuildDynamicAnalysisPrompt(
                    originalTask,
                    taskPlanning,
                    availableWorkflows);

                LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, " 调用 LLM 进行动态任务分析");

                // 构建 ChatHistory
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("你是一个专业的任务分析助手，擅长分析任务完成情况并提供改进建议。");
                chatHistory.AddUserMessage(prompt);

                // 调用 LLM
                var responseBuilder = new System.Text.StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    reflectionModelConfig,
                    chatHistory,
                    null,
                    "text",
                    true,
                    progress,
                    System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }

                var response = responseBuilder.ToString();

                // 解析响应
                var result = ParseDynamicAnalysisResponse(response, taskPlanning);

                LoggerHelper.LogInfo(_logger, ClawLogModules.REFLECTION, $" 动态分析完成 - Action: {result.Action}, 建议步骤数: {result.SuggestedSteps?.Count ?? 0}");

                return result;
            }
            catch (LLMException llmEx) when (llmEx.IsFatal)
            {
                // 致命 LLM 错误：不降级到标准反思（同样会调用坏掉的 LLM），向上抛出。
                _logger.LogError(llmEx, "[Reflection] 动态任务分析失败（LLM 致命错误）");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reflection] 动态任务分析失败");
                
                // 降级到标准反思
                return await ReflectOnExecutionAsync(
                    nodeData, reflectionModelConfig, taskPlanning,
                    executionResult, originalTask, 1, progress);
            }
        }

        /// <summary>
        /// 构建动态分析提示词
        /// </summary>
        private string BuildDynamicAnalysisPrompt(
            string originalTask,
            TaskPlanning taskPlanning,
            List<WorkflowConfigInfo> availableWorkflows)
        {
            var prompt = new System.Text.StringBuilder();

            prompt.AppendLine("# 任务反思与动态分析");
            prompt.AppendLine();
            prompt.AppendLine("## 原始任务");
            prompt.AppendLine(originalTask);
            prompt.AppendLine();

            // 已完成的步骤
            prompt.AppendLine("## 已完成的步骤");
            var completedSteps = taskPlanning.Steps
                .Where(s => s.StepStatus == StepStatus.Completed)
                .ToList();

            if (completedSteps.Count > 0)
            {
                foreach (var step in completedSteps)
                {
                    prompt.AppendLine($"### 步骤 {step.StepIndex}: {step.StepDescription}");
                    prompt.AppendLine($"**结果**：{(step.ActualOutput?.Length > 200 ? step.ActualOutput.Substring(0, 200) + "..." : step.ActualOutput)}");
                    prompt.AppendLine();
                }
            }
            else
            {
                prompt.AppendLine("（尚未完成任何步骤）");
                prompt.AppendLine();
            }

            // 可用的 WorkFlow
            prompt.AppendLine("## 可用的 WorkFlow 能力");
            if (availableWorkflows != null && availableWorkflows.Count > 0)
            {
                foreach (var workflow in availableWorkflows)
                {
                    prompt.AppendLine($"- **{workflow.name}** (ID: {workflow.workflowId})");
                    prompt.AppendLine($"  描述: {workflow.description}");
                    if (workflow.capabilities != null && workflow.capabilities.Count > 0)
                    {
                        prompt.AppendLine($"  能力: {string.Join(", ", workflow.capabilities)}");
                    }
                    prompt.AppendLine();
                }
            }
            else
            {
                prompt.AppendLine("（无可用 WorkFlow）");
                prompt.AppendLine();
            }

            // 分析要求
            prompt.AppendLine("---");
            prompt.AppendLine();
            prompt.AppendLine("## 你的任务");
            prompt.AppendLine();
            prompt.AppendLine("请分析当前执行结果，判断任务是否完成，以及是否需要额外步骤。");
            prompt.AppendLine();
            prompt.AppendLine("### 1. 任务完成度评估");
            prompt.AppendLine("- 当前完成度：0-100%");
            prompt.AppendLine("- 已实现的功能/内容");
            prompt.AppendLine("- 缺失的功能/内容");
            prompt.AppendLine();
            prompt.AppendLine("### 2. 质量评估");
            prompt.AppendLine("- 整体质量：0-100分");
            prompt.AppendLine("- 完整性：0-100分");
            prompt.AppendLine("- 准确性：0-100分");
            prompt.AppendLine();
            prompt.AppendLine("### 3. 是否需要额外步骤？");
            prompt.AppendLine();
            prompt.AppendLine("⚠️ **强制规则 - 必须严格遵守**：");
            prompt.AppendLine();
            prompt.AppendLine("**立即完成的情况（返回 action: \"complete\"）**：");
            prompt.AppendLine("1. ✅ 如果已完成步骤的质量分数 >= 80分 → **必须**返回 complete");
            prompt.AppendLine("2. ✅ 如果任务是简单问答/查询，且已有答案 → **必须**返回 complete");
            prompt.AppendLine("3. ✅ 如果 WorkFlow 已成功执行并返回结果 → **必须**返回 complete");
            prompt.AppendLine("4. ✅ 如果用户只是问候/闲聊，且已回复 → **必须**返回 complete");
            prompt.AppendLine();
            prompt.AppendLine("**需要重新规划的情况（返回 action: \"replan\"）**：");
            prompt.AppendLine("1. ❌ 任务明确需要多个步骤，但只完成了第一步");
            prompt.AppendLine("2. ❌ 用户要求生成多种内容（如文案+图片+视频）");
            prompt.AppendLine("3. ❌ 当前结果质量 < 60分，且有明确的改进方向");
            prompt.AppendLine("4. ❌ 任务明确包含\"并且\"、\"然后\"等连接词，表示多步骤");
            prompt.AppendLine();
            prompt.AppendLine("**禁止的行为**：");
            prompt.AppendLine("- 🚫 不要为了\"完美\"而反复添加步骤");
            prompt.AppendLine("- 🚫 不要为简单任务添加不必要的步骤");
            prompt.AppendLine("- 🚫 不要重复调用相同的 WorkFlow");
            prompt.AppendLine("- 🚫 不要在质量已经很高时继续优化");
            prompt.AppendLine();
            prompt.AppendLine("### 4. 步骤建议（仅在 action=\"replan\" 时提供）");
            prompt.AppendLine("基于可用的 WorkFlow，建议接下来应该执行什么步骤。");
            prompt.AppendLine();
            prompt.AppendLine("**重要规则**：");
            prompt.AppendLine("1. 只建议真正必要的步骤（最多3个）");
            prompt.AppendLine("2. 优先使用已有的 WorkFlow");
            prompt.AppendLine("3. 明确说明为什么需要这个步骤");
            prompt.AppendLine("4. 设置合理的优先级（1-10，10最高）");
            prompt.AppendLine("5. 如果从前置步骤提取数据，使用 JSON Path（如 $.copywriting）");
            prompt.AppendLine("6. **如果 action=\"complete\"，则 suggestedSteps 必须为空数组 []**");
            prompt.AppendLine();
            prompt.AppendLine("**返回格式（JSON）**：");
            prompt.AppendLine("```json");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"action\": \"complete\" | \"replan\",");
            prompt.AppendLine("  \"overallQuality\": 85,");
            prompt.AppendLine("  \"completenessScore\": 90,");
            prompt.AppendLine("  \"accuracyScore\": 80,");
            prompt.AppendLine("  \"reasoning\": \"分析说明\",");
            prompt.AppendLine("  \"taskAnalysis\": {");
            prompt.AppendLine("    \"completionPercentage\": 40,");
            prompt.AppendLine("    \"needsAdditionalSteps\": true,");
            prompt.AppendLine("    \"summary\": \"文案已生成，但缺少视觉元素\",");
            prompt.AppendLine("    \"missingCapabilities\": [\"图片生成\", \"排版设计\"],");
            prompt.AppendLine("    \"improvementSuggestions\": [\"添加配图\", \"优化排版\"]");
            prompt.AppendLine("  },");
            prompt.AppendLine("  \"suggestedSteps\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"stepDescription\": \"根据文案生成配图\",");
            prompt.AppendLine("      \"suggestedWorkflowIds\": [\"workflow_id\"],");
            prompt.AppendLine("      \"dependsOnStepIndices\": [1],");
            prompt.AppendLine("      \"inputSuggestions\": [");
            prompt.AppendLine("        {");
            prompt.AppendLine("          \"varName\": \"prompt\",");
            prompt.AppendLine("          \"value\": \"$.copywriting\",");
            prompt.AppendLine("          \"extractFromPreviousStep\": true,");
            prompt.AppendLine("          \"sourceStepIndex\": 1");
            prompt.AppendLine("        }");
            prompt.AppendLine("      ],");
            prompt.AppendLine("      \"priority\": 8,");
            prompt.AppendLine("      \"reason\": \"推文需要视觉元素\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");
            prompt.AppendLine("```");

            return prompt.ToString();
        }

        /// <summary>
        /// 解析动态分析响应
        /// </summary>
        private ReflectionResult ParseDynamicAnalysisResponse(string response, TaskPlanning taskPlanning)
        {
            try
            {
                // 提取 JSON
                var jsonContent = ExtractJsonFromResponse(response);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<DynamicAnalysisResponseData>(jsonContent);

                var result = new ReflectionResult
                {
                    OverallQuality = data.overallQuality,
                    CompletenessScore = data.completenessScore,
                    AccuracyScore = data.accuracyScore,
                    Reasoning = data.reasoning ?? "",
                    Action = ParseReflectionAction(data.action),
                    Reason = data.reasoning ?? ""
                };

                // 解析任务分析
                if (data.taskAnalysis != null)
                {
                    result.TaskAnalysis = new TaskAnalysis
                    {
                        CompletionPercentage = data.taskAnalysis.completionPercentage,
                        NeedsAdditionalSteps = data.taskAnalysis.needsAdditionalSteps,
                        Summary = data.taskAnalysis.summary ?? "",
                        MissingCapabilities = data.taskAnalysis.missingCapabilities ?? new List<string>(),
                        ImprovementSuggestions = data.taskAnalysis.improvementSuggestions ?? new List<string>()
                    };
                }

                // 解析建议步骤
                if (data.suggestedSteps != null && data.suggestedSteps.Count > 0)
                {
                    result.SuggestedSteps = new List<SuggestedStep>();
                    foreach (var stepData in data.suggestedSteps)
                    {
                        var suggestedStep = new SuggestedStep
                        {
                            StepDescription = stepData.stepDescription ?? "",
                            StepType = StepType.WorkflowCall,
                            SuggestedWorkflowIds = stepData.suggestedWorkflowIds ?? new List<string>(),
                            DependsOnStepIndices = stepData.dependsOnStepIndices ?? new List<int>(),
                            ExpectedOutput = stepData.expectedOutput ?? "",
                            Priority = stepData.priority > 0 ? stepData.priority : 5,
                            Reason = stepData.reason ?? ""
                        };

                        // 解析输入建议
                        if (stepData.inputSuggestions != null)
                        {
                            foreach (var inputData in stepData.inputSuggestions)
                            {
                                suggestedStep.InputSuggestions.Add(new InputSuggestion
                                {
                                    VarName = inputData.varName ?? "",
                                    Value = inputData.value ?? "",
                                    ExtractFromPreviousStep = inputData.extractFromPreviousStep,
                                    SourceStepIndex = inputData.sourceStepIndex
                                });
                            }
                        }

                        result.SuggestedSteps.Add(suggestedStep);
                    }
                }

                // 如果是 Complete，生成最终答案
                if (result.Action == ReflectionAction.Complete)
                {
                    result.FinalAnswer = CombineStepResults(taskPlanning);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reflection] 解析动态分析响应失败");
                _logger.LogWarning("[Reflection] 原始响应内容: {Response}", response.Length > 500 ? response.Substring(0, 500) + "..." : response);

                // 返回默认结果
                return new ReflectionResult
                {
                    OverallQuality = 70,
                    CompletenessScore = 70,
                    AccuracyScore = 70,
                    Reasoning = "解析失败，使用默认结果",
                    Action = ReflectionAction.Complete,
                    FinalAnswer = CombineStepResults(taskPlanning)
                };
            }
        }

        #endregion

        #region 动态分析数据类

        private class DynamicAnalysisResponseData
        {
            public string action { get; set; }
            public int overallQuality { get; set; }
            public int completenessScore { get; set; }
            public int accuracyScore { get; set; }
            public string reasoning { get; set; }
            public TaskAnalysisData taskAnalysis { get; set; }
            public List<SuggestedStepData> suggestedSteps { get; set; }
        }

        private class TaskAnalysisData
        {
            public int completionPercentage { get; set; }
            public bool needsAdditionalSteps { get; set; }
            public string summary { get; set; }
            public List<string> missingCapabilities { get; set; }
            public List<string> improvementSuggestions { get; set; }
        }

        private class SuggestedStepData
        {
            public string stepDescription { get; set; }
            public List<string> suggestedWorkflowIds { get; set; }
            public List<int> dependsOnStepIndices { get; set; }
            public List<InputSuggestionData> inputSuggestions { get; set; }
            public string expectedOutput { get; set; }
            public int priority { get; set; }
            public string reason { get; set; }
        }

        private class InputSuggestionData
        {
            public string varName { get; set; }
            public string value { get; set; }
            public bool extractFromPreviousStep { get; set; }
            public int? sourceStepIndex { get; set; }
        }

        #endregion

        #region 步骤结果完整性验证

        /// <summary>
        /// 验证所有步骤是否都有有效的执行结果
        /// </summary>
        private bool ValidateStepResultCompleteness(TaskPlanning taskPlanning, out string validationMessage)
        {
            var completedSteps = taskPlanning.Steps
                .Where(s => s.StepStatus == StepStatus.Completed)
                .ToList();

            if (completedSteps.Count == 0)
            {
                validationMessage = "没有已完成的步骤";
                return false;
            }

            // 检查是否所有已完成步骤都有结果
            var stepsWithoutResult = completedSteps
                .Where(s => string.IsNullOrEmpty(s.ExecutionResult) || s.ExecutionResult.Length < 10)
                .ToList();

            if (stepsWithoutResult.Count > 0)
            {
                validationMessage = $"有{stepsWithoutResult.Count}个步骤没有有效结果: {string.Join(", ", stepsWithoutResult.Select(s => $"步骤{s.StepIndex}"))}";
                return false;
            }

            // 检查是否所有步骤的质量分都达标
            var lowQualitySteps = completedSteps
                .Where(s => s.QualityScore.HasValue && s.QualityScore.Value < 60)
                .ToList();

            if (lowQualitySteps.Count > 0)
            {
                validationMessage = $"有{lowQualitySteps.Count}个步骤质量分过低(<60): {string.Join(", ", lowQualitySteps.Select(s => $"步骤{s.StepIndex}({s.QualityScore}分)"))}";
                return false;
            }

            validationMessage = $"所有{completedSteps.Count}个已完成步骤都有有效结果";
            return true;
        }

        #endregion
    }
}
