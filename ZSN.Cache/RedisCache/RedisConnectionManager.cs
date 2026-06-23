using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace ZSN.Cache.RedisCache
{
    /// <summary>
    /// Redis连接管理器，负责管理连接池和重试机制
    /// </summary>
    public class RedisConnectionManager
    {
        // 单例实例
        private static readonly Lazy<RedisConnectionManager> _instance = new Lazy<RedisConnectionManager>(() => new RedisConnectionManager());
        
        // Redis配置
        private readonly RedisConfig _config;
        
        // 连接池，键为连接字符串
        private readonly ConcurrentDictionary<string, Lazy<ConnectionMultiplexer>> _connections;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static RedisConnectionManager Instance => _instance.Value;

        private RedisConnectionManager()
        {
            _config = RedisConfig.LoadFromAppSettings();
            _connections = new ConcurrentDictionary<string, Lazy<ConnectionMultiplexer>>();
        }

        /// <summary>
        /// 获取Redis连接
        /// </summary>
        /// <param name="connectionString">连接字符串，如果为null则使用配置文件中的连接字符串</param>
        /// <returns>ConnectionMultiplexer实例</returns>
        public ConnectionMultiplexer GetConnection(string? connectionString = null)
        {
            connectionString ??= _config.ConnectionString;
            
            return _connections.GetOrAdd(connectionString, CreateConnectionMultiplexer).Value;
        }

        /// <summary>
        /// 获取Redis数据库
        /// </summary>
        /// <param name="db">数据库索引，如果为null则使用配置文件中的默认数据库</param>
        /// <param name="connectionString">连接字符串，如果为null则使用配置文件中的连接字符串</param>
        /// <returns>IDatabase实例</returns>
        public IDatabase GetDatabase(int? db = null, string? connectionString = null)
        {
            return GetConnection(connectionString).GetDatabase(db ?? _config.DefaultDatabase);
        }

        /// <summary>
        /// 使用指定重试次数执行Redis操作
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="action">要执行的操作</param>
        /// <param name="retryCount">重试次数，如果为null则使用配置文件中的重试次数</param>
        /// <returns>操作结果</returns>
        public T ExecuteWithRetry<T>(Func<T> action, int? retryCount = null)
        {
            int maxRetryCount = retryCount ?? _config.RetryCount;
            int retryDelay = _config.RetryDelay;
            int attempts = 0;
            
            while (true)
            {
                try
                {
                    attempts++;
                    return action();
                }
                catch (RedisConnectionException ex) when (IsRetryableException(ex) && attempts <= maxRetryCount)
                {
                    System.Diagnostics.Debug.WriteLine($"Redis连接异常，准备第{attempts}次重试: {ex.Message}");
                    Task.Delay(retryDelay * attempts).Wait(); // 指数退避策略
                }
                catch (SocketException ex) when (IsRetryableException(ex) && attempts <= maxRetryCount)
                {
                    System.Diagnostics.Debug.WriteLine($"Socket异常，准备第{attempts}次重试: {ex.Message}");
                    Task.Delay(retryDelay * attempts).Wait(); // 指数退避策略
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Redis操作异常: {ex.Message}");
                    throw; // 对于不可重试的异常，直接抛出
                }
            }
        }

        /// <summary>
        /// 异步方式使用指定重试次数执行Redis操作
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="retryCount">重试次数，如果为null则使用配置文件中的重试次数</param>
        /// <returns>操作结果</returns>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int? retryCount = null)
        {
            int maxRetryCount = retryCount ?? _config.RetryCount;
            int retryDelay = _config.RetryDelay;
            int attempts = 0;
            
            while (true)
            {
                try
                {
                    attempts++;
                    return await action();
                }
                catch (RedisConnectionException ex) when (IsRetryableException(ex) && attempts <= maxRetryCount)
                {
                    System.Diagnostics.Debug.WriteLine($"Redis连接异常，准备第{attempts}次重试: {ex.Message}");
                    await Task.Delay(retryDelay * attempts); // 指数退避策略
                }
                catch (SocketException ex) when (IsRetryableException(ex) && attempts <= maxRetryCount)
                {
                    System.Diagnostics.Debug.WriteLine($"Socket异常，准备第{attempts}次重试: {ex.Message}");
                    await Task.Delay(retryDelay * attempts); // 指数退避策略
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Redis操作异常: {ex.Message}");
                    throw; // 对于不可重试的异常，直接抛出
                }
            }
        }

        // 创建连接多路复用器
        private Lazy<ConnectionMultiplexer> CreateConnectionMultiplexer(string connectionString)
        {
            return new Lazy<ConnectionMultiplexer>(() => 
            {
                var options = ConfigurationOptions.Parse(connectionString);
                options.ConnectTimeout = _config.ConnectTimeout;
                options.SyncTimeout = _config.SyncTimeout;
                
                try
                {
                    return ConnectionMultiplexer.Connect(options);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"创建Redis连接失败: {ex.Message}");
                    throw;
                }
            });
        }

        // 判断异常是否可重试
        private bool IsRetryableException(Exception ex)
        {
            // 这里可以根据具体需求判断哪些异常是可以重试的
            return ex is RedisConnectionException || ex is SocketException;
        }

        /// <summary>
        /// 关闭所有连接
        /// </summary>
        public void CloseAll()
        {
            foreach (var connection in _connections.Values)
            {
                if (connection.IsValueCreated)
                {
                    connection.Value.Close();
                    connection.Value.Dispose();
                }
            }
            _connections.Clear();
        }
    }
}
