using System.Threading.Tasks;
using ZSN.AI.Core.Models.Image;
using ZSN.AI.Entity;

namespace ZSN.AI.Core.Service.Providers
{
    /// <summary>
    /// 图片生成提供商接口
    /// </summary>
    public interface IImageProvider
    {
        /// <summary>
        /// 生成图片
        /// </summary>
        Task<string> GenerateImageAsync(LargeModelInfo modelInfo, ImageGenerationRequest request);
    }
}
