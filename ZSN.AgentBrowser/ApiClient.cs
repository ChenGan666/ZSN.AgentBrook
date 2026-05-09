using System.Net.Http.Json;
using Newtonsoft.Json;
using ZSN.AgentBrowser.Models;

namespace ZSN.AgentBrowser
{
    /// <summary>
    /// Agent-Browser API 客户端
    /// </summary>
    public class AgentBrowserApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AgentBrowserApiClient(string baseUrl = "https://localhost:5001")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 打开 URL
        /// </summary>
        public async Task<ApiResponse<CommandResponse>> OpenAsync(string url)
        {
            var request = new OpenUrlRequest { Url = url };
            return await PostAsync<CommandResponse>("open", request);
        }

        /// <summary>
        /// 获取页面快照
        /// </summary>
        public async Task<ApiResponse<SnapshotResponse>> SnapshotAsync(bool includeInteractive = true)
        {
            var request = new SnapshotRequest { IncludeInteractive = includeInteractive };
            return await PostAsync<SnapshotResponse>("snapshot", request);
        }

        /// <summary>
        /// 点击元素
        /// </summary>
        public async Task<ApiResponse<CommandResponse>> ClickAsync(string elementRef)
        {
            var request = new ClickRequest { ElementRef = elementRef };
            return await PostAsync<CommandResponse>("click", request);
        }

        /// <summary>
        /// 输入文本
        /// </summary>
        public async Task<ApiResponse<CommandResponse>> TypeAsync(string elementRef, string text)
        {
            var request = new TypeRequest { ElementRef = elementRef, Text = text };
            return await PostAsync<CommandResponse>("type", request);
        }

        /// <summary>
        /// 按键操作
        /// </summary>
        public async Task<ApiResponse<CommandResponse>> PressAsync(string key)
        {
            var request = new PressRequest { Key = key };
            return await PostAsync<CommandResponse>("press", request);
        }

        /// <summary>
        /// 获取页面内容
        /// </summary>
        public async Task<ApiResponse<ContentResponse>> GetContentAsync()
        {
            return await GetAsync<ContentResponse>("content");
        }

        /// <summary>
        /// 获取当前 URL
        /// </summary>
        public async Task<ApiResponse<UrlResponse>> GetUrlAsync()
        {
            return await GetAsync<UrlResponse>("url");
        }

        /// <summary>
        /// 截图
        /// </summary>
        public async Task<ApiResponse<ScreenshotResponse>> ScreenshotAsync(string filePath = "")
        {
            var request = new ScreenshotRequest { FilePath = filePath };
            return await PostAsync<ScreenshotResponse>("screenshot", request);
        }

        /// <summary>
        /// 关闭浏览器
        /// </summary>
        public async Task<ApiResponse<CommandResponse>> CloseAsync()
        {
            var request = new { };
            return await PostAsync<CommandResponse>("close", request);
        }

        /// <summary>
        /// 执行自定义命令
        /// </summary>
        public async Task<ApiResponse<CommandResponse>> ExecuteCommandAsync(string command)
        {
            var request = new ExecuteCommandRequest { Command = command };
            return await PostAsync<CommandResponse>("execute", request);
        }

        /// <summary>
        /// 发送 POST 请求
        /// </summary>
        private async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object request)
        {
            try
            {
                var url = $"{_baseUrl}/api/browser/{endpoint}";
                var response = await _httpClient.PostAsJsonAsync(url, request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<T>>(json);
                return result ?? new ApiResponse<T> { Success = false, Error = "Empty response" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// 发送 GET 请求
        /// </summary>
        private async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var url = $"{_baseUrl}/api/browser/{endpoint}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse<T>>(json);
                return result ?? new ApiResponse<T> { Success = false, Error = "Empty response" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> { Success = false, Error = ex.Message };
            }
        }
    }
}
