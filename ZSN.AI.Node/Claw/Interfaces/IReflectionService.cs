using System;
using System.Threading.Tasks;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.Node.Claw.Interfaces
{
    /// <summary>
    /// 反思服务接口
    /// </summary>
    public interface IReflectionService
    {
        /// <summary>
        /// 对执行结果进行反思评估
        /// </summary>
        Task<ReflectionResult> ReflectOnExecutionAsync(
            ClawAIData nodeData,
            LargeModelConfig reflectionModelConfig,
            TaskPlanning taskPlanning,
            ExecutionResult executionResult,
            string originalTask,
            int iteration,
            IProgress<string> progress);

        /// <summary>
        /// 动态分析任务，建议下一步行动（支持动态步骤添加）
        /// </summary>
        Task<ReflectionResult> AnalyzeTaskDynamicallyAsync(
            ClawAIData nodeData,
            LargeModelConfig reflectionModelConfig,
            TaskPlanning taskPlanning,
            ExecutionResult executionResult,
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            IProgress<string> progress);

        /// <summary>
        /// 评估步骤质量
        /// </summary>
        Task<int> EvaluateStepQualityAsync(
            TaskStep step,
            ClawAIData nodeData,
            LargeModelConfig reflectionModelConfig,
            IProgress<string> progress);
    }
}
