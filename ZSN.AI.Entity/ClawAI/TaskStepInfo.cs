using System;
using System.Collections.Generic;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_task_step 任务步骤表
    /// </summary>
    public partial class TaskStepInfo
    {
        public TaskStepInfo() { }

        #region AutoField
        /// <summary>
        /// 步骤ID
        /// </summary>
        public string StepID { get; set; }

        /// <summary>
        /// 规划ID(外键)
        /// </summary>
        public string PlanningID { get; set; }

        /// <summary>
        /// 步骤序号
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// 步骤描述
        /// </summary>
        public string StepDescription { get; set; }

        /// <summary>
        /// 步骤类型: WorkflowCall, LLMReasoning, DataCollection, Validation, Synthesis
        /// </summary>
        public string StepType { get; set; }

        /// <summary>
        /// 分配的WorkFlow节点ID列表(JSON数组)
        /// </summary>
        public string AssignedWorkflowIds { get; set; }

        /// <summary>
        /// 步骤状态: Pending, Running, Completed, Failed, Skipped
        /// </summary>
        public string StepStatus { get; set; }

        /// <summary>
        /// 依赖的步骤ID列表(JSON数组)
        /// </summary>
        public string DependsOnStepIds { get; set; }

        /// <summary>
        /// 步骤输入参数(JSON数组,格式:[{"varname":"prompt","value":"..."}])
        /// </summary>
        public string StepInputs { get; set; }

        /// <summary>
        /// 预期输出
        /// </summary>
        public string ExpectedOutput { get; set; }

        /// <summary>
        /// 实际输出
        /// </summary>
        public string ActualOutput { get; set; }

        /// <summary>
        /// 执行结果(JSON)
        /// </summary>
        public string ExecutionResult { get; set; }

        /// <summary>
        /// 质量评分(0-100)
        /// </summary>
        public int QualityScore { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        #endregion
    }
}
