using ZSN.AI.Entity.Chat;

namespace ZSN.AgentBrook.MessageGateway.Models
{
    public class ReceiveMessageEvent
    {
        public string EventId { get; set; }
        public string ProviderType { get; set; }
        public string ChannelID { get; set; }
        public string FromUser { get; set; }
        public string FromUserName { get; set; }
        public string MessageType { get; set; }
        public string Content { get; set; }
        public string RawData { get; set; }
        public DateTime ReceiveTime { get; set; } = DateTime.Now;
        public List<AttachmentItem> Attachments { get; set; } = new List<AttachmentItem>();
        public dynamic AdditionalOptions { get; set; } = null;
    }

    public class WebhookContext
    {
        public string ProviderType { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> QueryParams { get; set; }
    }
}
