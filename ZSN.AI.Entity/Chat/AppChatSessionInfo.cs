using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_app_chat_session_info
    /// </summary>
    public partial class AppChatSessionInfo
    {
		public AppChatSessionInfo() { }
        #region AutoField
		/// <summary>
        /// ChatSessionID
        /// </summary>
		public string ChatSessionID { get; set; } = string.Empty;
        public string AppID { get; set; } = string.Empty;
        /// <summary>
        /// MemberID
        /// </summary>
        public string MemberID { get; set; } = string.Empty;
		/// <summary>
        /// TopicSummary
        /// </summary>
		public string TopicSummary { get; set; } 
		/// <summary>
        /// IsCoCreate
        /// </summary>
		public Int32 IsCoCreate { get; set; } = (Int32)(0);
		/// <summary>
        /// SystemStatus
        /// </summary>
		public Int32 SystemStatus { get; set; } = (Int32)(0);
		/// <summary>
        /// CreateTime
        /// </summary>
		public DateTime CreateTime { get; set; } 
        #endregion
    }
}
