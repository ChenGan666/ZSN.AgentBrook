using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Interface
{
    /// <summary>
    /// 向量存储库接口
    /// </summary>
    public interface IVectorRepository
    {
        /// <summary>
        /// 初始化向量表和索引
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存文档块向量
        /// </summary>
        Task SaveDocumentChunkAsync(DocumentChunkVector chunk, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量保存文档块向量
        /// </summary>
        Task SaveDocumentChunksAsync(IEnumerable<DocumentChunkVector> chunks, CancellationToken cancellationToken = default);

        /// <summary>
        /// 向量相似度搜索（文档块）
        /// </summary>
        Task<List<VectorSearchResult>> SearchDocumentChunksAsync(
            float[] queryEmbedding,
            int topK = 10,
            string? documentId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存实体向量
        /// </summary>
        Task SaveEntityEmbeddingAsync(EntityEmbedding entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量保存实体向量
        /// </summary>
        Task SaveEntityEmbeddingsAsync(IEnumerable<EntityEmbedding> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// 向量相似度搜索（实体）
        /// </summary>
        Task<List<VectorSearchResult>> SearchEntityEmbeddingsAsync(
            float[] queryEmbedding,
            int topK = 10,
            string? entityType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除文档的所有向量
        /// </summary>
        Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除实体向量
        /// </summary>
        Task DeleteEntityAsync(string entityId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量删除文档的所有向量
        /// </summary>
        Task DeleteDocumentsAsync(IEnumerable<string> documentIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量删除实体向量
        /// </summary>
        Task DeleteEntitiesAsync(IEnumerable<string> entityIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除知识库的所有文档向量
        /// </summary>
        Task DeleteKnowledgeBaseAsync(string knowledgeBaseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取文档块统计信息
        /// </summary>
        Task<int> GetDocumentChunkCountAsync(string? documentId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取实体向量统计信息
        /// </summary>
        Task<int> GetEntityEmbeddingCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据分块ID列表获取文档块
        /// </summary>
        /// <param name="chunkIds">分块ID列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文档块列表</returns>
        Task<List<VectorSearchResult>> GetDocumentChunksByIdsAsync(
            List<string> chunkIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 分页获取文档的所有分块
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="skip">跳过的记录数</param>
        /// <param name="take">获取的记录数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文档块列表</returns>
        Task<(List<VectorSearchResult> Chunks, int TotalCount)> GetDocumentChunksAsync(
            string documentId,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
    }
}
