using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Entity.KnowledgeBase
{
    /// <summary>
    /// 图路径查询结果
    /// </summary>
    public class GraphPathResult
    {
        /// <summary>
        /// 路径长度（跳数）
        /// </summary>
        public int PathLength { get; set; }

        /// <summary>
        /// 路径上的顶点ID列表
        /// </summary>
        public List<string> VertexIds { get; set; } = new();

        /// <summary>
        /// 路径上的顶点详情列表
        /// </summary>
        public List<Dictionary<string, object>> Vertices { get; set; } = new();

        /// <summary>
        /// 路径上的边详情列表
        /// </summary>
        public List<Dictionary<string, object>> Edges { get; set; } = new();

        /// <summary>
        /// 路径总权重（如果有权重属性）
        /// </summary>
        public double? TotalWeight { get; set; }
    }

    /// <summary>
    /// 子图查询结果
    /// </summary>
    public class SubGraphResult
    {
        /// <summary>
        /// 子图中的顶点列表
        /// </summary>
        public List<Dictionary<string, object>> Vertices { get; set; } = new();

        /// <summary>
        /// 子图中的边列表
        /// </summary>
        public List<Dictionary<string, object>> Edges { get; set; } = new();

        /// <summary>
        /// 顶点数量
        /// </summary>
        public int VertexCount => Vertices.Count;

        /// <summary>
        /// 边数量
        /// </summary>
        public int EdgeCount => Edges.Count;
    }

    /// <summary>
    /// 邻居查询结果
    /// </summary>
    public class NeighborResult
    {
        /// <summary>
        /// 邻居顶点ID
        /// </summary>
        public string VertexId { get; set; } = string.Empty;

        /// <summary>
        /// 邻居顶点属性
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// 连接的边
        /// </summary>
        public Dictionary<string, object> Edge { get; set; } = new();

        /// <summary>
        /// 关系类型（边标签）
        /// </summary>
        public string RelationType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 边定义
    /// </summary>
    public class EdgeDefinition
    {
        /// <summary>
        /// 起始顶点ID
        /// </summary>
        [Required]
        public string StartVertexId { get; set; } = string.Empty;

        /// <summary>
        /// 结束顶点ID
        /// </summary>
        [Required]
        public string EndVertexId { get; set; } = string.Empty;

        /// <summary>
        /// 边标签
        /// </summary>
        [Required]
        public string EdgeLabel { get; set; } = string.Empty;

        /// <summary>
        /// 边属性
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// 图索引信息
    /// </summary>
    public class GraphIndexInfo
    {
        /// <summary>
        /// 索引名称
        /// </summary>
        public string IndexName { get; set; } = string.Empty;

        /// <summary>
        /// 索引类型（label, property, edge_label, composite）
        /// </summary>
        public string IndexType { get; set; } = string.Empty;

        /// <summary>
        /// 标签名称
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// 属性名称（对于属性索引）
        /// </summary>
        public string? Property { get; set; }

        /// <summary>
        /// 属性列表（对于复合索引）
        /// </summary>
        public List<string> Properties { get; set; } = new();

        /// <summary>
        /// 索引是否唯一
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// 索引创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
