using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_knowledge_base_file_chunk_info
    /// </summary>
    public partial class KnowledgeBaseFileChunkInfo
    {
        public KnowledgeBaseFileChunkInfo() { }
        #region AutoField
        /// <summary>
        /// ChunkID
        /// </summary>
        public string id { get; set; } = string.Empty;
        /// <summary>
        /// KnowledgeBaseID
        /// </summary>
        public string embedding { get; set; } = string.Empty;
        /// <summary>
        /// FileID
        /// </summary>
        public string tags { get; set; } = string.Empty;
        /// <summary>
        /// FileChunkIndex
        /// </summary>
        public string content { get; set; }
        /// <summary>
        /// Thumbnail
        /// </summary>
        public string payload { get; set; } = string.Empty;
        
        #endregion
    }
}
