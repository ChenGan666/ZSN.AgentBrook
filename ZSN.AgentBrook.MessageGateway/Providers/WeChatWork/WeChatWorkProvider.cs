using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.MessageGateway.Providers.WeChatWork
{
    public class WeChatWorkProvider : IMessageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WeChatWorkProvider> _logger;
        private readonly ConcurrentDictionary<string, (string Token, DateTime Expiry)> _tokenCache = new();

        public string ProviderType => MessageProviderType.WeChatWork.ToString();

        public WeChatWorkProvider(IHttpClientFactory httpClientFactory, ILogger<WeChatWorkProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<SendResult> SendAsync(SendMessageRequest request, ChannelConfigInfo channelConfig, CancellationToken ct = default)
        {
            var cfg = ParseConfig(channelConfig.ConfigJson);
            var token = await GetAccessTokenAsync(cfg, ct);

            var body = new Dictionary<string, object>
            {
                ["touser"] = request.TargetUser,
                ["msgtype"] = request.MessageType == "markdown" ? "markdown" : "text",
                ["agentid"] = cfg.AgentId
            };

            if (request.MessageType == "markdown")
                body["markdown"] = new { content = request.Content };
            else
                body["text"] = new { content = request.Content };

            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"));
            var response = await client.PostAsync(
                $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={token}", content, ct);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync(ct));
            var errcode = json["errcode"]?.Value<int>() ?? -1;

            return new SendResult
            {
                Success = errcode == 0,
                PlatformMessageId = json["msgid"]?.ToString(),
                ErrorMessage = errcode != 0 ? $"errcode={errcode}, {json["errmsg"]}" : null,
                SendTime = DateTime.Now
            };
        }

        public Task<bool> ValidateWebhookAsync(WebhookContext context, ChannelConfigInfo channelConfig)
        {
            var cfg = ParseConfig(channelConfig.ConfigJson);
            var timestamp = context.QueryParams?.GetValueOrDefault("timestamp", "");
            var nonce = context.QueryParams?.GetValueOrDefault("nonce", "");
            var msgSignature = context.QueryParams?.GetValueOrDefault("msg_signature", "");
            var echostr = context.QueryParams?.GetValueOrDefault("echostr", "");

            if (!string.IsNullOrEmpty(echostr))
            {
                var calculated = SHA1Hash(SortStrings(cfg.Token, timestamp, nonce, echostr));
                return Task.FromResult(calculated == msgSignature);
            }

            var encrypt = ExtractEncryptFromBody(context.Body);
            if (string.IsNullOrEmpty(encrypt))
                return Task.FromResult(false);

            var sig = SHA1Hash(SortStrings(cfg.Token, timestamp, nonce, encrypt));
            return Task.FromResult(sig == msgSignature);
        }

        public ReceiveMessageEvent ParseWebhookEvent(WebhookContext context, ChannelConfigInfo channelConfig)
        {
            var cfg = ParseConfig(channelConfig.ConfigJson);
            var encrypt = ExtractEncryptFromBody(context.Body);
            if (string.IsNullOrEmpty(encrypt))
                return null;

            var decrypted = AESDecrypt(encrypt, cfg.EncodingAESKey);
            var xml = XElement.Parse(decrypted);

            var msgType = xml.Element("MsgType")?.Value ?? "text";
            if (msgType == "event")
                return null;

            return new ReceiveMessageEvent
            {
                EventId = xml.Element("MsgId")?.Value ?? Guid.NewGuid().ToString(),
                ProviderType = ProviderType,
                FromUser = xml.Element("FromUserName")?.Value ?? "",
                FromUserName = "",
                MessageType = msgType == "text" ? "text" : msgType,
                Content = xml.Element("Content")?.Value ?? "",
                RawData = context.Body,
                ReceiveTime = DateTime.Now
            };
        }

        public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
        {
            return await Task.FromResult(_tokenCache.Values.Any(v => v.Expiry > DateTime.UtcNow));
        }

        private async Task<string> GetAccessTokenAsync(WeChatWorkConfig cfg, CancellationToken ct)
        {
            string cacheKey = $"{cfg.CorpId}_{cfg.AgentId}";
            if (_tokenCache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
                return cached.Token;

            var client = _httpClientFactory.CreateClient();
            var resp = await client.GetStringAsync(
                $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={cfg.CorpId}&corpsecret={cfg.CorpSecret}", ct);
            var json = JObject.Parse(resp);

            var errcode = json["errcode"]?.Value<int>() ?? -1;
            if (errcode != 0)
                throw new Exception($"获取access_token失败: {json["errmsg"]}");

            var token = json["access_token"]?.ToString();
            var expiresIn = json["expires_in"]?.Value<int>() ?? 7200;

            _tokenCache[cacheKey] = (token, DateTime.UtcNow.AddSeconds(expiresIn - 300));
            return token;
        }

        private WeChatWorkConfig ParseConfig(string configJson)
        {
            return JsonConvert.DeserializeObject<WeChatWorkConfig>(configJson ?? "{}");
        }

        private static string ExtractEncryptFromBody(string body)
        {
            try
            {
                var xml = XElement.Parse(body);
                return xml.Element("Encrypt")?.Value ?? "";
            }
            catch { return ""; }
        }

        private static string SHA1Hash(string input)
        {
            using var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        private static string SortStrings(params string[] values)
        {
            return string.Join("", values.OrderBy(v => v, StringComparer.Ordinal));
        }

        private static string AESDecrypt(string encrypted, string encodingAESKey)
        {
            var key = Convert.FromBase64String(encodingAESKey + "=");
            var iv = key.Take(16).ToArray();
            var data = Convert.FromBase64String(encrypted);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);

            var msgLen = BitConverter.ToInt32(decrypted, 16);
            if (BitConverter.IsLittleEndian) msgLen = BitConverter.ToInt32(BitConverter.GetBytes(msgLen).Reverse().ToArray(), 0);
            var msgBytes = decrypted.Skip(20).Take(msgLen).ToArray();
            return Encoding.UTF8.GetString(msgBytes);
        }
    }

    public class WeChatWorkConfig
    {
        public string CorpId { get; set; }
        public string CorpSecret { get; set; }
        public int AgentId { get; set; }
        public string Token { get; set; }
        public string EncodingAESKey { get; set; }
    }
}
