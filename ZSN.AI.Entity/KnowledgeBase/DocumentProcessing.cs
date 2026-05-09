using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Entity.KnowledgeBase
{
    /// <summary>
    /// 文档处理请求
    /// </summary>
    public class DocumentProcessingRequest
    {
        /// <summary>
        /// 文档ID（可选，不提供则自动生成）
        /// </summary>
        public string? DocumentId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 文本内容（如果不提供文件路径）
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// 知识库ID
        /// </summary>
        public string KnowledgeBaseId { get; set; } = string.Empty;

        /// <summary>
        /// 处理选项
        /// </summary>
        public DocumentProcessingOptions? Options { get; set; }

        /// <summary>
        /// 元数据
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// 文档处理选项
    /// </summary>
    public class DocumentProcessingOptions
    {
        /// <summary>
        /// 分块策略
        /// </summary>
        public ChunkingStrategy ChunkingStrategy { get; set; } = ChunkingStrategy.SemanticBoundary;

        /// <summary>
        /// 最大块大小（tokens）
        /// </summary>
        public int MaxChunkSize { get; set; } = 500;

        /// <summary>
        /// 块重叠大小（tokens）
        /// </summary>
        public int ChunkOverlap { get; set; } = 50;

        /// <summary>
        /// 是否提取实体
        /// </summary>
        public bool ExtractEntities { get; set; } = true;

        /// <summary>
        /// 是否提取关系
        /// </summary>
        public bool ExtractRelations { get; set; } = true;

        /// <summary>
        /// 是否向量化
        /// </summary>
        public bool EnableEmbedding { get; set; } = true;

        /// <summary>
        /// 实体提取模型ID
        /// </summary>
        public int? EntityModelId { get; set; }

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;
    }

    /// <summary>
    /// 文档处理结果
    /// </summary>
    public class DocumentProcessingResult
    {
        /// <summary>
        /// 文档ID
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 处理状态
        /// </summary>
        public ProcessingStatus Status { get; set; }

        /// <summary>
        /// 分块数量
        /// </summary>
        public int ChunkCount { get; set; }

        /// <summary>
        /// 总Token数
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// 实体数量
        /// </summary>
        public int EntityCount { get; set; }

        /// <summary>
        /// 关系数量
        /// </summary>
        public int RelationCount { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 处理开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 处理结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 处理耗时
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// 分块列表
        /// </summary>
        public List<DocumentChunk> Chunks { get; set; } = new();

        /// <summary>
        /// 实体列表
        /// </summary>
        public List<Entity> Entities { get; set; } = new();

        /// <summary>
        /// 关系列表
        /// </summary>
        public List<Relation> Relations { get; set; } = new();
    }

    /// <summary>
    /// 文档处理状态
    /// </summary>
    public class DocumentProcessingStatus
    {
        /// <summary>
        /// 文档ID
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>
        /// 状态
        /// </summary>
        public ProcessingStatus Status { get; set; }

        /// <summary>
        /// 进度（0-100）
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// 当前阶段
        /// </summary>
        public string CurrentStage { get; set; } = string.Empty;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 处理状态枚举
    /// </summary>
    public enum ProcessingStatus
    {
        /// <summary>
        /// 等待中
        /// </summary>
        Pending,

        /// <summary>
        /// 解析中
        /// </summary>
        Parsing,

        /// <summary>
        /// 分块中
        /// </summary>
        Chunking,

        /// <summary>
        /// 向量化中
        /// </summary>
        Embedding,

        /// <summary>
        /// 提取实体中
        /// </summary>
        ExtractingEntities,

        /// <summary>
        /// 提取关系中
        /// </summary>
        ExtractingRelations,

        /// <summary>
        /// 保存中
        /// </summary>
        Saving,

        /// <summary>
        /// 完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// 文档处理进度
    /// </summary>
    public class DocumentProcessingProgress
    {
        /// <summary>
        /// 文档ID
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>
        /// 当前阶段
        /// </summary>
        public string Stage { get; set; } = string.Empty;

        /// <summary>
        /// 进度百分比（0-100）
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 当前处理的项目数
        /// </summary>
        public int CurrentItem { get; set; }

        /// <summary>
        /// 总项目数
        /// </summary>
        public int TotalItems { get; set; }
    }

    /// <summary>
    /// 文档块
    /// </summary>
    public class DocumentChunk
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
        /// 块序号
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Token数
        /// </summary>
        public int TokenCount { get; set; }

        /// <summary>
        /// 起始位置
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// 结束位置
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// 向量
        /// </summary>
        public float[]? Embedding { get; set; }

        /// <summary>
        /// 元数据
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// 文档信息
    /// </summary>
    public class DocumentInfo
    {
        /// <summary>
        /// 文档ID
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件MD5
        /// </summary>
        public string FileMd5 { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 知识库ID
        /// </summary>
        public string KnowledgeBaseId { get; set; } = string.Empty;

        /// <summary>
        /// 存储路径
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 处理状态
        /// </summary>
        public ProcessingStatus Status { get; set; }

        /// <summary>
        /// 分块数量
        /// </summary>
        public int ChunkCount { get; set; }

        /// <summary>
        /// 总Token数
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// 元数据
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// 文档删除结果
    /// </summary>
    public class DocumentDeletionResult
    {
        /// <summary>
        /// 文档ID
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 删除的向量数量
        /// </summary>
        public int DeletedVectors { get; set; }

        /// <summary>
        /// 删除的实体数量
        /// </summary>
        public int DeletedEntities { get; set; }

        /// <summary>
        /// 删除的关系数量
        /// </summary>
        public int DeletedRelations { get; set; }

        /// <summary>
        /// 文件是否被删除
        /// </summary>
        public bool FileDeleted { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 删除时间
        /// </summary>
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 知识库删除结果
    /// </summary>
    public class KnowledgeBaseDeletionResult
    {
        /// <summary>
        /// 知识库ID
        /// </summary>
        public string KnowledgeBaseId { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 删除的文档数量
        /// </summary>
        public int DeletedDocuments { get; set; }

        /// <summary>
        /// 删除的向量总数
        /// </summary>
        public int DeletedVectors { get; set; }

        /// <summary>
        /// 删除的实体总数
        /// </summary>
        public int DeletedEntities { get; set; }

        /// <summary>
        /// 删除的关系总数
        /// </summary>
        public int DeletedRelations { get; set; }

        /// <summary>
        /// 删除的文件总数
        /// </summary>
        public int DeletedFiles { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 删除时间
        /// </summary>
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    }
}
