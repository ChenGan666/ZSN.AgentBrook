using Microsoft.AspNetCore.Mvc;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AgentBrook.Web.Manage.Services;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    /// <summary>
    /// 首次运行欢迎向导控制器
    /// </summary>
    [Area("Manage")]
    [AdminAttributes(CheckLogin = false, CheckUrl = false, CheckPermissions = false)]
    public class WelcomeController : AdminBaseController
    {
        private readonly IWelcomeEnvironmentService _environmentService;
        private readonly IWelcomeStartInfoService _startInfoService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WelcomeController> _logger;

        public WelcomeController(
            IWelcomeEnvironmentService environmentService,
            IWelcomeStartInfoService startInfoService,
            IConfiguration configuration,
            ILogger<WelcomeController> logger)
        {
            _environmentService = environmentService;
            _startInfoService = startInfoService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 欢迎页面
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 环境检测接口
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CheckEnvironment()
        {
            var result = await _environmentService.CheckAllAsync();
            return Json(result);
        }

        /// <summary>
        /// 提交匿名统计信息
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitStartInfo(bool consent)
        {
            try
            {
                var installationId = await _startInfoService.SubmitAsync(consent);

                // 无论是否提交成功，都标记首次运行已完成
                var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                ConfigHelper.SetConfigurationValue("Welcome:FirstRun", "false");
                ConfigHelper.SetConfigurationValue("Welcome:FirstRunTime", now);

                if (!string.IsNullOrWhiteSpace(installationId))
                {
                    ConfigHelper.SetConfigurationValue("Welcome:InstallationId", installationId);
                }

                _logger.LogInformation("[Welcome] 首次运行向导完成");

                return Json(new { success = true, installationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Welcome] 提交启动信息失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 跳过发送（不同意时标记完成）
        /// </summary>
        [HttpPost]
        public IActionResult Skip()
        {
            try
            {
                var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                ConfigHelper.SetConfigurationValue("Welcome:FirstRun", "false");
                ConfigHelper.SetConfigurationValue("Welcome:FirstRunTime", now);

                _logger.LogInformation("[Welcome] 用户跳过发送，标记首次运行完成");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Welcome] 跳过失败");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
