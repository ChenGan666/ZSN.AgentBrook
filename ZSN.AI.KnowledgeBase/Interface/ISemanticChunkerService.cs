using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.Core.Interface
{
    /// <summary>
    /// 语义感知分块服务接口
    /// </summary>
    public interface ISemanticChunkerService
    {
        /// <summary>
        /// 语义感知分块
        /// </summary>
        /// <param name="content">待分块内容</param>
        /// <param name="strategy">分块策略</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>语义块列表</returns>
        Task<List<SemanticChunk>> ChunkAsync(
            string content,
            ChunkingStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 大文档流式分块
        /// </summary>
        /// <param name="contentStream">内容流</param>
        /// <param name="strategy">分块策略</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>语义块异步枚举</returns>
        IAsyncEnumerable<SemanticChunk> ChunkStreamAsync(
            Stream contentStream,
            ChunkingStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取分块统计信息
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分块统计信息</returns>
        Task<ChunkingStatistics> GetStatisticsAsync(
            string documentId,
            CancellationToken cancellationToken = default);
    }

    
}
