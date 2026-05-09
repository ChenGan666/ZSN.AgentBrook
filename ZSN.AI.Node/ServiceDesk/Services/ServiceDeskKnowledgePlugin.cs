using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.Entity.Model.Enum;
using ZSN.AI.Node.ServiceDesk.Models;

namespace ZSN.AI.Node.ServiceDesk.Services
{
    /// <summary>
    /// 知识库检索插件 — 供 LLM 通过 FunctionCall 调用
    /// </summary>
    public class ServiceDeskKnowledgePlugin
    {
        private readonly IHybridSearchService _hybridSearchService;
        private readonly ServiceDeskData _config;
        private readonly ILogger _logger;

        /// <summary>最后一次检索的结果，用于提取引用信息</summary>
        public List<RetrievalItem> LastSearchResults { get; private set; } = new();

        public ServiceDeskKnowledgePlugin(
            IHybridSearchService hybridSearchService,
            ServiceDeskData config,
            ILogger logger)
        {
            _hybridSearchService = hybridSearchService;
            _config = config;
            _logger = logger;
        }

        [KernelFunction]
        [Description("在知识库中搜索与指定关键词相关的文档内容。当需要查找具体信息、产品说明、技术文档或业务流程时使用此工具。输入应为提取核心概念的优化搜索词。")]
        public async Task<string> SearchKnowledgeBase(
            [Description("优化后的搜索关键词或短语，用于从知识库中查找相关内容。请提取用户问题中的核心概念作为搜索词，可以使用多个关键词组合以提高检索准确度。")] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return "搜索关键词为空，请提供有效的搜索词。";

            var knowledgeBases = _config.knowledgeBase;
            if (knowledgeBases == null || knowledgeBases.Count == 0)
                return "没有配置可用的知识库。";

            var searchOptions = new HybridSearchOptions
            {
                VectorWeight = _config.VectorSearchWeight,
                GraphWeight = _config.FullTextSearchWeight,
                MaxVectorResults = _config.TopK * 2,
                EnableRerank = true,
            };

            _logger.LogInformation("[ServiceDeskKB] 搜索知识库，关键词: {Query}", query);

            var allResults = new List<(SearchResult result, string kbName, string fileName)>();

            foreach (var kb in knowledgeBases)
            {
                try
                {
                    var files = KnowledgeBaseFileInfoBussiness.GetList(
                        $" KnowledgeBaseID = '{kb.KnowledgeBaseID}' and SystemStatus={(int)ImportKmsStatus.Success}");

                    foreach (var file in files)
                    {
                        var searchResult = await _hybridSearchService.SearchAsync(
                            query: query,
                            knowledgeBaseId: file.FileID.ToString(),
                            options: searchOptions,
                            cancellationToken: default);

                        if (searchResult?.FusedResults != null)
                        {
                            foreach (var sr in searchResult.FusedResults)
                            {
                                allResults.Add((sr, kb.Name, file.FileName));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ServiceDeskKB] 检索知识库 {KBName} 失败", kb.Name);
                }
            }

            var topResults = allResults
                .OrderByDescending(t => t.result.FusedScore)
                .Take(searchOptions.MaxVectorResults)
                .ToList();

            // 存储原始结果供引用提取
            LastSearchResults = topResults.Select(t => new RetrievalItem
            {
                Content = t.result.Content ?? "",
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
            }).ToList();

            _logger.LogInformation("[ServiceDeskKB] 检索完成，共 {Total} 条，取 Top {Count} 条",
                allResults.Count, topResults.Count);

            if (topResults.Count == 0)
                return "未在知识库中找到与搜索关键词相关的内容。";

            // 格式化返回给 LLM 的检索结果
            var sb = new StringBuilder();
            sb.AppendLine($"[知识库搜索结果 - 共找到 {topResults.Count} 条相关内容]");
            var topN = Math.Min(_config.FusedResultTopN > 0 ? _config.FusedResultTopN : 5, topResults.Count);
            for (int i = 0; i < topN; i++)
            {
                var t = topResults[i];
                sb.AppendLine($"\n--- 文档{i + 1} (来源: {t.kbName}/{t.fileName}, 相关度: {t.result.FusedScore:F2}) ---");
                sb.AppendLine(t.result.Content);
            }
            return sb.ToString();
        }
    }
}
