using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity.Model.Enum;
namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_knowledge_base_file_info
    /// </summary>
    public partial class KnowledgeBaseFileInfo
    {
        public KnowledgeBaseFileInfo() { }
        #region AutoField
        /// <summary>
        /// FileID
        /// </summary>
        public string FileID { get; set; } = string.Empty;
        public string KnowledgeBaseID { get; set; } = string.Empty;
        /// <summary>
        /// FileName
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        /// <summary>
        /// FilePath
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        /// <summary>
        /// Type
        /// </summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>
        /// ParserConfig
        /// </summary>
        public string ParserConfig { get; set; } = string.Empty;
        /// <summary>
        /// CreateTime
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// SystemStatus
        /// </summary>
        public ImportKmsStatus SystemStatus { get; set; }
        public int DataCount { get; set; }
        #endregion
    }
}
