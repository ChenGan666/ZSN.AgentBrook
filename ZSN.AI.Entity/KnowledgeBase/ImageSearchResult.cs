using System.Collections.Generic;

namespace ZSN.AI.Entity.KnowledgeBase
{
    public class ImageSearchResult
    {
        public string ImageId { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string StorageType { get; set; } = "file";
        public string? MimeType { get; set; }
        public string? Description { get; set; }
        public string? OcrText { get; set; }
        public int? PageNumber { get; set; }
        public float Similarity { get; set; }
        public string MatchType { get; set; } = "text";
        public string? ImageUrl { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class HybridSearchResultWithImages : HybridSearchResult
    {
        public List<ImageSearchResult> ImageResults { get; set; } = new();
        public Dictionary<string, List<ImageSearchResult>> ChunkImages { get; set; } = new();
    }
}
