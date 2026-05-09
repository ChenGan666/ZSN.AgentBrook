using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZSN.AI.Core.Models.Image;
using ZSN.AI.Core.Models.Image.Parameters;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;

namespace ZSN.AI.Core.Service.Providers
{
    /// <summary>
    /// OpenAI 兼容的图片生成提供商
    /// 支持 OpenAI DALL-E, QWen, Compshare 等兼容 OpenAI API 格式的服务
    /// </summary>
    public class OpenAICompatibleImageProvider : IImageProvider
    {
        /// <summary>
        /// 生成图片
        /// </summary>
        public async Task<string> GenerateImageAsync(LargeModelInfo modelInfo, ImageGenerationRequest request)
        {
            try
            {
                var httpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(modelInfo.EndPoint);
                
                // 构建完整的API URL
                var endpoint = modelInfo.EndPoint?.TrimEnd('/') ?? throw new ArgumentException("模型端点不能为空");
                
                // 智能处理端点：如果端点已包含 /v1，则只添加 /images/generations
                // 否则添加完整路径 /v1/images/generations
                var apiPath = endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) 
                    ? "/images/generations" 
                    : "/v1/images/generations";
                var apiUrl = $"{endpoint}{apiPath}";
                
                // 根据不同的AI服务商构建不同的请求体
                Dictionary<string, object> requestBodyDict = BuildRequestBody(modelInfo, request);
                
                var jsonContent = JsonSerializer.Serialize(requestBodyDict, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // 设置Authorization header
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {modelInfo.ModelKey}");
                
                
                // 调用 /v1/images/generations API
                var response = await httpClient.PostAsync(apiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API调用失败: {response.StatusCode}, {errorContent}");
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // 解析响应
                var imageUrl = ParseResponse(responseContent);
                
                
                return imageUrl;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 构建请求体
        /// </summary>
        private Dictionary<string, object> BuildRequestBody(LargeModelInfo modelInfo, ImageGenerationRequest request)
        {
            Dictionary<string, object> requestBodyDict;
            
            switch (modelInfo.ModelOrganizationID)
            {
                case AIType.Compshare:
                    // Compshare 提供商
                    if (modelInfo.TypeCode == AIModelType.T2Image)
                    {
                        // 文生图
                        requestBodyDict = new Dictionary<string, object>
                        {
                            ["model"] = modelInfo.ModelName,
                            ["prompt"] = request.Prompt,
                            ["size"] = $"{request.Width}x{request.Height}"
                        };
                    }
                    else if (modelInfo.TypeCode == AIModelType.I2Image)
                    {
                        // 图生图
                        requestBodyDict = new Dictionary<string, object>
                        {
                            ["model"] = modelInfo.ModelName,
                            ["prompt"] = request.Prompt,
                            ["size"] = $"{request.Width}x{request.Height}"
                        };
                        
                        // 图生图必须提供 image 参数
                        if (string.IsNullOrEmpty(request.ImageInput))
                        {
                            throw new ArgumentException("图生图模型需要提供 ImageInput 参数（Base64 格式的图像数据）");
                        }
                        
                        // 处理图片输入
                        var imageBase64 = ProcessImageInput(request.ImageInput).Result;
                        requestBodyDict["image"] = $"data:image/png;base64,{imageBase64}";
                    }
                    else
                    {
                        throw new ArgumentException($"不支持的 Compshare 模型类型: {modelInfo.TypeCode}");
                    }
                    break;
                    
                case AIType.OpenAI:
                    // OpenAI DALL-E 标准格式
                    requestBodyDict = new Dictionary<string, object>
                    {
                        ["model"] = modelInfo.ModelName,
                        ["prompt"] = request.Prompt,
                        ["n"] = request.N,
                        ["size"] = $"{request.Width}x{request.Height}"
                    };
                    
                    // OpenAI DALL-E 3 特有参数
                    if (modelInfo.ModelName.Contains("dall-e-3", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(request.Quality) && request.Quality != "standard")
                        {
                            requestBodyDict["quality"] = request.Quality;
                        }
                        if (!string.IsNullOrEmpty(request.Style) && request.Style != "vivid")
                        {
                            requestBodyDict["style"] = request.Style;
                        }
                    }
                    break;
                    
                default:
                    // 默认使用基础格式（兼容大多数服务商）
                    requestBodyDict = new Dictionary<string, object>
                    {
                        ["model"] = modelInfo.ModelName,
                        ["prompt"] = request.Prompt,
                        ["size"] = $"{request.Width}x{request.Height}"
                    };
                    break;
            }
            
            // 添加扩展参数
            if (request.ExtendedParameters != null)
            {
                foreach (var kvp in request.ExtendedParameters)
                {
                    requestBodyDict[kvp.Key] = kvp.Value;
                }
            }
            
            return requestBodyDict;
        }

        /// <summary>
        /// 处理图片输入(URL或Base64)
        /// </summary>
        private async Task<string> ProcessImageInput(string imageInput)
        {
            // 如果是 URL 格式,下载图片并转换为 base64
            if (imageInput.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                imageInput.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using (var imageHttpClient = new HttpClient())
                {
                    var imageResponse = await imageHttpClient.GetAsync(imageInput);
                    if (!imageResponse.IsSuccessStatusCode)
                    {
                        throw new Exception($"下载图片失败: {imageResponse.StatusCode}");
                    }
                     
                    // 使用内存流处理,不写入磁盘
                    using (var memoryStream = new MemoryStream())
                    {
                        await imageResponse.Content.CopyToAsync(memoryStream);
                        var imageBytes = memoryStream.ToArray();
                        var base64 = Convert.ToBase64String(imageBytes);
                        return base64;
                    }
                }
            }
            // 如果是 data URI,提取 base64 部分
            else if (imageInput.StartsWith("data:image/"))
            {
                var base64Index = imageInput.IndexOf("base64,");
                if (base64Index > 0)
                {
                    return imageInput.Substring(base64Index + 7);
                }
            }
            
            // 否则假设已经是 base64 格式,直接使用
            return imageInput;
        }

        /// <summary>
        /// 解析响应
        /// </summary>
        private string ParseResponse(string responseContent)
        {
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // 根据不同服务商解析返回结果
            string? imageUrl = null;
            
            if (jsonResponse.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
            {
                var firstItem = dataArray[0];
                
                // 尝试获取 url 字段
                if (firstItem.TryGetProperty("url", out var urlElement))
                {
                    imageUrl = urlElement.GetString();
                }
                // 尝试获取 b64_json 字段（QWen 可能返回 base64）
                else if (firstItem.TryGetProperty("b64_json", out var b64Element))
                {
                    var b64String = b64Element.GetString();
                    imageUrl = $"data:image/png;base64,{b64String}";
                }
            }
            
            if (string.IsNullOrEmpty(imageUrl))
            {
                throw new Exception("无法从响应中解析图像URL");
            }
            
            return imageUrl;
        }
    }
}
