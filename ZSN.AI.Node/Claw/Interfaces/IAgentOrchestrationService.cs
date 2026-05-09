using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Utils;

namespace ZSN.AI.Node.Claw.Interfaces
{
    /// <summary>
    /// Agent 编排服务接口
    /// </summary>
    public interface IAgentOrchestrationService
    {
        /// <summary>
        /// 获取可用的 WorkFlow 配置
        /// </summary>
        Task<List<WorkflowConfigInfo>> GetAvailableWorkflowsAsync(
            Entity.ClawAIData nodeData);

        /// <summary>
        /// 执行步骤
        /// </summary>
        Task<Entity.ClawAI.ExecutionResult> ExecuteStepsAsync(
            NodeConfig config,
            Entity.ClawAI.TaskPlanning taskPlanning,
            string AppID,
            string SessionID,
            string ProcessesID,
            string TaskID,
            string originalTask,
            List<Inputs> inputs,
            Entity.ClawAIData nodeData,
            LargeModelConfig reflectionModelConfig,
            ConcurrentQueue<string> Logs,
            IProgress<string> progress,
            ClawAIExecutionLogger execLogger = null,
            string MemberID = "system",
            string FromMainTaskID = "");

        /// <summary>
        /// 重试步骤
        /// </summary>
        Task RetryStepAsync(
            Entity.ClawAI.TaskPlanning taskPlanning,
            int stepIndex,
            string refinedPrompt);

        /// <summary>
        /// 合并步骤结果
        /// </summary>
        string CombineStepResults(Entity.ClawAI.TaskPlanning taskPlanning);
    }
}
