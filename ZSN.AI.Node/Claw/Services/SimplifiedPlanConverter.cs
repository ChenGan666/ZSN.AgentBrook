using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// 简化版任务规划转换器
    /// 将 LLM 输出的 SimplifiedPlanData 转换为完整的 TaskPlanning 对象
    /// </summary>
    public static class SimplifiedPlanConverter
    {
        /// <summary>
        /// 将简化版规划数据转换为完整 TaskPlanning
        /// </summary>
        /// <param name="simplified">LLM 输出的简化版数据</param>
        /// <param name="originalTask">原始用户任务</param>
        /// <param name="availableWorkflows">可用的 WorkFlow 列表</param>
        /// <param name="AppID">应用ID</param>
        /// <param name="SessionID">会话ID</param>
        /// <param name="MemberID">成员ID</param>
        /// <param name="NodeID">节点ID</param>
        /// <param name="ProcessesID">流程ID</param>
        /// <param name="Logs">日志队列</param>
        /// <returns>完整的 TaskPlanning 对象</returns>
        public static TaskPlanning Convert(
            SimplifiedPlanData simplified,
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            string AppID,
            string SessionID,
            string MemberID,
            string NodeID,
            string ProcessesID,
            ConcurrentQueue<string> Logs)
        {
            if (simplified == null)
                throw new ArgumentNullException(nameof(simplified));

            if (simplified.steps == null || simplified.steps.Count == 0)
                throw new ArgumentException("简化版规划数据中 steps 为空");

            // 第一遍：为每个步骤生成 StepID，建立 index → StepID 映射
            var stepIndexToIdMap = new Dictionary<int, string>();
            var steps = new List<TaskStep>();

            for (int i = 0; i < simplified.steps.Count; i++)
            {
                var stepData = simplified.steps[i];
                string stepId = GenerateShortStepID();

                stepIndexToIdMap[stepData.i] = stepId;

                // 推断 stepType：wf 非空 → WorkflowCall，否则 → LLMReasoning
                var stepType = InferStepType(stepData, availableWorkflows);

                // 构建 StepInputs：从 prompt 字段生成标准 inputs
                var stepInputs = BuildStepInputs(stepData, stepIndexToIdMap, Logs);

                var step = new TaskStep
                {
                    StepID = stepId,
                    PlanningID = "", // 由 CreateSimplePlan 设置
                    StepIndex = stepData.i,
                    StepDescription = stepData.desc ?? "",
                    StepType = stepType,
                    AssignedWorkflowIds = BuildAssignedWorkflowIds(stepData),
                    DependsOnStepIds = new List<string>(), // 第二遍填充
                    StepInputs = stepInputs,
                    ExpectedOutput = "", // 不再要求 LLM 输出此字段
                    StepStatus = StepStatus.Pending
                };

                steps.Add(step);
            }

            // 第二遍：将 dep 中的步骤索引映射为实际 StepID
            for (int i = 0; i < simplified.steps.Count; i++)
            {
                var stepData = simplified.steps[i];
                var step = steps[i];

                if (stepData.dep != null && stepData.dep.Count > 0)
                {
                    foreach (var depIndex in stepData.dep)
                    {
                        if (stepIndexToIdMap.TryGetValue(depIndex, out string depStepId))
                        {
                            step.DependsOnStepIds.Add(depStepId);
                        }
                        else
                        {
                            Logs.Enqueue($"[SimplifiedPlanConverter] 警告: 步骤 {stepData.i} 的依赖索引 {depIndex} 无效，忽略");
                        }
                    }
                }
            }

            // 第三遍：将 prompt 中的 {N} 占位符替换为标准 {output_<StepID>} 格式
            ResolvePromptPlaceholders(steps, stepIndexToIdMap, Logs);

            // 推断策略：分析依赖图，如果存在可并行的层则为 parallel
            string strategy = InferStrategy(steps);

            // 构建完整 TaskPlanning
            var planning = new TaskPlanning
            {
                OriginalTask = originalTask,
                Goal = simplified.goal ?? originalTask,
                AppID = AppID,
                SessionID = SessionID,
                MemberID = MemberID,
                NodeID = NodeID,
                ProcessesID = ProcessesID,
                TotalSteps = steps.Count,
                Steps = steps,
                PlanningStatus = PlanningStatus.Planning,
                Metadata = new PlanningMetadata
                {
                    Strategy = strategy,
                    Confidence = simplified.confidence,
                    EstimatedDuration = steps.Count * 15
                }
            };

            // 设置每个步骤的 PlanningID
            foreach (var step in steps)
            {
                step.PlanningID = planning.PlanningID;
            }

            Logs.Enqueue($"[SimplifiedPlanConverter] 转换完成: {steps.Count} 个步骤, strategy={strategy}");

            return planning;
        }

        /// <summary>
        /// 校验简化版规划数据，返回错误列表。空列表表示校验通过。
        /// </summary>
        public static List<string> Validate(
            SimplifiedPlanData simplified,
            List<WorkflowConfigInfo> availableWorkflows)
        {
            var errors = new List<string>();

            if (simplified.steps == null || simplified.steps.Count == 0)
            {
                errors.Add("decision=taskPlanning 但 steps 为空，必须至少包含一个步骤");
                return errors;
            }

            if (string.IsNullOrEmpty(simplified.goal))
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
            foreach (var s in simplified.steps)
            {
                validStepIndices.Add(s.i);
            }

            for (int i = 0; i < simplified.steps.Count; i++)
            {
                var step = simplified.steps[i];
                string prefix = $"步骤 {step.i}";

                // desc 不能为空
                if (string.IsNullOrEmpty(step.desc))
                {
                    errors.Add($"{prefix}: 缺少步骤描述 desc");
                }

                // wf 必须在可用清单中
                if (!string.IsNullOrEmpty(step.wf) && validWorkflowIds.Count > 0)
                {
                    if (!validWorkflowIds.Contains(step.wf))
                    {
                        errors.Add($"{prefix}: wf '{step.wf}' 不在可用 WorkFlow 清单中");
                    }
                }

                // prompt 不能为空
                if (string.IsNullOrEmpty(step.prompt))
                {
                    errors.Add($"{prefix}: 缺少 prompt 字段");
                }

                // dep 引用有效
                if (step.dep != null)
                {
                    foreach (var depIndex in step.dep)
                    {
                        if (!validStepIndices.Contains(depIndex))
                        {
                            errors.Add($"{prefix}: dep 引用了不存在的步骤索引 {depIndex}");
                        }
                    }
                }

                // 步骤索引唯一性
                if (simplified.steps.Count(s => s.i == step.i) > 1)
                {
                    errors.Add($"{prefix}: 步骤索引 {step.i} 重复");
                }
            }

            return errors;
        }

        /// <summary>
        /// 推断步骤类型：wf 非空 → WorkflowCall，否则 → LLMReasoning
        /// </summary>
        private static StepType InferStepType(SimplifiedStepData stepData, List<WorkflowConfigInfo> availableWorkflows)
        {
            if (string.IsNullOrEmpty(stepData.wf))
                return StepType.LLMReasoning;

            return StepType.WorkflowCall;
        }

        /// <summary>
        /// 构建 AssignedWorkflowIds：wf 非空则包装为列表，否则返回空列表
        /// </summary>
        private static List<string> BuildAssignedWorkflowIds(SimplifiedStepData stepData)
        {
            if (string.IsNullOrEmpty(stepData.wf))
                return new List<string>();

            return new List<string> { stepData.wf };
        }

        /// <summary>
        /// 从简化 prompt 字段构建标准 StepInputs
        /// 同时将 {N} 占位符替换为 {output_<StepID>} 格式
        /// </summary>
        private static List<Inputs> BuildStepInputs(
            SimplifiedStepData stepData,
            Dictionary<int, string> stepIndexToIdMap,
            ConcurrentQueue<string> Logs)
        {
            var inputs = new List<Inputs>();

            string promptValue = stepData.prompt ?? "";

            inputs.Add(new Inputs
            {
                varname = "prompt",
                value = promptValue
            });

            return inputs;
        }

        /// <summary>
        /// 将 prompt 中的 {N} 和 {N}[X] 占位符替换为标准 {output_<StepID>} 和 {output_<StepID>}[X] 格式
        /// 支持: {1} → {output_abc123}，{1}[0] → {output_abc123}[0]
        /// </summary>
        private static void ResolvePromptPlaceholders(
            List<TaskStep> steps,
            Dictionary<int, string> stepIndexToIdMap,
            ConcurrentQueue<string> Logs)
        {
            // 匹配 {N} 格式，以及紧跟其后的可选数组索引 [X]
            var placeholderPattern = new Regex(@"\{(\d+)\}(\[(\d+)\])?");

            foreach (var step in steps)
            {
                if (step.StepInputs == null || step.StepInputs.Count == 0) continue;

                foreach (var input in step.StepInputs)
                {
                    if (string.IsNullOrEmpty(input.value) || !input.value.Contains("{"))
                        continue;

                    string originalValue = input.value;

                    input.value = placeholderPattern.Replace(input.value, match =>
                    {
                        if (int.TryParse(match.Groups[1].Value, out int refIndex))
                        {
                            if (stepIndexToIdMap.TryGetValue(refIndex, out string refStepId))
                            {
                                // 保留数组索引后缀（如 [0]）
                                string arraySuffix = match.Groups[2].Success ? match.Groups[2].Value : "";
                                return $"{{output_{refStepId}}}{arraySuffix}";
                            }
                        }

                        // 无法解析，保持原样
                        return match.Value;
                    });

                    if (input.value != originalValue)
                    {
                        Logs.Enqueue($"[SimplifiedPlanConverter] 步骤 {step.StepIndex} [{input.varname}] 占位符解析: {originalValue.Length} → {input.value.Length} 字符");
                    }
                }
            }
        }

        /// <summary>
        /// 推断执行策略：分析依赖图，如果存在可并行的步骤则为 "parallel"，否则 "sequential"
        /// </summary>
        private static string InferStrategy(List<TaskStep> steps)
        {
            if (steps.Count <= 1)
                return "sequential";

            // 构建依赖图，做拓扑排序分层
            var processed = new HashSet<string>();
            var remaining = new HashSet<string>(steps.Select(s => s.StepID));
            var graph = steps.ToDictionary(s => s.StepID, s => s.DependsOnStepIds ?? new List<string>());

            int maxLayerWidth = 1;
            int layerCount = 0;

            while (remaining.Count > 0)
            {
                var currentLayer = remaining
                    .Where(stepId => graph[stepId].All(dep => processed.Contains(dep)))
                    .ToList();

                if (currentLayer.Count == 0)
                    break; // 循环依赖

                if (currentLayer.Count > maxLayerWidth)
                    maxLayerWidth = currentLayer.Count;

                foreach (var stepId in currentLayer)
                {
                    processed.Add(stepId);
                    remaining.Remove(stepId);
                }

                layerCount++;
            }

            // 如果某一层有超过1个步骤，说明存在并行机会
            return maxLayerWidth > 1 ? "parallel" : "sequential";
        }

        /// <summary>
        /// 生成短 StepID（与 TaskStep.GenerateShortStepID 一致的算法）
        /// </summary>
        private static string GenerateShortStepID()
        {
            var guid = Guid.NewGuid().ToString();
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(guid));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLower();
            }
        }
    }
}
