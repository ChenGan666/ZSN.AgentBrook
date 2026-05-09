using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model;
using ZSN.Utils.Core.Extensions;
using ZSN.AI.Core.Common.DependencyInjection;

namespace ZSN.AgentBrook.Plugins.Functions
{
    /// <summary>
    /// Markdown转换为docx响应结果
    /// </summary>
    public class MarkdownToDocResponse
    {
        /// <summary>
        /// 是否转换成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 文件的完整物理路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件名（含扩展名）
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件所在的日期目录（yyyyMMdd格式）
        /// </summary>
        public string DateFolder { get; set; }

        /// <summary>
        /// 成功提示信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 文件下载 URL
        /// </summary>
        public string DownloadUrl { get; set; }
    }
    [ServiceDescription(typeof(MarkdownPlugin), ServiceLifetime.Scoped)]
    [Description("Markdown能力插件")]
    public class MarkdownPlugin
    {
        private readonly ILogger<MarkdownPlugin> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _tempDirectory;
        private string _outputDirectory;
        private readonly bool _isWindows;
        private readonly string _pandocPath;

        public MarkdownPlugin(
            ILogger<MarkdownPlugin> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            // 检测当前系统类型
            _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _logger.LogInformation($"当前系统类型: {(_isWindows ? "Windows" : "Linux/Unix")}");

            // 从配置中读取Pandoc路径，如果未配置则使用默认值"pandoc"
            _pandocPath = _configuration.GetValue<string>("FileConversion:PandocPath", "pandoc");

            // 从配置中读取临时目录名称，如果未配置则使用默认值
            string tempDirName = _configuration.GetValue<string>("FileConversion:TempDirectory", "ZSN.Knowbase.FileConversions");
            _tempDirectory = Path.Combine(Path.GetTempPath(), tempDirName);

            // 从配置中读取图片输出目录配置，如果未配置则使用默认值
            _outputDirectory = Path.Combine(_tempDirectory, "DocOutputs");

            // 确保临时目录存在
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }

            // 确保输出目录存在
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        [KernelFunction]
        [Description("ZSN.AI.Plugins:将Markdown保存到docx文件")]
        [return: Description("数据ID")]
        public async Task<object> saveToDoc(string MarkdownString, string? Filename)
        {
            _logger.LogInformation($"收到转换请求");

            // 参数验证
            if (MarkdownString.IsNullOrEmpty())
            {
                _logger.LogWarning("Markdown内容为空");
                return new { Success = false, Message = "文本导入类型需要提供Text参数" };
            }

            _logger.LogInformation($"Markdown长度: {MarkdownString?.Length ?? 0}, Filename: {Filename}");


            if (string.IsNullOrWhiteSpace(Filename))
            {
                _logger.LogWarning("文件名为空");
                return new { Success = false, Message = "文件名不能为空" };
            }

            // 检查是否安装了Pandoc
            if (!IsPandocInstalled())
            {
                return new { Success = false, Message = "服务器未安装Pandoc，无法进行文件转换" };
            }

            // 清理文件名中的非法字符
            string filename = string.Join("_", Filename.Split(Path.GetInvalidFileNameChars()));

            // 创建临时Markdown文件路径
            string tempMarkdownFile = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}.md");
            string tempDocxFile = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}.docx");

            try
            {
                // 将Markdown字符串写入临时文件
                await System.IO.File.WriteAllTextAsync(tempMarkdownFile, MarkdownString, System.Text.Encoding.UTF8);
                _logger.LogInformation($"临时Markdown文件已创建: {tempMarkdownFile}");

                // 执行转换
                bool conversionSuccess = ConvertMarkdownToDocx(tempMarkdownFile, tempDocxFile);

                if (!conversionSuccess)
                {
                    return new { Success = false, Message = "Markdown转换为docx失败" };
                }

                // 创建按日期组织的目录结构 (yyyyMMdd)
                string dateFolder = DateTime.Now.ToString("yyyyMMdd");
                string targetDirectory = Path.Combine(_outputDirectory, dateFolder);

                // 确保目标目录存在
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                    _logger.LogInformation($"创建日期目录: {targetDirectory}");
                }

                // 构建最终的文件路径
                string finalFilePath = Path.Combine(targetDirectory, $"{filename}.docx");

                // 如果文件已存在，添加时间戳避免覆盖
                if (System.IO.File.Exists(finalFilePath))
                {
                    string timestamp = DateTime.Now.ToString("HHmmss");
                    finalFilePath = Path.Combine(targetDirectory, $"{filename}_{timestamp}.docx");
                    _logger.LogInformation($"文件已存在，使用时间戳重命名: {finalFilePath}");
                }

                // 移动临时文件到目标位置
                System.IO.File.Move(tempDocxFile, finalFilePath);
                _logger.LogInformation($"文件已保存到: {finalFilePath}");

                // 获取文件信息
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(finalFilePath);

                // 生成下载 URL
                // 优先使用配置的外部URL，支持Nginx反向代理场景
                string baseUrl = _configuration.GetValue<string>("ExternalUrl");
                
                string downloadUrl = $"{baseUrl}/MarkdownToDoc/Download?dateFolder={dateFolder}&fileName={Uri.EscapeDataString(Path.GetFileName(finalFilePath))}";

