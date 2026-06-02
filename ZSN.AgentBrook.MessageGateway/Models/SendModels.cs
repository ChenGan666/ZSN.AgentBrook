namespace ZSN.AgentBrook.MessageGateway.Models
{
    public class SendMessageRequest
    {
        public string ChannelID { get; set; }
        public string MessageType { get; set; } = "text";
        public string Content { get; set; }
        public string TargetUser { get; set; }
        public string TargetName { get; set; }
        public Dictionary<string, string> ExtraParams { get; set; }
    }

    public class SendResult
    {
        public bool Success { get; set; }
        public string PlatformMessageId { get; set; }
        public string ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime SendTime { get; set; }
    }

    public class MessageSendTask
    {
        public string RecordID { get; set; }
        public string ChannelID { get; set; }
        public string MessageType { get; set; }
        public string Content { get; set; }
        public string TargetUser { get; set; }
        public string TargetName { get; set; }
        public Dictionary<string, string> ExtraParams { get; set; }
        public string SessionID { get; set; }
        public string TaskID { get; set; }
        public string NodeID { get; set; }
        public DateTime EnqueueTime { get; set; } = DateTime.Now;
    }
}
