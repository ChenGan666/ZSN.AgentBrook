using MySql.Data.MySqlClient;
using StackExchange.Redis;
using System.Data;
using ZSN.AgentBrook.Web.Manage.Models.Welcome;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.Web.Manage.Services
{
    /// <summary>
    /// 首次运行环境检测服务实现
    /// </summary>
    public class WelcomeEnvironmentService : IWelcomeEnvironmentService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WelcomeEnvironmentService> _logger;

        public WelcomeEnvironmentService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<WelcomeEnvironmentService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<EnvironmentCheckResult> CheckAllAsync()
        {
            var result = new EnvironmentCheckResult();

            var (dbOk, dbMsg) = await CheckDatabaseInternalAsync();
            var (apiOk, apiMsg) = await CheckApiInternalAsync();
            var (redisOk, redisMsg) = await CheckRedisInternalAsync();

            result.DatabaseOk = dbOk;
            result.DatabaseMessage = dbMsg;
            result.ApiOk = apiOk;
            result.ApiMessage = apiMsg;
            result.RedisOk = redisOk;
            result.RedisMessage = redisMsg;
            result.AllOk = dbOk && apiOk && redisOk;

            _logger.LogInformation("[WelcomeEnvironment] 环境检测完成");

            return result;
        }

        public async Task<bool> CheckDatabaseAsync()
        {
            var (ok, _) = await CheckDatabaseInternalAsync();
            return ok;
        }

        public async Task<bool> CheckApiAsync()
        {
            var (ok, _) = await CheckApiInternalAsync();
            return ok;
        }

        public async Task<bool> CheckRedisAsync()
        {
            var (ok, _) = await CheckRedisInternalAsync();
            return ok;
        }

        private async Task<(bool, string)> CheckDatabaseInternalAsync()
        {
            try
            {
                var dbType = ConfigHelper.GetString("DbConnectionStrings:BaseDb:DbType", "MySql");
                var connectionString = ConfigHelper.GetString("DbConnectionStrings:BaseDb:Connection", "");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return (false, "未配置数据库连接字符串");
                }

                if (dbType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
                {
                    using var connection = new MySqlConnection(connectionString);
                    await connection.OpenAsync();
                    using var command = new MySqlCommand("SELECT 1", connection);
                    await command.ExecuteScalarAsync();
                    return (true, string.Empty);
                }

                return (false, $"暂不支持的数据库类型: {dbType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WelcomeEnvironment] 数据库检测失败");
                return (false, ex.Message);
            }
        }

        private async Task<(bool, string)> CheckApiInternalAsync()
        {
            try
            {
                var apiUrl = _configuration.GetValue<string>("Welcome:ApiCheckUrl") ?? "http://127.0.0.1:5003";
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.GetAsync($"{apiUrl.TrimEnd('/')}/api/File/Get?filecode=welcome-check");
                var ok = response.StatusCode == System.Net.HttpStatusCode.OK
                    || response.StatusCode == System.Net.HttpStatusCode.BadRequest
                    || response.StatusCode == System.Net.HttpStatusCode.NotFound;
                return (ok, ok ? string.Empty : $"API 返回状态码: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WelcomeEnvironment] API 检测失败");
                return (false, ex.Message);
            }
        }

        private async Task<(bool, string)> CheckRedisInternalAsync()
        {
            try
            {
                var redisConnectionString = _configuration.GetValue<string>("Welcome:RedisConnectionString")
                    ?? ConfigHelper.GetString("RedisConnectionString", "127.0.0.1:6379");

                using var connection = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
                var db = connection.GetDatabase();
                await db.StringSetAsync("agentbrook:welcome:check", "ok", TimeSpan.FromSeconds(10));
                var value = await db.StringGetAsync("agentbrook:welcome:check");
                return value == "ok" ? (true, string.Empty) : (false, "Redis 测试值不匹配");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WelcomeEnvironment] Redis 检测失败");
                return (false, ex.Message);
            }
        }
    }
}
