using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ZSN.Cache.MemoryCache;
using ZSN.Cache.RedisCache;
/* 缓存服务使用示例：

// 1. 依赖注入方式使用（推荐）
// 在 Startup.cs 或 Program.cs 中注册
services.AddSingleton<ICacheService>(provider => {
    var options = new CacheOptions { 
        Type = CacheType.Redis, // 使用Redis缓存
        FallbackToMemoryOnRedisFailure = true // Redis不可用时自动降级到内存缓存
    };
    return new CacheService(options);
});

// 在需要的地方注入使用
public class SomeService {
    private readonly ICacheService _cache;
    
    public SomeService(ICacheService cache) {
        _cache = cache;
    }
    
    public void SomeMethod() {
        // 字符串操作
        _cache.StringSet("key", "value", TimeSpan.FromMinutes(10));
        string value = _cache.StringGet("key");
        
        // 对象操作
        _cache.Set("user:1", new User { Id = 1, Name = "张三" }, TimeSpan.FromHours(1));
        User user = _cache.Get<User>("user:1");
        
        // 哈希操作
        _cache.HashSet("user:2", "name", "李四");
        _cache.HashSet("user:2", "age", "25");
        string name = _cache.HashGet("user:2", "name");
        var fields = _cache.HashGetAll("user:2"); // 返回Dictionary<string, string>
        
        // 列表操作
        _cache.ListRightPush("list:1", "item1");
        _cache.ListRightPush("list:1", "item2");
        var items = _cache.ListRange("list:1", 0, -1);
        
        // 计数器
        long counter = _cache.Increment("counter:1");
        counter = _cache.Decrement("counter:1");
    }
}

// 2. 直接实例化方式
var cache = new CacheService(new CacheOptions { Type = CacheType.Memory });
cache.StringSet("key", "value");
*/


namespace ZSN.Cache
{
    /// <summary>
    /// 缓存服务配置选项
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// 缓存类型
        /// </summary>
        public CacheType Type { get; set; } = CacheType.Memory;

