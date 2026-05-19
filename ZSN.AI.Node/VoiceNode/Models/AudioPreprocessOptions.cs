namespace ZSN.AI.Node.VoiceNode.Models
{
    /// <summary>
    /// 音频预处理选项
    /// </summary>
    public class AudioPreprocessOptions
    {
        /// <summary>目标采样率（Hz）</summary>
        public int TargetSampleRate { get; set; } = 16000;

        /// <summary>目标位深</summary>
        public int TargetBitDepth { get; set; } = 16;

        /// <summary>目标声道数</summary>
        public int TargetChannels { get; set; } = 1;

        /// <summary>最大文件大小（MB）</summary>
        public int MaxFileSizeMb { get; set; } = 500;

        /// <summary>最大音频时长（秒），0=不限</summary>
        public int MaxDurationSeconds { get; set; } = 0;

        /// <summary>长音频自动分段阈值（秒）</summary>
        public int AutoSegmentThresholdSeconds { get; set; } = 300;
    }
}
