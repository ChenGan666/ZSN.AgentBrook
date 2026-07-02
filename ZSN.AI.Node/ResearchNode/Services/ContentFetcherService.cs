using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using ZSN.AI.Node.ResearchNode.Models;

namespace ZSN.AI.Node.ResearchNode.Services
{
    public class ContentFetcherService : IContentFetcherService
    {
        private readonly PlaywrightBrowserPool _browserPool;
        private readonly IContentCache _cache;
        private readonly IOptions<ResearchNodeOptions> _options;
        private readonly ILogger<ContentFetcherService> _logger;

        public ContentFetcherService(
            PlaywrightBrowserPool browserPool,
            IContentCache cache,
            IOptions<ResearchNodeOptions> options,
            ILogger<ContentFetcherService> logger)
        {
            _browserPool = browserPool;
            _cache = cache;
            _options = options;
            _logger = logger;
        }

        public async Task<List<WebPageContent>> FetchAsync(
            List<string> urls, int maxContentLength = 5000,
            IProgress<string> progress = null, CancellationToken ct = default)
        {
            if (urls == null || urls.Count == 0) return new List<WebPageContent>();

            // 域名黑名单过滤
            var filteredUrls = urls.Where(url => !IsBlockedDomain(url)).ToList();
            if (filteredUrls.Count < urls.Count)
            {
                _logger.LogDebug("[ContentFetcher] 黑名单过滤: {Filtered}/{Total} 个URL被排除",
                    urls.Count - filteredUrls.Count, urls.Count);
            }

            var results = new List<WebPageContent>();
            int completed = 0;

            foreach (var url in filteredUrls)
            {
                // 先查缓存
                if (_options.Value.EnableContentCache)
                {
                    try
                    {
                        var cached = await _cache.GetAsync(url);
                        if (cached != null && cached.Success)
                        {
                            _logger.LogDebug("[ContentFetcher] 缓存命中: {Url}", url);
                            results.Add(cached);
                            completed++;
                            progress?.Report($"\n📄 抓取中: {completed}/{filteredUrls.Count} 页完成\n");
                            continue;
                        }
                    }
                    catch { /* 缓存读取失败不影响主流程 */ }
                }

                // 实际抓取
                var content = await FetchSingleAsync(url, maxContentLength, ct);
                results.Add(content);
                completed++;
                progress?.Report($"\n📄 抓取中: {completed}/{filteredUrls.Count} 页完成\n");

                // 写入缓存
                if (_options.Value.EnableContentCache && content.Success)
                {
                    try { await _cache.SetAsync(url, content); } catch { }
                }
            }

            return results;
        }

        private async Task<WebPageContent> FetchSingleAsync(string url, int maxContentLength, CancellationToken ct)
        {
            var result = new WebPageContent { Url = url, FetchTime = DateTime.UtcNow };
            IPage page = null;

            try
            {
                page = await _browserPool.AcquirePageAsync(ct);

                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });

                // 提取标题
                result.Title = await page.TitleAsync();

                // 提取正文（优先级：article > main > [role=main] > body 清理版）
                var content = await page.EvaluateAsync<string>(@"() => {
                    const article = document.querySelector('article');
                    if (article && article.innerText.trim().length > 100) return article.innerText;

                    const main = document.querySelector('main');
                    if (main && main.innerText.trim().length > 100) return main.innerText;

                    const roleMain = document.querySelector('[role=""main""]');
                    if (roleMain && roleMain.innerText.trim().length > 100) return roleMain.innerText;

                    // 清理版 body
                    const clone = document.body.cloneNode(true);
                    clone.querySelectorAll('nav,footer,aside,script,style,header,[role=""navigation""],iframe,svg')
                         .forEach(el => el.remove());
                    const text = clone.innerText;
                    return text;
                }");

                // 截断（优先在句号或换行处截断）
                if (content != null && content.Length > maxContentLength)
                {
                    var cutPoint = content.LastIndexOf('。', maxContentLength);
                    if (cutPoint < maxContentLength * 0.5)
                        cutPoint = content.LastIndexOf('\n', maxContentLength);
                    if (cutPoint < maxContentLength * 0.5)
                        cutPoint = maxContentLength;
                    content = content.Substring(0, cutPoint);
                }

                result.Content = content ?? "";
                result.ContentLength = result.Content.Length;
                result.Success = result.ContentLength > 0;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                _logger.LogDebug(ex, "[ContentFetcher] URL={Url} 抓取失败", url);
            }
            finally
            {
                if (page != null) await _browserPool.ReleasePageAsync(page);
            }

            return result;
        }

        public async Task<bool> IsPlaywrightAvailableAsync()
        {
            return await _browserPool.IsAvailableAsync();
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
            catch { return true; }
        }
    }
}
