using ZSN.AI.Node.ResearchNode.Models;

namespace ZSN.AI.Node.ResearchNode.Services
{
    public interface IContentCache
    {
        Task<WebPageContent> GetAsync(string url);
        Task SetAsync(string url, WebPageContent content, TimeSpan? expiry = null);
    }
}
