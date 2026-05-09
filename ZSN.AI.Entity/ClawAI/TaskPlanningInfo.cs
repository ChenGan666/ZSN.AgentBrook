using System;
using System.Collections.Generic;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_task_planning 任务规划表
    /// </summary>
    public partial class TaskPlanningInfo
    {
        public TaskPlanningInfo() { }

        #region AutoField
        /// <summary>
        /// 规划ID
        /// </summary>
        public string PlanningID { get; set; }

        /// <summary>
        /// 应用ID
        /// </summary>
        public string AppID { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionID { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public string MemberID { get; set; }

        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeID { get; set; }

        /// <summary>
        /// 流程ID
        /// </summary>
        public string ProcessesID { get; set; }

        /// <summary>
        /// 原始任务描述
        /// </summary>
        public string OriginalTask { get; set; }

        /// <summary>
        /// 规划状态: Planning, Executing, Completed, Failed, Paused
        /// </summary>
        public string PlanningStatus { get; set; }

        /// <summary>
        /// 当前执行到第几步
        /// </summary>
        public int CurrentStepIndex { get; set; }

        /// <summary>
        /// 总步骤数
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// 规划策略: sequential, parallel, adaptive
        /// </summary>
        public string Strategy { get; set; }

        /// <summary>
        /// 规划置信度(0-100)
        /// </summary>
        public int Confidence { get; set; }

        /// <summary>
        /// 预估总耗时(秒)
        /// </summary>
        public int EstimatedDuration { get; set; }

        /// <summary>
        /// 实际耗时(秒)
        /// </summary>
        public int ActualDuration { get; set; }

        /// <summary>
        /// 修订次数
        /// </summary>
        public int RevisionCount { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; }
        #endregion

        #region ExtendField
        /// <summary>
        /// 步骤列表(扩展字段,不在数据库中)
        /// </summary>
        public List<TaskStepInfo> Steps { get; set; }
        #endregion
    }
}
