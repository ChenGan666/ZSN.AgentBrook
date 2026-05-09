using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 文本向量化服务实现
    /// 使用数据库驱动的模型配置
    /// 参考 KMService 的实现方式
    /// </summary>
    public class EmbeddingService : IEmbeddingService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmbeddingService> _logger;
        private readonly LargeModelInfo _modelInfo;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// 向量维度（根据模型配置）
        /// </summary>
        public int VectorDimension { get; }

        public EmbeddingService(
            IConfiguration configuration,
            ILogger<EmbeddingService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // 从配置文件获取 EmbeddingModelID
            var embeddingModelId = int.Parse(_configuration["LargeModel:EmbeddingModelID"] ?? "12");

            // 从数据库获取模型配置信息
            _modelInfo = LargeModelInfoBussiness.GetModel(embeddingModelId);

            if (_modelInfo == null)
            {
                throw new InvalidOperationException($"未找到模型ID为 {embeddingModelId} 的嵌入模型配置");
            }

            // 创建 HttpClient（使用原始 EndPoint，不在构造函数中规范化）
            _httpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(_modelInfo.EndPoint);

            // 根据模型类型设置向量维度
            VectorDimension = GetVectorDimension(_modelInfo);
        }

        /// <summary>
        /// 构建完整的 API 端点 URL
        /// </summary>
        private string BuildApiEndpoint(string apiPath)
        {
            var baseUrl = _modelInfo.EndPoint.TrimEnd('/');

            // 根据模型类型处理 /v1 前缀
            switch (_modelInfo.ModelOrganizationID)
            {
                case AIType.OpenAI:
                    // OpenAI API 需要 /v1 前缀
                    if (!baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                    {
                        baseUrl += "/v1";
                    }
                    break;

                case AIType.Ollama:
                    // Ollama API 不需要 /v1 前缀
                    // 如果包含 /v1，则移除它
                    if (baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                    {
                        baseUrl = baseUrl.Substring(0, baseUrl.Length - 3);
                    }
                    break;

                default:
                    // 其他 OpenAI 兼容 API 通常需要 /v1
                    if (!baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                    {
                        baseUrl += "/v1";
                    }
                    break;
            }

            var fullUrl = baseUrl + apiPath;
            return fullUrl;
        }

        /// <summary>
        /// 生成单个文本的向量
        /// </summary>
        public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("文本不能为空", nameof(text));
            }

            var embeddings = await GetEmbeddingsAsync(new[] { text }, cancellationToken);
            return embeddings.FirstOrDefault();
        }

        /// <summary>
        /// 批量生成文本向量
        /// </summary>
        public async Task<float[][]> GetEmbeddingsAsync(string[] texts, CancellationToken cancellationToken = default)
        {
            if (texts == null || texts.Length == 0)
            {
                throw new ArgumentException("文本列表不能为空", nameof(texts));
            }

            try
            {
                // 根据模型类型调用相应的 API（参考 KMService 的 WithTextEmbeddingGenerationByAIType）
                switch (_modelInfo.ModelOrganizationID)
                {
                    case AIType.OpenAI:
                        return await GenerateOpenAIEmbeddingsAsync(texts, cancellationToken);

                    case AIType.Ollama:
                        return await GenerateOllamaEmbeddingsAsync(texts, cancellationToken);

                    default:
                        // 默认使用 OpenAI 兼容 API
                        return await GenerateOpenAIEmbeddingsAsync(texts, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成向量时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 使用 OpenAI 兼容 API 生成嵌入（参考 KMService）
        /// </summary>
        private async Task<float[][]> GenerateOpenAIEmbeddingsAsync(string[] texts, CancellationToken cancellationToken)
        {
            // 构建完整的 API 端点 URL
            var endpoint = BuildApiEndpoint("/embeddings");

            // 构建请求体
            var requestBody = new Dictionary<string, object>
            {
                { "model", _modelInfo.ModelName }
            };

            // OpenAI API: 单个文本用 string，多个文本用 array
            if (texts.Length == 1)
            {
                requestBody["input"] = texts[0];
            }
            else
            {
                requestBody["input"] = texts;
            }

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            // 添加 API Key
            if (!string.IsNullOrEmpty(_modelInfo.ModelKey))
            {
                request.Headers.Add("Authorization", $"Bearer {_modelInfo.ModelKey}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // 解析响应
            using var jsonDoc = JsonDocument.Parse(responseContent);
            var dataArray = jsonDoc.RootElement.GetProperty("data");

            var results = new float[dataArray.GetArrayLength()][];
            for (int i = 0; i < results.Length; i++)
            {
                var embeddingArray = dataArray[i].GetProperty("embedding");
                results[i] = embeddingArray.EnumerateArray().Select(x => x.GetSingle()).ToArray();
            }

            return results;
        }

        /// <summary>
        /// 使用 Ollama API 生成嵌入（参考 KMService）
        /// </summary>
        private async Task<float[][]> GenerateOllamaEmbeddingsAsync(string[] texts, CancellationToken cancellationToken)
        {
            // 构建完整的 API 端点 URL
            var endpoint = BuildApiEndpoint("/api/embeddings");

            var results = new List<float[]>();

            // Ollama API 需要逐个文本调用
            foreach (var text in texts)
            {
                var requestBody = new Dictionary<string, object>
                {
                    { "model", _modelInfo.ModelName },
                    { "prompt", text }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                using var jsonDoc = JsonDocument.Parse(responseContent);
                var embeddingArray = jsonDoc.RootElement.GetProperty("embedding");

                var embedding = embeddingArray.EnumerateArray().Select(x => x.GetSingle()).ToArray();
                results.Add(embedding);
            }

            return results.ToArray();
        }

        /// <summary>
        /// 根据模型信息获取向量维度
        /// </summary>
        private int GetVectorDimension(LargeModelInfo modelInfo)
        {
            // 常见模型的默认向量维度
            var knownModels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                // OpenAI 模型
                { "text-embedding-ada-002", 1536 },
                { "text-embedding-3-small", 1536 },
                { "text-embedding-3-large", 3072 },

                // BGE 模型
                { "bge-large-zh", 1024 },
                { "bge-large-zh-v1.5", 1024 },
                { "bge-base-zh-v1.5", 768 },
                { "bge-small-zh-v1.5", 512 },

                // Ollama 模型
                { "embeddinggemma", 768 },
                { "nomic-embed-text", 768 },
                { "mxbai-embed-large", 1024 },
                { "all-minilm", 384 },

                // 其他常见模型
                { "m3e-base", 768 },
                { "m3e-large", 1024 },
                { "gte-large", 1024 },
                { "e5-large-v2", 1024 },
            };

            // 尝试从模型名称匹配
            foreach (var knownModel in knownModels)
            {
                if (modelInfo.ModelName.Contains(knownModel.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return knownModel.Value;
                }
            }

            // 默认返回 1536（OpenAI ada-002 的维度）
            return 1536;
        }
    }
}
