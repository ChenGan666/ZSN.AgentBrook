using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Entity.KnowledgeBase
{
    /// <summary>
    /// 语义块
    /// </summary>
    public class SemanticChunk
    {
        /// <summary>
        /// 块唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 所属文档ID
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>
        /// 块内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 在原文中的起始位置
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// 在原文中的结束位置
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// Token数量
        /// </summary>
        public int TokenCount { get; set; }

        /// <summary>
        /// 包含的句子列表
        /// </summary>
        public List<string> Sentences { get; set; } = new();

        /// <summary>
        /// 包含的实体名称列表
        /// </summary>
        public List<string> EntityNames { get; set; } = new();

        /// <summary>
        /// 块摘要
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// 相关块ID列表
        /// </summary>
        public List<string> RelatedChunkIds { get; set; } = new();

        /// <summary>
        /// 元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 分块策略枚举
    /// </summary>
    public enum ChunkingStrategy
    {
        /// <summary>
        /// 原有硬切块（兼容模式）
        /// </summary>
        HardCutoff = 0,

        /// <summary>
        /// 语义边界分块
        /// </summary>
        SemanticBoundary = 1,

        /// <summary>
        /// 主题分割分块
        /// </summary>
        TopicSegmentation = 2,

        /// <summary>
        /// 实体感知分块
        /// </summary>
        EntityAware = 3,

        /// <summary>
        /// LLM智能分块
        /// </summary>
        LLMIntelligent = 4
    }

    /// <summary>
    /// 分块统计信息
    /// </summary>
    public class ChunkingStatistics
    {
        /// <summary>
        /// 总块数
        /// </summary>
        public int TotalChunks { get; set; }

        /// <summary>
        /// 总Token数
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// 平均每块Token数
        /// </summary>
        public double AverageTokensPerChunk { get; set; }

        /// <summary>
        /// 最小块Token数
        /// </summary>
        public int MinTokens { get; set; }

        /// <summary>
        /// 最大块Token数
        /// </summary>
        public int MaxTokens { get; set; }

        /// <summary>
        /// 总实体数
        /// </summary>
        public int TotalEntities { get; set; }
    }
}
