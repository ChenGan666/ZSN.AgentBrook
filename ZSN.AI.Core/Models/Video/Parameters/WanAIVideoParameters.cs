namespace ZSN.AI.Core.Models.Video.Parameters
{
    /// <summary>
    /// Wan-AI 模型参数
    /// 支持模型: Wan-AI/Wan2.2-T2V, Wan-AI/Wan2.2-I2V, Wan-AI/Wan2.5-T2V, Wan-AI/Wan2.5-I2V, 
    ///          Wan-AI/Wan2.6-T2V, Wan-AI/Wan2.6-I2V
    /// </summary>
    public class WanAIVideoParameters : VideoGenerationRequest
    {
        /// <summary>
        /// 分辨率
        /// Wan2.2/2.5: "720P" (1280x720或720x1280), "480P" (832x480或480x832)
        /// Wan2.6: "720P", "1080P"
        /// 默认: "720P"
        /// </summary>
        public new string Resolution { get; set; } = "720P";

        /// <summary>
        /// 随机种子
        /// 范围: [0, 2147483647] (Wan2.2/2.5)
        /// 范围: [0, 4294967295] (Wan2.6)
        /// </summary>
        public new int? Seed { get; set; }

        /// <summary>
        /// 负面提示词
        /// </summary>
        public new string? NegativePrompt { get; set; }

        /// <summary>
        /// 视频时长（仅Wan2.6支持）
        /// 支持: 5, 10, 15 秒
        /// 默认: 5
        /// </summary>
        public new int? Duration { get; set; }

        /// <summary>
        /// 是否启用提示词扩展（仅Wan2.6支持）
        /// 默认: true
        /// </summary>
        public bool? PromptExtend { get; set; }

        /// <summary>
        /// 镜头类型（仅Wan2.6支持）
        /// 支持: "single" (单镜头), "multi" (多镜头)
        /// 默认: "single"
        /// </summary>
        public string? ShotType { get; set; }

        /// <summary>
        /// 获取对应的模型名称
        /// </summary>
        /// <param name="version">版本号: "2.2", "2.5", "2.6"</param>
        /// <param name="generationType">生成类型</param>
        /// <returns>模型名称</returns>
        public static string GetModelName(string version, VideoGenerationType generationType)
        {
            var suffix = generationType == VideoGenerationType.TextToVideo ? "T2V" : "I2V";
            return $"Wan-AI/Wan{version}-{suffix}";
        }
    }
}
