using System.Threading.Tasks;
using ZSN.AI.Core.Models.Video;
using ZSN.AI.Entity;

namespace ZSN.AI.Core.Interface
{
    /// <summary>
    /// 视频生成服务接口
    /// </summary>
    public interface IVideoService
    {
        /// <summary>
        /// 提交视频生成任务
        /// </summary>
        /// <param name="modelInfo">模型信息</param>
        /// <param name="request">视频生成请求</param>
        /// <returns>视频生成响应(包含任务ID)</returns>
        Task<VideoGenerationResponse> SubmitVideoTaskAsync(LargeModelInfo modelInfo, VideoGenerationRequest request);

        /// <summary>
        /// 查询视频生成任务状态
        /// </summary>
        /// <param name="modelInfo">模型信息</param>
        /// <param name="taskId">任务ID</param>
        /// <returns>视频生成响应(包含任务状态和结果)</returns>
        Task<VideoGenerationResponse> QueryVideoTaskStatusAsync(LargeModelInfo modelInfo, string taskId);

        /// <summary>
        /// 生成视频(提交任务并等待完成)
        /// </summary>
        /// <param name="modelInfo">模型信息</param>
        /// <param name="request">视频生成请求</param>
        /// <param name="maxWaitSeconds">最大等待时间(秒),默认300秒</param>
        /// <param name="pollIntervalSeconds">轮询间隔(秒),默认5秒</param>
        /// <returns>视频生成响应(包含视频URL)</returns>
        Task<VideoGenerationResponse> GenerateVideoAsync(
            LargeModelInfo modelInfo, 
            VideoGenerationRequest request, 
            int maxWaitSeconds = 300, 
            int pollIntervalSeconds = 5);
    }
}
