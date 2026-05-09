using System;
using System.Text.Json.Serialization;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// MCP客户端配置类
    /// </summary>
    public class MCPConfig
    {
        /// <summary>
        /// 连接类型
        /// </summary>
        public MCPConnectionType Type { get; set; } = MCPConnectionType.Stdio;

        /// <summary>
        /// 服务器地址（HTTP连接方式使用）
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 客户端ID
        /// </summary>
        public string ClientId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 客户端名称
        /// </summary>
        public string ClientName { get; set; } = "ZSN.AI.MCPClient";

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// 是否启用日志
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// 命令行路径（Stdio连接方式使用）
        /// </summary>
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// 命令行参数（Stdio连接方式使用）
        /// </summary>
        public string[] Args { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 环境变量（Stdio连接方式使用）
        /// </summary>
        public Dictionary<string, string>? Env { get; set; }

        // 自动批准的工具列表
        public List<string>? AutoApprove { get; set; }
        // SSE 可选自定义请求头
        public Dictionary<string, string>? Headers { get; set; }

        public McpInfo Info { get; set; } = new McpInfo();
    }

    /// <summary>
    /// MCP连接类型
    /// </summary>
    public enum MCPConnectionType
    {
        /// <summary>
        /// HTTP连接
        /// </summary>
        Http,

        /// <summary>
        /// 标准输入输出连接
        /// </summary>
        Stdio,
        
        /// <summary>
        /// SSE服务器发送事件连接
        /// </summary>
        Sse,
        
        /// <summary>
        /// 流式连接
        /// </summary>
        Stream,

        /// <summary>
        /// gRPC连接
        /// </summary>
        Grpc
    }
}
