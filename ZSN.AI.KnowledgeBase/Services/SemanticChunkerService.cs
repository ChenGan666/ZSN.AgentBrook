using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.Core.Services
{
    /// <summary>
    /// 语义感知分块服务实现
    /// </summary>
    public class SemanticChunkerService : ISemanticChunkerService
    {
        private readonly ITokenCounter _tokenCounter;
        private readonly ILogger<SemanticChunkerService> _logger;
        private readonly IChatService? _chatService;
        private readonly IConfiguration? _configuration;

        public SemanticChunkerService(
            ITokenCounter? tokenCounter = null,
            ILogger<SemanticChunkerService>? logger = null,
            IChatService? chatService = null,
            IConfiguration? configuration = null)
        {
            _tokenCounter = tokenCounter ?? new TokenCounter();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SemanticChunkerService>.Instance;
            _chatService = chatService;
            _configuration = configuration;
        }

        /// <summary>
        /// 语义感知分块
        /// </summary>
        public async Task<List<SemanticChunk>> ChunkAsync(
            string content,
            ChunkingStrategy strategy,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("开始分块，策略：{Strategy}", strategy);

            return strategy switch
            {
                ChunkingStrategy.HardCutoff => await HardCutoffChunkAsync(content, cancellationToken),
                ChunkingStrategy.SemanticBoundary => await SemanticBoundaryChunkAsync(content, cancellationToken),
                ChunkingStrategy.TopicSegmentation => await TopicSegmentationChunkAsync(content, cancellationToken),
                ChunkingStrategy.EntityAware => await EntityAwareChunkAsync(content, cancellationToken),
                ChunkingStrategy.LLMIntelligent => await LLMIntelligentChunkAsync(content, cancellationToken),
                _ => await HardCutoffChunkAsync(content, cancellationToken)
            };
        }

        /// <summary>
        /// 大文档流式分块
        /// </summary>
        public async IAsyncEnumerable<SemanticChunk> ChunkStreamAsync(
            Stream contentStream,
            ChunkingStrategy strategy,
            CancellationToken cancellationToken = default)
        {
            // TODO: 阶段2后期实现流式处理
            // 当前版本先读取全部内容再分块
            using var reader = new StreamReader(contentStream);
            var content = await reader.ReadToEndAsync(cancellationToken);

            var chunks = await ChunkAsync(content, strategy, cancellationToken);

            foreach (var chunk in chunks)
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// 获取分块统计信息
        /// </summary>
        public async Task<ChunkingStatistics> GetStatisticsAsync(
            string documentId,
            CancellationToken cancellationToken = default)
        {
            // TODO: 从持久化存储中获取统计信息
            await Task.CompletedTask;

            return new ChunkingStatistics
            {
                TotalChunks = 0,
                TotalTokens = 0,
                AverageTokensPerChunk = 0,
                MinTokens = 0,
                MaxTokens = 0,
                TotalEntities = 0
            };
        }

        #region 分块策略实现

        /// <summary>
        /// 硬切块策略（兼容模式）
        /// </summary>
        private async Task<List<SemanticChunk>> HardCutoffChunkAsync(
            string content,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            var chunks = new List<SemanticChunk>();
            const int maxTokens = 1000;
            const int overlapTokens = 100;

            int position = 0;
            int chunkIndex = 0;

            while (position < content.Length)
            {
                // 计算当前块的结束位置
                int remainingLength = content.Length - position;
                int chunkLength = Math.Min(maxTokens * 4, remainingLength); // 粗略估算

                // 确保不在句子中间切分
                if (chunkLength < remainingLength && chunkLength > 200)
                {
                    // 寻找最近的句号、问号、感叹号
                    var separators = new[] { '.', '?', '!' };
                    int lastPunctuation = -1;
                    int searchStart = Math.Max(0, position + chunkLength - 200);
                    int searchEnd = Math.Min(content.Length - 1, position + chunkLength);

                    foreach (var sep in separators)
                    {
                        int pos = content.LastIndexOf(sep, searchEnd, searchEnd - searchStart);
                        if (pos > lastPunctuation && pos >= searchStart)
                        {
                            lastPunctuation = pos;
                        }
                    }

                    if (lastPunctuation > position)
                    {
                        chunkLength = lastPunctuation - position + 1;
                        chunkLength = Math.Min(chunkLength, remainingLength); // 确保不超出
                    }
                }

                var chunkContent = content.Substring(position, chunkLength);
                var tokenCount = _tokenCounter.CountTokens(chunkContent);

                chunks.Add(new SemanticChunk
                {
                    Id = $"chunk_{chunkIndex}",
                    DocumentId = "", // 需要外部设置
                    Content = chunkContent,
                    StartPosition = position,
                    EndPosition = position + chunkLength,
                    TokenCount = tokenCount,
                    Sentences = SplitIntoSentences(chunkContent),
                    Metadata = new Dictionary<string, object>
                    {
                        ["strategy"] = ChunkingStrategy.HardCutoff,
                        ["chunk_index"] = chunkIndex
                    }
                });

                // 确保至少前进1个字符，避免无限循环
                int advance = Math.Max(1, chunkLength - overlapTokens);
                position += advance;
                chunkIndex++;
            }

            _logger.LogInformation("硬切块完成，生成 {Count} 个块", chunks.Count);
            return chunks;
        }

        /// <summary>
        /// 语义边界分块策略
        /// </summary>
        /// <remarks>
        /// TODO: 阶段2中期实现完整版
        /// 当前版本使用简化实现
        /// </remarks>
        private async Task<List<SemanticChunk>> SemanticBoundaryChunkAsync(
            string content,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            // 简化实现：按段落分割
            var paragraphs = content.Split(
                new[] { "\n\n", "\r\n\r\n" },
                StringSplitOptions.RemoveEmptyEntries
            );

            var chunks = new List<SemanticChunk>();
            int position = 0;
            int chunkIndex = 0;

            foreach (var paragraph in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(paragraph))
                    continue;

                chunks.Add(new SemanticChunk
                {
                    Id = $"chunk_{chunkIndex}",
                    DocumentId = "",
                    Content = paragraph.Trim(),
                    StartPosition = position,
                    EndPosition = position + paragraph.Length,
                    TokenCount = _tokenCounter.CountTokens(paragraph),
                    Sentences = SplitIntoSentences(paragraph),
                    Metadata = new Dictionary<string, object>
                    {
                        ["strategy"] = ChunkingStrategy.SemanticBoundary,
                        ["chunk_index"] = chunkIndex
                    }
                });

                position += paragraph.Length;
                chunkIndex++;
            }

            _logger.LogInformation("语义边界分块完成，生成 {Count} 个块", chunks.Count);
            return chunks;
        }

        /// <summary>
        /// 主题分割分块策略
        /// </summary>
        /// <remarks>
        /// 基于文本相似度检测主题变化点，在主题边界处分块
        /// </remarks>
        private async Task<List<SemanticChunk>> TopicSegmentationChunkAsync(
            string content,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            // 1. 先按段落分割
            var paragraphs = content.Split(
                new[] { "\n\n", "\r\n\r\n" },
                StringSplitOptions.RemoveEmptyEntries
            ).Where(p => !string.IsNullOrWhiteSpace(p.Trim())).ToList();

            if (paragraphs.Count == 0)
            {
                return new List<SemanticChunk>();
            }

            // 2. 计算相邻段落相似度，找出主题分割点
            var segmentPoints = new List<int>();
            const double similarityThreshold = 0.3; // 相似度阈值

            for (int i = 0; i < paragraphs.Count - 1; i++)
            {
                var similarity = CalculateSimilarity(paragraphs[i], paragraphs[i + 1]);
                _logger.LogDebug("段落 {Index} 和 {Next} 相似度: {Similarity:F2}", i, i + 1, similarity);

                // 相似度低于阈值，认为主题发生变化
                if (similarity < similarityThreshold)
                {
                    segmentPoints.Add(i + 1);
                }
            }

            // 3. 根据分割点创建块
            var chunks = new List<SemanticChunk>();
            int chunkIndex = 0;
            int startIndex = 0;

            foreach (var splitPoint in segmentPoints)
            {
                // 合并从startIndex到splitPoint的段落
                var chunkContent = string.Join("\n\n", paragraphs.Skip(startIndex).Take(splitPoint - startIndex));
                var position = paragraphs.Take(startIndex).Sum(p => p.Length + 2); // +2 for \n\n

                chunks.Add(new SemanticChunk
                {
                    Id = $"chunk_{chunkIndex}",
                    DocumentId = "",
                    Content = chunkContent.Trim(),
                    StartPosition = position,
                    EndPosition = position + chunkContent.Length,
                    TokenCount = _tokenCounter.CountTokens(chunkContent),
                    Sentences = SplitIntoSentences(chunkContent),
                    Metadata = new Dictionary<string, object>
                    {
                        ["strategy"] = ChunkingStrategy.TopicSegmentation,
                        ["chunk_index"] = chunkIndex,
                        ["paragraph_count"] = splitPoint - startIndex
                    }
                });

                startIndex = splitPoint;
                chunkIndex++;
            }

            // 添加最后一个块
            if (startIndex < paragraphs.Count)
            {
                var chunkContent = string.Join("\n\n", paragraphs.Skip(startIndex));
                var position = paragraphs.Take(startIndex).Sum(p => p.Length + 2);

                chunks.Add(new SemanticChunk
                {
                    Id = $"chunk_{chunkIndex}",
                    DocumentId = "",
                    Content = chunkContent.Trim(),
                    StartPosition = position,
                    EndPosition = position + chunkContent.Length,
                    TokenCount = _tokenCounter.CountTokens(chunkContent),
                    Sentences = SplitIntoSentences(chunkContent),
                    Metadata = new Dictionary<string, object>
                    {
                        ["strategy"] = ChunkingStrategy.TopicSegmentation,
                        ["chunk_index"] = chunkIndex,
                        ["paragraph_count"] = paragraphs.Count - startIndex
                    }
                });
            }

            _logger.LogInformation("主题分割分块完成，生成 {Count} 个块，发现 {Points} 个主题变化点",
                chunks.Count, segmentPoints.Count);
            return chunks;
        }

        /// <summary>
        /// 实体感知分块策略
        /// </summary>
        /// <remarks>
        /// 识别文本中的实体，在实体边界处分块，避免实体被截断
        /// 简化实现：检测句子边界，优先在句子结束处分块
        /// </remarks>
        private async Task<List<SemanticChunk>> EntityAwareChunkAsync(
            string content,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            var chunks = new List<SemanticChunk>();
            const int maxTokens = 800;
            const int minTokens = 400;

            // 1. 先分割成句子
            var sentences = SplitIntoSentences(content);
            if (sentences.Count == 0)
            {
                return new List<SemanticChunk>();
            }

            // 2. 识别句子中的潜在实体
            var sentenceEntities = new List<HashSet<string>>();
            foreach (var sentence in sentences)
            {
                var entities = ExtractEntities(sentence);
                sentenceEntities.Add(entities);
            }

            // 3. 按句子分组，确保实体完整性
            int chunkIndex = 0;
            int startSentence = 0;
            int currentTokens = 0;
            var currentEntities = new HashSet<string>();

            for (int i = 0; i < sentences.Count; i++)
            {
                var sentenceTokens = _tokenCounter.CountTokens(sentences[i]);
                var sentenceEntities_i = sentenceEntities[i];

                // 检查是否应该开始新块
                bool shouldSplit = false;

                // 超过最大token数
                if (currentTokens + sentenceTokens > maxTokens && currentTokens >= minTokens)
                {
                    shouldSplit = true;
                }

                // 检查实体一致性：如果当前句子包含大量新实体，可能主题变化
                var newEntities = sentenceEntities_i.Except(currentEntities).ToList();
                if (currentTokens > minTokens && newEntities.Count > 3)
                {
                    shouldSplit = true;
                }

                if (shouldSplit && currentTokens > 0)
                {
                    // 创建当前块
                    var chunkContent = string.Join(" ", sentences.Skip(startSentence).Take(i - startSentence));
                    var position = sentences.Take(startSentence).Sum(s => s.Length + 1);

                    chunks.Add(new SemanticChunk
                    {
                        Id = $"chunk_{chunkIndex}",
                        DocumentId = "",
                        Content = chunkContent.Trim(),
                        StartPosition = position,
                        EndPosition = position + chunkContent.Length,
                        TokenCount = currentTokens,
                        Sentences = sentences.Skip(startSentence).Take(i - startSentence).ToList(),
                        Metadata = new Dictionary<string, object>
                        {
                            ["strategy"] = ChunkingStrategy.EntityAware,
                            ["chunk_index"] = chunkIndex,
                            ["sentence_count"] = i - startSentence,
                            ["entity_count"] = currentEntities.Count
                        }
                    });

                    startSentence = i;
                    currentTokens = 0;
                    currentEntities.Clear();
                    chunkIndex++;
                }

                // 添加当前句子到当前块
                currentTokens += sentenceTokens;
                foreach (var entity in sentenceEntities_i)
                {
                    currentEntities.Add(entity);
                }
            }

            // 添加最后一个块
            if (startSentence < sentences.Count)
            {
                var chunkContent = string.Join(" ", sentences.Skip(startSentence));
                var position = sentences.Take(startSentence).Sum(s => s.Length + 1);

                chunks.Add(new SemanticChunk
                {
                    Id = $"chunk_{chunkIndex}",
                    DocumentId = "",
                    Content = chunkContent.Trim(),
                    StartPosition = position,
                    EndPosition = position + chunkContent.Length,
                    TokenCount = currentTokens,
                    Sentences = sentences.Skip(startSentence).ToList(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["strategy"] = ChunkingStrategy.EntityAware,
                        ["chunk_index"] = chunkIndex,
                        ["sentence_count"] = sentences.Count - startSentence,
                        ["entity_count"] = currentEntities.Count
                    }
                });
            }

            _logger.LogInformation("实体感知分块完成，生成 {Count} 个块", chunks.Count);
            return chunks;
        }

        /// <summary>
        /// LLM 智能分块策略
        /// </summary>
        /// <remarks>
        /// 使用大语言模型进行智能分块：
        /// 1. 如果配置了IChatService，调用LLM API进行分析
        /// 2. 如果LLM调用失败或未配置，回退到组合策略
        /// </remarks>
        private async Task<List<SemanticChunk>> LLMIntelligentChunkAsync(
            string content,
            CancellationToken cancellationToken)
        {
            // 尝试使用真正的LLM API
            if (_chatService != null && _configuration != null)
            {
                try
                {
                    var chunks = await CallLLMForChunking(content, cancellationToken);
                    if (chunks.Count > 0)
                    {
                        _logger.LogInformation("LLM 智能分块完成，生成 {Count} 个块", chunks.Count);
                        return chunks;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LLM 调用失败，回退到组合策略");
                }
            }
            else
            {
                _logger.LogDebug("未配置 IChatService，使用组合策略");
            }

            // 回退到组合策略
            return await FallbackStrategyChunking(content, cancellationToken);
        }

        /// <summary>
        /// 调用LLM进行智能分块
        /// </summary>
        private async Task<List<SemanticChunk>> CallLLMForChunking(
            string content,
            CancellationToken cancellationToken)
        {
            if (_chatService == null || _configuration == null)
                return new List<SemanticChunk>();

            // 从配置中获取模型ID
            var chatModelIdStr = _configuration["LargeModel:ChatModelID"];
            if (string.IsNullOrEmpty(chatModelIdStr) || !int.TryParse(chatModelIdStr, out int chatModelId))
            {
                _logger.LogWarning("未配置 ChatModelID 或格式无效");
                return new List<SemanticChunk>();
            }

            // 从数据库获取模型信息
            var modelInfo = ZSN.AI.BLL.LargeModelInfoBussiness.GetModel(chatModelId);

            if (modelInfo == null)
            {
                _logger.LogWarning("无法获取模型信息，ModelID: {ModelID}", chatModelId);
                return new List<SemanticChunk>();
            }

            // 构建LLM配置（包含完整的模型信息）
            var modelConfig = new LargeModelConfig
            {
                Id = chatModelIdStr,
                Model = modelInfo,  // 设置完整的模型信息
                Temperature = 30,  // 较低温度以获得更一致的结果
                ResponseFormat = "json_object",  // 要求JSON格式输出
                AnswerTokens = 4096,
                Prompt = BuildChunkingPrompt(content)
            };

            // 构建聊天历史
            var history = new ChatHistory();
            history.AddUserMessage(modelConfig.Prompt ?? string.Empty);

            // 调用LLM API
            var responseBuilder = new System.Text.StringBuilder();
            await foreach (var response in _chatService.SendChatAsync(
                modelConfig,
                history,
                responseFormat: "json_object",
                ct: cancellationToken))
            {
                responseBuilder.Append(response);
            }

            var jsonResponse = responseBuilder.ToString();

            // 解析JSON响应
            return ParseLLMChunkResponse(jsonResponse, content);
        }

        /// <summary>
        /// 构建分块提示词
        /// </summary>
        private string BuildChunkingPrompt(string content)
        {
            var prompt = new System.Text.StringBuilder();

            prompt.AppendLine("你是一个专业的文档分块专家。请将以下文本按照语义完整性进行智能分块。");
            prompt.AppendLine();
            prompt.AppendLine("分块要求：");
            prompt.AppendLine("1. 每个块应该包含完整的思想或主题");
            prompt.AppendLine("2. 优先在段落、章节边界处分块");
            prompt.AppendLine("3. 保持上下文连贯性，避免将相关内容分割到不同块");
            prompt.AppendLine("4. 每个块的目标长度是500-800个token");
            prompt.AppendLine("5. 识别文本中的实体（人名、地名、组织名等），确保不被截断");
            prompt.AppendLine();
            prompt.AppendLine("请以JSON格式返回分块结果，格式如下：");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"chunks\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"index\": 0,");
            prompt.AppendLine("      \"content\": \"第一个块的内容...\",");
            prompt.AppendLine("      \"reason\": \"分块原因说明\"");
            prompt.AppendLine("    },");
            prompt.AppendLine("    ...");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");
            prompt.AppendLine();
            prompt.AppendLine("--- 文本内容 ---");
            prompt.AppendLine(content);

            return prompt.ToString();
        }

        /// <summary>
        /// 解析LLM返回的分块结果
        /// </summary>
        private List<SemanticChunk> ParseLLMChunkResponse(string jsonResponse, string originalContent)
        {
            var chunks = new List<SemanticChunk>();

            try
            {
                // 解析JSON响应
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("chunks", out var chunksElement))
                {
                    int position = 0;
                    foreach (var chunkElement in chunksElement.EnumerateArray())
                    {
                        var content = chunkElement.GetProperty("content").GetString();
                        var reason = chunkElement.GetProperty("reason").GetString();

                        if (!string.IsNullOrEmpty(content))
                        {
                            // 查找内容在原文中的位置
                            var contentPosition = originalContent.IndexOf(content);
                            if (contentPosition >= 0)
                            {
                                position = contentPosition;
                            }

                            chunks.Add(new SemanticChunk
                            {
                                Id = $"chunk_{chunks.Count}",
                                DocumentId = "",
                                Content = content,
                                StartPosition = position,
                                EndPosition = position + content.Length,
                                TokenCount = _tokenCounter.CountTokens(content),
                                Sentences = SplitIntoSentences(content),
                                Metadata = new Dictionary<string, object>
                                {
                                    ["strategy"] = ChunkingStrategy.LLMIntelligent,
                                    ["chunk_index"] = chunks.Count,
                                    ["llm_reason"] = reason ?? "LLM generated",
                                    ["sentence_count"] = SplitIntoSentences(content).Count
                                }
                            });

                            position += content.Length;
                        }
                    }
                }

                _logger.LogInformation("成功解析 LLM 返回的 {Count} 个分块", chunks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析 LLM 响应失败");
                return new List<SemanticChunk>();
            }

            return chunks;
        }

        /// <summary>
        /// 回退策略：组合多种规则的智能分块
        /// </summary>
        private async Task<List<SemanticChunk>> FallbackStrategyChunking(
            string content,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            _logger.LogInformation("使用回退策略（组合策略）");

            // 组合策略：
            // 1. 先按段落分割
            // 2. 计算段落相似度，识别主题变化
            // 3. 在主题内，考虑实体完整性进行细分

            var paragraphs = content.Split(
                new[] { "\n\n", "\r\n\r\n" },
                StringSplitOptions.RemoveEmptyEntries
            ).Where(p => !string.IsNullOrWhiteSpace(p.Trim())).ToList();

            if (paragraphs.Count == 0)
            {
                return new List<SemanticChunk>();
            }

            // 计算段落相似度矩阵
            var similarityMatrix = new double[paragraphs.Count, paragraphs.Count];
            for (int i = 0; i < paragraphs.Count; i++)
            {
                for (int j = i; j < paragraphs.Count; j++)
                {
                    var sim = CalculateSimilarity(paragraphs[i], paragraphs[j]);
                    similarityMatrix[i, j] = sim;
                    similarityMatrix[j, i] = sim;
                }
            }

            // 使用相似度变化检测主题边界
            var topicBoundaries = DetectTopicBoundaries(similarityMatrix);
            _logger.LogDebug("检测到 {Count} 个主题边界", topicBoundaries.Count);

            // 在每个主题内进行实体感知分块
            var chunks = new List<SemanticChunk>();
            int chunkIndex = 0;
            int startPara = 0;

            foreach (var endPara in topicBoundaries)
            {
                // 合并当前主题的段落
                var topicContent = string.Join("\n\n", paragraphs.Skip(startPara).Take(endPara - startPara));

                // 使用句子级别的智能分割
                var topicChunks = IntelligentSentenceChunking(topicContent, chunkIndex);

                chunks.AddRange(topicChunks);
                chunkIndex += topicChunks.Count;
                startPara = endPara;
            }

            // 处理最后一个主题
            if (startPara < paragraphs.Count)
            {
                var topicContent = string.Join("\n\n", paragraphs.Skip(startPara));
                var topicChunks = IntelligentSentenceChunking(topicContent, chunkIndex);
                chunks.AddRange(topicChunks);
            }

            _logger.LogInformation("LLM 智能分块完成，生成 {Count} 个块", chunks.Count);
            return chunks;
        }

        /// <summary>
        /// 检测主题边界
        /// </summary>
        private List<int> DetectTopicBoundaries(double[,] similarityMatrix)
        {
            var boundaries = new List<int>();
            int n = similarityMatrix.GetLength(0);

            if (n < 2)
                return boundaries;

            // 寻找相似度骤降点
            for (int i = 1; i < n; i++)
            {
                // 计算前一段的平均相似度
                double beforeAvg = 0;
                for (int j = 0; j < i; j++)
                    beforeAvg += similarityMatrix[j, i];
                beforeAvg /= i;

                // 计算后一段的平均相似度
                double afterAvg = 0;
                for (int j = i; j < n; j++)
                    afterAvg += similarityMatrix[i, j];
                afterAvg /= (n - i);

                // 如果前后差异大，认为是主题边界
                if (Math.Abs(beforeAvg - afterAvg) > 0.3)
                {
                    boundaries.Add(i);
                }
            }

            return boundaries;
        }

        /// <summary>
        /// 智能句子级别分块
        /// </summary>
        private List<SemanticChunk> IntelligentSentenceChunking(string content, int startChunkIndex)
        {
            var chunks = new List<SemanticChunk>();
            var sentences = SplitIntoSentences(content);

            if (sentences.Count == 0)
                return chunks;

            const int targetTokens = 600;
            const int maxTokens = 1000;

            int chunkIndex = startChunkIndex;
            int startSent = 0;
            int currentTokens = 0;

            for (int i = 0; i < sentences.Count; i++)
            {
                var sentTokens = _tokenCounter.CountTokens(sentences[i]);

                // 检查是否应该分块
                if (currentTokens + sentTokens > maxTokens ||
                    (currentTokens >= targetTokens && IsSentenceBoundary(sentences[i])))
                {
                    // 创建块
                    var chunkContent = string.Join(" ", sentences.Skip(startSent).Take(i - startSent));
                    var position = content.IndexOf(chunkContent);

                    chunks.Add(new SemanticChunk
                    {
                        Id = $"chunk_{chunkIndex}",
                        DocumentId = "",
                        Content = chunkContent.Trim(),
                        StartPosition = position >= 0 ? position : 0,
                        EndPosition = position >= 0 ? position + chunkContent.Length : chunkContent.Length,
                        TokenCount = currentTokens,
                        Sentences = sentences.Skip(startSent).Take(i - startSent).ToList(),
                        Metadata = new Dictionary<string, object>
                        {
                            ["strategy"] = ChunkingStrategy.LLMIntelligent,
                            ["chunk_index"] = chunkIndex,
                            ["sentence_count"] = i - startSent
                        }
                    });

                    startSent = i;
                    currentTokens = 0;
                    chunkIndex++;
                }

                currentTokens += sentTokens;
            }

            // 添加最后一个块
            if (startSent < sentences.Count)
            {
                var chunkContent = string.Join(" ", sentences.Skip(startSent));
                var position = content.IndexOf(chunkContent);

                chunks.Add(new SemanticChunk
                {
                    Id = $"chunk_{chunkIndex}",
                    DocumentId = "",
                    Content = chunkContent.Trim(),
                    StartPosition = position >= 0 ? position : 0,
                    EndPosition = position >= 0 ? position + chunkContent.Length : chunkContent.Length,
                    TokenCount = currentTokens,
                    Sentences = sentences.Skip(startSent).ToList(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["strategy"] = ChunkingStrategy.LLMIntelligent,
                        ["chunk_index"] = chunkIndex,
                        ["sentence_count"] = sentences.Count - startSent
                    }
                });
            }

            return chunks;
        }

        /// <summary>
        /// 判断是否为良好的句子边界
        /// </summary>
        private bool IsSentenceBoundary(string sentence)
        {
            // 以句号、问号、感叹号结尾的句子是好的边界
            return sentence.Trim().EndsWith(".") ||
                   sentence.Trim().EndsWith("。") ||
                   sentence.Trim().EndsWith("?") ||
                   sentence.Trim().EndsWith("？") ||
                   sentence.Trim().EndsWith("!") ||
                   sentence.Trim().EndsWith("！");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算两段文本的相似度（基于词重叠）
        /// </summary>
        /// <remarks>
        /// 简化实现：使用词重叠的Jaccard相似度
        /// 生产环境可考虑使用更复杂的算法如TF-IDF或Word2Vec
        /// </remarks>
        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return 0.0;

            // 提取词汇（去除标点和空格）
            var words1 = ExtractWords(text1);
            var words2 = ExtractWords(text2);

            if (words1.Count == 0 || words2.Count == 0)
                return 0.0;

            // 计算交集
            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            // Jaccard相似度
            return union == 0 ? 0.0 : (double)intersection / union;
        }

        /// <summary>
        /// 从文本中提取词汇
        /// </summary>
        private HashSet<string> ExtractWords(string text)
        {
            var words = new HashSet<string>();

            // 简化分词：按空格和标点分割
            // 对于中文，按字符分割
            var tokens = System.Text.RegularExpressions.Regex.Split(text, @"\s+|[.,!?;:，。！？；：、\(\)\[\]""']");

            foreach (var token in tokens)
            {
                var trimmed = token.Trim();
                if (trimmed.Length > 1) // 过滤单字符
                {
                    // 如果是中文，按字添加
                    if (IsCJK(trimmed[0]))
                    {
                        foreach (char c in trimmed)
                        {
                            if (!char.IsPunctuation(c) && !char.IsWhiteSpace(c))
                            {
                                words.Add(c.ToString());
                            }
                        }
                    }
                    else
                    {
                        // 英文按词添加
                        words.Add(trimmed.ToLower());
                    }
                }
            }

            return words;
        }

        /// <summary>
        /// 判断是否为中日韩（CJK）字符
        /// </summary>
        private bool IsCJK(char c)
        {
            return c >= 0x4E00 && c <= 0x9FFF || // CJK统一汉字
                   c >= 0x3400 && c <= 0x4DBF || // CJK扩展A
                   c >= 0x20000 && c <= 0x2A6DF || // CJK扩展B
                   c >= 0x2A700 && c <= 0x2B73F || // CJK扩展C
                   c >= 0x2B740 && c <= 0x2B81F || // CJK扩展D
                   c >= 0x2B820 && c <= 0x2CEAF || // CJK扩展E
                   c >= 0xF900 && c <= 0xFAFF || // CJK兼容汉字
                   c >= 0x2F800 && c <= 0x2FA1F; // CJK兼容汉字补充
        }

        /// <summary>
        /// 从句子中提取潜在实体
        /// </summary>
        /// <remarks>
        /// 简化实现：使用启发式规则识别可能的实体
        /// - 英文：大写字母开头的连续词
        /// - 中文：连续2-4个汉字的词组
        /// </remarks>
        private HashSet<string> ExtractEntities(string sentence)
        {
            var entities = new HashSet<string>();

            // 英文实体：连续大写字母开头的词（如 TensorFlow, New York）
            var englishPattern = new System.Text.RegularExpressions.Regex(@"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+){0,2}\b");
            var matches = englishPattern.Matches(sentence);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                entities.Add(match.Value);
            }

            // 中文实体：识别可能的人名、地名、组织名
            // 简化规则：连续2-4个汉字，且在常用词库之外
            var chinesePattern = new System.Text.RegularExpressions.Regex(@"[\u4e00-\u9fff]{2,4}");
            var chineseMatches = chinesePattern.Matches(sentence);
            foreach (System.Text.RegularExpressions.Match match in chineseMatches)
            {
                var candidate = match.Value;
                // 过滤常见虚词和助词
                if (!IsCommonWord(candidate))
                {
                    entities.Add(candidate);
                }
            }

            return entities;
        }

        /// <summary>
        /// 判断是否为常见词（虚词、助词等）
        /// </summary>
        private bool IsCommonWord(string word)
        {
            var commonWords = new[]
            {
                "的", "了", "在", "是", "我", "有", "和", "就", "不", "人", "都", "一", "一个", "上", "也", "很", "到", "说", "要", "去", "你", "会", "着", "没有", "看", "好", "自己", "这",
                "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "must", "can"
            };
            return commonWords.Contains(word.ToLower());
        }

        /// <summary>
        /// 将文本分割为句子列表
        /// </summary>
        private List<string> SplitIntoSentences(string text)
        {
            var sentences = new List<string>();

            // 简化的句子分割逻辑
            string[] separators = { ". ", "。", "? ", "？", "! ", "！" };
            int startIndex = 0;

            while (startIndex < text.Length)
            {
                int nextSeparatorIndex = -1;
                string foundSeparator = null;

                foreach (var separator in separators)
                {
                    int index = text.IndexOf(separator, startIndex);
                    if (index != -1 && (nextSeparatorIndex == -1 || index < nextSeparatorIndex))
                    {
                        nextSeparatorIndex = index;
                        foundSeparator = separator;
                    }
                }

                if (nextSeparatorIndex == -1)
                {
                    // 没有更多分隔符
                    var remainingText = text.Substring(startIndex).Trim();
                    if (remainingText.Length > 0)
                    {
                        sentences.Add(remainingText);
                    }
                    break;
                }

                // 计算句子长度，确保不超出文本范围
                int sentenceLength = nextSeparatorIndex + foundSeparator.Length - startIndex;
                sentenceLength = Math.Min(sentenceLength, text.Length - startIndex);

                var sentence = text.Substring(startIndex, sentenceLength).Trim();
                if (sentence.Length > 0)
                {
                    sentences.Add(sentence);
                }

                startIndex = nextSeparatorIndex + foundSeparator.Length;
            }

            return sentences;
        }

        #endregion
    }
}
