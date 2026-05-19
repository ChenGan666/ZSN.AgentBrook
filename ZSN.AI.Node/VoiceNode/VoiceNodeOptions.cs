namespace ZSN.AI.Node.VoiceNode
{
    /// <summary>
    /// 语音节点全局配置
    /// </summary>
    public class VoiceNodeOptions
    {
        /// <summary>默认转写服务商</summary>
        public string DefaultProvider { get; set; } = "FunASR";

        /// <summary>最大并发转写段数</summary>
        public int MaxConcurrentSegments { get; set; } = 4;

        /// <summary>音频文件最大大小（MB）</summary>
        public int MaxFileSizeMb { get; set; } = 500;

        /// <summary>单次任务最大处理时间（分钟）</summary>
        public int MaxProcessingTimeMinutes { get; set; } = 15;

        /// <summary>长音频自动分段阈值（秒）</summary>
        public int AutoSegmentThresholdSeconds { get; set; } = 300;

        /// <summary>支持的音频/视频格式</summary>
        public string[] SupportedFormats { get; set; } = {
            ".wav", ".mp3", ".pcm", ".m4a", ".ogg", ".flac", ".aac", ".wma",
            ".mp4", ".avi", ".mkv", ".mov"
        };

        /// <summary>LLM 后处理默认提示词</summary>
        public string DefaultSystemPrompt { get; set; } = "请对以下语音转写文本进行整理，修正标点符号和明显错误，并生成简要摘要。";

        /// <summary>临时文件目录（空则使用系统临时目录）</summary>
        public string TempFileDirectory { get; set; } = "";

        /// <summary>FFmpeg 可执行文件路径（空则在 PATH 中查找）</summary>
        public string FFmpegPath { get; set; } = "";

        /// <summary>是否保留原始响应 JSON</summary>
        public bool KeepRawResponse { get; set; } = false;

        /// <summary>熔断器连续失败阈值</summary>
        public int CircuitBreakerThreshold { get; set; } = 3;

        /// <summary>熔断器恢复时间（秒）</summary>
        public int CircuitBreakerRecoverySeconds { get; set; } = 60;
    }
}
