using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.Entity.Model.Enum;
using ZSN.AI.Node.ServiceDesk.Interfaces;
using ZSN.AI.Node.ServiceDesk.Models;
using ZSN.AI.Node.Utils;

namespace ZSN.AI.Node.ServiceDesk.Services
{
    /// <summary>
    /// 知识检索器 — 采用与 KnowledgeBase 节点一致的文件级混合检索逻辑
    /// </summary>
    public class KnowledgeRetriever : IKnowledgeRetriever
    {
        private readonly IHybridSearchService _hybridSearchService;
        private readonly IChatService _chatService;
        private readonly ILogger<KnowledgeRetriever> _logger;

        public KnowledgeRetriever(
            IHybridSearchService hybridSearchService,
            IChatService chatService,
            ILogger<KnowledgeRetriever> logger)
        {
            _hybridSearchService = hybridSearchService;
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// 从多个知识库检索相关知识
        /// 核心逻辑与 Execution.KnowledgeBaseNodeAsync 一致：遍历知识库→获取文件→按文件ID检索→融合排序
        /// </summary>
        public async Task<KnowledgeRetrievalResult> RetrieveKnowledgeAsync(
            string query,
            List<KnowledgeBaseInfo> knowledgeBases,
            HybridSearchOptions searchOptions,
            MemoryContext memoryContext,
            ServiceDeskData config)
        {
            var stopwatch = Stopwatch.StartNew();

            // 0. 验证查询不为空
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("[KnowledgeRetriever] 查询文本为空,返回空结果");
                return new KnowledgeRetrievalResult
                {
                    Query = query,
                    RewrittenQuery = query,
                    Items = new List<RetrievalItem>(),
                    TotalCount = 0,
                    Confidence = 0,
                    ElapsedMs = 0,
                    Sources = new List<KnowledgeSource>()
                };
            }

            if (knowledgeBases == null || knowledgeBases.Count == 0)
            {
                _logger.LogWarning("[KnowledgeRetriever] 知识库列表为空");
                return new KnowledgeRetrievalResult
                {
                    Query = query,
                    RewrittenQuery = query,
                    Items = new List<RetrievalItem>(),
                    TotalCount = 0,
                    Confidence = 0,
                    ElapsedMs = 0,
                    Sources = new List<KnowledgeSource>()
                };
            }

            // 1. 查询重写
            var rewrittenQuery = await RewriteQueryAsync(query, memoryContext);
            if (string.IsNullOrWhiteSpace(rewrittenQuery))
            {
                _logger.LogWarning("[KnowledgeRetriever] 查询重写后为空,使用原查询");
                rewrittenQuery = query;
            }

            _logger.LogInformation($"[KnowledgeRetriever] 开始混合检索,查询: {rewrittenQuery}");
            _logger.LogInformation($"[KnowledgeRetriever] 检索配置 - MaxVectorResults: {searchOptions.MaxVectorResults}, VectorWeight: {searchOptions.VectorWeight:F2}, GraphWeight: {searchOptions.GraphWeight:F2}");

            // 2. 遍历所有知识库，按文件级别检索（与 KnowledgeBaseNodeAsync 逻辑一致）
            var allSearchResults = new List<(SearchResult result, string kbName, string fileName)>();
            var allChunkImages = new Dictionary<string, List<ZSN.AI.Entity.KnowledgeBase.ImageSearchResult>>();
            int totalChunks = 0;

            foreach (var knowledgeBase in knowledgeBases)
            {
                try
                {
                    _logger.LogInformation($"[KnowledgeRetriever] 检索知识库: {knowledgeBase.Name} (ID: {knowledgeBase.KnowledgeBaseID})");

                    List<KnowledgeBaseFileInfo> knowledgeBaseFiles = KnowledgeBaseFileInfoBussiness.GetList(
                        $" KnowledgeBaseID = '{knowledgeBase.KnowledgeBaseID}' and SystemStatus={(int)ImportKmsStatus.Success}");

                    foreach (var knowledgeBaseFileInfo in knowledgeBaseFiles)
                    {
                        var searchResult = await _hybridSearchService.SearchAsync(
                            query: rewrittenQuery,
                            knowledgeBaseId: knowledgeBaseFileInfo.FileID.ToString(),
                            options: searchOptions,
                            cancellationToken: default);

                        if (searchResult?.FusedResults != null && searchResult.FusedResults.Count > 0)
                        {
                            foreach (var sr in searchResult.FusedResults)
                            {
                                allSearchResults.Add((sr, knowledgeBase.Name, knowledgeBaseFileInfo.FileName));
                            }
                            totalChunks += searchResult.FusedResults.Count;
                            _logger.LogInformation($"[KnowledgeRetriever] 文件 {knowledgeBaseFileInfo.FileName} 检索到 {searchResult.FusedResults.Count} 个相关文档块");

                            // 收集图片信息
                            if (searchResult.ChunkImages != null && searchResult.ChunkImages.Count > 0)
                            {
                                foreach (var kv in searchResult.ChunkImages)
                                    allChunkImages[kv.Key] = kv.Value;
                            }

                            for (int i = 0; i < Math.Min(3, searchResult.FusedResults.Count); i++)
                            {
                                var result = searchResult.FusedResults[i];
                                _logger.LogDebug($"  - 结果 {i + 1}: Score={result.FusedScore:F4}, Content={result.Content.Substring(0, Math.Min(50, result.Content.Length))}...");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[KnowledgeRetriever] 检索知识库 {knowledgeBase.Name} 失败: {ex.Message}");
                }
            }

            // 3. 按融合得分排序，取 MaxVectorResults 个结果
            var topResults = allSearchResults
                .OrderByDescending(t => t.result.FusedScore)
                .Take(searchOptions.MaxVectorResults)
                .ToList();

            _logger.LogInformation($"[KnowledgeRetriever] 混合检索完成，共检索到 {totalChunks} 个文档块，取Top {topResults.Count} 个结果");

            // 4. 构建检索结果
            var items = topResults.Select(t =>
            {
                // 拼接图片信息到内容中
                var content = t.result.Content ?? "";
                if (allChunkImages.TryGetValue(t.result.ChunkId, out var images) && images.Count > 0)
                {
                    var imgBuilder = new StringBuilder();
                    imgBuilder.AppendLine();
                    imgBuilder.AppendLine($"\n[关联图片 {images.Count} 张]");
                    foreach (var img in images)
                    {
                        imgBuilder.AppendLine($"[图片] ID:{img.ImageId}");
                        if (!string.IsNullOrEmpty(img.Description))
                            imgBuilder.AppendLine($"  描述: {img.Description}");
                        if (!string.IsNullOrEmpty(img.OcrText))
                            imgBuilder.AppendLine($"  OCR: {img.OcrText}");
                    }
                    content += imgBuilder.ToString();
                }

                return new RetrievalItem
                {
                    Content = content,
                    Score = t.result.Score,
                    FinalScore = t.result.FusedScore,
                    Source = new KnowledgeSource
                    {
                        KnowledgeBaseName = t.kbName,
                        DocumentId = t.result.DocumentId,
                        DocumentTitle = t.fileName,
                        ChunkId = t.result.ChunkId,
                    },
                    Metadata = t.result.Metadata?.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "")
                };
            }).ToList();

            // 5. 计算整体置信度
            var confidence = CalculateOverallConfidence(topResults.Select(t => t.result).ToList());

            stopwatch.Stop();

            var finalItems = items.Take(config?.FusedResultTopN ?? 5).ToList();

            return new KnowledgeRetrievalResult
            {
                Query = query,
                RewrittenQuery = rewrittenQuery,
                Items = finalItems,
                TotalCount = totalChunks,
                Confidence = confidence,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Sources = finalItems.Select(r => r.Source).Distinct().ToList()
            };
        }

        /// <summary>结合对话历史重写查询</summary>
        private async Task<string> RewriteQueryAsync(string originalQuery, MemoryContext memoryContext)
        {
            if (memoryContext?.ShortTermMemory == null || memoryContext.ShortTermMemory.Count == 0)
                return originalQuery;

            if (!NeedsRewriting(originalQuery))
                return originalQuery;

            var recentHistory = memoryContext.ShortTermMemory
                .TakeLast(3)
                .Select(m => $"{m.Role}: {m.Content}");

            string promptTemplate = Utils.Utils.LoadPromptTemplate("ServiceDeskQueryRewritePrompt");
            string prompt;
            if (!string.IsNullOrEmpty(promptTemplate))
            {
                prompt = promptTemplate
                    .Replace("{{chatHistory}}", string.Join("\n", recentHistory))
                    .Replace("{{userQuery}}", originalQuery);
            }
            else
            {
                prompt = $@"根据对话历史，将用户的简短问题补充完整。

对话历史：
{string.Join("\n", recentHistory)}

用户问题：{originalQuery}

补充后的完整问题（只输出问题，不要解释）：";
            }

            try
            {
                var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
                history.AddUserMessage(prompt);
                var model = new ZSN.AI.Entity.LargeModelConfig();
                var chunks = _chatService.SendChatAsync(model, history);
                var sb = new StringBuilder();
                await foreach (var chunk in chunks)
                {
                    sb.Append(chunk);
                }
                var rewritten = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(rewritten))
                {
                    _logger.LogInformation($"[QueryRewrite] '{originalQuery}' → '{rewritten}'");
                    return rewritten;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[QueryRewrite] 查询重写失败，使用原查询");
            }

            return originalQuery;
        }

        private bool NeedsRewriting(string query)
        {
            var pronouns = new[] { "它", "他", "她", "这个", "那个", "这", "那" };
            if (pronouns.Any(p => query.Contains(p))) return true;
            if (query.Length < 10) return true;
            return false;
        }

        /// <summary>基于融合得分计算整体检索置信度</summary>
        private double CalculateOverallConfidence(List<SearchResult> topResults)
        {
            if (topResults == null || topResults.Count == 0) return 0;

            double top1Score = Math.Min(1.0, topResults[0].FusedScore) * 0.5;
            double top3Avg = topResults.Take(3).Average(r => Math.Min(1.0, r.FusedScore)) * 0.3;
            double countScore = Math.Min(1.0, topResults.Count / 5.0) * 0.2;

            return Math.Min(1.0, top1Score + top3Avg + countScore);
        }
    }
}
