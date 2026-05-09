using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Node;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AgentBrook.Plugins.Functions
{
    /// <summary>
    /// Word书签替换响应结果
    /// </summary>
    public class DocxResponse
    {
        /// <summary>
        /// 是否替换成功
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
        /// 成功提示信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 文件下载 URL
        /// </summary>
        public string DownloadUrl { get; set; }
        /// <summary>
        /// 书签数据
        /// </summary>
        public Dictionary<string,string> DocLabelData { get; set; }
    }

    [ServiceDescription(typeof(MarkdownPlugin), ServiceLifetime.Scoped)]
    [Description("Word操作插件")]
    public class DocxPlugin
    {
        private readonly ILogger<DocxPlugin> _logger;
        private readonly IConfiguration _configuration;
        private readonly IChatService _chatService;

        private readonly string _tempDirectory;
        private string _outputDirectory;
        private readonly bool _isWindows;
        public DocxPlugin(ILogger<DocxPlugin> logger, IConfiguration configuration, IChatService chatService)
        {
            _logger = logger;
            _configuration = configuration;
            _chatService = chatService;

            // 检测当前系统类型
            _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _logger.LogInformation($"当前系统类型: {(_isWindows ? "Windows" : "Linux/Unix")}");

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
        [Description("ZSN.AI.Plugins:根据模板ID生成Word文件,由大模型参与参数校验")]
        [return: Description("Word文件地址")]
        public async Task<object> toDocByLLM(string tmeplateID, string templateLabelJson, string? Filename)
        {

            if (string.IsNullOrWhiteSpace(templateLabelJson))
            {
                _logger.LogWarning("模板书签数据为空");
                return new { Success = false, Message = "模板书签数据为空" };
            }
            if (string.IsNullOrWhiteSpace(tmeplateID))
            {
                _logger.LogWarning("模板文件ID为空");
                return new { Success = false, Message = "模板文件ID为空" };
            }
            WordTemplateInfo wordTemplate = WordTemplateInfoBussiness.GetModel(tmeplateID);

            LargeModelInfo defaultLLM = LargeModelInfoBussiness.GetDefaultModel();

            if (defaultLLM == null)
            {
                _logger.LogWarning("默认大模型未配置");
                return new { Success = false, Message = "默认大模型未配置" };
            }

            if (wordTemplate == null)
            {
                _logger.LogWarning("模板文件不存在");
                return new { Success = false, Message = "模板文件不存在" };
            }
            //读取模板书签数据，使用LLM进行处理
            var wordTemplateLabelJson = wordTemplate.WLabel;
            if (wordTemplateLabelJson == null)
            {
                _logger.LogWarning("模板书签数据丢失");
                return new { Success = false, Message = "模板书签数据丢失" };
            }

            // 使用LLM处理书签数据
            var dataMsg = new StringBuilder();
            CallFunction callFunction = new CallFunction();
            dataMsg.AppendLine("#角色");
            dataMsg.AppendLine("你是一个 JSON 数据匹配助手，只做确定性结构补全。");

            dataMsg.AppendLine("#任务");
            dataMsg.AppendLine("输入参数 A（书签 JSON）和参数 B（替换值 JSON）。");
            dataMsg.AppendLine("以 参数 A 的 key 为最终 key，从参数 B 中匹配对应 value，生成完整 JSON。");

            dataMsg.AppendLine("#规则");
            dataMsg.AppendLine("输出 JSON 的 key 必须与参数 A 完全一致");
            dataMsg.AppendLine("参数 B 的 key 可不完全相同，允许语义/前缀匹配");
            dataMsg.AppendLine("若 A 的 key 仅末尾数字不同（如 名称1/名称2），而 B 中存在去数字后的 key，则该 value 应用于所有对应 key");
            dataMsg.AppendLine("优先级：完全匹配 ＞ 去尾号匹配 ＞ 明显语义相同");
            dataMsg.AppendLine("无法匹配的 key，value 输出 \"\"");

            dataMsg.AppendLine("#输出");
            dataMsg.AppendLine("仅输出最终 JSON，不要解释");

            dataMsg.AppendLine("#输入");

            dataMsg.AppendLine($"参数A: ```{wordTemplateLabelJson}```");
            dataMsg.AppendLine($"参数B: ```{templateLabelJson}```");


            ChatHistory history = new ChatHistory();
            LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(defaultLLM.LargeModelID);
            LargeModelConfig modelConfig = new LargeModelConfig();
            modelConfig.Id = largeModel.LargeModelID.ToString();
            modelConfig.Model = largeModel;
            modelConfig.SemanticFunction = null;
            modelConfig.NativeFunction = null;
            modelConfig.Temperature = 0.3;
            modelConfig.TopPCoefficient = 0.8;
            modelConfig.ResponseFormat = "json_object";
            modelConfig.Thinking = false;

            history.AddSystemMessage(dataMsg.ToString());
            history.AddUserMessage("继续");

            callFunction = new CallFunction();
            callFunction.Prompt = string.Join("\n", history.Select(x => x.Role + ": " + x.Content)) + "\n" + dataMsg.ToString();
            callFunction.Input = "继续";

            modelConfig.Temperature = 0;//不允许大模型自由发挥

            var functionCallRe = _chatService.PromptFunctionCall(modelConfig, callFunction);
            string response_str = "";
            await foreach (var response in functionCallRe)
            {
                response_str += response;
            }

            return await toDoc(tmeplateID, response_str, Filename);
        }

        [KernelFunction]
        [Description("ZSN.AI.Plugins:根据模板ID生成Word文件")]
        [return: Description("Word文件地址")]
        public async Task<object> toDoc(string tmeplateID, string templateLabelJson, string? Filename)
        {
            if (string.IsNullOrWhiteSpace(templateLabelJson))
            {
                _logger.LogWarning("模板书签数据为空");
                return new { Success = false, Message = "模板书签数据为空" };
            }

            // 过滤大模型生成的JSON标记(```json等)
            if (ZSN.Utils.Core.Utils.Utils.TryExtractStrictJson(templateLabelJson, out var cleanedJson))
            {
                templateLabelJson = cleanedJson;
                _logger.LogInformation("已过滤JSON代码围栏标记");
            }
            else
            {
                _logger.LogWarning("JSON过滤失败,将尝试直接解析");
            }

            Dictionary<string, string> templateLabelValue = new Dictionary<string, string>();
            try
            {
                var jo = JObject.Parse(templateLabelJson);
                foreach (var prop in jo.Properties())
                {
                    var key = prop.Name;
                    var token = prop.Value;
                    string valueStr = string.Empty;

                    if (token == null || token.Type == JTokenType.Null)
                    {
                        valueStr = string.Empty;
                    }
                    else if (token.Type == JTokenType.Array)
                    {
                        var arr = (JArray)token;
                        var parts = arr.Select(item =>
                        {
                            if (item == null || item.Type == JTokenType.Null) return string.Empty;
                            return item is JValue ? item.ToString() : item.ToString(Formatting.None);
                        });
                        valueStr = string.Join("\n", parts);
                    }
                    else if (token.Type == JTokenType.Object)
                    {
                        valueStr = token.ToString(Formatting.None);
                    }
                    else
                    {
                        valueStr = token.ToString();
                    }

                    templateLabelValue[key] = valueStr;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"模板书签数据格式错误:{templateLabelJson}");
                return new { Success = false, Message = $"模板书签数据格式错误:{ex.Message}\ntemplateLabelJson:{templateLabelJson}" };
            }

            if (string.IsNullOrWhiteSpace(tmeplateID))
            {
                _logger.LogWarning("模板文件ID为空");
                return new { Success = false, Message = "模板文件ID为空" };
            }

            WordTemplateInfo wordTemplate = WordTemplateInfoBussiness.GetModel(tmeplateID);

            if (wordTemplate == null)
            {
                _logger.LogWarning("模板文件不存在");
                return new { Success = false, Message = "模板文件不存在" };
            }

            FilesInfo fileInfo = FilesInfoBussiness.GetModel(wordTemplate.FileCode);
            if (fileInfo == null)
            {
                _logger.LogWarning("模板文件信息不存在");
                return new { Success = false, Message = "模板文件信息不存在" };
            }

            string templateFilePath = fileInfo.FFilePath + "/" + fileInfo.FName;
            return await saveToDoc(templateLabelValue, templateFilePath, Filename);
        }


        [KernelFunction]
        [Description("ZSN.AI.Plugins:根据模板文件生成Word文件")]
        [return: Description("Word文件地址")]
        public async Task<object> saveToDoc(Dictionary<string, string> templateLabelValue, string templateFile, string? Filename)
        {
            if (string.IsNullOrWhiteSpace(templateFile))
            {
                _logger.LogWarning("模板文件名为空");
                return new { Success = false, Message = "模板文件名为空" };
            }

            if (System.IO.File.Exists(templateFile) == false)
            {
                _logger.LogWarning("模板文件不存在");
                return new { Success = false, Message = "模板文件不存在" };
            }

            try
            {
                string outputFileName = string.IsNullOrWhiteSpace(Filename) ? $"Generated_{Guid.NewGuid()}.docx" : Filename;


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
                string finalFilePath = Path.Combine(targetDirectory, outputFileName);

                // 如果文件已存在，添加时间戳避免覆盖
                if (System.IO.File.Exists(finalFilePath))
                {
                    string timestamp = DateTime.Now.ToString("HHmmss");
                    finalFilePath = Path.Combine(targetDirectory, $"{timestamp}_{outputFileName}");
                    _logger.LogInformation($"文件已存在，使用时间戳重命名: {finalFilePath}");
                }

                // 复制模板文件到输出路径
                System.IO.File.Copy(templateFile, finalFilePath, true);

                // 使用 OpenXML 处理书签替换
                using (WordprocessingDocument doc = WordprocessingDocument.Open(finalFilePath, true))
                {
                    var mainPart = doc.MainDocumentPart;
                    var document = mainPart.Document;
                    var body = document.Body;

                    ReplaceBookmarks(mainPart, templateLabelValue);
                    // 保存文档
                    document.Save();
                }

                // 获取文件信息
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(finalFilePath);

                // 生成下载 URL
                // 优先使用配置的外部URL，支持Nginx反向代理场景
                string baseUrl = _configuration.GetValue<string>("ExternalUrl");
                string downloadUrl = $"{baseUrl}/MarkdownToDoc/Download?dateFolder={dateFolder}&fileName={Uri.EscapeDataString(Path.GetFileName(finalFilePath))}";


                // 返回成功结果
                var response = new DocxResponse
                {
                    Success = true,
                    FilePath = finalFilePath,
                    FileName = Path.GetFileName(finalFilePath),
                    FileSize = fileInfo.Length,
                    Message = "生成Word文件成功",
                    DownloadUrl = downloadUrl,
                    DocLabelData = templateLabelValue
                };

                return new { Success = true, Message = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成Word文件时出错");
                return new { Success = false, Message = $"生成Word文件时出错: {ex.Message}" };
            }
        }

        private void ReplaceBookmarks(MainDocumentPart mainPart, IDictionary<string, string> values)
        {
            if (mainPart == null || mainPart.Document == null) return;
            if (values == null || values.Count == 0) return;

            var document = mainPart.Document;

            var endById = document
                .Descendants<BookmarkEnd>()
                .Where(be => be.Id != null)
                .GroupBy(be => be.Id.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var startsByName = document
                .Descendants<BookmarkStart>()
                .GroupBy(bs => bs.Name?.Value ?? string.Empty)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kv in values)
            {
                var name = kv.Key;
                var newText = kv.Value;

                // 只检查书签名称,允许空值替换
                if (string.IsNullOrEmpty(name)) continue;
                if (!startsByName.TryGetValue(name, out var starts) || starts == null || starts.Count == 0)
                {
                    _logger.LogWarning($"未找到书签：{name}");
                    continue;
                }

                foreach (var bookmarkStart in starts)
                {
                    try
                    {
                        var id = bookmarkStart.Id?.Value;
                        if (string.IsNullOrEmpty(id))
                        {
                            _logger.LogWarning($"书签缺少Id，名称：{name}");
                            continue;
                        }
                        if (!endById.TryGetValue(id, out var bookmarkEnd) || bookmarkEnd == null)
                        {
                            _logger.LogWarning($"书签结束标记不存在：{name}");
                            continue;
                        }

                        // 验证bookmarkEnd的父节点存在
                        if (bookmarkEnd.Parent == null)
                        {
                            _logger.LogWarning($"书签结束标记缺少父节点：{name}");
                            continue;
                        }

                        // 验证bookmarkStart的父节点存在
                        if (bookmarkStart.Parent == null)
                        {
                            _logger.LogWarning($"书签开始标记缺少父节点：{name}");
                            continue;
                        }

                        // 记录书签信息用于调试
                        _logger.LogInformation($"处理书签：{name}, ID:{id}, 新值长度:{newText?.Length ?? 0}");

                        // 删除书签之间的所有内容(支持跨段落)
                        DeleteContentBetweenBookmarks(bookmarkStart, bookmarkEnd);

                        // 创建新的Run并插入到bookmarkStart之后
                        var run = BuildRunFromString(newText ?? string.Empty);
                        
                        // 将新内容插入到bookmarkStart的父节点中
                        if (bookmarkStart.Parent != null)
                        {
                            bookmarkStart.Parent.InsertAfter(run, bookmarkStart);
                        }
                        else
                        {
                            _logger.LogWarning($"无法插入新内容,书签开始标记缺少父节点：{name}");
                            continue;
                        }

                        _logger.LogInformation($"成功替换书签：{name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"替换书签时出错：{name}");
                    }
                }
            }
        }

        /// <summary>
        /// 删除书签之间的内容(支持跨段落)
        /// </summary>
        private void DeleteContentBetweenBookmarks(BookmarkStart start, BookmarkEnd end)
        {
            var elementsToRemove = new List<OpenXmlElement>();
            
            // 检查是否在同一父节点下
            if (start.Parent == end.Parent)
            {
                // 同一父节点,直接删除兄弟节点
                var current = start.NextSibling();
                while (current != null && current != end)
                {
                    elementsToRemove.Add(current);
                    current = current.NextSibling();
                }
            }
            else
            {
                // 跨段落的书签,需要更复杂的处理
                // 找到共同的祖先(通常是Body)
                var commonAncestor = start.Ancestors().FirstOrDefault(a => a.Descendants().Contains(end));
                if (commonAncestor == null)
                {
                    _logger.LogWarning("无法找到书签的共同祖先");
                    return;
                }
                
                // 获取共同祖先下的所有元素
                var allElements = commonAncestor.Descendants().ToList();
                var startIndex = allElements.IndexOf(start);
                var endIndex = allElements.IndexOf(end);
                
                if (startIndex >= 0 && endIndex >= 0 && startIndex < endIndex)
                {
                    for (int i = startIndex + 1; i < endIndex; i++)
                    {
                        var element = allElements[i];
                        // 只删除Run元素(文本内容),不删除Paragraph(段落结构)和BookmarkStart/End
                        if (element is Run && !elementsToRemove.Contains(element))
                        {
                            elementsToRemove.Add(element);
                        }
                    }
                }
            }

            // 统一删除收集的元素
            foreach (var element in elementsToRemove)
            {
                try
                {
                    element.Remove();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"删除书签内容失败: {ex.Message}");
                }
            }
        }

        private Run BuildRunFromString(string value)
        {
            if (value == null) value = string.Empty;
            var run = new Run();
            var lines = value.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var text = new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve };
                run.Append(text);
                if (i < lines.Length - 1)
                {
                    run.Append(new Break());
                }
            }
            return run;
        }
        /// <summary>
        /// 获取书签之间的元素
        /// </summary>
        /// <param name="start">书签开始标记</param>
        /// <param name="end">书签结束标记</param>
        /// <returns>书签之间的元素列表</returns>
        private IEnumerable<OpenXmlElement> GetElementsBetweenBookmarks(OpenXmlElement start, OpenXmlElement end)
        {
            var elements = new List<OpenXmlElement>();
            var current = start.NextSibling();
            while (current != null && current != end)
            {
                elements.Add(current);
                current = current.NextSibling();
            }
            return elements;
        }
    }
}
