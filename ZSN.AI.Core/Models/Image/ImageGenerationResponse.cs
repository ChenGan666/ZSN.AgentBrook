using System.Collections.Generic;

namespace ZSN.AI.Core.Models.Image
{
    /// <summary>
    /// 图片生成响应模型
    /// </summary>
    public class ImageGenerationResponse
    {
        /// <summary>
        /// 生成的图片URL列表
        /// </summary>
        public List<string> ImageUrls { get; set; }

        /// <summary>
        /// 请求ID
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
