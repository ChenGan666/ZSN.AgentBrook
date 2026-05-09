using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Services;
using ZSN.AI.Node.Claw.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.Entity;

namespace ZSN.AI.Node.Claw.Pipeline
{
    /// <summary>
    /// 规划处理器 - 负责任务规划的创建和管理
    /// </summary>
    public class PlanningHandler
    {
        private readonly ITaskPlanningService _taskPlanningService;
        private readonly ILogger _logger;

        public PlanningHandler(
            ITaskPlanningService taskPlanningService,
            ILogger logger)
        {
            _taskPlanningService = taskPlanningService;
            _logger = logger;
        }

        /// <summary>
        /// 创建任务规划
        /// </summary>
        public async Task<PlanningResult> CreatePlanningAsync(
            ClawAIData nodeData,
            LargeModelConfig planningModelConfig,
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            MemoryContext memoryContext,
            string appId,
            string sessionId,
            string memberId,
            string nodeId,
            string processesId,
            IProgress<string> progress)
        {
            var result = new PlanningResult
            {
                Logs = new List<string>()
            };

            result.Logs.Add("=== 阶段 2: 任务规划 ===");

            TaskPlanning taskPlanning = null;

            if (nodeData.taskPlanningConfig.enabled && availableWorkflows.Count > 0)
            {
                // 创建详细规划
                taskPlanning = await _taskPlanningService.CreatePlanningAsync(
                    nodeData,
                    planningModelConfig,
                    originalTask,
                    availableWorkflows,
                    memoryContext,
                    appId,
                    sessionId,
                    memberId,
                    nodeId,
                    processesId,
                    progress
                );

                result.TaskPlanning = taskPlanning;
                result.PlanningLogMessage = BuildPlanningLogMessage(taskPlanning);
                result.PlanningStepsJson = BuildPlanningStepsJson(taskPlanning);
                result.Logs.AddRange(result.PlanningLogMessage.Lines);

                LoggerHelper.LogInfo(_logger, ClawLogModules.PLANNING_HANDLER, $"规划创建成功 - PlanningID: {taskPlanning.PlanningID}, Steps: {taskPlanning.TotalSteps}");
            }
            else
            {
                // 创建简单计划
                taskPlanning = _taskPlanningService.CreateSimplePlan(
                    originalTask, availableWorkflows, appId, sessionId, memberId, nodeId, processesId);

                result.TaskPlanning = taskPlanning;
                result.Logs.Add("创建简单执行计划(未启用规划或无可用WorkFlow)");

                LoggerHelper.LogInfo(_logger, ClawLogModules.PLANNING_HANDLER, $"简单计划创建成功 - PlanningID: {taskPlanning.PlanningID}");
            }

            return result;
        }

        /// <summary>
        /// 构建规划日志消息
        /// </summary>
        private PlanningLogMessage BuildPlanningLogMessage(TaskPlanning taskPlanning)
        {
            var lines = new List<string>();
            var messageBuilder = new StringBuilder($"✓ 规划完成:\n");

            lines.Add("✓ 规划完成:");
            lines.Add($"  - 总步骤: {taskPlanning.TotalSteps}");
            messageBuilder.AppendLine($"  - 总步骤: {taskPlanning.TotalSteps}");

            lines.Add($"  - 策略: {taskPlanning.Metadata.Strategy}");
            messageBuilder.AppendLine($"  - 策略: {taskPlanning.Metadata.Strategy}");

            lines.Add($"  - 置信度: {taskPlanning.Metadata.Confidence}%");
            messageBuilder.AppendLine($"  - 置信度: {taskPlanning.Metadata.Confidence}%");

            lines.Add($"  - 预估耗时: {taskPlanning.Metadata.EstimatedDuration} 秒");
            messageBuilder.AppendLine($"  - 预估耗时: {taskPlanning.Metadata.EstimatedDuration} 秒");

            lines.Add("\n步骤详情:");
            messageBuilder.AppendLine();

            for (int i = 0; i < taskPlanning.Steps.Count; i++)
            {
                var step = taskPlanning.Steps[i];
                var stepLine = $"  步骤 {step.StepIndex}: {step.StepDescription}";
                lines.Add(stepLine);
                messageBuilder.AppendLine(stepLine);

                var typeLine = $"    - 类型: {step.StepType}";
                lines.Add(typeLine);
                messageBuilder.AppendLine(typeLine);

                if (step.AssignedWorkflowIds.Count > 0)
                {
                    var workFlowLine = $"    - WorkFlow: {string.Join(", ", step.AssignedWorkflowIds)}";
                    lines.Add(workFlowLine);
                    messageBuilder.AppendLine(workFlowLine);
                }

                if (step.DependsOnStepIds.Count > 0)
                {
                    var depLine = $"    - 依赖: 步骤 {string.Join(", ", step.DependsOnStepIds)}";
                    lines.Add(depLine);
                    messageBuilder.AppendLine(depLine);
                }
            }

            return new PlanningLogMessage
            {
                Lines = lines,
                FullMessage = messageBuilder.ToString()
            };
        }

        /// <summary>
        /// 构建规划步骤JSON
        /// </summary>
        private string BuildPlanningStepsJson(TaskPlanning taskPlanning)
        {
            var stepsJson = JsonConvert.SerializeObject(new
            {
                type = "planning_steps",
                planningId = taskPlanning.PlanningID,
                totalSteps = taskPlanning.TotalSteps,
                strategy = taskPlanning.Metadata.Strategy,
                confidence = taskPlanning.Metadata.Confidence,
                estimatedDuration = taskPlanning.Metadata.EstimatedDuration,
                steps = taskPlanning.Steps.Select(s => new
                {
                    stepId = s.StepID,
                    stepIndex = s.StepIndex,
                    description = s.StepDescription,
                    type = s.StepType.ToString(),
                    assignedWorkflowIds = s.AssignedWorkflowIds,
                    dependencies = s.DependsOnStepIds,
                    status = s.StepStatus.ToString(),
                    expectedOutput = s.ExpectedOutput
                }).ToList()
            });

            return $"\n[PLANNING_STEPS]{stepsJson}[/PLANNING_STEPS]\n";
        }
    }

    /// <summary>
    /// 规划结果
    /// </summary>
    public class PlanningResult
    {
        public TaskPlanning TaskPlanning { get; set; }
        public List<string> Logs { get; set; }
        public PlanningLogMessage PlanningLogMessage { get; set; }
        public string PlanningStepsJson { get; set; }
    }

    /// <summary>
    /// 规划日志消息
    /// </summary>
    public class PlanningLogMessage
    {
        public List<string> Lines { get; set; }
        public string FullMessage { get; set; }
    }
}
