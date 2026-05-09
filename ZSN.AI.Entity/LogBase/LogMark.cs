using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// log_mark
    /// </summary>
    public partial class LogMark
    {
		public LogMark() { }
        #region AutoField
		/// <summary>
        /// Id
        /// </summary>
		public Int32 Id { get; set; } 
		/// <summary>
        /// MarkName
        /// </summary>
		public string MarkName { get; set; } 
		/// <summary>
        /// MarkRemarks
        /// </summary>
		public string MarkRemarks { get; set; } 
		/// <summary>
        /// ClassId
        /// </summary>
		public Int32 ClassId { get; set; } 
		/// <summary>
        /// LevelId
        /// </summary>
		public Int32 LevelId { get; set; } 
		/// <summary>
        /// Status
        /// </summary>
		public bool Status { get; set; } 
		/// <summary>
        /// CreateTime
        /// </summary>
		public DateTime CreateTime { get; set; } = DateTime.Now;
		/// <summary>
        /// UpdateTime
        /// </summary>
		public DateTime UpdateTime { get; set; } = DateTime.Now;
        #endregion
    }
}
