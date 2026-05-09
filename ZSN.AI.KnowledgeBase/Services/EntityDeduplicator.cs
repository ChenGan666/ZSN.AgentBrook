using Microsoft.Extensions.Logging;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.Core.Interface;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 实体去重器
    /// 提供多层次实体去重和合并功能
    /// </summary>
    public class EntityDeduplicator
    {
        private readonly IEmbeddingService? _embeddingService;
        private readonly ILogger<EntityDeduplicator> _logger;

        public EntityDeduplicator(
            IEmbeddingService? embeddingService,
            ILogger<EntityDeduplicator> logger)
        {
            _embeddingService = embeddingService;
            _logger = logger;
        }

        /// <summary>
        /// 实体去重和合并（多层次匹配策略）
        /// </summary>
        public async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> DeduplicateEntitiesAsync(
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            float similarityThreshold = 0.85f,
            CancellationToken cancellationToken = default)
        {
            if (entities.Count == 0)
                return entities;

            _logger.LogInformation("开始多层次实体去重，原始实体数: {Count}", entities.Count);

            // 按类型分组
            var groupedEntities = entities.GroupBy(e => e.Type).ToList();
            var deduplicatedEntities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();

            foreach (var group in groupedEntities)
            {
                var groupEntities = group.ToList();
                var merged = await DeduplicateEntityGroupAsync(groupEntities, similarityThreshold, cancellationToken);
                deduplicatedEntities.AddRange(merged);
            }

            _logger.LogInformation("去重后实体数: {Count}，去除了{Removed}个实体",
                deduplicatedEntities.Count, entities.Count - deduplicatedEntities.Count);

            return deduplicatedEntities;
        }

        /// <summary>
        /// 对同类型实体组进行去重
        /// </summary>
        private async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> DeduplicateEntityGroupAsync(
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            float similarityThreshold,
            CancellationToken cancellationToken)
        {
            var merged = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
            var processed = new HashSet<int>();

            for (int i = 0; i < entities.Count; i++)
            {
                if (processed.Contains(i))
                    continue;

                var currentEntity = entities[i];
                var similarEntities = new List<ZSN.AI.Entity.KnowledgeBase.Entity> { currentEntity };

                // 查找相似实体
                for (int j = i + 1; j < entities.Count; j++)
                {
                    if (processed.Contains(j))
                        continue;

                    var candidateEntity = entities[j];
                    var similarity = await CalculateEntitySimilarityAsync(
                        currentEntity,
                        candidateEntity,
                        cancellationToken);

                    if (similarity >= similarityThreshold)
                    {
                        similarEntities.Add(candidateEntity);
                        processed.Add(j);
                        _logger.LogDebug("发现相似实体: {Entity1} <-> {Entity2}, 相似度: {Similarity:F2}",
                            currentEntity.Text, candidateEntity.Text, similarity);
                    }
                }

                // 合并相似实体
                var mergedEntity = MergeSimilarEntities(similarEntities);
                merged.Add(mergedEntity);
                processed.Add(i);
            }

            return merged;
        }

        /// <summary>
        /// 计算实体相似度（多层次策略）
        /// </summary>
        private async Task<float> CalculateEntitySimilarityAsync(
            ZSN.AI.Entity.KnowledgeBase.Entity entity1,
            ZSN.AI.Entity.KnowledgeBase.Entity entity2,
            CancellationToken cancellationToken)
        {
            // 层次1：类型必须相同
            if (entity1.Type != entity2.Type)
                return 0f;

            // 层次2：精确字符串匹配
            if (entity1.Text == entity2.Text)
                return 1.0f;

            // 层次3：规范化后的字符串匹配
            var normalizedText1 = NormalizeEntityText(entity1.Text);
            var normalizedText2 = NormalizeEntityText(entity2.Text);

            if (normalizedText1 == normalizedText2)
                return 0.98f; // 规范化后完全相同

            // 层次4：编辑距离相似度
            var textSimilarity = CalculateTextSimilarity(entity1.Text, entity2.Text);
            if (textSimilarity >= 0.90f)
                return textSimilarity;

            // 层次5：检查别名关系
            if (IsAlias(entity1, entity2))
                return 0.92f;

            // 层次6：语义相似度（如果有embedding服务）
            if (_embeddingService != null)
            {
                try
                {
                    var semanticSimilarity = await CalculateSemanticSimilarityAsync(
                        entity1, entity2, cancellationToken);

                    // 如果语义相似度很高，结合文本相似度
                    if (semanticSimilarity >= 0.90f)
                    {
                        return (textSimilarity * 0.3f + semanticSimilarity * 0.7f);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "计算语义相似度失败，降级为文本相似度");
                }
            }

            // 默认返回文本相似度
            return textSimilarity;
        }

        /// <summary>
        /// 规范化实体文本（处理全角半角、大小写、空格等）
        /// </summary>
        private string NormalizeEntityText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 转换为半角
            text = text.Replace('（', '(').Replace('）', ')');
            text = text.Replace('，', ',').Replace('。', '.');

            // 去除空格
            text = text.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

            // 转小写（对英文）
            text = text.ToLowerInvariant();

            // 去除常见的标点符号（使用字符数组避免中文引号问题）
            var charsToTrim = new[] { '\'', '"', '`', '、', '；', ';' };
            text = text.Trim(charsToTrim);

            return text;
        }

        /// <summary>
        /// 检查是否为别名关系
        /// </summary>
        private bool IsAlias(ZSN.AI.Entity.KnowledgeBase.Entity entity1, ZSN.AI.Entity.KnowledgeBase.Entity entity2)
        {
            // 检查实体1的属性中是否包含实体2的文本作为别名
            if (entity1.Attributes.TryGetValue("别名", out var alias1))
            {
                if (alias1.Contains(entity2.Text) || entity2.Text.Contains(alias1))
                    return true;
            }

            // 检查实体2的属性中是否包含实体1的文本作为别名
            if (entity2.Attributes.TryGetValue("别名", out var alias2))
            {
                if (alias2.Contains(entity1.Text) || entity1.Text.Contains(alias2))
                    return true;
            }

            // 检查简称关系
            var shortForms = new[]
            {
                ("人工智能", "AI"), ("机器学习", "ML"), ("深度学习", "DL"),
                ("自然语言处理", "NLP"), ("计算机视觉", "CV"),
                ("中华人民共和国", "中国"), ("美利坚合众国", "美国")
            };

            foreach (var (full, shortForm) in shortForms)
            {
                if ((entity1.Text.Contains(full) || entity1.Text.Contains(shortForm)) &&
                    (entity2.Text.Contains(full) || entity2.Text.Contains(shortForm)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 计算文本相似度（编辑距离）
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
        /// 计算语义相似度（基于embedding）
        /// </summary>
        private async Task<float> CalculateSemanticSimilarityAsync(
            ZSN.AI.Entity.KnowledgeBase.Entity entity1,
            ZSN.AI.Entity.KnowledgeBase.Entity entity2,
            CancellationToken cancellationToken)
        {
            if (_embeddingService == null)
                return 0f;

            try
            {
                // 生成实体的embedding
                var embedding1 = await _embeddingService.GetEmbeddingAsync(
                    entity1.Text, cancellationToken);
                var embedding2 = await _embeddingService.GetEmbeddingAsync(
                    entity2.Text, cancellationToken);

                if (embedding1 == null || embedding2 == null ||
                    embedding1.Length == 0 || embedding2.Length == 0)
                {
                    return 0f;
                }

                // 计算余弦相似度
                return CosineSimilarity(embedding1, embedding2);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "计算语义相似度失败");
                return 0f;
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
        /// 合并相似实体
        /// </summary>
        private ZSN.AI.Entity.KnowledgeBase.Entity MergeSimilarEntities(List<ZSN.AI.Entity.KnowledgeBase.Entity> entities)
        {
            if (entities.Count == 0)
                throw new ArgumentException("实体列表不能为空", nameof(entities));

            // 选择置信度最高的作为主实体
            var primaryEntity = entities.OrderByDescending(e => e.Confidence).First();
            var mergedEntity = new ZSN.AI.Entity.KnowledgeBase.Entity
            {
                Id = primaryEntity.Id,
                Text = primaryEntity.Text,
                Type = primaryEntity.Type,
                Confidence = primaryEntity.Confidence,
                StartPosition = primaryEntity.StartPosition,
                EndPosition = primaryEntity.EndPosition
            };

            // 合并来源分块ID
            foreach (var entity in entities)
            {
                foreach (var chunkId in entity.SourceChunkIds)
                {
                    if (!mergedEntity.SourceChunkIds.Contains(chunkId))
                    {
                        mergedEntity.SourceChunkIds.Add(chunkId);
                    }
                }
            }

            // 合并属性
            foreach (var entity in entities)
            {
                foreach (var attr in entity.Attributes)
                {
                    // 如果属性已存在，合并值
                    if (mergedEntity.Attributes.TryGetValue(attr.Key, out var existingValue))
                    {
                        if (!existingValue.Contains(attr.Value))
                        {
                            mergedEntity.Attributes[attr.Key] = $"{existingValue}; {attr.Value}";
                        }
                    }
                    else
                    {
                        mergedEntity.Attributes[attr.Key] = attr.Value;
                    }
                }
            }

            // 合并别名
            var allAliases = new List<string>();
            foreach (var entity in entities)
            {
                if (entity.Attributes.TryGetValue("别名", out var alias))
                {
                    allAliases.Add(alias);
                }
                // 实体文本本身也是潜在别名
                if (entity.Text != mergedEntity.Text)
                {
                    allAliases.Add(entity.Text);
                }
            }

            if (allAliases.Count > 0)
            {
                var uniqueAliases = allAliases.Distinct().ToList();
                mergedEntity.Attributes["别名"] = string.Join("; ", uniqueAliases);
            }

            // 更新置信度（取最大值）
            mergedEntity.Confidence = entities.Max(e => e.Confidence);

            _logger.LogDebug("合并实体: 主实体={PrimaryText}, 合并数量={Count}, 最终别名={Aliases}",
                mergedEntity.Text, entities.Count, mergedEntity.Attributes.GetValueOrDefault("别名", "无"));

            return mergedEntity;
        }
    }
}
