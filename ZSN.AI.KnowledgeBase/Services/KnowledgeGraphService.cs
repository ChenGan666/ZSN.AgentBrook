using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Models;
using ZSN.AI.Entity;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 知识图谱服务实现
    /// </summary>
    public class KnowledgeGraphService : IKnowledgeGraphService
    {
        private readonly IGraphRepository _graphRepository;
        private readonly IChatService? _chatService;
        private readonly IEmbeddingService? _embeddingService;
        private readonly IConfiguration? _configuration;
        private readonly ILogger<KnowledgeGraphService> _logger;
        private readonly string _graphName;
        private readonly EntityDeduplicator _entityDeduplicator;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CrossChunkEntityLinker _crossChunkLinker;
        private readonly ExtractionCache _extractionCache;
        private readonly KnowledgeBaseEntityLinker _knowledgeBaseLinker;
        private readonly DynamicEntityTypeManager _typeManager;

        /// <summary>
        /// 构造函数
        /// </summary>
        public KnowledgeGraphService(
            IGraphRepository graphRepository,
            IChatService? chatService,
            IEmbeddingService? embeddingService,
            IConfiguration? configuration,
            ILogger<KnowledgeGraphService> logger,
            ILoggerFactory loggerFactory)
        {
            _graphRepository = graphRepository;
            _chatService = chatService;
            _embeddingService = embeddingService;
            _configuration = configuration;
            _logger = logger;
            _loggerFactory = loggerFactory;

            // 从配置中获取图名称
            _graphName = configuration?["DbConnectionStrings:KnowledgeBaseDb:GraphName"] ?? "knowledge_graph";

            // 初始化实体去重器
            _entityDeduplicator = new EntityDeduplicator(
                _embeddingService,
                _loggerFactory.CreateLogger<EntityDeduplicator>());

            // 初始化跨分块实体链接器
            _crossChunkLinker = new CrossChunkEntityLinker(
                _chatService,
                _embeddingService,
                _loggerFactory.CreateLogger<CrossChunkEntityLinker>());

            // 初始化提取缓存
            _extractionCache = new ExtractionCache(
                new ExtractionCacheConfig
                {
                    EnableCache = true,
                    CacheExpirationMinutes = 60,
                    MaxCacheSize = 1000
                },
                _loggerFactory.CreateLogger<ExtractionCache>());

            // 初始化知识库实体链接器
            _knowledgeBaseLinker = new KnowledgeBaseEntityLinker(
                _graphRepository,
                _embeddingService,
                _chatService,
                _loggerFactory.CreateLogger<KnowledgeBaseEntityLinker>(),
                _graphName);

            // 初始化动态类型管理器
            var typeMappingConfigPath = configuration?["KnowledgeGraph:TypeMappingConfigPath"]
                ?? "Config/entity_type_mapping.json";
            _typeManager = new DynamicEntityTypeManager(
                _loggerFactory.CreateLogger<DynamicEntityTypeManager>(),
                typeMappingConfigPath);

            // 输出配置摘要
            //_logger.LogInformation(_typeManager.GetConfigSummary());
        }

        #region 实体识别

        /// <summary>
        /// 从文档构建知识图谱（P2优化版：支持批处理、缓存和知识库链接）
        /// </summary>
        public async Task<string> BuildGraphFromDocumentAsync(
            string documentId,
            string knowledgeBaseId,
            GraphBuildOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("开始为文档 {DocumentId} 构建知识图谱（P2优化模式）", documentId);

            try
            {
                // 确保图存在
                if (!await _graphRepository.GraphExistsAsync(_graphName, cancellationToken))
                {
                    await _graphRepository.CreateGraphAsync(_graphName, cancellationToken);
                    _logger.LogInformation("创建图: {GraphName}", _graphName);
                }

                // 清空跨分块链接器的实体池
                _crossChunkLinker.ClearPool();

                // TODO: 从数据库获取文档的分块内容
                // 这里需要实现从数据库读取分块
                var chunks = await GetDocumentChunksAsync(documentId, cancellationToken);

                var allEntities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
                var allRelations = new List<ZSN.AI.Entity.KnowledgeBase.Relation>();

                // P2优化：启用批处理（默认批大小为3）
                var batchConfig = new BatchExtractionConfig
                {
                    EnableBatching = true,
                    BatchSize = 3,
                    MaxTokensPerRequest = 12000
                };

                // 批量实体提取（减少API调用）
                var extractionConfig = new EntityExtractionConfig
                {
                    ModelId = GetChatModelId(),
                    MinConfidence = 0.7f
                };

                // 使用批量提取器处理实体
                var batchExtractor = new BatchExtractor(_chatService, _loggerFactory.CreateLogger<BatchExtractor>());
                var entityResults = await batchExtractor.ExtractEntitiesBatchAsync(
                    chunks,
                    extractionConfig,
                    batchConfig,
                    cancellationToken);

                // 展平结果并使用跨分块链接器
                foreach (var (entities, index) in entityResults.Select((e, i) => (e, i)))
                {
                    var chunkId = chunks[index].ChunkId;

                    // 使用跨分块链接器维护一致性
                    var linkedEntities = await _crossChunkLinker.LinkEntitiesAsync(
                        entities,
                        chunkId,
                        useLLMForLinking: false,
                        cancellationToken);

                    allEntities.AddRange(linkedEntities);
                    _logger.LogInformation("分块 {ChunkId} 识别出 {Count} 个实体", chunkId, linkedEntities.Count);
                }

                // P2优化：链接到知识库已有实体
                allEntities = await _knowledgeBaseLinker.LinkToKnowledgeBaseAsync(
                    allEntities,
                    knowledgeBaseId,
                    useLLMForLinking: false,
                    similarityThreshold: 0.90f,
                    cancellationToken);

                _logger.LogInformation("知识库实体链接完成，总实体数: {Count}", allEntities.Count);

                // 关系抽取（逐个处理以保持准确性）
                foreach (var chunk in chunks)
                {
                    // 获取该文本块的实体
                    var chunkEntities = allEntities
                        .Where(e => e.SourceChunkIds.Contains(chunk.ChunkId))
                        .ToList();

                    if (options.ExtractRelations && chunkEntities.Count > 0)
                    {
                        var relations = await ExtractRelationsAsync(chunk.Content, chunkEntities, cancellationToken);

                        // 标记来源分块
                        foreach (var relation in relations)
                        {
                            relation.SourceChunkIds.Add(chunk.ChunkId);
                        }

                        allRelations.AddRange(relations);
                        _logger.LogInformation("分块 {ChunkId} 抽取到 {Count} 个关系", chunk.ChunkId, relations.Count);
                    }
                }

                // 实体去重（在跨分块链接和知识库链接后进一步去重）
                if (options.EnableDeduplication)
                {
                    allEntities = await DeduplicateEntitiesAsync(allEntities, 0.85f, cancellationToken);
                    _logger.LogInformation("去重后剩余 {Count} 个实体", allEntities.Count);
                }

                // 存储实体和关系到图数据库
                await StoreEntitiesAsync(allEntities, cancellationToken);
                await StoreRelationsAsync(allRelations, cancellationToken);

                // 实体向量化
                if (options.VectorizeEntities)
                {
                    await VectorizeEntitiesAsync(allEntities, cancellationToken);
                }

                // 输出统计信息
                var poolStats = _crossChunkLinker.GetPoolStatistics();
                var cacheStats = _extractionCache.GetStatistics();

                _logger.LogInformation("全局实体池统计: {PoolStats}", string.Join(", ", poolStats.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                _logger.LogInformation("缓存统计: 启用={Enabled}, 实体缓存={EntityCache}, 关系缓存={RelationCache}, 总命中次数={TotalHits}",
                    cacheStats.GetValueOrDefault("enabled", false),
                    cacheStats.GetValueOrDefault("entity_cache_count", 0),
                    cacheStats.GetValueOrDefault("relation_cache_count", 0),
                    cacheStats.GetValueOrDefault("total_hits", 0));

                _logger.LogInformation("文档 {DocumentId} 知识图谱构建完成（P2优化），实体数: {EntityCount}，关系数: {RelationCount}",
                    documentId, allEntities.Count, allRelations.Count);

                return _graphName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建知识图谱失败: {Message}", ex.Message);
                throw;
            }
            finally
            {
                // 清理全局实体池
                _crossChunkLinker.ClearPool();
            }
        }

        /// <summary>
        /// 实体识别（带缓存支持）
        /// </summary>
        public async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> ExtractEntitiesAsync(
            string text,
            EntityExtractionConfig config,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<ZSN.AI.Entity.KnowledgeBase.Entity>();

            _logger.LogDebug("开始实体识别，文本长度: {Length}", text.Length);

            try
            {
                // 尝试从缓存获取
                if (_extractionCache.TryGetEntities(text, config, out var cachedEntities))
                {
                    _logger.LogInformation("实体识别缓存命中，返回 {Count} 个实体", cachedEntities.Count);
                    return cachedEntities;
                }

                // 使用LLM进行实体识别
                List<ZSN.AI.Entity.KnowledgeBase.Entity> entities;
                if (_chatService != null)
                {
                    entities = await CallLLMForEntityExtraction(text, config, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("ChatService 未注册，使用空结果");
                    entities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
                }

                // 缓存结果
                if (entities.Count > 0)
                {
                    _extractionCache.SetEntities(text, config, entities);
                    _logger.LogDebug("实体识别结果已缓存");
                }

                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "实体识别失败: {Message}", ex.Message);
                return new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
            }
        }

        /// <summary>
        /// 批量实体识别
        /// </summary>
        public async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> ExtractEntitiesBatchAsync(
            List<string> textChunks,
            EntityExtractionConfig config,
            CancellationToken cancellationToken = default)
        {
            var allEntities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();

            foreach (var chunk in textChunks)
            {
                var entities = await ExtractEntitiesAsync(chunk, config, cancellationToken);
                allEntities.AddRange(entities);
            }

            return allEntities;
        }

        /// <summary>
        /// 使用LLM进行实体识别（带重试和验证）
        /// </summary>
        private async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> CallLLMForEntityExtraction(
            string text,
            EntityExtractionConfig config,
            CancellationToken cancellationToken)
        {
            if (_chatService == null || _configuration == null)
                return new List<ZSN.AI.Entity.KnowledgeBase.Entity>();

            // 获取模型ID
            var modelId = config.ModelId > 0 ? config.ModelId : GetChatModelId();

            // 从数据库获取模型信息
            var modelInfo = ZSN.AI.BLL.LargeModelInfoBussiness.GetModel(modelId);

            if (modelInfo == null)
            {
                _logger.LogWarning("无法获取模型信息，ModelID: {ModelID}", modelId);
                return new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
            }

            // 构建Prompt（使用优化的PromptBuilder）
            var prompt = EntityPromptBuilder.BuildEntityExtractionPrompt(text, config);

            // 使用重试机制调用LLM
            var retryResult = await RetryHelper.ExecuteWithRetryAsync(
                async (attempt, temperatureOffset) =>
                {
                    // 计算当前temperature（基础温度 + 偏移）
                    var currentTemperature = 0.2 + temperatureOffset;

                    // 构建模型配置
                    var modelConfig = new LargeModelConfig
                    {
                        Id = modelId.ToString(),
                        Model = modelInfo,
                        Temperature = (int)(currentTemperature * 100),  // 转换为整数格式
                        ResponseFormat = "json_object",
                        AnswerTokens = 4096,
                        Prompt = prompt
                    };

                    // 构建聊天历史
                    var history = new ChatHistory();
                    history.AddUserMessage(modelConfig.Prompt ?? string.Empty);

                    // 调用LLM API
                    var responseBuilder = new StringBuilder();
                    await foreach (var response in _chatService.SendChatAsync(
                        modelConfig,
                        history,
                        responseFormat: "json_object",
                        ct: cancellationToken))
                    {
                        responseBuilder.Append(response);
                    }

                    return responseBuilder.ToString();
                },
                RetryHelper.CreateEntityExtractionRetryOptions(),
                _logger,
                "实体提取",
                cancellationToken);

            // 检查是否成功
            if (!retryResult.IsSuccess || string.IsNullOrWhiteSpace(retryResult.Data))
            {
                _logger.LogError("实体提取失败: {Error}", retryResult.ErrorMessage);
                return new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
            }

            // 使用OutputValidator验证和修复响应
            var (entities, isValid, errorMessage) = OutputValidator.ValidateAndRepairEntityResponse(
                retryResult.Data,
                config.MinConfidence,
                text,
                _logger);

            if (!isValid)
            {
                _logger.LogWarning("实体提取响应验证失败: {Error}", errorMessage);
            }

            return entities;
        }

        #endregion

        #region 关系抽取

        /// <summary>
        /// 关系抽取
        /// </summary>
        public async Task<List<Relation>> ExtractRelationsAsync(
            string text,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text) || entities.Count == 0)
                return new List<Relation>();

            _logger.LogDebug("开始关系抽取，实体数: {Count}", entities.Count);

            try
            {
                // 使用LLM进行关系抽取
                if (_chatService != null)
                {
                    return await CallLLMForRelationExtraction(text, entities, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("ChatService 未注册，使用空结果");
                    return new List<Relation>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关系抽取失败: {Message}", ex.Message);
                return new List<Relation>();
            }
        }

        /// <summary>
        /// 批量关系抽取
        /// </summary>
        public async Task<List<Relation>> ExtractRelationsBatchAsync(
            List<(string text, List<ZSN.AI.Entity.KnowledgeBase.Entity> entities)> chunks,
            CancellationToken cancellationToken = default)
        {
            var allRelations = new List<Relation>();

            foreach (var (text, entities) in chunks)
            {
                var relations = await ExtractRelationsAsync(text, entities, cancellationToken);
                allRelations.AddRange(relations);
            }

            return allRelations;
        }

        /// <summary>
        /// 使用LLM进行关系抽取（带重试和验证）
        /// </summary>
        private async Task<List<Relation>> CallLLMForRelationExtraction(
            string text,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            CancellationToken cancellationToken)
        {
            if (_chatService == null || _configuration == null)
                return new List<Relation>();

            // 获取模型ID
            var modelId = GetChatModelId();

            // 从数据库获取模型信息
            var modelInfo = ZSN.AI.BLL.LargeModelInfoBussiness.GetModel(modelId);

            if (modelInfo == null)
            {
                _logger.LogWarning("无法获取模型信息，ModelID: {ModelID}", modelId);
                return new List<Relation>();
            }

            // 构建Prompt（使用优化的PromptBuilder）
            var prompt = EntityPromptBuilder.BuildRelationExtractionPrompt(text, entities);

            // 使用重试机制调用LLM
            var retryResult = await RetryHelper.ExecuteWithRetryAsync(
                async (attempt, temperatureOffset) =>
                {
                    // 计算当前temperature（基础温度 + 偏移）
                    var currentTemperature = 0.3 + temperatureOffset;

                    // 构建模型配置
                    var modelConfig = new LargeModelConfig
                    {
                        Id = modelId.ToString(),
                        Model = modelInfo,
                        Temperature = (int)(currentTemperature * 100),  // 转换为整数格式
                        ResponseFormat = "json_object",
                        AnswerTokens = 4096,
                        Prompt = prompt
                    };

                    // 构建聊天历史
                    var history = new ChatHistory();
                    history.AddUserMessage(modelConfig.Prompt ?? string.Empty);

                    // 调用LLM API
                    var responseBuilder = new StringBuilder();
                    await foreach (var response in _chatService.SendChatAsync(
                        modelConfig,
                        history,
                        responseFormat: "json_object",
                        ct: cancellationToken))
                    {
                        responseBuilder.Append(response);
                    }

                    return responseBuilder.ToString();
                },
                RetryHelper.CreateRelationExtractionRetryOptions(),
                _logger,
                "关系抽取",
                cancellationToken);

            // 检查是否成功
            if (!retryResult.IsSuccess || string.IsNullOrWhiteSpace(retryResult.Data))
            {
                _logger.LogError("关系抽取失败: {Error}", retryResult.ErrorMessage);
                return new List<Relation>();
            }

            // 使用OutputValidator验证和修复响应
            var (relations, isValid, errorMessage) = OutputValidator.ValidateAndRepairRelationResponse(
                retryResult.Data,
                entities,
                _logger);

            if (!isValid)
            {
                _logger.LogWarning("关系抽取响应验证失败: {Error}", errorMessage);
            }

            return relations;
        }

        #endregion

        #region 实体去重

        /// <summary>
        /// 实体去重和合并（使用EntityDeduplicator）
        /// </summary>
        public async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> DeduplicateEntitiesAsync(
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            float similarityThreshold = 0.85f,
            CancellationToken cancellationToken = default)
        {
            return await _entityDeduplicator.DeduplicateEntitiesAsync(
                entities, similarityThreshold, cancellationToken);
        }

        #endregion

        #region 图谱存储和查询

        /// <summary>
        /// 存储实体到图数据库
        /// </summary>
        private async Task StoreEntitiesAsync(List<ZSN.AI.Entity.KnowledgeBase.Entity> entities, CancellationToken cancellationToken)
        {
            foreach (var entity in entities)
            {
                try
                {
                    var properties = new Dictionary<string, object>
                    {
                        { "id", entity.Id },
                        { "text", entity.Text },
                        { "type", entity.Type },
                        { "confidence", entity.Confidence },
                        { "source_chunk_ids", string.Join(",", entity.SourceChunkIds) }
                    };

                    // 添加属性
                    foreach (var attr in entity.Attributes)
                    {
                        properties[attr.Key] = attr.Value;
                    }

                    await _graphRepository.CreateVertexAsync(_graphName, "Entity", properties, cancellationToken);
                    _logger.LogDebug("存储实体: {Text} ({Type})", entity.Text, entity.Type);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "存储实体失败: {Text}", entity.Text);
                }
            }
        }

        /// <summary>
        /// 存储关系到图数据库
        /// </summary>
        private async Task StoreRelationsAsync(List<Relation> relations, CancellationToken cancellationToken)
        {
            foreach (var relation in relations)
            {
                try
                {
                    var properties = new Dictionary<string, object>
                    {
                        { "id", relation.Id },
                        { "type", relation.RelationType },
                        { "confidence", relation.Confidence },
                        { "source_chunk_ids", string.Join(",", relation.SourceChunkIds) }
                    };

                    if (!string.IsNullOrEmpty(relation.Description))
                    {
                        properties["description"] = relation.Description;
                    }

                    await _graphRepository.CreateEdgeAsync(
                        _graphName,
                        relation.HeadEntityId,
                        relation.TailEntityId,
                        "RELATION",
                        properties,
                        cancellationToken);

                    _logger.LogDebug("存储关系: {Head} -> {Tail} ({Type})",
                        relation.HeadEntityId, relation.TailEntityId, relation.RelationType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "存储关系失败: {Head} -> {Tail}",
                        relation.HeadEntityId, relation.TailEntityId);
                }
            }
        }

        /// <summary>
        /// 实体向量化
        /// </summary>
        private async Task VectorizeEntitiesAsync(List<ZSN.AI.Entity.KnowledgeBase.Entity> entities, CancellationToken cancellationToken)
        {
            // TODO: 实现实体向量化
            // 这需要调用embedding service将实体文本转换为向量
            await Task.CompletedTask;
        }

        /// <summary>
        /// 保存实体到图数据库
        /// </summary>
        public async Task SaveEntitiesAsync(
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            string? sourceChunkId = null,
            string? sourceDocumentId = null,
            CancellationToken cancellationToken = default)
        {
            if (entities == null || entities.Count == 0)
            {
                _logger.LogWarning("实体列表为空，跳过保存");
                return;
            }

            _logger.LogInformation("开始保存 {Count} 个实体到图数据库，文档ID: {DocumentId}", entities.Count, sourceDocumentId ?? "未指定");

            try
            {
                foreach (var entity in entities)
                {
                    // 标准化实体类型
                    var originalType = entity.Type;
                    entity.Type = _typeManager.NormalizeType(originalType);

                    if (originalType != entity.Type)
                    {
                        _logger.LogDebug("标准化实体类型: {Original} -> {Normalized}",
                            originalType, entity.Type);
                    }

                    // 生成唯一ID
                    var entityId = string.IsNullOrEmpty(entity.Id)
                        ? $"{entity.Type}_{entity.Text.GetHashCode()}"
                        : entity.Id;

                    // 构建Cypher查询
                    var cypher = $@"
                    MERGE (e:Entity {{id: '{entityId}'}})
                    SET e.type = '{entity.Type}',
                        e.text = '{entity.Text.Replace("'", "\\'")}',
                        e.confidence = {entity.Confidence.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},
                        e.source_chunk_ids = '{sourceChunkId ?? ""}',
                        e.source_document_id = '{sourceDocumentId ?? ""}'
                    RETURN e.id as id
                    ";

                    var result = await _graphRepository.ExecuteCypherAsync(_graphName, cypher, null, cancellationToken);

                    if (result.Count > 0)
                    {
                        _logger.LogDebug("成功保存实体: {EntityType} - {EntityText}", entity.Type, entity.Text);
                    }
                }

                _logger.LogInformation("成功保存 {Count} 个实体到图数据库", entities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存实体到图数据库失败: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 保存关系到图数据库
        /// </summary>
        public async Task SaveRelationsAsync(
            List<Relation> relations,
            string? sourceDocumentId = null,
            CancellationToken cancellationToken = default)
        {
            if (relations == null || relations.Count == 0)
            {
                _logger.LogWarning("关系列表为空，跳过保存");
                return;
            }

            _logger.LogInformation("开始保存 {Count} 个关系到图数据库，文档ID: {DocumentId}", relations.Count, sourceDocumentId ?? "未指定");

            try
            {
                foreach (var relation in relations)
                {
                    // 使用HeadEntityId和TailEntityId
                    var sourceId = relation.HeadEntityId;
                    var targetId = relation.TailEntityId;

                    // 构建Cypher查询
                    var confidenceStr = relation.Confidence.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
                    var cypher = string.Format(@"
                    MERGE (s:Entity {{id: '{0}'}})
                    MERGE (t:Entity {{id: '{1}'}})
                    MERGE (s)-[r:RELATION {{type: '{2}', confidence: {3}, source_document_id: '{4}'}}]->(t)
                    RETURN s.id as source_id, t.id as target_id
                    ", sourceId, targetId, relation.RelationType, confidenceStr, sourceDocumentId ?? "");

                    var result = await _graphRepository.ExecuteCypherAsync(_graphName, cypher, null, cancellationToken);

                    if (result.Count > 0)
                    {
                        _logger.LogDebug("成功保存关系: {SourceId} --[{RelationType}]--> {TargetId}",
                            sourceId, relation.RelationType, targetId);
                    }
                }

                _logger.LogInformation("成功保存 {Count} 个关系到图数据库", relations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存关系到图数据库失败: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 图谱查询
        /// </summary>
        public async Task<GraphQueryResult> QueryAsync(
            string cypherQuery,
            GraphQueryOptions options,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var rows = await _graphRepository.ExecuteCypherAsync(
                    _graphName,
                    cypherQuery,
                    options.Parameters,
                    cancellationToken);

                var executionTime = DateTime.UtcNow - startTime;

                return new GraphQueryResult
                {
                    Rows = rows,
                    TotalCount = rows.Count,
                    ExecutionTime = executionTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "图谱查询失败: {Message}", ex.Message);
                return new GraphQueryResult
                {
                    Rows = new List<Dictionary<string, object>>(),
                    TotalCount = 0,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// 通过实体文本查找图数据库中的节点ID
        /// </summary>
        public async Task<string?> FindEntityIdByTextAsync(
            string entityText,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(entityText))
                return null;

            try
            {
                var cypher = $@"
                MATCH (e:Entity)
                WHERE properties(e).text = '{entityText.Replace("'", "''")}'
                RETURN properties(e).id as entity_id
                LIMIT 1
                ";

                var results = await _graphRepository.ExecuteCypherAsync(_graphName, cypher, cancellationToken: cancellationToken);
                
                if (results.Count > 0 && results[0].ContainsKey("entity_id"))
                {
                    var entityId = results[0]["entity_id"]?.ToString();
                    _logger.LogDebug("找到实体ID: Text={Text}, Id={Id}", entityText, entityId);
                    return entityId;
                }

                _logger.LogDebug("未找到实体: Text={Text}", entityText);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查找实体ID失败: Text={Text}, 错误={Message}", entityText, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 多跳查询
        /// </summary>
        public async Task<List<GraphPath>> MultiHopQueryAsync(
            string startEntityId,
            List<string> relationTypes,
            int maxHops,
            CancellationToken cancellationToken = default)
        {
            // 验证起点ID
            if (string.IsNullOrWhiteSpace(startEntityId))
            {
                _logger.LogWarning("多跳查询: 起点ID为空，跳过查询");
                return new List<GraphPath>();
            }

            _logger.LogInformation("多跳查询: 起点={StartEntityId}, 最大跳数={MaxHops}, 关系类型过滤={RelationTypeCount}个",
                startEntityId, maxHops, relationTypes.Count);

            try
            {
                // 构建Cypher查询
                // 注意: 'end'是Cypher保留关键字,不能作为变量名
                string cypher;
                
                if (relationTypes.Count > 0)
                {
                    // 有关系类型过滤
                    var relationTypesStr = FormatRelationTypes(relationTypes);
                    cypher = $@"
                    MATCH path = (start:Entity {{id: '{startEntityId}'}})-[r*1..{maxHops}]-(endNode:Entity)
                    WHERE ALL(rel IN r WHERE type(rel) IN {relationTypesStr})
                    RETURN path,
                           [n IN nodes(path) | properties(n).text] as entity_names,
                           [rel IN relationships(path) | type(rel)] as relation_types
                    LIMIT 50
                    ";
                }
                else
                {
                    // 无关系类型过滤
                    cypher = $@"
                    MATCH path = (start:Entity {{id: '{startEntityId}'}})-[r*1..{maxHops}]-(endNode:Entity)
                    RETURN path,
                           [n IN nodes(path) | properties(n).text] as entity_names,
                           [rel IN relationships(path) | type(rel)] as relation_types
                    LIMIT 50
                    ";
                }

                var results = await _graphRepository.ExecuteCypherAsync(_graphName, cypher, cancellationToken: cancellationToken);
                var paths = ParseGraphPaths(results);
                
                _logger.LogInformation("多跳查询完成: 起点={StartEntityId}, 找到={PathCount}条路径", 
                    startEntityId, paths.Count);
                
                return paths;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "多跳查询失败: 起点={StartEntityId}, 错误={Message}", 
                    startEntityId, ex.Message);
                return new List<GraphPath>();
            }
        }

        /// <summary>
        /// 格式化关系类型列表
        /// </summary>
        private string FormatRelationTypes(List<string> relationTypes)
        {
            var formatted = relationTypes.Select(rt => $"'{rt}'");
            return $"[{string.Join(", ", formatted)}]";
        }

        /// <summary>
        /// 解析图谱路径
        /// </summary>
        private List<GraphPath> ParseGraphPaths(List<Dictionary<string, object>> results)
        {
            var paths = new List<GraphPath>();

            foreach (var result in results)
            {
                // TODO: 解析路径数据
                // 这里需要根据实际的返回格式进行解析
            }

            return paths;
        }

        /// <summary>
        /// 获取图谱统计信息
        /// </summary>
        public async Task<GraphStatistics> GetStatisticsAsync(
            string knowledgeBaseId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 确保图数据库仓储已初始化
                await _graphRepository.InitializeAsync(cancellationToken);

                // 统计实体数量
                var entityCountCypher = "MATCH (e:Entity) RETURN count(e)";
                var entityResults = await _graphRepository.ExecuteCypherAsync(_graphName, entityCountCypher, null, cancellationToken);
                var totalEntities = 0;
                if (entityResults.FirstOrDefault()?.TryGetValue("a0", out var entityCountObj) == true)
                {
                    // AGE 返回的是字符串格式的数字，需要解析
                    if (int.TryParse(entityCountObj?.ToString(), out var count))
                    {
                        totalEntities = count;
                    }
                }

                // 统计关系数量
                var relationCountCypher = "MATCH ()-[r:RELATION]->() RETURN count(r)";
                var relationResults = await _graphRepository.ExecuteCypherAsync(_graphName, relationCountCypher, null, cancellationToken);
                var totalRelations = 0;
                if (relationResults.FirstOrDefault()?.TryGetValue("a0", out var relationCountObj) == true)
                {
                    // AGE 返回的是字符串格式的数字，需要解析
                    if (int.TryParse(relationCountObj?.ToString(), out var count))
                    {
                        totalRelations = count;
                    }
                }

                // TODO: 按类型统计

                return new GraphStatistics
                {
                    TotalEntities = totalEntities,
                    TotalRelations = totalRelations,
                    EntityCountsByType = new Dictionary<string, int>(),
                    RelationCountsByType = new Dictionary<string, int>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取图谱统计信息失败: {Message}", ex.Message);
                return new GraphStatistics();
            }
        }

        /// <summary>
        /// 获取指定文档的图谱统计信息
        /// </summary>
        public async Task<GraphStatistics> GetDocumentStatisticsAsync(
            string documentId,
            string knowledgeBaseId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("获取文档 {DocumentId} 的图谱统计", documentId);

                // 确保图数据库仓储已初始化
                await _graphRepository.InitializeAsync(cancellationToken);

                // 统计指定文档的实体数量 - 使用 WHERE 子句更可靠
                var entityCountCypher = $"MATCH (e:Entity) WHERE e.source_document_id = '{documentId}' RETURN count(e)";
                var entityResults = await _graphRepository.ExecuteCypherAsync(_graphName, entityCountCypher, null, cancellationToken);
                var totalEntities = 0;

                if (entityResults.Count > 0)
                {
                    var firstRow = entityResults.FirstOrDefault();

                    if (firstRow != null)
                    {
                        // 尝试多个可能的键名
                        string[] possibleKeys = { "count", "a0", "entity_count" };
                        foreach (var key in possibleKeys)
                        {
                            if (firstRow.TryGetValue(key, out var entityCountObj))
                            {
                                var countStr = entityCountObj?.ToString();

                                // 处理可能的字符串格式（如 "14" 或 '{"int": 14}'）
                                if (!string.IsNullOrEmpty(countStr))
                                {
                                    // 尝试直接解析整数
                                    if (int.TryParse(countStr, out var count))
                                    {
                                        totalEntities = count;
                                        break;
                                    }

                                    // 尝试解析 agtype 格式（如 {"int": 14}）
                                    if (countStr.StartsWith("{") && countStr.Contains("int"))
                                    {
                                        var match = System.Text.RegularExpressions.Regex.Match(countStr, @"""int""\s*:\s*(\d+)");
                                        if (match.Success && int.TryParse(match.Groups[1].Value, out count))
                                        {
                                            totalEntities = count;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 统计指定文档的关系数量 - 使用 WHERE 子句更可靠
                var relationCountCypher = $"MATCH ()-[r:RELATION]->() WHERE r.source_document_id = '{documentId}' RETURN count(r)";
                var relationResults = await _graphRepository.ExecuteCypherAsync(_graphName, relationCountCypher, null, cancellationToken);
                var totalRelations = 0;

                if (relationResults.Count > 0)
                {
                    var firstRow = relationResults.FirstOrDefault();

                    if (firstRow != null)
                    {
                        // 尝试多个可能的键名
                        string[] possibleKeys = { "count", "a0", "relation_count" };
                        foreach (var key in possibleKeys)
                        {
                            if (firstRow.TryGetValue(key, out var relationCountObj))
                            {
                                var countStr = relationCountObj?.ToString();

                                // 处理可能的字符串格式
                                if (!string.IsNullOrEmpty(countStr))
                                {
                                    // 尝试直接解析整数
                                    if (int.TryParse(countStr, out var count))
                                    {
                                        totalRelations = count;
                                        break;
                                    }

                                    // 尝试解析 agtype 格式
                                    if (countStr.StartsWith("{") && countStr.Contains("int"))
                                    {
                                        var match = System.Text.RegularExpressions.Regex.Match(countStr, @"""int""\s*:\s*(\d+)");
                                        if (match.Success && int.TryParse(match.Groups[1].Value, out count))
                                        {
                                            totalRelations = count;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation("文档 {DocumentId} 统计结果: 实体数={EntityCount}, 关系数={RelationCount}",
                    documentId, totalEntities, totalRelations);

                return new GraphStatistics
                {
                    TotalEntities = totalEntities,
                    TotalRelations = totalRelations,
                    EntityCountsByType = new Dictionary<string, int>(),
                    RelationCountsByType = new Dictionary<string, int>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文档图谱统计信息失败: {Message}", ex.Message);
                return new GraphStatistics();
            }
        }

        /// <summary>
        /// 清理没有 source_document_id 的旧数据
        /// </summary>
        public async Task<int> CleanupOldDataWithoutDocumentIdAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始清理没有 source_document_id 的旧数据");

                await _graphRepository.InitializeAsync(cancellationToken);

                // 删除没有 source_document_id 的关系
                var deleteRelationCypher = "MATCH ()-[r:RELATION]->() WHERE r.source_document_id IS NULL OR r.source_document_id = '' DELETE r";
                var relationResult = await _graphRepository.ExecuteCypherAsync(_graphName, deleteRelationCypher, null, cancellationToken);
                var deletedRelations = relationResult.Count;

                // 删除没有 source_document_id 的实体
                var deleteEntityCypher = "MATCH (e:Entity) WHERE e.source_document_id IS NULL OR e.source_document_id = '' DELETE e";
                var entityResult = await _graphRepository.ExecuteCypherAsync(_graphName, deleteEntityCypher, null, cancellationToken);
                var deletedEntities = entityResult.Count;

                _logger.LogInformation("清理完成: 删除了 {DeletedEntities} 个实体和 {DeletedRelations} 个关系",
                    deletedEntities, deletedRelations);

                return deletedEntities + deletedRelations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理旧数据失败: {Message}", ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// 获取指定文档的图谱可视化数据
        /// </summary>
        public async Task<GraphVisualizationData> GetDocumentGraphDataAsync(
            string documentId,
            string knowledgeBaseId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("获取文档 {DocumentId} 的图谱可视化数据", documentId);

                await _graphRepository.InitializeAsync(cancellationToken);

                var result = new GraphVisualizationData();

                // 查询所有实体 - 不能直接返回实体对象，需要返回属性
                var entityCypher = $"MATCH (e:Entity) WHERE e.source_document_id = '{documentId}' RETURN e.id as id, e.text as text, e.type as type, e.confidence as confidence, e.source_chunk_ids as sourceChunkIds";
                var entityResults = await _graphRepository.ExecuteCypherAsync(_graphName, entityCypher, null, cancellationToken);

                foreach (var row in entityResults)
                {
                    try
                    {

                        // Apache AGE 返回的列名是 a0, a1, a2, a3, a4
                        // a0 = id, a1 = 可能是某个属性, a2 = type, a3 = confidence, a4 = source_chunk_ids
                        var id = row.TryGetValue("a0", out var idObj) ? ParseAgtypeString(idObj) : string.Empty;

                        // 尝试从多个可能的列获取文本内容
                        var text = string.Empty;
                        if (row.TryGetValue("a1", out var textObj))
                        {
                            text = ParseAgtypeString(textObj) ?? string.Empty;
                        }

                        var type = row.TryGetValue("a2", out var typeObj) ? ParseAgtypeString(typeObj) : string.Empty;

                        // 解析置信度
                        var confidence = 0f;
                        if (row.TryGetValue("a3", out var confObj))
                        {
                            var confStr = confObj?.ToString();
                            if (!string.IsNullOrEmpty(confStr))
                            {
                                // 尝试解析 agtype 格式
                                if (confStr.StartsWith("{") && confStr.Contains("float"))
                                {
                                    var match = System.Text.RegularExpressions.Regex.Match(confStr, @"""float""\s*:\s*([0-9.]+)");
                                    if (match.Success)
                                    {
                                        float.TryParse(match.Groups[1].Value, out confidence);
                                    }
                                }
                                else
                                {
                                    float.TryParse(confStr, out confidence);
                                }
                            }
                        }

                        // 解析 source_chunk_ids 数组
                        var sourceChunkIds = new List<string>();
                        if (row.TryGetValue("a4", out var chunksObj))
                        {
                            var chunksStr = chunksObj?.ToString();
                            if (!string.IsNullOrEmpty(chunksStr))
                            {
                                try
                                {
                                    // 尝试解析 agtype 数组格式: ["id1", "id2", ...]
                                    if (chunksStr.StartsWith("["))
                                    {
                                        var chunksJson = System.Text.Json.JsonDocument.Parse(chunksStr);
                                        foreach (var chunk in chunksJson.RootElement.EnumerateArray())
                                        {
                                            var chunkId = chunk.GetString();
                                            if (!string.IsNullOrEmpty(chunkId))
                                            {
                                                sourceChunkIds.Add(chunkId);
                                            }
                                        }
                                    }
                                }
                                catch (System.Text.Json.JsonException ex)
                                {
                                    _logger.LogDebug(ex, "解析 source_chunk_ids 失败: {ChunksData}", chunksStr);
                                }
                            }
                        }

                        var node = new GraphNode
                        {
                            Id = id ?? string.Empty,
                            Name = text ?? string.Empty,
                            Type = type ?? string.Empty,
                            Confidence = confidence,
                            SourceChunkIds = sourceChunkIds
                        };

                        // 根据连接数设置节点大小
                        node.Size = 10 + (node.SourceChunkIds.Count * 2);

                        result.Nodes.Add(node);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "处理实体行数据失败");
                    }
                }

                _logger.LogInformation("找到 {NodeCount} 个节点", result.Nodes.Count);

                // 查询所有关系 - 不能直接返回关系对象，需要返回属性
                var relationCypher = $"MATCH (s:Entity)-[r:RELATION]->(t:Entity) WHERE r.source_document_id = '{documentId}' RETURN s.id as source, t.id as target, r.type as relationType, r.confidence as confidence";
                var relationResults = await _graphRepository.ExecuteCypherAsync(_graphName, relationCypher, null, cancellationToken);

                foreach (var row in relationResults)
                {
                    try
                    {

                        // Apache AGE 返回的列名是 a0, a1, a2, a3
                        // a0 = s.id (source), a1 = t.id (target), a2 = r.type (relationType), a3 = r.confidence
                        var sourceId = row.TryGetValue("a0", out var sourceObj) ? ParseAgtypeString(sourceObj) : string.Empty;
                        var targetId = row.TryGetValue("a1", out var targetObj) ? ParseAgtypeString(targetObj) : string.Empty;
                        var relationType = row.TryGetValue("a2", out var typeObj) ? ParseAgtypeString(typeObj) : string.Empty;

                        // 解析置信度 (a3)
                        var confidence = 0f;
                        if (row.TryGetValue("a3", out var confObj))
                        {
                            var confStr = ParseAgtypeString(confObj);
                            if (!string.IsNullOrEmpty(confStr))
                            {
                                float.TryParse(confStr, out confidence);
                            }
                        }

                        var link = new GraphLink
                        {
                            Source = sourceId ?? string.Empty,
                            Target = targetId ?? string.Empty,
                            RelationType = relationType ?? string.Empty,
                            Confidence = confidence
                        };

                        // 只有当两个节点都存在时才添加关系
                        if (result.Nodes.Any(n => n.Id == link.Source) && result.Nodes.Any(n => n.Id == link.Target))
                        {
                            result.Links.Add(link);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "处理关系行数据失败");
                    }
                }

                _logger.LogInformation("找到 {LinkCount} 个关系", result.Links.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文档图谱可视化数据失败: {Message}", ex.Message);
                return new GraphVisualizationData();
            }
        }

        /// <summary>
        /// 删除指定文档的所有实体和关系
        /// </summary>
        public async Task<(int entityCount, int relationCount)> DeleteDocumentGraphAsync(
            string documentId,
            string knowledgeBaseId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _graphRepository.InitializeAsync(cancellationToken);
                var graphName = knowledgeBaseId.Replace("-", "_");

                int entityCount = 0;
                int relationCount = 0;

                // 首先删除与该文档相关的所有关系
                var relationCypher = $"MATCH ()-[r:RELATION]->() WHERE r.source_document_id = '{documentId}' RETURN r";
                var relationResults = await _graphRepository.ExecuteCypherAsync(graphName, relationCypher, null, cancellationToken);

                foreach (var relationRow in relationResults)
                {
                    try
                    {
                        if (relationRow.TryGetValue("r", out var relationObj))
                        {
                            // 从agtype中提取边ID
                            var relationStr = relationObj?.ToString();
                            if (!string.IsNullOrEmpty(relationStr) && relationStr.Contains("\"id\":"))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(relationStr, "\"id\":\\s*\"([^\"]+)\"");
                                if (match.Success)
                                {
                                    var edgeId = match.Groups[1].Value;
                                    await _graphRepository.DeleteEdgeAsync(graphName, edgeId, cancellationToken);
                                    relationCount++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除关系失败");
                    }
                }

                // 删除与该文档相关的所有实体
                var entityCypher = $"MATCH (e:Entity) WHERE e.source_document_id = '{documentId}' RETURN e";
                var entityResults = await _graphRepository.ExecuteCypherAsync(graphName, entityCypher, null, cancellationToken);

                foreach (var entityRow in entityResults)
                {
                    try
                    {
                        if (entityRow.TryGetValue("e", out var entityObj))
                        {
                            // 从agtype中提取顶点ID
                            var entityStr = entityObj?.ToString();
                            if (!string.IsNullOrEmpty(entityStr) && entityStr.Contains("\"id\":"))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(entityStr, "\"id\":\\s*\"([^\"]+)\"");
                                if (match.Success)
                                {
                                    var vertexId = match.Groups[1].Value;
                                    await _graphRepository.DeleteVertexAsync(graphName, vertexId, cancellationToken);
                                    entityCount++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除实体失败");
                    }
                }

                _logger.LogInformation("删除文档 {DocumentId} 的图谱数据: {EntityCount} 个实体, {RelationCount} 个关系",
                    documentId, entityCount, relationCount);

                return (entityCount, relationCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除文档图谱数据失败: {Message}", ex.Message);
                return (0, 0);
            }
        }

        /// <summary>
        /// 删除指定知识库的所有图谱数据
        /// </summary>
        public async Task<(int entityCount, int relationCount)> DeleteKnowledgeBaseGraphAsync(
            string knowledgeBaseId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _graphRepository.InitializeAsync(cancellationToken);
                var graphName = knowledgeBaseId.Replace("-", "_");

                // 检查图是否存在
                var graphExists = await _graphRepository.GraphExistsAsync(graphName, cancellationToken);
                if (!graphExists)
                {
                    _logger.LogWarning("图 {GraphName} 不存在", graphName);
                    return (0, 0);
                }

                // 删除整个图（这会删除所有顶点和边）
                await _graphRepository.DropGraphAsync(graphName, cancellationToken);

                _logger.LogInformation("删除知识库 {KnowledgeBaseId} 的所有图谱数据", knowledgeBaseId);

                // 由于我们删除了整个图，无法返回准确的计数
                // 返回 (-1, -1) 表示整个图被删除
                return (-1, -1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除知识库图谱数据失败: {Message}", ex.Message);
                return (0, 0);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 解析 Apache AGE agtype 字符串值
        /// agtype 格式: "string" 或 {"float": 123.0} 等
        /// </summary>
        private string? ParseAgtypeString(object? value)
        {
            if (value == null) return string.Empty;

            var str = value.ToString();
            if (string.IsNullOrEmpty(str)) return string.Empty;

            // 去除首尾空白
            str = str.Trim();

            // 如果已经是普通字符串（不包含特殊字符），直接返回
            if (!str.StartsWith("\"") && !str.StartsWith("{"))
            {
                return str;
            }

            // 如果是 JSON 字符串格式（带引号），解析它
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(str);
                    return json.RootElement.GetString() ?? string.Empty;
                }
                catch (System.Text.Json.JsonException)
                {
                    // 解析失败，去掉引号
                    return str.Trim('"');
                }
            }

            // 如果是 agtype 对象格式（如 {"float": 123.0} 或 {"string": "value"}）
            if (str.StartsWith("{"))
            {
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(str);
                    // 尝试获取各种可能的值类型
                    if (json.RootElement.TryGetProperty("string", out var stringProp))
                        return stringProp.GetString() ?? string.Empty;
                    if (json.RootElement.TryGetProperty("float", out var floatProp))
                        return floatProp.GetSingle().ToString();
                    if (json.RootElement.TryGetProperty("integer", out var intProp))
                        return intProp.GetInt32().ToString();
                    if (json.RootElement.TryGetProperty("bigint", out var bigIntProp))
                        return bigIntProp.GetInt64().ToString();
                }
                catch (System.Text.Json.JsonException)
                {
                    // 解析失败，返回原字符串
                }
            }

            return str;
        }

        /// <summary>
        /// 获取Chat模型ID
        /// </summary>
        private int GetChatModelId()
        {
            var modelIdStr = _configuration?["LargeModel:ChatModelID"];
            if (int.TryParse(modelIdStr, out var modelId))
            {
                return modelId;
            }
            return 13; // 默认值
        }

        /// <summary>
        /// 获取文档分块（TODO: 需要实现）
        /// </summary>
        private async Task<List<(string ChunkId, string Content)>> GetDocumentChunksAsync(
            string documentId,
            CancellationToken cancellationToken)
        {
            // TODO: 从数据库获取文档的分块内容
            // 这需要查询存储在PostgreSQL中的分块数据
            await Task.CompletedTask;
            return new List<(string, string)>();
        }

        #endregion
    }
}
