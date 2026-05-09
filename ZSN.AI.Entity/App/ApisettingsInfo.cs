using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_apisettings_info
    /// </summary>
    public partial class ApisettingsInfo
    {
		public ApisettingsInfo() { }
        #region AutoField
		/// <summary>
        /// ApiID
        /// </summary>
		public Int32 ApiID { get; set; }
        /// <summary>
        /// CompanyID
        /// </summary>
        public int CompanyID { get; set; } = 0;
		/// <summary>
        /// AppID
        /// </summary>
		public string AppID { get; set; } = string.Empty;
		/// <summary>
        /// SecretKey
        /// </summary>
		public string SecretKey { get; set; } = string.Empty;
		/// <summary>
        /// SettingName
        /// </summary>
		public string SettingName { get; set; } = string.Empty;
		/// <summary>
        /// Remark
        /// </summary>
		public string Remark { get; set; } 
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
    public class ByteFile
    {
        public byte[] fileStream { get; set; } = new byte[0];
        public string contentType { get; set; } = "";
        public string fileName { get; set; } = "";
    }
    public partial class PostData
    {
        public PostData() { }

        public string AppID { get; set; }
        public string Data { get; set; }
        public string Timestamp { get; set; }
        public string Sign { get; set; }
    }
    public partial class GetToken
    {
        public GetToken() { }
        public string UserName { get; set; }
        public string PWD { get; set; }
    }

    public partial class MemberSettings
    {
        public MemberSettings() {  }
        public FullMemberInfo FullMember { get; set; }
        public MemberAuthInfo MemberAuth { get; set; }
        public MemberOtherAuthInfo MemberOtherAuth { get; set; }
        public StaffInfo StaffInfo { get; set; }

        public UserInfo UserInfo { get; set; }

        public string GetAppKey()
        {
            if (MemberAuth.AccessToken != null)
            {
                return MemberAuth.AccessToken.Substring(0, 16);
            }
            else
            {
                return null;
            }
        }
    }
}
