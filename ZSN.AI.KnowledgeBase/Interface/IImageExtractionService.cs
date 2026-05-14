using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Interface
{
    public interface IImageExtractionService
    {
        Task<List<ExtractedImage>> ExtractFromDocumentAsync(
            string filePath, ImageExtractionOptions? options = null,
            CancellationToken cancellationToken = default);

        Task<ExtractedImage> ProcessStandaloneImageAsync(
            string filePath, CancellationToken cancellationToken = default);
    }
}
