using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ZSN.AI.Node.ResearchNode.Models;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.Node.ResearchNode.Services
{
    public class RedisContentCache : IContentCache
    {
        private readonly IOptions<ResearchNodeOptions> _options;
        private readonly ILogger<RedisContentCache> _logger;
        private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(24);

        public RedisContentCache(
            IOptions<ResearchNodeOptions> options,
            ILogger<RedisContentCache> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<WebPageContent> GetAsync(string url)
        {
            try
            {
                var redis = new RedisHelper();
                var key = $"research:content:{ComputeHash(url)}";
                var json = await redis.StringGetAsync(key, null);
                if (string.IsNullOrEmpty(json)) return null;
                return JsonConvert.DeserializeObject<WebPageContent>(json);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[ContentCache] Redis 读取失败: {Url}", url);
                return null;
            }
        }

        public async Task SetAsync(string url, WebPageContent content, TimeSpan? expiry = null)
        {
            try
            {
                var redis = new RedisHelper();
                var key = $"research:content:{ComputeHash(url)}";
                var json = JsonConvert.SerializeObject(content);
                var hours = _options.Value.CacheExpiryHours > 0 ? _options.Value.CacheExpiryHours : 24;
                await redis.StringSetAsync(key, json, expiry ?? TimeSpan.FromHours(hours));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[ContentCache] Redis 写入失败: {Url}", url);
            }
        }

        private static string ComputeHash(string url)
        {
            var hash = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(url));
            return Convert.ToHexString(hash).Substring(0, 16);
        }
    }
}
