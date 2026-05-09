using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ZSN.AI.KnowledgeBase.Models
{
    /// <summary>
    /// 文档块向量记录
    /// </summary>
    public class DocumentChunkVector
    {
        public long Id { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public string ChunkId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float[]? Embedding { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public int TokenCount { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public System.DateTime UpdatedAt { get; set; }

        [JsonIgnore]
        public float[]? EmbeddingVector
        {
            get => Embedding;
            set => Embedding = value;
        }
    }

    /// <summary>
    /// 实体向量记录
    /// </summary>
    public class EntityEmbedding
    {
        public long Id { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityText { get; set; } = string.Empty;
        public float[]? Embedding { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public System.DateTime UpdatedAt { get; set; }

        [JsonIgnore]
        public float[]? EmbeddingVector
        {
            get => Embedding;
            set => Embedding = value;
        }
    }

    /// <summary>
    /// 向量相似度搜索结果
    /// </summary>
    public class VectorSearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float Similarity { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 向量相似度搜索结果（带上下文扩展）
    /// 包含匹配的chunk及其前后相邻的chunk
    /// </summary>
    public class VectorSearchResultWithContext
    {
        /// <summary>
        /// 匹配的chunk ID
        /// </summary>
        public string ChunkId { get; set; } = string.Empty;

        /// <summary>
        /// 匹配的chunk内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 相似度分数
        /// </summary>
        public float Similarity { get; set; }

        /// <summary>
        /// Chunk的序列号（用于确定顺序）
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// 之前的chunk内容（按顺序排列）
        /// </summary>
        public List<string> BeforeChunks { get; set; } = new List<string>();

        /// <summary>
        /// 之后的chunk内容（按顺序排列）
        /// </summary>
        public List<string> AfterChunks { get; set; } = new List<string>();

        /// <summary>
        /// 获取完整的上下文内容（Before + Match + After）
        /// </summary>
        public string GetFullContext()
        {
            var fullContext = new System.Text.StringBuilder();

            // 添加之前的chunk
            foreach (var before in BeforeChunks)
            {
                fullContext.AppendLine(before);
            }

            // 添加匹配的chunk
            fullContext.AppendLine(Content);

            // 添加之后的chunk
            foreach (var after in AfterChunks)
            {
                fullContext.AppendLine(after);
            }

            return fullContext.ToString();
        }

        /// <summary>
        /// 元数据
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
