namespace ZSN.AgentBrook.Web.Manage.Models.Welcome
{
    /// <summary>
    /// 服务端返回结果
    /// </summary>
    public class StartInfoResponse
    {
        public bool Success { get; set; }
        public string InstallationId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
