namespace ZSN.AI.Core.Models.Image.Parameters
{
    /// <summary>
    /// OpenAI DALL-E 模型参数
    /// 支持模型: dall-e-2, dall-e-3
    /// </summary>
    public class OpenAIImageParameters : ImageGenerationRequest
    {
        /// <summary>
        /// 图片尺寸
        /// DALL-E 2: 256x256, 512x512, 1024x1024
        /// DALL-E 3: 1024x1024, 1792x1024, 1024x1792
        /// </summary>
        public string Size { get; set; } = "1024x1024";

        /// <summary>
        /// 图片质量 (仅DALL-E 3支持)
        /// 可选值: "standard", "hd"
        /// </summary>
        public new string Quality { get; set; } = "standard";

        /// <summary>
        /// 图片风格 (仅DALL-E 3支持)
        /// 可选值: "vivid", "natural"
        /// </summary>
        public new string Style { get; set; } = "vivid";

        /// <summary>
        /// 生成图片数量
        /// DALL-E 2: 1-10
        /// DALL-E 3: 仅支持1
        /// </summary>
        public new int N { get; set; } = 1;

        /// <summary>
        /// 获取对应的模型名称
        /// </summary>
        public static string GetModelName(int version = 3)
        {
            return version == 2 ? "dall-e-2" : "dall-e-3";
        }
    }
}
