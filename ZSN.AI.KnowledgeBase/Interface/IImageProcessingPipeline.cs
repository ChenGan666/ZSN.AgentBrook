using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Interface
{
    public interface IImageProcessingPipeline
    {
        Task<ImageProcessingResult> ProcessAsync(
            string documentId, string filePath,
            List<DocumentChunk> existingChunks,
            ImageProcessingOptions? options = null,
            IProgress<DocumentProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default);

        Task<int> RegenerateDescriptionsAsync(
            string documentId, int? visionModelId = null,
            CancellationToken cancellationToken = default);
    }
}
