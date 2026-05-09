using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.KnowledgeBase.Models;
using ZSN.AI.KnowledgeBase.Interface;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 混合检索服务实现
    /// </summary>
    public class HybridSearchService : IHybridSearchService
    {
        private readonly IKnowledgeGraphService _knowledgeGraphService;
        private readonly IGraphRepository _graphRepository;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorRepository _vectorRepository;
        private readonly IConfiguration? _configuration;
        private readonly ILogger<HybridSearchService> _logger;
        private readonly string _graphName;

        /// <summary>
        /// 构造函数
        /// </summary>
        public HybridSearchService(
            IKnowledgeGraphService knowledgeGraphService,
            IGraphRepository graphRepository,
            IEmbeddingService embeddingService,
            IVectorRepository vectorRepository,
            IConfiguration? configuration,
            ILogger<HybridSearchService> logger)
        {
            _knowledgeGraphService = knowledgeGraphService;
            _graphRepository = graphRepository;
            _embeddingService = embeddingService;
            _vectorRepository = vectorRepository;
            _configuration = configuration;
            _logger = logger;

            // 从配置中获取图名称
            _graphName = configuration?["DbConnectionStrings:KnowledgeBaseDb:GraphName"] ?? "knowledge_graph";
            _logger.LogInformation("混合检索服务初始化，图名称: {GraphName}", _graphName);
        }

        #region 混合检索

        /// <summary>
        /// 混合检索（向量+图谱）
        /// </summary>
        public async Task<HybridSearchResult> SearchAsync(
            string query,
            string knowledgeBaseId,
            HybridSearchOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("开始混合检索: 查询={Query}, 知识库={KnowledgeBaseId}",
                query, knowledgeBaseId);

            var stopwatch = Stopwatch.StartNew();
            var result = new HybridSearchResult();

            try
            {
                // 并行执行向量检索和图谱检索
                var vectorTask = VectorSearchAsync(query, knowledgeBaseId, options.MaxVectorResults, cancellationToken);
                var graphTask = GraphSearchAsync(query, knowledgeBaseId, new GraphSearchOptions
                {
                    MaxEntities = options.MaxGraphResults,
                    MaxHops = options.MaxExpansionHops
                }, cancellationToken);

                await Task.WhenAll(vectorTask, graphTask);

                var vectorResults = await vectorTask;
                var graphResults = await graphTask;

                result.VectorResults = vectorResults;
                result.GraphResults = graphResults;
                result.Metadata.VectorSearchTime = stopwatch.Elapsed;
                result.Metadata.VectorResultCount = vectorResults.Count;
                result.Metadata.GraphResultCount = graphResults.Count;

                _logger.LogInformation("向量检索: {Count} 个结果, 图谱检索: {Count} 个结果",
                    vectorResults.Count, graphResults.Count);

                // 获取相关路径
                if (options.EnableGraphExpansion)
                {
                    result.RelatedPaths = await ExtractRelatedPaths(graphResults, cancellationToken);
                }

                // 结果融合
                var fusionStopwatch = Stopwatch.StartNew();
                result.FusedResults = await FuseResultsAsync(options, vectorResults, graphResults, cancellationToken);
                result.Metadata.FusionTime = fusionStopwatch.Elapsed;
                result.Metadata.FinalResultCount = result.FusedResults.Count;

                // 重排序
                if (options.EnableRerank)
                {
                    result.FusedResults = await RerankAsync(query, result.FusedResults, new RerankOptions
                    {
                        Method = RerankMethod.CrossEncoder,
                        TopK = options.MaxVectorResults
                    }, cancellationToken);
                }

                result.Metadata.TotalTime = stopwatch.Elapsed;

                _logger.LogInformation("混合检索完成，总结果数: {Count}, 耗时: {ElapsedMs}ms",
                    result.FusedResults.Count, result.Metadata.TotalTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "混合检索失败: {Message}", ex.Message);
                result.Metadata.TotalTime = stopwatch.Elapsed;
                return result;
            }
        }

        #endregion

        #region 向量检索

        /// <summary>
        /// 向量检索
        /// </summary>
        public async Task<List<SearchResult>> VectorSearchAsync(
            string query,
            string knowledgeBaseId,
            int topK,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 步骤1: 对查询进行向量化
                var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);

                if (queryEmbedding == null || queryEmbedding.Length == 0)
                {
                    _logger.LogWarning("查询向量化失败，返回空结果");
                    return new List<SearchResult>();
                }

                // 步骤2: 在向量数据库中检索相似文档
                var vectorResults = await _vectorRepository.SearchDocumentChunksAsync(
                    queryEmbedding,
                    topK,
                    knowledgeBaseId,  // documentId 作为 knowledgeBaseId
                    cancellationToken);

                // 步骤3: 转换为 SearchResult 格式
                var results = new List<SearchResult>();
                foreach (var vectorResult in vectorResults)
                {
                    results.Add(new SearchResult
                    {
                        ChunkId = vectorResult.Id,
                        Content = vectorResult.Content,
                        Score = vectorResult.Similarity,
                        Source = "VectorSearch"
                    });
                }

                _logger.LogInformation("向量检索成功，返回 {Count} 个结果", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "向量检索失败: {Message}", ex.Message);
                return new List<SearchResult>();
            }
        }

        #endregion

        #region 图谱检索

        /// <summary>
        /// 图谱检索
        /// </summary>
        public async Task<List<GraphSearchResult>> GraphSearchAsync(
            string query,
            string knowledgeBaseId,
            GraphSearchOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("开始图谱检索: 查询={Query}, 最大实体数={MaxEntities}",
                query, options.MaxEntities);

            try
            {
                // 步骤1: 从查询中识别实体
                var entities = await _knowledgeGraphService.ExtractEntitiesAsync(query,
                    new EntityExtractionConfig
                    {
                        ModelId = GetChatModelId(),
                        MinConfidence = 0.6f
                    }, cancellationToken);

                _logger.LogInformation("从查询中识别出 {Count} 个实体", entities.Count);

                if (entities.Count == 0)
                {
                    return new List<GraphSearchResult>();
                }

                var results = new List<GraphSearchResult>();

                // 步骤2: 在图中查找匹配的实体
                foreach (var queryEntity in entities.Take(options.MaxEntities))
                {
                    var graphEntities = await FindEntitiesInGraph(queryEntity, options, cancellationToken);

                    foreach (var graphEntity in graphEntities)
                    {
                        // 获取相关路径
                        var paths = await GetEntityPaths(graphEntity.Id, options, cancellationToken);

                        results.Add(new GraphSearchResult
                        {
                            Entity = graphEntity,
                            RelatedPaths = paths,
                            Score = queryEntity.Confidence,
                            MatchingChunkIds = graphEntity.SourceChunkIds
                        });
                    }
                }

                _logger.LogInformation("图谱检索完成，找到 {Count} 个结果", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "图谱检索失败: {Message}", ex.Message);
                return new List<GraphSearchResult>();
            }
        }

        /// <summary>
        /// 在图中查找实体
        /// </summary>
        private async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> FindEntitiesInGraph(
            ZSN.AI.Entity.KnowledgeBase.Entity queryEntity,
            GraphSearchOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                // 构建Cypher查询
                var cypher = $@"
                MATCH (e:Entity)
                WHERE e.type = '{queryEntity.Type}'
                  AND e.text CONTAINS '{queryEntity.Text}'
                RETURN e.id as id,
                       e.text as text,
                       e.type as type,
                       e.confidence as confidence,
                       e.source_chunk_ids as source_chunk_ids
                LIMIT {options.MaxPathsPerEntity}
                ";

                var results = await _graphRepository.ExecuteCypherAsync(_graphName, cypher, null, cancellationToken);
                return ParseGraphEntities(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "在图中查找实体失败: {Message}", ex.Message);
                return new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
            }
        }

        /// <summary>
        /// 解析图实体
        /// </summary>
        private List<ZSN.AI.Entity.KnowledgeBase.Entity> ParseGraphEntities(List<Dictionary<string, object>> results)
        {
            var entities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();

            foreach (var result in results)
            {
                try
                {
                    var entity = new ZSN.AI.Entity.KnowledgeBase.Entity
                    {
                        Id = result.GetValueOrDefault("id")?.ToString() ?? string.Empty,
                        Text = result.GetValueOrDefault("text")?.ToString() ?? string.Empty,
                        Type = result.GetValueOrDefault("type")?.ToString() ?? string.Empty,
                        Confidence = float.Parse(result.GetValueOrDefault("confidence")?.ToString() ?? "0")
                    };

                    var chunkIdsStr = result.GetValueOrDefault("source_chunk_ids")?.ToString();
                    if (!string.IsNullOrEmpty(chunkIdsStr))
                    {
                        entity.SourceChunkIds = chunkIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                    }

                    entities.Add(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解析实体失败");
                }
            }

            return entities;
        }

        /// <summary>
        /// 获取实体路径
        /// </summary>
        private async Task<List<GraphPath>> GetEntityPaths(
            string entityId,
            GraphSearchOptions options,
            CancellationToken cancellationToken)
        {
            var paths = await _knowledgeGraphService.MultiHopQueryAsync(
                entityId,
                options.RelationTypes,
                options.MaxHops,
                cancellationToken);

            return paths;
        }

        #endregion

        #region 图谱增强检索

        /// <summary>
        /// 图谱增强的向量检索
        /// </summary>
        public async Task<List<SearchResult>> GraphEnhancedSearchAsync(
            string query,
            string knowledgeBaseId,
            GraphEnhancedSearchOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("开始图谱增强检索: 查询={Query}", query);

            try
            {
                // 步骤1: 向量检索获取初始结果
                var vectorResults = await VectorSearchAsync(query, knowledgeBaseId, 20, cancellationToken);

                // 步骤2: 从查询中识别实体
                var entities = await _knowledgeGraphService.ExtractEntitiesAsync(query,
                    new EntityExtractionConfig
                    {
                        ModelId = GetChatModelId(),
                        MinConfidence = options.EntityMatchThreshold
                    }, cancellationToken);

                if (entities.Count == 0 || !options.ExpandRelatedEntities)
                {
                    return vectorResults;
                }

                // 步骤3: 扩展相关实体
                var expandedChunkIds = new HashSet<string>();
                int validQueryCount = 0;
                int emptyIdCount = 0;
                
                foreach (var entity in entities)
                {
                    _logger.LogDebug("处理实体: Id={EntityId}, Text={EntityText}, Type={EntityType}", 
                        entity.Id ?? "(空)", entity.Text, entity.Type);
                    
                    // 尝试通过文本查找图数据库中的实体ID
                    string? graphEntityId = null;
                    
                    if (!string.IsNullOrWhiteSpace(entity.Id))
                    {
                        // 如果实体已有ID，先尝试使用它
                        graphEntityId = entity.Id;
                    }
                    
                    // 如果ID无效或为临时GUID，尝试通过文本查找
                    if (string.IsNullOrWhiteSpace(graphEntityId) || graphEntityId.Length == 36)
                    {
                        graphEntityId = await _knowledgeGraphService.FindEntityIdByTextAsync(
                            entity.Text, cancellationToken);
                    }
                    
                    // 检查是否找到有效的图数据库ID
                    if (string.IsNullOrWhiteSpace(graphEntityId))
                    {
                        emptyIdCount++;
                        _logger.LogWarning("未找到图数据库中的实体ID: Text={EntityText}, Type={EntityType}", 
                            entity.Text, entity.Type);
                        continue;
                    }
                    
                    validQueryCount++;
                    _logger.LogDebug("使用图数据库ID查询: GraphId={GraphId}, Text={Text}", 
                        graphEntityId, entity.Text);
                    
                    var paths = await _knowledgeGraphService.MultiHopQueryAsync(
                        graphEntityId,
                        new List<string>(),
                        options.ExpansionDepth,
                        cancellationToken);

                    foreach (var path in paths)
                    {
                        foreach (var chunkId in path.ChunkIds)
                        {
                            expandedChunkIds.Add(chunkId);
                        }
                    }
                }
                
                _logger.LogInformation("实体查询统计: 总数={Total}, 有效查询={Valid}, 空ID跳过={Empty}", 
                    entities.Count, validQueryCount, emptyIdCount);

                // 步骤4: 获取扩展分块的内容
                var expandedResults = await GetResultsByChunkIds(expandedChunkIds.ToList(), cancellationToken);

                // 步骤5: 合并和排序结果
                var combinedResults = vectorResults.ToList();

                foreach (var expandedResult in expandedResults)
                {
                    var existing = combinedResults.FirstOrDefault(r => r.ChunkId == expandedResult.ChunkId);
                    if (existing == null)
                    {
                        combinedResults.Add(expandedResult);
                    }
                }

                _logger.LogInformation("图谱增强检索完成，原始结果: {OriginalCount}, 扩展后: {ExpandedCount}",
                    vectorResults.Count, combinedResults.Count);

                return combinedResults.OrderByDescending(r => r.Score).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "图谱增强检索失败: {Message}", ex.Message);
                return new List<SearchResult>();
            }
        }

        /// <summary>
        /// 根据分块ID获取结果
        /// </summary>
        private async Task<List<SearchResult>> GetResultsByChunkIds(
            List<string> chunkIds,
            CancellationToken cancellationToken)
        {
            try
            {
                if (chunkIds == null || chunkIds.Count == 0)
                {
                    _logger.LogWarning("获取分块结果时，chunkIds为空");
                    return new List<SearchResult>();
                }

                // 从向量数据库获取分块内容
                var vectorResults = await _vectorRepository.GetDocumentChunksByIdsAsync(chunkIds, cancellationToken);

                // 转换为SearchResult格式
                var results = vectorResults.Select(vr =>
                {
                    var result = new SearchResult
                    {
                        ChunkId = vr.Id,
                        Content = vr.Content,
                        Score = vr.Similarity,
                        FusedScore = vr.Similarity,
                        Source = "database",
                        RelatedPaths = new List<GraphPath>(),
                        Metadata = new Dictionary<string, object>()
                    };

                    // 从VectorSearchResult的Metadata中提取DocumentId等信息
                    if (vr.Metadata != null)
                    {
                        if (vr.Metadata.TryGetValue("document_id", out var docIdObj))
                        {
                            result.DocumentId = docIdObj?.ToString() ?? string.Empty;
                        }

                        // 复制所有其他元数据
                        foreach (var kvp in vr.Metadata)
                        {
                            result.Metadata.TryAdd(kvp.Key, kvp.Value);
                        }
                    }

                    return result;
                }).ToList();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据分块ID获取结果时发生错误: {Message}", ex.Message);
                return new List<SearchResult>();
            }
        }

        #endregion

        #region 结果融合

        /// <summary>
        /// 结果融合
        /// </summary>
        public async Task<List<SearchResult>> FuseResultsAsync(
            HybridSearchOptions options,
            List<SearchResult> vectorResults,
            List<GraphSearchResult> graphResults,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            _logger.LogDebug("开始结果融合: 向量结果={VectorCount}, 图谱结果={GraphCount}",
                vectorResults.Count, graphResults.Count);

            try
            {
                switch (options.FusionStrategy)
                {
                    case FusionStrategy.WeightedSum:
                        return WeightedSumFusion(options, vectorResults, graphResults);

                    case FusionStrategy.ReciprocalRankFusion:
                        return ReciprocalRankFusion(options, vectorResults, graphResults);

                    case FusionStrategy.Condorcet:
                        return CondorcetFusion(options, vectorResults, graphResults);

                    case FusionStrategy.LearningToRank:
                        // TODO: 实现学习排序融合
                        return WeightedSumFusion(options, vectorResults, graphResults);

                    default:
                        return WeightedSumFusion(options, vectorResults, graphResults);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "结果融合失败: {Message}", ex.Message);
                return vectorResults; // 返回向量检索结果作为后备
            }
        }

        /// <summary>
        /// 加权求和融合
        /// </summary>
        private List<SearchResult> WeightedSumFusion(
            HybridSearchOptions options,
            List<SearchResult> vectorResults,
            List<GraphSearchResult> graphResults)
        {
            var fusedResults = new Dictionary<string, SearchResult>();

            // 归一化权重
            var totalWeight = options.VectorWeight + options.GraphWeight;
            var normalizedVectorWeight = options.VectorWeight / totalWeight;
            var normalizedGraphWeight = options.GraphWeight / totalWeight;

            // 添加向量检索结果
            foreach (var result in vectorResults)
            {
                result.FusedScore = result.Score * normalizedVectorWeight;
                result.Source = "vector";
                fusedResults[result.ChunkId] = result;
            }

            // 添加图谱检索结果
            foreach (var graphResult in graphResults)
            {
                foreach (var chunkId in graphResult.MatchingChunkIds)
                {
                    if (fusedResults.ContainsKey(chunkId))
                    {
                        // 已存在，累加图谱得分
                        fusedResults[chunkId].FusedScore += graphResult.Score * normalizedGraphWeight;
                        fusedResults[chunkId].Source = "hybrid";
                    }
                    else
                    {
                        // 新增结果
                        var newResult = new SearchResult
                        {
                            ChunkId = chunkId,
                            Score = 0, // 图谱检索没有原始分数
                            FusedScore = graphResult.Score * normalizedGraphWeight,
                            Source = "graph",
                            RelatedPaths = graphResult.RelatedPaths
                        };
                        fusedResults[chunkId] = newResult;
                    }
                }
            }

            return fusedResults.Values.OrderByDescending(r => r.FusedScore).ToList();
        }

        /// <summary>
        /// 倒数排名融合（RRF）
        /// </summary>
        private List<SearchResult> ReciprocalRankFusion(
            HybridSearchOptions options,
            List<SearchResult> vectorResults,
            List<GraphSearchResult> graphResults)
        {
            const int k = 60; // RRF常数
            var fusedScores = new Dictionary<string, float>();
            var fusedResults = new Dictionary<string, SearchResult>();

            // 计算向量检索的RRF分数
            for (int i = 0; i < vectorResults.Count; i++)
            {
                var chunkId = vectorResults[i].ChunkId;
                var score = 1f / (k + i + 1);

                fusedScores.TryGetValue(chunkId, out var currentScore);
                fusedScores[chunkId] = currentScore + score;

                if (!fusedResults.ContainsKey(chunkId))
                {
                    var result = vectorResults[i];
                    result.Source = "vector";
                    fusedResults[chunkId] = result;
                }
                else
                {
                    fusedResults[chunkId].Source = "hybrid";
                }
            }

            // 计算图谱检索的RRF分数
            var rank = 0;
            foreach (var graphResult in graphResults.OrderByDescending(r => r.Score))
            {
                foreach (var chunkId in graphResult.MatchingChunkIds)
                {
                    var score = 1f / (k + rank + 1);

                    fusedScores.TryGetValue(chunkId, out var currentScore);
                    fusedScores[chunkId] = currentScore + score;

                    if (!fusedResults.ContainsKey(chunkId))
                    {
                        var newResult = new SearchResult
                        {
                            ChunkId = chunkId,
                            Score = graphResult.Score,
                            FusedScore = 0,
                            Source = "graph",
                            RelatedPaths = graphResult.RelatedPaths
                        };
                        fusedResults[chunkId] = newResult;
                    }
                    else
                    {
                        fusedResults[chunkId].Source = "hybrid";
                        fusedResults[chunkId].RelatedPaths = graphResult.RelatedPaths;
                    }
                }
                rank++;
            }

            // 更新融合分数
            foreach (var kvp in fusedScores)
            {
                if (fusedResults.ContainsKey(kvp.Key))
                {
                    fusedResults[kvp.Key].FusedScore = kvp.Value;
                }
            }

            return fusedResults.Values.OrderByDescending(r => r.FusedScore).ToList();
        }

        /// <summary>
        /// Condorcet投票融合
        /// </summary>
        private List<SearchResult> CondorcetFusion(
            HybridSearchOptions options,
            List<SearchResult> vectorResults,
            List<GraphSearchResult> graphResults)
        {
            // 简化实现：基于在两个列表中出现的次数
            var votes = new Dictionary<string, int>();

            // 向量检索投票
            foreach (var result in vectorResults)
            {
                votes.TryGetValue(result.ChunkId, out var currentVotes);
                votes[result.ChunkId] = currentVotes + 1;
            }

            // 图谱检索投票
            foreach (var graphResult in graphResults)
            {
                foreach (var chunkId in graphResult.MatchingChunkIds)
                {
                    votes.TryGetValue(chunkId, out var currentVotes);
                    votes[chunkId] = currentVotes + 1;
                }
            }

            // 构建融合结果
            var fusedResults = new List<SearchResult>();

            // 添加向量检索结果
            foreach (var result in vectorResults)
            {
                result.FusedScore = votes.GetValueOrDefault(result.ChunkId, 0);
                result.Source = "vector";
                fusedResults.Add(result);
            }

            // 添加仅在图谱检索中的结果
            foreach (var graphResult in graphResults)
            {
                foreach (var chunkId in graphResult.MatchingChunkIds)
                {
                    if (!fusedResults.Any(r => r.ChunkId == chunkId))
                    {
                        fusedResults.Add(new SearchResult
                        {
                            ChunkId = chunkId,
                            Score = graphResult.Score,
                            FusedScore = votes.GetValueOrDefault(chunkId, 0),
                            Source = "graph",
                            RelatedPaths = graphResult.RelatedPaths
                        });
                    }
                    else
                    {
                        var existing = fusedResults.First(r => r.ChunkId == chunkId);
                        existing.Source = "hybrid";
                        existing.RelatedPaths = graphResult.RelatedPaths;
                    }
                }
            }

            return fusedResults.OrderByDescending(r => r.FusedScore).ToList();
        }

        /// <summary>
        /// 提取相关路径
        /// </summary>
        private async Task<List<GraphPath>> ExtractRelatedPaths(
            List<GraphSearchResult> graphResults,
            CancellationToken cancellationToken)
        {
            var allPaths = new List<GraphPath>();

            foreach (var result in graphResults)
            {
                allPaths.AddRange(result.RelatedPaths);
            }

            // 去重
            var uniquePaths = allPaths.GroupBy(p => string.Join(",", p.Nodes.Select(n => n.Id)))
                .Select(g => g.First())
                .ToList();

            return await Task.FromResult(uniquePaths);
        }

        #endregion

        #region 重排序

        /// <summary>
        /// 重排序
        /// </summary>
        public async Task<List<SearchResult>> RerankAsync(
            string query,
            List<SearchResult> results,
            RerankOptions options,
            CancellationToken cancellationToken = default)
        {
            if (results.Count == 0)
                return results;

            _logger.LogDebug("开始重排序: 方法={Method}, 结果数={Count}",
                options.Method, results.Count);

            try
            {
                switch (options.Method)
                {
                    case RerankMethod.CrossEncoder:
                        return await CrossEncoderRerank(query, results, options, cancellationToken);

                    case RerankMethod.LLMRerank:
                        return await LLMRerank(query, results, options, cancellationToken);

                    case RerankMethod.RuleBased:
                        return RuleBasedRerank(query, results);

                    default:
                        return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重排序失败: {Message}", ex.Message);
                return results;
            }
        }

        /// <summary>
        /// 交叉编码器重排序
        /// 注意：这是一个基于向量相似度的近似实现。
        /// 真正的交叉编码器需要专门的ML模型来评分query-document对，
        /// 这通常需要额外的模型服务（如Cohere Rerank API、BGE Reranker等）。
        /// 当前实现使用查询-文档向量相似度作为reranking信号。
        /// </summary>
        private async Task<List<SearchResult>> CrossEncoderRerank(
            string query,
            List<SearchResult> results,
            RerankOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                if (results.Count == 0)
                {
                    _logger.LogWarning("交叉编码器重排序：结果为空");
                    return results;
                }

                _logger.LogDebug("开始交叉编码器重排序: 查询='{Query}', 结果数={Count}", query, results.Count);

                // 生成查询的向量
                var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);
                if (queryEmbedding == null || queryEmbedding.Length == 0)
                {
                    _logger.LogWarning("无法生成查询向量，返回原始结果");
                    return results;
                }

                // 为每个文档生成向量并计算相似度
                var rerankedResults = new List<SearchResult>();
                var documents = results.Select(r => r.Content).ToArray();

                // 批量生成文档向量
                var documentEmbeddings = await _embeddingService.GetEmbeddingsAsync(documents, cancellationToken);

                if (documentEmbeddings == null || documentEmbeddings.Length != results.Count)
                {
                    _logger.LogWarning("文档向量生成失败，返回原始结果");
                    return results;
                }

                // 计算每个文档与查询的相似度并更新分数
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    var docEmbedding = documentEmbeddings[i];

                    if (docEmbedding != null && docEmbedding.Length > 0)
                    {
                        // 计算余弦相似度
                        var similarity = ComputeCosineSimilarity(queryEmbedding, docEmbedding);

                        // 结合原始分数和新相似度分数
                        // 使用加权组合：原始分数占40%，新相似度占60%
                        var combinedScore = result.Score * 0.4f + similarity * 0.6f;

                        result.FusedScore = combinedScore;
                        result.Score = combinedScore;
                        result.Metadata["cross_encoder_score"] = similarity;
                        result.Metadata["original_score"] = results[i].Score;
                    }

                    rerankedResults.Add(result);
                }

                // 按新的分数排序
                var sortedResults = rerankedResults
                    .OrderByDescending(r => r.Score)
                    .Take(options.TopK)
                    .ToList();

                _logger.LogInformation("交叉编码器重排序完成: 输入={InputCount}, 输出={OutputCount}",
                    results.Count, sortedResults.Count);

                return sortedResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "交叉编码器重排序失败: {Message}", ex.Message);
                return results.Take(options.TopK).ToList();
            }
        }

        /// <summary>
        /// 计算余弦相似度
        /// </summary>
        private float ComputeCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
                return 0f;

            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = (float)Math.Sqrt(magnitude1);
            magnitude2 = (float)Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0f;

            return dotProduct / (magnitude1 * magnitude2);
        }

        /// <summary>
        /// LLM重排序
        /// 注意：这是一个简化实现框架。
        /// 真正的LLM重排序需要：
        /// 1. 集成LLM服务（如OpenAI API、本地LLM等）
        /// 2. 构建专门的prompt让LLM评估query-document相关性
        /// 3. 解析LLM返回的评分并重排序结果
        ///
        /// 当前实现使用增强的规则方法作为占位符，
        /// 包含查询-文档语义匹配度计算。
        /// </summary>
        private async Task<List<SearchResult>> LLMRerank(
            string query,
            List<SearchResult> results,
            RerankOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                if (results.Count == 0)
                {
                    _logger.LogWarning("LLM重排序：结果为空");
                    return results;
                }

                _logger.LogDebug("开始LLM重排序: 查询='{Query}', 结果数={Count}", query, results.Count);

                // TODO: 在实际生产环境中，这里应该调用LLM API
                // 示例实现框架：
                //
                // var prompt = BuildRerankingPrompt(query, results);
                // var llmResponse = await _llmService.CompleteAsync(prompt, cancellationToken);
                // var scores = ParseLLMScores(llmResponse);
                //
                // 由于当前没有集成直接的LLM服务到HybridSearchService中，
                // 我们使用增强的规则方法作为替代

                // 生成查询向量用于语义匹配
                float[]? queryEmbedding = null;
                try
                {
                    queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "无法生成查询向量，使用基础重排序");
                }

                var queryLower = query.ToLower();
                var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // 计算每个结果的LLM风格评分
                var scoredResults = new List<(SearchResult result, float llmScore)>();

                foreach (var result in results)
                {
                    float llmScore = 0f;
                    var contentLower = result.Content.ToLower();

                    // 1. 精确匹配得分 (权重: 0.3)
                    if (contentLower.Contains(queryLower))
                    {
                        llmScore += 0.3f;
                    }

                    // 2. 词汇匹配度 (权重: 0.3)
                    var matchedTerms = queryTerms.Count(term => contentLower.Contains(term));
                    var termMatchRatio = (float)matchedTerms / queryTerms.Length;
                    llmScore += termMatchRatio * 0.3f;

                    // 3. 语义相似度 (权重: 0.2)
                    if (queryEmbedding != null)
                    {
                        try
                        {
                            var documents = new[] { result.Content };
                            var docEmbeddings = await _embeddingService.GetEmbeddingsAsync(documents, cancellationToken);
                            if (docEmbeddings != null && docEmbeddings.Length > 0 && docEmbeddings[0] != null)
                            {
                                var semanticSim = ComputeCosineSimilarity(queryEmbedding, docEmbeddings[0]);
                                llmScore += semanticSim * 0.2f;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "计算语义相似度失败");
                        }
                    }

                    // 4. 原始分数贡献 (权重: 0.2)
                    llmScore += result.Score * 0.2f;

                    // 存储评分
                    result.Metadata["llm_rerank_score"] = llmScore;
                    result.Metadata["original_score"] = result.Score;

                    scoredResults.Add((result, llmScore));
                }

                // 按LLM评分排序
                var rerankedResults = scoredResults
                    .OrderByDescending(x => x.llmScore)
                    .Select(x => x.result)
                    .ToList();

                // 更新分数并返回TopK
                foreach (var result in rerankedResults)
                {
                    result.Score = (float)result.Metadata["llm_rerank_score"];
                    result.FusedScore = result.Score;
                }

                var finalResults = rerankedResults.Take(options.TopK).ToList();

                _logger.LogInformation("LLM重排序完成: 输入={InputCount}, 输出={OutputCount}",
                    results.Count, finalResults.Count);

                return finalResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM重排序失败: {Message}", ex.Message);
                return results.Take(options.TopK).ToList();
            }
        }

        /// <summary>
        /// 规则重排序
        /// </summary>
        private List<SearchResult> RuleBasedRerank(string query, List<SearchResult> results)
        {
            var queryLower = query.ToLower();

            foreach (var result in results)
            {
                var rerankScore = result.FusedScore;

                // 规则1: 混合来源加分
                if (result.Source == "hybrid")
                {
                    rerankScore *= 1.2f;
                }

                // 规则2: 有相关路径加分
                if (result.RelatedPaths.Count > 0)
                {
                    rerankScore *= 1.1f;
                }

                // 规则3: 查询词在内容中匹配加分
                if (!string.IsNullOrEmpty(result.Content) && result.Content.ToLower().Contains(queryLower))
                {
                    rerankScore *= 1.15f;
                }

                result.FusedScore = rerankScore;
            }

            return results.OrderByDescending(r => r.FusedScore).ToList();
        }

        #endregion

        #region 辅助方法

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

        #endregion
    }
}
