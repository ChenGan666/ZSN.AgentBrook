namespace ZSN.AgentBrook.Web.Manage.Models.Welcome
{
    /// <summary>
    /// 提交到服务端的匿名统计信息
    /// </summary>
    public class StartInfoRequest
    {
        public string Version { get; set; } = string.Empty;
        public string Os { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string FirstRunTime { get; set; } = string.Empty;
        public bool Consent { get; set; }
    }
}
