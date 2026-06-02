using System;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_msg_send_record
    /// </summary>
    public partial class MessageSendRecordInfo
    {
        public MessageSendRecordInfo() { }

        /// <summary>记录唯一ID</summary>
        public string RecordID { get; set; } = string.Empty;

        /// <summary>关联渠道ID</summary>
        public string ChannelID { get; set; } = string.Empty;

        /// <summary>关联会话ID</summary>
        public string SessionID { get; set; }

        /// <summary>关联任务ID</summary>
        public string TaskID { get; set; }

        /// <summary>关联节点ID</summary>
        public string NodeID { get; set; }

        /// <summary>消息类型</summary>
        public string MessageType { get; set; } = "text";

        /// <summary>发送内容</summary>
        public string Content { get; set; }

        /// <summary>目标用户/群组</summary>
        public string TargetUser { get; set; }

        /// <summary>发送状态: 0=待发送, 1=成功, -1=失败</summary>
        public Int32 SendStatus { get; set; } = 0;

        /// <summary>IM平台消息ID</summary>
        public string PlatformMessageId { get; set; }

        /// <summary>实际重试次数</summary>
        public Int32 RetryCount { get; set; } = 0;

        /// <summary>错误信息</summary>
        public string ErrorMessage { get; set; }

        /// <summary>发送时间</summary>
        public DateTime? SendTime { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
