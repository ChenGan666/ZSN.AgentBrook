using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;
using ZSN.AI.BLL;
using ZSN.AI.Service.WebHelpers;
using ZSN.AI.KnowledgeBase.Services;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AgentBrook.AutoJob
{
    /// <summary>
    /// 文件导入知识库Job
    /// 使用ZSN.AI.KnowledgeBase项目进行文档处理
    /// </summary>
    [DisallowConcurrentExecution]
    public class FileToKnowledgeBaseJob : JobBase, IJob
    {
        private readonly IDocumentProcessingService _documentProcessingService;

        public FileToKnowledgeBaseJob(IDocumentProcessingService documentProcessingService)
        {
            _documentProcessingService = documentProcessingService;
        }

        Task IJob.Execute(IJobExecutionContext context)
        {
            var res = Auto();
            return res;
        }

        /// <summary>
        /// 自动执行任务
        /// 每5秒轮动一次，处理完一轮后继续下一轮
        /// </summary>
        public async Task<int> Auto()
        {
            int num = 0;
            try
            {
                // 获取需要AI执行的任务
                List<NodeType> nodeTypes = new List<NodeType>() { NodeType.NotNode_FileToKnowledgeBase };
                List<TaskInfo> tasks = TaskInfoBussiness.GetList(0, nodeTypes, DateTime.Now, 1, 100);

                if (tasks != null && tasks.Count > 0)
                {
                    foreach (var task in tasks)
                    {
                        if (task != null)
                        {
                            num++;
                            await this.FileToKnowledgeBaseWorkerAsync_Node(task);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                num = -1;
                DefaultLogService.AddOperationLog(ErrorId, e.Message);
            }
            return await Task.FromResult(num);
        }

        /// <summary>
        /// 处理单个文件导入知识库任务
        /// </summary>
        private async Task FileToKnowledgeBaseWorkerAsync_Node(TaskInfo task)
        {
            TaskConfig taskConfig = task.TaskConfig;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (taskConfig.NotNodeConfig != null)
                {
                    // 反序列化配置
                    FileToKnowledgeBaseConfig config = JsonConvert.DeserializeObject<FileToKnowledgeBaseConfig>(
                        JsonConvert.SerializeObject(taskConfig.NotNodeConfig));

                    // 验证必要参数
                    if (string.IsNullOrEmpty(config.KnowledgeBaseId))
                    {
                        throw new ArgumentException("知识库ID不能为空");
                    }

                    if (string.IsNullOrEmpty(config.FilePath) && string.IsNullOrEmpty(config.Text) && string.IsNullOrEmpty(config.Url))
                    {
                        throw new ArgumentException("必须提供文件路径、文本内容或URL地址中的一个");
                    }

                    // 创建处理结果
                    var result = new FileToKnowledgeBaseResult
                    {
                        Success = false,
                        DocumentId = string.Empty
                    };

                    // 准备文档处理请求
                    DocumentProcessingRequest? request = null;

                    // 根据输入类型创建请求
                    if (!string.IsNullOrEmpty(config.FilePath))
                    {
                        // 从文件路径处理
                        request = new DocumentProcessingRequest
                        {
                            DocumentId = config.FileId,  // 使用文件ID作为文档ID
                            FileName = config.FileName ?? System.IO.Path.GetFileName(config.FilePath),
                            FilePath = config.FilePath,
                            KnowledgeBaseId = config.KnowledgeBaseId,
                            Options = CreateProcessingOptions(config)
                        };

                        // 调用新的方法签名
                        var processingService = _documentProcessingService as DocumentProcessingService;
                        if (processingService != null)
                        {
                            var processingResult = await processingService.ProcessDocumentFromFileAsync(
                                config.FileId,
                                config.FilePath,
                                config.KnowledgeBaseId,
                                CreateProcessingOptions(config)
                            );

                            stopwatch.Stop();

                            // 填充处理结果
                            result.DocumentId = processingResult.DocumentId;
                            result.ChunkCount = processingResult.ChunkCount;
                            result.EntityCount = processingResult.EntityCount;
                            result.RelationCount = processingResult.RelationCount;
                            result.TokensConsumed = processingResult.TotalTokens;
                            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                            if (processingResult.Status == ProcessingStatus.Completed)
                            {
                                result.Success = true;
                                result.Details = $"文档处理成功：分块数={result.ChunkCount}, 实体数={result.EntityCount}, 关系数={result.RelationCount}";
                                UpdateFileStatus(config.FileId, ImportKmsStatus.Success, result.ChunkCount);
                            }
                            else
                            {
                                result.Success = false;
                                result.ErrorMessage = processingResult.ErrorMessage ?? "文档处理失败";
                                UpdateFileStatus(config.FileId, ImportKmsStatus.Fail, 0);
                            }

                            task.Results = new Results() { Data = result };
                            task.State = result.Success ? TaskState.Completed : TaskState.Failure;
                            task.UpdateTime = DateTime.Now;
                            TaskInfoBussiness.Update(task);
                            return;
                        }
                    }
                    else if (!string.IsNullOrEmpty(config.Text))
                    {
                        // 从文本内容处理
                        request = new DocumentProcessingRequest
                        {
                            DocumentId = config.FileId,  // 使用文件ID作为文档ID
                            FileName = config.FileName ?? "text_input.txt",
                            Content = config.Text,
                            KnowledgeBaseId = config.KnowledgeBaseId,
                            Options = CreateProcessingOptions(config)
                        };

                        // 调用新的方法签名
                        var processingService = _documentProcessingService as DocumentProcessingService;
                        if (processingService != null)
                        {
                            var processingResult = await processingService.ProcessDocumentFromTextAsync(
                                config.FileId,
                                config.FileName ?? "text_input.txt",
                                config.Text,
                                config.KnowledgeBaseId,
                                CreateProcessingOptions(config)
                            );

                            stopwatch.Stop();

                            // 填充处理结果
                            result.DocumentId = processingResult.DocumentId;
                            result.ChunkCount = processingResult.ChunkCount;
                            result.EntityCount = processingResult.EntityCount;
                            result.RelationCount = processingResult.RelationCount;
                            result.TokensConsumed = processingResult.TotalTokens;
                            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                            if (processingResult.Status == ProcessingStatus.Completed)
                            {
                                result.Success = true;
                                result.Details = $"文档处理成功：分块数={result.ChunkCount}, 实体数={result.EntityCount}, 关系数={result.RelationCount}";
                                UpdateFileStatus(config.FileId, ImportKmsStatus.Success, result.ChunkCount);
                            }
                            else
                            {
                                result.Success = false;
                                result.ErrorMessage = processingResult.ErrorMessage ?? "文档处理失败";
                                UpdateFileStatus(config.FileId, ImportKmsStatus.Fail, 0);
                            }

                            task.Results = new Results() { Data = result };
                            task.State = result.Success ? TaskState.Completed : TaskState.Failure;
                            task.UpdateTime = DateTime.Now;
                            TaskInfoBussiness.Update(task);
                            return;
                        }
                    }
                    else if (!string.IsNullOrEmpty(config.Url))
                    {
                        // 从URL处理（需要先下载文件）
                        // 这里可以添加URL下载逻辑
                        throw new NotImplementedException("URL导入功能暂未实现");
                    }

                    if (request != null)
                    {
                        UpdateFileStatus(config.FileId, ImportKmsStatus.Loadding, 0);
                        // 执行文档处理
                        var processingResult = await _documentProcessingService.ProcessDocumentAsync(request);

                        stopwatch.Stop();

                        // 填充处理结果
                        result.DocumentId = processingResult.DocumentId;
                        result.ChunkCount = processingResult.ChunkCount;
                        result.EntityCount = processingResult.EntityCount;
                        result.RelationCount = processingResult.RelationCount;
                        result.TokensConsumed = processingResult.TotalTokens;
                        result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;


                        if (processingResult.Status == ProcessingStatus.Completed)
                        {
                            result.Success = true;
                            result.Details = $"文档处理成功：分块数={result.ChunkCount}, 实体数={result.EntityCount}, 关系数={result.RelationCount}";

                            // 更新文件状态为成功
                            UpdateFileStatus(config.FileId, ImportKmsStatus.Success, result.ChunkCount);
                        }
                        else
                        {
                            result.Success = false;
                            result.ErrorMessage = processingResult.ErrorMessage ?? "文档处理失败";

                            // 更新文件状态为失败
                            UpdateFileStatus(config.FileId, ImportKmsStatus.Fail, 0);
                        }
                    }

                    // 更新任务结果
                    task.Results = new Results() { Data = result };
                    task.State = result.Success ? TaskState.Completed : TaskState.Failure;
                }
                else
                {
                    throw new ArgumentException("任务配置为空");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // 创建失败结果
                var errorResult = new FileToKnowledgeBaseResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };

                task.Results = new Results() { Data = errorResult };
                task.State = TaskState.Failure;

                // 更新文件状态为失败
                try
                {
                    if (taskConfig?.NotNodeConfig != null)
                    {
                        var config = JsonConvert.DeserializeObject<FileToKnowledgeBaseConfig>(
                            JsonConvert.SerializeObject(taskConfig.NotNodeConfig));
                        if (config != null && !string.IsNullOrEmpty(config.FileId))
                        {
                            UpdateFileStatus(config.FileId, ImportKmsStatus.Fail, 0);
                        }
                    }
                }
                catch (Exception updateEx)
                {
                    DefaultLogService.AddOperationLog(ErrorId, $"更新文件状态失败: {updateEx.Message}");
                }

                DefaultLogService.AddOperationLog(ErrorId, $"文件导入知识库失败: {ex.Message}");
            }

            task.UpdateTime = DateTime.Now;
            TaskInfoBussiness.Update(task);
        }

        /// <summary>
        /// 创建文档处理选项
        /// </summary>
        private DocumentProcessingOptions CreateProcessingOptions(FileToKnowledgeBaseConfig config)
        {
            var options = new DocumentProcessingOptions();

            // 解析分块策略
            if (!string.IsNullOrEmpty(config.ChunkStrategy))
            {
                switch (config.ChunkStrategy.ToLower())
                {
                    case "semantic":
                    case "semanticboundary":
                        options.ChunkingStrategy = ChunkingStrategy.SemanticBoundary;
                        break;
                    case "fixed":
                    case "hardcutoff":
                        options.ChunkingStrategy = ChunkingStrategy.HardCutoff;
                        break;
                    case "recursive":
                    case "topic":
                    case "topicsegmentation":
                        options.ChunkingStrategy = ChunkingStrategy.TopicSegmentation;
                        break;
                    case "entityaware":
                    case "entity":
                        options.ChunkingStrategy = ChunkingStrategy.EntityAware;
                        break;
                    default:
                        options.ChunkingStrategy = ChunkingStrategy.SemanticBoundary;
                        break;
                }
            }

            // 设置分块参数
            options.MaxChunkSize = config.MaxChunkSize > 0 ? config.MaxChunkSize : 500;
            options.ChunkOverlap = config.ChunkOverlap > 0 ? config.ChunkOverlap : 50;

            // 设置实体和关系提取
            options.ExtractEntities = config.EnableEntityExtraction;
            options.ExtractRelations = config.EnableRelationExtraction;

            // 设置向量化
            options.EnableEmbedding = true;

            // 设置实体提取模型ID（如果提供）
            if (!string.IsNullOrEmpty(config.LargeModelId) && int.TryParse(config.LargeModelId, out int modelId))
            {
                options.EntityModelId = modelId;
            }

            return options;
        }

        /// <summary>
        /// 更新文件处理状态
        /// </summary>
        private void UpdateFileStatus(string fileId, ImportKmsStatus status, int dataCount)
        {
            try
            {
                if (string.IsNullOrEmpty(fileId))
                {
                    return;
                }

                // 获取文件信息
                var fileInfo = KnowledgeBaseFileInfoBussiness.GetModel(fileId);
                if (fileInfo != null)
                {
                    // 更新状态和数据计数
                    fileInfo.SystemStatus = status;
                    fileInfo.DataCount = dataCount;
                    KnowledgeBaseFileInfoBussiness.Update(fileInfo);

                    DefaultLogService.AddOperationLog(ErrorId, $"文件状态已更新: FileID={fileId}, Status={status}, DataCount={dataCount}");
                }
                else
                {
                    DefaultLogService.AddOperationLog(ErrorId, $"文件信息未找到: FileID={fileId}");
                }
            }
            catch (Exception ex)
            {
                DefaultLogService.AddOperationLog(ErrorId, $"更新文件状态失败: FileID={fileId}, Error={ex.Message}");
            }
        }
    }
}
