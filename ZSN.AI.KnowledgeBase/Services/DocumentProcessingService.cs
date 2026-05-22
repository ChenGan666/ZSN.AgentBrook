using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using  ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.KnowledgeBase.Interface;
using ZSN.AI.KnowledgeBase.Models;
using ZSN.AI.Core.Interface;


namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 文档处理服务实现
    /// </summary>
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly ISemanticChunkerService _chunkerService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IKnowledgeGraphService _knowledgeGraphService;
        private readonly IVectorRepository _vectorRepository;
        private readonly IImageRepository? _imageRepository;
        private readonly IImageStorageService? _imageStorageService;
        private readonly IImageProcessingPipeline? _imageProcessingPipeline;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentProcessingService> _logger;
        private readonly string _documentRootPath;
        private readonly bool _enableFileSync;
        private readonly Dictionary<string, DocumentProcessingStatus> _processingStatuses;

        public DocumentProcessingService(
            ISemanticChunkerService chunkerService,
            IEmbeddingService embeddingService,
            IKnowledgeGraphService knowledgeGraphService,
            IVectorRepository vectorRepository,
            IConfiguration configuration,
            ILogger<DocumentProcessingService> logger,
            IImageProcessingPipeline? imageProcessingPipeline = null,
            IImageRepository? imageRepository = null,
            IImageStorageService? imageStorageService = null)
        {
            _chunkerService = chunkerService;
            _embeddingService = embeddingService;
            _knowledgeGraphService = knowledgeGraphService;
            _vectorRepository = vectorRepository;
            _imageProcessingPipeline = imageProcessingPipeline;
            _imageRepository = imageRepository;
            _imageStorageService = imageStorageService;
            _configuration = configuration;
            _logger = logger;
            _processingStatuses = new Dictionary<string, DocumentProcessingStatus>();

            // 读取配置
            _documentRootPath = _configuration["DocumentStorage:RootPath"] ?? "./Documents";
            _enableFileSync = _configuration.GetValue<bool>("DocumentStorage:EnableFileSync", true);

            // 创建根目录
            if (_configuration.GetValue<bool>("DocumentStorage:AutoCreateDirectories", true))
            {
                Directory.CreateDirectory(_documentRootPath);
            }

            //_logger.LogInformation("DocumentProcessingService 初始化完成 - 根路径: {Path}, 文件同步: {Enabled}",_documentRootPath, _enableFileSync);
        }

        public async Task<DocumentProcessingResult> ProcessDocumentAsync(
            DocumentProcessingRequest request,
            IProgress<DocumentProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new DocumentProcessingResult
            {
                DocumentId = request.DocumentId ?? Guid.NewGuid().ToString(),
                FileName = request.FileName,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // 初始化状态
                UpdateStatus(result.DocumentId, ProcessingStatus.Pending, "准备处理文档", 0);

                // 如果提供了文件路径，从文件读取
                if (!string.IsNullOrEmpty(request.FilePath))
                {
                    return await ProcessDocumentFromFileAsync(
                        result.DocumentId,  // 传递documentId
                        request.FilePath,
                        request.KnowledgeBaseId,
                        request.Options,
                        progress,
                        cancellationToken);
                }

                // 如果提供了内容，直接处理内容
                if (!string.IsNullOrEmpty(request.Content))
                {
                    return await ProcessDocumentFromTextAsync(
                        result.DocumentId,  // 传递documentId
                        request.FileName,
                        request.Content,
                        request.KnowledgeBaseId,
                        request.Options,
                        progress,
                        cancellationToken);
                }

                throw new ArgumentException("必须提供 FilePath 或 Content");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档处理失败: {DocumentId}, 错误: {Error}", result.DocumentId, ex.Message);
                result.Status = ProcessingStatus.Failed;
                result.ErrorMessage = ex.Message;
                UpdateStatus(result.DocumentId, ProcessingStatus.Failed, ex.Message, 0);
                throw;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }
        }

        public async Task<DocumentProcessingResult> ProcessDocumentFromFileAsync(
            string documentId,
            string filePath,
            string knowledgeBaseId,
            DocumentProcessingOptions? options = null,
            IProgress<DocumentProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("开始处理文档文件: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            // 根据文件扩展名选择处理方式
            string content;
            switch (extension)
            {
                case ".pdf":
                    content = await ExtractTextFromPdfAsync(filePath, cancellationToken);
                    break;

                case ".docx":
                    content = await ExtractTextFromWordAsync(filePath, cancellationToken);
                    break;

                case ".xlsx":
                    content = await ExtractTextFromExcelAsync(filePath, cancellationToken);
                    break;

                case ".pptx":
                    content = await ExtractTextFromPowerPointAsync(filePath, cancellationToken);
                    break;

                case ".txt":
                case ".md":
                    content = await File.ReadAllTextAsync(filePath, cancellationToken);
                    break;

                default:
                    throw new NotSupportedException($"不支持的文件格式: {extension}");
            }

            var textResult = await ProcessDocumentFromTextAsync(documentId, fileName, content, knowledgeBaseId, options, progress, cancellationToken);

            // 图片处理（独立于文本处理，失败不影响文本入库）
            if (_imageProcessingPipeline != null && options?.EnableImageProcessing == true)
            {
                try
                {
                    var imageOptions = new Models.ImageProcessingOptions
                    {
                        VisionModelId = options?.VisionModelId
                    };
                    var imageResult = await _imageProcessingPipeline.ProcessAsync(
                        documentId, filePath, textResult.Chunks, imageOptions, progress, cancellationToken);
                    _logger.LogInformation("图片处理完成: {Status}, 提取{Extracted}张, 处理{Processed}张",
                        imageResult.Status, imageResult.TotalExtracted, imageResult.TotalProcessed);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "图片处理失败，不影响文本入库");
                }
            }

            return textResult;
        }

        public async Task<DocumentProcessingResult> ProcessDocumentFromTextAsync(
            string documentId,
            string fileName,
            string content,
            string knowledgeBaseId,
            DocumentProcessingOptions? options = null,
            IProgress<DocumentProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new DocumentProcessingResult
            {
                DocumentId = documentId,  // 使用传入的documentId
                FileName = fileName,
                StartTime = DateTime.UtcNow
            };

            options ??= new DocumentProcessingOptions();

            try
            {
                _logger.LogInformation("开始处理文档: {FileName}, ID: {DocumentId}", fileName, documentId);

                // 清理旧数据（重新处理时）
                await CleanupDocumentDataAsync(documentId, knowledgeBaseId, cancellationToken);

                // 步骤1: 计算MD5并创建存储目录
                UpdateStatus(documentId, ProcessingStatus.Parsing, "计算文档MD5", 5);
                ReportProgress(progress, documentId, "计算MD5", 5, 1, 1);

                var md5 = ComputeMd5Hash(content);
                var storagePath = GetStoragePath(md5);

                if (_enableFileSync)
                {
                    Directory.CreateDirectory(storagePath);
                }

                // 步骤2: 保存原始文件
                if (_enableFileSync)
                {
                    var originalFilePath = Path.Combine(storagePath, "original");
                    Directory.CreateDirectory(originalFilePath);
                    await File.WriteAllTextAsync(
                        Path.Combine(originalFilePath, fileName),
                        content,
                        cancellationToken);
                }

                // 步骤3: 文本分块
                UpdateStatus(documentId, ProcessingStatus.Chunking, "文本分块", 20);
                ReportProgress(progress, documentId, "文本分块", 20, 1, 4);

                var chunks = await ChunkDocumentAsync(content, options, cancellationToken);
                result.ChunkCount = chunks.Count;
                result.TotalTokens = chunks.Sum(c => c.TokenCount);

                _logger.LogInformation("文档分块完成: {Count} 个块, {Tokens} tokens",
                    chunks.Count, result.TotalTokens);

                // 步骤4: 保存分块文件
                if (_enableFileSync)
                {
                    UpdateStatus(documentId, ProcessingStatus.Saving, "保存分块文件", 30);
                    ReportProgress(progress, documentId, "保存分块文件", 30, 2, 4);

                    var chunksDir = Path.Combine(storagePath, "chunks");
                    Directory.CreateDirectory(chunksDir);

                    for (int i = 0; i < chunks.Count; i++)
                    {
                        var chunkFileName = $"chunk_{i + 1:D3}.md";
                        var chunkFilePath = Path.Combine(chunksDir, chunkFileName);
                        await File.WriteAllTextAsync(chunkFilePath, chunks[i].Content, cancellationToken);
                    }
                }

                // 步骤5: 向量化
                if (options.EnableEmbedding)
                {
                    UpdateStatus(documentId, ProcessingStatus.Embedding, "向量化处理", 50);
                    ReportProgress(progress, documentId, "向量化", 50, 3, 4);

                    await VectorizeChunksAsync(documentId, chunks, cancellationToken);
                }

                // 步骤6: 保存到数据库
                UpdateStatus(documentId, ProcessingStatus.Saving, "保存到数据库", 70);
                ReportProgress(progress, documentId, "保存到数据库", 70, 4, 4);

                await SaveChunksToDatabaseAsync(documentId, knowledgeBaseId, chunks, cancellationToken);

                // 步骤7: 提取实体和关系
                if (options.ExtractEntities || options.ExtractRelations)
                {
                    UpdateStatus(documentId, ProcessingStatus.ExtractingEntities, "提取知识图谱", 85);

                    if (options.ExtractEntities)
                    {
                        var entities = await ExtractEntitiesFromChunksAsync(chunks, options, cancellationToken);
                        result.Entities = entities;
                        result.EntityCount = entities.Count;

                        if (_enableFileSync && entities.Count > 0)
                        {
                            var entitiesJson = JsonSerializer.Serialize(entities, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            });
                            await File.WriteAllTextAsync(
                                Path.Combine(storagePath, "entities.json"),
                                entitiesJson,
                                cancellationToken);
                        }

                        await _knowledgeGraphService.SaveEntitiesAsync(entities, null, documentId, cancellationToken);
                    }

                    if (options.ExtractRelations)
                    {
                        var relations = await ExtractRelationsFromChunksAsync(
                            chunks, result.Entities, cancellationToken);
                        result.RelationCount = relations.Count;
                        result.Relations = relations;

                        if (_enableFileSync && relations.Count > 0)
                        {
                            var relationsJson = JsonSerializer.Serialize(relations, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            });
                            await File.WriteAllTextAsync(
                                Path.Combine(storagePath, "relations.json"),
                                relationsJson,
                                cancellationToken);
                        }

                        await _knowledgeGraphService.SaveRelationsAsync(relations, documentId, cancellationToken);
                    }
                }

                // 步骤8: 保存元数据
                if (_enableFileSync)
                {
                    var metadata = new
                    {
                        DocumentId = documentId,
                        FileName = fileName,
                        Md5 = md5,
                        KnowledgeBaseId = knowledgeBaseId,
                        ChunkCount = result.ChunkCount,
                        TotalTokens = result.TotalTokens,
                        EntityCount = result.EntityCount,
                        RelationCount = result.RelationCount,
                        ProcessedAt = DateTime.UtcNow
                    };

                    var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    await File.WriteAllTextAsync(
                        Path.Combine(storagePath, "metadata.json"),
                        metadataJson,
                        cancellationToken);
                }

                result.Status = ProcessingStatus.Completed;
                result.Chunks = chunks;

                UpdateStatus(documentId, ProcessingStatus.Completed, "处理完成", 100);
                ReportProgress(progress, documentId, "完成", 100, 4, 4);

                _logger.LogInformation("文档处理完成: {DocumentId}, 块数: {ChunkCount}, Token数: {TotalTokens}",
                    documentId, result.ChunkCount, result.TotalTokens);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档处理失败: {DocumentId}, 错误: {Error}", documentId, ex.Message);
                result.Status = ProcessingStatus.Failed;
                result.ErrorMessage = ex.Message;
                UpdateStatus(documentId, ProcessingStatus.Failed, ex.Message, 0);
                throw;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }
        }

        public Task<List<DocumentProcessingResult>> ProcessDocumentsAsync(
            List<DocumentProcessingRequest> requests,
            IProgress<DocumentProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("批量处理功能待实现");
        }

        public Task<DocumentProcessingStatus?> GetProcessingStatusAsync(
            string documentId,
            CancellationToken cancellationToken = default)
        {
            _processingStatuses.TryGetValue(documentId, out var status);
            return Task.FromResult<DocumentProcessingStatus?>(status);
        }

        public string GetDocumentStoragePath(string documentId)
        {
            // TODO: 从数据库查询文档的MD5，然后返回存储路径
            return Path.Combine(_documentRootPath, "unknown");
        }

        public Task<DocumentInfo?> GetDocumentByMd5Async(
            string md5,
            CancellationToken cancellationToken = default)
        {
            // TODO: 从数据库查询文档信息
            return Task.FromResult<DocumentInfo?>(null);
        }

        public async Task<DocumentDeletionResult> DeleteDocumentAsync(
            string documentId,
            CancellationToken cancellationToken = default)
        {
            var result = new DocumentDeletionResult
            {
                DocumentId = documentId,
                Success = false
            };

            try
            {
                _logger.LogInformation("开始删除文档: {DocumentId}", documentId);

                // 1. 删除向量数据
                try
                {
                    await _vectorRepository.DeleteDocumentAsync(documentId, cancellationToken);
                    var vectorCount = await _vectorRepository.GetDocumentChunkCountAsync(documentId, cancellationToken);
                    result.DeletedVectors = vectorCount;
                    _logger.LogInformation("删除文档向量: {Count} 个", vectorCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除文档向量失败: {Message}", ex.Message);
                }

                // 2. 删除图谱数据（需要knowledgeBaseId）
                try
                {
                    // 从documentId中提取knowledgeBaseId（假设格式为 knowledgeBaseId_fileId）
                    var knowledgeBaseId = documentId.Contains('_') ? documentId.Split('_')[0] : documentId;

                    var (entityCount, relationCount) = await _knowledgeGraphService.DeleteDocumentGraphAsync(
                        documentId,
                        knowledgeBaseId,
                        cancellationToken);

                    result.DeletedEntities = entityCount;
                    result.DeletedRelations = relationCount;

                    _logger.LogInformation("删除文档图谱: {EntityCount} 个实体, {RelationCount} 个关系",
                        entityCount, relationCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除文档图谱失败: {Message}", ex.Message);
                }

                // 3. 删除文件
                try
                {
                    if (_enableFileSync)
                    {
                        var storagePath = GetDocumentStoragePath(documentId);
                        if (Directory.Exists(storagePath))
                        {
                            Directory.Delete(storagePath, recursive: true);
                            result.FileDeleted = true;
                            _logger.LogInformation("删除文档文件: {Path}", storagePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除文档文件失败: {Message}", ex.Message);
                }

                result.Success = true;
                _logger.LogInformation("文档删除完成: {DocumentId}", documentId);

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "删除文档失败: {DocumentId}, 错误: {Message}", documentId, ex.Message);
                return result;
            }
        }

        public async Task<List<DocumentDeletionResult>> DeleteDocumentsAsync(
            List<string> documentIds,
            CancellationToken cancellationToken = default)
        {
            var results = new List<DocumentDeletionResult>();

            foreach (var documentId in documentIds)
            {
                var result = await DeleteDocumentAsync(documentId, cancellationToken);
                results.Add(result);
            }

            return results;
        }

        public async Task<KnowledgeBaseDeletionResult> DeleteKnowledgeBaseAsync(
            string knowledgeBaseId,
            CancellationToken cancellationToken = default)
        {
            var result = new KnowledgeBaseDeletionResult
            {
                KnowledgeBaseId = knowledgeBaseId,
                Success = false
            };

            try
            {
                _logger.LogInformation("开始删除知识库: {KnowledgeBaseId}", knowledgeBaseId);

                // 1. 删除所有向量数据
                try
                {
                    await _vectorRepository.DeleteKnowledgeBaseAsync(knowledgeBaseId, cancellationToken);
                    _logger.LogInformation("删除知识库向量数据完成");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除知识库向量数据失败: {Message}", ex.Message);
                }

                // 2. 删除所有图谱数据
                try
                {
                    var (entityCount, relationCount) = await _knowledgeGraphService.DeleteKnowledgeBaseGraphAsync(
                        knowledgeBaseId,
                        cancellationToken);

                    result.DeletedEntities = entityCount == -1 ? 0 : entityCount;
                    result.DeletedRelations = relationCount == -1 ? 0 : relationCount;

                    _logger.LogInformation("删除知识库图谱数据完成: {EntityCount} 个实体, {RelationCount} 个关系",
                        entityCount, relationCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除知识库图谱数据失败: {Message}", ex.Message);
                }

                // 3. 删除所有文件
                try
                {
                    if (_enableFileSync)
                    {
                        var kbStoragePath = Path.Combine(_documentRootPath, knowledgeBaseId);
                        if (Directory.Exists(kbStoragePath))
                        {
                            var files = Directory.GetFiles(kbStoragePath, "*", SearchOption.AllDirectories);
                            foreach (var file in files)
                            {
                                try
                                {
                                    File.Delete(file);
                                }
                                catch { }
                            }

                            Directory.Delete(kbStoragePath, recursive: true);
                            result.DeletedFiles = files.Length;
                            _logger.LogInformation("删除知识库文件: {Count} 个", files.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除知识库文件失败: {Message}", ex.Message);
                }

                result.Success = true;
                _logger.LogInformation("知识库删除完成: {KnowledgeBaseId}", knowledgeBaseId);

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "删除知识库失败: {KnowledgeBaseId}, 错误: {Message}", knowledgeBaseId, ex.Message);
                return result;
            }
        }

        public async Task<DocumentDeletionResult> DeleteByFileIdAsync(
            string fileId,
            CancellationToken cancellationToken = default)
        {
            // 根据fileId查找对应的documentId
            // 这里需要根据实际的存储规则来实现
            // 假设存储规则是: knowledgeBaseId/fileId/*

            try
            {
                if (_enableFileSync)
                {
                    // 遍历所有知识库目录
                    var kbDirectories = Directory.GetDirectories(_documentRootPath);
                    foreach (var kbDir in kbDirectories)
                    {
                        var knowledgeBaseId = Path.GetFileName(kbDir);
                        var fileDir = Path.Combine(kbDir, fileId);

                        if (Directory.Exists(fileDir))
                        {
                            // 找到对应的文件，构造documentId
                            var documentId = $"{knowledgeBaseId}_{fileId}";
                            return await DeleteDocumentAsync(documentId, cancellationToken);
                        }
                    }
                }

                return new DocumentDeletionResult
                {
                    DocumentId = fileId,
                    Success = false,
                    ErrorMessage = "未找到对应的文档"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据FileId删除文档失败: {FileId}, 错误: {Message}", fileId, ex.Message);
                return new DocumentDeletionResult
                {
                    DocumentId = fileId,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 清理文档旧数据（分块、知识图谱、图片），用于重新处理前
        /// </summary>
        private async Task CleanupDocumentDataAsync(
            string documentId,
            string knowledgeBaseId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始清理文档旧数据: {DocumentId}", documentId);

            // 1. 删除旧的向量分块数据
            try
            {
                await _vectorRepository.DeleteDocumentAsync(documentId, cancellationToken);
                _logger.LogInformation("已清理文档旧分块数据: {DocumentId}", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理文档旧分块数据失败: {DocumentId}", documentId);
            }

            // 2. 删除旧的知识图谱数据
            try
            {
                await _knowledgeGraphService.DeleteDocumentGraphAsync(documentId, knowledgeBaseId, cancellationToken);
                _logger.LogInformation("已清理文档旧知识图谱数据: {DocumentId}", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理文档旧知识图谱数据失败: {DocumentId}", documentId);
            }

            // 3. 删除旧的图片元数据和文件
            try
            {
                if (_imageRepository != null)
                {
                    await _imageRepository.DeleteByDocumentIdAsync(documentId, cancellationToken);
                }
                if (_imageStorageService != null)
                {
                    await _imageStorageService.DeleteByDocumentAsync(documentId, cancellationToken);
                }
                _logger.LogInformation("已清理文档旧图片数据: {DocumentId}", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理文档旧图片数据失败: {DocumentId}", documentId);
            }
        }

        /// <summary>
        /// 从PDF文件提取文本
        /// </summary>
        private async Task<string> ExtractTextFromPdfAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder();

                    using (var pdfDocument = PdfDocument.Open(filePath))
                    {
                        _logger.LogInformation("PDF页数: {PageCount}", pdfDocument.NumberOfPages);

                        foreach (var page in pdfDocument.GetPages())
                        {
                            // 提取页面文本
                            var text = page.Text;

                            // 清理文本（移除多余的空白字符）
                            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                sb.AppendLine(text);
                            }
                        }
                    }

                    var result = sb.ToString();

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PDF文本提取失败: {FilePath}", filePath);
                    throw new InvalidOperationException($"无法从PDF文件提取文本: {ex.Message}", ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 从Word文档提取文本
        /// </summary>
        private async Task<string> ExtractTextFromWordAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder();

                    using (var wordDocument = WordprocessingDocument.Open(filePath, false))
                    {
                        var mainPart = wordDocument.MainDocumentPart;
                        if (mainPart == null)
                        {
                            _logger.LogWarning("Word文档没有主文档部分");
                            return string.Empty;
                        }

                        var body = mainPart.Document.Body;
                        if (body != null)
                        {
                            foreach (var paragraph in body.Elements<Paragraph>())
                            {
                                var text = paragraph.InnerText;
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    sb.AppendLine(text);
                                }
                            }
                        }

                        // 处理表格
                        foreach (var table in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>())
                        {
                            foreach (var row in table.Elements<TableRow>())
                            {
                                var cells = row.Elements<TableCell>()
                                    .Select(c => c.InnerText)
                                    .Where(text => !string.IsNullOrWhiteSpace(text));

                                if (cells.Any())
                                {
                                    sb.AppendLine(string.Join(" | ", cells));
                                }
                            }
                        }
                    }

                    var result = sb.ToString();

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Word文档文本提取失败: {FilePath}", filePath);
                    throw new InvalidOperationException($"无法从Word文档提取文本: {ex.Message}", ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 从Excel文档提取文本
        /// </summary>
        private async Task<string> ExtractTextFromExcelAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder();

                    using (var spreadsheetDocument = SpreadsheetDocument.Open(filePath, false))
                    {
                        var workbookPart = spreadsheetDocument.WorkbookPart;
                        if (workbookPart == null)
                        {
                            _logger.LogWarning("Excel文档没有工作簿部分");
                            return string.Empty;
                        }

                        foreach (var worksheetPart in workbookPart.WorksheetParts)
                        {
                            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
                            if (sheetData == null) continue;

                            foreach (var row in sheetData.Elements<Row>())
                            {
                                var cells = row.Elements<Cell>()
                                    .Select(cell =>
                                    {
                                        var cellValue = cell.InnerText;
                                        // 处理共享字符串
                                        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                                        {
                                            var stringTable = workbookPart.SharedStringTablePart;
                                            if (stringTable != null)
                                            {
                                                var sharedString = stringTable.SharedStringTable
                                                    .Elements<SharedStringItem>()
                                                    .ElementAt(int.Parse(cellValue));
                                                return sharedString.InnerText;
                                            }
                                        }
                                        return cellValue;
                                    })
                                    .Where(text => !string.IsNullOrWhiteSpace(text));

                                if (cells.Any())
                                {
                                    sb.AppendLine(string.Join(" | ", cells));
                                }
                            }

                            sb.AppendLine(); // 工作表之间添加空行
                        }
                    }

                    var result = sb.ToString();

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Excel文档文本提取失败: {FilePath}", filePath);
                    throw new InvalidOperationException($"无法从Excel文档提取文本: {ex.Message}", ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 从PowerPoint文档提取文本
        /// </summary>
        private async Task<string> ExtractTextFromPowerPointAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder();

                    using (var presentationDocument = PresentationDocument.Open(filePath, false))
                    {
                        var presentationPart = presentationDocument.PresentationPart;
                        if (presentationPart == null)
                        {
                            _logger.LogWarning("PowerPoint文档没有演示文稿部分");
                            return string.Empty;
                        }

                        var slideIndex = 0;
                        foreach (var slidePart in presentationPart.SlideParts)
                        {
                            slideIndex++;
                            sb.AppendLine($"=== 幻灯片 {slideIndex} ===");

                            // 提取幻灯片中的所有文本
                            var slideTextParts = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();

                            foreach (var textPart in slideTextParts)
                            {
                                var text = textPart.Text;
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    sb.AppendLine(text);
                                }
                            }

                            sb.AppendLine(); // 幻灯片之间添加空行
                        }
                    }

                    var result = sb.ToString();

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PowerPoint文档文本提取失败: {FilePath}", filePath);
                    throw new InvalidOperationException($"无法从PowerPoint文档提取文本: {ex.Message}", ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 计算文本的MD5哈希值
        /// </summary>
        private string ComputeMd5Hash(string content)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(content);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// 获取存储路径（按年/月/日/MD5组织）
        /// </summary>
        private string GetStoragePath(string md5)
        {
            var now = DateTime.UtcNow;
            var year = now.ToString("yyyy");
            var month = now.ToString("MM");
            var day = now.ToString("dd");

            return Path.Combine(_documentRootPath, year, month, day, md5);
        }

        /// <summary>
        /// 文档分块
        /// </summary>
        private async Task<List<DocumentChunk>> ChunkDocumentAsync(
            string content,
            DocumentProcessingOptions options,
            CancellationToken cancellationToken)
        {
            var semanticChunks = await _chunkerService.ChunkAsync(
                content,
                options.ChunkingStrategy,
                cancellationToken);

            var chunks = new List<DocumentChunk>();
            int sequenceNumber = 1;

            // 使用配置的MaxChunkSize，如果未配置则使用默认值1000
            var maxChunkTokens = options.MaxChunkSize > 0 ? options.MaxChunkSize : 1000;
            var overlapTokens = options.ChunkOverlap > 0 ? options.ChunkOverlap : 200;

            for (int i = 0; i < semanticChunks.Count; i++)
            {
                var chunk = semanticChunks[i];

                // 如果单个块超过最大token限制，进行强制分割
                if (chunk.TokenCount > maxChunkTokens)
                {
                    _logger.LogWarning(
                        "检测到超大块 (Tokens: {TokenCount})，进行强制分割以避免向量化失败",
                        chunk.TokenCount);

                    var subChunks = await ForceSplitLargeChunkAsync(
                        chunk.Content,
                        chunk.TokenCount,
                        maxChunkTokens,
                        overlapTokens,
                        chunk.StartPosition,
                        sequenceNumber,
                        cancellationToken);

                    chunks.AddRange(subChunks);
                    sequenceNumber += subChunks.Count;
                }
                else
                {
                    chunks.Add(new DocumentChunk
                    {
                        ChunkId = $"{Guid.NewGuid()}",
                        SequenceNumber = sequenceNumber++,
                        Content = chunk.Content,
                        TokenCount = chunk.TokenCount,
                        StartPosition = chunk.StartPosition,
                        EndPosition = chunk.EndPosition
                    });
                }
            }

            return chunks;
        }

        /// <summary>
        /// 强制分割超大块（优化版：智能分割点 + Overlap机制）
        /// </summary>
        private async Task<List<DocumentChunk>> ForceSplitLargeChunkAsync(
            string content,
            int originalTokenCount,
            int maxTokens,
            int overlapTokens,
            int startPosition,
            int startSequenceNumber,
            CancellationToken cancellationToken)
        {
            var chunks = new List<DocumentChunk>();

            // 使用传入的overlapTokens配置
            var effectiveMaxTokens = maxTokens - overlapTokens; // 实际可用空间

            // 智能分割点优先级：段落 > 句子 > 强制分割
            var segments = SmartSplitContent(content);

            var currentChunk = new StringBuilder();
            var overlapBuffer = new StringBuilder(); // 用于存储overlap内容
            int currentTokens = 0;
            int currentPosition = startPosition;
            double tokenRatio = (double)originalTokenCount / content.Length;

            foreach (var segment in segments)
            {
                var segmentTokens = (int)Math.Ceiling(segment.Length * tokenRatio);

                // 检查是否需要分割
                if (currentTokens + segmentTokens > effectiveMaxTokens && currentChunk.Length > 0)
                {
                    // 添加overlap内容（从上一个chunk的末尾取）
                    var overlapText = GetOverlapText(currentChunk.ToString(), overlapTokens, tokenRatio);
                    var chunkContent = currentChunk.ToString();

                    chunks.Add(new DocumentChunk
                    {
                        ChunkId = $"{Guid.NewGuid()}",
                        SequenceNumber = startSequenceNumber++,
                        Content = chunkContent,
                        TokenCount = currentTokens,
                        StartPosition = currentPosition,
                        EndPosition = currentPosition + currentChunk.Length
                    });

                    currentPosition += currentChunk.Length;

                    // 新chunk以overlap内容开始
                    currentChunk = new StringBuilder();
                    if (!string.IsNullOrEmpty(overlapText))
                    {
                        currentChunk.Append(overlapText);
                        currentTokens = (int)Math.Ceiling(overlapText.Length * tokenRatio);
                    }
                    else
                    {
                        currentTokens = 0;
                    }
                }

                currentChunk.Append(segment);
                currentTokens += segmentTokens;
            }

            // 保存最后一个块
            if (currentChunk.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    ChunkId = $"{Guid.NewGuid()}",
                    SequenceNumber = startSequenceNumber,
                    Content = currentChunk.ToString(),
                    TokenCount = currentTokens,
                    StartPosition = currentPosition,
                    EndPosition = currentPosition + currentChunk.Length
                });
            }

            _logger.LogInformation("智能分割完成 (含Overlap): 原 {OriginalTokens} tokens → {SplitCount} 个块, Overlap={Overlap} tokens",
                originalTokenCount, chunks.Count, overlapTokens);

            return chunks;
        }

        /// <summary>
        /// 智能分割内容：优先级为 段落 > 句子 > 强制
        /// </summary>
        private string[] SmartSplitContent(string content)
        {
            // 首先按段落分割（双换行或单换行+缩进）
            var paragraphs = content.Split(new[] { "\n\n", "\n\r\n", "\n    ", "\n\t" },
                StringSplitOptions.None);

            // 如果段落数量太少（少于5个），则按句子分割
            if (paragraphs.Length < 5)
            {
                return content.Split(new[] { '。', '！', '？', '.', '!', '?', '\n', '\r' },
                    StringSplitOptions.RemoveEmptyEntries);
            }

            // 否则返回段落（有些段落可能仍需要进一步细分）
            var segments = new List<string>();
            foreach (var para in paragraphs)
            {
                // 如果单个段落太长（超过2000字符），进一步按句子分割
                if (para.Length > 2000)
                {
                    var sentences = para.Split(new[] { '。', '！', '？', '.', '!', '?' },
                        StringSplitOptions.RemoveEmptyEntries);
                    segments.AddRange(sentences);
                }
                else
                {
                    segments.Add(para);
                }
            }

            return segments.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        }

        /// <summary>
        /// 获取overlap文本（从chunk末尾提取指定token数的内容）
        /// </summary>
        private string GetOverlapText(string chunkContent, int overlapTokens, double tokenRatio)
        {
            if (overlapTokens <= 0) return string.Empty;

            // 估算需要的字符数
            int overlapChars = (int)Math.Ceiling(overlapTokens / tokenRatio);

            // 如果chunk太短，返回空
            if (chunkContent.Length <= overlapChars * 2)
            {
                return string.Empty;
            }

            // 从末尾提取overlap字符
            var overlapText = chunkContent.Substring(chunkContent.Length - overlapChars);

            // 尝试在句子边界处截断，保持语义完整
            var lastSentenceEnd = overlapText.LastIndexOfAny(new[] { '。', '！', '？', '.', '!', '?', '\n' });
            if (lastSentenceEnd > overlapChars / 2) // 确保overlap不会太小
            {
                overlapText = overlapText.Substring(lastSentenceEnd + 1).TrimStart();
            }

            return overlapText;
        }

        /// <summary>
        /// 向量化分块（优化版：分批处理+重试机制）
        /// </summary>
        private async Task VectorizeChunksAsync(
            string documentId,
            List<DocumentChunk> chunks,
            CancellationToken cancellationToken)
        {
            // 优化2: 分批处理，避免一次性向量化过多文本
            const int batchSize = 5; // 每批处理5个块
            const int maxRetries = 3; // 最大重试次数

            int totalChunks = chunks.Count;
            int processedChunks = 0;

            for (int batch = 0; batch < (int)Math.Ceiling((double)totalChunks / batchSize); batch++)
            {
                int startIndex = batch * batchSize;
                int endIndex = Math.Min(startIndex + batchSize, totalChunks);
                var currentBatch = chunks.Skip(startIndex).Take(endIndex - startIndex).ToList();

                _logger.LogInformation("向量化批次 {Batch}/{TotalBatches}: 处理 {Count} 个块",
                    batch + 1, (int)Math.Ceiling((double)totalChunks / batchSize), currentBatch.Count);

                // 并行处理当前批次
                var tasks = currentBatch.Select(async chunk =>
                {
                    // 优化3: 添加重试机制
                    for (int retry = 0; retry <= maxRetries; retry++)
                    {
                        try
                        {
                            var embedding = await _embeddingService.GetEmbeddingAsync(chunk.Content, cancellationToken);
                            chunk.Embedding = embedding;
                            break; // 成功，跳出重试循环
                        }
                        catch (Exception ex)
                        {
                            if (retry == maxRetries)
                            {
                                _logger.LogError(ex, "向量化失败（已重试{Retries}次）: ChunkId={ChunkId}, 错误={Error}",
                                    maxRetries, chunk.ChunkId, ex.Message);
                            }
                            else
                            {
                                _logger.LogWarning("向量化失败（第{Retry}次重试）: ChunkId={ChunkId}",
                                    retry + 1, chunk.ChunkId);
                                await Task.Delay(1000 * (retry + 1), cancellationToken); // 递增延迟
                            }
                        }
                    }
                    return chunk;
                }).ToList();

                await Task.WhenAll(tasks);
                processedChunks += currentBatch.Count;
            }
        }

        /// <summary>
        /// 保存分块到数据库
        /// </summary>
        private async Task SaveChunksToDatabaseAsync(
            string documentId,
            string knowledgeBaseId,
            List<DocumentChunk> chunks,
            CancellationToken cancellationToken)
        {
            var chunkVectors = new List<DocumentChunkVector>();

            foreach (var chunk in chunks)
            {
                if (chunk.Embedding != null && chunk.Embedding.Length > 0)
                {
                    chunkVectors.Add(new DocumentChunkVector
                    {
                        DocumentId = documentId,
                        ChunkId = chunk.ChunkId,
                        Content = chunk.Content,
                        Embedding = chunk.Embedding,
                        TokenCount = chunk.TokenCount,
                        Metadata = new Dictionary<string, object>
                        {
                            { "sequence_number", chunk.SequenceNumber },
                            { "token_count", chunk.TokenCount }
                        }
                    });
                }
            }

            if (chunkVectors.Count > 0)
            {
                await _vectorRepository.SaveDocumentChunksAsync(chunkVectors, cancellationToken);
            }
        }

        /// <summary>
        /// 从分块中提取实体
        /// </summary>
        private async Task<List<ZSN.AI.Entity.KnowledgeBase.Entity>> ExtractEntitiesFromChunksAsync(
            List<DocumentChunk> chunks,
            DocumentProcessingOptions options,
            CancellationToken cancellationToken)
        {
            var allEntities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();

            foreach (var chunk in chunks)
            {
                try
                {
                    var config = new EntityExtractionConfig
                    {
                        ModelId = options.EntityModelId ?? 13, // 使用配置的模型ID或默认值
                        MinConfidence = 0.7f
                    };

                    var entities = await _knowledgeGraphService.ExtractEntitiesAsync(
                        chunk.Content, config, cancellationToken);

                    // 关联分块ID
                    foreach (var entity in entities)
                    {
                        entity.SourceChunkIds.Add(chunk.ChunkId);
                    }

                    allEntities.AddRange(entities);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "实体提取失败: ChunkId={ChunkId}", chunk.ChunkId);
                }
            }

            return allEntities;
        }

        /// <summary>
        /// 从分块中提取关系
        /// </summary>
        private async Task<List<Relation>> ExtractRelationsFromChunksAsync(
            List<DocumentChunk> chunks,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            CancellationToken cancellationToken)
        {
            var allRelations = new List<Relation>();

            foreach (var chunk in chunks)
            {
                try
                {
                    // 获取该分块中的实体
                    var chunkEntities = entities.Where(e =>
                        e.SourceChunkIds.Contains(chunk.ChunkId)).ToList();

                    if (chunkEntities.Count < 2)
                        continue;

                    var relations = await _knowledgeGraphService.ExtractRelationsAsync(
                        chunk.Content, chunkEntities, cancellationToken);

                    // 关联分块ID
                    foreach (var relation in relations)
                    {
                        relation.SourceChunkIds.Add(chunk.ChunkId);
                    }

                    allRelations.AddRange(relations);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "关系提取失败: ChunkId={ChunkId}", chunk.ChunkId);
                }
            }

            return allRelations;
        }

        /// <summary>
        /// 更新处理状态
        /// </summary>
        private void UpdateStatus(
            string documentId,
            ProcessingStatus status,
            string currentStage,
            int progress)
        {
            var statusObj = new DocumentProcessingStatus
            {
                DocumentId = documentId,
                Status = status,
                CurrentStage = currentStage,
                Progress = progress,
                UpdatedAt = DateTime.UtcNow
            };

            _processingStatuses[documentId] = statusObj;
        }

        /// <summary>
        /// 报告进度
        /// </summary>
        private void ReportProgress(
            IProgress<DocumentProcessingProgress>? progress,
            string documentId,
            string stage,
            int percentage,
            int currentItem,
            int totalItems)
        {
            progress?.Report(new DocumentProcessingProgress
            {
                DocumentId = documentId,
                Stage = stage,
                Percentage = percentage,
                CurrentItem = currentItem,
                TotalItems = totalItems
            });
        }

        #endregion
    }
}
