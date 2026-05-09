using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_app_chat_summary_info
    /// </summary>
    public partial class AppChatSummaryInfo
    {
		public AppChatSummaryInfo() { }
        #region AutoField
		/// <summary>
        /// SummaryID
        /// </summary>
		public string SummaryID { get; set; } = Guid.NewGuid().ToString();
		/// <summary>
        /// AppID
        /// </summary>
		public string AppID { get; set; } 
		/// <summary>
        /// ChatSessionID
        /// </summary>
		public string ChatSessionID { get; set; } = string.Empty;
		/// <summary>
        /// Content
        /// </summary>
		public string Content { get; set; } = string.Empty;
		/// <summary>
        /// CreateTime
        /// </summary>
		public DateTime CreateTime { get; set; } 
		/// <summary>
        /// LogOrder
        /// </summary>
		public string ChatLogIDList { get; set; } = string.Empty;
        #endregion
    }
}
