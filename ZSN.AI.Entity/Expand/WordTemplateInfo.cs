using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_word_template_Info
    /// </summary>
    public partial class WordTemplateInfo
    {
		public WordTemplateInfo() { }
        
        #region AutoField
        /// <summary>
        /// WordTemplateID
        /// </summary>
        public string WordTemplateID { get; set; } = string.Empty;
		/// <summary>
        /// wName
        /// </summary>
		public string WName { get; set; } = string.Empty;
		/// <summary>
        /// wDescription
        /// </summary>
		public string WDescription { get; set; } 
		/// <summary>
        /// FileCode
        /// </summary>
		public string FileCode { get; set; } = string.Empty;
		/// <summary>
        /// wLabel
        /// </summary>
		public string WLabel { get; set; } = string.Empty;
		/// <summary>
        /// CreateTime
        /// </summary>
		public DateTime CreateTime { get; set; } 
		/// <summary>
        /// UpdateTime
        /// </summary>
		public DateTime UpdateTime { get; set; } 
		/// <summary>
        /// SystemStatus
        /// </summary>
		public Int32 SystemStatus { get; set; } 
        #endregion
    }
    public enum WordTemplateStatus
    {
        Disabled = 1,
        Normal = 0
    }
}
