using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.Core.Interface
{
    /// <summary>
    /// 混合检索服务接口
    /// </summary>
    public interface IHybridSearchService
    {
        /// <summary>
        /// 混合检索（向量+图谱）
        /// </summary>
        /// <param name="query">查询文本</param>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="options">检索选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>混合检索结果</returns>
        Task<HybridSearchResult> SearchAsync(
            string query,
            string knowledgeBaseId,
            HybridSearchOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 向量检索
        /// </summary>
        /// <param name="query">查询文本</param>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="topK">返回前K个结果</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>检索结果列表</returns>
        Task<List<SearchResult>> VectorSearchAsync(
            string query,
            string knowledgeBaseId,
            int topK,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 图谱检索
        /// </summary>
        /// <param name="query">查询文本</param>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="options">图谱检索选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>图谱检索结果列表</returns>
        Task<List<GraphSearchResult>> GraphSearchAsync(
            string query,
            string knowledgeBaseId,
            GraphSearchOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 图谱增强的向量检索
        /// </summary>
        /// <param name="query">查询文本</param>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="options">图谱增强检索选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>检索结果列表</returns>
        Task<List<SearchResult>> GraphEnhancedSearchAsync(
            string query,
            string knowledgeBaseId,
            GraphEnhancedSearchOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 结果融合
        /// </summary>
        /// <param name="options">检索选项</param>
        /// <param name="vectorResults">向量检索结果</param>
        /// <param name="graphResults">图谱检索结果</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>融合后的结果列表</returns>
        Task<List<SearchResult>> FuseResultsAsync(
            HybridSearchOptions options,
            List<SearchResult> vectorResults,
            List<GraphSearchResult> graphResults,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 重排序
        /// </summary>
        /// <param name="query">查询文本</param>
        /// <param name="results">待重排序的结果</param>
        /// <param name="options">重排序选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重排序后的结果列表</returns>
        Task<List<SearchResult>> RerankAsync(
            string query,
            List<SearchResult> results,
            RerankOptions options,
            CancellationToken cancellationToken = default);
    }

    
}
