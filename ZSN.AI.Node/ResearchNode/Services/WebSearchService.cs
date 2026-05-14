using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using ZSN.AI.Node.ResearchNode.Models;

namespace ZSN.AI.Node.ResearchNode.Services
{
    public class WebSearchService : IWebSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<ResearchNodeOptions> _options;
        private readonly ILogger<WebSearchService> _logger;
        private bool _jsonFormatSupported = true;

        public WebSearchService(
            HttpClient httpClient,
            IOptions<ResearchNodeOptions> options,
            ILogger<WebSearchService> logger)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(options.Value.SearchTimeoutSeconds);
            _options = options;
            _logger = logger;
        }

        public async Task<List<SearchResultItem>> SearchAsync(
            SearchPlan plan, HashSet<string> excludeUrls, CancellationToken ct = default)
        {
            var allResults = new List<SearchResultItem>();
            var seenUrls = new HashSet<string>(excludeUrls, StringComparer.OrdinalIgnoreCase);

            foreach (var keyword in plan.Keywords)
            {
                try
                {
                    var items = await SearchKeywordAsync(keyword, plan, ct);

                    foreach (var item in items)
                    {
                        if (string.IsNullOrEmpty(item.Url)) continue;
                        if (seenUrls.Contains(item.Url)) continue;
                        if (IsBlockedDomain(item.Url)) continue;
                        seenUrls.Add(item.Url);
                        allResults.Add(item);
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("[WebSearch] keyword={Keyword} 搜索超时", keyword);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[WebSearch] keyword={Keyword} 搜索失败", keyword);
                }
            }

            var maxResults = _options.Value.MaxResultsPerQuery * plan.Keywords.Count;
            return allResults
                .OrderByDescending(r => r.Score)
                .Take(maxResults)
                .ToList();
        }

        private async Task<List<SearchResultItem>> SearchKeywordAsync(string keyword, SearchPlan plan, CancellationToken ct)
        {
            // 优先尝试 JSON API
            if (_jsonFormatSupported)
            {
                var jsonUrl = BuildSearchUrl(keyword, plan, jsonFormat: true);
                var response = await _httpClient.GetAsync(jsonUrl, ct);

                if (response.IsSuccessStatusCode)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                    if (contentType.Contains("json"))
                    {
                        var json = await response.Content.ReadAsStringAsync(ct);
                        var items = ParseSearXNGJsonResponse(json);
                        if (items.Count > 0) return items;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogInformation("[WebSearch] JSON API 返回403，切换到 HTML 解析模式");
                    _jsonFormatSupported = false;
                }
            }

            // 降级到 HTML 解析
            var htmlUrl = BuildSearchUrl(keyword, plan, jsonFormat: false);
            var htmlResponse = await _httpClient.GetAsync(htmlUrl, ct);
            if (!htmlResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("[WebSearch] keyword={Keyword} HTML请求返回 {StatusCode}", keyword, htmlResponse.StatusCode);
                return new List<SearchResultItem>();
            }

            var html = await htmlResponse.Content.ReadAsStringAsync(ct);
            return ParseSearXNGHtmlResponse(html);
        }

        private string BuildSearchUrl(string keyword, SearchPlan plan, bool jsonFormat)
        {
            var baseUrl = _options.Value.SearXNGBaseUrl.TrimEnd('/');
            var categories = string.Join(",", plan.Categories ?? new List<string> { "general" });
            var query = $"{baseUrl}/search?q={Uri.EscapeDataString(keyword)}" +
                        $"&language={Uri.EscapeDataString(plan.Language ?? "zh-CN")}" +
                        $"&categories={Uri.EscapeDataString(categories)}";
            if (jsonFormat)
            {
                query += "&format=json";
            }
            if (!string.IsNullOrEmpty(plan.TimeRange))
            {
                query += $"&time_range={Uri.EscapeDataString(plan.TimeRange)}";
            }
            return query;
        }

        private List<SearchResultItem> ParseSearXNGJsonResponse(string json)
        {
            var results = new List<SearchResultItem>();
            try
            {
                var jObj = JObject.Parse(json);
                var resultsArray = jObj["results"] as JArray;
                if (resultsArray == null) return results;

                foreach (var r in resultsArray)
                {
                    results.Add(new SearchResultItem
                    {
                        Title = r["title"]?.ToString() ?? "",
                        Url = r["url"]?.ToString() ?? "",
                        Snippet = r["content"]?.ToString() ?? "",
                        Engine = r["engine"]?.ToString() ?? "",
                        Score = r["score"]?.ToObject<double>() ?? 0
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WebSearch] 解析 SearXNG JSON 响应失败");
            }
            return results;
        }

        /// <summary>
        /// 解析 SearXNG HTML 搜索结果页
        /// </summary>
        private List<SearchResultItem> ParseSearXNGHtmlResponse(string html)
        {
            var results = new List<SearchResultItem>();
            try
            {
                // 匹配每个搜索结果 article 元素
                var articlePattern = @"<article[^>]*class=""result[^""]*""[^>]*>(.*?)</article>";
                var articles = Regex.Matches(html, articlePattern, RegexOptions.Singleline);

                foreach (Match article in articles)
                {
                    var block = article.Groups[1].Value;

                    // 提取 URL
                    var urlMatch = Regex.Match(block, @"href=""([^""]+)""", RegexOptions.Singleline);
                    var url = urlMatch.Success ? urlMatch.Groups[1].Value : "";
                    if (string.IsNullOrEmpty(url)) continue;

                    // 提取标题
                    var titleMatch = Regex.Match(block, @"<h3[^>]*>(.*?)</h3>", RegexOptions.Singleline);
                    var title = titleMatch.Success
                        ? Regex.Replace(titleMatch.Groups[1].Value, @"<[^>]+>", "").Trim()
                        : "";

                    // 提取描述/Snippet
                    var snippetMatch = Regex.Match(block, @"<p[^>]*class=""[^""]*content[^""]*""[^>]*>(.*?)</p>", RegexOptions.Singleline);
                    if (!snippetMatch.Success)
                        snippetMatch = Regex.Match(block, @"<span[^>]*class=""[^""]*description[^""]*""[^>]*>(.*?)</span>", RegexOptions.Singleline);
                    var snippet = snippetMatch.Success
                        ? Regex.Replace(snippetMatch.Groups[1].Value, @"<[^>]+>", "").Trim()
                        : "";

                    // 提取引擎
                    var engineMatch = Regex.Match(block, @"<span[^>]*class=""[^""]*engine[^""]*""[^>]*>(.*?)</span>", RegexOptions.Singleline);
                    var engine = engineMatch.Success
                        ? Regex.Replace(engineMatch.Groups[1].Value, @"<[^>]+>", "").Trim()
                        : "";

                    results.Add(new SearchResultItem
                    {
                        Title = title,
                        Url = url,
                        Snippet = snippet,
                        Engine = engine,
                        Score = results.Count > 0 ? 1.0 - (results.Count * 0.05) : 1.0
                    });
                }

                _logger.LogDebug("[WebSearch] HTML 解析获取 {Count} 条结果", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WebSearch] 解析 SearXNG HTML 响应失败");
            }
            return results;
        }

        private bool IsBlockedDomain(string url)
        {
            var blocked = _options.Value.BlockedDomains;
            if (blocked == null || blocked.Count == 0) return false;

            try
            {
                var host = new Uri(url).Host;
                return blocked.Any(d =>
                    host.Equals(d, StringComparison.OrdinalIgnoreCase) ||
                    host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
            }
            catch { return false; }
        }

        /// <summary>
        /// 根据研究目标对搜索结果进行多维度评分排序
        /// </summary>
        public List<SearchResultItem> RankByRelevance(
            List<SearchResultItem> results, string researchGoal, int maxCount)
        {
            if (results == null || results.Count == 0) return results;

            var keywords = ExtractKeywords(researchGoal);

            return results
                .Select(r => new { Result = r, Score = CalculateRelevanceScore(r, keywords) })
                .OrderByDescending(x => x.Score)
                .Take(maxCount)
                .Select(x => x.Result)
                .ToList();
        }

        private double CalculateRelevanceScore(SearchResultItem item, List<string> keywords)
        {
            double score = item.Score;

            foreach (var kw in keywords)
            {
                if (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    score += 0.2;
                if (!string.IsNullOrEmpty(item.Snippet) && item.Snippet.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    score += 0.1;
            }

            // 权威域名加分
            try
            {
                var host = new Uri(item.Url).Host;
                var authoritative = new[] { "wikipedia.org", "github.com", "arxiv.org", "zhihu.com",
                    "stackoverflow.com", "microsoft.com", "openai.com", "nature.com", "ieee.org" };
                foreach (var auth in authoritative)
                {
                    if (host.EndsWith("." + auth, StringComparison.OrdinalIgnoreCase) ||
                        host.Equals(auth, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.3;
                        break;
                    }
                }
            }
            catch { }

            return score;
        }

        private static List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();

            // 简单关键词提取：分词后取长度>=2的词
            var words = new List<string>();
            var parts = text.Split(new[] { ' ', '　', ',', '，', '、', ';', '；', '\t', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Length >= 2) words.Add(trimmed);
            }

            // 对长中文文本按2-4字滑窗提取子串
            if (words.Count == 0 && text.Length >= 2)
            {
                for (int len = Math.Min(4, text.Length); len >= 2; len--)
                {
                    for (int i = 0; i <= text.Length - len; i++)
                    {
                        words.Add(text.Substring(i, len));
                    }
                    if (words.Count > 10) break;
                }
            }

            return words.Distinct().Take(10).ToList();
        }
    }
}
