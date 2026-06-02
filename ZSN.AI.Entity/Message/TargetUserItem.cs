using System;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// 目标用户项（MessageNode 使用）
    /// </summary>
    public class TargetUserItem
    {
        /// <summary>IM平台用户唯一标识（区别于系统UserID）</summary>
        public string IMUserID { get; set; } = string.Empty;

        /// <summary>IM平台用户显示名称（可选，用于日志展示）</summary>
        public string IMUserName { get; set; } = string.Empty;

        /// <summary>该用户个性化消息覆盖（可选，为空则使用全局 MessageTemplate）</summary>
        public string ContentOverride { get; set; } = string.Empty;
    }
}
