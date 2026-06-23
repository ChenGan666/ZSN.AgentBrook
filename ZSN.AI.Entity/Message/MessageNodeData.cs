using System;
using System.Collections.Generic;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// MessageNode 节点配置数据（Entity层，供 toTask() switch 使用）
    /// </summary>
    public class MessageNodeData: NodeData
    {
        public MessageNodeData()
        {
            label = "Message";
        }
        /// <summary>目标渠道 ID</summary>
        public string ChannelID { get; set; } = string.Empty;

        /// <summary>消息类型: text, markdown, image, file</summary>
        public string MessageType { get; set; } = "text";

        /// <summary>消息内容模板（支持 {{占位符}}）</summary>
        public string MessageTemplate { get; set; } = string.Empty;

        /// <summary>目标用户配置</summary>
        public TargetUserConfig TargetUserConfig { get; set; } = new TargetUserConfig();

        /// <summary>是否等待发送确认再触发下游</summary>
        public bool WaitForConfirmation { get; set; } = false;

        /// <summary>Provider 特定额外参数</summary>
        public Dictionary<string, string> ExtraParams { get; set; } = new Dictionary<string, string>();

        /// <summary>输入定义（标准字段）</summary>
        public List<Inputs> inputs { get; set; } = new List<Inputs>();

        /// <summary>输出变量定义（标准字段）</summary>
        public new List<Output> output { get; set; } = new List<Output>();
    }
}
