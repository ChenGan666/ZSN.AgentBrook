using ZSN.AI.Node.ResearchNode.Models;

namespace ZSN.AI.Node.ResearchNode.Services
{
    public interface IWebSearchService
    {
        Task<List<SearchResultItem>> SearchAsync(
            SearchPlan plan,
            HashSet<string> excludeUrls,
            CancellationToken ct = default);

        List<SearchResultItem> RankByRelevance(
            List<SearchResultItem> results,
            string researchGoal,
            int maxCount);
    }
}
