using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZSN.AgentBrowser
{
    /// <summary>
    /// Agent-Browser 服务 - 为AI代理设计的无头浏览器自动化
    /// 支持跨平台（Windows/Linux/macOS）
    /// </summary>
    public class AgentBrowserService
    {
        private readonly string _agentBrowserPath;
        private readonly int _commandTimeoutMs;

        public AgentBrowserService(string agentBrowserPath = "agent-browser", int commandTimeoutMs = 30000)
        {
            _agentBrowserPath = agentBrowserPath;
            _commandTimeoutMs = commandTimeoutMs;
        }

        /// <summary>
        /// 打开URL
        /// </summary>
        public async Task<CommandResult> OpenAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL不能为空", nameof(url));
            
            return await ExecuteCommandAsync($"open {url}");
        }

        /// <summary>
        /// 获取页面快照（包含可访问性树和ref引用）
        /// </summary>
        /// <param name="includeInteractive">是否包含交互元素</param>
        public async Task<SnapshotResult> SnapshotAsync(bool includeInteractive = true)
        {
            var args = includeInteractive ? "snapshot -i" : "snapshot";
            var result = await ExecuteCommandAsync(args);
            return ParseSnapshot(result);
        }

        /// <summary>
        /// 点击元素
        /// </summary>
        public async Task<CommandResult> ClickAsync(string elementRef)
        {
            if (string.IsNullOrWhiteSpace(elementRef))
                throw new ArgumentException("元素引用不能为空", nameof(elementRef));
            
            if (!elementRef.StartsWith("@"))
                elementRef = "@" + elementRef;
            
            return await ExecuteCommandAsync($"click {elementRef}");
        }

        /// <summary>
        /// 输入文本
        /// </summary>
        public async Task<CommandResult> TypeAsync(string elementRef, string text)
        {
            if (string.IsNullOrWhiteSpace(elementRef))
                throw new ArgumentException("元素引用不能为空", nameof(elementRef));
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            
            if (!elementRef.StartsWith("@"))
                elementRef = "@" + elementRef;
            
            var escapedText = text.Replace("\"", "\\\"");
            return await ExecuteCommandAsync($"type {elementRef} \"{escapedText}\"");
        }

        /// <summary>
        /// 按键操作（如Enter、Tab、Escape等）
        /// </summary>
        public async Task<CommandResult> PressAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("按键不能为空", nameof(key));
            
            return await ExecuteCommandAsync($"press {key}");
        }

        /// <summary>
        /// 获取页面截图
        /// </summary>
        public async Task<CommandResult> ScreenshotAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));
            
            return await ExecuteCommandAsync($"screenshot {filePath}");
        }

        /// <summary>
        /// 获取页面内容
        /// </summary>
        public async Task<string> GetContentAsync()
        {
            var result = await ExecuteCommandAsync("content");
            return result.Success ? result.Output : string.Empty;
        }

        /// <summary>
        /// 获取当前页面URL
        /// </summary>
        public async Task<string> GetUrlAsync()
        {
            var result = await ExecuteCommandAsync("url");
            return result.Success ? result.Output.Trim() : string.Empty;
        }

        /// <summary>
        /// 保存截图到指定位置，按年月日/小时目录结构组织
        /// </summary>
        public async Task<(bool Success, string ScreenshotUrl, string FilePath, string Error)> SaveScreenshotAsync(string screenshotFileName = "")
        {
            try
            {
                // 生成目录结构: screenshots/yyyy/MM/dd/HH/
                var now = DateTime.Now;
                var screenshotDir = Path.Combine(
                    AppContext.BaseDirectory,
                    "screenshots",
                    now.Year.ToString(),
                    now.Month.ToString("D2"),
                    now.Day.ToString("D2"),
                    now.Hour.ToString("D2")
                );

                // 创建目录
                Directory.CreateDirectory(screenshotDir);

                // 生成文件名
                if (string.IsNullOrWhiteSpace(screenshotFileName))
                {
                    screenshotFileName = $"screenshot_{now:HHmmss_fff}.png";
                }
                else if (!screenshotFileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    screenshotFileName += ".png";
                }

                var filePath = Path.Combine(screenshotDir, screenshotFileName);

                // 执行 agent-browser 截图命令
                var result = await ExecuteCommandAsync($"screenshot {filePath}");

                if (!result.Success)
                {
                    return (false, string.Empty, filePath, result.Error);
                }

                // 生成 URL路径（相对于 screenshots 目录）
                var relativePath = Path.Combine(
                    "screenshots",
                    now.Year.ToString(),
                    now.Month.ToString("D2"),
                    now.Day.ToString("D2"),
                    now.Hour.ToString("D2"),
                    screenshotFileName
                ).Replace("\\", "/");

                return (true, relativePath, filePath, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, string.Empty, ex.Message);
            }
        }

        /// <summary>
        /// 关闭浏览器
        /// </summary>
        public async Task<CommandResult> CloseAsync()
        {
            return await ExecuteCommandAsync("close");
        }

        /// <summary>
        /// 执行自定义命令
        /// </summary>
        public async Task<CommandResult> ExecuteCommandAsync(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("命令不能为空", nameof(command));
            
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = _agentBrowserPath,
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                        return new CommandResult 
                        { 
                            Success = false, 
                            Error = "Failed to start agent-browser process" 
                        };

                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    if (!process.WaitForExit(_commandTimeoutMs))
                    {
                        process.Kill();
                        return new CommandResult 
                        { 
                            Success = false, 
                            Error = "Command timeout" 
                        };
                    }

                    var output = await outputTask;
                    var error = await errorTask;

                    return new CommandResult
                    {
                        Success = process.ExitCode == 0,
                        Output = output,
                        Error = error,
                        ExitCode = process.ExitCode
                    };
                }
            }
            catch (Exception ex)
            {
                return new CommandResult 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        /// <summary>
        /// 解析快照输出为结构化数据
        /// </summary>
        private SnapshotResult ParseSnapshot(CommandResult result)
        {
            var snapshot = new SnapshotResult { Success = result.Success };

            if (!result.Success)
            {
                snapshot.Error = result.Error;
                return snapshot;
            }

            var elements = new List<PageElement>();
            var lines = result.Output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var element = ParseElementLine(line);
                if (element != null)
                    elements.Add(element);
            }

            snapshot.Elements = elements;
            return snapshot;
        }

        /// <summary>
        /// 解析单行元素信息
        /// 格式: - type "text" [ref=e1]
        /// </summary>
        private PageElement? ParseElementLine(string line)
        {
            try
            {
                // 匹配模式: - type "text" [ref=xxx]
                var pattern = @"^\s*-\s+(\w+)\s+""([^""]*?)""\s+\[ref=([\w]+)\]";
                var match = Regex.Match(line, pattern);

                if (!match.Success)
                    return null;

                return new PageElement
                {
                    Type = match.Groups[1].Value,
                    Text = match.Groups[2].Value,
                    Ref = match.Groups[3].Value
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
