namespace ZSN.AI.Node.VoiceNode.Models
{
    /// <summary>
    /// 音频分段信息
    /// </summary>
    public class AudioSegmentInfo
    {
        public string FilePath { get; set; }
        public double StartTimeSeconds { get; set; }
        public double EndTimeSeconds { get; set; }
        public double DurationSeconds { get; set; }
    }
}
