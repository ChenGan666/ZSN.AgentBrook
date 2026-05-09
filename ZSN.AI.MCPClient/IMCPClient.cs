using ModelContextProtocol.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZSN.AI.MCPClient
{
    /// <summary>
    /// MCP客户端接口定义
    /// </summary>
    public interface IMCPClient 
    {
        /// <summary>
        /// 检查MCP服务健康状态
        /// </summary>
        /// <returns>服务是否健康</returns>
        Task<bool> CheckHealthAsync();

        /// <summary>
        /// 获取MCP服务可用工具列表
        /// </summary>
        /// <returns>工具名称列表</returns>
        Task<IList<McpClientTool>> GetToolsAsync();

        /// <summary>
        /// 调用MCP服务工具
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <param name="parameters">工具参数（JSON格式）</param>
        /// <returns>调用结果</returns>
        Task<string> InvokeToolAsync(string toolName, string parameters);

        /// <summary>
        /// 调用MCP服务工具并返回强类型结果
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="toolName">工具名称</param>
        /// <param name="parameters">工具参数对象</param>
        /// <returns>调用结果</returns>
        Task<TResult?> InvokeToolAsync<TResult>(string toolName, object parameters);
    }
}
