using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// MCP客户端完整配置类
    /// </summary>
    public class McpClientConfiguration
    {

        /*
         // 使用构建器模式创建配置
        var config = new McpConfigurationBuilder()
            .AddFileSystemServer("filesystem", "/path/to/directory")
            .AddPostgreSqlServer("postgres", "postgresql://user:pass@localhost:5432/db")
            .AddCustomServer("custom", "python", new List<string> { "server.py" })
            .WithAppearance("dark", 14)
            .Build();

        // 验证配置
        var validation = McpConfigurationValidator.ValidateConfiguration(config);
        if (!validation.IsValid)
        {
        }
         */

        /// <summary>
        /// MCP服务器配置集合
        /// </summary>
        [JsonPropertyName("mcpServers")]
        public Dictionary<string, McpServerConfiguration> McpServers { get; set; } = new();

        /// <summary>
        /// 全局快捷键
        /// </summary>
        [JsonPropertyName("globalShortcut")]
        public string? GlobalShortcut { get; set; }

        /// <summary>
        /// 更新器配置
        /// </summary>
        [JsonPropertyName("updater")]
        public UpdaterConfiguration? Updater { get; set; }

        /// <summary>
        /// 外观配置
        /// </summary>
        [JsonPropertyName("appearance")]
        public AppearanceConfiguration? Appearance { get; set; }

        /// <summary>
        /// 实验性功能配置
        /// </summary>
        [JsonPropertyName("experimental")]
        public ExperimentalConfiguration? Experimental { get; set; }
    }

    /// <summary>
    /// MCP服务器配置类
    /// </summary>
    public class McpServerConfiguration
    {
        /// <summary>
        /// 启动命令
        /// </summary>
        [Required]
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// 命令参数列表
        /// </summary>
        [JsonPropertyName("args")]
        public List<string> Args { get; set; } = new();

        /// <summary>
        /// 环境变量
        /// </summary>
        [JsonPropertyName("env")]
        public Dictionary<string, string> Environment { get; set; } = new();

        /// <summary>
        /// 服务器描述
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 是否启用此服务器
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        [JsonPropertyName("timeoutMs")]
        public int? TimeoutMs { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        [JsonPropertyName("retryCount")]
        public int? RetryCount { get; set; }

        /// <summary>
        /// 工作目录
        /// </summary>
        [JsonPropertyName("workingDirectory")]
        public string? WorkingDirectory { get; set; }
    }

    /// <summary>
    /// 更新器配置
    /// </summary>
    public class UpdaterConfiguration
    {
        /// <summary>
        /// 是否检查更新
        /// </summary>
        [JsonPropertyName("checkForUpdates")]
        public bool CheckForUpdates { get; set; } = true;

        /// <summary>
        /// 是否自动下载更新
        /// </summary>
        [JsonPropertyName("autoDownload")]
        public bool AutoDownload { get; set; } = false;

        /// <summary>
        /// 更新检查间隔（小时）
        /// </summary>
        [JsonPropertyName("checkIntervalHours")]
        public int? CheckIntervalHours { get; set; }
    }

    /// <summary>
    /// 外观配置
    /// </summary>
    public class AppearanceConfiguration
    {
        /// <summary>
        /// 主题设置
        /// </summary>
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "system";

        /// <summary>
        /// 字体大小
        /// </summary>
        [JsonPropertyName("fontSize")]
        public int? FontSize { get; set; }

        /// <summary>
        /// 是否启用暗色模式
        /// </summary>
        [JsonPropertyName("darkMode")]
        public bool? DarkMode { get; set; }
    }

    /// <summary>
    /// 实验性功能配置
    /// </summary>
    public class ExperimentalConfiguration
    {
        /// <summary>
        /// 是否启用新功能
        /// </summary>
        [JsonPropertyName("enableNewFeatures")]
        public bool EnableNewFeatures { get; set; } = false;

        /// <summary>
        /// 调试模式
        /// </summary>
        [JsonPropertyName("debugMode")]
        public bool? DebugMode { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        [JsonPropertyName("logLevel")]
        public string? LogLevel { get; set; }
    }

    /// <summary>
    /// MCP服务器类型枚举
    /// </summary>
    public enum McpServerType
    {
        FileSystem,
        Database,
        Api,
        Custom,
        Docker,
        Memory,
        Fetch
    }

    /// <summary>
    /// MCP配置验证器
    /// </summary>
    public static class McpConfigurationValidator
    {
        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public static ValidationResult ValidateConfiguration(McpClientConfiguration config)
        {
            var result = new ValidationResult();

            if (config.McpServers == null || config.McpServers.Count == 0)
            {
                result.AddError("至少需要配置一个MCP服务器");
                return result;
            }

            foreach (var server in config.McpServers)
            {
                if (string.IsNullOrWhiteSpace(server.Value.Command))
                {
                    result.AddError($"服务器 '{server.Key}' 缺少命令配置");
                }

                if (server.Value.TimeoutMs.HasValue && server.Value.TimeoutMs <= 0)
                {
                    result.AddError($"服务器 '{server.Key}' 超时时间必须大于0");
                }

                if (server.Value.RetryCount.HasValue && server.Value.RetryCount < 0)
                {
                    result.AddError($"服务器 '{server.Key}' 重试次数不能为负数");
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 验证结果类
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new();
        public bool IsValid => Errors.Count == 0;

        public void AddError(string error)
        {
            Errors.Add(error);
        }
    }

    /// <summary>
    /// MCP配置构建器
    /// </summary>
    public class McpConfigurationBuilder
    {
        private readonly McpClientConfiguration _config = new();

        /// <summary>
        /// 添加文件系统服务器
        /// </summary>
        public McpConfigurationBuilder AddFileSystemServer(string name, string allowedPath)
        {
            _config.McpServers[name] = new McpServerConfiguration
            {
                Command = "npx",
                Args = new List<string> { "--yes", "@modelcontextprotocol/server-filesystem", allowedPath },
                Description = "文件系统访问服务器"
            };
            return this;
        }

        /// <summary>
        /// 添加PostgreSQL服务器
        /// </summary>
        public McpConfigurationBuilder AddPostgreSqlServer(string name, string connectionString)
        {
            _config.McpServers[name] = new McpServerConfiguration
            {
                Command = "npx",
                Args = new List<string> { "-y", "@modelcontextprotocol/server-postgres", connectionString },
                Environment = new Dictionary<string, string> { { "DATABASE_URL", connectionString } },
                Description = "PostgreSQL数据库服务器"
            };
            return this;
        }

        /// <summary>
        /// 添加自定义服务器
        /// </summary>
        public McpConfigurationBuilder AddCustomServer(string name, string command, List<string> args, Dictionary<string, string>? env = null)
        {
            _config.McpServers[name] = new McpServerConfiguration
            {
                Command = command,
                Args = args ?? new List<string>(),
                Environment = env ?? new Dictionary<string, string>(),
                Description = "自定义服务器"
            };
            return this;
        }

        /// <summary>
        /// 设置外观配置
        /// </summary>
        public McpConfigurationBuilder WithAppearance(string theme = "system", int? fontSize = null)
        {
            _config.Appearance = new AppearanceConfiguration
            {
                Theme = theme,
                FontSize = fontSize
            };
            return this;
        }

        /// <summary>
        /// 构建配置
        /// </summary>
        public McpClientConfiguration Build()
        {
            var validation = McpConfigurationValidator.ValidateConfiguration(_config);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"配置验证失败: {string.Join(", ", validation.Errors)}");
            }
            return _config;
        }
    }
}
