using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using Newtonsoft.Json;
using ZSN.AI.Entity;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ZSN.AI.MCPClient
{
    /// <summary>
    /// MCP客户端Stdio实现 - 基于ModelContextProtocol
    /// </summary>
    public class MCPStdioClient : IMCPClient
    {
        private readonly IMcpClient _mcpClient;
        private readonly MCPConfig _config;
        private readonly ILogger? _logger;

        /// <summary>
        /// 初始化MCP Stdio客户端
        /// </summary>
        /// <param name="mcpClient">ModelContextProtocol客户端</param>
        /// <param name="config">MCP客户端配置</param>
        /// <param name="logger">日志记录器（可选）</param>
        public MCPStdioClient(IMcpClient mcpClient, MCPConfig config, ILogger? logger = null)
        {
            _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
            
            LogInfo($"MCP Stdio客户端已初始化，命令: {_config.Command}");
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
                
                // 获取工具列表来检查连接是否正常
                var tools = await _mcpClient.ListToolsAsync();
                var isHealthy = tools != null && tools.Count > 0;
                
                LogInfo($"服务健康检查结果: {(isHealthy ? "正常" : "异常")}");
                return isHealthy;
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
                LogInfo("Stdio正在获取可用工具列表...");
                
                var mcpTools = await _mcpClient.ListToolsAsync();

                LogInfo($"Stdio获取到{mcpTools.Count}个工具:{string.Join(", ", mcpTools.Select(t => t.Name))}");
                return mcpTools;
            }
            catch (Exception ex)
            {
                LogError($"Stdio获取工具列表异常: {ex.Message}");
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
                
                // 将JSON字符串参数转换为字典
                Dictionary<string, object?>? paramDict = null;
                if (!string.IsNullOrEmpty(parameters))
                {
                    paramDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        parameters, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                
                // 调用MCP工具
                var result = await _mcpClient.CallToolAsync(
                    toolName, 
                    paramDict ?? new Dictionary<string, object?>()
                );
                
                // 处理结果内容
                string responseText = "";
                foreach (var content in result.Content)
                {
                    if (content.Type == "text")
                    {
                        responseText += content.ToString();
                    }
                }
                
                LogInfo($"工具调用成功: {toolName}");
                return responseText;
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
                var parametersJson = JsonConvert.SerializeObject(parameters);
                LogInfo($"正在调用工具: {toolName}，参数类型: {parameters.GetType().Name}");
                
                // 将参数对象序列化为字典
                var paramDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                    parametersJson, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                
                // 调用MCP工具
                var result = await _mcpClient.CallToolAsync(
                    toolName, 
                    paramDict ?? new Dictionary<string, object?>()
                );
                
                // 处理结果内容
                string responseText = "";
                foreach (var content in result.Content)
                {
                    if (content.Type == "text")
                    {
                        responseText += content.ToString();
                    }
                }
                
                var typedResult = JsonConvert.DeserializeObject<TResult>(responseText);
                LogInfo($"工具调用成功: {toolName}");
                return typedResult;
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
