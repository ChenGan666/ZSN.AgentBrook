using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZSN.AI.Entity;


// MCP客户端统一入口类 - 结合工厂使用不同实现
/*
// 创建配置
var config = new MCPConfig
{
    ConnectionType = MCPConnectionType.Sse,  // 或者 Stdio
    SseUrl = "https://example.com/sse",      // 如果使用SSE连接
    ClientName = "MyClient",
    EnableLogging = true
};

// 创建统一客户端
var client = new MCPClient(config, logger);

// 调用方法 - 内部会根据配置自动选择正确的实现
var isHealthy = await client.CheckHealthAsync();
var tools = await client.GetToolsAsync();
var result = await client.InvokeToolAsync<MyResultType>("toolName", parameters);
*/

namespace ZSN.AI.MCPClient
{
    /// <summary>
    /// MCP客户端统一入口类 - 结合工厂使用不同实现
    /// </summary>
    public class MCPClient : IMCPClient
    {
        private readonly IMCPClient _client;
        private readonly MCPConfig _config;
        private readonly ILogger? _logger;

        /// <summary>
        /// 初始化MCP客户端
        /// </summary>
        /// <param name="config">MCP客户端配置</param>
        /// <param name="logger">日志记录器（可选）</param>
        public MCPClient(MCPConfig config, ILogger? logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
            
            // 创建内部客户端实例需要异步方法，所以在构造函数中无法直接初始化
            // 将在首次调用方法时延迟初始化
            _client = null!;
            
            LogInfo($"MCP客户端已初始化，连接类型: {_config.Type}");
        }
        
        /// <summary>
        /// 初始化或获取客户端实例
        /// </summary>
        /// <returns>内部客户端实例</returns>
        private async Task<IMCPClient> GetClientAsync()
        {
            if (_client == null)
            {
                LogInfo($"正在创建内部客户端实例，连接类型: {_config.Type}");
                return await MCPClientFactory.CreateAsync(_config, _logger);
            }
            return _client;
        }

        /// <summary>
        /// 检查MCP服务健康状态
        /// </summary>
        /// <returns>服务是否健康</returns>
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                LogInfo("正在检查服务健康状态...");
                var client = await GetClientAsync();
                return await client.CheckHealthAsync();
            }
            catch (Exception ex)
            {
                LogError($"健康检查异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取MCP服务可用工具列表
        /// </summary>
        /// <returns>工具名称列表</returns>
        public async Task<IList<McpClientTool>> GetToolsAsync()
        {
            try
            {
                LogInfo("正在获取可用工具列表...");
                var client = await GetClientAsync();
                return await client.GetToolsAsync();
            }
            catch (Exception ex)
            {
                LogError($"获取工具列表异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 调用MCP服务工具
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <param name="parameters">工具参数（JSON格式）</param>
        /// <returns>调用结果</returns>
        public async Task<string> InvokeToolAsync(string toolName, string parameters)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentException("工具名称不能为空", nameof(toolName));
            }

            try
            {
                LogInfo($"正在调用工具: {toolName}");
                var client = await GetClientAsync();
                return await client.InvokeToolAsync(toolName, parameters);
            }
            catch (Exception ex)
            {
                LogError($"工具调用异常: {toolName}, 错误: {ex.Message}");
                return $"{{\"error\": \"Exception\", \"message\": \"{ex.Message}\"}}";
            }
        }

        /// <summary>
        /// 调用MCP服务工具并返回强类型结果
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="toolName">工具名称</param>
        /// <param name="parameters">工具参数对象</param>
        /// <returns>调用结果</returns>
        public async Task<TResult?> InvokeToolAsync<TResult>(string toolName, object parameters)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentException("工具名称不能为空", nameof(toolName));
            }

            try
            {
                LogInfo($"正在调用工具: {toolName}，参数类型: {parameters.GetType().Name}");
                var client = await GetClientAsync();
                return await client.InvokeToolAsync<TResult>(toolName, parameters);
            }
            catch (Exception ex)
            {
                LogError($"工具调用异常: {toolName}, 错误: {ex.Message}");
                return default;
            }
        }

        #region 日志方法

        private void LogInfo(string message)
        {
            if (_config.EnableLogging)
            {
                _logger?.LogInformation(message);
                Console.WriteLine($"[INFO] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (_config.EnableLogging)
            {
                _logger?.LogWarning(message);
                Console.WriteLine($"[WARNING] {message}");
            }
        }

        private void LogError(string message)
        {
            if (_config.EnableLogging)
            {
                _logger?.LogError(message);
                Console.WriteLine($"[ERROR] {message}");
            }
        }

        #endregion
    }
}
