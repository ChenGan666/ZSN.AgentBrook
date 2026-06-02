using System.Text;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.MessageGateway.Providers.DingTalk
{
    public class DingTalkProvider : IMessageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DingTalkProvider> _logger;
        private readonly ConcurrentDictionary<string, (string Token, DateTime Expiry)> _tokenCache = new();

        public string ProviderType => MessageProviderType.DingTalk.ToString();

        public DingTalkProvider(IHttpClientFactory httpClientFactory, ILogger<DingTalkProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<SendResult> SendAsync(SendMessageRequest request, ChannelConfigInfo channelConfig, CancellationToken ct = default)
        {
            var cfg = ParseConfig(channelConfig.ConfigJson);
            var token = await GetAccessTokenAsync(cfg, ct);

            var msgKey = request.MessageType == "markdown" ? "sampleMarkdown" : "sampleText";
            var msgParam = request.MessageType == "markdown"
                ? JsonConvert.SerializeObject(new { title = "通知", text = request.Content })
                : JsonConvert.SerializeObject(new { content = request.Content });

            var body = new
            {
                agent_id = cfg.AgentId,
                userid_list = request.TargetUser,
                msgKey,
                msgParam
            };

            var client = _httpClientFactory.CreateClient();
            var sendContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"));
            var response = await client.PostAsync(
                $"https://oapi.dingtalk.com/topapi/message/corpconversation/asyncsend_v2?access_token={token}",
                sendContent, ct);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync(ct));
            var errcode = json["errcode"]?.Value<int>() ?? -1;

            return new SendResult
            {
                Success = errcode == 0,
                PlatformMessageId = json["task_id"]?.ToString(),
                ErrorMessage = errcode != 0 ? $"errcode={errcode}, {json["errmsg"]}" : null,
                SendTime = DateTime.Now
            };
        }

        public Task<bool> ValidateWebhookAsync(WebhookContext context, ChannelConfigInfo channelConfig)
        {
            var cfg = ParseConfig(channelConfig.ConfigJson);
            var token = context.QueryParams?.GetValueOrDefault("token", "");
            return Task.FromResult(token == cfg.Token);
        }

        public ReceiveMessageEvent ParseWebhookEvent(WebhookContext context, ChannelConfigInfo channelConfig)
        {
            var json = JObject.Parse(context.Body ?? "{}");

            var msgType = json["msgtype"]?.ToString() ?? "text";
            var content = msgType == "text"
                ? json["text"]?["content"]?.ToString()
                : json[msgType]?.ToString();

            return new ReceiveMessageEvent
            {
                EventId = json["messageId"]?.ToString() ?? Guid.NewGuid().ToString(),
                ProviderType = ProviderType,
                FromUser = json["senderStaffId"]?.ToString() ?? json["senderId"]?.ToString() ?? "",
                FromUserName = json["senderNick"]?.ToString() ?? "",
                MessageType = msgType,
                Content = content ?? "",
                RawData = context.Body,
                ReceiveTime = DateTime.Now
            };
        }

        public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
        {
            return await Task.FromResult(_tokenCache.Values.Any(v => v.Expiry > DateTime.UtcNow));
        }

        private async Task<string> GetAccessTokenAsync(DingTalkConfig cfg, CancellationToken ct)
        {
            string cacheKey = $"{cfg.AppKey}";
            if (_tokenCache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
                return cached.Token;

            var client = _httpClientFactory.CreateClient();
            var body = new { appKey = cfg.AppKey, appSecret = cfg.AppSecret };
            var tokenContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"));
            var response = await client.PostAsync(
                "https://oapi.dingtalk.com/gettoken", tokenContent, ct);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync(ct));
            var errcode = json["errcode"]?.Value<int>() ?? -1;
            if (errcode != 0)
                throw new Exception($"获取access_token失败: {json["errmsg"]}");

            var token = json["access_token"]?.ToString();
            var expiresIn = json["expires_in"]?.Value<int>() ?? 7200;

            _tokenCache[cacheKey] = (token, DateTime.UtcNow.AddSeconds(expiresIn - 300));
            return token;
        }

        private DingTalkConfig ParseConfig(string configJson)
        {
            return JsonConvert.DeserializeObject<DingTalkConfig>(configJson ?? "{}");
        }
    }

    public class DingTalkConfig
    {
        public string AppKey { get; set; }
        public string AppSecret { get; set; }
        public long AgentId { get; set; }
        public string Token { get; set; }
    }
}