                // 返回成功结果
                var response = new MarkdownToDocResponse
                {
                    Success = true,
                    FilePath = finalFilePath,
                    FileName = Path.GetFileName(finalFilePath),
                    FileSize = fileInfo.Length,
                    DateFolder = dateFolder,
                    Message = "Markdown转换为docx成功",
                    DownloadUrl = downloadUrl
                };

                return new { Success = true, Message = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Markdown转换过程中发生错误");
                return new { Success = false, Message = $"转换失败: {ex.Message}" };
            }
            finally
            {
                // 清理临时文件
                SafeDeleteFile(tempMarkdownFile);
                SafeDeleteFile(tempDocxFile);
            }
        }

        /// <summary>
        /// 检查系统是否安装了Pandoc
        /// </summary>
        private bool IsPandocInstalled()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _pandocPath,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Contains("pandoc");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 使用Pandoc将Markdown文件转换为docx文档
        /// </summary>
        /// <param name="sourcePath">源Markdown文件路径</param>
        /// <param name="targetPath">目标docx文件路径</param>
        /// <returns>转换是否成功</returns>
        private bool ConvertMarkdownToDocx(string sourcePath, string targetPath)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _pandocPath,
                        Arguments = FormatPandocArguments(sourcePath, targetPath),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _logger.LogInformation($"执行Pandoc转换: {_pandocPath} {process.StartInfo.Arguments}");

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Pandoc转换失败: {error}");
                    return false;
                }

                _logger.LogInformation($"Pandoc转换成功，输出文件: {targetPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行Pandoc转换时发生错误");
                return false;
            }
        }
    
    /// <summary>
        /// 根据操作系统格式化Pandoc参数
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="targetPath">目标文件路径</param>
        /// <returns>格式化后的命令行参数</returns>
        private string FormatPandocArguments(string sourcePath, string targetPath)
        {
            // 转换路径分隔符以兼容不同的系统
            string normalizedSourcePath = sourcePath.Replace('\\', '/');
            string normalizedTargetPath = targetPath.Replace('\\', '/');

            // Windows使用双引号，Linux使用单引号或转义的路径
            if (_isWindows)
            {
                return $"\"{normalizedSourcePath}\" -o \"{normalizedTargetPath}\" --standalone";
            }
            else
            {
                // Linux下的路径处理，空格和特殊字符需要转义
                normalizedSourcePath = EscapeLinuxPath(normalizedSourcePath);
                normalizedTargetPath = EscapeLinuxPath(normalizedTargetPath);

                return $"{normalizedSourcePath} -o {normalizedTargetPath} --standalone";
            }
        }

        /// <summary>
        /// 转义Linux下的路径中的特殊字符
        /// </summary>
        private string EscapeLinuxPath(string path)
        {
            // 如果路径包含空格或其他需要转义的特殊字符
            if (path.Contains(" ") || path.Contains("(") || path.Contains(")") || path.Contains("'") || path.Contains("&"))
            {
                // 先转义所有特殊字符
                path = path.Replace("\'", "\\'")
                          .Replace(" ", "\\ ")
                          .Replace("(", "\\(")
                          .Replace(")", "\\)")
                          .Replace("&", "\\&");

                // 返回带单引号的路径
                return $"'{path}'";
            }

            return path;
        }

        /// <summary>
        /// 安全删除文件，忽略可能的异常
        /// </summary>
        private void SafeDeleteFile(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation($"已删除临时文件: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"删除临时文件失败: {filePath}");
            }
        }

        /// <summary>
        /// 下载转换后的docx文件
        /// </summary>
        /// <param name="dateFolder">日期目录（yyyyMMdd格式）</param>
        /// <param name="fileName">文件名（含扩展名）</param>
        /// <returns>文件下载结果</returns>
        [HttpGet("Download")]
        [Description("下载转换后的docx文件。参数：dateFolder-日期目录(yyyyMMdd)，fileName-文件名。返回二进制文件流。")]
        public IActionResult Download([FromQuery] string dateFolder, [FromQuery] string fileName)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrWhiteSpace(dateFolder))
                {
                    return new BadRequestObjectResult(new { Error = "日期目录参数不能为空" });
                }

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return new BadRequestObjectResult(new { Error = "文件名参数不能为空" });
                }

                // 验证日期目录格式是否符合 yyyyMMdd
                if (!System.Text.RegularExpressions.Regex.IsMatch(dateFolder, @"^\d{8}$"))
                {
                    return new BadRequestObjectResult(new { Error = "日期目录格式不正确，应为yyyyMMdd格式" });
                }

                // 验证文件扩展名
                if (!fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    return new BadRequestObjectResult(new { Error = "只支持下载.docx文件" });
                }

                // 防止路径穿越攻击
                fileName = Path.GetFileName(fileName);

                // 构建文件完整路径
                string filePath = Path.Combine(_outputDirectory, dateFolder, fileName);

                // 检查文件是否存在
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning($"请求下载的文件不存在: {filePath}");
                    return new NotFoundObjectResult(new { Error = "文件不存在或已被删除" });
                }

                _logger.LogInformation($"开始下载文件: {filePath}");

                // 读取文件内容
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                // 返回文件
                return new FileContentResult(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document") { FileDownloadName = fileName };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件下载过程中发生错误");
                return new ObjectResult(new { Error = $"文件下载失败: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}
