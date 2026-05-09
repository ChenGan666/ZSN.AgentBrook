using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_member_auth_info
    /// </summary>
    public partial class MemberAuthInfo
    {
		public MemberAuthInfo() { }
        #region AutoField
		/// <summary>
        /// MemberAuthID
        /// </summary>
		public int MemberAuthID { get; set; } 
		/// <summary>
        /// MemberID
        /// </summary>
		public string MemberID { get; set; } = string.Empty;
		/// <summary>
        /// AccessToken
        /// </summary>
		public string AccessToken { get; set; } = string.Empty;
		/// <summary>
        /// RefreshToken
        /// </summary>
		public string RefreshToken { get; set; } = string.Empty;
		/// <summary>
        /// maAppendTime
        /// </summary>
		public DateTime MaAppendTime { get; set; } 
		/// <summary>
        /// maUpdateTime
        /// </summary>
		public DateTime MaUpdateTime { get; set; } 
        #endregion
    }
}
