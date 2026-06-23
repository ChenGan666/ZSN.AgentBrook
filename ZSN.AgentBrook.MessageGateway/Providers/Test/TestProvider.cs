using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;

namespace ZSN.AgentBrook.MessageGateway.Providers.Test
{
    public class TestProvider : IMessageProvider
    {
        private readonly ILogger<TestProvider> _logger;

        public string ProviderType => MessageProviderType.Test.ToString();

        public TestProvider(ILogger<TestProvider> logger)
        {
            _logger = logger;
        }

        public Task<SendResult> SendAsync(SendMessageRequest request, ChannelConfigInfo channelConfig, CancellationToken ct = default)
        {
            _logger.LogInformation("[TestProvider] 模拟发送: To={User}, Type={Type}, Content={Content}",
                request.TargetUser, request.MessageType,
                (request.Content ?? "").Length > 50 ? request.Content[..50] + "..." : request.Content);

            return Task.FromResult(new SendResult
            {
                Success = true,
                PlatformMessageId = $"test_msg_{Guid.NewGuid():N}",
                ErrorMessage = null,
                SendTime = DateTime.Now
            });
        }

        public Task<bool> ValidateWebhookAsync(WebhookContext context, ChannelConfigInfo channelConfig)
            => Task.FromResult(true);

        public ReceiveMessageEvent ParseWebhookEvent(WebhookContext context, ChannelConfigInfo channelConfig)
        {
            string content = context.Body ?? "";
            string fromUser = "test_user";
            string fromUserName = "测试用户";
            string msgType = "text";
            string eventId = Guid.NewGuid().ToString();
            List<AttachmentItem> attachments = new List<AttachmentItem>();
            dynamic additionalOptions = null;

            try
            {
                var json = JObject.Parse(context.Body ?? "{}");
                if (json.TryGetValue("content", out var c)) content = c.ToString();
                else if (json.TryGetValue("message", out var m)) content = m.ToString();
                if (json.TryGetValue("fromUser", out var u)) fromUser = u.ToString();
                if (json.TryGetValue("fromUserName", out var n)) fromUserName = n.ToString();
                if (json.TryGetValue("fromUserName", StringComparison.OrdinalIgnoreCase, out var n2)) fromUserName = n2.ToString();
                if (json.TryGetValue("msgType", out var t)) msgType = t.ToString();
                if (json.TryGetValue("eventId", out var e)) eventId = e.ToString();
                if (json.TryGetValue("attachments", out var att))
                    attachments = att.ToObject<List<AttachmentItem>>() ?? new List<AttachmentItem>();
                if (json.TryGetValue("additionalOptions", out var opt))
                    additionalOptions = opt;
            }
            catch { }

            return new ReceiveMessageEvent
            {
                EventId = eventId,
                ProviderType = ProviderType,
                FromUser = fromUser,
                FromUserName = fromUserName,
                MessageType = msgType,
                Content = content,
                RawData = context.Body,
                ReceiveTime = DateTime.Now,
                Attachments = attachments,
                AdditionalOptions = additionalOptions
            };
        }

        public Task<bool> IsHealthyAsync(CancellationToken ct = default)
            => Task.FromResult(true);
    }
}
