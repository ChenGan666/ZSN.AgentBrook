using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Interface
{
    public interface IImageStorageService
    {
        Task<ImageStorageInfo> SaveAsync(string documentId, ExtractedImage image, CancellationToken ct = default);
        Task<string?> GetUrlAsync(string storagePath, CancellationToken ct = default);
        Task<byte[]?> GetDataAsync(string storagePath, CancellationToken ct = default);
        Task DeleteByDocumentAsync(string documentId, CancellationToken ct = default);
    }
}
