using System;
using System.Threading.Tasks;
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Models.Video;
using ZSN.AI.Core.Service.Providers;
using ZSN.AI.Entity;

namespace ZSN.AI.Core.Service
{
    /// <summary>
    /// 视频生成服务 - 对外统一接口
    /// </summary>
    [ServiceDescription(typeof(IVideoService), ServiceLifetime.Scoped)]
    public class VideoService : IVideoService
    {
        private readonly IServiceProvider _serviceProvider;

        public VideoService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 提交视频生成任务
        /// </summary>
        public async Task<VideoGenerationResponse> SubmitVideoTaskAsync(LargeModelInfo modelInfo, VideoGenerationRequest request)
        {
            try
            {
                Console.WriteLine($"[视频生成] 开始提交视频生成任务");
                Console.WriteLine($"[视频生成] 模型: {modelInfo.ModelName}");
                Console.WriteLine($"[视频生成] 生成类型: {request.GenerationType}");
                Console.WriteLine($"[视频生成] 提示词: {request.Prompt}");

                // 根据模型信息选择对应的提供商
                var provider = GetProvider(modelInfo);
                
                // 调用提供商的提交任务方法
                var response = await provider.SubmitVideoTaskAsync(modelInfo, request);

                Console.WriteLine($"[视频生成] 任务提交成功, 任务ID: {response.TaskId}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[视频生成] 提交任务失败: {ex.Message}");
                Console.WriteLine($"[视频生成] 异常堆栈: {ex.StackTrace}");
                throw new Exception($"视频生成任务提交失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 查询视频生成任务状态
        /// </summary>
        public async Task<VideoGenerationResponse> QueryVideoTaskStatusAsync(LargeModelInfo modelInfo, string taskId)
        {
            try
            {
                Console.WriteLine($"[视频生成] 查询任务状态, 任务ID: {taskId}");

                // 根据模型信息选择对应的提供商
                var provider = GetProvider(modelInfo);
                
                // 调用提供商的查询状态方法
                var response = await provider.QueryVideoTaskStatusAsync(modelInfo, taskId);

                Console.WriteLine($"[视频生成] 任务状态: {response.TaskStatus}");
                if (response.TaskStatus == VideoTaskStatus.Success)
                {
                    Console.WriteLine($"[视频生成] 视频URL: {string.Join(", ", response.VideoUrls ?? new System.Collections.Generic.List<string>())}");
                }
                else if (response.TaskStatus == VideoTaskStatus.Failure)
                {
                    Console.WriteLine($"[视频生成] 失败原因: {response.ErrorMessage}");
                }

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[视频生成] 查询任务状态失败: {ex.Message}");
                Console.WriteLine($"[视频生成] 异常堆栈: {ex.StackTrace}");
                throw new Exception($"查询视频生成任务状态失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 生成视频(提交任务并等待完成)
        /// </summary>
        public async Task<VideoGenerationResponse> GenerateVideoAsync(
            LargeModelInfo modelInfo, 
            VideoGenerationRequest request, 
            int maxWaitSeconds = 300, 
            int pollIntervalSeconds = 5)
        {
            try
            {
                Console.WriteLine($"[视频生成] 开始生成视频(同步模式)");
                Console.WriteLine($"[视频生成] 最大等待时间: {maxWaitSeconds}秒, 轮询间隔: {pollIntervalSeconds}秒");

                // 1. 提交任务
                var submitResponse = await SubmitVideoTaskAsync(modelInfo, request);
                var taskId = submitResponse.TaskId;

                // 2. 轮询查询任务状态
                var startTime = DateTime.UtcNow;
                var maxWaitTime = TimeSpan.FromSeconds(maxWaitSeconds);

                while (true)
                {
                    // 检查是否超时
                    if (DateTime.UtcNow - startTime > maxWaitTime)
                    {
                        throw new TimeoutException($"视频生成超时,已等待{maxWaitSeconds}秒");
                    }

                    // 查询任务状态
                    var statusResponse = await QueryVideoTaskStatusAsync(modelInfo, taskId);

                    // 任务完成(成功或失败)
                    if (statusResponse.TaskStatus == VideoTaskStatus.Success)
                    {
                        Console.WriteLine($"[视频生成] 视频生成成功");
                        return statusResponse;
                    }
                    else if (statusResponse.TaskStatus == VideoTaskStatus.Failure)
                    {
                        throw new Exception($"视频生成失败: {statusResponse.ErrorMessage}");
                    }

                    // 任务进行中,等待后继续轮询
                    Console.WriteLine($"[视频生成] 任务状态: {statusResponse.TaskStatus}, {pollIntervalSeconds}秒后重试...");
                    await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[视频生成] 生成视频失败: {ex.Message}");
                Console.WriteLine($"[视频生成] 异常堆栈: {ex.StackTrace}");
                throw new Exception($"视频生成失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据模型信息获取对应的提供商
        /// </summary>
        private IVideoProvider GetProvider(LargeModelInfo modelInfo)
        {
            // 目前只实现了Compshare提供商
            // 后续可以根据modelInfo的不同属性(如EndPoint、ModelOrganizationID等)选择不同的提供商

            if (modelInfo.ModelOrganizationID == Entity.Model.Enum.AIType.Compshare) {
                return new CompshareVideoProvider();
            }

            // 默认使用Compshare提供商
            // 后续可以扩展其他提供商,如:
            // - OpenAI官方提供商
            // - 其他第三方提供商
            Console.WriteLine($"[视频生成] 使用默认提供商: Compshare");
            return new CompshareVideoProvider();
        }
    }
}
