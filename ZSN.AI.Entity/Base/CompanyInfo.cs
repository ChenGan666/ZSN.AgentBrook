using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_company_info
    /// </summary>
    public partial class CompanyInfo
    {
		public CompanyInfo() { }
        #region AutoField
		/// <summary>
        /// CompanyID
        /// </summary>
		public Int32 CompanyID { get; set; } 
		/// <summary>
        /// cFullName
        /// </summary>
		public string CFullName { get; set; } = string.Empty;
		/// <summary>
        /// cTitle
        /// </summary>
		public string CTitle { get; set; } = string.Empty;
		/// <summary>
        /// cIDCode
        /// </summary>
		public string CIDCode { get; set; } = string.Empty;
		/// <summary>
        /// cCity
        /// </summary>
		public string CCity { get; set; } = string.Empty;
		/// <summary>
        /// cScale
        /// </summary>
		public string CScale { get; set; } = string.Empty;
		/// <summary>
        /// cInfo
        /// </summary>
		public string CInfo { get; set; } = string.Empty;
		/// <summary>
        /// cLogo
        /// </summary>
		public string CLogo { get; set; } = string.Empty;
		/// <summary>
        /// cAppendTime
        /// </summary>
		public DateTime CAppendTime { get; set; } = DateTime.Now;
        public string AppID { get; set; }  = Guid.NewGuid().ToString();
        public string SecretKey { get;set; } = Guid.NewGuid().ToString();
        #endregion
    }
}
