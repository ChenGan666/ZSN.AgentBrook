namespace ZSN.AI.Core.Models.Image.Parameters
{
    /// <summary>
    /// QWen (通义千问) 图片生成模型参数
    /// 支持模型: Qwen/Qwen-Image (文生图), Qwen/Qwen-Image-Edit (图生图)
    /// </summary>
    public class QWenImageParameters : ImageGenerationRequest
    {
        /// <summary>
        /// 图片尺寸
        /// 格式: "widthxheight" (如: "1024x1024")
        /// </summary>
        public string Size { get; set; } = "1024x1024";

        /// <summary>
        /// 获取对应的模型名称
        /// </summary>
        /// <param name="generationType">生成类型</param>
        /// <returns>模型名称</returns>
        public static string GetModelName(ImageGenerationType generationType)
        {
            return generationType switch
            {
                ImageGenerationType.TextToImage => "Qwen/Qwen-Image",
                ImageGenerationType.ImageToImage => "Qwen/Qwen-Image-Edit",
                _ => "Qwen/Qwen-Image"
            };
        }
    }
}
