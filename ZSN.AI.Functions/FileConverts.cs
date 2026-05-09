using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Functions.Common;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AI.Functions
{
    /// <summary>
    /// 图片文件信息
    /// </summary>
    public class ImageFileInfo
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }
    }

    public class ConvertToImageFiles
    {
        public string OriginalFileName { get; set; }
        public int ImageCount { get; set; }
        public List<ImageFileInfo> ImageFiles { get; set; }
    }
    public class MarkdownFileInfo
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        public string MarkdownContent { get; set; } = string.Empty;
    }
    public class ConvertToMarkdownFiles
    {
        public string OriginalFileName { get; set; }
        public int MarkdownCount { get; set; }
        public List<MarkdownFileInfo> MarkdownFiles { get; set; }

    }
    public class FileConverts
    {
        private readonly ILogger<FileConverts> _logger;
        private readonly FileToImageConverter _fileToImageConverter;
        private readonly IConfiguration _configuration;
        private readonly IChatService _chatService;

        private readonly string _tempDirectory;
        private string _outputDirectory;
        public FileConverts(ILogger<FileConverts> logger, IConfiguration configuration, IChatService chatService)
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


            _fileToImageConverter = new FileToImageConverter(_outputDirectory);
        }

        public string GetMD5HashFromFile(IFormFile file)
        {
            return _fileToImageConverter.GetMD5HashFromFile(file);
        }

        /// <summary>
        /// 文件转图片
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="sourceFileName"></param>
        /// <returns></returns>
        public ConvertToImageFiles ToImageFiles(string sourcePath, string sourceFileName,string sourceFileCode = "")
        {
            // 使用FileToImageConverter将文件转换为图片
            List<string> imagePaths = _fileToImageConverter.ConvertToImages(sourcePath, sourceFileCode);

            if (imagePaths == null || imagePaths.Count == 0)
            {
                // 转换失败，删除临时文件
                SafeDeleteFile(sourcePath);
            }

            // 收集图片文件信息
            List<ImageFileInfo> imageFiles = new List<ImageFileInfo>();
            foreach (var imagePath in imagePaths)
            {
                imageFiles.Add(new ImageFileInfo
                {
                    FileName = Path.GetFileName(imagePath),
                    FilePath = imagePath.Replace("\\", "/"),
                    FileSize = new System.IO.FileInfo(imagePath).Length
                });

                _logger.LogInformation($"生成图片文件: {imagePath}");
            }

            // 返回转换结果，包含图片文件信息
            return new ConvertToImageFiles
            {
                OriginalFileName = sourceFileName,
                ImageCount = imageFiles.Count,
                ImageFiles = imageFiles
            };
        }

        /// <summary>
        /// 安全删除文件，忽略可能的异常
        /// </summary>
        public void SafeDeleteFile(string filePath)
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
        /// 转换为Markdown文件
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="sourceFileName"></param>
        /// <returns>转换后的Markdown文件路径列表</returns>
        public async Task<ConvertToMarkdownFiles> ToMarkdownFilesAsync(string sourcePath, string sourceFileName,string sourceFileCode = "", bool returnMarkdown = false,string prompt = "")
        {
            var VLLLMConfig = _configuration.GetSection("VLLLMConfig");

            string fileMd5 = sourceFileCode.IsNullOrEmpty() ? _fileToImageConverter.GetFileMd5(sourcePath) : sourceFileCode;
            _outputDirectory = Path.Combine(_outputDirectory, fileMd5);
            //先将文件转换为图片
            ConvertToImageFiles imageFiles = ToImageFiles(sourcePath, sourceFileName, fileMd5);

            ConvertToMarkdownFiles markdownFiles = new ConvertToMarkdownFiles()
            {
                OriginalFileName = sourceFileName
            };
            markdownFiles.MarkdownFiles = new List<MarkdownFileInfo>();

            //将图片有视觉模型识别转换为Markdown
            if (imageFiles?.ImageFiles?.Count > 0)
            {
                foreach (var imageFile in imageFiles.ImageFiles)
                {
                    if (System.IO.File.Exists(imageFile.FilePath))
                    {
                        string markdownFileName = Path.GetFileNameWithoutExtension(imageFile.FilePath) + ".md";
                        string markdownFilePath = Path.Combine(_outputDirectory, $"{markdownFileName}");

                        string markdownContent = string.Empty;

                        // 性能优化：如果目标Markdown文件已存在,直接读取内容,不重新解析
                        if (System.IO.File.Exists(markdownFilePath))
                        {
                            markdownContent = await System.IO.File.ReadAllTextAsync(markdownFilePath);
                            _logger.LogInformation($"Markdown文件已存在,直接读取: {markdownFilePath}");
                        }
                        else
                        {
                            // 文件不存在,需要通过AI模型识别生成
                            LargeModelConfig modelConfig = new LargeModelConfig()
                            {
                                Id = "",
                                Model = new LargeModelInfo()
                                {
                                    Name = VLLLMConfig.GetValue<string>("Name", ""),
                                    ModelName = VLLLMConfig.GetValue<string>("ModelName", ""),
                                    ModelKey = VLLLMConfig.GetValue<string>("ModelKey", ""),
                                    ModelOrganizationName = VLLLMConfig.GetValue<string>("ModelOrganizationName", ""),
                                    EndPoint = VLLLMConfig.GetValue<string>("EndPoint", ""),
                                    MConfig = VLLLMConfig.GetValue<string>("MConfig", "")

                                },
                                Temperature = VLLLMConfig.GetValue<double>("Temperature", 0.75),
                                TopPCoefficient = VLLLMConfig.GetValue<double>("TopPCoefficient", 0.95),
                            };

                            prompt = prompt.IsNullOrEmpty()? VLLLMConfig.GetValue<string>("SystemPrompt", $"请将图片内容转写为 Markdown 文本,要求:\r\n\r\n" +
                                "1、保持原有结构和格式（标题、段落、列表等）。\r\n\r\n" +
                                "2、准确识别文字,转写为对应的 Markdown。\r\n\r\n" +
                                "3、表格 → 转为 Markdown 表格。\r\n\r\n" +
                                "4、图表/示意图 → 用文字详细描述,并可用表格补充关键数据。\r\n\r\n" +
                                "5、公式 → 用 LaTeX 表达($...$ 或 $$...$$)。\r\n\r\n" +
                                "6、非文字图片/插图 → 用 ![描述](#) 并详细描述内容。\r\n\r\n" +
                                "7、Markdown 语法必须正确,可直接渲染。"): prompt;

                            var chatHistory = new ChatHistory();
                            chatHistory.AddSystemMessage(prompt);

                            List<AttachmentItem> attachmentItems = new List<AttachmentItem>();
                            attachmentItems.Add(new AttachmentItem()
                            {
                                FilePath = imageFile.FilePath,
                                Type = "png",
                            });

                            chatHistory = await Node.Utils.Utils.AttachmentToChatHistoryAsync(attachmentItems, chatHistory);

                            var responseBuilder = new System.Text.StringBuilder();
                            await foreach (var chunk in _chatService.SendChatAsync(modelConfig, chatHistory))
                            {
                                responseBuilder.Append(chunk.ConvertToString());
                            }
                            markdownContent = responseBuilder.ToString();
                            // 保存生成的Markdown内容到文件
                            await System.IO.File.WriteAllTextAsync(markdownFilePath, markdownContent);
                            _logger.LogInformation($"AI识别完成,已保存Markdown文件: {markdownFilePath}");
                        }

                        // 返回Markdown文件路径
                        markdownFiles.MarkdownCount += 1;

                        markdownFiles.MarkdownFiles.Add(new MarkdownFileInfo()
                        {
                            FileName = markdownFileName,
                            FilePath = markdownFilePath,
                            FileSize = new System.IO.FileInfo(markdownFilePath).Length,
                            MarkdownContent = returnMarkdown ? markdownContent : string.Empty
                        });
                    }
                }
            }
            return markdownFiles;
        }
    }
}
