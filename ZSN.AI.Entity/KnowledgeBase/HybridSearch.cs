using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Entity.KnowledgeBase
{
    /// <summary>
    /// 混合搜索结果
    /// </summary>
    public class HybridSearchResult
    {
        /// <summary>
        /// 向量检索结果
        /// </summary>
        public List<SearchResult> VectorResults { get; set; } = new();

        /// <summary>
        /// 图谱检索结果
        /// </summary>
        public List<GraphSearchResult> GraphResults { get; set; } = new();

        /// <summary>
        /// 融合后的结果
        /// </summary>
        public List<SearchResult> FusedResults { get; set; } = new();

        /// <summary>
        /// 相关路径
        /// </summary>
        public List<GraphPath> RelatedPaths { get; set; } = new();

        /// <summary>
        /// 检索元数据
        /// </summary>
        public SearchResultMetadata Metadata { get; set; } = new();

        /// <summary>
        /// chunk关联图片（key=chunkId, value=关联图片列表）
        /// 仅当 EnableImageSearch=true 时填充
        /// </summary>
        public Dictionary<string, List<ImageSearchResult>> ChunkImages { get; set; } = new();
    }

    /// <summary>
    /// 向量搜索结果
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// 块ID
        /// </summary>
        public string ChunkId { get; set; } = string.Empty;

        /// <summary>
        /// 文档ID
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 得分
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// 融合得分
        /// </summary>
        public float FusedScore { get; set; }

        /// <summary>
        /// 来源（"vector", "graph", "hybrid"）
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 相关路径
        /// </summary>
        public List<GraphPath> RelatedPaths { get; set; } = new();

        /// <summary>
        /// 元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 图谱搜索结果
    /// </summary>
    public class GraphSearchResult
    {
        /// <summary>
        /// 实体
        /// </summary>
        public Entity Entity { get; set; } = new();

        /// <summary>
        /// 相关路径
        /// </summary>
        public List<GraphPath> RelatedPaths { get; set; } = new();

        /// <summary>
        /// 得分
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// 匹配的块ID列表
        /// </summary>
        public List<string> MatchingChunkIds { get; set; } = new();
    }

    /// <summary>
    /// 混合搜索选项
    /// </summary>
    public class HybridSearchOptions
    {
        /// <summary>
        /// 向量检索权重
        /// </summary>
        public float VectorWeight { get; set; } = 0.6f;

        /// <summary>
        /// 图谱检索权重
        /// </summary>
        public float GraphWeight { get; set; } = 0.4f;

        /// <summary>
        /// 最大向量结果数
        /// </summary>
        public int MaxVectorResults { get; set; } = 20;

        /// <summary>
        /// 最大图谱结果数
        /// </summary>
        public int MaxGraphResults { get; set; } = 10;

        /// <summary>
        /// 是否启用重排序
        /// </summary>
        public bool EnableRerank { get; set; } = true;

        /// <summary>
        /// 是否启用图谱扩展
        /// </summary>
        public bool EnableGraphExpansion { get; set; } = true;

        /// <summary>
        /// 最大扩展跳数
        /// </summary>
        public int MaxExpansionHops { get; set; } = 2;

        /// <summary>
        /// 融合策略
        /// </summary>
        public FusionStrategy FusionStrategy { get; set; } = FusionStrategy.WeightedSum;

        /// <summary>
        /// 是否启用图片检索（返回关联图片信息），默认开启
        /// 需同时满足知识库 EnableImageProcessing=true 才实际生效
        /// </summary>
        public bool EnableImageSearch { get; set; } = true;
    }

    /// <summary>
    /// 融合策略
    /// </summary>
    public enum FusionStrategy
    {
        /// <summary>
        /// 加权求和
        /// </summary>
        WeightedSum,

        /// <summary>
        /// 倒数排名融合（RRF）
        /// </summary>
        ReciprocalRankFusion,

        /// <summary>
        /// Condorcet投票
        /// </summary>
        Condorcet,

        /// <summary>
        /// 学习排序
        /// </summary>
        LearningToRank
    }

    /// <summary>
    /// 图谱搜索选项
    /// </summary>
    public class GraphSearchOptions
    {
        /// <summary>
        /// 最大实体数
        /// </summary>
        public int MaxEntities { get; set; } = 10;

        /// <summary>
        /// 每个实体的最大路径数
        /// </summary>
        public int MaxPathsPerEntity { get; set; } = 5;

        /// <summary>
        /// 最大跳数
        /// </summary>
        public int MaxHops { get; set; } = 2;

        /// <summary>
        /// 关系类型过滤
        /// </summary>
        public List<string> RelationTypes { get; set; } = new();
    }

    /// <summary>
    /// 图谱增强搜索选项
    /// </summary>
    public class GraphEnhancedSearchOptions
    {
        /// <summary>
        /// 实体匹配阈值
        /// </summary>
        public float EntityMatchThreshold { get; set; } = 0.8f;

        /// <summary>
        /// 是否扩展相关实体
        /// </summary>
        public bool ExpandRelatedEntities { get; set; } = true;

        /// <summary>
        /// 扩展深度
        /// </summary>
        public int ExpansionDepth { get; set; } = 2;

        /// <summary>
        /// 是否包含关系上下文
        /// </summary>
        public bool IncludeRelationContext { get; set; } = true;
    }

    /// <summary>
    /// 重排序选项
    /// </summary>
    public class RerankOptions
    {
        /// <summary>
        /// 重排序方法
        /// </summary>
        public RerankMethod Method { get; set; } = RerankMethod.CrossEncoder;

        /// <summary>
        /// 返回前K个结果
        /// </summary>
        public int TopK { get; set; } = 10;

        /// <summary>
        /// 模型ID
        /// </summary>
        public string? ModelId { get; set; }
    }

    /// <summary>
    /// 重排序方法
    /// </summary>
    public enum RerankMethod
    {
        /// <summary>
        /// 交叉编码器
        /// </summary>
        CrossEncoder,

        /// <summary>
        /// LLM重排
        /// </summary>
        LLMRerank,

        /// <summary>
        /// 规则重排
        /// </summary>
        RuleBased
    }

    /// <summary>
    /// 搜索结果元数据
    /// </summary>
    public class SearchResultMetadata
    {
        /// <summary>
        /// 向量检索耗时
        /// </summary>
        public TimeSpan VectorSearchTime { get; set; }

        /// <summary>
        /// 图谱检索耗时
        /// </summary>
        public TimeSpan GraphSearchTime { get; set; }

        /// <summary>
        /// 结果融合耗时
        /// </summary>
        public TimeSpan FusionTime { get; set; }

        /// <summary>
        /// 总耗时
        /// </summary>
        public TimeSpan TotalTime { get; set; }

        /// <summary>
        /// 向量结果数量
        /// </summary>
        public int VectorResultCount { get; set; }

        /// <summary>
        /// 图谱结果数量
        /// </summary>
        public int GraphResultCount { get; set; }

        /// <summary>
        /// 最终结果数量
        /// </summary>
        public int FinalResultCount { get; set; }
    }
}
