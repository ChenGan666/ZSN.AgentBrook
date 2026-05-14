using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Interface
{
    public interface IImageDescriptionService
    {
        Task<ImageDescription> DescribeAsync(
            byte[] imageData, string? mimeType = null,
            string? context = null, int? visionModelId = null,
            CancellationToken cancellationToken = default);
    }
}
