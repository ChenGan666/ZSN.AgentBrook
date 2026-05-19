namespace ZSN.AI.Node.VoiceNode.Models
{
    /// <summary>
    /// Provider 功能标识
    /// </summary>
    [Flags]
    public enum VoiceFeature
    {
        FileTranscription = 1,
        SpeakerDiarization = 2,
        EmotionDetection = 4,
        AudioEventDetection = 8,
        PunctuationRestoration = 16,
        HotwordBoosting = 32
    }
}
