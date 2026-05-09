using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Server;
using MySqlX.XDevAPI;
using Pandoc;
using System.ComponentModel;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Functions;
using ZSN.AI.Functions.Common;
using ZSN.AI.Node;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AI.MCPServer.Controllers
{

    [McpServerToolType]
    [ApiController]
    [Route("[controller]")]
    public class ToMarkdownController: ApiBaseController
    {
        private readonly ILogger<ToMarkdownController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _tempDirectory;
        private string _outputDirectory;

        private FileConverts _fileConverts;

        private readonly IChatService _chatService;
        public ToMarkdownController(ILogger<ToMarkdownController> logger, IConfiguration configuration, IChatService chatService, ILogger<FileConverts> fileConvertsLogger)
        {
            _logger = logger;
            _configuration = configuration;
            _chatService = chatService;

            // 从配置中读取临时目录名称，如果未配置则使用默认值
            string tempDirName = _configuration.GetValue<string>("FileConversion:TempDirectory", "ZSN.Knowbase.FileConversions");
            _tempDirectory = Path.Combine(Path.GetTempPath(), tempDirName);

            // 从配置中读取图片输出目录配置，如果未配置则使用默认值
            _outputDirectory = Path.Combine(_tempDirectory, "MarkdownOutputs");

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

            _fileConverts = new FileConverts(fileConvertsLogger, _configuration, _chatService);
        }
        /// <summary>
        /// 将文件转换Markdown格式
        /// </summary>
        /// <param name="file">要转换的文件</param>
        /// <returns>转换后的Markdown文件路径列表</returns>
        [HttpPost("Convert")]
        [Produces("application/json")]
        [Consumes("multipart/form-data")]
        [Description("同步方法,将文件转换Markdown格式")]
        public async Task<IActionResult> Convert(IFormFile file,bool returnMarkdown = false)
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
                string sourceFileCode = _fileConverts.GetMD5HashFromFile(file);

                _outputDirectory = Path.Combine(_outputDirectory, fileId); 
                if (!Directory.Exists(_outputDirectory)) {
                    Directory.CreateDirectory(_outputDirectory);
                }

                // 保存上传的文件
                using (var stream = new FileStream(sourcePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(await _fileConverts.ToMarkdownFilesAsync(sourcePath, sourceFileName, sourceFileCode, returnMarkdown));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件处理过程中发生错误");
                return StatusCode(500, new { Error = $"文件处理失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 异步转换文件为Markdown格式，转换完成后通过回调地址POST结果
        /// </summary>
        /// <param name="file"></param>
        /// <param name="reCallUrl">转换完成后回调地址，需要支持POST，格式为ConvertToMarkdownFiles<param>
        /// <returns></returns>
        [HttpPost("ConvertAsync")]
        [Produces("application/json")]
        [Consumes("multipart/form-data")]
        [Description("异步方法,将文件转换Markdown格式")]
        public async Task<IActionResult> ConvertAsync(IFormFile file,string reCallUrl) {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Error = "请提供有效的文件" });
            }
            if (reCallUrl.IsNullOrEmpty()) { return BadRequest("请提供有效的回调地址");}
            // 创建唯一的临时文件名
            string sourceFileName = Path.GetFileName(file.FileName);
            string sourceExtension = Path.GetExtension(sourceFileName).ToLower();
            string fileId = _fileConverts.GetMD5HashFromFile(file);
            string sourcePath = Path.Combine(_tempDirectory, $"{fileId}{sourceExtension}");
            string sourceFileCode = _fileConverts.GetMD5HashFromFile(file);

            _outputDirectory = Path.Combine(_outputDirectory, fileId);
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            // 保存上传的文件
            using (var stream = new FileStream(sourcePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }


            List<ToMarkdownFile> toMarkdownFiles = new List<ToMarkdownFile>() { new ToMarkdownFile() { FilePath= sourcePath ,FileName= sourceFileName,FileCode = sourceFileCode } };

            TaskInfo taskInfo = new TaskInfo();
            taskInfo.TaskID = Guid.NewGuid().ToString();
            taskInfo.TaskType = NodeType.NotNode_Markdown;
            taskInfo.TaskConfig = new TaskConfig();
            taskInfo.TaskConfig.NodeConfig = null;
            taskInfo.TaskConfig.NotNodeConfig = new MarkdownConfig() { sourceFile= toMarkdownFiles, reCallUrl = reCallUrl };
            taskInfo.TaskConfig.Data = new TaskData() {  };
            taskInfo.LoopType = LoopType.NOLoop;
            taskInfo.RepeatValue = 1;
            taskInfo.RedoCount = 0;
            taskInfo.CreateTime = DateTime.Now;
            taskInfo.UpdateTime = DateTime.Now;
            taskInfo.FromTaskID = "";
            taskInfo.FromMainTaskID = "";

            TaskInfoBussiness.Add(taskInfo);

            return Ok(new {
                status = "success",
                message = "文件处理成功，请稍等，转换结果将发送到回调地址",
                data = new {
                    fileId = _fileConverts.GetMD5HashFromFile(file),
                    fileName = file.FileName,
                    fileSize = file.Length,
                    reCallUrl = reCallUrl
                }
            });
        }

        
    }
}
