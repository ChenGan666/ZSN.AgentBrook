using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_workflow_edge_info
    /// </summary>
    public partial class WorkflowEdgeInfo
    {
		public WorkflowEdgeInfo() { }
        #region AutoField
		/// <summary>
        /// LineID
        /// </summary>
		public string EdgeID { get; set; } = string.Empty;
		/// <summary>
        /// WorkflowID
        /// </summary>
		public string WorkflowID { get; set; } = string.Empty;
		/// <summary>
        /// SourceNodeId
        /// </summary>
		public string SourceNodeId { get; set; } 
		/// <summary>
        /// TargetNodeId
        /// </summary>
		public string TargetNodeId { get; set; } 
		/// <summary>
        /// Condition
        /// </summary>
		public object? ConditionConfig { get; set; } 
		/// <summary>
        /// LName
        /// </summary>
		public string LName { get; set; }
        public object? Config { get; set; }
        /// <summary>
        /// CreateTime
        /// </summary>
        public DateTime? CreateTime { get; set; } = DateTime.Now;
		/// <summary>
        /// LastUpdateTime
        /// </summary>
		public DateTime? LastUpdateTime { get; set; } = DateTime.Now;
        #endregion
    }
}