        /// <summary>
        /// 当Redis不可用时是否降级到内存缓存（仅当Type=Redis时有效）
        /// </summary>
        public bool FallbackToMemoryOnRedisFailure { get; set; } = true;
    }

    /// <summary>
    /// 缓存类型枚举
    /// </summary>
    public enum CacheType
    {
        /// <summary>
        /// Redis缓存
        /// </summary>
        Redis,

        /// <summary>
        /// 内存缓存
        /// </summary>
        Memory
    }

    /// <summary>
    /// 通用缓存服务，根据配置自动选择使用Redis缓存或内存缓存
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly ICacheService _implementation;

        /// <summary>
        /// 当前使用的缓存类型
        /// </summary>
        public CacheType CurrentCacheType { get; private set; }

        /// <summary>
        /// 初始化缓存服务
        /// </summary>
        public CacheService()
        {
            // 加载配置
            var options = LoadCacheOptions();
            
            // 初始化缓存实现
            _implementation = InitializeCache(options);
        }

        /// <summary>
        /// 使用指定选项初始化缓存服务
        /// </summary>
        /// <param name="options">缓存选项</param>
        public CacheService(CacheOptions options)
        {
            _implementation = InitializeCache(options);
        }

        /// <summary>
        /// 从appsettings.json加载缓存配置
        /// </summary>
        /// <returns>缓存配置选项</returns>
        private CacheOptions LoadCacheOptions()
        {
            try
            {
                // 获取程序集基础路径
                string? basePath = Path.GetDirectoryName(typeof(CacheService).Assembly.Location);
                if (string.IsNullOrEmpty(basePath))
                {
                    // 如果无法获取入口程序集位置，则使用当前目录
                    basePath = Directory.GetCurrentDirectory();
                }

                // 加载配置文件
                var builder = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                var configuration = builder.Build();
                
                // 从配置中读取缓存配置
                var options = new CacheOptions();
                configuration.GetSection("CacheOptions").Bind(options);
                
                return options;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载缓存配置失败: {ex.Message}");
                return new CacheOptions(); // 返回默认配置（Memory类型）
            }
        }

        /// <summary>
        /// 初始化缓存实现
        /// </summary>
        /// <param name="options">缓存选项</param>
        /// <returns>缓存服务实现</returns>
        private ICacheService InitializeCache(CacheOptions options)
        {
            if (options.Type == CacheType.Redis)
            {
                try
                {
                    // 尝试初始化Redis缓存
                    CurrentCacheType = CacheType.Redis;
                    return new RedisCacheService();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Redis缓存初始化失败: {ex.Message}");
                    
                    // 如果配置为Redis缓存失败时降级到内存缓存
                    if (options.FallbackToMemoryOnRedisFailure)
                    {
                        System.Diagnostics.Debug.WriteLine("降级到内存缓存");
                        CurrentCacheType = CacheType.Memory;
                        return new MemoryCacheService();
                    }
                    
                    // 否则继续抛出异常
                    throw;
                }
            }
            else
            {
                // 使用内存缓存
                CurrentCacheType = CacheType.Memory;
                return new MemoryCacheService();
            }
        }

        #region ICacheService接口实现（转发到具体实现）

        #region 字符串操作

        /// <inheritdoc/>
        public bool StringSet(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.StringSet(key, value, expiry, when, flags, db);
        }

        /// <inheritdoc/>
        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _implementation.StringSetAsync(key, value, expiry, when, flags, db);
        }

        /// <inheritdoc/>
        public string? StringGet(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.StringGet(key, flags, db);
        }

        /// <inheritdoc/>
        public async Task<string?> StringGetAsync(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _implementation.StringGetAsync(key, flags, db);
        }

        #endregion

        #region 对象操作

        /// <inheritdoc/>
        public bool Set<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.Set<T>(key, value, expiry, when, flags, db);
        }

        /// <inheritdoc/>
        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _implementation.SetAsync<T>(key, value, expiry, when, flags, db);
        }

        /// <inheritdoc/>
        public T? Get<T>(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.Get<T>(key, flags, db);
        }

        /// <inheritdoc/>
        public async Task<T?> GetAsync<T>(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _implementation.GetAsync<T>(key, flags, db);
        }

        #endregion

        #region 键操作

        /// <inheritdoc/>
        public bool KeyExists(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.KeyExists(key, flags, db);
        }

        /// <inheritdoc/>
        public async Task<bool> KeyExistsAsync(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _implementation.KeyExistsAsync(key, flags, db);
        }

        /// <inheritdoc/>
        public bool KeyDelete(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.KeyDelete(key, flags, db);
        }

        /// <inheritdoc/>
        public async Task<bool> KeyDeleteAsync(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _implementation.KeyDeleteAsync(key, flags, db);
        }

        /// <inheritdoc/>
        public bool KeyExpire(string key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.KeyExpire(key, expiry, flags, db);
        }

        /// <inheritdoc/>
        public async Task<bool> KeyExpireAsync(string key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _implementation.KeyExpireAsync(key, expiry, flags, db);
        }

        #endregion

        #region 哈希操作

        /// <inheritdoc/>
        public bool HashSet(string key, string hashField, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.HashSet(key, hashField, value, when, flags, db);
        }

        /// <inheritdoc/>
        public async Task<bool> HashSetAsync(string key, string hashField, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _implementation.HashSetAsync(key, hashField, value, when, flags, db);
        }

        /// <inheritdoc/>
        public bool HashSet<T>(string key, string hashField, T value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.HashSet<T>(key, hashField, value, when, flags, db);
        }

        /// <inheritdoc/>
        public string? HashGet(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.HashGet(key, hashField, flags, db);
        }

        /// <inheritdoc/>
        public async Task<string?> HashGetAsync(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return await _implementation.HashGetAsync(key, hashField, flags, db);
        }

        /// <inheritdoc/>
        public T? HashGet<T>(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.HashGet<T>(key, hashField, flags, db);
        }

        /// <inheritdoc/>
        public bool HashDelete(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.HashDelete(key, hashField, flags, db);
        }

        /// <inheritdoc/>
        public Dictionary<string, string> HashGetAll(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.HashGetAll(key, flags, db);
        }

        #endregion

        #region 列表操作

        /// <inheritdoc/>
        public long ListLeftPush(string key, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.ListLeftPush(key, value, when, flags, db);
        }

        /// <inheritdoc/>
        public long ListRightPush(string key, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.ListRightPush(key, value, when, flags, db);
        }

        /// <inheritdoc/>
        public string? ListLeftPop(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.ListLeftPop(key, flags, db);
        }

        /// <inheritdoc/>
        public string? ListRightPop(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.ListRightPop(key, flags, db);
        }

        /// <inheritdoc/>
        public long ListLength(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.ListLength(key, flags, db);
        }

        #endregion

        #region 计数器

        /// <inheritdoc/>
        public long Increment(string key, long value = 1, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.Increment(key, value, flags, db);
        }

        /// <inheritdoc/>
        public long Decrement(string key, long value = 1, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            return _implementation.Decrement(key, value, flags, db);
        }

        #endregion

        #region 发布订阅

        /// <inheritdoc/>
        public long Publish(string channel, string message, CommandFlags flags = CommandFlags.None)
        {
            return _implementation.Publish(channel, message, flags);
        }

        /// <inheritdoc/>
        public async Task<long> PublishAsync(string channel, string message, CommandFlags flags = CommandFlags.None)
        {
            return await _implementation.PublishAsync(channel, message, flags);
        }

        /// <inheritdoc/>
        public void Subscribe(string channel, Action<string, string> handler, CommandFlags flags = CommandFlags.None)
        {
            _implementation.Subscribe(channel, handler, flags);
        }

        /// <inheritdoc/>
        public async Task SubscribeAsync(string channel, Action<string, string> handler, CommandFlags flags = CommandFlags.None)
        {
            await _implementation.SubscribeAsync(channel, handler, flags);
        }

        /// <inheritdoc/>
        public void Unsubscribe(string channel, CommandFlags flags = CommandFlags.None)
        {
            _implementation.Unsubscribe(channel, flags);
        }

        /// <inheritdoc/>
        public async Task UnsubscribeAsync(string channel, CommandFlags flags = CommandFlags.None)
        {
            await _implementation.UnsubscribeAsync(channel, flags);
        }

        #endregion

        #endregion
    }
}
