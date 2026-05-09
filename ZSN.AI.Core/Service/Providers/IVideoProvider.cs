using System.Threading.Tasks;
using ZSN.AI.Core.Models.Video;
using ZSN.AI.Entity;

namespace ZSN.AI.Core.Service.Providers
{
    /// <summary>
    /// 视频生成提供商接口
    /// </summary>
    public interface IVideoProvider
    {
        /// <summary>
        /// 提交视频生成任务
        /// </summary>
        Task<VideoGenerationResponse> SubmitVideoTaskAsync(LargeModelInfo modelInfo, VideoGenerationRequest request);

        /// <summary>
        /// 查询视频生成任务状态
        /// </summary>
        Task<VideoGenerationResponse> QueryVideoTaskStatusAsync(LargeModelInfo modelInfo, string taskId);
    }
}
