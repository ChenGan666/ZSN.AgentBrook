using System;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_user_profile 用户画像表
    /// </summary>
    public partial class UserProfileInfo
    {
        public UserProfileInfo() { }

        #region AutoField
        /// <summary>
        /// 画像ID
        /// </summary>
        public string ProfileID { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public string MemberID { get; set; }

        /// <summary>
        /// 应用ID
        /// </summary>
        public string AppID { get; set; }

        /// <summary>
        /// 用户偏好摘要
        /// </summary>
        public string PreferencesSummary { get; set; }

        /// <summary>
        /// 用户偏好详情(JSON)
        /// </summary>
        public string PreferencesDetail { get; set; }

        /// <summary>
        /// 交互模式摘要
        /// </summary>
        public string InteractionPatternsSummary { get; set; }

        /// <summary>
        /// 交互模式详情(JSON)
        /// </summary>
        public string InteractionPatternsDetail { get; set; }

        /// <summary>
        /// 个性化响应强度(0-100)
        /// </summary>
        public int PersonalizationStrength { get; set; }

        /// <summary>
        /// 总交互次数
        /// </summary>
        public int TotalInteractions { get; set; }

        /// <summary>
        /// 最后交互时间
        /// </summary>
        public DateTime LastInteractionTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;
        #endregion
    }
}
