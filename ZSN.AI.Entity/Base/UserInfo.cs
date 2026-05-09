using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_user_info
    /// </summary>
    public partial class UserInfo
    {
		public UserInfo() { }
        #region AutoField
		/// <summary>
        /// UserID
        /// </summary>
		public Int32 UserID { get; set; } 
		/// <summary>
        /// uName
        /// </summary>
		public string UName { get; set; } = string.Empty;
		/// <summary>
        /// uPWD
        /// </summary>
		public string UPWD { get; set; } = string.Empty;
		/// <summary>
        /// PermissionCode
        /// </summary>
		public string PermissionCode { get; set; } = string.Empty;
		/// <summary>
        /// uState
        /// </summary>
		public Int32 UState { get; set; } = (Int32)(0);
        /// <summary>
        /// uAppendTime
        /// </summary>
        public DateTime UAppendTime { get; set; } = DateTime.Now;
        #endregion
    }
    public partial class UserInfoAccess
    {
        public Int32 UserID { get; set; } = 0;
        public string Token { get; set; } = "";
    }
}
