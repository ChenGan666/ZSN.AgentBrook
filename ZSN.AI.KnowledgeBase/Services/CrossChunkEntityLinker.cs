using Microsoft.Extensions.Logging;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.Core.Interface;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 跨分块实体链接器
    /// 维护跨分块的实体一致性，将相同实体在不同分块中识别为同一实体
    /// </summary>
    public class CrossChunkEntityLinker
    {
        private readonly IChatService? _chatService;
        private readonly IEmbeddingService? _embeddingService;
        private readonly ILogger<CrossChunkEntityLinker> _logger;

        /// <summary>
        /// 全局实体池（跨所有已处理的分块）
        /// </summary>
        private readonly Dictionary<string, EntityPoolEntry> _globalEntityPool;

        public CrossChunkEntityLinker(
            IChatService? chatService,
            IEmbeddingService? embeddingService,
            ILogger<CrossChunkEntityLinker> logger)
        {
            _chatService = chatService;
            _embeddingService = embeddingService;
            _logger = logger;
            _globalEntityPool = new Dictionary<string, EntityPoolEntry>();
        }

        /// <summary>
        /// 实体池条目
        /// </summary>
        private class EntityPoolEntry
        {
            public string Id { get; set; } = string.Empty;
            public ZSN.AI.Entity.KnowledgeBase.Entity Entity { get; set; } = null!;
            public List<string> Aliases { get; set; } = new();
            public int ChunkCount { get; set; }
            public DateTime FirstSeenAt { get; set; }
        }

        /// <summary>
        /// 链接分块中的实体到全局实体池
        /// </summary>
        /// <param name="entities">当前分块的实体列表</param>
        /// <param name="chunkId">分块ID</param>
        /// <param name="useLLMForLinking">是否使用LLM进行链接判断</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>链接后的实体列表</returns>
        public async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> LinkEntitiesAsync(
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            string chunkId,
            bool useLLMForLinking = false,
            CancellationToken cancellationToken = default)
        {
            if (entities.Count == 0)
                return entities;

            _logger.LogInformation("开始链接分块 {ChunkId} 中的 {Count} 个实体到全局实体池",
                chunkId, entities.Count);

            var linkedEntities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();

            foreach (var entity in entities)
            {
                var linkedEntity = await LinkSingleEntityAsync(
                    entity, chunkId, useLLMForLinking, cancellationToken);

                linkedEntities.Add(linkedEntity);
            }

            _logger.LogInformation("分块 {ChunkId} 实体链接完成，全局实体池大小: {PoolSize}",
                chunkId, _globalEntityPool.Count);

            return linkedEntities;
        }

        /// <summary>
        /// 链接单个实体到全局实体池
        /// </summary>
        private async Task<ZSN.AI.Entity.KnowledgeBase.Entity> LinkSingleEntityAsync(
            ZSN.AI.Entity.KnowledgeBase.Entity entity,
            string chunkId,
            bool useLLMForLinking,
            CancellationToken cancellationToken)
        {
            // 在全局池中查找匹配的实体
            var matchedEntry = await FindMatchingEntityInPoolAsync(
                entity, useLLMForLinking, cancellationToken);

            if (matchedEntry != null)
            {
                // 找到匹配实体，合并信息
                _logger.LogDebug("实体 '{Text}' ({Type}) 与全局池中的实体 {PoolId} 匹配",
                    entity.Text, entity.Type, matchedEntry.Id);

                // 更新实体池条目
                matchedEntry.Entity.SourceChunkIds.Add(chunkId);
                matchedEntry.ChunkCount++;

                // 合并属性
                MergeEntityAttributes(matchedEntry.Entity, entity);

                // 添加别名
                if (entity.Text != matchedEntry.Entity.Text &&
                    !matchedEntry.Aliases.Contains(entity.Text))
                {
                    matchedEntry.Aliases.Add(entity.Text);
                }

                // 返回池中的实体（统一ID）
                var resultEntity = CloneEntity(matchedEntry.Entity);
                resultEntity.SourceChunkIds = new List<string> { chunkId };
                return resultEntity;
            }
            else
            {
                // 未找到匹配实体，添加到池中
                var newEntry = new EntityPoolEntry
                {
                    Id = entity.Id,
                    Entity = CloneEntity(entity),
                    Aliases = new List<string>(),
                    ChunkCount = 1,
                    FirstSeenAt = DateTime.UtcNow
                };
                newEntry.Entity.SourceChunkIds.Add(chunkId);

                // 使用规范化的文本作为键
                var poolKey = GeneratePoolKey(entity);
                _globalEntityPool[poolKey] = newEntry;

                _logger.LogDebug("实体 '{Text}' ({Type}) 作为新实体添加到全局池，PoolKey: {PoolKey}",
                    entity.Text, entity.Type, poolKey);

                return CloneEntity(entity);
            }
        }

        /// <summary>
        /// 在全局池中查找匹配的实体
        /// </summary>
        private async Task<EntityPoolEntry?> FindMatchingEntityInPoolAsync(
            ZSN.AI.Entity.KnowledgeBase.Entity entity,
            bool useLLMForLinking,
            CancellationToken cancellationToken)
        {
            if (_globalEntityPool.Count == 0)
                return null;

            // 筛选出相同类型的实体
            var sameTypeEntries = _globalEntityPool.Values
                .Where(e => e.Entity.Type == entity.Type)
                .ToList();

            if (sameTypeEntries.Count == 0)
                return null;

            // 层次1：精确文本匹配
            var exactMatch = sameTypeEntries.FirstOrDefault(e =>
                e.Entity.Text == entity.Text);

            if (exactMatch != null)
                return exactMatch;

            // 层次2：别名匹配
            var aliasMatch = sameTypeEntries.FirstOrDefault(e =>
                e.Aliases.Contains(entity.Text) ||
                entity.Attributes.TryGetValue("别名", out var alias) && e.Aliases.Contains(alias));

            if (aliasMatch != null)
                return aliasMatch;

            // 层次3：属性匹配（检查是否为同一实体的不同表述）
            var attrMatch = sameTypeEntries.FirstOrDefault(e =>
                EntitiesHaveSameAttributes(e.Entity, entity));

            if (attrMatch != null)
                return attrMatch;

            // 层次4：文本相似度匹配
            var similarityMatch = sameTypeEntries.FirstOrDefault(e =>
                CalculateTextSimilarity(e.Entity.Text, entity.Text) >= 0.85f);

            if (similarityMatch != null)
                return similarityMatch;

            // 层次5：语义相似度匹配（如果可用）
            if (_embeddingService != null)
            {
                try
                {
                    var semanticMatch = await FindSemanticMatchAsync(entity, sameTypeEntries, cancellationToken);
                    if (semanticMatch != null)
                        return semanticMatch;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "语义匹配失败");
                }
            }

            // 层次6：LLM判断（如果启用）
            if (useLLMForLinking && _chatService != null && sameTypeEntries.Count > 0)
            {
                try
                {
                    var llmMatch = await FindLLMMatchAsync(entity, sameTypeEntries, cancellationToken);
                    if (llmMatch != null)
                        return llmMatch;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LLM匹配失败");
                }
            }

            return null;
        }

        /// <summary>
        /// 生成实体池键
        /// </summary>
        private string GeneratePoolKey(ZSN.AI.Entity.KnowledgeBase.Entity entity)
        {
            // 使用类型+规范化文本作为键
            var normalizedText = NormalizeEntityText(entity.Text);
            return $"{entity.Type.ToUpperInvariant()}:{normalizedText}";
        }

        /// <summary>
        /// 规范化实体文本
        /// </summary>
        private string NormalizeEntityText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 转半角、小写、去空格
            text = text.Replace("（", "(").Replace("）", ")");
            text = text.Replace("，", ",").Replace("。", ".");
            text = text.Replace(" ", "").Replace("\t", "").Trim();
            text = text.ToLowerInvariant();

            return text;
        }

        /// <summary>
        /// 计算文本相似度
        /// </summary>
        private float CalculateTextSimilarity(string text1, string text2)
        {
            if (text1 == text2)
                return 1f;

            var len1 = text1.Length;
            var len2 = text2.Length;
            var maxLen = Math.Max(len1, len2);

            if (maxLen == 0)
                return 1f;

            var distance = LevenshteinDistance(text1, text2);
            return 1f - (float)distance / maxLen;
        }

        /// <summary>
        /// 计算编辑距离
        /// </summary>
        private int LevenshteinDistance(string s1, string s2)
        {
            var d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }

        /// <summary>
        /// 检查实体是否具有相同属性
        /// </summary>
        private bool EntitiesHaveSameAttributes(
            ZSN.AI.Entity.KnowledgeBase.Entity entity1,
            ZSN.AI.Entity.KnowledgeBase.Entity entity2)
        {
            // 检查是否有共同的别名
            var alias1 = entity1.Attributes.GetValueOrDefault("别名", "");
            var alias2 = entity2.Attributes.GetValueOrDefault("别名", "");

            if (!string.IsNullOrEmpty(alias1) && !string.IsNullOrEmpty(alias2))
            {
                if (alias1 == alias2 || alias2.Contains(alias1) || alias1.Contains(alias2))
                    return true;
            }

            // 检查其他关键属性
            var keyAttrs = new[] { "职位", "组织", "地点" };
            foreach (var attr in keyAttrs)
            {
                var val1 = entity1.Attributes.GetValueOrDefault(attr, "");
                var val2 = entity2.Attributes.GetValueOrDefault(attr, "");

                if (!string.IsNullOrEmpty(val1) && !string.IsNullOrEmpty(val2) && val1 == val2)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 使用语义相似度查找匹配
        /// </summary>
        private async Task<EntityPoolEntry?> FindSemanticMatchAsync(
            ZSN.AI.Entity.KnowledgeBase.Entity entity,
            List<EntityPoolEntry> candidates,
            CancellationToken cancellationToken)
        {
            if (_embeddingService == null)
                return null;

            try
            {
                var targetEmbedding = await _embeddingService.GetEmbeddingAsync(
                    entity.Text, cancellationToken);

                if (targetEmbedding == null || targetEmbedding.Length == 0)
                    return null;

                EntityPoolEntry? bestMatch = null;
                float bestSimilarity = 0.90f; // 语义相似度阈值

                foreach (var candidate in candidates)
                {
                    var candidateEmbedding = await _embeddingService.GetEmbeddingAsync(
                        candidate.Entity.Text, cancellationToken);

                    if (candidateEmbedding == null || candidateEmbedding.Length == 0)
                        continue;

                    var similarity = CosineSimilarity(targetEmbedding, candidateEmbedding);

                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestMatch = candidate;
                    }
                }

                if (bestMatch != null)
                {
                    _logger.LogDebug("实体 '{Text}' 与 '{MatchText}' 语义相似度: {Similarity:F2}",
                        entity.Text, bestMatch.Entity.Text, bestSimilarity);
                }

                return bestMatch;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "语义匹配失败");
                return null;
            }
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
        /// 使用LLM判断实体是否匹配
        /// </summary>
        private async Task<EntityPoolEntry?> FindLLMMatchAsync(
            ZSN.AI.Entity.KnowledgeBase.Entity entity,
            List<EntityPoolEntry> candidates,
            CancellationToken cancellationToken)
        {
            // 选择文本最相似的前5个候选
            var topCandidates = candidates
                .OrderByDescending(c => CalculateTextSimilarity(c.Entity.Text, entity.Text))
                .Take(5)
                .ToList();

            if (topCandidates.Count == 0)
                return null;

            // 构建判断Prompt
            var prompt = $@"
你是一个实体链接专家。判断以下实体是否为同一个实体的不同表述。

目标实体：
- 文本：{entity.Text}
- 类型：{entity.Type}
- 属性：{FormatAttributes(entity.Attributes)}

候选实体：
{FormatCandidates(topCandidates)}

请判断候选实体中是否有与目标实体相同的实体。
如果找到相同实体，返回其编号（1-{topCandidates.Count}）。
如果没有找到，返回0。

输出格式：只返回一个数字。
";

            // 这里简化处理，实际应该调用LLM API
            // 由于需要IChatService的完整实现，这里暂时返回null
            _logger.LogDebug("LLM匹配功能需要完整的IChatService实现");
            return null;
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
        /// 格式化候选实体列表
        /// </summary>
        private string FormatCandidates(List<EntityPoolEntry> candidates)
        {
            var lines = new List<string>();
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                lines.Add($"{i + 1}. 文本：{c.Entity.Text}，类型：{c.Entity.Type}，属性：{FormatAttributes(c.Entity.Attributes)}");
            }
            return string.Join("\n", lines);
        }

        /// <summary>
        /// 合并实体属性
        /// </summary>
        private void MergeEntityAttributes(
            ZSN.AI.Entity.KnowledgeBase.Entity target,
            ZSN.AI.Entity.KnowledgeBase.Entity source)
        {
            foreach (var attr in source.Attributes)
            {
                if (!target.Attributes.ContainsKey(attr.Key))
                {
                    target.Attributes[attr.Key] = attr.Value;
                }
                else if (!target.Attributes[attr.Key].Contains(attr.Value))
                {
                    target.Attributes[attr.Key] += "; " + attr.Value;
                }
            }

            // 更新置信度（取最大值）
            target.Confidence = Math.Max(target.Confidence, source.Confidence);
        }

        /// <summary>
        /// 克隆实体
        /// </summary>
        private ZSN.AI.Entity.KnowledgeBase.Entity CloneEntity(ZSN.AI.Entity.KnowledgeBase.Entity source)
        {
            return new ZSN.AI.Entity.KnowledgeBase.Entity
            {
                Id = source.Id,
                Text = source.Text,
                Type = source.Type,
                Confidence = source.Confidence,
                StartPosition = source.StartPosition,
                EndPosition = source.EndPosition,
                SourceChunkIds = new List<string>(source.SourceChunkIds),
                Attributes = new Dictionary<string, string>(source.Attributes)
            };
        }

        /// <summary>
        /// 获取全局实体池统计信息
        /// </summary>
        public Dictionary<string, int> GetPoolStatistics()
        {
            return _globalEntityPool.Values
                .GroupBy(e => e.Entity.Type)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );
        }

        /// <summary>
        /// 清空全局实体池
        /// </summary>
        public void ClearPool()
        {
            _globalEntityPool.Clear();
            _logger.LogInformation("全局实体池已清空");
        }

        /// <summary>
        /// 获取全局实体池中的所有实体
        /// </summary>
        public List<ZSN.AI.Entity.KnowledgeBase.Entity> GetAllPoolEntities()
        {
            return _globalEntityPool.Values
                .Select(e => CloneEntity(e.Entity))
                .ToList();
        }
    }
}
