using System.Collections.Generic;

namespace ZSN.AI.Core.Models.Image
{
    /// <summary>
    /// 图片生成请求基类 - 通用参数
    /// </summary>
    public class ImageGenerationRequest
    {
        /// <summary>
        /// 图片生成类型
        /// </summary>
        public ImageGenerationType GenerationType { get; set; }

        /// <summary>
        /// 提示词描述
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// 输入图片URL或Base64 (用于图生图)
        /// </summary>
        public string? ImageInput { get; set; }

        /// <summary>
        /// 图片宽度
        /// </summary>
        public int Width { get; set; } = 1024;

        /// <summary>
        /// 图片高度
        /// </summary>
        public int Height { get; set; } = 1024;

        /// <summary>
        /// 图片质量 (如: "standard", "hd")
        /// </summary>
        public string Quality { get; set; } = "standard";

        /// <summary>
        /// 图片风格 (如: "vivid", "natural")
        /// </summary>
        public string Style { get; set; } = "vivid";

        /// <summary>
        /// 生成图片数量
        /// </summary>
        public int N { get; set; } = 1;

        /// <summary>
        /// 扩展参数字典 - 用于不同模型的特殊参数
        /// </summary>
        public Dictionary<string, object>? ExtendedParameters { get; set; }
    }
}
