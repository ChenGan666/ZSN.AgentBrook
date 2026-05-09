using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_member_other_auth_info
    /// </summary>
    public partial class MemberOtherAuthInfo
    {
		public MemberOtherAuthInfo() { }
        #region AutoField
		/// <summary>
        /// MemberOtherAuthID
        /// </summary>
		public Int32 MemberOtherAuthID { get; set; } 
		/// <summary>
        /// MemberID
        /// </summary>
		public string MemberID { get; set; } = string.Empty;
		/// <summary>
        /// Server_Type
        /// </summary>
		public Int32 ServerType { get; set; } 
		/// <summary>
        /// Server_ID
        /// </summary>
		public Int32 ServerID { get; set; } = (Int32)(0);
		/// <summary>
        /// User_Nickname
        /// </summary>
		public string UserNickname { get; set; } 
		/// <summary>
        /// OpenID
        /// </summary>
		public string OpenID { get; set; } 
		/// <summary>
        /// UnionID
        /// </summary>
		public string UnionID { get; set; } 
		/// <summary>
        /// Session_Key
        /// </summary>
		public string SessionKey { get; set; } 
		/// <summary>
        /// AccessToken
        /// </summary>
		public string AccessToken { get; set; } 
		/// <summary>
        /// RefreshToken
        /// </summary>
		public string RefreshToken { get; set; } 
		/// <summary>
        /// Phone
        /// </summary>
		public string Phone { get; set; } 
		/// <summary>
        /// Sex
        /// </summary>
		public Int32 Sex { get; set; } 
		/// <summary>
        /// Language
        /// </summary>
		public string Language { get; set; } = "zh_cn";
		/// <summary>
        /// Country
        /// </summary>
		public string Country { get; set; } 
		/// <summary>
        /// Province
        /// </summary>
		public string Province { get; set; } 
		/// <summary>
        /// City
        /// </summary>
		public string City { get; set; } 
		/// <summary>
        /// Region
        /// </summary>
		public string Region { get; set; } 
		/// <summary>
        /// Head_img
        /// </summary>
		public string HeadImg { get; set; } 
		/// <summary>
        /// Subscribe
        /// </summary>
		public Int32 Subscribe { get; set; } 
		/// <summary>
        /// Remake
        /// </summary>
		public string Remake { get; set; } 
		/// <summary>
        /// Append_Time
        /// </summary>
		public DateTime AppendTime { get; set; } 
		/// <summary>
        /// Update_Time
        /// </summary>
		public DateTime UpdateTime { get; set; } 
        #endregion
    }
}
