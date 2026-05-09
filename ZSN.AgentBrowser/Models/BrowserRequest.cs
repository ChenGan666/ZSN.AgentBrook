namespace ZSN.AgentBrowser.Models
{
    /// <summary>
    /// 打开 URL 请求
    /// </summary>
    public class OpenUrlRequest
    {
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// 获取快照请求
    /// </summary>
    public class SnapshotRequest
    {
        public bool IncludeInteractive { get; set; } = true;
    }

    /// <summary>
    /// 点击元素请求
    /// </summary>
    public class ClickRequest
    {
        public string ElementRef { get; set; } = string.Empty;
    }

    /// <summary>
    /// 输入文本请求
    /// </summary>
    public class TypeRequest
    {
        public string ElementRef { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// 按键请求
    /// </summary>
    public class PressRequest
    {
        public string Key { get; set; } = string.Empty;
    }

    /// <summary>
    /// 截图请求
    /// </summary>
    public class ScreenshotRequest
    {
        public string FilePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// 执行自定义命令请求
    /// </summary>
    public class ExecuteCommandRequest
    {
        public string Command { get; set; } = string.Empty;
    }
}
