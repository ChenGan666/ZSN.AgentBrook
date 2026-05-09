using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// log_mark_class
    /// </summary>
    public partial class LogMarkClass
    {
		public LogMarkClass() { }
        #region AutoField
		/// <summary>
        /// Id
        /// </summary>
		public Int32 Id { get; set; } 
		/// <summary>
        /// ClassName
        /// </summary>
		public string ClassName { get; set; } 
		/// <summary>
        /// ClassRemarks
        /// </summary>
		public string ClassRemarks { get; set; } 
		/// <summary>
        /// ParentId
        /// </summary>
		public Int32 ParentId { get; set; } 
		/// <summary>
        /// RootId
        /// </summary>
		public Int32 RootId { get; set; } 
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
