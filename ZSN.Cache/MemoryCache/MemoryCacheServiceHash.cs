using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ZSN.Cache.MemoryCache
{
    /// <summary>
    /// MemoryCacheService的哈希表操作部分
    /// </summary>
    public partial class MemoryCacheService
    {
        #region 哈希操作

        /// <summary>
        /// 哈希表缓存项类
        /// </summary>
        private class HashEntry
        {
            public ConcurrentDictionary<string, string> Fields { get; }

            public HashEntry()
            {
                Fields = new ConcurrentDictionary<string, string>();
            }
        }

        /// <summary>
        /// 设置哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="value">值</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功（字段是新增时返回true，更新时返回false）</returns>
        public bool HashSet(string key, string hashField, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (string.IsNullOrEmpty(hashField))
                return false;

            // 确保缓存项存在并且是HashEntry类型
            if (!_cache.TryGetValue(key, out var item))
            {
                var hashItem = new HashEntry();
                var cacheItem = new MemoryCacheItem(key, hashItem, TimeSpan.FromMinutes(_config.DefaultExpirationMinutes));
                
                if (_cache.TryAdd(key, cacheItem))
                {
                    // 更新内存使用统计
                    lock (_memoryUsageLock)
                    {
                        _currentMemoryUsage += cacheItem.Size;
                    }
                    
                    item = cacheItem;
                }
                else
                {
                    // 如果并发添加失败，则再次获取
                    if (!_cache.TryGetValue(key, out item))
                    {
                        return false;
                    }
                }
            }
            
            // 检查缓存项是否过期
            if (item.IsExpired())
            {
                RemoveItem(key);
                return false;
            }
            
            // 更新最后访问时间
            item.UpdateLastAccessTime();
            
            // 获取哈希表对象
            HashEntry hashEntry;
            if (item.Value is HashEntry he)
            {
                hashEntry = he;
            }
            else
            {
                // 如果缓存项不是哈希表，则创建一个新的哈希表
                hashEntry = new HashEntry();
                item.Value = hashEntry;
            }

            // 检查操作条件
            if (when == When.Exists && !hashEntry.Fields.ContainsKey(hashField))
                return false;
            
            if (when == When.NotExists && hashEntry.Fields.ContainsKey(hashField))
                return false;
            
            // 设置字段值
            bool isNewField = !hashEntry.Fields.ContainsKey(hashField);
            hashEntry.Fields[hashField] = value;
            
            // 更新内存使用统计
            if (isNewField)
            {
                lock (_memoryUsageLock)
                {
                    _currentMemoryUsage += hashField.Length * 2 + value.Length * 2;
                }
            }
            else
            {
                // 假设更新字段不会改变内存使用量（简化处理）
            }
            
            return isNewField;
        }

        /// <summary>
        /// 异步设置哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="value">值</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功（字段是新增时返回true，更新时返回false）</returns>
        public async Task<bool> HashSetAsync(string key, string hashField, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await Task.FromResult(HashSet(key, hashField, value, when, flags, db));
        }

        /// <summary>
        /// 设置哈希对象字段（序列化为JSON）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="value">对象</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        public bool HashSet<T>(string key, string hashField, T value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (value == null) return false;
            string json = JsonSerializer.Serialize(value);
            return HashSet(key, hashField, json, when, flags, db);
        }

        /// <summary>
        /// 获取哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字段值，不存在时返回null</returns>
        public string? HashGet(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null)
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
                
                // 获取哈希表对象
                if (item.Value is HashEntry hashEntry)
                {
                    if (hashEntry.Fields.TryGetValue(hashField, out string? value))
                    {
                        return value;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// 异步获取哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字段值，不存在时返回null</returns>
        public async Task<string?> HashGetAsync(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await Task.FromResult(HashGet(key, hashField, flags, db));
        }

        /// <summary>
        /// 获取哈希对象字段（从JSON反序列化）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>对象，不存在时返回默认值</returns>
        public T? HashGet<T>(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            string? json = HashGet(key, hashField, flags, db);
            if (string.IsNullOrEmpty(json))
                return default;
            
            return JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// 删除哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功删除</returns>
        public bool HashDelete(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                // 检查是否过期
                if (item.IsExpired())
                {
                    RemoveItem(key);
                    return false;
                }
                
                // 更新最后访问时间
                item.UpdateLastAccessTime();
                
                // 删除字段
                if (item.Value is HashEntry hashEntry)
                {
                    return hashEntry.Fields.TryRemove(hashField, out _);
                }
            }
            
            return false;
        }

        /// <summary>
        /// 获取所有哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字段名和值的字典</returns>
        public Dictionary<string, string> HashGetAll(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                // 检查是否过期
                if (item.IsExpired())
                {
                    RemoveItem(key);
                    return new Dictionary<string, string>();
                }
                
                // 更新最后访问时间
                item.UpdateLastAccessTime();
                
                // 获取所有字段
                if (item.Value is HashEntry hashEntry)
                {
                    return new Dictionary<string, string>(hashEntry.Fields);
                }
            }
            
            return new Dictionary<string, string>();
        }

        #endregion
    }
}
