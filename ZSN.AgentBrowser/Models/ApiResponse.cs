namespace ZSN.AgentBrowser.Models
{
    /// <summary>
    /// API 响应基类
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? Error { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "操作成功")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResponse(string error, string message = "操作失败")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Error = error
            };
        }
    }

    /// <summary>
    /// 快照响应
    /// </summary>
    public class SnapshotResponse
    {
        public bool Success { get; set; }
        public List<PageElementDto> Elements { get; set; } = new List<PageElementDto>();
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>
    /// 页面元素 DTO
    /// </summary>
    public class PageElementDto
    {
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Ref { get; set; } = string.Empty;
    }

    /// <summary>
    /// 命令执行响应
    /// </summary>
    public class CommandResponse
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
    }

    /// <summary>
    /// 页面内容响应
    /// </summary>
    public class ContentResponse
    {
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// URL 响应
    /// </summary>
    public class UrlResponse
    {
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// 截图响应
    /// </summary>
    public class ScreenshotResponse
    {
        public bool Success { get; set; }
        public string ScreenshotUrl { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
