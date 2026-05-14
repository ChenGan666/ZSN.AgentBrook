using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model;
using ZSN.AI.Entity.Model.Enum;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AgentBrook.Plugins.Functions
{
    [ServiceDescription(typeof(KnowledgeBasePlugin), ServiceLifetime.Scoped)]
    [Description("知识库能力插件")]
    public class KnowledgeBasePlugin
    {

        private readonly ILogger<KnowledgeBasePlugin> _logger;
        private readonly IConfiguration _configuration;
        private readonly IImportKMSService _importKMSService;

        public KnowledgeBasePlugin(
            ILogger<KnowledgeBasePlugin> logger,
            IConfiguration configuration,
            IImportKMSService importKMSService)
        {
            _logger = logger;
            _configuration = configuration;
            _importKMSService = importKMSService;
        }
        /// <summary>
        /// 将数据保存到知识库
        /// </summary>
        /// <returns></returns>
        [KernelFunction]
        [Description("ZSN.AI.Plugins:将数据保存到知识库")]
        [return: Description("数据ID")]
        public async Task<object> Save(string knowledgeBaseId,string text,string? fileName,bool isQAValue = false,string delimiter = "")
        {
            // 基本参数校验
            if (string.IsNullOrWhiteSpace(knowledgeBaseId))
            {
                return new { Success = false, Message = "知识库KmsId不能为空" };
            }
            // 构建 ImportKMSTaskReq 对象
            var request = new ImportKMSTaskReq
            {
                KmsId = knowledgeBaseId,
                ImportType = ZSN.AI.Entity.Model.ImportType.Text,
                Url = string.Empty,
                Text = text ?? string.Empty,
                FilePath = string.Empty,
                FileName = fileName ?? string.Empty,
                IsQA = isQAValue,
                KnowledgeBaseFile = new KnowledgeBaseFileInfo()
            };
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return new { Success = false, Message = "文本导入类型需要提供Text参数" };
            }
            List<string> list = new List<string>();
            if (!delimiter.IsNullOrEmpty())
            {
                request.Text = request.Text + delimiter;
                request.Text.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(t =>
                {
                    if (!t.IsNullOrEmpty())
                    {
                        list.Add(t);
                    }
                });
            }
            else
            {
                list.Add(request.Text);
            }
            
            for (int i = 0; i < list.Count; i++)
            {
                request.Text = list[i].Trim();
                await doSaveToKnowledgeBase(request,$"{i}_{fileName}");
            }


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

            return response;
        }

        private async Task<string> doSaveToKnowledgeBase(ImportKMSTaskReq request,string fileName) {
            //判断文本长度，不能超过1000字符
            if (request.Text.Length > 1000)
            {
                //将文本保存到文件
                string tempDirName = _configuration.GetValue<string>("FileConversion:TempDirectory", "ZSN.Knowbase.FileConversions");
                string _tempDirectory = Path.Combine(Path.GetTempPath(), tempDirName);

                // 从配置中读取图片输出目录配置，如果未配置则使用默认值
                string _outputDirectory = Path.Combine(_tempDirectory, "TextMDOutputs");

                var new_file_name = fileName.IsNullOrEmpty() ? $"{Guid.NewGuid()}.txt" : fileName;
                //替换文件后缀为.txt
                new_file_name = Path.ChangeExtension(new_file_name, ".txt");

                string tempFilePath = System.IO.Path.Combine(_outputDirectory, "text_file");
                if (!System.IO.Directory.Exists(tempFilePath))
                {
                    System.IO.Directory.CreateDirectory(tempFilePath!);
                }
                tempFilePath = Path.Combine(tempFilePath, new_file_name);
                await System.IO.File.WriteAllTextAsync(tempFilePath, request.Text);

                request.FilePath = tempFilePath;
                request.ImportType = AI.Entity.Model.ImportType.File;
            }

            /*
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
            */

            //使用新版本知识库
            string KnowledgeBaseID = request.KmsId;
            string FileID = Guid.NewGuid().ToString();
            KnowledgeBaseFileInfo knowledgeBaseFile = new KnowledgeBaseFileInfo();
            knowledgeBaseFile.FileID = FileID;
            knowledgeBaseFile.KnowledgeBaseID = KnowledgeBaseID;
            knowledgeBaseFile.FileName = request.FileName;
            knowledgeBaseFile.FilePath = request.FilePath;
            knowledgeBaseFile.Type = request.FilePath;
            knowledgeBaseFile.ParserConfig = "{}";
            knowledgeBaseFile.CreateTime = DateTime.Now;
            knowledgeBaseFile.SystemStatus = 0;

            KnowledgeBaseFileInfoBussiness.Add(knowledgeBaseFile);

            // ============ 新实现：使用 FileToKnowledgeBaseJob ============
            // 从知识库配置中读取参数
            var knowledgeBaseConfig = KnowledgeBaseInfoBussiness.GetModel(KnowledgeBaseID);

            // 创建 FileToKnowledgeBaseConfig 配置
            FileToKnowledgeBaseConfig fileToKBConfig = new FileToKnowledgeBaseConfig
            {
                KnowledgeBaseId = KnowledgeBaseID,
                FilePath = knowledgeBaseFile.FilePath,
                FileName = knowledgeBaseFile.FileName,
                FileId = FileID,

                // 默认使用语义分块策略
                ChunkStrategy = "Semantic",

                // 从知识库配置中读取分块大小和重叠
                MaxChunkSize = knowledgeBaseConfig?.ParagraphSlice > 0 ? knowledgeBaseConfig.ParagraphSlice : 1000,
                ChunkOverlap = knowledgeBaseConfig?.OverlapSection > 0 ? knowledgeBaseConfig.OverlapSection : 200,

                // 从知识库配置中读取模型ID
                LargeModelId = knowledgeBaseConfig?.PreprocessModelID > 0 ? knowledgeBaseConfig.PreprocessModelID.ToString() : "",
                EmbeddingModelId = knowledgeBaseConfig?.VectorModelID > 0 ? knowledgeBaseConfig.VectorModelID.ToString() : "",

                // 启用实体和关系提取
                EnableEntityExtraction = true,
                EnableRelationExtraction = true,

                // 图片处理配置（从知识库配置中读取）
                EnableImageProcessing = knowledgeBaseConfig?.EnableImageProcessing ?? false,
                VisionModelID = knowledgeBaseConfig?.VisionModelID ?? 0
            };

            knowledgeBaseFile.SystemStatus = ImportKmsStatus.Loadding;
            KnowledgeBaseFileInfoBussiness.Update(knowledgeBaseFile);

            // 创建新的任务
            TaskInfo taskInfo = new TaskInfo();
            taskInfo.TaskID = Guid.NewGuid().ToString();
            taskInfo.TaskType = NodeType.NotNode_FileToKnowledgeBase;  // 使用新的Job类型
            taskInfo.TaskConfig = new TaskConfig();
            taskInfo.TaskConfig.NotNodeConfig = fileToKBConfig;
            taskInfo.TaskConfig.Data = new TaskData() { };
            taskInfo.LoopType = LoopType.NOLoop;
            taskInfo.RepeatValue = 1;
            taskInfo.RedoCount = 0;
            taskInfo.State = TaskState.Waiting;
            taskInfo.CreateTime = DateTime.Now;
            taskInfo.UpdateTime = DateTime.Now;

            TaskInfoBussiness.Add(taskInfo);

            return request.KmsId;
        }
    
        
    }
}
