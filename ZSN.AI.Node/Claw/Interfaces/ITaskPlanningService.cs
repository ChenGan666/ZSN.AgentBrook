using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.Node.Claw.Interfaces
{
    /// <summary>
    /// 任务规划服务接口
    /// </summary>
    public interface ITaskPlanningService
    {
        /// <summary>
        /// 创建任务规划
        /// </summary>
        Task<Entity.ClawAI.TaskPlanning> CreatePlanningAsync(
            Entity.ClawAIData nodeData,
            LargeModelConfig planningModelConfig,
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            Entity.ClawAI.MemoryContext memoryContext,
            string AppID,
            string SessionID,
            string MemberID,
            string NodeID,
            string ProcessesID,
            IProgress<string> progress);

        /// <summary>
        /// 创建简单计划
        /// </summary>
        Entity.ClawAI.TaskPlanning CreateSimplePlan(
            string originalTask,
            List<WorkflowConfigInfo> availableWorkflows,
            string AppID,
            string SessionID,
            string MemberID,
            string NodeID,
            string ProcessesID);

        /// <summary>
        /// 重新规划
        /// </summary>
        Task<Entity.ClawAI.TaskPlanning> ReplanAsync(
            Entity.ClawAIData nodeData,
            LargeModelConfig planningModelConfig,
            Entity.ClawAI.TaskPlanning currentPlanning,
            Entity.ClawAI.ExecutionResult executionResult,
            Entity.ClawAI.ReflectionResult reflectionResult,
            List<WorkflowConfigInfo> availableWorkflows,
            Entity.ClawAI.MemoryContext memoryContext,
            IProgress<string> progress);

        /// <summary>
        /// 更新规划状态
        /// </summary>
        Task UpdatePlanningStatusAsync(Entity.ClawAI.TaskPlanning taskPlanning);

        /// <summary>
        /// 保存规划到数据库
        /// </summary>
        Task SavePlanningAsync(Entity.ClawAI.TaskPlanning taskPlanning);

        /// <summary>
        /// 获取历史规划
        /// </summary>
        Task<List<Entity.ClawAI.TaskPlanning>> GetHistoricalPlansAsync(string AppID, string MemberID, int limit);

        /// <summary>
        /// 应用建议的步骤到任务规划（动态添加步骤）
        /// </summary>
        Task ApplySuggestedStepsAsync(
            Entity.ClawAI.TaskPlanning taskPlanning,
            List<Entity.ClawAI.SuggestedStep> suggestedSteps,
            List<WorkflowConfigInfo> availableWorkflows,
            ConcurrentQueue<string> Logs);
    }
}
