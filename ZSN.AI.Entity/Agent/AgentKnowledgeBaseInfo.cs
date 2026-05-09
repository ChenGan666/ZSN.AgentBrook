using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_app_knowledge_base_info
    /// </summary>
    public partial class AgentKnowledgeBaseInfo
    {
		public AgentKnowledgeBaseInfo() { }
        #region AutoField
		/// <summary>
        /// AgentKnowledgeBaseID
        /// </summary>
		public Int32 AgentKnowledgeBaseID { get; set; } 
		/// <summary>
        /// AppID
        /// </summary>
		public string AgentID { get; set; } = string.Empty;
		/// <summary>
        /// KnowledgeBaseID
        /// </summary>
		public string KnowledgeBaseID { get; set; } = string.Empty;
		/// <summary>
        /// Weight
        /// </summary>
		public Int32 Weight { get; set; } = (Int32)(0);
        #endregion
    }
}
