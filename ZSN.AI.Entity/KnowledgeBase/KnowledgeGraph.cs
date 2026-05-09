using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Entity.KnowledgeBase
{
    /// <summary>
    /// 实体
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 实体文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 实体类型（PERSON, ORG, LOC, DATE, etc.）
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 实体属性
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; } = new();

        /// <summary>
        /// 来源块ID列表
        /// </summary>
        public List<string> SourceChunkIds { get; set; } = new();

        /// <summary>
        /// 置信度
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// 起始位置
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// 结束位置
        /// </summary>
        public int EndPosition { get; set; }
    }

    /// <summary>
    /// 关系
    /// </summary>
    public class Relation
    {
        /// <summary>
        /// 关系ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 头实体ID
        /// </summary>
        public string HeadEntityId { get; set; } = string.Empty;

        /// <summary>
        /// 尾实体ID
        /// </summary>
        public string TailEntityId { get; set; } = string.Empty;

        /// <summary>
        /// 关系类型
        /// </summary>
        public string RelationType { get; set; } = string.Empty;

        /// <summary>
        /// 关系描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 来源块ID列表
        /// </summary>
        public List<string> SourceChunkIds { get; set; } = new();

        /// <summary>
        /// 置信度
        /// </summary>
        public float Confidence { get; set; }
    }

    /// <summary>
    /// 图谱构建选项
    /// </summary>
    public class GraphBuildOptions
    {
        /// <summary>
        /// 是否抽取实体
        /// </summary>
        public bool ExtractEntities { get; set; } = true;

        /// <summary>
        /// 是否抽取关系
        /// </summary>
        public bool ExtractRelations { get; set; } = true;

        /// <summary>
        /// 是否向量化实体
        /// </summary>
        public bool VectorizeEntities { get; set; } = true;

        /// <summary>
        /// 是否启用去重
        /// </summary>
        public bool EnableDeduplication { get; set; } = true;

        /// <summary>
        /// 每块最大实体数
        /// </summary>
        public int MaxEntitiesPerChunk { get; set; } = 50;

        /// <summary>
        /// 每块最大关系数
        /// </summary>
        public int MaxRelationsPerChunk { get; set; } = 30;
    }

    /// <summary>
    /// 实体抽取配置
    /// </summary>
    public class EntityExtractionConfig
    {
        /// <summary>
        /// 模型ID
        /// </summary>
        public int ModelId { get; set; }

        /// <summary>
        /// 过滤的实体类型
        /// </summary>
        public List<string> EntityTypes { get; set; } = new();

        /// <summary>
        /// 最小置信度
        /// </summary>
        public float MinConfidence { get; set; } = 0.7f;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;
    }

    /// <summary>
    /// 图谱查询结果
    /// </summary>
    public class GraphQueryResult
    {
        /// <summary>
        /// 结果行列表
        /// </summary>
        public List<Dictionary<string, object>> Rows { get; set; } = new();

        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// 图谱查询选项
    /// </summary>
    public class GraphQueryOptions
    {
        /// <summary>
        /// 限制数量
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// 偏移量
        /// </summary>
        public int? Offset { get; set; }

        /// <summary>
        /// 查询参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// 图谱路径
    /// </summary>
    public class GraphPath
    {
        /// <summary>
        /// 路径节点列表
        /// </summary>
        public List<GraphPathNode> Nodes { get; set; } = new();

        /// <summary>
        /// 路径边列表
        /// </summary>
        public List<GraphPathEdge> Edges { get; set; } = new();

        /// <summary>
        /// 相关性得分
        /// </summary>
        public float RelevanceScore { get; set; }

        /// <summary>
        /// 相关块ID列表
        /// </summary>
        public List<string> ChunkIds { get; set; } = new();
    }

    /// <summary>
    /// 路径节点
    /// </summary>
    public class GraphPathNode
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 节点文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 节点类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 节点属性
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// 路径边
    /// </summary>
    public class GraphPathEdge
    {
        /// <summary>
        /// 关系类型
        /// </summary>
        public string RelationType { get; set; } = string.Empty;

        /// <summary>
        /// 关系描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 置信度
        /// </summary>
        public float Confidence { get; set; }
    }

    /// <summary>
    /// 图谱统计信息
    /// </summary>
    public class GraphStatistics
    {
        /// <summary>
        /// 总实体数
        /// </summary>
        public int TotalEntities { get; set; }

        /// <summary>
        /// 总关系数
        /// </summary>
        public int TotalRelations { get; set; }

        /// <summary>
        /// 按类型统计的实体数
        /// </summary>
        public Dictionary<string, int> EntityCountsByType { get; set; } = new();

        /// <summary>
        /// 按类型统计的关系数
        /// </summary>
        public Dictionary<string, int> RelationCountsByType { get; set; } = new();
    }

    /// <summary>
    /// 图谱可视化数据
    /// </summary>
    public class GraphVisualizationData
    {
        /// <summary>
        /// 节点列表（实体）
        /// </summary>
        public List<GraphNode> Nodes { get; set; } = new();

        /// <summary>
        /// 关系列表（边）
        /// </summary>
        public List<GraphLink> Links { get; set; } = new();

        /// <summary>
        /// 节点总数
        /// </summary>
        public int NodeCount => Nodes.Count;

        /// <summary>
        /// 关系总数
        /// </summary>
        public int LinkCount => Links.Count;
    }

    /// <summary>
    /// 图谱节点（实体）
    /// </summary>
    public class GraphNode
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 节点名称（实体文本）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点类型（实体类型）
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 节点大小（可选，用于可视化）
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// 节点属性
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new();

        /// <summary>
        /// 来源块ID列表
        /// </summary>
        public List<string> SourceChunkIds { get; set; } = new();

        /// <summary>
        /// 置信度
        /// </summary>
        public float Confidence { get; set; }
    }

    /// <summary>
    /// 图谱关系（边）
    /// </summary>
    public class GraphLink
    {
        /// <summary>
        /// 源节点ID
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 目标节点ID
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// 关系类型
        /// </summary>
        public string RelationType { get; set; } = string.Empty;

        /// <summary>
        /// 关系描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 置信度
        /// </summary>
        public float Confidence { get; set; }
    }
}
