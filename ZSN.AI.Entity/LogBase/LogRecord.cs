using System;

namespace ZSN.AI.Entity
{
	/// <summary>
    /// log_record
    /// </summary>
    public partial class LogRecord
    {
		public LogRecord() { }
        #region AutoField
		/// <summary>
        /// Id
        /// </summary>
		public Int64 Id { get; set; } 
		/// <summary>
        /// MarkId
        /// </summary>
		public Int32 MarkId { get; set; } 
		/// <summary>
        /// LevelId
        /// </summary>
		public Int32 LevelId { get; set; } 
		/// <summary>
        /// LogDetail
        /// </summary>
		public string LogDetail { get; set; } 
		/// <summary>
        /// LogRemarks
        /// </summary>
		public string LogRemarks { get; set; } 
		/// <summary>
        /// LogUrl
        /// </summary>
		public string LogUrl { get; set; } 
		/// <summary>
        /// LogCreatorId
        /// </summary>
		public string LogCreatorId { get; set; } = string.Empty;
		/// <summary>
        /// LogCreatorIP
        /// </summary>
		public string LogCreatorIP { get; set; } = string.Empty;
		/// <summary>
        /// OperateTime
        /// </summary>
		public DateTime OperateTime { get; set; } = DateTime.Now;
		/// <summary>
        /// DateCode
        /// </summary>
		public Int32 DateCode { get; set; } 
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
