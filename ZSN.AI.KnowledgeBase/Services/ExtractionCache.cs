using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 缓存配置
    /// </summary>
    public class ExtractionCacheConfig
    {
        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCache { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（分钟，默认60分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// 最大缓存条目数
        /// </summary>
        public int MaxCacheSize { get; set; } = 1000;

        /// <summary>
        /// 缓存键前缀
        /// </summary>
        public string CacheKeyPrefix { get; set; } = "extraction_";
    }

    /// <summary>
    /// 缓存条目
    /// </summary>
    public class CacheEntry<T>
    {
        public T Data { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int HitCount { get; set; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// 提取结果缓存
    /// 基于文本哈希缓存实体和关系提取结果，避免重复计算
    /// </summary>
    public class ExtractionCache
    {
        private readonly ExtractionCacheConfig _config;
        private readonly ILogger<ExtractionCache> _logger;
        private readonly ConcurrentDictionary<string, CacheEntry<List<ZSN.AI.Entity.KnowledgeBase.Entity>>> _entityCache;
        private readonly ConcurrentDictionary<string, CacheEntry<List<Relation>>> _relationCache;
        private readonly Timer _cleanupTimer;

        public ExtractionCache(
            ExtractionCacheConfig? config = null,
            ILogger<ExtractionCache>? logger = null)
        {
            _config = config ?? new ExtractionCacheConfig();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ExtractionCache>.Instance;

            _entityCache = new ConcurrentDictionary<string, CacheEntry<List<ZSN.AI.Entity.KnowledgeBase.Entity>>>();
            _relationCache = new ConcurrentDictionary<string, CacheEntry<List<Relation>>>();

            // 定期清理过期缓存（每5分钟）
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            //_logger.LogInformation("提取缓存初始化完成，过期时间: {Minutes}分钟，最大缓存数: {MaxSize}",_config.CacheExpirationMinutes, _config.MaxCacheSize);
        }

        /// <summary>
        /// 获取实体提取缓存
        /// </summary>
        public bool TryGetEntities(
            string text,
            EntityExtractionConfig config,
            out List<ZSN.AI.Entity.KnowledgeBase.Entity> entities)
        {
            if (!_config.EnableCache)
            {
                entities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
                return false;
            }

            var cacheKey = GenerateCacheKey(text, config);

            if (_entityCache.TryGetValue(cacheKey, out var entry))
            {
                if (entry.IsExpired)
                {
                    // 缓存已过期，删除
                    _entityCache.TryRemove(cacheKey, out _);
                    _logger.LogDebug("实体缓存已过期: {Key}", cacheKey);
                    entities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
                    return false;
                }

                // 缓存命中
                entry.HitCount++;
                entities = CloneEntities(entry.Data);
                _logger.LogDebug("实体缓存命中: {Key}, 命中次数: {Count}", cacheKey, entry.HitCount);
                return true;
            }

            _logger.LogDebug("实体缓存未命中: {Key}", cacheKey);
            entities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
            return false;
        }

        /// <summary>
        /// 缓存实体提取结果
        /// </summary>
        public void SetEntities(
            string text,
            EntityExtractionConfig config,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities)
        {
            if (!_config.EnableCache)
                return;

            // 检查缓存大小限制
            if (_entityCache.Count >= _config.MaxCacheSize)
            {
                // 清理最旧的缓存条目
                CleanupOldestEntries(_entityCache, _config.MaxCacheSize / 10);
            }

            var cacheKey = GenerateCacheKey(text, config);
            var expiration = DateTime.UtcNow.AddMinutes(_config.CacheExpirationMinutes);

            var entry = new CacheEntry<List<ZSN.AI.Entity.KnowledgeBase.Entity>>
            {
                Data = CloneEntities(entities),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiration,
                HitCount = 0
            };

            _entityCache.TryAdd(cacheKey, entry);
            _logger.LogDebug("实体缓存已添加: {Key}, 过期时间: {Expiration}, 当前缓存数: {Count}",
                cacheKey, expiration, _entityCache.Count);
        }

        /// <summary>
        /// 获取关系抽取缓存
        /// </summary>
        public bool TryGetRelations(
            string text,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            out List<Relation> relations)
        {
            if (!_config.EnableCache)
            {
                relations = new List<Relation>();
                return false;
            }

            var cacheKey = GenerateRelationCacheKey(text, entities);

            if (_relationCache.TryGetValue(cacheKey, out var entry))
            {
                if (entry.IsExpired)
                {
                    _relationCache.TryRemove(cacheKey, out _);
                    _logger.LogDebug("关系缓存已过期: {Key}", cacheKey);
                    relations = new List<Relation>();
                    return false;
                }

                entry.HitCount++;
                relations = CloneRelations(entry.Data);
                _logger.LogDebug("关系缓存命中: {Key}, 命中次数: {Count}", cacheKey, entry.HitCount);
                return true;
            }

            _logger.LogDebug("关系缓存未命中: {Key}", cacheKey);
            relations = new List<Relation>();
            return false;
        }

        /// <summary>
        /// 缓存关系抽取结果
        /// </summary>
        public void SetRelations(
            string text,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            List<Relation> relations)
        {
            if (!_config.EnableCache)
                return;

            // 检查缓存大小限制
            if (_relationCache.Count >= _config.MaxCacheSize)
            {
                CleanupOldestEntries(_relationCache, _config.MaxCacheSize / 10);
            }

            var cacheKey = GenerateRelationCacheKey(text, entities);
            var expiration = DateTime.UtcNow.AddMinutes(_config.CacheExpirationMinutes);

            var entry = new CacheEntry<List<Relation>>
            {
                Data = CloneRelations(relations),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiration,
                HitCount = 0
            };

            _relationCache.TryAdd(cacheKey, entry);
            _logger.LogDebug("关系缓存已添加: {Key}, 过期时间: {Expiration}, 当前缓存数: {Count}",
                cacheKey, expiration, _relationCache.Count);
        }

        /// <summary>
        /// 生成缓存键
        /// </summary>
        private string GenerateCacheKey(string text, EntityExtractionConfig config)
        {
            // 使用文本内容和配置的哈希值作为键
            var content = $"{text}|{config.ModelId}|{string.Join(",", config.EntityTypes)}|{config.MinConfidence}";
            var hash = ComputeHash(content);
            return $"{_config.CacheKeyPrefix}entity_{hash}";
        }

        /// <summary>
        /// 生成关系缓存键
        /// </summary>
        private string GenerateRelationCacheKey(
            string text,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities)
        {
            // 使用文本和实体列表的哈希值作为键
            var entityTexts = string.Join("|", entities.Select(e => e.Text).OrderBy(t => t));
            var content = $"{text}|{entityTexts}";
            var hash = ComputeHash(content);
            return $"{_config.CacheKeyPrefix}relation_{hash}";
        }

        /// <summary>
        /// 计算哈希值
        /// </summary>
        private string ComputeHash(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash)[..16]; // 取前16个字符
        }

        /// <summary>
        /// 克隆实体列表
        /// </summary>
        private List<ZSN.AI.Entity.KnowledgeBase.Entity> CloneEntities(
            List<ZSN.AI.Entity.KnowledgeBase.Entity> source)
        {
            return source.Select(e => new ZSN.AI.Entity.KnowledgeBase.Entity
            {
                Id = e.Id,
                Text = e.Text,
                Type = e.Type,
                Confidence = e.Confidence,
                StartPosition = e.StartPosition,
                EndPosition = e.EndPosition,
                SourceChunkIds = new List<string>(e.SourceChunkIds),
                Attributes = new Dictionary<string, string>(e.Attributes)
            }).ToList();
        }

        /// <summary>
        /// 克隆关系列表
        /// </summary>
        private List<Relation> CloneRelations(List<Relation> source)
        {
            return source.Select(r => new Relation
            {
                Id = r.Id,
                HeadEntityId = r.HeadEntityId,
                TailEntityId = r.TailEntityId,
                RelationType = r.RelationType,
                Description = r.Description,
                Confidence = r.Confidence,
                SourceChunkIds = new List<string>(r.SourceChunkIds)
            }).ToList();
        }

        /// <summary>
        /// 清理过期的缓存条目
        /// </summary>
        private void CleanupExpiredEntries(object? state)
        {
            try
            {
                var now = DateTime.UtcNow;

                // 清理实体缓存
                var expiredEntityKeys = _entityCache
                    .Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredEntityKeys)
                {
                    _entityCache.TryRemove(key, out _);
                }

                // 清理关系缓存
                var expiredRelationKeys = _relationCache
                    .Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredRelationKeys)
                {
                    _relationCache.TryRemove(key, out _);
                }

                if (expiredEntityKeys.Count > 0 || expiredRelationKeys.Count > 0)
                {
                    _logger.LogInformation("清理过期缓存: 实体 {EntityCount} 条, 关系 {RelationCount} 条, 剩余: 实体 {RemainingEntity} 条, 关系 {RemainingRelation} 条",
                        expiredEntityKeys.Count, expiredRelationKeys.Count,
                        _entityCache.Count, _relationCache.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期缓存失败");
            }
        }

        /// <summary>
        /// 清理最旧的缓存条目
        /// </summary>
        private void CleanupOldestEntries<T>(
            ConcurrentDictionary<string, CacheEntry<T>> cache,
            int count)
        {
            var oldestEntries = cache
                .OrderBy(kvp => kvp.Value.CreatedAt)
                .Take(count)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestEntries)
            {
                cache.TryRemove(key, out _);
            }

            _logger.LogDebug("清理了 {Count} 个最旧的缓存条目", oldestEntries.Count);
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public Dictionary<string, object> GetStatistics()
        {
            var now = DateTime.UtcNow;
            var expiredEntityCount = _entityCache.Values.Count(e => e.IsExpired);
            var expiredRelationCount = _relationCache.Values.Count(rel => rel.IsExpired);
            var totalHits = _entityCache.Values.Sum(e => e.HitCount) + _relationCache.Values.Sum(rel => rel.HitCount);

            return new Dictionary<string, object>
            {
                { "enabled", _config.EnableCache },
                { "entity_cache_count", _entityCache.Count },
                { "entity_cache_expired", expiredEntityCount },
                { "relation_cache_count", _relationCache.Count },
                { "relation_cache_expired", expiredRelationCount },
                { "total_hits", totalHits },
                { "max_cache_size", _config.MaxCacheSize },
                { "expiration_minutes", _config.CacheExpirationMinutes }
            };
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            _entityCache.Clear();
            _relationCache.Clear();
            _logger.LogInformation("所有缓存已清空");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}
