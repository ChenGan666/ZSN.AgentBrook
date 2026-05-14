using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Interface
{
    public interface IImageRepository
    {
        Task SaveImageInfosAsync(List<DocumentImageInfo> images, CancellationToken ct = default);
        Task SaveChunkImageRelationsAsync(List<ChunkImageRelation> relations, CancellationToken ct = default);
        Task<List<string>> GetExistingHashesAsync(List<string> hashes, CancellationToken ct = default);
        Task<List<DocumentImageInfo>> GetByDocumentIdAsync(string documentId, CancellationToken ct = default);
        Task<Dictionary<string, List<ImageSearchResult>>> GetImagesByChunkIdsAsync(List<string> chunkIds, CancellationToken ct = default);
        Task DeleteByDocumentIdAsync(string documentId, CancellationToken ct = default);
        Task<List<DocumentImageInfo>> GetFailedDescriptionsAsync(string? documentId = null, CancellationToken ct = default);
        Task UpdateDescriptionAsync(string imageId, string description, string? ocrText, string status, CancellationToken ct = default);
        Task<DocumentImageInfo?> GetByImageIdAsync(string imageId, CancellationToken ct = default);
    }
}
