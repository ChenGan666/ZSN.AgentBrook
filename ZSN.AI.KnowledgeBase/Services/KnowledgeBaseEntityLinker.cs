using Microsoft.Extensions.Logging;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.Core.Interface;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 知识库实体链接器
    /// 将新提取的实体链接到知识库中已有的实体，建立实体关联
    /// </summary>
    public class KnowledgeBaseEntityLinker
    {
        private readonly IGraphRepository _graphRepository;
        private readonly IEmbeddingService? _embeddingService;
        private readonly IChatService? _chatService;
        private readonly ILogger<KnowledgeBaseEntityLinker> _logger;
        private readonly string _graphName;

        public KnowledgeBaseEntityLinker(
            IGraphRepository graphRepository,
            IEmbeddingService? embeddingService,
            IChatService? chatService,
            ILogger<KnowledgeBaseEntityLinker> logger,
            string graphName)
        {
            _graphRepository = graphRepository;
            _embeddingService = embeddingService;
            _chatService = chatService;
            _logger = logger;
            _graphName = graphName;
        }

        /// <summary>
        /// 链接实体到知识库
        /// </summary>
        public async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> LinkToKnowledgeBaseAsync(
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            string knowledgeBaseId,
            bool useLLMForLinking = false,
            float similarityThreshold = 0.90f,
            CancellationToken cancellationToken = default)
        {
            if (entities.Count == 0)
                return entities;

            _logger.LogInformation("开始链接 {Count} 个实体到知识库 {KnowledgeBaseId}",
                entities.Count, knowledgeBaseId);

            var linkedEntities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
            var linkStats = new Dictionary<string, int>
            {
                { "matched", 0 },
                { "unmatched", 0 },
                { "merged", 0 }
            };

            foreach (var entity in entities)
            {
                var linkedEntity = await LinkSingleEntityAsync(
                    entity,
                    knowledgeBaseId,
                    useLLMForLinking,
                    similarityThreshold,
                    cancellationToken);

                linkedEntities.Add(linkedEntity);

                if (linkedEntity.Id != entity.Id)
                {
                    linkStats["matched"]++;
                    linkStats["merged"]++;
                }
                else
                {
                    linkStats["unmatched"]++;
                }
            }

            _logger.LogInformation("实体链接完成: 匹配={Matched}, 未匹配={Unmatched}, 合并={Merged}",
                linkStats["matched"], linkStats["unmatched"], linkStats["merged"]);

            return linkedEntities;
        }

        /// <summary>
        /// 链接单个实体到知识库
        /// </summary>
        private async Task<ZSN.AI.Entity.KnowledgeBase.Entity> LinkSingleEntityAsync(
            ZSN.AI.Entity.KnowledgeBase.Entity entity,
            string knowledgeBaseId,
            bool useLLMForLinking,
            float similarityThreshold,
            CancellationToken cancellationToken)
        {
            try
            {
                // 步骤1：查询知识库中相似的同类型实体
                var similarEntities = await FindSimilarEntitiesInKnowledgeBaseAsync(
                    entity,
                    knowledgeBaseId,
                    similarityThreshold,
                    cancellationToken);

                if (similarEntities.Count == 0)
                {
                    // 未找到相似实体，返回原实体
                    _logger.LogDebug("实体 '{Text}' ({Type}) 在知识库中未找到匹配实体",
                        entity.Text, entity.Type);
                    return entity;
                }

                // 步骤2：选择最相似的实体
                var bestMatch = similarEntities.OrderByDescending(e => e.Similarity).First();

                // 步骤3：使用LLM判断是否为同一实体（可选）
                bool isSameEntity = true;
                if (useLLMForLinking && _chatService != null)
                {
                    isSameEntity = await VerifyEntityMatchWithLLMAsync(
                        entity,
                        bestMatch,
                        cancellationToken);
                }

                if (isSameEntity && bestMatch.Similarity >= similarityThreshold)
                {
                    // 链接成功，使用知识库中的实体
                    _logger.LogDebug("实体 '{Text}' ({Type}) 链接到知识库实体 {EntityId} (相似度: {Similarity:F2})",
                        entity.Text, entity.Type, bestMatch.Id, bestMatch.Similarity);

                    // 更新知识库实体的属性
                    await UpdateKnowledgeBaseEntityAsync(
                        bestMatch.Id,
                        entity,
                        cancellationToken);

                    // 返回知识库实体
                    return new ZSN.AI.Entity.KnowledgeBase.Entity
                    {
                        Id = bestMatch.Id,
                        Text = bestMatch.Text,
                        Type = bestMatch.Type,
                        Confidence = Math.Max(entity.Confidence, bestMatch.Confidence),
                        Attributes = MergeAttributes(bestMatch.Attributes, entity.Attributes),
                        SourceChunkIds = new List<string>(entity.SourceChunkIds),
                        StartPosition = entity.StartPosition,
                        EndPosition = entity.EndPosition
                    };
                }
                else
                {
                    // 未匹配，返回原实体
                    _logger.LogDebug("实体 '{Text}' ({Type}) 未通过匹配验证",
                        entity.Text, entity.Type);
                    return entity;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "链接实体 '{Text}' 到知识库失败", entity.Text);
                return entity;
            }
        }

        /// <summary>
        /// 在知识库中查找相似实体
        /// </summary>
        private async Task<List<KnowledgeBaseEntity>> FindSimilarEntitiesInKnowledgeBaseAsync(
            ZSN.AI.Entity.KnowledgeBase.Entity entity,
            string knowledgeBaseId,
            float similarityThreshold,
            CancellationToken cancellationToken)
        {
            var similarEntities = new List<KnowledgeBaseEntity>();

            try
            {
                // 策略1：精确文本匹配
                var exactMatchQuery = $@"
                    MATCH (e:Entity)
                    WHERE e.type = '{entity.Type}' AND e.text = '{entity.Text.Replace("'", "\\'")}'
                    RETURN e.id as id, e.text as text, e.type as type, e.confidence as confidence,
                           e.attributes as attributes
                    LIMIT 5
                ";

                var exactResults = await _graphRepository.ExecuteCypherAsync(
                    _graphName, exactMatchQuery, null, cancellationToken);

                foreach (var row in exactResults)
                {
                    var kbEntity = ParseKnowledgeBaseEntity(row);
                    if (kbEntity != null)
                    {
                        kbEntity.Similarity = 1.0f;
                        similarEntities.Add(kbEntity);
                    }
                }

                if (similarEntities.Count > 0)
                    return similarEntities;

                // 策略2：别名匹配
                var aliasMatchQuery = $@"
                    MATCH (e:Entity)
                    WHERE e.type = '{entity.Type}'
                      AND (e.text = '{entity.Text.Replace("'", "\\'")}'
                           OR e.attributes包含 '{entity.Text.Replace("'", "\\'")}'
                           OR '{entity.Text.Replace("'", "\\'")}' IN e.attributes)
                    RETURN e.id as id, e.text as text, e.type as type, e.confidence as confidence,
                           e.attributes as attributes
                    LIMIT 10
                ";

                var aliasResults = await _graphRepository.ExecuteCypherAsync(
                    _graphName, aliasMatchQuery, null, cancellationToken);

                foreach (var row in aliasResults)
                {
                    var kbEntity = ParseKnowledgeBaseEntity(row);
                    if (kbEntity != null && kbEntity.Text != entity.Text)
                    {
                        kbEntity.Similarity = 0.95f; // 别名匹配给予高相似度
                        similarEntities.Add(kbEntity);
                    }
                }

                // 策略3：语义相似度匹配（如果有embedding服务）
                if (_embeddingService != null && similarEntities.Count == 0)
                {
                    var semanticMatches = await FindSemanticMatchesAsync(
                        entity,
                        knowledgeBaseId,
                        similarityThreshold,
                        cancellationToken);

                    similarEntities.AddRange(semanticMatches);
                }

                _logger.LogDebug("在知识库中找到 {Count} 个相似实体（类型: {Type}）",
                    similarEntities.Count, entity.Type);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "查找知识库中相似实体失败");
            }

            return similarEntities;
        }

        /// <summary>
        /// 使用语义相似度查找匹配
        /// </summary>
        private async Task<List<KnowledgeBaseEntity>> FindSemanticMatchesAsync(
            ZSN.AI.Entity.KnowledgeBase.Entity entity,
            string knowledgeBaseId,
            float similarityThreshold,
            CancellationToken cancellationToken)
        {
            var matches = new List<KnowledgeBaseEntity>();

            try
            {
                // 生成查询实体的embedding
                var queryEmbedding = await _embeddingService!.GetEmbeddingAsync(
                    entity.Text, cancellationToken);

                if (queryEmbedding == null || queryEmbedding.Length == 0)
                    return matches;

                // 查询知识库中同类型的所有实体
                var query = $@"
                    MATCH (e:Entity)
                    WHERE e.type = '{entity.Type}'
                    RETURN e.id as id, e.text as text, e.type as type, e.confidence as confidence,
                           e.attributes as attributes
                    LIMIT 100
                ";

                var results = await _graphRepository.ExecuteCypherAsync(
                    _graphName, query, null, cancellationToken);

                foreach (var row in results)
                {
                    var kbEntity = ParseKnowledgeBaseEntity(row);
                    if (kbEntity == null)
                        continue;

                    // 计算语义相似度
                    var kbEmbedding = await _embeddingService.GetEmbeddingAsync(
                        kbEntity.Text, cancellationToken);

                    if (kbEmbedding != null && kbEmbedding.Length > 0)
                    {
                        var similarity = CosineSimilarity(queryEmbedding, kbEmbedding);
                        if (similarity >= similarityThreshold)
                        {
                            kbEntity.Similarity = similarity;
                            matches.Add(kbEntity);
                        }
                    }
                }

                _logger.LogDebug("语义匹配找到 {Count} 个实体（阈值: {Threshold:F2})",
                    matches.Count, similarityThreshold);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "语义匹配失败");
            }

            return matches;
        }

        /// <summary>
        /// 使用LLM验证实体匹配
        /// </summary>
        private async Task<bool> VerifyEntityMatchWithLLMAsync(
            ZSN.AI.Entity.KnowledgeBase.Entity newEntity,
            KnowledgeBaseEntity kbEntity,
            CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"
你是一个实体链接专家。判断以下两个实体是否为同一个实体的不同表述。

新实体：
- 文本：{newEntity.Text}
- 类型：{newEntity.Type}
- 属性：{FormatAttributes(newEntity.Attributes)}

知识库实体：
- 文本：{kbEntity.Text}
- 类型：{kbEntity.Type}
- 属性：{FormatAttributes(kbEntity.Attributes)}

请判断这两个实体是否表示同一个现实世界对象。
如果它们是同一实体，返回true。
如果不是同一实体，返回false。

只返回true或false，不要返回其他内容。
";

                // TODO: 实际调用LLM API
                // 这里简化处理，直接基于相似度判断
                await Task.CompletedTask;

                return kbEntity.Similarity >= 0.90f;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM验证失败，默认返回false");
                return false;
            }
        }

        /// <summary>
        /// 更新知识库实体
        /// </summary>
        private async Task UpdateKnowledgeBaseEntityAsync(
            string entityId,
            ZSN.AI.Entity.KnowledgeBase.Entity newEntity,
            CancellationToken cancellationToken)
        {
            try
            {
                // 合并属性
                var mergedAttributes = new Dictionary<string, string>();

                // 添加新属性
                foreach (var attr in newEntity.Attributes)
                {
                    if (!string.IsNullOrWhiteSpace(attr.Value))
                    {
                        mergedAttributes[attr.Key] = attr.Value;
                    }
                }

                // 构建更新Cypher
                var setClauses = new List<string>();
                foreach (var attr in mergedAttributes)
                {
                    setClauses.Add($"e.attributes['{attr.Key}'] = '{attr.Value.Replace("'", "\\'")}'");
                }

                if (setClauses.Count > 0)
                {
                    var query = $@"
                        MATCH (e:Entity {{id: '{entityId}'}})
                        {string.Join("\n", setClauses.Select((s, i) => i == 0 ? "SET " + s : ", " + s))}
                        RETURN e
                    ";

                    await _graphRepository.ExecuteCypherAsync(_graphName, query, null, cancellationToken);
                    _logger.LogDebug("更新知识库实体 {EntityId} 的属性", entityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "更新知识库实体 {EntityId} 失败", entityId);
            }
        }

        /// <summary>
        /// 解析知识库实体
        /// </summary>
        private KnowledgeBaseEntity? ParseKnowledgeBaseEntity(Dictionary<string, object> row)
        {
            try
            {
                return new KnowledgeBaseEntity
                {
                    Id = row.GetValueOrDefault("id", "")?.ToString() ?? string.Empty,
                    Text = row.GetValueOrDefault("text", "")?.ToString() ?? string.Empty,
                    Type = row.GetValueOrDefault("type", "")?.ToString() ?? string.Empty,
                    Confidence = float.TryParse(row.GetValueOrDefault("confidence", "0")?.ToString(), out var conf) ? conf : 0f,
                    Attributes = ParseAttributes(row.GetValueOrDefault("attributes", "")?.ToString())
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析知识库实体失败");
                return null;
            }
        }

        /// <summary>
        /// 解析属性
        /// </summary>
        private Dictionary<string, string> ParseAttributes(string? attributesStr)
        {
            var attributes = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(attributesStr))
                return attributes;

            try
            {
                // 简化处理，实际应该解析JSON
                // 这里假设attributesStr是简单的key=value格式
                var pairs = attributesStr.Split(';', ',');
                foreach (var pair in pairs)
                {
                    var parts = pair.Split('=');
                    if (parts.Length == 2)
                    {
                        attributes[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析属性失败: {Attributes}", attributesStr);
            }

            return attributes;
        }

        /// <summary>
        /// 合并属性
        /// </summary>
        private Dictionary<string, string> MergeAttributes(
            Dictionary<string, string> baseAttrs,
            Dictionary<string, string> newAttrs)
        {
            var merged = new Dictionary<string, string>(baseAttrs);

            foreach (var attr in newAttrs)
            {
                if (!merged.ContainsKey(attr.Key))
                {
                    merged[attr.Key] = attr.Value;
                }
            }

            return merged;
        }

        /// <summary>
        /// 格式化属性
        /// </summary>
        private string FormatAttributes(Dictionary<string, string> attributes)
        {
            if (attributes.Count == 0)
                return "无";

            return string.Join(", ", attributes.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        /// <summary>
        /// 计算余弦相似度
        /// </summary>
        private float CosineSimilarity(float[] vector1, float[] vector2)
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
        /// 知识库实体
        /// </summary>
        private class KnowledgeBaseEntity
        {
            public string Id { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public float Confidence { get; set; }
            public Dictionary<string, string> Attributes { get; set; } = new();
            public float Similarity { get; set; }
        }
    }
}
