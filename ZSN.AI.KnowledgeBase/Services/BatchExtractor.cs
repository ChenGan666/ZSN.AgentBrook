using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.Core.Interface;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 批处理配置
    /// </summary>
    public class BatchExtractionConfig
    {
        /// <summary>
        /// 批处理大小（每次合并的文本块数）
        /// </summary>
        public int BatchSize { get; set; } = 3;

        /// <summary>
        /// 单个请求的最大token数
        /// </summary>
        public int MaxTokensPerRequest { get; set; } = 12000;

        /// <summary>
        /// 是否启用批处理
        /// </summary>
        public bool EnableBatching { get; set; } = true;

        /// <summary>
        /// 文本块分隔符
        /// </summary>
        public string ChunkSeparator { get; set; } = "\n\n--- 文本分块分隔 ---\n\n";
    }

    /// <summary>
    /// 批处理提取器
    /// 支持批量处理多个文本块，减少API调用次数和成本
    /// </summary>
    public class BatchExtractor
    {
        private readonly IChatService? _chatService;
        private readonly ILogger<BatchExtractor> _logger;

        public BatchExtractor(
            IChatService? chatService,
            ILogger<BatchExtractor> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// 批量提取实体
        /// </summary>
        public async Task<List<List<ZSN.AI.Entity.KnowledgeBase.Entity>>> ExtractEntitiesBatchAsync(
            List<(string ChunkId, string Content)> chunks,
            EntityExtractionConfig config,
            BatchExtractionConfig batchConfig,
            CancellationToken cancellationToken = default)
        {
            if (chunks.Count == 0)
                return new List<List<ZSN.AI.Entity.KnowledgeBase.Entity>>();

            if (!batchConfig.EnableBatching || chunks.Count == 1)
            {
                // 单个处理或未启用批处理
                var results = new List<List<ZSN.AI.Entity.KnowledgeBase.Entity>>();
                foreach (var chunk in chunks)
                {
                    var entities = await ExtractEntitiesFromSingleChunkAsync(
                        chunk.Content, config, cancellationToken);
                    results.Add(entities);
                }
                return results;
            }

            _logger.LogInformation("开始批量提取实体，总块数: {TotalCount}, 批大小: {BatchSize}",
                chunks.Count, batchConfig.BatchSize);

            var allResults = new List<List<ZSN.AI.Entity.KnowledgeBase.Entity>>();

            // 将文本块分组
            var batches = CreateBatches(chunks, batchConfig);

            for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
            {
                var batch = batches[batchIndex];
                _logger.LogInformation("处理批次 {BatchIndex}/{TotalBatches}, 包含 {ChunkCount} 个文本块",
                    batchIndex + 1, batches.Count, batch.Count);

                // 批量提取
                var batchResults = await ExtractEntitiesFromBatchAsync(
                    batch, config, batchIndex, cancellationToken);

                allResults.AddRange(batchResults);
            }

            _logger.LogInformation("批量提取完成，共处理 {TotalCount} 个文本块", allResults.Count);

            return allResults;
        }

        /// <summary>
        /// 批量抽取关系
        /// </summary>
        public async Task<List<List<Relation>>> ExtractRelationsBatchAsync(
            List<(string ChunkId, string Content)> chunks,
            List<List<ZSN.AI.Entity.KnowledgeBase.Entity>> entitiesList,
            BatchExtractionConfig batchConfig,
            CancellationToken cancellationToken = default)
        {
            if (chunks.Count == 0 || chunks.Count != entitiesList.Count)
                return new List<List<Relation>>();

            if (!batchConfig.EnableBatching || chunks.Count == 1)
            {
                // 单个处理
                var results = new List<List<Relation>>();
                for (int i = 0; i < chunks.Count; i++)
                {
                    var relations = await ExtractRelationsFromSingleChunkAsync(
                        chunks[i].Content, entitiesList[i], cancellationToken);
                    results.Add(relations);
                }
                return results;
            }

            _logger.LogInformation("开始批量抽取关系，总块数: {TotalCount}, 批大小: {BatchSize}",
                chunks.Count, batchConfig.BatchSize);

            var allResults = new List<List<Relation>>();

            // 将文本块分组
            var batches = CreateBatches(chunks, batchConfig);

            for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
            {
                var batch = batches[batchIndex];

                // 获取对应的实体列表
                var startIndex = batchIndex * batchConfig.BatchSize;
                var batchEntities = entitiesList.Skip(startIndex).Take(batch.Count).ToList();

                _logger.LogInformation("处理批次 {BatchIndex}/{TotalBatches}, 包含 {ChunkCount} 个文本块",
                    batchIndex + 1, batches.Count, batch.Count);

                // 批量抽取
                var batchResults = await ExtractRelationsFromBatchAsync(
                    batch, batchEntities, batchIndex, cancellationToken);

                allResults.AddRange(batchResults);
            }

            _logger.LogInformation("批量关系抽取完成，共处理 {TotalCount} 个文本块", allResults.Count);

            return allResults;
        }

        /// <summary>
        /// 创建批次
        /// </summary>
        private List<List<(string ChunkId, string Content)>> CreateBatches(
            List<(string ChunkId, string Content)> chunks,
            BatchExtractionConfig config)
        {
            var batches = new List<List<(string ChunkId, string Content)>>();
            var currentBatch = new List<(string ChunkId, string Content)>();
            int currentTokens = 0;

            foreach (var chunk in chunks)
            {
                var chunkTokens = EstimateTokenCount(chunk.Content);

                // 检查是否需要新建批次
                if (currentBatch.Count >= config.BatchSize ||
                    currentTokens + chunkTokens > config.MaxTokensPerRequest)
                {
                    if (currentBatch.Count > 0)
                    {
                        batches.Add(currentBatch);
                        currentBatch = new List<(string ChunkId, string Content)>();
                        currentTokens = 0;
                    }
                }

                currentBatch.Add(chunk);
                currentTokens += chunkTokens;
            }

            // 添加最后一个批次
            if (currentBatch.Count > 0)
            {
                batches.Add(currentBatch);
            }

            return batches;
        }

        /// <summary>
        /// 从批次中提取实体
        /// </summary>
        private async Task<List<List<ZSN.AI.Entity.KnowledgeBase.Entity>>> ExtractEntitiesFromBatchAsync(
            List<(string ChunkId, string Content)> batch,
            EntityExtractionConfig config,
            int batchIndex,
            CancellationToken cancellationToken)
        {
            var results = new List<List<ZSN.AI.Entity.KnowledgeBase.Entity>>();

            try
            {
                // 合并文本块
                var combinedText = CombineChunksForExtraction(batch);

                // 构建批量提取Prompt
                var prompt = BuildBatchEntityExtractionPrompt(batch, config);

                // 调用LLM
                var jsonResponse = await CallLLMAsync(prompt, cancellationToken);

                // 解析批量响应
                var batchEntities = ParseBatchEntityResponse(jsonResponse, batch, config.MinConfidence);

                // 分发结果到各个文本块
                for (int i = 0; i < batch.Count; i++)
                {
                    var chunkId = batch[i].ChunkId;
                    var chunkText = batch[i].Content;

                    // 筛选属于该文本块的实体
                    var chunkEntities = batchEntities
                        .Where(e => BelongsToChunk(e, chunkId, chunkText, i))
                        .Select(e => CloneEntityForChunk(e, chunkId))
                        .ToList();

                    results.Add(chunkEntities);

                    _logger.LogDebug("批次 {BatchIndex} 文本块 {ChunkId}: 提取到 {Count} 个实体",
                        batchIndex, chunkId, chunkEntities.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次 {BatchIndex} 实体提取失败", batchIndex);

                // 返回空结果
                for (int i = 0; i < batch.Count; i++)
                {
                    results.Add(new List<ZSN.AI.Entity.KnowledgeBase.Entity>());
                }
            }

            return results;
        }

        /// <summary>
        /// 从批次中抽取关系
        /// </summary>
        private async Task<List<List<Relation>>> ExtractRelationsFromBatchAsync(
            List<(string ChunkId, string Content)> batch,
            List<List<ZSN.AI.Entity.KnowledgeBase.Entity>> entitiesList,
            int batchIndex,
            CancellationToken cancellationToken)
        {
            var results = new List<List<Relation>>();

            try
            {
                // 为每个文本块提取关系
                for (int i = 0; i < batch.Count; i++)
                {
                    var chunk = batch[i];
                    var entities = entitiesList[i];

                    var relations = await ExtractRelationsFromSingleChunkAsync(
                        chunk.Content, entities, cancellationToken);

                    results.Add(relations);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次 {BatchIndex} 关系抽取失败", batchIndex);

                // 返回空结果
                for (int i = 0; i < batch.Count; i++)
                {
                    results.Add(new List<Relation>());
                }
            }

            return results;
        }

        /// <summary>
        /// 从单个文本块提取实体
        /// </summary>
        private async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> ExtractEntitiesFromSingleChunkAsync(
            string text,
            EntityExtractionConfig config,
            CancellationToken cancellationToken)
        {
            var prompt = EntityPromptBuilder.BuildEntityExtractionPrompt(text, config);
            var jsonResponse = await CallLLMAsync(prompt, cancellationToken);

            var (entities, _, _) = OutputValidator.ValidateAndRepairEntityResponse(
                jsonResponse, config.MinConfidence, text, _logger);

            return entities;
        }

        /// <summary>
        /// 从单个文本块抽取关系
        /// </summary>
        private async Task<List<Relation>> ExtractRelationsFromSingleChunkAsync(
            string text,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            CancellationToken cancellationToken)
        {
            var prompt = EntityPromptBuilder.BuildRelationExtractionPrompt(text, entities);
            var jsonResponse = await CallLLMAsync(prompt, cancellationToken);

            var (relations, _, _) = OutputValidator.ValidateAndRepairRelationResponse(
                jsonResponse, entities, _logger);

            return relations;
        }

        /// <summary>
        /// 合并文本块用于批量提取
        /// </summary>
        private string CombineChunksForExtraction(List<(string ChunkId, string Content)> chunks)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < chunks.Count; i++)
            {
                sb.AppendLine($"[文本块 {i + 1} ID: {chunks[i].ChunkId}]");
                sb.AppendLine(chunks[i].Content);

                if (i < chunks.Count - 1)
                {
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 构建批量实体提取Prompt
        /// </summary>
        private string BuildBatchEntityExtractionPrompt(
            List<(string ChunkId, string Content)> chunks,
            EntityExtractionConfig config)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# 角色");
            sb.AppendLine("你是一个专业的实体识别专家，擅长从多个中文文本块中准确识别和分类实体。");
            sb.AppendLine();

            sb.AppendLine("# 任务");
            sb.AppendLine($"从以下 {chunks.Count} 个文本块中识别所有重要实体，并按照指定的类型进行分类。");
            sb.AppendLine();

            sb.AppendLine("# 重要提示");
            sb.AppendLine("1. 每个文本块都需要独立识别实体");
            sb.AppendLine("2. 在输出中必须标识每个实体属于哪个文本块（使用chunk_index字段）");
            sb.AppendLine("3. chunk_index从0开始，表示第几个文本块");
            sb.AppendLine();

            // 添加实体类型定义
            sb.AppendLine(EntityPromptBuilder.BuildEntityExtractionPrompt("", config));
            sb.AppendLine();

            // 构建合并后的文本
            sb.AppendLine("# 待处理的文本块");
            sb.AppendLine(CombineChunksForExtraction(chunks));
            sb.AppendLine();

            // 输出格式
            sb.AppendLine("# 输出格式");
            sb.AppendLine("请严格按照以下JSON格式输出：");
            sb.AppendLine("{");
            sb.AppendLine("  \"entities\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"text\": \"实体文本\",");
            sb.AppendLine("      \"type\": \"PERSON\",");
            sb.AppendLine("      \"attributes\": {\"属性名\": \"属性值\"},");
            sb.AppendLine("      \"confidence\": 0.95,");
            sb.AppendLine("      \"chunk_index\": 0,");
            sb.AppendLine("      \"start_position\": 0,");
            sb.AppendLine("      \"end_position\": 10");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 解析批量实体响应
        /// </summary>
        private List<ZSN.AI.Entity.KnowledgeBase.Entity> ParseBatchEntityResponse(
            string jsonResponse,
            List<(string ChunkId, string Content)> chunks,
            float minConfidence)
        {
            var entities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();

            try
            {
                var jsonContent = OutputValidator.ExtractJsonContent(jsonResponse);
                if (string.IsNullOrWhiteSpace(jsonContent))
                    return entities;

                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                if (root.TryGetProperty("entities", out var entitiesElement))
                {
                    foreach (var entityElement in entitiesElement.EnumerateArray())
                    {
                        var entity = new ZSN.AI.Entity.KnowledgeBase.Entity
                        {
                            Text = entityElement.GetProperty("text").GetString() ?? string.Empty,
                            Type = entityElement.GetProperty("type").GetString() ?? string.Empty,
                            Confidence = entityElement.GetProperty("confidence").GetSingle(),
                            StartPosition = entityElement.TryGetProperty("start_position", out var startProp)
                                ? startProp.GetInt32() : 0,
                            EndPosition = entityElement.TryGetProperty("end_position", out var endProp)
                                ? endProp.GetInt32() : 0
                        };

                        // 解析chunk_index
                        if (entityElement.TryGetProperty("chunk_index", out var chunkIndexProp))
                        {
                            var chunkIndex = chunkIndexProp.GetInt32();
                            if (chunkIndex >= 0 && chunkIndex < chunks.Count)
                            {
                                entity.SourceChunkIds.Add(chunks[chunkIndex].ChunkId);
                            }
                        }

                        // 解析属性
                        if (entityElement.TryGetProperty("attributes", out var attrsElement))
                        {
                            foreach (var attr in attrsElement.EnumerateObject())
                            {
                                entity.Attributes[attr.Name] = attr.Value.GetString() ?? string.Empty;
                            }
                        }

                        // 过滤低置信度实体
                        if (entity.Confidence >= minConfidence)
                        {
                            entities.Add(entity);
                        }
                    }
                }

                _logger.LogInformation("批量解析得到 {Count} 个实体", entities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析批量实体响应失败");
            }

            return entities;
        }

        /// <summary>
        /// 判断实体是否属于指定文本块
        /// </summary>
        private bool BelongsToChunk(
            ZSN.AI.Entity.KnowledgeBase.Entity entity,
            string chunkId,
            string chunkText,
            int chunkIndex)
        {
            // 方法1：检查SourceChunkIds
            if (entity.SourceChunkIds.Contains(chunkId))
                return true;

            // 方法2：检查chunk_index属性
            if (entity.Attributes.TryGetValue("chunk_index", out var chunkIndexStr))
            {
                if (int.TryParse(chunkIndexStr, out var idx) && idx == chunkIndex)
                    return true;
            }

            // 方法3：检查实体文本是否在当前文本块中
            if (chunkText.Contains(entity.Text))
                return true;

            return false;
        }

        /// <summary>
        /// 克隆实体并分配到指定文本块
        /// </summary>
        private ZSN.AI.Entity.KnowledgeBase.Entity CloneEntityForChunk(
            ZSN.AI.Entity.KnowledgeBase.Entity source,
            string chunkId)
        {
            return new ZSN.AI.Entity.KnowledgeBase.Entity
            {
                Id = source.Id,
                Text = source.Text,
                Type = source.Type,
                Confidence = source.Confidence,
                StartPosition = source.StartPosition,
                EndPosition = source.EndPosition,
                SourceChunkIds = new List<string> { chunkId },
                Attributes = new Dictionary<string, string>(source.Attributes)
            };
        }

        /// <summary>
        /// 调用LLM API
        /// </summary>
        private async Task<string> CallLLMAsync(
            string prompt,
            CancellationToken cancellationToken)
        {
            if (_chatService == null)
                return string.Empty;

            // TODO: 这里需要实际的LLM调用实现
            // 暂时返回空字符串
            await Task.CompletedTask;
            return string.Empty;
        }

        /// <summary>
        /// 估算token数量（简单估算：中文约1.5字符=1token，英文约4字符=1token）
        /// </summary>
        private int EstimateTokenCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            int chineseChars = text.Count(c => c >= 0x4E00 && c <= 0x9FFF);
            int otherChars = text.Length - chineseChars;

            // 中文：约1.5字符=1token，其他：约4字符=1token
            return (int)(chineseChars / 1.5 + otherChars / 4.0);
        }
    }
}
