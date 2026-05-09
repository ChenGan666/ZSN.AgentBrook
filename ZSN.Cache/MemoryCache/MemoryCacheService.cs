using System.Collections.Concurrent;
using System.Text.Json;
using StackExchange.Redis;

namespace ZSN.Cache.MemoryCache
{
    /// <summary>
    /// 内存缓存条目类，用于存储缓存项和相关元数据
    /// </summary>
    internal class MemoryCacheItem
    {
        /// <summary>
        /// 缓存键
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// 缓存值
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime? ExpireTime { get; set; }
        
        /// <summary>
        /// 上次访问时间
        /// </summary>
        public DateTime LastAccessTime { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        
        /// <summary>
        /// 大小估算（字节）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expiry">过期时间间隔</param>
        public MemoryCacheItem(string key, object value, TimeSpan? expiry)
        {
            Key = key;
            Value = value;
            CreateTime = DateTime.Now;
            LastAccessTime = DateTime.Now;
            
            if (expiry.HasValue)
            {
                ExpireTime = DateTime.Now.Add(expiry.Value);
            }
            
            // 估算对象大小（粗略）
            Size = EstimateSize(value);
        }

        /// <summary>
        /// 检查缓存项是否已过期
        /// </summary>
        /// <returns>是否已过期</returns>
        public bool IsExpired()
        {
            return ExpireTime.HasValue && DateTime.Now > ExpireTime.Value;
        }

        /// <summary>
        /// 更新最后访问时间
        /// </summary>
        public void UpdateLastAccessTime()
        {
            LastAccessTime = DateTime.Now;
        }

        /// <summary>
        /// 估算对象大小（粗略）
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>估算大小（字节）</returns>
        private long EstimateSize(object obj)
        {
            if (obj == null)
                return 0;

            if (obj is string str)
                return str.Length * 2; // 每个字符约2字节

            try
            {
                // 将对象序列化为JSON，以估算大小
                string json = JsonSerializer.Serialize(obj);
                return json.Length * 2; // 序列化结果大致是实际内存使用的一半，所以乘2
            }
            catch
            {
                // 无法序列化，返回一个基本大小
                return 1024; // 默认1KB
            }
        }
    }

    /// <summary>
    /// 基于内存的高速缓存服务，接口与RedisCacheService保持一致
    /// </summary>
    public partial class MemoryCacheService: ICacheService
    {
        // 缓存存储
        private readonly ConcurrentDictionary<string, MemoryCacheItem> _cache;
        
        // 配置
        private readonly MemoryCacheConfig _config;
        
        // 定时清理任务
        private readonly Timer _cleanupTimer;
        
        // 当前内存使用量（字节）
        private long _currentMemoryUsage;
        
        // 锁对象，用于同步内存统计更新
        private readonly object _memoryUsageLock = new object();

        /// <summary>
        /// 初始化内存缓存服务
        /// </summary>
        public MemoryCacheService()
        {
            _config = MemoryCacheConfig.LoadFromAppSettings();
            _cache = new ConcurrentDictionary<string, MemoryCacheItem>();
            _currentMemoryUsage = 0;
            
            // 启动定时清理任务
            _cleanupTimer = new Timer(CleanupExpiredItems, null, 
                TimeSpan.FromSeconds(_config.CleanupIntervalSeconds), 
                TimeSpan.FromSeconds(_config.CleanupIntervalSeconds));
            
            // 如果启用了内存压力监控，注册GC回调
            if (_config.EnableMemoryPressureMonitoring)
            {
                GC.RegisterForFullGCNotification(10, 10);
                StartGCMonitoring();
            }
        }

        /// <summary>
        /// 启动GC监控线程
        /// </summary>
        private void StartGCMonitoring()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (GC.WaitForFullGCApproach(1000) == GCNotificationStatus.Succeeded)
                    {
                        // GC即将发生，说明内存压力大，清理部分缓存
                        Log("检测到内存压力，开始紧急清理");
                        RemoveLeastRecentlyUsedItems(0.2); // 清理20%的缓存
                    }
                    
                    Thread.Sleep(1000);
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 清理过期项
        /// </summary>
        /// <param name="state">定时器状态（未使用）</param>
        private void CleanupExpiredItems(object? state)
        {
            Log("开始定期清理");
            
            // 删除过期项
            foreach (var key in _cache.Keys)
            {
                if (_cache.TryGetValue(key, out var item) && item.IsExpired())
                {
                    RemoveItem(key);
                    Log($"删除过期项：{key}");
                }
            }
            
            // 检查内存使用量
            CheckMemoryUsage();
            
            Log($"清理完成，当前缓存项：{_cache.Count}，内存使用：{_currentMemoryUsage / (1024.0 * 1024):F2}MB");
        }

