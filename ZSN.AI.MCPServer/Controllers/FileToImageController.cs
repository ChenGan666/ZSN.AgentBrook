using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using ZSN.AI.Core.Interface;
using ZSN.AI.Functions;
using ZSN.AI.Functions.Common;
using ZSN.Utils.Core.Utils;

namespace ZSN.AI.MCPServer.Controllers
{
    

    [McpServerToolType]
    [ApiController]
    [Route("[controller]")]
    public class FileToImageController : ApiBaseController
    {
        private readonly ILogger<FileToImageController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _tempDirectory;
        private readonly string _outputDirectory;
        private FileConverts _fileConverts;
        private readonly IChatService _chatService;

        public FileToImageController(ILogger<FileToImageController> logger, IConfiguration configuration, IChatService chatService, ILogger<FileConverts> fileConvertsLogger)
        {
            _logger = logger;
            _configuration = configuration;
            _chatService = chatService;

            // 从配置中读取临时目录名称，如果未配置则使用默认值
            string tempDirName = _configuration.GetValue<string>("FileConversion:TempDirectory", "ZSN.Knowbase.FileConversions");
            _tempDirectory = Path.Combine(Path.GetTempPath(), tempDirName);
            
            // 从配置中读取图片输出目录配置，如果未配置则使用默认值
            _outputDirectory = Path.Combine(_tempDirectory, "ImageOutputs");
            
            // 确保临时目录存在
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
            
            // 确保图片输出目录存在
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            _fileConverts = new FileConverts(fileConvertsLogger, _configuration, _chatService);
        }

        
        /// <summary>
        /// 将文件转换为图片
        /// </summary>
        /// <param name="file">要转换的文件</param>
        /// <returns>转换后的图片文件路径列表</returns>
        [HttpPost("Convert")]
        [Produces("application/json")]
        [Consumes("multipart/form-data")]
        [Description("将文件转换为图片")]
        public async Task<IActionResult> Convert(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Error = "请提供有效的文件" });
            }

            try
            {
                // 创建唯一的临时文件名
                string sourceFileName = Path.GetFileName(file.FileName);
                string sourceExtension = Path.GetExtension(sourceFileName).ToLower();
                string fileId = _fileConverts.GetMD5HashFromFile(file);
                string sourcePath = Path.Combine(_tempDirectory, $"{fileId}{sourceExtension}");
                
                // 保存上传的文件
                using (var stream = new FileStream(sourcePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                try
                {
                    return Ok(_fileConverts.ToImageFiles(sourcePath, sourceFileName));
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Ghostscript"))
                {
                    _logger.LogError(ex, "缺少Ghostscript组件");
                    return BadRequest(new { 
                        Error = "转换PDF文件需要Ghostscript组件，请在服务器上正确安装Ghostscript并添加到系统PATH环境变量。",
                        Details = ex.Message
                    });
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("LibreOffice"))
                {
                    _logger.LogError(ex, "缺少LibreOffice组件");
                    return BadRequest(new { 
                        Error = "转换Office文档需要LibreOffice组件，请在服务器上正确安装LibreOffice。",
                        Details = ex.Message
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "文件转换过程中发生错误");
                    return StatusCode(500, new { Error = $"文件转换为图片失败: {ex.Message}" });
                }
                finally
                {
                    // 清理临时文件
                    _fileConverts.SafeDeleteFile(sourcePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件处理过程中发生错误");
                return StatusCode(500, new { Error = $"文件处理失败: {ex.Message}" });
            }
        }

        
    }
}
