using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_knowledge_base_tag_info
    /// </summary>
    public partial class KnowledgeBaseTagInfo
    {
		public KnowledgeBaseTagInfo() { }
        #region AutoField
		/// <summary>
        /// KnowledgeBaseTagID
        /// </summary>
		public Int32 KnowledgeBaseTagID { get; set; } 
		/// <summary>
        /// TagClassID
        /// </summary>
		public Int32 TagClassID { get; set; } 
        public string TagClassName { get; set; } = string.Empty;
		/// <summary>
        /// Tag
        /// </summary>
		public string Tag { get; set; } = string.Empty;
		/// <summary>
        /// tAppendTime
        /// </summary>
		public DateTime TAppendTime { get; set; } = DateTime.Now;
		/// <summary>
        /// tCount
        /// </summary>
		public Int32 TCount { get; set; } = (Int32)(0);
		/// <summary>
        /// tSummary
        /// </summary>
		public string TSummary { get; set; } = string.Empty;
        #endregion
    }
}
