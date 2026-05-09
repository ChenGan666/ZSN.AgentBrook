using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;

namespace ZSN.Cache.MemoryCache
{
    /// <summary>
    /// 内存缓存配置类
    /// </summary>
    public class MemoryCacheConfig
    {
        /// <summary>
        /// 默认过期时间（分钟）
        /// </summary>
        public int DefaultExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// 最大内存使用量（MB）
        /// </summary>
        public int MaxMemoryMB { get; set; } = 100;

        /// <summary>
        /// 缓存清理检查间隔（秒）
        /// </summary>
        public int CleanupIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 缓存溢出百分比，达到此百分比时触发清理（如95表示当使用内存达到最大内存的95%时触发清理）
        /// </summary>
        public int MemoryOverflowPercentage { get; set; } = 95;

        /// <summary>
        /// 是否启用内存压力监控
        /// </summary>
        public bool EnableMemoryPressureMonitoring { get; set; } = true;

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;

        /// <summary>
        /// 从appsettings.json加载内存缓存配置
        /// </summary>
        /// <returns>内存缓存配置对象</returns>
        public static MemoryCacheConfig LoadFromAppSettings()
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
                
                // 从配置中读取内存缓存配置部分
                var cacheConfig = new MemoryCacheConfig();
                configuration.GetSection("MemoryCacheConfig").Bind(cacheConfig);
                
                return cacheConfig;
            }
            catch (Exception ex)
            {
                return new MemoryCacheConfig(); // 返回默认配置
            }
        }
    }
}
