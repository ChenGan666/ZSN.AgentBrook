using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_knowledge_base_info
    /// </summary>
    public partial class KnowledgeBaseInfo
    {
        public KnowledgeBaseInfo() { }
        #region 
        /// <summary>
        /// KnowledgeBaseID
        /// </summary>
        public string KnowledgeBaseID { get; set; } = string.Empty;
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// DicIDList
        /// </summary>
        public string DicIDList { get; set; }
        /// <summary>
        /// DicNameList
        /// </summary>
        public string DicNameList { get; set; }
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// PreprocessModelID
        /// </summary>
        public int PreprocessModelID { get; set; }
        public string PreprocessModelName { get; set; } = string.Empty;
        /// <summary>
        /// VectorModelID
        /// </summary>
        public int VectorModelID { get; set; }
        public string VectorModelName { get; set; } = string.Empty;
        /// <summary>
        /// ParagraphSlice,分块长度
        /// </summary>
        public int ParagraphSlice { get; set; } = 1000;
        /// <summary>
        /// LineSliceCount
        /// </summary>
        public int LineSliceCount { get; set; } = 1000;
        /// <summary>
        /// OverlapSection,适合需要上下文连续性的场景,0=不重叠
        /// </summary>
        public int OverlapSection { get; set; } = 20;
        /// <summary>
        /// SystemStatus
        /// </summary>
        public KnowledgeBaseStatus SystemStatus { get; set; } = KnowledgeBaseStatus.UnderReview;
        /// <summary>
        /// MemberID
        /// </summary>
        public string MemberID { get; set; } = string.Empty;
        /// <summary>
        /// ChargeType
        /// </summary>
        public int ChargeType { get; set; } = 0;
        /// <summary>
        /// CreateTime
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// LastUpdateTime
        /// </summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;
        #endregion
    }

    public enum KnowledgeBaseStatus
    {
        Disabled = -1,
        UnderReview = 0,
        Normal = 1,
    }
}
