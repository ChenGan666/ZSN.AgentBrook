namespace ZSN.AI.Node.VoiceNode
{
    /// <summary>
    /// VoiceNode 节点配置数据（对应前端节点编辑器的配置）
    /// 继承 Entity 层的 VoiceNodeData 字段结构，运行时使用
    /// </summary>
    public class VoiceNodeData
    {
        // ─── 继承自 LargeModelData 的模型配置 ───

        /// <summary>LLM 模型配置（LargeModelInfo，含 LargeModelID/ModelName/EndPoint/ModelKey）</summary>
        public ZSN.AI.Entity.LargeModelInfo model { get; set; }

        /// <summary>温度参数</summary>
        public int temperature { get; set; } = 30;

        /// <summary>TopP 参数</summary>
        public int topp { get; set; } = 80;

        /// <summary>系统提示词 / prompt（用于 LLM 后处理，支持占位符如 {{transcription}}）</summary>
        public string prompt { get; set; }

        // ─── Voice 节点专属配置 ───

        /// <summary>音频来源（支持占位符变量，如 {{上游节点ID_fileUrl}}）</summary>
        public string AudioSource { get; set; }

        /// <summary>转写服务商：FunASR（空则使用默认）</summary>
        public string Provider { get; set; }

        /// <summary>是否启用 LLM 后处理</summary>
        public bool EnablePostProcessing { get; set; } = true;

        /// <summary>输出格式</summary>
        public VoiceOutputFormat OutputFormat { get; set; } = VoiceOutputFormat.PlainText;

        /// <summary>语言提示（zh/en/auto）</summary>
        public string Language { get; set; } = "auto";

        // ─── 说话人分离配置 ───

        /// <summary>是否启用说话人分离</summary>
        public bool EnableSpeakerDiarization { get; set; } = true;

        /// <summary>预期说话人数量（0=自动检测，>0=指定数量）</summary>
        public int ExpectedSpeakerCount { get; set; } = 0;

        /// <summary>说话人标签映射（可选，如 {"Speaker_0": "张经理", "Speaker_1": "李工"}）</summary>
        public Dictionary<string, string> SpeakerLabelMap { get; set; }

        // ─── 增强功能 ───

        /// <summary>是否启用情感识别</summary>
        public bool EnableEmotionDetection { get; set; }

        /// <summary>是否启用音频事件检测（掌声、笑声等）</summary>
        public bool EnableAudioEventDetection { get; set; }

        /// <summary>热词列表（提升特定词汇识别率）</summary>
        public Dictionary<string, int> Hotwords { get; set; }

        /// <summary>最大音频时长（秒），超时截断，0=不限制</summary>
        public int MaxAudioDurationSeconds { get; set; } = 0;
    }

    /// <summary>输出格式枚举</summary>
    public enum VoiceOutputFormat
    {
        /// <summary>纯文本（说话人分离时格式：[发言人A] 内容\n[发言人B] 内容）</summary>
        PlainText = 0,
        /// <summary>带时间戳的分段 JSON</summary>
        SegmentsJson = 1,
        /// <summary>SRT 字幕格式</summary>
        SRT = 2,
        /// <summary>WebVTT 字幕格式</summary>
        VTT = 3
    }
}
