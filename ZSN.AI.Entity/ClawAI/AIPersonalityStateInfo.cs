using System;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_ai_personality_state AI个性状态表
    /// </summary>
    public partial class AIPersonalityStateInfo
    {
        public AIPersonalityStateInfo() { }

        #region AutoField
        /// <summary>
        /// 状态ID
        /// </summary>
        public string StateID { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionID { get; set; }

        /// <summary>
        /// 应用ID
        /// </summary>
        public string AppID { get; set; }

        /// <summary>
        /// AI个性特征(JSON)
        /// </summary>
        public string PersonalityTraits { get; set; }

        /// <summary>
        /// 情绪状态(JSON)
        /// </summary>
        public string EmotionalState { get; set; }

        /// <summary>
        /// 当前目标(JSON)
        /// </summary>
        public string CurrentGoals { get; set; }

        /// <summary>
        /// 交互次数
        /// </summary>
        public int InteractionCount { get; set; }

        /// <summary>
        /// 成功率(%)
        /// </summary>
        public decimal SuccessRate { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; }
        #endregion
    }
}
