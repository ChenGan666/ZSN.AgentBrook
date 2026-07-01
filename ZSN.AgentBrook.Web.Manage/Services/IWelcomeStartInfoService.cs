namespace ZSN.AgentBrook.Web.Manage.Services
{
    /// <summary>
    /// 首次运行信息提交服务接口
    /// </summary>
    public interface IWelcomeStartInfoService
    {
        Task<string?> SubmitAsync(bool consent);
        Task<string?> GetAgentBrookVersionAsync();
    }
}
