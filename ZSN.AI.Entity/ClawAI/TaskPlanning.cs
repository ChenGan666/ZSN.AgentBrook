using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ZSN.AI.Entity.ClawAI
{
    /// <summary>
    /// 任务规划
    /// </summary>
    public class TaskPlanning
    {
        public string PlanningID { get; set; } = Guid.NewGuid().ToString();
        public string AppID { get; set; }
        public string SessionID { get; set; }
        public string MemberID { get; set; }
        public string NodeID { get; set; }
        public string ProcessesID { get; set; }

        public string OriginalTask { get; set; }

        /// <summary>
        /// 任务最终目标（由 LLM 在规划时声明，用于执行后反思对照）
        /// </summary>
        public string Goal { get; set; }

        public PlanningStatus PlanningStatus { get; set; } = PlanningStatus.Planning;
        public int CurrentStepIndex { get; set; } = 0;
        public int TotalSteps { get; set; } = 0;

        /// <summary>
        /// 规划的步骤列表
        /// </summary>
        public List<TaskStep> Steps { get; set; } = new List<TaskStep>();

        /// <summary>
        /// 规划元数据
        /// </summary>
        public PlanningMetadata Metadata { get; set; } = new PlanningMetadata();

        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 任务步骤
    /// </summary>
    public class TaskStep
    {
        public string StepID { get; set; } = GenerateShortStepID();
        public string PlanningID { get; set; }
        public int StepIndex { get; set; }
        public string StepDescription { get; set; }
        public StepType StepType { get; set; } = StepType.WorkflowCall;

        /// <summary>
        /// 分配的 WorkFlow 节点ID列表
        /// </summary>
        public List<string> AssignedWorkflowIds { get; set; } = new List<string>();

        /// <summary>
        /// 步骤状态
        /// </summary>
        public StepStatus StepStatus { get; set; } = StepStatus.Pending;

        /// <summary>
        /// 依赖的步骤ID列表
        /// </summary>
        public List<string> DependsOnStepIds { get; set; } = new List<string>();

        /// <summary>
        /// 步骤特定的输入参数(用于传递给WorkFlow)
        /// </summary>
        public List<Inputs> StepInputs { get; set; } = new List<Inputs>();

        /// <summary>
        /// 执行结果
        /// </summary>
        public string ExecutionResult { get; set; }

        /// <summary>
        /// 执行开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 执行结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 质量评分(0-100)
        /// </summary>
        public int? QualityScore { get; set; }

        /// <summary>
        /// 预期输出
        /// </summary>
        public string ExpectedOutput { get; set; }

        /// <summary>
        /// 实际输出
        /// </summary>
        public string ActualOutput { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 循环执行次数（默认1次，即不循环）。
        /// 当任务需要重复同类操作N次时设置为N，执行引擎会重复调用该步骤N次，
        /// 每次自动注入 loopIndex（从1开始）和 loopTotal 变量。
        /// </summary>
        public int LoopCount { get; set; } = 1;

        /// <summary>
        /// 生成短StepID（使用GUID的MD5哈希前8位）
        /// </summary>
        private static string GenerateShortStepID()
        {
            var guid = Guid.NewGuid().ToString();
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(guid));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLower();
            }
        }
    }

    /// <summary>
    /// 规划元数据
    /// </summary>
    public class PlanningMetadata
    {
        /// <summary>
        /// 规划策略: sequential(顺序), parallel(并行), adaptive(自适应)
        /// </summary>
        public string Strategy { get; set; } = "adaptive";

        /// <summary>
        /// 预估总耗时(秒)
        /// </summary>
        public int EstimatedDuration { get; set; }

        /// <summary>
        /// 实际耗时(秒)
        /// </summary>
        public int ActualDuration { get; set; }

        /// <summary>
        /// 规划置信度(0-100)
        /// </summary>
        public int Confidence { get; set; }

        /// <summary>
        /// 修订次数
        /// </summary>
        public int RevisionCount { get; set; } = 0;
    }

    /// <summary>
    /// 规划状态枚举
    /// </summary>
    public enum PlanningStatus
    {
        Planning = 1,      // 规划中
        Executing = 2,     // 执行中
        Completed = 3,     // 已完成
        Failed = 4,        // 失败
        Paused = 5         // 暂停
    }

    /// <summary>
    /// 步骤状态枚举
    /// </summary>
    public enum StepStatus
    {
        Pending = 1,       // 待执行
        Executing = 2,     // 执行中
        Completed = 3,     // 已完成
        Failed = 4,        // 失败
        Skipped = 5        // 已跳过
    }

    /// <summary>
    /// 步骤类型枚举
    /// </summary>
    public enum StepType
    {
        WorkflowCall = 1,      // 调用 WorkFlow
        LLMReasoning = 2,      // LLM 推理
        DataCollection = 3,    // 数据收集
        Validation = 4,        // 验证
        Synthesis = 5,          // 综合
            Query = 6,
        Search  =7,
        Analysis =8,
        Generation =9,
        Processing =10,
        Optimization =11
    }
}
