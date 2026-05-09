using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// log_level
    /// </summary>
    public partial class LogLevel
    {
		public LogLevel() { }
        #region AutoField
		/// <summary>
        /// Id
        /// </summary>
		public Int32 Id { get; set; } 
		/// <summary>
        /// LevelName
        /// </summary>
		public string LevelName { get; set; } 
		/// <summary>
        /// LevelRemarks
        /// </summary>
		public string LevelRemarks { get; set; } 
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
