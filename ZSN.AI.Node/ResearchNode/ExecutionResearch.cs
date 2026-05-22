using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Node.ResearchNode.Models;
using ZSN.AI.Node.ResearchNode.Services;
using ZSN.AI.Node.Utils;
using ZSN.AI.Node.Utils.Pipeline;
using ZSN.AI.Node.Claw.Pipeline;
using ZSN.AI.Service.Helpers;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AI.Node.ResearchNode
{
    public class ExecutionResearch : BaseExecution
    {
        private readonly IWebSearchService _searchService;
        private readonly IContentFetcherService _fetcherService;
        private readonly IResearchEngineService _engineService;
        private readonly IOptions<ResearchNodeOptions> _nodeOptions;
        private readonly ILogger<ExecutionResearch> _researchLogger;

        public ExecutionResearch(
            IChatService chatService,
            IServiceProvider provider,
            ILogger<ExecutionResearch> logger,
            IWebSearchService searchService,
            IContentFetcherService fetcherService,
            IResearchEngineService engineService,
            IOptions<ResearchNodeOptions> nodeOptions)
            : base(chatService, provider, logger)
        {
            _searchService = searchService;
            _fetcherService = fetcherService;
            _engineService = engineService;
            _nodeOptions = nodeOptions;
            _researchLogger = logger;
        }

        public async Task<string> ResearchNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            var outputs = new List<Output>();
            var Logs = new ConcurrentQueue<string>();
            ExecutionRecordStatus ExecutionRecordStatus = ExecutionRecordStatus.Success;

            string AppID = data.AppID;
            string TaskID = data.TaskID;
            string SessionID = data.SessionID;
            string ProcessesID = data.ProcessesID.IsNullOrEmpty() ? Guid.NewGuid().ToString() : data.ProcessesID;
            string MemberID = data.MemberID.IsNullOrEmpty() ? "system" : data.MemberID;
            string FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;

            RecordID = Utils.Utils.newExcutionRecord(
                SessionID, config, ProcessesID, TaskID,
                FromMainTaskID: FromMainTaskID, inputs: inputs);

            using var throttler = new RecordUpdateThrottler(
                RecordID, outputs, Logs,
                (rid, status, outs, logs) => Utils.Utils.updateExcutionRecord(rid, status, outs, logs),
                intervalMs: 500);

            // 流式输出
            string streamKey = StreamKey.Build(SessionID, ProcessesID);
            using var batchWriter = new StreamBatchWriter(
                _streamSync, streamKey, SessionID, ProcessesID, TaskID, config.id, intervalMs: 200);

            var progress = new Progress<string>(delta =>
            {
                batchWriter.Append(delta);
            });

            // 整体超时保护
            var timeoutMinutes = _nodeOptions.Value.OverallTimeoutMinutes > 0
                ? _nodeOptions.Value.OverallTimeoutMinutes : 5;
            using var overallCts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));

            try
            {
                Logs.Enqueue("=== Research 节点开始执行 ===");
                batchWriter.Append("\n=== Research 节点开始执行 ===\n");
                throttler.MarkDirty();

                // ── 1. 初始化：解析配置 ──
                var nodeData = JsonConvert.DeserializeObject<ResearchNodeData>(config.data.ToString());
                if (nodeData == null)
                    throw new Exception("Research 节点配置解析失败");

                // 替换 prompt 中的变量
                var promptCache = this.BuildPromptReplaceCache(inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
                nodeData.prompt = await this.ReplacePromptValueCached(nodeData.prompt, promptCache, SessionID, AppID, ProcessesID);

                if (string.IsNullOrWhiteSpace(nodeData.prompt))
                    throw new Exception("研究目标为空，请检查 prompt 配置或输入参数。");

                Logs.Enqueue($"[Init] 研究目标: {nodeData.prompt}");
                batchWriter.Append($"🔍 开始研究: \"{nodeData.prompt}\"\n");
                throttler.MarkDirty();

                // ── 2. 构建 LLM 配置（主模型 + 规划模型） ──
                var modelConfig = BuildModelConfig(nodeData?.model, nodeData?.temperature ?? 30, nodeData?.topp ?? 80);
                if (modelConfig == null)
                    throw new Exception("无可用 LLM 模型，请检查节点或系统默认模型配置。");

                // 规划模型：优先使用 planModel，否则用主模型
                var planModelConfig = nodeData?.planModel?.LargeModelID > 0
                    ? BuildModelConfig(nodeData.planModel, nodeData.temperature, nodeData.topp) ?? modelConfig
                    : modelConfig;

                // ── 3. 检测 Playwright 可用性 ──
                bool playwrightAvailable = false;
                try
                {
                    playwrightAvailable = await _fetcherService.IsPlaywrightAvailableAsync();
                }
                catch (Exception ex)
                {
                    _researchLogger.LogWarning(ex, "[Research] Playwright 不可用，将使用 Snippet 模式");
                }

                if (playwrightAvailable)
                {
                    batchWriter.Append("🌐 Playwright 已就绪，启用网页抓取模式\n");
                    Logs.Enqueue("[Init] Playwright 网页抓取模式");
                }
                else
                {
                    batchWriter.Append("⚠️ Playwright 不可用，使用搜索摘要模式\n");
                    Logs.Enqueue("[Init] Snippet 降级模式");
                }

                // ── 4. 研究主循环 ──
                var sw = Stopwatch.StartNew();
                int llmCallsUsed = 0;
                string accumulatedSummary = "";
                var allSources = new List<SourceInfo>();
                var excludeUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var usedKeywords = new List<string>();
                double completenessScore = 0;
                int totalPagesFetched = 0;

                // 首次搜索规划
                batchWriter.Append("🧠 正在规划搜索策略...\n");
                throttler.MarkDirty();

                var plan = await _engineService.GenerateSearchPlanAsync(
                    nodeData.prompt, new List<string>(), "", planModelConfig);
                llmCallsUsed++;
                usedKeywords.AddRange(plan.Keywords);

                batchWriter.Append($"📋 搜索规划完成，关键词: {string.Join(", ", plan.Keywords)}\n");
                Logs.Enqueue($"[Plan] 关键词: {string.Join(", ", plan.Keywords)}");
                throttler.MarkDirty();

                int iteration = 0;
                for (; iteration < nodeData.MaxIterations; iteration++)
                {
                    // Token 预算检查（预留1次给最终格式化）
                    if (llmCallsUsed >= nodeData.MaxLLMCalls - 1)
                    {
                        batchWriter.Append("\n⚠️ LLM调用预算已达上限，停止搜索\n");
                        Logs.Enqueue($"[Budget] LLM调用次数 {llmCallsUsed} 达到上限 {nodeData.MaxLLMCalls}");
                        break;
                    }

                    batchWriter.Append($"\n📋 第{iteration + 1}轮搜索\n");
                    batchWriter.Append($"  → 关键词: {string.Join(", ", plan.Keywords)}\n");

                    // [a] 搜索
                    var searchResults = await _searchService.SearchAsync(plan, excludeUrls, overallCts.Token);
                    if (searchResults.Count == 0)
                    {
                        batchWriter.Append("  → 未找到新结果，停止搜索\n");
                        Logs.Enqueue($"[Search] 第{iteration + 1}轮无新搜索结果");
                        break;
                    }

                    batchWriter.Append($"  → 找到 {searchResults.Count} 条相关结果\n");
                    Logs.Enqueue($"[Search] 第{iteration + 1}轮找到 {searchResults.Count} 条结果");
                    throttler.MarkDirty();

                    // [b] 抓取网页内容或使用 Snippet
                    var rankedResults = _searchService.RankByRelevance(searchResults, nodeData.prompt, nodeData.MaxFetchUrls);
                    var urlsToProcess = rankedResults;
                    List<WebPageContent> contents;

                    if (playwrightAvailable)
                    {
                        var urls = urlsToProcess.Select(r => r.Url).ToList();
                        contents = await _fetcherService.FetchAsync(
                            urls, nodeData.MaxContentLength, progress, overallCts.Token);

                        // 抓取失败的用 Snippet 兜底
                        for (int i = 0; i < contents.Count; i++)
                        {
                            if (!contents[i].Success || string.IsNullOrEmpty(contents[i].Content))
                            {
                                var sr = urlsToProcess.FirstOrDefault(r => r.Url == contents[i].Url);
                                if (sr != null)
                                {
                                    contents[i].Content = sr.Snippet ?? "";
                                    contents[i].Title = sr.Title ?? contents[i].Title;
                                    contents[i].ContentLength = sr.Snippet?.Length ?? 0;
                                    contents[i].Success = !string.IsNullOrEmpty(sr.Snippet);
                                }
                            }
                        }

                        var fetchedPages = contents.Count(c => c.Success && c.ContentLength > 100);
                        totalPagesFetched += fetchedPages;
                    }
                    else
                    {
                        // Snippet 降级模式
                        contents = urlsToProcess.Select(r => new WebPageContent
                        {
                            Url = r.Url,
                            Title = r.Title,
                            Content = r.Snippet ?? "",
                            FetchTime = DateTime.UtcNow,
                            Success = true,
                            ContentLength = r.Snippet?.Length ?? 0
                        }).ToList();
                    }

                    // 记录已处理的 URL
                    foreach (var c in contents) excludeUrls.Add(c.Url);

                    // 记录来源
                    allSources.AddRange(contents.Where(c => c.Success && !string.IsNullOrEmpty(c.Content)).Select(c => new SourceInfo
                    {
                        Title = c.Title,
                        Url = c.Url,
                        FetchTime = c.FetchTime,
                        Snippet = c.Content.Length > 200 ? c.Content.Substring(0, 200) + "..." : c.Content
                    }));

                    // [c] 分析+反思
                    batchWriter.Append("\n  🧠 分析中...\n");
                    throttler.MarkDirty();

                    var analysis = await _engineService.AnalyzeAndReflectAsync(
                        nodeData.prompt, contents, accumulatedSummary, iteration + 1, modelConfig);
                    llmCallsUsed++;

                    accumulatedSummary = analysis.OrganizedSummary;
                    completenessScore = analysis.CompletenessScore;

                    batchWriter.Append($"  → 完整度: {(int)(completenessScore * 100)}%");
                    if (analysis.Gaps != null && analysis.Gaps.Count > 0)
                        batchWriter.Append($" | 缺口: {string.Join("、", analysis.Gaps.Take(3))}");
                    batchWriter.Append("\n");

                    Logs.Enqueue($"[Analysis] 第{iteration + 1}轮 完整度={completenessScore:F2}, IsSatisfied={analysis.IsSatisfied}");
                    throttler.MarkDirty();

                    // [d] 检查是否满足
                    if (analysis.IsSatisfied || completenessScore >= nodeData.CompletionThreshold)
                    {
                        batchWriter.Append("  ✓ 研究目标已满足\n");
                        Logs.Enqueue($"[Done] 研究目标已满足，完整度={completenessScore:F2}");
                        break;
                    }

                    // [e] 更新搜索计划
                    if (analysis.SuggestedKeywords != null && analysis.SuggestedKeywords.Count > 0)
                    {
                        plan = new SearchPlan
                        {
                            Keywords = analysis.SuggestedKeywords,
                            Language = plan.Language,
                            Categories = plan.Categories,
                            TimeRange = plan.TimeRange
                        };
                        usedKeywords.AddRange(analysis.SuggestedKeywords);
                    }
                    else
                    {
                        batchWriter.Append("  → 无新的搜索建议，停止搜索\n");
                        break;
                    }
                }

                // ── 5. 最终格式化 ──
                batchWriter.Append("\n📝 生成研究报告...\n");
                throttler.MarkDirty();

                var stats = new ResearchStats
                {
                    Iterations = iteration,
                    TotalPagesFetched = totalPagesFetched > 0 ? totalPagesFetched : allSources.Count,
                    TotalSourcesUsed = allSources.Count,
                    LLMCallsUsed = llmCallsUsed + 1,
                    FinalCompletenessScore = completenessScore,
                    TotalElapsedMs = sw.ElapsedMilliseconds
                };

                var result = await _engineService.FormatFinalResultAsync(
                    nodeData.prompt, accumulatedSummary, allSources, stats, modelConfig, progress);

                batchWriter.Append($"\n✅ 研究完成 ({stats.Iterations}轮, {stats.TotalPagesFetched}页, {stats.TotalSourcesUsed}个来源, 耗时{sw.ElapsedMilliseconds / 1000}秒)\n");
                Logs.Enqueue($"[Complete] 研究完成 {stats.Iterations}轮, 耗时{sw.ElapsedMilliseconds}ms");

                // ── 6. 输出变量 ──
                outputs.Add(new Output { varname = "results", value = result.DetailedContent ?? "", nodeId = config.id });
                outputs.Add(new Output { varname = "summary", value = result.Summary ?? "", nodeId = config.id });
                outputs.Add(new Output { varname = "sources", value = JsonConvert.SerializeObject(result.Sources ?? new List<SourceInfo>()), nodeId = config.id });
                outputs.Add(new Output { varname = "key_findings", value = JsonConvert.SerializeObject(result.KeyFindings ?? new List<KeyFinding>()), nodeId = config.id });

                throttler.MarkDirty();

                // ── 7. 触发下一节点 ──
                Logs.Enqueue("[NextNode] 准备触发下一节点");
                WorkflowNodeInfoBussiness.NextNode(
                    AppID, SessionID, ProcessesID, TaskID, FromMainTaskID,
                    AgentNodeID: "", config, inputs, outputs, Logs.ToList());
                Logs.Enqueue("[NextNode] 下一节点已触发");
                throttler.MarkDirty();
            }
            catch (OperationCanceledException)
            {
                _researchLogger.LogWarning("[Research] 执行超时 ({Timeout}分钟)，返回已获取内容", timeoutMinutes);
                Logs.Enqueue($"\n⚠️ 研究执行超时({timeoutMinutes}分钟)，返回已获取内容");
                batchWriter.Append($"\n⚠️ 研究执行超时({timeoutMinutes}分钟)\n");

                outputs.Add(new Output { varname = "results", value = "研究执行超时，内容不完整", nodeId = config.id });
                outputs.Add(new Output { varname = "summary", value = "研究执行超时", nodeId = config.id });

                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }
            catch (Exception ex)
            {
                _researchLogger.LogError(ex, "[Research] 执行失败 - SessionID: {SessionID}", data.SessionID);
                Logs.Enqueue($"\n执行失败: {ex.Message}");
                batchWriter.Append($"\n❌ 研究执行失败: {ex.Message}\n");

                outputs.Add(new Output { varname = "results", value = $"研究执行失败: {ex.Message}", nodeId = config.id });
                outputs.Add(new Output { varname = "summary", value = "研究执行失败", nodeId = config.id });

                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            throttler.FlushWithStatus(ExecutionRecordStatus);
            return RecordID;
        }

        /// <summary>
        /// 构建模型配置，优先使用指定模型，否则回退到系统默认模型
        /// </summary>
        private LargeModelConfig BuildModelConfig(LargeModelInfo modelSetting, int temperature, int topp)
        {
            LargeModelInfo modelInfo = null;

            if (modelSetting?.LargeModelID > 0)
            {
                modelInfo = LargeModelInfoBussiness.GetModel(modelSetting.LargeModelID);
            }

            if (modelInfo == null)
            {
                modelInfo = LargeModelInfoBussiness.GetDefaultModel();
                if (modelInfo != null)
                {
                    _researchLogger.LogInformation("[Research] 使用系统默认模型: {ModelName} (ID: {ModelID})",
                        modelInfo.ModelName, modelInfo.LargeModelID);
                }
            }

            if (modelInfo == null ||
                string.IsNullOrWhiteSpace(modelInfo.ModelName) ||
                string.IsNullOrWhiteSpace(modelInfo.ModelKey) ||
                string.IsNullOrWhiteSpace(modelInfo.EndPoint))
            {
                return null;
            }

            return new LargeModelConfig
            {
                Model = modelInfo,
                Temperature = temperature,
                TopPCoefficient = topp,
            };
        }
    }
}
