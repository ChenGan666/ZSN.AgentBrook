namespace ZSN.AI.Node.VoiceNode.Models
{
    /// <summary>
    /// 转写进度
    /// </summary>
    public class VoiceProgress
    {
        /// <summary>进度百分比 0~100</summary>
        public int Percentage { get; set; }

        /// <summary>进度描述</summary>
        public string Message { get; set; }

        /// <summary>当前阶段</summary>
        public string Stage { get; set; }
    }
}
