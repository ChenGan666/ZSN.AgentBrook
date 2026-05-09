using Microsoft.AspNetCore.Mvc;
using ZSN.AgentBrowser.Models;

namespace ZSN.AgentBrowser.Controllers
{
    /// <summary>
    /// Agent-Browser API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BrowserController : ControllerBase
    {
        private readonly AgentBrowserService _browserService;
        private readonly ILogger<BrowserController> _logger;

        public BrowserController(AgentBrowserService browserService, ILogger<BrowserController> logger)
        {
            _browserService = browserService;
            _logger = logger;
        }

        /// <summary>
        /// 打开 URL
        /// </summary>
        [HttpPost("open")]
        public async Task<ActionResult<ApiResponse<CommandResponse>>> OpenAsync([FromBody] OpenUrlRequest request)
        {
            try
            {
                _logger.LogInformation("打开 URL: {Url}", request.Url);
                var result = await _browserService.OpenAsync(request.Url);
                
                var response = new CommandResponse
                {
                    Success = result.Success,
                    Output = result.Output,
                    Error = result.Error,
                    ExitCode = result.ExitCode
                };

                return Ok(ApiResponse<CommandResponse>.SuccessResponse(response, "URL 打开成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开 URL 失败");
                return BadRequest(ApiResponse<CommandResponse>.ErrorResponse(ex.Message, "打开 URL 失败"));
            }
        }

        /// <summary>
        /// 获取页面快照
        /// </summary>
        [HttpPost("snapshot")]
        public async Task<ActionResult<ApiResponse<SnapshotResponse>>> SnapshotAsync([FromBody] SnapshotRequest request)
        {
            try
            {
                _logger.LogInformation("获取页面快照，包含交互元素: {IncludeInteractive}", request.IncludeInteractive);
                var result = await _browserService.SnapshotAsync(request.IncludeInteractive);

                var response = new SnapshotResponse
                {
                    Success = result.Success,
                    Error = result.Error,
                    Elements = result.Elements.Select(e => new PageElementDto
                    {
                        Type = e.Type,
                        Text = e.Text,
                        Ref = e.Ref
                    }).ToList()
                };

                return Ok(ApiResponse<SnapshotResponse>.SuccessResponse(response, $"快照获取成功，找到 {response.Elements.Count} 个元素"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取快照失败");
                return BadRequest(ApiResponse<SnapshotResponse>.ErrorResponse(ex.Message, "获取快照失败"));
            }
        }

        /// <summary>
        /// 点击元素
        /// </summary>
        [HttpPost("click")]
        public async Task<ActionResult<ApiResponse<CommandResponse>>> ClickAsync([FromBody] ClickRequest request)
        {
            try
            {
                _logger.LogInformation("点击元素: {ElementRef}", request.ElementRef);
                var result = await _browserService.ClickAsync(request.ElementRef);

                var response = new CommandResponse
                {
                    Success = result.Success,
                    Output = result.Output,
                    Error = result.Error,
                    ExitCode = result.ExitCode
                };

                return Ok(ApiResponse<CommandResponse>.SuccessResponse(response, "点击成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "点击失败");
                return BadRequest(ApiResponse<CommandResponse>.ErrorResponse(ex.Message, "点击失败"));
            }
        }

        /// <summary>
        /// 输入文本
        /// </summary>
        [HttpPost("type")]
        public async Task<ActionResult<ApiResponse<CommandResponse>>> TypeAsync([FromBody] TypeRequest request)
        {
            try
            {
                _logger.LogInformation("输入文本到元素: {ElementRef}", request.ElementRef);
                var result = await _browserService.TypeAsync(request.ElementRef, request.Text);

                var response = new CommandResponse
                {
                    Success = result.Success,
                    Output = result.Output,
                    Error = result.Error,
                    ExitCode = result.ExitCode
                };

                return Ok(ApiResponse<CommandResponse>.SuccessResponse(response, "输入成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "输入失败");
                return BadRequest(ApiResponse<CommandResponse>.ErrorResponse(ex.Message, "输入失败"));
            }
        }

        /// <summary>
        /// 按键操作
        /// </summary>
        [HttpPost("press")]
        public async Task<ActionResult<ApiResponse<CommandResponse>>> PressAsync([FromBody] PressRequest request)
        {
            try
            {
                _logger.LogInformation("按键: {Key}", request.Key);
                var result = await _browserService.PressAsync(request.Key);

                var response = new CommandResponse
                {
                    Success = result.Success,
                    Output = result.Output,
                    Error = result.Error,
                    ExitCode = result.ExitCode
                };

                return Ok(ApiResponse<CommandResponse>.SuccessResponse(response, "按键成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按键失败");
                return BadRequest(ApiResponse<CommandResponse>.ErrorResponse(ex.Message, "按键失败"));
            }
        }

        /// <summary>
        /// 获取页面内容
        /// </summary>
        [HttpGet("content")]
        public async Task<ActionResult<ApiResponse<ContentResponse>>> GetContentAsync()
        {
            try
            {
                _logger.LogInformation("获取页面内容");
                var content = await _browserService.GetContentAsync();

                var response = new ContentResponse { Content = content };
                return Ok(ApiResponse<ContentResponse>.SuccessResponse(response, "内容获取成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取内容失败");
                return BadRequest(ApiResponse<ContentResponse>.ErrorResponse(ex.Message, "获取内容失败"));
            }
        }

        /// <summary>
        /// 获取当前 URL
        /// </summary>
        [HttpGet("url")]
        public async Task<ActionResult<ApiResponse<UrlResponse>>> GetUrlAsync()
        {
            try
            {
                _logger.LogInformation("获取当前 URL");
                var url = await _browserService.GetUrlAsync();

                var response = new UrlResponse { Url = url };
                return Ok(ApiResponse<UrlResponse>.SuccessResponse(response, "URL 获取成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取 URL 失败");
                return BadRequest(ApiResponse<UrlResponse>.ErrorResponse(ex.Message, "获取 URL 失败"));
            }
        }

        /// <summary>
        /// 截图
        /// </summary>
        [HttpPost("screenshot")]
        public async Task<ActionResult<ApiResponse<ScreenshotResponse>>> ScreenshotAsync([FromBody] ScreenshotRequest request)
        {
            try
            {
                _logger.LogInformation("截图: {FilePath}", request.FilePath);
                var (success, screenshotUrl, filePath, error) = await _browserService.SaveScreenshotAsync(request.FilePath);

                var response = new ScreenshotResponse
                {
                    Success = success,
                    ScreenshotUrl = screenshotUrl,
                    FilePath = filePath,
                    Error = error
                };

                if (success)
                {
                    return Ok(ApiResponse<ScreenshotResponse>.SuccessResponse(response, $"截图成功: {screenshotUrl}"));
                }
                else
                {
                    return BadRequest(ApiResponse<ScreenshotResponse>.ErrorResponse(error, "截图失败"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "截图失败");
                return BadRequest(ApiResponse<ScreenshotResponse>.ErrorResponse(ex.Message, "截图失败"));
            }
        }

        /// <summary>
        /// 关闭浏览器
        /// </summary>
        [HttpPost("close")]
        public async Task<ActionResult<ApiResponse<CommandResponse>>> CloseAsync()
        {
            try
            {
                _logger.LogInformation("关闭浏览器");
                var result = await _browserService.CloseAsync();

                var response = new CommandResponse
                {
                    Success = result.Success,
                    Output = result.Output,
                    Error = result.Error,
                    ExitCode = result.ExitCode
                };

                return Ok(ApiResponse<CommandResponse>.SuccessResponse(response, "浏览器已关闭"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭浏览器失败");
                return BadRequest(ApiResponse<CommandResponse>.ErrorResponse(ex.Message, "关闭浏览器失败"));
            }
        }

        /// <summary>
        /// 执行自定义命令
        /// </summary>
        [HttpPost("execute")]
        public async Task<ActionResult<ApiResponse<CommandResponse>>> ExecuteCommandAsync([FromBody] ExecuteCommandRequest request)
        {
            try
            {
                _logger.LogInformation("执行命令: {Command}", request.Command);
                var result = await _browserService.ExecuteCommandAsync(request.Command);

                var response = new CommandResponse
                {
                    Success = result.Success,
                    Output = result.Output,
                    Error = result.Error,
                    ExitCode = result.ExitCode
                };

                return Ok(ApiResponse<CommandResponse>.SuccessResponse(response, "命令执行成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行命令失败");
                return BadRequest(ApiResponse<CommandResponse>.ErrorResponse(ex.Message, "执行命令失败"));
            }
        }
    }
}
