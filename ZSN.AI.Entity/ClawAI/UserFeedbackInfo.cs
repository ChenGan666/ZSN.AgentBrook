using System;

namespace ZSN.AI.Entity.ClawAI
{
    /// <summary>
    /// tb_claw_user_feedback - Claw AI用户反馈表
    /// 用于收集用户对AI回答的反馈，动态调整知识重要性
    /// </summary>
    public partial class UserFeedbackInfo
    {
        public UserFeedbackInfo() { }

        #region AutoField

        /// <summary>
        /// 反馈ID
        /// </summary>
        public string FeedbackID { get; set; } = string.Empty;

        /// <summary>
        /// 应用ID
        /// </summary>
        public string AppID { get; set; } = string.Empty;

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionID { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID
        /// </summary>
        public string MemberID { get; set; } = string.Empty;

        /// <summary>
        /// 关联的记忆ID
        /// </summary>
        public string MemoryID { get; set; } = string.Empty;

        /// <summary>
        /// 用户提问
        /// </summary>
        public string UserQuery { get; set; } = string.Empty;

        /// <summary>
        /// AI回答
        /// </summary>
        public string AIResponse { get; set; } = string.Empty;

        /// <summary>
        /// 反馈类型: positive(正面), negative(负面), neutral(中性)
        /// </summary>
        public string FeedbackType { get; set; } = string.Empty;

        /// <summary>
        /// 评分(1-5)
        /// </summary>
        public int FeedbackScore { get; set; } = 3;

        /// <summary>
        /// 反馈评论
        /// </summary>
        public string FeedbackComment { get; set; } = string.Empty;

        /// <summary>
        /// 使用的记忆ID列表(JSON格式)
        /// </summary>
        public string UsedMemories { get; set; } = string.Empty;

        /// <summary>
        /// 元数据(JSON格式,存储额外信息)
        /// </summary>
        public string Metadata { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        #endregion
    }

    /// <summary>
    /// 反馈类型枚举
    /// </summary>
    public enum FeedbackType
    {
        /// <summary>
        /// 正面反馈
        /// </summary>
        Positive,

        /// <summary>
        /// 负面反馈
        /// </summary>
        Negative,

        /// <summary>
        /// 中性反馈
        /// </summary>
        Neutral
    }

    /// <summary>
    /// 知识反馈统计信息
    /// </summary>
    public class KnowledgeFeedbackStats
    {
        /// <summary>
        /// 记忆ID
        /// </summary>
        public string MemoryID { get; set; } = string.Empty;

        /// <summary>
        /// 总反馈数
        /// </summary>
        public int TotalFeedbacks { get; set; }

        /// <summary>
        /// 正面反馈数
        /// </summary>
        public int PositiveCount { get; set; }

        /// <summary>
        /// 负面反馈数
        /// </summary>
        public int NegativeCount { get; set; }

        /// <summary>
        /// 中性反馈数
        /// </summary>
        public int NeutralCount { get; set; }

        /// <summary>
        /// 平均评分
        /// </summary>
        public float AverageScore { get; set; }

        /// <summary>
        /// 正面率
        /// </summary>
        public float PositiveRate { get; set; }

        /// <summary>
        /// 最后反馈时间
        /// </summary>
        public DateTime? LastFeedbackTime { get; set; }
    }
}
