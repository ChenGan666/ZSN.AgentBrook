namespace ZSN.AI.Node.ServiceDesk.Models
{
    /// <summary>会话状态枚举</summary>
    public enum SessionState
    {
        /// <summary>空闲状态</summary>
        Idle = 0,

        /// <summary>信息收集中</summary>
        InformationGathering = 1,

        /// <summary>处理请求中</summary>
        ProcessingRequest = 2,

        /// <summary>等待用户确认</summary>
        WaitingForConfirmation = 3,

        /// <summary>已升级到 ClawAI</summary>
        Escalated = 4,

        /// <summary>已完成</summary>
        Completed = 5
    }

    /// <summary>会话状态上下文</summary>
    public class SessionStateContext
    {
        /// <summary>会话 ID</summary>
        public string SessionId { get; set; }

        /// <summary>应用 ID</summary>
        public string AppId { get; set; }

        /// <summary>用户 ID</summary>
        public string MemberId { get; set; }

        /// <summary>当前状态</summary>
        public SessionState CurrentState { get; set; } = SessionState.Idle;

        /// <summary>检测到的意图</summary>
        public string DetectedIntent { get; set; }

        /// <summary>已收集的信息</summary>
        public Dictionary<string, string> CollectedInfo { get; set; } = new Dictionary<string, string>();

        /// <summary>缺失的字段</summary>
        public List<string> MissingFields { get; set; } = new List<string>();

        /// <summary>状态转换历史</summary>
        public List<StateTransition> StateHistory { get; set; } = new List<StateTransition>();

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>最后更新时间</summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;
    }

    /// <summary>状态转换记录</summary>
    public class StateTransition
    {
        public SessionState FromState { get; set; }
        public SessionState ToState { get; set; }
        public string Reason { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
