using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.MessageGateway.Providers.WhatsApp
{
    public class WhatsAppProvider : IMessageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WhatsAppProvider> _logger;

        public string ProviderType => MessageProviderType.WhatsApp.ToString();

        public WhatsAppProvider(IHttpClientFactory httpClientFactory, ILogger<WhatsAppProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<SendResult> SendAsync(SendMessageRequest request, ChannelConfigInfo channelConfig, CancellationToken ct = default)
        {
            var cfg = ParseConfig(channelConfig.ConfigJson);

            var body = new Dictionary<string, object>
            {
                ["messaging_product"] = "whatsapp",
                ["to"] = request.TargetUser,
                ["type"] = "text",
                ["text"] = new { preview_url = false, body = request.Content }
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg.AccessToken);

            var httpContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"));
            var response = await client.PostAsync(
                $"https://graph.facebook.com/v21.0/{cfg.PhoneNumberId}/messages", httpContent, ct);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync(ct));

            var messageId = json["messages"]?[0]?["id"]?.ToString();
            var error = json["error"]?["message"]?.ToString();

            return new SendResult
            {
                Success = messageId != null,
                PlatformMessageId = messageId,
                ErrorMessage = error,
                SendTime = DateTime.Now
            };
        }

        public Task<bool> ValidateWebhookAsync(WebhookContext context, ChannelConfigInfo channelConfig)
        {
            var cfg = ParseConfig(channelConfig.ConfigJson);
            var signature = context.Headers?.GetValueOrDefault("X-Hub-Signature-256", "");
            if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256="))
                return Task.FromResult(false);

            var expectedHash = signature.Substring(7);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(cfg.AppSecret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(context.Body ?? ""));
            var computedHex = BitConverter.ToString(computedHash).Replace("-", "").ToLowerInvariant();

            return Task.FromResult(computedHex == expectedHash.ToLowerInvariant());
        }

        public ReceiveMessageEvent ParseWebhookEvent(WebhookContext context, ChannelConfigInfo channelConfig)
        {
            var json = JObject.Parse(context.Body ?? "{}");

            var entry = json["entry"]?[0]?["changes"]?[0]?["value"];
            var message = entry?["messages"]?[0];
            if (message == null)
                return null;

            return new ReceiveMessageEvent
            {
                EventId = message["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                ProviderType = ProviderType,
                FromUser = message["from"]?.ToString() ?? "",
                FromUserName = entry?["contacts"]?[0]?["profile"]?["name"]?.ToString() ?? "",
                MessageType = message["type"]?.ToString() ?? "text",
                Content = message["text"]?["body"]?.ToString() ?? "",
                RawData = context.Body,
                ReceiveTime = DateTime.Now
            };
        }

        public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
        {
            return await Task.FromResult(true);
        }

        private WhatsAppConfig ParseConfig(string configJson)
        {
            return JsonConvert.DeserializeObject<WhatsAppConfig>(configJson ?? "{}");
        }
    }

    public class WhatsAppConfig
    {
        public string PhoneNumberId { get; set; }
        public string AccessToken { get; set; }
        public string VerifyToken { get; set; }
        public string AppSecret { get; set; }
    }
}
