using System.Text;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.MessageGateway.Providers.Feishu
{
    public class FeishuProvider : IMessageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FeishuProvider> _logger;
        private readonly ConcurrentDictionary<string, (string Token, DateTime Expiry)> _tokenCache = new();

        public string ProviderType => MessageProviderType.Feishu.ToString();

        public FeishuProvider(IHttpClientFactory httpClientFactory, ILogger<FeishuProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<SendResult> SendAsync(SendMessageRequest request, ChannelConfigInfo channelConfig, CancellationToken ct = default)
        {
            var cfg = ParseConfig(channelConfig.ConfigJson);
            var token = await GetTenantAccessTokenAsync(cfg, ct);

            var contentObj = request.MessageType == "markdown"
                ? JsonConvert.SerializeObject(new { text = request.Content })
                : JsonConvert.SerializeObject(new { text = request.Content });

            var body = new
            {
                receive_id = request.TargetUser,
                msg_type = request.MessageType == "markdown" ? "post" : "text",
                content = contentObj
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var msgContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"));
            var response = await client.PostAsync(
                "https://open.feishu.cn/open-apis/im/v1/messages?receive_id_type=open_id", msgContent, ct);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync(ct));
            var code = json["code"]?.Value<int>() ?? -1;

            return new SendResult
            {
                Success = code == 0,
                PlatformMessageId = json["data"]?["message_id"]?.ToString(),
                ErrorMessage = code != 0 ? $"code={code}, {json["msg"]}" : null,
                SendTime = DateTime.Now
            };
        }

        public Task<bool> ValidateWebhookAsync(WebhookContext context, ChannelConfigInfo channelConfig)
        {
            var cfg = ParseConfig(channelConfig.ConfigJson);
            var json = JObject.Parse(context.Body ?? "{}");

            // URL验证：飞书发送 challenge
            var challenge = json["challenge"]?.ToString();
            var token = json["token"]?.ToString();
            if (!string.IsNullOrEmpty(challenge))
                return Task.FromResult(token == cfg.VerificationToken);

            // 事件推送验证：检查 header 中的 token
            var headerToken = json["header"]?["token"]?.ToString();
            return Task.FromResult(headerToken == cfg.VerificationToken);
        }

        public ReceiveMessageEvent ParseWebhookEvent(WebhookContext context, ChannelConfigInfo channelConfig)
        {
            var json = JObject.Parse(context.Body ?? "{}");

            var eventType = json["header"]?["event_type"]?.ToString();
            if (eventType != "im.message.receive_v1")
                return null;

            var eventId = json["header"]?["event_id"]?.ToString() ?? Guid.NewGuid().ToString();
            var sender = json["event"]?["sender"]?["sender_id"]?["open_id"]?.ToString() ?? "";
            var message = json["event"]?["message"];
            var msgType = message?["message_type"]?.ToString() ?? "text";

            var contentStr = message?["content"]?.ToString() ?? "{}";
            var contentJson = JObject.Parse(contentStr);
            var text = contentJson["text"]?.ToString() ?? contentStr;

            return new ReceiveMessageEvent
            {
                EventId = eventId,
                ProviderType = ProviderType,
                FromUser = sender,
                FromUserName = "",
                MessageType = msgType,
                Content = text,
                RawData = context.Body,
                ReceiveTime = DateTime.Now
            };
        }

        public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
        {
            return await Task.FromResult(_tokenCache.Values.Any(v => v.Expiry > DateTime.UtcNow));
        }

        private async Task<string> GetTenantAccessTokenAsync(FeishuConfig cfg, CancellationToken ct)
        {
            string cacheKey = cfg.AppId;
            if (_tokenCache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
                return cached.Token;

            var client = _httpClientFactory.CreateClient();
            var body = new { app_id = cfg.AppId, app_secret = cfg.AppSecret };
            var authContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"));
            var response = await client.PostAsync(
                "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal", authContent, ct);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync(ct));
            var code = json["code"]?.Value<int>() ?? -1;
            if (code != 0)
                throw new Exception($"获取tenant_access_token失败: {json["msg"]}");

            var token = json["tenant_access_token"]?.ToString();
            var expire = json["expire"]?.Value<int>() ?? 7200;

            _tokenCache[cacheKey] = (token, DateTime.UtcNow.AddSeconds(expire - 300));
            return token;
        }

        private FeishuConfig ParseConfig(string configJson)
        {
            return JsonConvert.DeserializeObject<FeishuConfig>(configJson ?? "{}");
        }
    }

    public class FeishuConfig
    {
        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string VerificationToken { get; set; }
        public string EncryptKey { get; set; }
    }
}
