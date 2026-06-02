using System;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_msg_route_rule
    /// </summary>
    public partial class MessageRouteRuleInfo
    {
        public MessageRouteRuleInfo() { }

        /// <summary>规则唯一ID</summary>
        public string RuleID { get; set; } = string.Empty;

        /// <summary>关联渠道ID（空=所有渠道）</summary>
        public string ChannelID { get; set; }

        /// <summary>规则名称</summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>匹配类型: All, Keyword, Regex, Intent</summary>
        public string MatchType { get; set; } = "All";

        /// <summary>匹配条件JSON</summary>
        public string MatchCondition { get; set; }

        /// <summary>目标应用ID</summary>
        public string TargetAppID { get; set; }

        /// <summary>自定义inputs映射JSON</summary>
        public string InputMapping { get; set; }

        /// <summary>会话超时分钟数（0=每次新建）</summary>
        public Int32 SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>未匹配时是否自动回复</summary>
        public Int32 EnableAutoReply { get; set; } = 0;

        /// <summary>自动回复内容模板</summary>
        public string AutoReplyContent { get; set; }

        /// <summary>优先级（越大越高）</summary>
        public Int32 Priority { get; set; } = 0;

        /// <summary>是否启用</summary>
        public Int32 Enabled { get; set; } = 1;

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
}
