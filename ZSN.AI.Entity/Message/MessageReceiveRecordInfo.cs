using System;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_msg_receive_record
    /// </summary>
    public partial class MessageReceiveRecordInfo
    {
        public MessageReceiveRecordInfo() { }

        /// <summary>记录唯一ID</summary>
        public string RecordID { get; set; } = string.Empty;

        /// <summary>关联渠道ID</summary>
        public string ChannelID { get; set; } = string.Empty;

        /// <summary>平台事件唯一ID（幂等去重）</summary>
        public string EventId { get; set; } = string.Empty;

        /// <summary>来源Provider类型</summary>
        public string ProviderType { get; set; } = string.Empty;

        /// <summary>发送者ID</summary>
        public string FromUser { get; set; }

        /// <summary>发送者显示名称</summary>
        public string FromUserName { get; set; }

        /// <summary>消息类型</summary>
        public string MessageType { get; set; } = "text";

        /// <summary>消息文本内容</summary>
        public string Content { get; set; }

        /// <summary>原始IM平台消息JSON</summary>
        public string RawPayload { get; set; }

        /// <summary>路由到的工作流ID</summary>
        public string RoutedWorkflowID { get; set; }

        /// <summary>创建的任务ID</summary>
        public string RoutedTaskID { get; set; }

        /// <summary>路由状态: 0=待路由, 1=已路由, -1=未匹配</summary>
        public Int32 RouteStatus { get; set; } = 0;

        /// <summary>消息接收时间</summary>
        public DateTime? ReceiveTime { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
