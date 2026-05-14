using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Node.ResearchNode.Models;

namespace ZSN.AI.Node.ResearchNode.Services
{
    public class ResearchEngineService : IResearchEngineService
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ResearchEngineService> _logger;

        public ResearchEngineService(IChatService chatService, ILogger<ResearchEngineService> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public async Task<SearchPlan> GenerateSearchPlanAsync(
            string researchGoal,
            List<string> previousKeywords,
            string previousFindings,
            LargeModelConfig modelConfig,
            IProgress<string> progress = null)
        {
            var prompt = BuildSearchPlanPrompt(researchGoal, previousKeywords, previousFindings);
            var response = await CallLLMAsync(modelConfig, prompt, "json_object");

            return ParseSearchPlan(response) ?? new SearchPlan
            {
                Keywords = new List<string> { researchGoal }
            };
        }

        public async Task<AnalysisResult> AnalyzeAndReflectAsync(
            string researchGoal,
            List<WebPageContent> newContents,
            string accumulatedSummary,
            int currentIteration,
            LargeModelConfig modelConfig,
            IProgress<string> progress = null)
        {
            var prompt = BuildAnalysisPrompt(researchGoal, newContents, accumulatedSummary, currentIteration);
            var response = await CallLLMAsync(modelConfig, prompt, "json_object");

            return ParseAnalysisResult(response) ?? new AnalysisResult
            {
                OrganizedSummary = accumulatedSummary ?? "",
                CompletenessScore = 0.3,
                IsSatisfied = false,
                Gaps = new List<string> { "分析结果解析失败" },
                SuggestedKeywords = new List<string>()
            };
        }

        public async Task<ResearchResult> FormatFinalResultAsync(
            string researchGoal,
            string accumulatedSummary,
            List<SourceInfo> allSources,
            ResearchStats stats,
            LargeModelConfig modelConfig,
            IProgress<string> progress = null)
        {
            var prompt = BuildFormatPrompt(researchGoal, accumulatedSummary, allSources, stats);
            var response = await CallLLMStreamingAsync(modelConfig, prompt, progress);

            var result = ParseFinalResult(response, accumulatedSummary, allSources, stats) ?? new ResearchResult
            {
                Summary = accumulatedSummary ?? "研究完成，但格式化失败",
                DetailedContent = accumulatedSummary ?? "",
                Sources = allSources,
                Stats = stats
            };

            // XSS 防护
            result.Summary = SanitizeOutput(result.Summary);
            result.DetailedContent = SanitizeOutput(result.DetailedContent);

            return result;
        }

        #region Prompt 构建

        private string BuildSearchPlanPrompt(string researchGoal, List<string> previousKeywords, string previousFindings)
        {
            var prevKw = previousKeywords != null && previousKeywords.Count > 0
                ? string.Join("、", previousKeywords)
                : "（首次搜索，无历史关键词）";
            var prevFind = string.IsNullOrEmpty(previousFindings) ? "（暂无）" : previousFindings;

            return $@"你是一个搜索规划专家。请根据研究目标，生成最佳的搜索关键词组合。

## 研究目标
{researchGoal}

## 已搜索过的关键词（避免重复）
{prevKw}

## 之前的发现摘要
{prevFind}

## 输出要求
请以严格JSON格式输出（不要额外文字）：
{{
  ""keywords"": [""关键词1"", ""关键词2"", ""关键词3""],
  ""language"": ""zh-CN"",
  ""categories"": [""general""],
  ""time_range"": """"
}}

要求：
- 生成2-4个不同角度的搜索关键词
- 关键词应覆盖不同维度和表达方式
- 避免与已搜索关键词重复
- 如果有之前的发现，应针对信息缺口设计关键词";
        }

        private string BuildAnalysisPrompt(string researchGoal, List<WebPageContent> newContents, string accumulatedSummary, int currentIteration)
        {
            var summarySection = string.IsNullOrEmpty(accumulatedSummary)
                ? "（首次分析，暂无历史摘要）"
                : accumulatedSummary;

            var contentBuilder = new System.Text.StringBuilder();
            if (newContents != null)
            {
                for (int i = 0; i < newContents.Count; i++)
                {
                    var c = newContents[i];
                    if (!c.Success || string.IsNullOrEmpty(c.Content)) continue;
                    contentBuilder.AppendLine($"\n### 来源{i + 1}: {c.Title}");
                    contentBuilder.AppendLine($"URL: {c.Url}");
                    contentBuilder.AppendLine(c.Content);
                }
            }

            return $@"# 研究分析任务（第{currentIteration}轮）

## 研究目标
{researchGoal}

## 已有研究摘要（前几轮成果）
{summarySection}

## 本轮新获取内容
{(contentBuilder.Length > 0 ? contentBuilder.ToString() : "（本轮未获取到新内容）")}

## 输出要求
请完成以下分析，以严格JSON格式输出（不要额外文字）：
{{
  ""organized_summary"": ""整合新旧内容的完整摘要，Markdown格式，每个信息点标注[来源URL]"",
  ""completeness_score"": 0.0到1.0之间的数字,
  ""is_satisfied"": true或false,
  ""gaps"": [""缺口1"", ""缺口2""],
  ""suggested_keywords"": [""关键词1"", ""关键词2""],
  ""reasoning"": ""简要评估理由""
}}

要求：
- organized_summary 要整合所有轮次的信息，按维度归类
- completeness_score 表示对研究目标的信息覆盖完整度
- is_satisfied 仅在信息非常充分时才为true
- gaps 列出尚未覆盖的信息缺口
- suggested_keywords 针对gaps提出新的搜索关键词（最多3个）";
        }

        private string BuildFormatPrompt(string researchGoal, string accumulatedSummary, List<SourceInfo> allSources, ResearchStats stats)
        {
            var sourceList = new System.Text.StringBuilder();
            if (allSources != null)
            {
                for (int i = 0; i < allSources.Count; i++)
                {
                    var s = allSources[i];
                    sourceList.AppendLine($"- [{s.Title}]({s.Url})");
                }
            }

            return $@"# 研究报告生成

## 研究目标
{researchGoal}

## 研究摘要
{accumulatedSummary}

## 信息来源（共{allSources?.Count ?? 0}个）
{sourceList}

## 统计
- 搜索轮次: {stats.Iterations}
- 抓取页面: {stats.TotalPagesFetched}
- 完整度评分: {stats.FinalCompletenessScore:P0}

## 输出要求
请以严格JSON格式输出（不要额外文字）：
{{
  ""summary"": ""2-3段话的研究摘要，提炼核心发现"",
  ""detailed_content"": ""完整Markdown研究报告，含标题、小节、引用来源"",
  ""key_findings"": [
    {{""finding"": ""发现内容"", ""source_url"": ""来源URL"", ""dimension"": ""所属维度""}}
  ]
}}

要求：
- summary 要精炼概括，2-3段话
- detailed_content 要结构清晰，使用Markdown标题和小节
- key_findings 列出3-5个最重要的发现
- 每个信息点都要标注来源";
        }

        #endregion

        #region LLM 调用

        private async Task<string> CallLLMAsync(LargeModelConfig modelConfig, string prompt, string responseFormat = "text")
        {
            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var config = new LargeModelConfig
            {
                Model = modelConfig.Model,
                Temperature = modelConfig.Temperature,
                TopPCoefficient = modelConfig.TopPCoefficient,
                ResponseFormat = responseFormat
            };

            var sb = new System.Text.StringBuilder();
            await foreach (var chunk in _chatService.SendChatAsync(config, history, responseFormat: responseFormat))
            {
                sb.Append(chunk);
            }

            return sb.ToString();
        }

        private async Task<string> CallLLMStreamingAsync(LargeModelConfig modelConfig, string prompt, IProgress<string> progress)
        {
            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var config = new LargeModelConfig
            {
                Model = modelConfig.Model,
                Temperature = modelConfig.Temperature,
                TopPCoefficient = modelConfig.TopPCoefficient,
                ResponseFormat = "text"
            };

            var sb = new System.Text.StringBuilder();
            await foreach (var chunk in _chatService.SendChatAsync(
                config, history, responseFormat: "text",
                enableStreamingObservation: true, progress: progress))
            {
                sb.Append(chunk);
            }

            return sb.ToString();
        }

        #endregion

        #region 结果解析

        private SearchPlan ParseSearchPlan(string response)
        {
            try
            {
                var json = ExtractJsonObject(response);
                if (json == null) return null;

                return new SearchPlan
                {
                    Keywords = json["keywords"]?.ToObject<List<string>>() ?? new List<string>(),
                    Language = json["language"]?.ToString() ?? "zh-CN",
                    Categories = json["categories"]?.ToObject<List<string>>() ?? new List<string> { "general" },
                    TimeRange = json["time_range"]?.ToString() ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ResearchEngine] 解析 SearchPlan 失败: {Response}", response?.Substring(0, Math.Min(200, response?.Length ?? 0)));
                return null;
            }
        }

        private AnalysisResult ParseAnalysisResult(string response)
        {
            try
            {
                var json = ExtractJsonObject(response);
                if (json == null) return null;

                return new AnalysisResult
                {
                    OrganizedSummary = json["organized_summary"]?.ToString() ?? "",
                    CompletenessScore = json["completeness_score"]?.ToObject<double>() ?? 0.3,
                    IsSatisfied = json["is_satisfied"]?.ToObject<bool>() ?? false,
                    Gaps = json["gaps"]?.ToObject<List<string>>() ?? new List<string>(),
                    SuggestedKeywords = json["suggested_keywords"]?.ToObject<List<string>>() ?? new List<string>(),
                    Reasoning = json["reasoning"]?.ToString() ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ResearchEngine] 解析 AnalysisResult 失败: {Response}", response?.Substring(0, Math.Min(200, response?.Length ?? 0)));
                return null;
            }
        }

        private ResearchResult ParseFinalResult(string response, string fallbackSummary, List<SourceInfo> sources, ResearchStats stats)
        {
            try
            {
                var json = ExtractJsonObject(response);
                if (json == null) return null;

                return new ResearchResult
                {
                    Summary = json["summary"]?.ToString() ?? fallbackSummary ?? "",
                    DetailedContent = json["detailed_content"]?.ToString() ?? fallbackSummary ?? "",
                    KeyFindings = json["key_findings"]?.ToObject<List<KeyFinding>>() ?? new List<KeyFinding>(),
                    Sources = sources ?? new List<SourceInfo>(),
                    Stats = stats ?? new ResearchStats()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ResearchEngine] 解析 ResearchResult 失败: {Response}", response?.Substring(0, Math.Min(200, response?.Length ?? 0)));
                return null;
            }
        }

        /// <summary>
        /// 从 LLM 响应中提取 JSON 对象（兼容 markdown 代码块包裹）
        /// </summary>
        private JObject ExtractJsonObject(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return null;

            var text = response.Trim();

            // 去掉 markdown 代码块包裹
            if (text.StartsWith("```json"))
                text = text.Substring(7);
            else if (text.StartsWith("```"))
                text = text.Substring(3);
            if (text.EndsWith("```"))
                text = text.Substring(0, text.Length - 3);

            text = text.Trim();

            // 找到第一个 { 和最后一个 }
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start) return null;

            var jsonStr = text.Substring(start, end - start + 1);
            return JObject.Parse(jsonStr);
        }

        #endregion

        #region 安全

        /// <summary>
        /// XSS 防护：保留 Markdown 格式，转义危险 HTML 标签
        /// </summary>
        private string SanitizeOutput(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            return System.Text.RegularExpressions.Regex.Replace(
                content,
                @"<(?!/?(?:br|p|h[1-6]|ul|ol|li|strong|em|code|pre|blockquote|table|thead|tbody|tr|th|td|a|img|div|span)\b)[^>]+>",
                m => System.Net.WebUtility.HtmlEncode(m.Value));
        }

        #endregion
    }
}
