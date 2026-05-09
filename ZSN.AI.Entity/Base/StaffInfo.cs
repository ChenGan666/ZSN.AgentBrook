using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_staff_info
    /// </summary>
    public partial class StaffInfo
    {
		public StaffInfo() { }
        #region AutoField
		/// <summary>
        /// StaffID
        /// </summary>
		public Int32 StaffID { get; set; } 
		/// <summary>
        /// sCode
        /// </summary>
		public string SCode { get; set; } = string.Empty;
		/// <summary>
        /// sName
        /// </summary>
		public string SName { get; set; } = string.Empty;
		/// <summary>
        /// sTitle
        /// </summary>
		public string STitle { get; set; } 
		/// <summary>
        /// DepartmentID
        /// </summary>
		public Int32 DepartmentID { get; set; } 
		/// <summary>
        /// dName
        /// </summary>
		public string DName { get; set; } = string.Empty;
		/// <summary>
        /// sEntryTime
        /// </summary>
		public DateTime SEntryTime { get; set; } 
		/// <summary>
        /// sState
        /// </summary>
		public Int32 SState { get; set; } 
		/// <summary>
        /// sAppendTime
        /// </summary>
		public DateTime SAppendTime { get; set; } = DateTime.Now;
		/// <summary>
        /// sEmail
        /// </summary>
		public string SEmail { get; set; } = string.Empty;
		/// <summary>
        /// sPhone
        /// </summary>
		public string SPhone { get; set; } = string.Empty;
		/// <summary>
        /// MemberID
        /// </summary>
		public string MemberID { get; set; } = string.Empty;
        public int UserID { get; set; } = 0;
        #endregion
    }
}
