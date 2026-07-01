namespace ZSN.AgentBrook.Web.Manage.Models.Welcome
{
    /// <summary>
    /// 环境检测结果
    /// </summary>
    public class EnvironmentCheckResult
    {
        public bool DatabaseOk { get; set; }
        public string DatabaseMessage { get; set; } = string.Empty;

        public bool ApiOk { get; set; }
        public string ApiMessage { get; set; } = string.Empty;

        public bool RedisOk { get; set; }
        public string RedisMessage { get; set; } = string.Empty;

        public bool AllOk { get; set; }
    }
}
