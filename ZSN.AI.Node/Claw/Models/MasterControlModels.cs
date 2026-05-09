using System;

namespace ZSN.AI.Node.Claw.Models
{
    /// <summary>
    /// 主控判断决策类型
    /// </summary>
    public enum MasterControlDecision
    {
        /// <summary>
        /// 直接回复（问候、感谢、简单问答等，不需要任务规划）
        /// </summary>
        DirectResponse,

        /// <summary>
        /// 任务规划（复杂任务、需要调用WorkFlow或多步骤处理）
        /// </summary>
        TaskPlanning
    }

    /// <summary>
    /// 主控判断结果
    /// </summary>
    public class MasterControlResult
    {
        /// <summary>
        /// 决策类型
        /// </summary>
        public MasterControlDecision Decision { get; set; }

        /// <summary>
        /// 决策理由（用于日志和调试）
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 置信度（0-100）
        /// </summary>
        public int Confidence { get; set; }

        /// <summary>
        /// 是否来自缓存
        /// </summary>
        public bool FromCache { get; set; }

        /// <summary>
        /// 判断耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// 建议的响应策略（可选，用于DirectResponse场景）
        /// </summary>
        public string SuggestedResponseStrategy { get; set; }

        /// <summary>
        /// 直接回复的内容（仅在Decision为DirectResponse时有值）
        /// 主控LLM在判断的同时生成回复内容，避免二次调用
        /// </summary>
        public string DirectResponseContent { get; set; }

        /// <summary>
        /// 创建直接回复决策
        /// </summary>
        public static MasterControlResult CreateDirectResponse(string reason, int confidence = 90, string responseStrategy = "friendly")
        {
            return new MasterControlResult
            {
                Decision = MasterControlDecision.DirectResponse,
                Reason = reason,
                Confidence = confidence,
                SuggestedResponseStrategy = responseStrategy
            };
        }

        /// <summary>
        /// 创建任务规划决策
        /// </summary>
        public static MasterControlResult CreateTaskPlanning(string reason, int confidence = 90)
        {
            return new MasterControlResult
            {
                Decision = MasterControlDecision.TaskPlanning,
                Reason = reason,
                Confidence = confidence
            };
        }
    }

    /// <summary>
    /// 主控判断上下文
    /// </summary>
    public class MasterControlContext
    {
        /// <summary>
        /// 用户输入
        /// </summary>
        public string UserInput { get; set; }

        /// <summary>
        /// 系统提示词（定义系统能力和职责）
        /// </summary>
        public string SystemPrompt { get; set; }

        /// <summary>
        /// 对话历史（最近N条）
        /// </summary>
        public string ChatHistory { get; set; }

        /// <summary>
        /// 可用的WorkFlow列表（用于判断是否需要调用）
        /// </summary>
        public string AvailableWorkflows { get; set; }

        /// <summary>
        /// 用户画像摘要（可选）
        /// </summary>
        public string UserProfileSummary { get; set; }

        /// <summary>
        /// 应用ID
        /// </summary>
        public string AppID { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionID { get; set; }

        /// <summary>
        /// 成员ID
        /// </summary>
        public string MemberID { get; set; }

        /// <summary>
        /// 模型配置（用于主控判断的LLM模型）
        /// </summary>
        public ZSN.AI.Entity.LargeModelConfig ModelConfig { get; set; }

        /// <summary>
        /// 主控判断提示词模板（由前端配置）
        /// </summary>
        public string PromptTemplate { get; set; }
    }
}
