using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_department_info
    /// </summary>
    public partial class DepartmentInfo
    {
		public DepartmentInfo() { }
        #region AutoField
		/// <summary>
        /// DepartmentID
        /// </summary>
		public Int32 DepartmentID { get; set; } 
		/// <summary>
        /// dName
        /// </summary>
		public string DName { get; set; } = string.Empty;
		/// <summary>
        /// dInfo
        /// </summary>
		public string DInfo { get; set; } = string.Empty;
		/// <summary>
        /// dAppendtime
        /// </summary>
		public DateTime DAppendtime { get; set; } = DateTime.Now;
		/// <summary>
        /// dState
        /// </summary>
		public Int32 DState { get; set; } 
        #endregion
    }
}
