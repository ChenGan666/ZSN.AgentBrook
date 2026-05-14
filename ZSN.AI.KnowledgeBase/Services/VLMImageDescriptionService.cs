using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model;
using ZSN.AI.KnowledgeBase.Interface;
using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Services
{
    public class VLMImageDescriptionService : IImageDescriptionService
    {
        private readonly IChatService _chatService;
        private readonly ILogger<VLMImageDescriptionService> _logger;

        public VLMImageDescriptionService(
            IChatService chatService,
            ILogger<VLMImageDescriptionService> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public async Task<ImageDescription> DescribeAsync(
            byte[] imageData, string? mimeType = null,
            string? context = null, int? visionModelId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await CallVlmAsync(imageData, mimeType, context, visionModelId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "VLM描述失败，启用降级策略");

                if (!string.IsNullOrEmpty(context))
                {
                    return new ImageDescription
                    {
                        Description = $"[自动生成] 此图片出现在以下上下文中：{context[..Math.Min(200, context.Length)]}",
                        Summary = "VLM描述失败，使用上下文文本",
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }

                return new ImageDescription
                {
                    Description = "[待描述] 图片描述生成失败，等待重试",
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<ImageDescription> CallVlmAsync(
            byte[] imageData, string? mimeType, string? context,
            int? visionModelId, CancellationToken ct)
        {
            if (imageData.Length > 20 * 1024 * 1024)
                throw new InvalidOperationException("图片超过20MB，跳过VLM调用");

            var modelId = visionModelId ?? 0;
            if (modelId <= 0)
                throw new InvalidOperationException("未配置视觉模型ID");

            var modelInfo = LargeModelInfoBussiness.GetModel(modelId);
            if (modelInfo == null)
                throw new InvalidOperationException($"视觉模型不存在: {modelId}");

            var prompt = @"请分析这张图片，以JSON格式返回：
{
  ""description"": ""图片的详细描述，包括类型、主要内容、关键信息"",
  ""summary"": ""一句话摘要"",
  ""tags"": [""标签1"", ""标签2""],
  ""contentType"": ""chart/photo/diagram/text/screenshot"",
  ""ocrText"": ""图片中所有可识别的文字（如果没有则留空）""
}";

            if (!string.IsNullOrEmpty(context))
                prompt += $"\n\n图片出现在以下文本上下文中：\n{context}";

            var history = new ChatHistory();
            var items = new ChatMessageContentItemCollection
            {
                new TextContent(prompt),
                new ImageContent(imageData, mimeType ?? "image/png")
            };
            history.AddUserMessage(items);

            var modelConfig = new LargeModelConfig
            {
                Id = modelId.ToString(),
                Model = modelInfo,
                Temperature = 0.2f,
                ResponseFormat = "json_object"
            };

            var responseBuilder = new StringBuilder();
            await foreach (var chunk in _chatService.SendChatAsync(modelConfig, history, responseFormat: "json_object"))
            {
                responseBuilder.Append(chunk);
            }

            var responseText = responseBuilder.ToString().Trim();
            return ParseJsonResponse(responseText);
        }

        private ImageDescription ParseJsonResponse(string response)
        {
            try
            {
                var json = response;
                if (json.Contains("```"))
                {
                    var start = json.IndexOf('{');
                    var end = json.LastIndexOf('}');
                    if (start >= 0 && end > start)
                        json = json[start..(end + 1)];
                }

                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new ImageDescription
                {
                    Description = root.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    Summary = root.TryGetProperty("summary", out var sum) ? sum.GetString() : null,
                    Tags = root.TryGetProperty("tags", out var tags)
                        ? tags.EnumerateArray().Select(t => t.GetString() ?? "").ToList()
                        : new List<string>(),
                    ContentType = root.TryGetProperty("contentType", out var ct) ? ct.GetString() : null,
                    OcrText = root.TryGetProperty("ocrText", out var ocr) ? ocr.GetString() : null,
                    Success = true
                };
            }
            catch (JsonException)
            {
                // JSON解析失败，使用原始文本作为描述
                return new ImageDescription
                {
                    Description = response,
                    Success = true
                };
            }
        }
    }
}
