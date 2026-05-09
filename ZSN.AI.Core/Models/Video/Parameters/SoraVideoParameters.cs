namespace ZSN.AI.Core.Models.Video.Parameters
{
    /// <summary>
    /// OpenAI Sora 模型参数
    /// 支持模型: openai/sora-2/text-to-video, openai/sora-2/text-to-video-pro, 
    ///          openai/sora-2/image-to-video, openai/sora-2/image-to-video-pro
    /// </summary>
    public class SoraVideoParameters : VideoGenerationRequest
    {
        /// <summary>
        /// 视频尺寸
        /// 支持: "720x1280" (竖屏), "1280x720" (横屏)
        /// 默认: "720x1280"
        /// </summary>
        public new string Size { get; set; } = "720x1280";

        /// <summary>
        /// 视频时长(秒)
        /// T2V支持: 4, 8, 12 秒
        /// I2V支持: 4 秒
        /// 默认: 4
        /// </summary>
        public new int Duration { get; set; } = 4;

        /// <summary>
        /// 获取对应的模型名称
        /// </summary>
        /// <param name="generationType">生成类型</param>
        /// <param name="isPro">是否使用Pro版本</param>
        /// <returns>模型名称</returns>
        public static string GetModelName(VideoGenerationType generationType, bool isPro = false)
        {
            return generationType switch
            {
                VideoGenerationType.TextToVideo => isPro ? "openai/sora-2/text-to-video-pro" : "openai/sora-2/text-to-video",
                VideoGenerationType.ImageToVideo => isPro ? "openai/sora-2/image-to-video-pro" : "openai/sora-2/image-to-video",
                _ => "openai/sora-2/text-to-video"
            };
        }
    }
}
