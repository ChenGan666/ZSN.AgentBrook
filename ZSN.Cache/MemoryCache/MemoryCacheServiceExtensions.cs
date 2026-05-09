using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ZSN.Cache.MemoryCache
{
    /// <summary>
    /// MemoryCacheService的扩展部分，实现对象和哈希表操作
    /// </summary>
    public partial class MemoryCacheService
    {
        #region 对象操作

        /// <summary>
        /// 设置对象（序列化为JSON）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">对象</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        public bool Set<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (value == null) return false;
            string json = JsonSerializer.Serialize(value);
            return StringSet(key, json, expiry, when, flags, db);
        }

        /// <summary>
        /// 异步设置对象（序列化为JSON）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">对象</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (value == null) return false;
            string json = JsonSerializer.Serialize(value);
            return await StringSetAsync(key, json, expiry, when, flags, db);
        }

        /// <summary>
        /// 获取对象（从JSON反序列化）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>对象，不存在时返回默认值</returns>
        public T? Get<T>(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            string? json = StringGet(key, flags, db);
            if (string.IsNullOrEmpty(json))
                return default;
            
            return JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// 异步获取对象（从JSON反序列化）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>对象，不存在时返回默认值</returns>
        public async Task<T?> GetAsync<T>(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            string? json = await StringGetAsync(key, flags, db);
            if (string.IsNullOrEmpty(json))
                return default;
            
            return JsonSerializer.Deserialize<T>(json);
        }

        #endregion

        #region 键操作

        /// <summary>
        /// 判断键是否存在
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否存在</returns>
        public bool KeyExists(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _cache.ContainsKey(key) && !_cache[key].IsExpired();
        }

        /// <summary>
        /// 异步判断键是否存在
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否存在</returns>
        public async Task<bool> KeyExistsAsync(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await Task.FromResult(KeyExists(key, flags, db));
        }

        /// <summary>
        /// 删除键
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否删除成功</returns>
        public bool KeyDelete(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return RemoveItem(key);
        }

        /// <summary>
        /// 异步删除键
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> KeyDeleteAsync(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await Task.FromResult(KeyDelete(key, flags, db));
        }

        /// <summary>
        /// 设置键过期时间
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否设置成功</returns>
        public bool KeyExpire(string key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (expiry == null)
                {
                    item.ExpireTime = null;
                }
                else
                {
                    item.ExpireTime = DateTime.Now.Add(expiry.Value);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 异步设置键过期时间
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否设置成功</returns>
        public async Task<bool> KeyExpireAsync(string key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await Task.FromResult(KeyExpire(key, expiry, flags, db));
        }

        #endregion

        #region 计数器

        /// <summary>
        /// 递增
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">递增值，默认为1</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>递增后的值</returns>
        public long Increment(string key, long value = 1, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (_cache.TryGetValue(key, out var item) && !item.IsExpired())
            {
                if (item.Value is string strValue && long.TryParse(strValue, out var numValue))
                {
                    var newValue = numValue + value;
                    StringSet(key, newValue.ToString(), item.ExpireTime - DateTime.Now);
                    return newValue;
                }
            }

            // 如果键不存在或不是数字，则从value开始计数
            StringSet(key, value.ToString());
            return value;
        }

        /// <summary>
        /// 递减
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">递减值，默认为1</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>递减后的值</returns>
        public long Decrement(string key, long value = 1, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return Increment(key, -value, flags, db);
        }

        #endregion
    }
}
