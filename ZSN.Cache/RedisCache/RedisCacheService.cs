using StackExchange.Redis;
using System.Text.Json;

namespace ZSN.Cache.RedisCache
{
    /// <summary>
    /// Redis缓存服务类，提供简洁的缓存读写接口
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly RedisConnectionManager _connectionManager;
        private readonly int? _defaultDb;
        private readonly string? _connectionString;

        /// <summary>
        /// 初始化Redis缓存服务
        /// </summary>
        /// <param name="db">数据库索引，默认为null表示使用配置中的默认值</param>
        /// <param name="connectionString">连接字符串，默认为null表示使用配置中的默认值</param>
        public RedisCacheService(int? db = null, string? connectionString = null)
        {
            _connectionManager = RedisConnectionManager.Instance;
            _defaultDb = db;
            _connectionString = connectionString;
        }

        /// <summary>
        /// 获取数据库
        /// </summary>
        /// <param name="db">数据库索引，默认为null表示使用构造函数中指定的值</param>
        /// <returns>IDatabase实例</returns>
        private IDatabase GetDatabase(int? db = null)
        {
            return _connectionManager.GetDatabase(db ?? _defaultDb, _connectionString);
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
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        public bool StringSet(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).StringSet(key, value, expiry, when, flags));
        }

        /// <summary>
        /// 异步设置字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _connectionManager.ExecuteWithRetryAsync(async () => await GetDatabase(db).StringSetAsync(key, value, expiry, when, flags));
        }

        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字符串值，不存在时返回null</returns>
        public string? StringGet(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).StringGet(key, flags));
        }

        /// <summary>
        /// 异步获取字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字符串值，不存在时返回null</returns>
        public async Task<string?> StringGetAsync(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _connectionManager.ExecuteWithRetryAsync(async () => await GetDatabase(db).StringGetAsync(key, flags));
        }

        #endregion

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

        #region 哈希操作

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
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).HashSet(key, hashField, value, when, flags));
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
            return await _connectionManager.ExecuteWithRetryAsync(async () => await GetDatabase(db).HashSetAsync(key, hashField, value, when, flags));
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
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).HashGet(key, hashField, flags));
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
            return await _connectionManager.ExecuteWithRetryAsync(async () => await GetDatabase(db).HashGetAsync(key, hashField, flags));
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
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).HashDelete(key, hashField, flags));
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
            var hashEntries = _connectionManager.ExecuteWithRetry(() => GetDatabase(db).HashGetAll(key, flags));
            var result = new Dictionary<string, string>();
            
            foreach (var entry in hashEntries)
            {
                result.Add(entry.Name.ToString(), entry.Value.ToString());
            }
            
            return result;
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
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).KeyExists(key, flags));
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
            return await _connectionManager.ExecuteWithRetryAsync(async () => await GetDatabase(db).KeyExistsAsync(key, flags));
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
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).KeyDelete(key, flags));
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
            return await _connectionManager.ExecuteWithRetryAsync(async () => await GetDatabase(db).KeyDeleteAsync(key, flags));
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
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).KeyExpire(key, expiry, flags));
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
            return await _connectionManager.ExecuteWithRetryAsync(async () => await GetDatabase(db).KeyExpireAsync(key, expiry, flags));
        }

        #endregion

        #region 列表操作

        /// <summary>
        /// 向列表左侧添加值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>添加后列表长度</returns>
        public long ListLeftPush(string key, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).ListLeftPush(key, value, when, flags));
        }

        /// <summary>
        /// 向列表右侧添加值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>添加后列表长度</returns>
        public long ListRightPush(string key, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).ListRightPush(key, value, when, flags));
        }

        /// <summary>
        /// 从列表左侧弹出值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>弹出的值，列表为空时返回null</returns>
        public string? ListLeftPop(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).ListLeftPop(key, flags));
        }

        /// <summary>
        /// 从列表右侧弹出值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>弹出的值，列表为空时返回null</returns>
        public string? ListRightPop(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).ListRightPop(key, flags));
        }

        /// <summary>
        /// 获取列表长度
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>列表长度</returns>
        public long ListLength(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).ListLength(key, flags));
        }

        #endregion

        #region 发布订阅

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="message">消息</param>
        /// <param name="flags">命令标志</param>
        /// <returns>接收消息的订阅者数量</returns>
        public long Publish(string channel, string message, CommandFlags flags = CommandFlags.None)
        {
            var subscriber = _connectionManager.GetConnection(_connectionString).GetSubscriber();
            return _connectionManager.ExecuteWithRetry(() => subscriber.Publish(channel, message, flags));
        }

        /// <summary>
        /// 异步发布消息
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="message">消息</param>
        /// <param name="flags">命令标志</param>
        /// <returns>接收消息的订阅者数量</returns>
        public async Task<long> PublishAsync(string channel, string message, CommandFlags flags = CommandFlags.None)
        {
            var subscriber = _connectionManager.GetConnection(_connectionString).GetSubscriber();
            return await _connectionManager.ExecuteWithRetryAsync(async () => await subscriber.PublishAsync(channel, message, flags));
        }

        /// <summary>
        /// 订阅频道
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="handler">消息处理器</param>
        /// <param name="flags">命令标志</param>
        public void Subscribe(string channel, Action<string, string> handler, CommandFlags flags = CommandFlags.None)
        {
            var subscriber = _connectionManager.GetConnection(_connectionString).GetSubscriber();
            subscriber.Subscribe(channel, (redisChannel, value) => handler(redisChannel.ToString(), value), flags);
        }

        /// <summary>
        /// 异步订阅频道
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="handler">消息处理器</param>
        /// <param name="flags">命令标志</param>
        /// <returns>异步任务</returns>
        public async Task SubscribeAsync(string channel, Action<string, string> handler, CommandFlags flags = CommandFlags.None)
        {
            var subscriber = _connectionManager.GetConnection(_connectionString).GetSubscriber();
            await subscriber.SubscribeAsync(channel, (redisChannel, value) => handler(redisChannel.ToString(), value), flags);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="flags">命令标志</param>
        public void Unsubscribe(string channel, CommandFlags flags = CommandFlags.None)
        {
            var subscriber = _connectionManager.GetConnection(_connectionString).GetSubscriber();
            subscriber.Unsubscribe(channel, null, flags);
        }

        /// <summary>
        /// 异步取消订阅
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="flags">命令标志</param>
        /// <returns>异步任务</returns>
        public async Task UnsubscribeAsync(string channel, CommandFlags flags = CommandFlags.None)
        {
            var subscriber = _connectionManager.GetConnection(_connectionString).GetSubscriber();
            await subscriber.UnsubscribeAsync(channel, null, flags);
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
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).StringIncrement(key, value, flags));
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
            return _connectionManager.ExecuteWithRetry(() => GetDatabase(db).StringDecrement(key, value, flags));
        }

        #endregion
    }
}
