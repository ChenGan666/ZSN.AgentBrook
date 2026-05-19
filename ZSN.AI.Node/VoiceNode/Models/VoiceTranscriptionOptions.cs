namespace ZSN.AI.Node.VoiceNode.Models
{
    /// <summary>
    /// 转写选项
    /// </summary>
    public class VoiceTranscriptionOptions
    {
        public bool EnableSpeakerDiarization { get; set; }
        public int ExpectedSpeakerCount { get; set; }
        public bool EnableEmotionDetection { get; set; }
        public bool EnableAudioEventDetection { get; set; }
        public string Language { get; set; } = "auto";
        public Dictionary<string, int> Hotwords { get; set; }
    }
}
