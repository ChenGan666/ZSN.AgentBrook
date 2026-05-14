using System;
using System.Collections.Generic;

namespace ZSN.AI.Entity.KnowledgeBase
{
    public class DocumentImageInfo
    {
        public long Id { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public string ImageId { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public int SequenceNumber { get; set; }
        public string? OriginalFilename { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public string StorageType { get; set; } = "file";
        public string? MimeType { get; set; }
        public long? FileSize { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? ContentHash { get; set; }
        public string? Description { get; set; }
        public string? OcrText { get; set; }
        public string DescriptionStatus { get; set; } = "pending";
        public bool IsDecorative { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ChunkImageRelation
    {
        public long Id { get; set; }
        public string ChunkId { get; set; } = string.Empty;
        public string ImageId { get; set; } = string.Empty;
        public string RelationType { get; set; } = "nearby";
    }
}
