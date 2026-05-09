using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;
using Senparc.CO2NET.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model;

namespace ZSN.AI.MCPServer.Controllers
{
    [McpServerToolType]
    [ApiController]
    [Route("[controller]")]
    public class KnowledgeBaseController: ApiBaseController
    {
        private readonly ILogger<KnowledgeBaseController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IImportKMSService _importKMSService;

        public KnowledgeBaseController(
            ILogger<KnowledgeBaseController> logger, 
            IConfiguration configuration,
            IImportKMSService importKMSService)
        { 
            _logger = logger;
            _configuration = configuration;
            _importKMSService = importKMSService;
        }

        /// <summary>
        /// 将文件、URL、文本或Excel内容保存到知识库管理系统(KMS)
        /// 支持多种导入类型：普通文件(PDF/Word/等)、网页URL、纯文本、Excel QA格式
        /// </summary>
        /// <param name="request">知识库导入请求参数对象(ImportKMSTaskReq)，包含以下关键属性：
        /// - KmsId: 目标知识库的唯一标识符(必填)
        /// - ImportType: 导入类型枚举(1=File, 2=Url, 3=Text, 4=Excel)(必填)
        /// - FilePath: 本地文件完整路径(当ImportType为File或Excel时必填)
        /// - Url: 网页URL地址(当ImportType为Url时必填)
        /// - Text: 文本内容(当ImportType为Text时必填)
        /// - IsQA: 是否为QA问答格式数据(必填，默认false)
        /// - KnowledgeBaseFile: 知识库文件元信息对象(可选，系统会自动初始化)
        /// </param>
        /// <returns>IActionResult - HTTP响应结果，包含以下情况：
        /// - 成功(200 OK): 返回JSON对象 { Success: true, Message: "成功消息", Data: { KmsId, ImportType, DataCount } }
        /// - 参数错误(400 BadRequest): 返回JSON对象 { Success: false, Message: "错误描述" }
        /// - 服务器错误(500 InternalServerError): 返回JSON对象 { Success: false, Message: "错误详情" }
        /// </returns>
        [McpServerTool]
        [HttpPost("SaveToKMS")]
        [Produces("application/json")]
        [Description(@"将文件、URL、文本或按QA格式整理后的Excel文件的内容保存到知识库管理系统(Knowledge Management System)。
参数说明：
- KmsId (必填): 知识库ID，字符串类型，不能为空
- ImportType (必填): 导入类型，整数枚举值：
  * 1 = File (文件导入)
  * 2 = Url (网页导入)
  * 3 = Text (文本导入)
  * 4 = Excel (Excel文件导入)
- FilePath (条件必填): 文件的完整路径，当ImportType为1(File)或4(Excel)时必填，文件必须存在
- Url (条件必填): 网页URL地址，当ImportType为2(Url)时必填，必须是有效的绝对URL格式(如: https://example.com)
- Text (条件必填): 文本内容，当ImportType为3(Text)时必填，不能为空字符串
- FileName (必填): 文件名称，字符串类型,如果是文本导入，提供一个描述性名称
- IsQA (可选): 导入数据是否为QA格式，布尔值，默认false

请求JSON示例1(文件导入):
{
  ""KmsId"": ""kb_12345"",
  ""ImportType"": 1,
  ""FilePath"": ""C:/Documents/example.pdf"",
  ""FileName"": ""example.pdf"",
  ""IsQA"": false
}

请求JSON示例2(URL导入):
{
  ""KmsId"": ""kb_12345"",
  ""ImportType"": 2,
  ""Url"": ""https://example.com/article"",
  ""IsQA"": true
}

请求JSON示例3(文本导入):
{
  ""KmsId"": ""kb_12345"",
  ""ImportType"": 3,
  ""Text"": ""这是要保存到知识库的文本内容"",
  ""IsQA"": false,
  ""FileName"": ""SampleText.txt""
}")]
        public async Task<IActionResult> SaveToKMS(
            string KmsId,
            int ImportType,
            string? Url = null,
            string? Text = null,
            string? FilePath = null,
            string? FileName = null,
            string? IsQA = null)
        {
            // 解析 IsQA 参数（处理字符串类型）
            bool isQAValue = false;
            if (!string.IsNullOrEmpty(IsQA))
            {
                if (bool.TryParse(IsQA, out bool parsed))
                {
                    isQAValue = parsed;
                }
                else if (IsQA.Equals("1", StringComparison.OrdinalIgnoreCase) || 
                         IsQA.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                         IsQA.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    isQAValue = true;
                }
            }
            
            // 构建 ImportKMSTaskReq 对象
            var request = new ImportKMSTaskReq
            {
                KmsId = KmsId,
                ImportType = (ImportType)ImportType,
                Url = Url ?? string.Empty,
                Text = Text ?? string.Empty,
                FilePath = FilePath ?? string.Empty,
                FileName = FileName ?? string.Empty,
                IsQA = isQAValue,
                KnowledgeBaseFile = new KnowledgeBaseFileInfo()
            };
            
            // 调试日志 - 确认方法被调用
            _logger.LogInformation($"[KnowledgeBaseController] SaveToKMS 被调用");
            _logger.LogInformation($"[KnowledgeBaseController] KmsId: {request.KmsId}, ImportType: {request.ImportType}");
            
            try
            {

                // 验证知识库ID
                if (string.IsNullOrWhiteSpace(request.KmsId))
                {
                    return BadRequest(new { Success = false, Message = "知识库KmsId不能为空" });
                }

                // 根据导入类型验证对应的必填字段
                switch (request.ImportType)
                {
                    case ZSN.AI.Entity.Model.ImportType.File:
                        if (string.IsNullOrWhiteSpace(request.FilePath))
                        {
                            return BadRequest(new { Success = false, Message = "文件导入类型需要提供FilePath参数" });
                        }
                        if (!System.IO.File.Exists(request.FilePath))
                        {
                            return BadRequest(new { Success = false, Message = $"指定的文件不存在: {request.FilePath}" });
                        }
                        break;

                    case ZSN.AI.Entity.Model.ImportType.Url:
                        if (string.IsNullOrWhiteSpace(request.Url))
                        {
                            return BadRequest(new { Success = false, Message = "URL导入类型需要提供Url参数" });
                        }
                        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
                        {
                            return BadRequest(new { Success = false, Message = "提供的URL格式无效" });
                        }
                        break;

                    case ZSN.AI.Entity.Model.ImportType.Text:
                        if (string.IsNullOrWhiteSpace(request.Text))
                        {
                            return BadRequest(new { Success = false, Message = "文本导入类型需要提供Text参数" });
                        }
                        else
                        {
                            //判断文本长度，不能超过1000字符
                            if (request.Text.Length > 1000)
                            {
                                //将文本保存到文件
                                string tempDirName = _configuration.GetValue<string>("FileConversion:TempDirectory", "ZSN.Knowbase.FileConversions");
                                string _tempDirectory = Path.Combine(Path.GetTempPath(), tempDirName);

                                // 从配置中读取图片输出目录配置，如果未配置则使用默认值
                                string _outputDirectory = Path.Combine(_tempDirectory, "TextMDOutputs");

                                var new_file_name = request.FileName.IsNullOrEmpty() ? $"{Guid.NewGuid()}.txt" : request.FileName;
                                //替换文件后缀为.txt
                                new_file_name = Path.ChangeExtension(new_file_name, ".txt");

                                string tempFilePath = System.IO.Path.Combine(_outputDirectory, "text_file");
                                if(!System.IO.Directory.Exists(tempFilePath))
                                {
                                    System.IO.Directory.CreateDirectory(tempFilePath!);
                                }
                                tempFilePath = Path.Combine(tempFilePath, new_file_name);
                                await System.IO.File.WriteAllTextAsync(tempFilePath, request.Text);

                                request.FilePath = tempFilePath;
                                request.ImportType = Entity.Model.ImportType.File;
                            }
                        }
                        break;

                    case ZSN.AI.Entity.Model.ImportType.Excel:
                        if (string.IsNullOrWhiteSpace(request.FilePath))
                        {
                            return BadRequest(new { Success = false, Message = "Excel导入类型需要提供FilePath参数" });
                        }
                        if (!System.IO.File.Exists(request.FilePath))
                        {
                            return BadRequest(new { Success = false, Message = $"指定的Excel文件不存在: {request.FilePath}" });
                        }
                        break;

                    default:
                        _logger.LogWarning($"SaveToKMS收到未知的导入类型: {request.ImportType}");
                        return BadRequest(new { Success = false, Message = "未知的导入类型" });
                }

                //补充KnowledgeBaseFile信息
                request.KnowledgeBaseFile.FileName = request.FileName;
                request.KnowledgeBaseFile.FileID = Guid.NewGuid().ToString();//txt将被重写
                request.KnowledgeBaseFile.KnowledgeBaseID = request.KmsId;
                request.KnowledgeBaseFile.FilePath = request.FilePath;
                request.KnowledgeBaseFile.ParserConfig = "{}";
                request.KnowledgeBaseFile.DataCount = 0;
                request.KnowledgeBaseFile.CreateTime = DateTime.Now;
                request.KnowledgeBaseFile.Type = request.ImportType.ToString();
                request.KnowledgeBaseFile.SystemStatus = ZSN.AI.Entity.Model.Enum.ImportKmsStatus.Success;


                // 执行导入任务
                _logger.LogInformation($"开始执行知识库导入任务 - KmsId: {request.KmsId}, ImportType: {request.ImportType}");
                request = await _importKMSService.ImportKMSTask(request);
                _logger.LogInformation($"知识库导入任务执行成功 - KmsId: {request.KmsId}");

                //处理完成后，保存KnowledgeBaseFileInfo记录
                KnowledgeBaseFileInfoBussiness.Add(request.KnowledgeBaseFile);

                var response = new 
                { 
                    Success = true, 
                    Message = "内容已成功保存到知识库",
                    Data = new
                    {
                        KmsId = request.KmsId,
                        ImportType = request.ImportType.ToString(),
                        DataCount = request.KnowledgeBaseFile?.DataCount ?? 0
                    }
                };

                // 记录API调用日志（兼容MCP和直接HTTP调用）
                LogApiCall(request, response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SaveToKMS执行失败 - KmsId: {request?.KmsId}");
                return StatusCode(500, new 
                { 
                    Success = false, 
                    Message = $"保存到知识库时发生错误: {ex.Message}" 
                });
            }
        }
    }
}
