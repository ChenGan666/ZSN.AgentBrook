namespace ZSN.AI.Node.VoiceNode.Models
{
    /// <summary>
    /// 音频预处理结果
    /// </summary>
    public class AudioPreprocessResult
    {
        /// <summary>处理后的文件路径</summary>
        public string ProcessedFilePath { get; set; }

        /// <summary>音频时长（秒）</summary>
        public double DurationSeconds { get; set; }

        /// <summary>原始格式</summary>
        public string OriginalFormat { get; set; }

        /// <summary>是否进行了格式转换</summary>
        public bool WasConverted { get; set; }

        /// <summary>分段列表（长音频 VAD 分段后）</summary>
        public List<AudioSegmentInfo> Segments { get; set; }

        /// <summary>是否需要清理临时文件</summary>
        public bool RequiresCleanup { get; set; }
    }
}
