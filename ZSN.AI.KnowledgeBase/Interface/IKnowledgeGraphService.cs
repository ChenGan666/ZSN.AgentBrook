using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.Core.Interface
{
    /// <summary>
    /// 知识图谱服务接口
    /// </summary>
    public interface IKnowledgeGraphService
    {
        /// <summary>
        /// 从文档构建知识图谱
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="options">构建选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>图名称</returns>
        Task<string> BuildGraphFromDocumentAsync(
            string documentId,
            string knowledgeBaseId,
            GraphBuildOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 实体识别
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="config">抽取配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体列表</returns>
        Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> ExtractEntitiesAsync(
            string text,
            EntityExtractionConfig config,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量实体识别
        /// </summary>
        /// <param name="textChunks">文本块列表</param>
        /// <param name="config">抽取配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体列表</returns>
        Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> ExtractEntitiesBatchAsync(
            List<string> textChunks,
            EntityExtractionConfig config,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 关系抽取
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="entities">实体列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>关系列表</returns>
        Task<List<Relation>> ExtractRelationsAsync(
            string text,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量关系抽取
        /// </summary>
        /// <param name="chunks">文本块和实体对列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>关系列表</returns>
        Task<List<Relation>> ExtractRelationsBatchAsync(
            List<(string text, List<ZSN.AI.Entity.KnowledgeBase.Entity> entities)> chunks,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 图谱查询（Cypher）
        /// </summary>
        /// <param name="cypherQuery">Cypher查询语句</param>
        /// <param name="options">查询选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>查询结果</returns>
        Task<GraphQueryResult> QueryAsync(
            string cypherQuery,
            GraphQueryOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 通过实体文本查找图数据库中的节点ID
        /// </summary>
        /// <param name="entityText">实体文本</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体ID，未找到返回null</returns>
        Task<string?> FindEntityIdByTextAsync(
            string entityText,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 多跳查询
        /// </summary>
        /// <param name="startEntityId">起始实体ID</param>
        /// <param name="relationTypes">关系类型列表</param>
        /// <param name="maxHops">最大跳数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>图谱路径列表</returns>
        Task<List<GraphPath>> MultiHopQueryAsync(
            string startEntityId,
            List<string> relationTypes,
            int maxHops,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 实体去重和合并
        /// </summary>
        /// <param name="entities">实体列表</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>去重后的实体列表</returns>
        Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> DeduplicateEntitiesAsync(
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            float similarityThreshold = 0.85f,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取图谱统计信息
        /// </summary>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>统计信息</returns>
        Task<GraphStatistics> GetStatisticsAsync(
            string knowledgeBaseId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定文档的图谱统计信息
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>统计信息</returns>
        Task<GraphStatistics> GetDocumentStatisticsAsync(
            string documentId,
            string knowledgeBaseId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存实体到图数据库
        /// </summary>
        /// <param name="entities">实体列表</param>
        /// <param name="sourceChunkId">来源块ID</param>
        /// <param name="sourceDocumentId">来源文档ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SaveEntitiesAsync(
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            string? sourceChunkId = null,
            string? sourceDocumentId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存关系到图数据库
        /// </summary>
        /// <param name="relations">关系列表</param>
        /// <param name="sourceDocumentId">来源文档ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SaveRelationsAsync(
            List<Relation> relations,
            string? sourceDocumentId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理没有 source_document_id 的旧数据
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除的数据条数</returns>
        Task<int> CleanupOldDataWithoutDocumentIdAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定文档的图谱可视化数据
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>图谱可视化数据（节点和关系）</returns>
        Task<GraphVisualizationData> GetDocumentGraphDataAsync(
            string documentId,
            string knowledgeBaseId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除指定文档的所有实体和关系
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除的实体和关系数量</returns>
        Task<(int entityCount, int relationCount)> DeleteDocumentGraphAsync(
            string documentId,
            string knowledgeBaseId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除指定知识库的所有图谱数据
        /// </summary>
        /// <param name="knowledgeBaseId">知识库ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除的实体和关系数量</returns>
        Task<(int entityCount, int relationCount)> DeleteKnowledgeBaseGraphAsync(
            string knowledgeBaseId,
            CancellationToken cancellationToken = default);
    }


}
