using System.Collections.Generic;

namespace ZSN.AI.Core.Models.Video
{
    /// <summary>
    /// 视频生成请求基类 - 通用参数
    /// </summary>
    public class VideoGenerationRequest
    {
        /// <summary>
        /// 视频生成类型
        /// </summary>
        public VideoGenerationType GenerationType { get; set; }

        /// <summary>
        /// 提示词描述
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// 负面提示词(可选)
        /// </summary>
        public string? NegativePrompt { get; set; }

        /// <summary>
        /// 输入图片URL或Base64 (用于图生视频)
        /// </summary>
        public string? ImageInput { get; set; }

        /// <summary>
        /// 参考图片列表 (用于参考图生成视频)
        /// </summary>
        public List<string>? ReferenceImages { get; set; }

        /// <summary>
        /// 首帧图片URL或Base64 (用于首尾帧生成视频)
        /// </summary>
        public string? FirstFrameUrl { get; set; }

        /// <summary>
        /// 尾帧图片URL或Base64 (用于首尾帧生成视频)
        /// </summary>
        public string? LastFrameUrl { get; set; }

        /// <summary>
        /// 视频时长(秒) - 不同模型支持不同时长
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// 视频分辨率/尺寸 (如: "720x1280", "1080P", "16:9")
        /// </summary>
        public string? Size { get; set; }

        /// <summary>
        /// 宽高比 (如: "16:9", "9:16", "1:1")
        /// </summary>
        public string? AspectRatio { get; set; }

        /// <summary>
        /// 分辨率 (如: "720p", "1080p")
        /// </summary>
        public string? Resolution { get; set; }

        /// <summary>
        /// 随机种子 (用于可重现的生成)
        /// </summary>
        public int? Seed { get; set; }

        /// <summary>
        /// 扩展参数字典 - 用于不同模型的特殊参数
        /// </summary>
        public Dictionary<string, object>? ExtendedParameters { get; set; }
    }
}
