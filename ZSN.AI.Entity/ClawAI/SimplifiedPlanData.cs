using System.Collections.Generic;

namespace ZSN.AI.Entity.ClawAI
{
    /// <summary>
    /// 简化版统一规划响应数据结构（LLM 输出）
    /// LLM 只需输出此简化格式，由 SimplifiedPlanConverter 转换为完整 TaskPlanning
    /// </summary>
    public class SimplifiedPlanData
    {
        /// <summary>
        /// 决策: "directResponse" 或 "taskPlanning"
        /// </summary>
        public string decision { get; set; }

        /// <summary>
        /// 判断理由
        /// </summary>
        public string reason { get; set; }

        /// <summary>
        /// 置信度(0-100)
        /// </summary>
        public int confidence { get; set; }

        /// <summary>
        /// 任务最终目标（含量化指标）
        /// </summary>
        public string goal { get; set; }

        /// <summary>
        /// 直接回复内容（仅 decision=directResponse 时使用）
        /// </summary>
        public string directResponse { get; set; }

        /// <summary>
        /// 简化步骤列表
        /// </summary>
        public List<SimplifiedStepData> steps { get; set; }
    }

    /// <summary>
    /// 简化版步骤数据（LLM 输出）
    /// </summary>
    public class SimplifiedStepData
    {
        /// <summary>
        /// 步骤索引（从1开始）
        /// </summary>
        public int i { get; set; }

        /// <summary>
        /// 步骤描述
        /// </summary>
        public string desc { get; set; }

        /// <summary>
        /// WorkFlow ID（空字符串表示 llm_reasoning 步骤，不需要调用 WorkFlow）
        /// </summary>
        public string wf { get; set; }

        /// <summary>
        /// 依赖的步骤索引列表（如 [1] 表示依赖步骤1）
        /// </summary>
        public List<int> dep { get; set; }

        /// <summary>
        /// 步骤的提示词/指令
        /// 使用 {N} 引用第N步的输出，如 {1} 表示步骤1的输出
        /// </summary>
        public string prompt { get; set; }
    }
}
