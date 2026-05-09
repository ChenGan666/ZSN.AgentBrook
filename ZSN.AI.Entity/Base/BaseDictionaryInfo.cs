using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// base_dictionary_info
    /// </summary>
    public partial class BaseDictionaryInfo
    {
		public BaseDictionaryInfo() { }
        #region AutoField
		/// <summary>
        /// DicId
        /// </summary>
		public Int32 DicId { get; set; } 
		/// <summary>
        /// DicName
        /// </summary>
		public string DicName { get; set; } = string.Empty;
		/// <summary>
        /// DicTitle
        /// </summary>
		public string DicTitle { get; set; } = string.Empty;
		/// <summary>
        /// DicValue
        /// </summary>
		public string DicValue { get; set; } = string.Empty;
		/// <summary>
        /// DicRemark
        /// </summary>
		public string DicRemark { get; set; } 
		/// <summary>
        /// Remark
        /// </summary>
		public string Remark { get; set; } 
		/// <summary>
        /// Status
        /// </summary>
		public Int32 Status { get; set; } = (Int32)(0);
		/// <summary>
        /// Sort
        /// </summary>
		public Int32? Sort { get; set; } 
		/// <summary>
        /// Pid
        /// </summary>
		public Int32? Pid { get; set; } 
		/// <summary>
        /// Cid
        /// </summary>
		public Int32? Cid { get; set; } 
		/// <summary>
        /// CreateTime
        /// </summary>
		public DateTime CreateTime { get; set; } 
		/// <summary>
        /// UpdateTime
        /// </summary>
		public DateTime UpdateTime { get; set; } 

        public int TagCount { get; set; }
        public int KnowledgeBaseCount { get; set; }
        public string Icon { get; set; } = string.Empty;
        #endregion
    }
    /// <summary>
    /// DictionaryInfo
    /// </summary>
    public partial class BaseDictionaryInfo
    {
        public List<BaseDictionaryInfo> ChildrenList { get; set; }

        public List<BaseDictionaryInfo> RealChildrenList
        {
            get { return ChildrenList?.Where(t => t.Status==0).ToList(); }
        }
    }
}
