using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_vcode_info
    /// </summary>
    public partial class VcodeInfo
    {
		public VcodeInfo() { }
        #region AutoField
		/// <summary>
        /// VCodeID
        /// </summary>
		public Int32 VCodeID { get; set; } 
		/// <summary>
        /// PhoneNumber
        /// </summary>
		public string PhoneNumber { get; set; } = string.Empty;
		/// <summary>
        /// VCode
        /// </summary>
		public string VCode { get; set; } = string.Empty;
		/// <summary>
        /// VAppendTime
        /// </summary>
		public DateTime VAppendTime { get; set; } = DateTime.Now;
		/// <summary>
        /// VState
        /// </summary>
		public Int32 VState { get; set; } = (Int32)(0);
        #endregion
    }
}
