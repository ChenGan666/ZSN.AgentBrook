namespace ZSN.AI.Entity
{
    /// <summary>
    /// IM平台类型
    /// </summary>
    public enum MessageProviderType
    {
        /// <summary>
        /// 企业微信
        /// </summary>
        WeChatWork = 1,
        /// <summary>
        /// WhatsApp
        /// </summary>
        WhatsApp = 2,
        /// <summary>
        /// 钉钉
        /// </summary>
        DingTalk = 3,
        /// <summary>
        /// 飞书
        /// </summary>
        Feishu = 4,
        /// <summary>
        /// 测试用（模拟发送，不连接真实IM）
        /// </summary>
        Test = 99
    }

    /// <summary>
    /// 消息流向
    /// </summary>
    public enum MessageFlowDirection
    {
        /// <summary>
        /// 仅发送
        /// </summary>
        SendOnly = 1,
        /// <summary>
        /// 仅接收
        /// </summary>
        ReceiveOnly = 2,
        /// <summary>
        /// 双向
        /// </summary>
        Bidirectional = 3
    }
}
