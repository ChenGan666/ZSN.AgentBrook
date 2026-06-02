namespace ZSN.AgentBrook.MessageGateway.Configuration
{
    public class GatewayOptions
    {
        public string SendQueueName { get; set; } = "msg_send_queue";
        public int MaxConcurrentSends { get; set; } = 5;
        public int SendTimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public int RetryIntervalSeconds { get; set; } = 5;
        public bool EnableSendLog { get; set; } = true;
        public bool EnableReceiveLog { get; set; } = true;
        public bool EnableMessageRouting { get; set; } = true;
        public int DefaultSessionTimeoutMinutes { get; set; } = 30;
        public bool EnableSessionReuse { get; set; } = true;
        public string WebhookBaseUrl { get; set; }
        public int CircuitBreakerThreshold { get; set; } = 3;
        public int CircuitBreakerRecoverySeconds { get; set; } = 60;
    }
}
