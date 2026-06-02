namespace ZSN.AI.Node.MessageNode
{
    /// <summary>
    /// MessageNode 节点配置选项
    /// </summary>
    public class MessageNodeOptions
    {
        /// <summary>Redis 发送队列名称</summary>
        public string SendQueueName { get; set; } = "msg_send_queue";

        /// <summary>WaitForConfirmation=true 时最大等待秒数</summary>
        public int WaitTimeoutSeconds { get; set; } = 30;

        /// <summary>轮询 tb_msg_send_record 状态的间隔（毫秒）</summary>
        public int PollIntervalMs { get; set; } = 500;

        /// <summary>是否记录详细日志</summary>
        public bool VerboseLogging { get; set; } = false;
    }
}
