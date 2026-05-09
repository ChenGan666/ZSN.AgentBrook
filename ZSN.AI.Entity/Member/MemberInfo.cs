using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_member_info
    /// </summary>
    public partial class MemberInfo
    {
		public MemberInfo() { }
        #region AutoField
		/// <summary>
        /// MemberID
        /// </summary>
		public string MemberID { get; set; } = string.Empty;
		/// <summary>
        /// mPhoneNumber
        /// </summary>
		public string MPhoneNumber { get; set; } = string.Empty;
		/// <summary>
        /// mNickName
        /// </summary>
		public string MNickName { get; set; } = string.Empty;

        /// <summary>
        /// mPWD
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MPWD { get; set; } = string.Empty;
		/// <summary>
        /// mIcon
        /// </summary>
		public string MIcon { get; set; } = string.Empty;
        /// <summary>
        /// mBirthday
        /// </summary>
        [System.Text.Json.Serialization.JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime MBirthday { get; set; } = DateTime.Now;
		/// <summary>
        /// mState
        /// </summary>
		public Int32 MState { get; set; } = (Int32)(0);
        /// <summary>
        /// mPoints
        /// </summary>
        public Int32 MPoints { get; set; } = 0;
        /// <summary>
        /// mLevel
        /// </summary>
        public Int32 MLevel { get; set; } = 0;
		/// <summary>
        /// mIntroducer
        /// </summary>
		public string MIntroducer { get; set; }
        /// <summary>
        /// mAppendTime
        /// </summary>
        public DateTime MAppendTime { get; set; } = DateTime.Now;
        #endregion
    }
    public partial class FullMemberInfo
    {
        public FullMemberInfo() { }
        public MemberInfo Member { get; set; }


    }
}
