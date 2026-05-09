using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;

namespace ZSN.AI.MCPServer.Controllers
{
    /// <summary>
    /// 媒体文件信息
    /// </summary>
    public class MediaFileInfo
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// 文件类型
        /// </summary>
        public string FileType { get; set; }
        
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }
    }

    [McpServerToolType]
    [ApiController]
    [Route("[controller]")]
    public class FileToMarkdownController: ApiBaseController
    {
        private readonly ILogger<FileToMarkdownController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _tempDirectory;
        private readonly string _mediaBaseDir;
        private readonly bool _isWindows;
        private readonly string _pandocPath;

        public FileToMarkdownController(ILogger<FileToMarkdownController> logger, IConfiguration configuration)
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
            
            // 从配置中读取媒体目录配置，如果未配置则使用默认值
            // 使用跨平台兼容的路径分隔符
            _mediaBaseDir = _configuration.GetValue<string>("FileConversion:MediaDir", "FileConversions/Media")
                .Replace("\\", "/"); // 确保路径分隔符统一
            
            // 确保临时目录存在
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
            
            // 确保媒体基目录存在
            string mediaBaseDir = Path.Combine(_tempDirectory, _mediaBaseDir);
            if (!Directory.Exists(mediaBaseDir))
            {
                Directory.CreateDirectory(mediaBaseDir);
            }
        }

        /// <summary>
        /// 将文件转换为Markdown格式
        /// </summary>
        /// <param name="file">要转换的文件</param>
        /// <returns>转换后的Markdown内容</returns>
        [HttpPost("Convert")]
        [Produces("application/json")]
        [Consumes("multipart/form-data")]
        [Description("将文件转换为Markdown格式")]
        public async Task<IActionResult> Convert(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Error = "请提供有效的文件" });
            }

            try
            {
                // 检查是否安装了Pandoc
                if (!IsPandocInstalled())
                {
                    return BadRequest(new { Error = "服务器未安装Pandoc，无法进行文件转换" });
                }

                // 创建唯一的临时文件名
                string sourceFileName = Path.GetFileName(file.FileName);
                string sourceExtension = Path.GetExtension(sourceFileName).ToLower();
                
                // 检查文件格式是否支持
                if (!IsSupportedFileFormat(sourceExtension))
                {
                    return BadRequest(new { Error = $"不支持的文件格式: {sourceExtension}" });
                }
                
                string fileId = Guid.NewGuid().ToString("N");
                string sourcePath = Path.Combine(_tempDirectory, $"{fileId}{sourceExtension}");
                string targetPath = Path.Combine(_tempDirectory, $"{fileId}.md");
                
                // 使用配置的媒体目录基路径，并添加唯一标识符避免冲突
                string mediaDir = Path.Combine(_tempDirectory, _mediaBaseDir, fileId);

                // 保存上传的文件
                using (var stream = new FileStream(sourcePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 使用Pandoc将文件转换为Markdown
                bool success = ConvertToMarkdown(sourcePath, targetPath);

                if (!success)
                {
                    // 转换失败，删除临时文件
                    SafeDeleteFile(sourcePath);
                    return StatusCode(500, new { Error = "文件转换失败" });
                }

                // 读取生成的Markdown文件
                string markdownContent = await System.IO.File.ReadAllTextAsync(targetPath);
                
                // 检查媒体目录是否存在并包含文件
                 mediaDir = Path.Combine(_tempDirectory, _mediaBaseDir, fileId);
                bool hasMedia = Directory.Exists(mediaDir) && Directory.GetFiles(mediaDir, "*", SearchOption.AllDirectories).Length > 0;
                
                // 如果有媒体文件，收集媒体文件信息
                List<MediaFileInfo> mediaFiles = new List<MediaFileInfo>();
                if (hasMedia)
                {
                    // 获取媒体目录中的所有文件
                    foreach (var _file in Directory.GetFiles(mediaDir, "*", SearchOption.AllDirectories))
                    {
                        mediaFiles.Add(new MediaFileInfo
                        {
                            FileName = Path.GetFileName(_file),
                            FileType = Path.GetExtension(_file).TrimStart('.').ToLower(),
                            FilePath = _file.Replace("\\", "/"),
                            FileSize = new FileInfo(_file).Length
                        });
                        
                        _logger.LogInformation($"发现媒体文件: {_file}");
                    }
                }

                // 清理临时文件
                SafeDeleteFile(sourcePath);
                SafeDeleteFile(targetPath);
                SafeDeleteDirectory(mediaDir);
                
                // 如果媒体目录的上级目录为空，也进行清理
                string mediaBaseDir = Path.Combine(_tempDirectory, _mediaBaseDir);
                if (Directory.Exists(mediaBaseDir) && Directory.GetDirectories(mediaBaseDir).Length == 0)
                {
                    try
                    {
                        Directory.Delete(mediaBaseDir, false); // 只删除空目录
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"删除空的媒体目录失败: {mediaBaseDir}");
                    }
                }

                // 返回转换结果，包含媒体文件信息如果存在
                return Ok(new { 
                    Content = markdownContent, 
                    OriginalFileName = sourceFileName,
                    HasMedia = hasMedia,
                    MediaFiles = mediaFiles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件转换过程中发生错误");
                return StatusCode(500, new { Error = $"文件转换失败: {ex.Message}" });
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
        /// 检查文件格式是否支持转换
        /// </summary>
        /// <param name="extension">文件扩展名</param>
        /// <returns>是否支持</returns>
        private bool IsSupportedFileFormat(string extension)
        {
            // 支持的文件格式列表
            string[] supportedFormats = {
                ".docx", ".doc", ".odt", ".rtf", ".html", ".htm",
                ".epub", ".pdf", ".tex", ".rst", ".org", ".wiki", 
                ".pptx", ".ppt", ".xlsx", ".xls", ".csv", ".txt"
            };
            
            return Array.Exists(supportedFormats, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// 使用Pandoc将文件转换为Markdown
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="targetPath">目标Markdown文件路径</param>
        /// <returns>转换是否成功</returns>
        private bool ConvertToMarkdown(string sourcePath, string targetPath)
        {
            try
            {
                // 获取目标文件的ID
                string fileId = Path.GetFileNameWithoutExtension(targetPath);
                string mediaDir = Path.Combine(_tempDirectory, _mediaBaseDir, fileId);
                
                // 确保媒体目录存在
                if (!Directory.Exists(mediaDir))
                {
                    Directory.CreateDirectory(mediaDir);
                }
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _pandocPath,
                        // 根据系统类型处理路径格式
                        Arguments = FormatPandocArguments(sourcePath, targetPath, mediaDir),
                        // 如果是Linux，可能需要指定一些额外的环境变量
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Pandoc转换失败: {error}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行Pandoc转换时发生错误");
                return false;
            }
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"删除临时文件失败: {filePath}");
            }
        }
        
        /// <summary>
        /// 安全删除目录及其内容，忽略可能的异常
        /// </summary>
        private void SafeDeleteDirectory(string dirPath)
        {
            try
            {
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true); // true表示递归删除
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"删除临时目录失败: {dirPath}");
            }
        }
        
        /// <summary>
        /// 根据操作系统格式化Pandoc参数
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="targetPath">目标路径</param>
        /// <param name="mediaDir">媒体目录</param>
        /// <returns>格式化后的命令行参数</returns>
        private string FormatPandocArguments(string sourcePath, string targetPath, string mediaDir)
        {
            // 转换路径分隔符以兼容不同的系统
            string normalizedSourcePath = sourcePath.Replace('\\', '/');
            string normalizedTargetPath = targetPath.Replace('\\', '/');
            string normalizedMediaDir = mediaDir.Replace('\\', '/');
            
            // Windows使用双引号，Linux使用单引号或转义的路径
            if (_isWindows)
            {
                return $"\"{normalizedSourcePath}\" -o \"{normalizedTargetPath}\" --extract-media=\"{normalizedMediaDir}\" --standalone";
            }
            else
            {
                // Linux下的路径处理，空格和特殊字符需要转义
                normalizedSourcePath = EscapeLinuxPath(normalizedSourcePath);
                normalizedTargetPath = EscapeLinuxPath(normalizedTargetPath);
                normalizedMediaDir = EscapeLinuxPath(normalizedMediaDir);
                
                return $"{normalizedSourcePath} -o {normalizedTargetPath} --extract-media={normalizedMediaDir} --standalone";
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
                path = path.Replace("\'", "\\\'")
                          .Replace(" ", "\\ ")
                          .Replace("(", "\\(")
                          .Replace(")", "\\)")
                          .Replace("&", "\\&");
                
                // 返回带单引号的路径
                return $"'{path}'";
            }
            
            return path;
        }
    }
}
