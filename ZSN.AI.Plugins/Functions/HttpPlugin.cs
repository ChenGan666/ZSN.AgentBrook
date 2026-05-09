using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZSN.AI.Plugins.Functions
{
    [Description("Http能力插件")]
    public class HttpPlugin
    {
        // 静态HttpClient实例,支持Session保持
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer()
        });

        static HttpPlugin()
        {
            // 设置默认超时时间
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 通用GET请求
        /// </summary>
        [KernelFunction]
        [Description("发送HTTP GET请求")]
        [return: Description("响应内容")]
        public async Task<string> HttpGet(
            [Description("目标URL地址")] string url,
            [Description("可选的请求头,JSON格式,例如:{\"Authorization\":\"Bearer token\",\"Content-Type\":\"application/json\"}")] string headers = null)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    // 添加自定义请求头
                    if (!string.IsNullOrEmpty(headers))
                    {
                        var headerDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);
                        if (headerDict != null)
                        {
                            foreach (var header in headerDict)
                            {
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }
                        }
                    }

                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = ex.Message,
                    type = ex.GetType().Name
                });
            }
        }

        /// <summary>
        /// 通用POST请求
        /// </summary>
        [KernelFunction]
        [Description("发送HTTP POST请求")]
        [return: Description("响应内容")]
        public async Task<string> HttpPost(
            [Description("目标URL地址")] string url,
            [Description("POST数据,JSON格式")] string postData,
            [Description("可选的请求头,JSON格式,例如:{\"Authorization\":\"Bearer token\",\"Content-Type\":\"application/json\"}")] string headers = null)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    // 设置POST内容
                    if (!string.IsNullOrEmpty(postData))
                    {
                        request.Content = new StringContent(postData, Encoding.UTF8, "application/json");
                    }

                    // 添加自定义请求头
                    if (!string.IsNullOrEmpty(headers))
                    {
                        var headerDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);
                        if (headerDict != null)
                        {
                            foreach (var header in headerDict)
                            {
                                // Content-Type由Content处理,其他header添加到Headers
                                if (header.Key.ToLower() != "content-type")
                                {
                                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                                }
                                else if (request.Content != null)
                                {
                                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(header.Value);
                                }
                            }
                        }
                    }

                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = ex.Message,
                    type = ex.GetType().Name
                });
            }
        }

        /// <summary>
        /// 文件上传POST请求
        /// </summary>
        [KernelFunction]
        [Description("发送HTTP POST请求并上传文件")]
        [return: Description("响应内容")]
        public async Task<string> HttpFilePost(
            [Description("目标URL地址")] string url,
            [Description("POST表单数据,JSON格式,例如:{\"field1\":\"value1\",\"field2\":\"value2\"}")] string postData = null,
            [Description("文件路径列表,JSON数组格式,例如:[{\"fieldName\":\"file\",\"filePath\":\"C:\\\\test.txt\"}]")] string files = null,
            [Description("可选的请求头,JSON格式,例如:{\"Authorization\":\"Bearer token\"}")] string headers = null)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    var content = new MultipartFormDataContent();

                    // 添加表单字段
                    if (!string.IsNullOrEmpty(postData))
                    {
                        var formData = JsonConvert.DeserializeObject<Dictionary<string, string>>(postData);
                        if (formData != null)
                        {
                            foreach (var field in formData)
                            {
                                content.Add(new StringContent(field.Value, Encoding.UTF8), field.Key);
                            }
                        }
                    }

                    // 添加文件
                    if (!string.IsNullOrEmpty(files))
                    {
                        var fileList = JsonConvert.DeserializeObject<List<FileUploadInfo>>(files);
                        if (fileList != null)
                        {
                            foreach (var fileInfo in fileList)
                            {
                                if (System.IO.File.Exists(fileInfo.FilePath))
                                {
                                    var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(fileInfo.FilePath));
                                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                                    content.Add(fileContent, fileInfo.FieldName, System.IO.Path.GetFileName(fileInfo.FilePath));
                                }
                            }
                        }
                    }

                    request.Content = content;

                    // 添加自定义请求头
                    if (!string.IsNullOrEmpty(headers))
                    {
                        var headerDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);
                        if (headerDict != null)
                        {
                            foreach (var header in headerDict)
                            {
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }
                        }
                    }

                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = ex.Message,
                    type = ex.GetType().Name
                });
            }
        }

        /// <summary>
        /// 文件上传信息
        /// </summary>
        private class FileUploadInfo
        {
            public string FieldName { get; set; }
            public string FilePath { get; set; }
        }
    }
}
