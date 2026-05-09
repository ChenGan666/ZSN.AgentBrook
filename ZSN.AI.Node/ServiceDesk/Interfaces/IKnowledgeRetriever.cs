using ZSN.AI.Entity;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.Node.ServiceDesk.Models;

namespace ZSN.AI.Node.ServiceDesk.Interfaces
{
    /// <summary>知识检索器接口</summary>
    public interface IKnowledgeRetriever
    {
        /// <summary>
        /// 从多个知识库检索相关知识（文件级检索，与 KnowledgeBase 节点逻辑一致）
        /// </summary>
        Task<KnowledgeRetrievalResult> RetrieveKnowledgeAsync(
            string query,
            List<KnowledgeBaseInfo> knowledgeBases,
            HybridSearchOptions searchOptions,
            MemoryContext memoryContext,
            ServiceDeskData config);
    }
}
