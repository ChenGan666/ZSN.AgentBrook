namespace ZSN.AI.Node.VoiceNode.Models
{
    /// <summary>
    /// 转写结果
    /// </summary>
    public class TranscriptionResult
    {
        /// <summary>完整文本</summary>
        public string FullText { get; set; }

        /// <summary>分段结果</summary>
        public List<TranscriptionSegment> Segments { get; set; } = new();

        /// <summary>检测到的说话人列表</summary>
        public List<SpeakerInfo> Speakers { get; set; } = new();

        /// <summary>总音频时长（秒）</summary>
        public double DurationSeconds { get; set; }

        /// <summary>检测到的语言</summary>
        public string DetectedLanguage { get; set; }

        /// <summary>转写耗时（毫秒）</summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>使用的 Provider 名称</summary>
        public string Provider { get; set; }

        /// <summary>是否为降级结果</summary>
        public bool IsFallback { get; set; }

        /// <summary>原始响应 JSON（调试用）</summary>
        public string RawResponse { get; set; }
    }
}
