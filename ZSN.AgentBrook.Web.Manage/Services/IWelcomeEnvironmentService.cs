using ZSN.AgentBrook.Web.Manage.Models.Welcome;

namespace ZSN.AgentBrook.Web.Manage.Services
{
    /// <summary>
    /// 首次运行环境检测服务接口
    /// </summary>
    public interface IWelcomeEnvironmentService
    {
        Task<EnvironmentCheckResult> CheckAllAsync();
        Task<bool> CheckDatabaseAsync();
        Task<bool> CheckApiAsync();
        Task<bool> CheckRedisAsync();
    }
}
