using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

using ZSN.Utils.Core.Helpers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Entity.Model.Enum;
using Elastic.Clients.Elasticsearch.Cluster;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI;
using System.IO;
using MongoDB.Bson.IO;
using System.Text.Json.Serialization;
using ZSN.AI.Entity.Model;
using ZSN.AI.KnowledgeBase.Interface;
using ZSN.AI.Core.Interface;
using ZSN.AI.Service.WebHelpers;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class KnowledgeBaseController: AdminBaseController
    {
        private readonly IVectorRepository _vectorRepository;
        private readonly IKnowledgeGraphService _knowledgeGraphService;
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly ILogger<KnowledgeBaseController> _logger;

        public KnowledgeBaseController(
            IVectorRepository vectorRepository,
            IKnowledgeGraphService knowledgeGraphService,
            IDocumentProcessingService documentProcessingService,
            ILogger<KnowledgeBaseController> logger)
        {
            _vectorRepository = vectorRepository;
            _knowledgeGraphService = knowledgeGraphService;
            _documentProcessingService = documentProcessingService;
            _logger = logger;
        }
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = KnowledgeBaseInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.KnowledgeBaseList = lst;
            return View();
        }

        public IActionResult filelist(string KnowledgeBaseID,int index = 1, int size = 10)
        {
            string where = " KnowledgeBaseID='"+ KnowledgeBaseID.SecureSQL()+"'";
            var lst = KnowledgeBaseFileInfoBussiness.GetListByPage(size, index, where, out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.KnowledgeBaseID = KnowledgeBaseID;
            ViewBag.KnowledgeBaseFileList = lst;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> KnowledgeBaseStatus(string mid, bool status)
        {
            var KnowledgeBase = KnowledgeBaseInfoBussiness.GetModel(mid);
            KnowledgeBase.SystemStatus = status ? ZSN.AI.Entity.KnowledgeBaseStatus.Normal : ZSN.AI.Entity.KnowledgeBaseStatus.Disabled;

            KnowledgeBaseInfoBussiness.Update(KnowledgeBase);
            return JsonMsg<string>.OK("更新成功");
        }

        public IActionResult Edit(string mid = "")
        {
            var KnowledgeBase = mid == "" ? new KnowledgeBaseInfo() : KnowledgeBaseInfoBussiness.GetModel(mid);
            var ModelList = LargeModelInfoBussiness.GetList(" SystemStatus = 0 ");
            ViewBag.KnowledgeBase = KnowledgeBase;

            ViewBag.TagClassList = BaseDictionaryInfoBussiness.GetAllChildList("标签分类", false, false);
            ViewBag.TagList = KnowledgeBaseTagInfoBussiness.GetList("");

            var tree = BaseDictionaryInfoBussiness.BuildTree(ViewBag.TagClassList, ViewBag.TagList, 24);

            ViewBag.TagClass = tree;

            ViewBag.PreprocessModeList = ModelList.FindAll(x=>x.TypeCode == AIModelType.Chat);
            ViewBag.VectorModelList = ModelList.FindAll(x => x.TypeCode == AIModelType.Embedding);

            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");
            return View();
        }
        [HttpPost]
        public JsonMsg<string> KnowledgeBaseSave(KnowledgeBaseInfo KnowledgeBase)
        {
            KnowledgeBase.MemberID = KnowledgeBase.MemberID.IsNullOrEmpty() ? KnowledgeBase.MemberID : "system";
            if (KnowledgeBase.KnowledgeBaseID.IsNullOrEmpty())
            {
                KnowledgeBase.KnowledgeBaseID = hashEncrypt.MD5System(Guid.NewGuid().ToString());
                KnowledgeBase.CreateTime = DateTime.Now;
                KnowledgeBaseInfoBussiness.Add(KnowledgeBase);
            }
            else
            {

                KnowledgeBaseInfoBussiness.Update(KnowledgeBase);
            }
            return JsonMsg<string>.OK("保存成功");
        }

        public JsonMsg<string> KnowledgeBaseDel(string mid)
        {
            try
            {
                _logger.LogInformation("开始删除知识库: KnowledgeBaseID={KnowledgeBaseID}", mid);

                // 使用新的知识库删除服务
                var deleteResult = System.Threading.Tasks.Task.Run(async () =>
                {
                    return await _documentProcessingService.DeleteKnowledgeBaseAsync(mid);
                }).Result;

                if (!deleteResult.Success)
                {
                    _logger.LogWarning("删除知识库向量、图谱数据失败: {ErrorMessage}", deleteResult.ErrorMessage);
                }
                else
                {
                    _logger.LogInformation("删除知识库向量、图谱数据成功: Documents={DeletedDocuments}, Vectors={DeletedVectors}, Entities={DeletedEntities}, Relations={DeletedRelations}, Files={DeletedFiles}",
                        deleteResult.DeletedDocuments, deleteResult.DeletedVectors, deleteResult.DeletedEntities, deleteResult.DeletedRelations, deleteResult.DeletedFiles);
                }

                // 删除知识库中的所有文件记录
                var fileList = KnowledgeBaseFileInfoBussiness.GetList($"KnowledgeBaseID='{mid.SecureSQL()}'");
                if (fileList != null && fileList.Count > 0)
                {
                    foreach (var file in fileList)
                    {
                        if (!file.FileID.IsNullOrEmpty())
                        {
                            // 删除文件在旧系统中的分块数据
                            try
                            {
                                KnowledgeBaseFileChunkInfoBussiness.Delete(file.FileID, mid);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "删除旧分块数据失败: FileID={FileID}", file.FileID);
                            }

                            // 删除文件记录
                            try
                            {
                                KnowledgeBaseFileInfoBussiness.DeleteList(file.FileID);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "删除文件记录失败: FileID={FileID}", file.FileID);
                            }
                        }
                    }
                }

                // 删除知识库记录
                KnowledgeBaseInfoBussiness.DeleteList(mid);

                _logger.LogInformation("知识库删除完成: KnowledgeBaseID={KnowledgeBaseID}", mid);

                return JsonMsg<string>.OK($"删除成功，已删除 {deleteResult.DeletedDocuments} 个文档、{deleteResult.DeletedVectors} 个向量、{deleteResult.DeletedEntities} 个实体、{deleteResult.DeletedRelations} 个关系");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除知识库失败: KnowledgeBaseID={KnowledgeBaseID}", mid);
                return JsonMsg<string>.Error($"删除失败: {ex.Message}", ErrorCode.ServerError);
            }
        }

        public IActionResult EditFile(string KnowledgeBaseID,string FileID = "", int index = 1, int size = 10) {
            FileID = FileID.SecureSQL();
            if (!FileID.IsNullOrEmpty())
            {
                //string where = " id like 'd="+ FileID+"%'";
                //var KnowledgeBaseFileChunkList = KnowledgeBaseFileChunkInfoBussiness.GetListByPage(KnowledgeBaseID,size, index, where, out int pagetotal, out int total);

                ViewBag.Index = index;
                ViewBag.Size = size;
                ViewBag.Total = 0;
                ViewBag.FileID = FileID;
                ViewBag.KnowledgeBaseID = KnowledgeBaseID;
                //ViewBag.KnowledgeBaseFileChunkList = KnowledgeBaseFileChunkList;
                return View();
            }
            else
            {
                ViewBag.FileID = FileID;
                ViewBag.KnowledgeBaseID = KnowledgeBaseID;
                return View();
            }
        }
        public JsonMsg<string> AddFile(string fileCode,string fileName,string KnowledgeBaseID) {
            if (!fileCode.IsNullOrEmpty() && !fileName.IsNullOrEmpty() && !KnowledgeBaseID.IsNullOrEmpty())
            {
                KnowledgeBaseFileInfo knowledgeBaseFileInfo = KnowledgeBaseFileInfoBussiness.GetModel(fileCode);
                FilesInfo fileInfo = FilesInfoBussiness.GetModel(fileCode);
                if (fileInfo != null && knowledgeBaseFileInfo==null)
                {
                    KnowledgeBaseFileInfo knowledgeBaseFile = new KnowledgeBaseFileInfo();
                    knowledgeBaseFile.FileID = fileCode;
                    knowledgeBaseFile.KnowledgeBaseID = KnowledgeBaseID;
                    knowledgeBaseFile.FileName = fileInfo.FOriginName;
                    knowledgeBaseFile.FilePath = fileInfo.FFilePath+ fileInfo.FName;
                    knowledgeBaseFile.Type = fileInfo.FType;
                    knowledgeBaseFile.ParserConfig =  "{}";
                    knowledgeBaseFile.CreateTime = DateTime.Now;
                    knowledgeBaseFile.SystemStatus = 0;

                    KnowledgeBaseFileInfoBussiness.Add(knowledgeBaseFile);
                }
                return JsonMsg<string>.OK("成功");
            }
            else {
                return JsonMsg<string>.Error("参数错误",ErrorCode.DataEmpty);
            }
        }
        public JsonMsg<string> KnowledgeBaseFileDel(string mid, string KnowledgeBaseID)
        {
            try
            {
                _logger.LogInformation("开始删除文件: FileID={FileID}, KnowledgeBaseID={KnowledgeBaseID}", mid, KnowledgeBaseID);

                // 使用新的文档删除服务
                var deleteResult = System.Threading.Tasks.Task.Run(async () =>
                {
                    return await _documentProcessingService.DeleteByFileIdAsync(mid);
                }).Result;

                if (!deleteResult.Success)
                {
                    _logger.LogWarning("删除文件向量、图谱数据失败: {ErrorMessage}", deleteResult.ErrorMessage);
                }
                else
                {
                    _logger.LogInformation("删除文件向量、图谱数据成功: Vectors={DeletedVectors}, Entities={DeletedEntities}, Relations={DeletedRelations}",
                        deleteResult.DeletedVectors, deleteResult.DeletedEntities, deleteResult.DeletedRelations);
                }

                // 删除原分块数据
                KnowledgeBaseFileChunkInfoBussiness.Delete(mid, KnowledgeBaseID);

                // 删除文件记录
                KnowledgeBaseFileInfoBussiness.DeleteList(mid);

                return JsonMsg<string>.OK($"删除成功，已删除 {deleteResult.DeletedVectors} 个向量、{deleteResult.DeletedEntities} 个实体、{deleteResult.DeletedRelations} 个关系");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除文件失败: FileID={FileID}", mid);
                return JsonMsg<string>.Error($"删除失败: {ex.Message}", ErrorCode.ServerError);
            }
        }
        public JsonMsg<string> KnowledgeBaseFileToJob(string KnowledgeBaseID, string FileID)
        {
            if (!KnowledgeBaseID.IsNullOrEmpty() && !FileID.IsNullOrEmpty())
            {
                //删除原分块数据
                KnowledgeBaseFileChunkInfoBussiness.Delete(FileID, KnowledgeBaseID);

                KnowledgeBaseFileInfo knowledgeBaseFile = KnowledgeBaseFileInfoBussiness.GetModel(FileID);

                // ============ 原有实现（已屏蔽）============
                #if false
                ImportType importType = ImportKMSCommon.GetImportType(knowledgeBaseFile.Type);


                ImportKMSTaskReq importKMSTask = new ImportKMSTaskReq();
                importKMSTask.KmsId = KnowledgeBaseID;
                importKMSTask.IsQA = false;
                importKMSTask.ImportType = importType;
                importKMSTask.FileName = knowledgeBaseFile.FileName;
                importKMSTask.FilePath = knowledgeBaseFile.FilePath;
                importKMSTask.KnowledgeBaseFile = knowledgeBaseFile;

                TaskInfo taskInfo = new TaskInfo();
                taskInfo.TaskType = NodeType.NotNode_FileChunk;
                taskInfo.TaskConfig = new TaskConfig();
                taskInfo.TaskConfig.NotNodeConfig = new FileChunkConfig() { KnowledgeBaseID = KnowledgeBaseID, FileID = FileID, ImportKMSTask = importKMSTask };
                taskInfo.TaskConfig.Data = new TaskData() { };
                taskInfo.LoopType = LoopType.NOLoop;
                taskInfo.RepeatValue = 1;
                taskInfo.RedoCount = 0;
                taskInfo.CreateTime = DateTime.Now;
                taskInfo.UpdateTime = DateTime.Now;

                TaskInfoBussiness.Add(taskInfo);
                #endif
                // ============ 原有实现结束 ============


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
                    EnableRelationExtraction = true
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

                
                // ============ 新实现结束 ============

                return JsonMsg<string>.OK("知识库文件导入任务已添加，将在后台处理");
            }
            else
            {
                return JsonMsg<string>.Error("参数错误", ErrorCode.DataEmpty);
            }
        }

        /// <summary>
        /// 获取文件的向量分块数据
        /// </summary>
        [HttpGet]
        public JsonMsg<object> GetFileChunks(string fileID, int index = 1, int size = 10)
        {
            try
            {
                if (fileID.IsNullOrEmpty())
                {
                    return JsonMsg<object>.Error("文件ID不能为空", ErrorCode.DataEmpty);
                }

                // 计算skip值 (index从1开始)
                int skip = (index - 1) * size;

                // 分页获取文档块数据
                var result = System.Threading.Tasks.Task.Run(async () =>
                {
                    return await _vectorRepository.GetDocumentChunksAsync(fileID, skip, size);
                }).Result;

                if (result.TotalCount == 0)
                {
                    return JsonMsg<object>.OK(new { chunks = new List<object>(), total = 0, message = "该文件尚未处理或没有分块数据" });
                }

                // 构建返回数据
                var chunksData = result.Chunks.Select(c => new
                {
                    id = c.Id,
                    content = c.Content,
                    sequenceNumber = c.Metadata.TryGetValue("sequence_number", out var seqNum) ? seqNum : 0,
                    tokenCount = c.Metadata.TryGetValue("token_count", out var tokenCnt) ? tokenCnt : 0,
                    documentId = c.Metadata.TryGetValue("document_id", out var docId) ? docId : string.Empty
                }).ToList();

                return JsonMsg<object>.OK(new
                {
                    chunks = chunksData,
                    total = result.TotalCount,
                    index = index,
                    size = size,
                    fileID = fileID
                });
            }
            catch (Exception ex)
            {
                return JsonMsg<object>.Error($"获取文件分块失败: {ex.Message}", ErrorCode.ServerError);
            }
        }

        /// <summary>
        /// 获取知识库统计信息
        /// </summary>
        [HttpGet]
        public JsonMsg<object> GetKnowledgeBaseStats(string knowledgeBaseID, string? fileID = null)
        {
            try
            {
                if (knowledgeBaseID.IsNullOrEmpty())
                {
                    return JsonMsg<object>.Error("知识库ID不能为空", ErrorCode.DataEmpty);
                }

                int totalChunks = 0;
                int totalFiles = 0;

                // 如果指定了fileID，只统计该文件
                if (!fileID.IsNullOrEmpty())
                {
                    var file = KnowledgeBaseFileInfoBussiness.GetModel(fileID);
                    if (file != null)
                    {
                        totalFiles = 1;
                        try
                        {
                            var chunkCount = System.Threading.Tasks.Task.Run(async () =>
                            {
                                return await _vectorRepository.GetDocumentChunkCountAsync(fileID);
                            }).Result;
                            totalChunks = chunkCount;
                        }
                        catch (Exception ex)
                        {
                            DefaultLogService.AddOperationLog(ErrorId, $"统计文件分块数失败: {ex.Message}");
                        }
                    }

                    // 获取指定文档的图谱统计信息
                    GraphStatistics? graphStats = null;
                    try
                    {
                        graphStats = System.Threading.Tasks.Task.Run(async () =>
                        {
                            return await _knowledgeGraphService.GetDocumentStatisticsAsync(fileID, knowledgeBaseID);
                        }).Result;
                    }
                    catch (Exception ex)
                    {
                        DefaultLogService.AddOperationLog(ErrorId, $"获取文档图谱统计失败: {ex.Message}");
                    }

                    var stats = new
                    {
                        knowledgeBaseID = knowledgeBaseID,
                        fileID = fileID,
                        totalFiles = totalFiles,
                        totalChunks = totalChunks,
                        graphStatistics = graphStats != null ? new
                        {
                            totalEntities = graphStats.TotalEntities,
                            totalRelations = graphStats.TotalRelations,
                            entityCountsByType = graphStats.EntityCountsByType,
                            relationCountsByType = graphStats.RelationCountsByType
                        } : null
                    };

                    return JsonMsg<object>.OK(stats);
                }
                else
                {
                    // 获取知识库中的所有文件
                    var fileList = KnowledgeBaseFileInfoBussiness.GetList($"KnowledgeBaseID='{knowledgeBaseID.SecureSQL()}'");
                    totalFiles = fileList?.Count ?? 0;

                    // 统计总分块数
                    if (fileList != null && fileList.Count > 0)
                    {
                        foreach (var file in fileList)
                        {
                            if (!file.FileID.IsNullOrEmpty())
                            {
                                try
                                {
                                    var chunkCount = System.Threading.Tasks.Task.Run(async () =>
                                    {
                                        return await _vectorRepository.GetDocumentChunkCountAsync(file.FileID);
                                    }).Result;

                                    totalChunks += chunkCount;
                                }
                                catch (Exception ex)
                                {
                                    DefaultLogService.AddOperationLog(ErrorId, $"统计文件分块数失败: {ex.Message}");
                                }
                            }
                        }
                    }

                    // 获取整个知识库的图谱统计信息
                    GraphStatistics? graphStats = null;
                    try
                    {
                        graphStats = System.Threading.Tasks.Task.Run(async () =>
                        {
                            return await _knowledgeGraphService.GetStatisticsAsync(knowledgeBaseID);
                        }).Result;
                    }
                    catch (Exception ex)
                    {
                        DefaultLogService.AddOperationLog(ErrorId, $"获取图谱统计失败: {ex.Message}");
                    }

                    var stats = new
                    {
                        knowledgeBaseID = knowledgeBaseID,
                        totalFiles = totalFiles,
                        totalChunks = totalChunks,
                        graphStatistics = graphStats != null ? new
                        {
                            totalEntities = graphStats.TotalEntities,
                            totalRelations = graphStats.TotalRelations,
                            entityCountsByType = graphStats.EntityCountsByType,
                            relationCountsByType = graphStats.RelationCountsByType
                        } : null
                    };

                    return JsonMsg<object>.OK(stats);
                }
            }
            catch (Exception ex)
            {
                return JsonMsg<object>.Error($"获取知识库统计失败: {ex.Message}", ErrorCode.ServerError);
            }
        }

        /// <summary>
        /// 清理没有 source_document_id 的旧数据
        /// </summary>
        [HttpPost]
        public JsonMsg<object> CleanupOldGraphData()
        {
            try
            {
                var deletedCount = System.Threading.Tasks.Task.Run(async () =>
                {
                    return await _knowledgeGraphService.CleanupOldDataWithoutDocumentIdAsync();
                }).Result;

                if (deletedCount < 0)
                {
                    return JsonMsg<object>.Error("清理旧数据失败", ErrorCode.ServerError);
                }

                return JsonMsg<object>.OK(new { deletedCount = deletedCount, message = $"成功清理 {deletedCount} 条旧数据" });
            }
            catch (Exception ex)
            {
                return JsonMsg<object>.Error($"清理旧数据失败: {ex.Message}", ErrorCode.ServerError);
            }
        }

        /// <summary>
        /// 获取文件的知识图谱可视化数据
        /// </summary>
        [HttpGet]
        public async Task<JsonMsg<object>> GetFileGraphData(string fileID, string knowledgeBaseID)
        {
            var startTime = DateTime.Now;
            try
            {
                _logger.LogInformation("[图谱加载] 开始获取文件图谱数据 - FileID: {FileID}, KnowledgeBaseID: {KnowledgeBaseID}", fileID, knowledgeBaseID);

                if (fileID.IsNullOrEmpty())
                {
                    _logger.LogWarning("[图谱加载] 文件ID为空");
                    return JsonMsg<object>.Error("文件ID不能为空", ErrorCode.DataEmpty);
                }

                if (knowledgeBaseID.IsNullOrEmpty())
                {
                    _logger.LogWarning("[图谱加载] 知识库ID为空");
                    return JsonMsg<object>.Error("知识库ID不能为空", ErrorCode.DataEmpty);
                }

                _logger.LogInformation("[图谱加载] 调用知识图谱服务...");
                var graphData = await _knowledgeGraphService.GetDocumentGraphDataAsync(fileID, knowledgeBaseID);

                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                _logger.LogInformation("[图谱加载] 知识图谱服务返回，耗时: {Elapsed}ms", elapsed);

                if (graphData == null)
                {
                    _logger.LogWarning("[图谱加载] 图谱数据为null");
                    return JsonMsg<object>.Error("获取图谱数据失败", ErrorCode.ServerError);
                }

                // 转换为前端需要的格式
                var nodes = graphData.Nodes.Select(n => new
                {
                    id = n.Id,
                    name = n.Name,
                    category = n.Type,
                    value = n.Size,
                    symbolSize = n.Size ?? 20,
                    itemStyle = new
                    {
                        color = GetColorByType(n.Type)
                    }
                }).ToList();

                var links = graphData.Links.Select(l => new
                {
                    source = l.Source,
                    target = l.Target,
                    name = l.RelationType,
                    value = l.RelationType,
                    lineStyle = new
                    {
                        color = "#999"
                    }
                }).ToList();

                var categories = graphData.Nodes.Select(n => n.Type).Distinct().Select(t => new
                {
                    name = t
                }).ToList();

                _logger.LogInformation("[图谱加载] 数据转换完成 - 节点数: {NodeCount}, 关系数: {LinkCount}, 类别数: {CategoryCount}", nodes.Count, links.Count, categories.Count);

                var result = new
                {
                    nodes = nodes,
                    links = links,
                    categories = categories,
                    nodeCount = nodes.Count,
                    linkCount = links.Count
                };

                var totalElapsed = (DateTime.Now - startTime).TotalMilliseconds;
                _logger.LogInformation("[图谱加载] 请求完成，总耗时: {TotalElapsed}ms", totalElapsed);

                return JsonMsg<object>.OK(result);
            }
            catch (Exception ex)
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                _logger.LogError(ex, "[图谱加载] 获取图谱数据失败，耗时: {Elapsed}ms", elapsed);
                return JsonMsg<object>.Error($"获取图谱数据失败: {ex.Message}", ErrorCode.ServerError);
            }
        }

        /// <summary>
        /// 根据实体类型获取颜色
        /// </summary>
        private string GetColorByType(string type)
        {
            return type switch
            {
                "PERSON" => "#c23531",
                "ORG" => "#2f4554",
                "LOC" => "#61a0a8",
                "DATE" => "#d48265",
                "EVENT" => "#91c7ae",
                "WORK" => "#749f83",
                "LAW" => "#ca8622",
                "TERM" => "#bda29a",
                _ => "#546570"
            };
        }
    }
}
