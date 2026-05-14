using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ZSN.AI.Node.ResearchNode
{
    public class PlaywrightBrowserPool : IAsyncDisposable
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly SemaphoreSlim _pageSemaphore;
        private readonly ResearchNodeOptions _options;
        private readonly ILogger<PlaywrightBrowserPool> _logger;
        private bool _initialized;
        private bool _available = true;

        public PlaywrightBrowserPool(
            IOptions<ResearchNodeOptions> options,
            ILogger<PlaywrightBrowserPool> logger)
        {
            _options = options.Value;
            _logger = logger;
            _pageSemaphore = new SemaphoreSlim(_options.MaxConcurrentFetches);
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;
            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;
                _playwright = await Playwright.CreateAsync();
                var execPath = _options.ChromiumExecutablePath
                    ?? Environment.GetEnvironmentVariable("CHROMIUM_EXECUTABLE_PATH");

                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = _options.Headless,
                    ExecutablePath = execPath,
                    Args = new[] { "--no-sandbox", "--disable-dev-shm-usage", "--disable-gpu" }
                });
                _initialized = true;
                _logger.LogInformation("[BrowserPool] Playwright Chromium 浏览器已启动 (Headless={Headless}, ExecPath={ExecPath})",
                    _options.Headless, execPath ?? "bundled");
            }
            catch (Exception ex)
            {
                _available = false;
                _logger.LogWarning(ex, "[BrowserPool] Playwright 初始化失败，将降级为 Snippet 模式");
                throw;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<IPage> AcquirePageAsync(CancellationToken ct = default)
        {
            await _pageSemaphore.WaitAsync(ct);
            await EnsureInitializedAsync();

            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = _options.BrowserUserAgent,
                IgnoreHTTPSErrors = true
            });
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(_options.PageTimeoutSeconds * 1000);
            return page;
        }

        public async Task ReleasePageAsync(IPage page)
        {
            try
            {
                var context = page.Context;
                await page.CloseAsync();
                await context.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[BrowserPool] 关闭 Page 时出错（可忽略）");
            }
            finally
            {
                _pageSemaphore.Release();
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            if (!_available) return false;
            try
            {
                await EnsureInitializedAsync();
                return _initialized && _browser != null && _browser.IsConnected;
            }
            catch
            {
                _available = false;
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null)
            {
                try { await _browser.CloseAsync(); } catch { }
            }
            _playwright?.Dispose();
            _logger.LogInformation("[BrowserPool] Playwright 浏览器已关闭");
        }
    }
}
