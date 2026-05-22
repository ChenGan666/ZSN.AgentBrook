using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZSN.AI.Core.Models.Video;
using ZSN.AI.Core.Models.Video.Parameters;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;

namespace ZSN.AI.Core.Service.Providers
{
    /// <summary>
    /// Compshare (优云智算) 视频生成提供商
    /// API文档: https://www.compshare.cn/docs/modelverse/models/video_api/
    /// </summary>
    public class CompshareVideoProvider : IVideoProvider
    {
        private const string SUBMIT_ENDPOINT = "/tasks/submit";
        private const string STATUS_ENDPOINT = "/tasks/status";

        /// <summary>
        /// 提交视频生成任务
        /// </summary>
        public async Task<VideoGenerationResponse> SubmitVideoTaskAsync(LargeModelInfo modelInfo, VideoGenerationRequest request)
        {
            try
            {
                var httpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(modelInfo.EndPoint);
                
                // 构建API URL
                var endpoint = modelInfo.EndPoint?.TrimEnd('/') ?? throw new ArgumentException("模型端点不能为空");
                var apiUrl = $"{endpoint}{SUBMIT_ENDPOINT}";

                // 构建请求体
                var requestBody = BuildSubmitRequestBody(modelInfo.ModelName, request);
                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 设置Authorization header
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", modelInfo.ModelKey);

                Console.WriteLine($"[Compshare视频] 提交任务到: {apiUrl}");
                Console.WriteLine($"[Compshare视频] 模型: {modelInfo.ModelName}");
                //Console.WriteLine($"[Compshare视频] 请求体: {jsonContent}");

                // 调用API
                var response = await httpClient.PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Compshare视频] API错误响应: {errorContent}");
                    throw new Exception($"API调用失败: {response.StatusCode}, {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Compshare视频] API响应: {responseContent}");

                // 解析响应
                return ParseSubmitResponse(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Compshare视频] 提交任务失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 查询视频生成任务状态
        /// </summary>
        public async Task<VideoGenerationResponse> QueryVideoTaskStatusAsync(LargeModelInfo modelInfo, string taskId)
        {
            try
            {
                var httpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(modelInfo.EndPoint);
                
                // 构建API URL
                var endpoint = modelInfo.EndPoint?.TrimEnd('/') ?? throw new ArgumentException("模型端点不能为空");
                var apiUrl = $"{endpoint}{STATUS_ENDPOINT}?task_id={taskId}";

                // 设置Authorization header
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", modelInfo.ModelKey);

                Console.WriteLine($"[Compshare视频] 查询任务状态: {apiUrl}");

                // 调用API
                var response = await httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Compshare视频] API错误响应: {errorContent}");
                    throw new Exception($"API调用失败: {response.StatusCode}, {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Compshare视频] API响应: {responseContent}");

                // 解析响应
                return ParseStatusResponse(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Compshare视频] 查询任务状态失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 构建提交任务的请求体
        /// </summary>
        private Dictionary<string, object> BuildSubmitRequestBody(string modelName, VideoGenerationRequest request)
        {
            var requestBody = new Dictionary<string, object>
            {
                ["model"] = modelName,
                ["input"] = BuildInputObject(modelName, request),
                ["parameters"] = BuildParametersObject(modelName, request)
            };

            return requestBody;
        }

        /// <summary>
        /// 构建input对象
        /// </summary>
        private Dictionary<string, object> BuildInputObject(string modelName, VideoGenerationRequest request)
        {
            var input = new Dictionary<string, object>
            {
                ["prompt"] = request.Prompt
            };

            // 负面提示词
            if (!string.IsNullOrEmpty(request.NegativePrompt))
            {
                input["negative_prompt"] = request.NegativePrompt;
            }

            // 图生视频 - 首帧图片
            if (request.GenerationType == VideoGenerationType.ImageToVideo && !string.IsNullOrEmpty(request.ImageInput))
            {
                // Wan2.6-I2V使用img_url，其他模型使用first_frame_url
                var imageFieldName = modelName.Contains("Wan2.6-I2V", StringComparison.OrdinalIgnoreCase) 
                    ? "img_url" 
                    : "first_frame_url";
                input[imageFieldName] = ConvertToDataUriIfNeeded(request.ImageInput);
            }

            // 参考图生成视频 - 参考图片列表
            if (request.GenerationType == VideoGenerationType.ReferenceToVideo && request.ReferenceImages != null && request.ReferenceImages.Count > 0)
            {
                // 转换所有参考图为Data URI格式
                var convertedImages = request.ReferenceImages.Select(img => ConvertToDataUriIfNeeded(img)).ToList();
                input["images"] = convertedImages;
            }

            // 首尾帧生成视频
            if (request.GenerationType == VideoGenerationType.StartEndToVideo)
            {
                if (!string.IsNullOrEmpty(request.FirstFrameUrl))
                {
                    input["first_frame_url"] = ConvertToDataUriIfNeeded(request.FirstFrameUrl);
                }
                if (!string.IsNullOrEmpty(request.LastFrameUrl))
                {
                    input["last_frame_url"] = ConvertToDataUriIfNeeded(request.LastFrameUrl);
                }
            }

            return input;
        }

        /// <summary>
        /// 构建parameters对象
        /// </summary>
        private Dictionary<string, object> BuildParametersObject(string modelName, VideoGenerationRequest request)
        {
            var parameters = new Dictionary<string, object>();

            // 根据不同模型构建参数
            if (modelName.Contains("sora", StringComparison.OrdinalIgnoreCase))
            {
                // Sora模型参数
                if (request is SoraVideoParameters soraParams)
                {
                    if (!string.IsNullOrEmpty(soraParams.Size))
                        parameters["size"] = soraParams.Size;
                    if (soraParams.Duration > 0)
                        parameters["duration"] = soraParams.Duration;
                }
                else
                {
                    // 使用默认值
                    if (!string.IsNullOrEmpty(request.Size))
                        parameters["size"] = request.Size;
                    if (request.Duration.HasValue)
                        parameters["duration"] = request.Duration.Value;
                }
            }
            else if (modelName.Contains("vidu", StringComparison.OrdinalIgnoreCase))
            {
                // Vidu模型参数
                if (request is ViduVideoParameters viduParams)
                {
                    if (!string.IsNullOrEmpty(viduParams.ViduType))
                        parameters["vidu_type"] = viduParams.ViduType;
                    if (viduParams.Duration > 0)
                        parameters["duration"] = viduParams.Duration;
                    
                    // text2video和reference2video使用aspect_ratio
                    if (viduParams.ViduType == "text2video" || viduParams.ViduType == "reference2video")
                    {
                        if (!string.IsNullOrEmpty(viduParams.AspectRatio))
                            parameters["aspect_ratio"] = viduParams.AspectRatio;
                        if (!string.IsNullOrEmpty(viduParams.Resolution))
                            parameters["resolution"] = viduParams.Resolution.ToLower(); // 720p格式
                    }
                    // img2video和start-end2video使用resolution
                    else
                    {
                        if (!string.IsNullOrEmpty(viduParams.Resolution))
                            parameters["resolution"] = viduParams.Resolution;
                    }
                    
                    if (!string.IsNullOrEmpty(viduParams.MovementAmplitude))
                        parameters["movement_amplitude"] = viduParams.MovementAmplitude;
                    
                    parameters["bgm"] = viduParams.Bgm;
                }
                else
                {
                    // 自动设置vidu_type
                    var viduType = request.GenerationType switch
                    {
                        VideoGenerationType.TextToVideo => "text2video",
                        VideoGenerationType.ImageToVideo => "img2video",
                        VideoGenerationType.ReferenceToVideo => "reference2video",
                        VideoGenerationType.StartEndToVideo => "start-end2video",
                        _ => "text2video"
                    };
                    parameters["vidu_type"] = viduType;
                    
                    if (request.Duration.HasValue)
                        parameters["duration"] = request.Duration.Value;
                    if (!string.IsNullOrEmpty(request.AspectRatio))
                        parameters["aspect_ratio"] = request.AspectRatio;
                    if (!string.IsNullOrEmpty(request.Resolution))
                        parameters["resolution"] = request.Resolution;
                }
            }
            else if (modelName.Contains("Wan-AI", StringComparison.OrdinalIgnoreCase) || modelName.Contains("Wan", StringComparison.OrdinalIgnoreCase))
            {
                // WanAI模型参数
                if (request is WanAIVideoParameters wanParams)
                {
                    if (!string.IsNullOrEmpty(wanParams.Resolution))
                        parameters["resolution"] = wanParams.Resolution;
                    if (wanParams.Seed.HasValue)
                        parameters["seed"] = wanParams.Seed.Value;
                    
                    // Wan2.6特殊参数
                    if (modelName.Contains("Wan2.6", StringComparison.OrdinalIgnoreCase))
                    {
                        if (wanParams.Duration.HasValue && wanParams.Duration.Value > 0)
                            parameters["duration"] = wanParams.Duration.Value;
                        
                        // 默认启用提示词扩展
                        parameters["prompt_extend"] = wanParams.PromptExtend ?? true;
                        
                        // 默认使用单镜头
                        parameters["shot_type"] = wanParams.ShotType ?? "single";
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(request.Resolution))
                        parameters["resolution"] = request.Resolution;
                    if (request.Seed.HasValue)
                        parameters["seed"] = request.Seed.Value;
                    
                    // Wan2.6特殊参数（使用默认值）
                    if (modelName.Contains("Wan2.6", StringComparison.OrdinalIgnoreCase))
                    {
                        if (request.Duration.HasValue && request.Duration.Value > 0)
                            parameters["duration"] = request.Duration.Value;
                        else
                            parameters["duration"] = 5; // 默认5秒
                        
                        parameters["prompt_extend"] = true;
                        parameters["shot_type"] = "single";
                    }
                }
            }
            else if (modelName.Contains("Hailuo", StringComparison.OrdinalIgnoreCase) || modelName.Contains("MiniMax", StringComparison.OrdinalIgnoreCase))
            {
                // Hailuo模型参数
                if (request is HailuoVideoParameters hailuoParams)
                {
                    if (hailuoParams.Duration > 0)
                        parameters["duration"] = hailuoParams.Duration;
                    if (!string.IsNullOrEmpty(hailuoParams.Resolution))
                        parameters["resolution"] = hailuoParams.Resolution;
                    parameters["prompt_optimizer"] = hailuoParams.PromptOptimizer;
                    parameters["fast_pretreatment"] = hailuoParams.FastPretreatment;
                    parameters["aigc_watermark"] = hailuoParams.AigcWatermark;
                }
                else
                {
                    if (request.Duration.HasValue)
                        parameters["duration"] = request.Duration.Value;
                    if (!string.IsNullOrEmpty(request.Resolution))
                        parameters["resolution"] = request.Resolution;
                }
            }
            else if (modelName.Contains("doubao", StringComparison.OrdinalIgnoreCase))
            {
                // Doubao模型参数
                if (request is DoubaoVideoParameters doubaoParams)
                {
                    if (doubaoParams.Duration.HasValue)
                        parameters["duration"] = doubaoParams.Duration.Value;
                    if (!string.IsNullOrEmpty(doubaoParams.Resolution))
                        parameters["resolution"] = doubaoParams.Resolution;
                }
                else
                {
                    if (request.Duration.HasValue)
                        parameters["duration"] = request.Duration.Value;
                    if (!string.IsNullOrEmpty(request.Resolution))
                        parameters["resolution"] = request.Resolution;
                }
            }

            // 添加扩展参数
            if (request.ExtendedParameters != null)
            {
                foreach (var kvp in request.ExtendedParameters)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }

            return parameters;
        }

        /// <summary>
        /// 解析提交任务的响应
        /// </summary>
        private VideoGenerationResponse ParseSubmitResponse(string responseContent)
        {
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            var response = new VideoGenerationResponse
            {
                TaskStatus = VideoTaskStatus.Pending
            };

            if (jsonResponse.TryGetProperty("output", out var output))
            {
                if (output.TryGetProperty("task_id", out var taskId))
                {
                    response.TaskId = taskId.GetString();
                }
            }

            if (jsonResponse.TryGetProperty("request_id", out var requestId))
            {
                response.RequestId = requestId.GetString();
            }

            return response;
        }

        /// <summary>
        /// 解析查询状态的响应
        /// </summary>
        private VideoGenerationResponse ParseStatusResponse(string responseContent)
        {
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            var response = new VideoGenerationResponse();

            if (jsonResponse.TryGetProperty("output", out var output))
            {
                if (output.TryGetProperty("task_id", out var taskId))
                {
                    response.TaskId = taskId.GetString();
                }

                if (output.TryGetProperty("task_status", out var taskStatus))
                {
                    var statusStr = taskStatus.GetString();
                    response.TaskStatus = statusStr switch
                    {
                        "Pending" => VideoTaskStatus.Pending,
                        "Running" => VideoTaskStatus.Running,
                        "Success" => VideoTaskStatus.Success,
                        "Failure" => VideoTaskStatus.Failure,
                        _ => VideoTaskStatus.Pending
                    };
                }

                if (output.TryGetProperty("urls", out var urls) && urls.ValueKind == JsonValueKind.Array)
                {
                    response.VideoUrls = new List<string>();
                    foreach (var url in urls.EnumerateArray())
                    {
                        response.VideoUrls.Add(url.GetString());
                    }
                }

                if (output.TryGetProperty("submit_time", out var submitTime))
                {
                    response.SubmitTime = submitTime.GetInt64();
                }

                if (output.TryGetProperty("finish_time", out var finishTime))
                {
                    response.FinishTime = finishTime.GetInt64();
                }

                if (output.TryGetProperty("error_message", out var errorMessage))
                {
                    response.ErrorMessage = errorMessage.GetString();
                }
                
                // 如果任务失败但没有错误信息，尝试从整个响应中提取更多信息
                if (response.TaskStatus == VideoTaskStatus.Failure && string.IsNullOrEmpty(response.ErrorMessage))
                {
                    // 记录完整的响应以便调试
                    Console.WriteLine($"[Compshare视频] 任务失败但未返回错误信息，完整响应: {responseContent}");
                    response.ErrorMessage = "任务失败，但API未返回具体错误信息。请检查：1)图片URL是否可访问 2)图片格式是否支持 3)模型参数是否正确";
                }
            }

            if (jsonResponse.TryGetProperty("usage", out var usage))
            {
                response.Usage = new VideoUsageInfo();
                
                if (usage.TryGetProperty("duration", out var duration))
                {
                    response.Usage.Duration = duration.GetInt32();
                }
            }

            if (jsonResponse.TryGetProperty("request_id", out var requestId))
            {
                response.RequestId = requestId.GetString();
            }

            return response;
        }

        /// <summary>
        /// 将Base64字符串转换为Data URI格式（如果需要）
        /// </summary>
        private string ConvertToDataUriIfNeeded(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 如果已经是HTTP URL，直接返回
            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }

            // 如果已经是Data URI格式，直接返回
            if (input.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }

            // 否则认为是纯Base64字符串，转换为Data URI格式
            // 默认使用PNG格式，实际应该根据图片内容判断
            Console.WriteLine($"[Compshare视频] 将Base64字符串转换为Data URI格式");
            return $"data:image/png;base64,{input}";
        }
    }
}
