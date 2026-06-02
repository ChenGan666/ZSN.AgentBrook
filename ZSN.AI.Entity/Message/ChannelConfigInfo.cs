using System;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_msg_channel_config
    /// </summary>
    public partial class ChannelConfigInfo
    {
        public ChannelConfigInfo() { }

        /// <summary>ChannelID</summary>
        public string ChannelID { get; set; } = string.Empty;

        /// <summary>渠道显示名称</summary>
        public string ChannelName { get; set; } = string.Empty;

        /// <summary>Provider类型: MessageProviderType枚举</summary>
        public Int32 ProviderType { get; set; } = (int)MessageProviderType.WeChatWork;

        /// <summary>Provider特定配置JSON（加密存储）</summary>
        public string ConfigJson { get; set; } = string.Empty;

        /// <summary>流向: MessageFlowDirection枚举</summary>
        public Int32 FlowDirection { get; set; } = (int)MessageFlowDirection.Bidirectional;

        /// <summary>接收流向: 目标应用ID</summary>
        public string TargetAppID { get; set; }

        /// <summary>接收流向: 会话超时分钟数</summary>
        public Int32 SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>是否启用</summary>
        public Int32 Enabled { get; set; } = 1;

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
}
