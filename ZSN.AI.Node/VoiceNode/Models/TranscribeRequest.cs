namespace ZSN.AI.Node.VoiceNode.Models
{
    /// <summary>
    /// 转写请求
    /// </summary>
    public class TranscribeRequest
    {
        /// <summary>音频文件路径（已预处理为标准格式）</summary>
        public string AudioFilePath { get; set; }

        /// <summary>原始文件名</summary>
        public string OriginalFileName { get; set; }

        /// <summary>音频时长（秒）</summary>
        public double DurationSeconds { get; set; }

        /// <summary>转写选项</summary>
        public VoiceTranscriptionOptions Options { get; set; }
    }
}
