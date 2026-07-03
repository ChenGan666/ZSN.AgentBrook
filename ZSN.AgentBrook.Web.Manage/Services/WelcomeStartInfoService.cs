using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using ZSN.AgentBrook.Web.Manage.Models.Welcome;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.Web.Manage.Services
{
    /// <summary>
    /// 首次运行信息提交服务实现
    /// </summary>
    public class WelcomeStartInfoService : IWelcomeStartInfoService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WelcomeStartInfoService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WelcomeStartInfoService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<WelcomeStartInfoService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string?> SubmitAsync(bool consent)
        {
            if (!consent)
            {
                _logger.LogInformation("[WelcomeStartInfo] 用户不同意发送匿名统计信息，跳过提交");
                return null;
            }

            try
            {
                var request = BuildRequest();
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var serviceUrl = _configuration.GetValue<string>("Welcome:ServiceUrl") ?? "http://localhost:5008";
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.PostAsync($"{serviceUrl.TrimEnd('/')}/api/start/start", content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[WelcomeStartInfo] 提交失败: {StatusCode}", response.StatusCode);
                    return null;
                }

                var result = JsonConvert.DeserializeObject<StartInfoResponse>(responseText);
                if (result?.Success == true && !string.IsNullOrWhiteSpace(result.InstallationId))
                {
                    _logger.LogInformation("[WelcomeStartInfo] 提交成功");
                    return result.InstallationId;
                }

                _logger.LogWarning("[WelcomeStartInfo] 提交响应异常");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WelcomeStartInfo] 提交过程发生异常");
                return null;
            }
        }

        public async Task<string?> GetAgentBrookVersionAsync()
        {
            await Task.CompletedTask;
            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString(3) ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }

        private StartInfoRequest BuildRequest()
        {
            var os = GetOperatingSystem();
            var ip = GetClientIp();
            var country = "CN";
            var firstRunTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            return new StartInfoRequest
            {
                Version = _configuration.GetValue<string>("Welcome:Version") ?? GetAgentBrookVersionAsync().Result ?? "1.0.0",
                Os = os,
                Country = country,
                Ip = ip,
                FirstRunTime = firstRunTime,
                Consent = true
            };
        }

        private string GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return $"Windows {RuntimeInformation.OSArchitecture}";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return $"Linux {RuntimeInformation.OSArchitecture}";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return $"macOS {RuntimeInformation.OSArchitecture}";
            return RuntimeInformation.OSDescription;
        }

        private string GetClientIp()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null)
                    return string.Empty;

                var headers = new[] { "X-Forwarded-For", "X-Real-IP" };
                foreach (var header in headers)
                {
                    var value = context.Request.Headers[header].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var ip = value.Split(',')[0].Trim();
                        if (!string.IsNullOrWhiteSpace(ip))
                            return ip;
                    }
                }

                return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }
}
