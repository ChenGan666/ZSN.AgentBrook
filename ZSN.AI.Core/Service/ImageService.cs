using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Models.Image;
using ZSN.AI.Core.Service.Providers;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using Newtonsoft.Json;

namespace ZSN.AI.Core.Service
{
    /// <summary>
    /// 图片生成服务 - 对外统一接口
    /// </summary>
    [ServiceDescription(typeof(IImageService), ServiceLifetime.Scoped)]
    public class ImageService : IImageService
    {
        private const int LLMLogMarkId = 311;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOperationLogService _logService;

        public ImageService(IServiceProvider serviceProvider, IOperationLogService logService)
        {
            _serviceProvider = serviceProvider;
            _logService = logService;
        }

        /// <summary>
        /// 生成图片
        /// </summary>
        public async Task<string> GenerateImageAsync(LargeModelInfo modelInfo, ImageGenerationRequest request)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                Console.WriteLine($"[图片生成] 开始生成图片");
                Console.WriteLine($"[图片生成] 模型: {modelInfo.ModelName}");
                Console.WriteLine($"[图片生成] 生成类型: {request.GenerationType}");
                Console.WriteLine($"[图片生成] 提示词: {request.Prompt}");

                var provider = GetProvider(modelInfo);
                var imageUrl = await provider.GenerateImageAsync(modelInfo, request);

                LogImageCall(modelInfo, request, imageUrl, sw.ElapsedMilliseconds, "success");
                Console.WriteLine($"[图片生成] 图片生成成功");
                Console.WriteLine($"[图片生成] 图片URL: {imageUrl}");

                return imageUrl;
            }
            catch (Exception ex)
            {
                LogImageCall(modelInfo, request, null, sw.ElapsedMilliseconds, "error", ex.Message);
                Console.WriteLine($"[图片生成] 生成图片失败: {ex.Message}");
                Console.WriteLine($"[图片生成] 异常堆栈: {ex.StackTrace}");
                throw new Exception($"图片生成失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 生成图片(简化版本 - 仅文生图)
        /// </summary>
        public async Task<string> GenerateImageAsync(
            LargeModelInfo modelInfo,
            string prompt,
            int width = 1024,
            int height = 1024,
            string quality = "standard",
            string style = "vivid")
        {
            var request = new ImageGenerationRequest
            {
                GenerationType = ImageGenerationType.TextToImage,
                Prompt = prompt,
                Width = width,
                Height = height,
                Quality = quality,
                Style = style
            };

            return await GenerateImageAsync(modelInfo, request);
        }

        /// <summary>
        /// 根据模型信息获取对应的提供商
        /// </summary>
        private IImageProvider GetProvider(LargeModelInfo modelInfo)
        {
            Console.WriteLine($"[图片生成] 使用 OpenAI 兼容提供商");
            return new OpenAICompatibleImageProvider();
        }

        private void LogImageCall(LargeModelInfo modelInfo, ImageGenerationRequest request, string imageUrl, long durationMs, string status, string error = null)
        {
            try
            {
                var logDetail = JsonConvert.SerializeObject(new
                {
                    serviceName = "ImageService",
                    methodName = "GenerateImageAsync",
                    model = new
                    {
                        modelId = modelInfo.LargeModelID,
                        modelName = modelInfo.ModelName,
                        typeName = modelInfo.TypeName,
                        organization = modelInfo.ModelOrganizationName
                    },
                    parameters = new
                    {
                        generationType = request.GenerationType.ToString(),
                        prompt = request.Prompt,
                        width = request.Width,
                        height = request.Height,
                        quality = request.Quality,
                        style = request.Style
                    },
                    output = new { imageUrl, success = status == "success" },
                    timing = new { durationMs },
                    status,
                    error
                }, Formatting.None);
                _logService.AddOperationLog(LLMLogMarkId, logDetail);
            }
            catch { }
        }
    }
}
