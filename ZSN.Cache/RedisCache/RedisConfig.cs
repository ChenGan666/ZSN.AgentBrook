using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;

namespace ZSN.Cache.RedisCache
{
    /// <summary>
    /// Redis配置类，用于从appsettings.json读取Redis相关配置
    /// </summary>
    public class RedisConfig
    {
        /// <summary>
        /// Redis连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = "localhost:6379";
        
        /// <summary>
        /// 默认数据库索引
        /// </summary>
        public int DefaultDatabase { get; set; } = 0;
        
        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 3;
        
        /// <summary>
        /// 重试延迟(毫秒)
        /// </summary>
        public int RetryDelay { get; set; } = 200;
        
        /// <summary>
        /// 连接超时时间(毫秒)
        /// </summary>
        public int ConnectTimeout { get; set; } = 5000;
        
        /// <summary>
        /// 同步操作超时时间(毫秒)
        /// </summary>
        public int SyncTimeout { get; set; } = 5000;

        /// <summary>
        /// 从appsettings.json加载Redis配置
        /// </summary>
        /// <returns>Redis配置对象</returns>
        public static RedisConfig LoadFromAppSettings()
        {
            try
            {
                // 获取项目根目录
                string? basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
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
                
                // 从配置中读取Redis配置部分
                var redisConfig = new RedisConfig();
                configuration.GetSection("RedisConfig").Bind(redisConfig);
                
                return redisConfig;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载Redis配置失败: {ex.Message}");
                return new RedisConfig(); // 返回默认配置
            }
        }
    }
}
