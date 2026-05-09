using System.Threading.Tasks;
using ZSN.AI.Core.Models.Image;
using ZSN.AI.Entity;

namespace ZSN.AI.Core.Interface
{
    /// <summary>
    /// 图片生成服务接口
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// 生成图片
        /// </summary>
        /// <param name="modelInfo">模型信息</param>
        /// <param name="request">图片生成请求</param>
        /// <returns>图片URL</returns>
        Task<string> GenerateImageAsync(LargeModelInfo modelInfo, ImageGenerationRequest request);

        /// <summary>
        /// 生成图片(简化版本 - 仅文生图)
        /// </summary>
        /// <param name="modelInfo">模型信息</param>
        /// <param name="prompt">提示词</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="quality">质量</param>
        /// <param name="style">风格</param>
        /// <returns>图片URL</returns>
        Task<string> GenerateImageAsync(
            LargeModelInfo modelInfo, 
            string prompt, 
            int width = 1024, 
            int height = 1024, 
            string quality = "standard", 
            string style = "vivid");
    }
}
