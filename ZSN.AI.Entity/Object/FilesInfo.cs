using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tbPICInfo
    /// </summary>
    public partial class FilesInfo
    {
		public FilesInfo() { }
        #region AutoField
		/// <summary>
        /// PICCode
        /// </summary>
		public string FileCode { get; set; } = string.Empty;
        /// <summary>
        /// fFilePath
        /// </summary>
        public string FFilePath { get; set; } = string.Empty;
        /// <summary>
        /// fName
        /// </summary>
        public string FName { get; set; } = string.Empty;
        /// <summary>
        /// fOriginName
        /// </summary>
        public string FOriginName { get; set; } = string.Empty;
        /// <summary>
        /// fType
        /// </summary>
        public string FType { get; set; } = string.Empty;
        /// <summary>
        /// fAppendTime
        /// </summary>
        public DateTime FAppendTime { get; set; } = DateTime.Now;
        #endregion
    }
}
