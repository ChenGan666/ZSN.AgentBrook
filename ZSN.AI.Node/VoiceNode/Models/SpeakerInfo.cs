namespace ZSN.AI.Node.VoiceNode.Models
{
    /// <summary>
    /// 说话人信息
    /// </summary>
    public class SpeakerInfo
    {
        /// <summary>说话人 ID</summary>
        public string SpeakerId { get; set; }

        /// <summary>说话人显示标签</summary>
        public string Label { get; set; }

        /// <summary>总发言时长（秒）</summary>
        public double TotalSpeakingSeconds { get; set; }

        /// <summary>发言段数</summary>
        public int SegmentCount { get; set; }
    }
}