        /// <summary>
        /// 检查内存使用量，如果超过阈值，则清理部分缓存
        /// </summary>
        private void CheckMemoryUsage()
        {
            long maxMemoryBytes = (long)_config.MaxMemoryMB * 1024 * 1024;
            long threshold = maxMemoryBytes * _config.MemoryOverflowPercentage / 100;
            
            if (_currentMemoryUsage > threshold)
            {
                double percentToRemove = 1.0 - ((double)maxMemoryBytes * 0.8 / _currentMemoryUsage);
                Log($"内存使用超过阈值，开始清理，当前：{_currentMemoryUsage / (1024.0 * 1024):F2}MB，" +
                    $"阈值：{threshold / (1024.0 * 1024):F2}MB，计划清理：{percentToRemove:P2}");
                
                RemoveLeastRecentlyUsedItems(percentToRemove);
            }
        }

        /// <summary>
        /// 删除最近最少使用的缓存项
        /// </summary>
        /// <param name="percentToRemove">要删除的百分比</param>
        private void RemoveLeastRecentlyUsedItems(double percentToRemove)
        {
            if (_cache.Count == 0)
                return;

            // 计算要删除的项数
            int itemsToRemove = Math.Max(1, (int)(_cache.Count * percentToRemove));
            
            // 按最后访问时间排序
            var itemsToEvict = _cache.Values
                .OrderBy(item => item.LastAccessTime)
                .Take(itemsToRemove)
                .ToList();
            
            foreach (var item in itemsToEvict)
            {
                RemoveItem(item.Key);
                Log($"由于内存限制，删除缓存项：{item.Key}，最后访问：{item.LastAccessTime}");
            }
        }

        /// <summary>
        /// 从缓存中删除项并更新内存使用统计
        /// </summary>
        /// <param name="key">要删除的键</param>
        /// <returns>是否成功删除</returns>
        private bool RemoveItem(string key)
        {
            if (_cache.TryRemove(key, out var removedItem))
            {
                lock (_memoryUsageLock)
                {
                    _currentMemoryUsage -= removedItem.Size;
                    if (_currentMemoryUsage < 0)
                        _currentMemoryUsage = 0;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">日志消息</param>
        private void Log(string message)
        {
            if (_config.EnableVerboseLogging)
            {
            }
        }

        #region 字符串操作

        /// <summary>
        /// 设置字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引（内存缓存忽略此参数）</param>
        /// <returns>是否成功</returns>
        public bool StringSet(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            // 如果未指定过期时间，使用默认过期时间
            expiry ??= TimeSpan.FromMinutes(_config.DefaultExpirationMinutes);
            
            var cacheItem = new MemoryCacheItem(key, value, expiry);
            
            // 检查操作条件
            if (when == When.Exists && !_cache.ContainsKey(key))
                return false;
            
            if (when == When.NotExists && _cache.ContainsKey(key))
                return false;
            
            bool result = _cache.AddOrUpdate(
                key,
                cacheItem, // 添加新项
                (k, existing) => // 更新现有项
                {
                    // 更新内存使用统计
                    lock (_memoryUsageLock)
                    {
                        _currentMemoryUsage -= existing.Size;
                    }
                    return cacheItem;
                }) != null;
            
            // 更新内存使用统计
            lock (_memoryUsageLock)
            {
                _currentMemoryUsage += cacheItem.Size;
            }
            
            // 如果内存使用量超过阈值，立即检查
            CheckMemoryUsage();
            
            return result;
        }

        /// <summary>
        /// 异步设置字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引（内存缓存忽略此参数）</param>
        /// <returns>是否成功</returns>
        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            // 在内存缓存中，同步和异步操作本质上是一样的，只是为了保持接口一致
            return await Task.FromResult(StringSet(key, value, expiry, when, flags, db));
        }

        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引（内存缓存忽略此参数）</param>
        /// <returns>字符串值，不存在时返回null</returns>
        public string? StringGet(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                // 检查是否过期
                if (item.IsExpired())
                {
                    RemoveItem(key);
                    return null;
                }
                
                // 更新最后访问时间
                item.UpdateLastAccessTime();
                
                return item.Value as string;
            }
            
            return null;
        }

        /// <summary>
        /// 异步获取字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引（内存缓存忽略此参数）</param>
        /// <returns>字符串值，不存在时返回null</returns>
        public async Task<string?> StringGetAsync(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            // 在内存缓存中，同步和异步操作本质上是一样的，只是为了保持接口一致
            return await Task.FromResult(StringGet(key, flags, db));
        }

        #endregion
    }
}
