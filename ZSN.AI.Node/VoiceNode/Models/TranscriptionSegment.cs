namespace ZSN.AI.Node.VoiceNode.Models
{
    /// <summary>
    /// 转写分段
    /// </summary>
    public class TranscriptionSegment
    {
        /// <summary>分段文本</summary>
        public string Text { get; set; }

        /// <summary>开始时间（毫秒）</summary>
        public long StartTimeMs { get; set; }

        /// <summary>结束时间（毫秒）</summary>
        public long EndTimeMs { get; set; }

        /// <summary>说话人标识（如 "Speaker_0"）</summary>
        public string SpeakerId { get; set; }

        /// <summary>说话人显示名称（映射后）</summary>
        public string SpeakerLabel { get; set; }

        /// <summary>情感标签</summary>
        public string Emotion { get; set; }

        /// <summary>音频事件标签</summary>
        public string AudioEvent { get; set; }

        /// <summary>置信度 0~1</summary>
        public double Confidence { get; set; }
    }
}
