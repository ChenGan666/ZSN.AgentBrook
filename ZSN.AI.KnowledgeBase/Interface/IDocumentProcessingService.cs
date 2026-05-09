using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.Core.Interface
{
    /// <summary>
    /// 文档处理服务接口
    /// </summary>
    public interface IDocumentProcessingService
    {
        /// <summary>
        /// 处理文档（完整流程：上传、解析、分块、存储、同步数据库）
        /// </summary>
        /// <param name="request">文档处理请求</param>
        /// <param name="progress">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文档处理结果</returns>
        Task<DocumentProcessingResult> ProcessDocumentAsync(
            DocumentProcessingRequest request,
            IProgress<DocumentProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 从文件路径处理文档
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="options">处理选项</param>
        /// <param name="progress">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文档处理结果</returns>
        Task<DocumentProcessingResult> ProcessDocumentFromFileAsync(
            string documentId,
            string filePath,
            string knowledgeBaseId,
            DocumentProcessingOptions? options = null,
            IProgress<DocumentProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 从文本内容处理文档
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="fileName">文件名</param>
        /// <param name="content">文本内容</param>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="options">处理选项</param>
        /// <param name="progress">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文档处理结果</returns>
        Task<DocumentProcessingResult> ProcessDocumentFromTextAsync(
            string documentId,
            string fileName,
            string content,
            string knowledgeBaseId,
            DocumentProcessingOptions? options = null,
            IProgress<DocumentProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量处理文档
        /// </summary>
        /// <param name="requests">文档处理请求列表</param>
        /// <param name="progress">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文档处理结果列表</returns>
        Task<List<DocumentProcessingResult>> ProcessDocumentsAsync(
            List<DocumentProcessingRequest> requests,
            IProgress<DocumentProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取文档处理状态
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文档处理状态</returns>
        Task<DocumentProcessingStatus?> GetProcessingStatusAsync(
            string documentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取文档存储路径
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <returns>文档存储路径</returns>
        string GetDocumentStoragePath(string documentId);

        /// <summary>
        /// 通过MD5查找文档
        /// </summary>
        /// <param name="md5">文档MD5</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文档信息</returns>
        Task<DocumentInfo?> GetDocumentByMd5Async(
            string md5,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除文档（包括文件和数据库记录）
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除结果</returns>
        Task<DocumentDeletionResult> DeleteDocumentAsync(
            string documentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量删除文档
        /// </summary>
        /// <param name="documentIds">文档ID列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除结果列表</returns>
        Task<List<DocumentDeletionResult>> DeleteDocumentsAsync(
            List<string> documentIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除知识库的所有数据
        /// </summary>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除结果</returns>
        Task<KnowledgeBaseDeletionResult> DeleteKnowledgeBaseAsync(
            string knowledgeBaseId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据文件ID删除文档
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除结果</returns>
        Task<DocumentDeletionResult> DeleteByFileIdAsync(
            string fileId,
            CancellationToken cancellationToken = default);
    }


}
