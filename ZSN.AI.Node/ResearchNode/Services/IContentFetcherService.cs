using ZSN.AI.Node.ResearchNode.Models;

namespace ZSN.AI.Node.ResearchNode.Services
{
    public interface IContentFetcherService
    {
        Task<List<WebPageContent>> FetchAsync(
            List<string> urls,
            int maxContentLength = 5000,
            IProgress<string> progress = null,
            CancellationToken ct = default);

        Task<bool> IsPlaywrightAvailableAsync();
    }
}
