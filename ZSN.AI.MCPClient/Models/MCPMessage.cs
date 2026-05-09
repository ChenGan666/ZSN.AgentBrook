using System;

namespace ZSN.AI.MCPClient.Models
{
    /// <summary>
    /// MCP服务消息模型
    /// </summary>
    public class MCPMessage
    {
        /// <summary>
        /// 消息唯一标识
        /// </summary>
        public string? Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 消息内容
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 客户端标识
        /// </summary>
        public string? ClientId { get; set; }
        
        /// <summary>
        /// 工具名称
        /// </summary>
        public string? ToolName { get; set; }
        
        /// <summary>
        /// 工具参数（JSON格式）
        /// </summary>
        public string? ToolParameters { get; set; }
    }
}
