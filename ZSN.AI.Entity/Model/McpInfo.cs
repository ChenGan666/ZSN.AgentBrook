using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_mcp_info
    /// </summary>
    public partial class McpInfo
    {
		public McpInfo() { }
        #region AutoField
		/// <summary>
        /// MCPID
        /// </summary>
		public string MCPID { get; set; } = string.Empty;
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; } = string.Empty;
		/// <summary>
        /// Description
        /// </summary>
		public string Description { get; set; } = string.Empty;
		/// <summary>
        /// Tag
        /// </summary>
		public string Tag { get; set; } 
		/// <summary>
        /// ICON
        /// </summary>
		public string ICON { get; set; } 
		/// <summary>
        /// Config
        /// </summary>
		public string Config { get; set; } = string.Empty;
		/// <summary>
        /// EnvironmentVar
        /// </summary>
		public string EnvironmentVar { get; set; } = string.Empty;
        public List<Output> OutputConfig { get; set; } = new List<Output>();
		/// <summary>
        /// CreateTime
        /// </summary>
		public DateTime CreateTime { get; set; } = DateTime.Now;
		/// <summary>
        /// SystemStatus
        /// </summary>
		public McpState SystemStatus { get; set; } = McpState.Disabled;
		/// <summary>
        /// RunHost
        /// </summary>
		public RunHostType RunHost { get; set; } = RunHostType.Server;
        #endregion
    }
    public enum McpState
    {
        Disabled = 1,
        Normal = 0
    }
    public enum RunHostType
    {
        Client = 0,
        Server = 1
    }
}
