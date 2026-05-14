using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.KnowledgeBase.Interface;
using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Services
{
    public class ImageProcessingPipeline : IImageProcessingPipeline
    {
        private readonly IImageExtractionService _extractionService;
        private readonly IImageStorageService _storageService;
        private readonly IImageDescriptionService _descriptionService;
        private readonly IImageRepository _imageRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImageProcessingPipeline> _logger;

        public ImageProcessingPipeline(
            IImageExtractionService extractionService,
            IImageStorageService storageService,
            IImageDescriptionService descriptionService,
            IImageRepository imageRepository,
            IConfiguration configuration,
            ILogger<ImageProcessingPipeline> logger)
        {
            _extractionService = extractionService;
            _storageService = storageService;
            _descriptionService = descriptionService;
            _imageRepository = imageRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ImageProcessingResult> ProcessAsync(
            string documentId, string filePath,
            List<DocumentChunk> existingChunks,
            ImageProcessingOptions? options = null,
            IProgress<DocumentProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new ImageProcessingResult { DocumentId = documentId };

            try
            {
                // 步骤1：提取图片
                var images = await _extractionService.ExtractFromDocumentAsync(
                    filePath, options?.ExtractionOptions, cancellationToken);
                if (images.Count == 0)
                {
                    result.Status = "no_images";
                    return result;
                }

                _logger.LogInformation("文档{DocId}提取到{Count}张候选图片", documentId, images.Count);

                // 步骤2：跨文档去重
                var hashes = images.Select(i => i.ContentHash).Where(h => !string.IsNullOrEmpty(h)).ToList();
                var existingHashes = hashes.Count > 0
                    ? await _imageRepository.GetExistingHashesAsync(hashes, cancellationToken)
                    : new List<string>();

                var newImages = images.Where(img => !existingHashes.Contains(img.ContentHash)).ToList();
                result.TotalExtracted = images.Count;
                result.TotalSkipped = images.Count - newImages.Count;

                if (newImages.Count == 0)
                {
                    result.Status = "all_duplicates";
                    return result;
                }

                // 步骤3：并行处理（存储+VLM描述）
                var concurrency = options?.VlmConcurrency > 0
                    ? options.VlmConcurrency
                    : _configuration.GetValue<int>("ImageProcessing:VLM:Concurrency", 3);

                using var semaphore = new SemaphoreSlim(concurrency, concurrency);
                var tasks = newImages.Select(img => ProcessSingleImageAsync(
                    documentId, img, semaphore, options, cancellationToken));
                var imageResults = (await Task.WhenAll(tasks))
                    .Where(x => x != null).ToList()!;

                // 步骤4：批量保存到数据库
                if (imageResults.Count > 0)
                {
                    await _imageRepository.SaveImageInfosAsync(imageResults, cancellationToken);
                }

                // 步骤5：关联图片到文本分块
                var relations = LinkImagesToChunks(imageResults, existingChunks);
                if (relations.Count > 0)
                {
                    await _imageRepository.SaveChunkImageRelationsAsync(relations, cancellationToken);
                }

                result.TotalProcessed = imageResults.Count;
                result.Status = "completed";

                _logger.LogInformation(
                    "图片Pipeline完成: {DocId}, 提取{Extracted}张, 处理{Processed}张, 跳过{Skipped}张",
                    documentId, result.TotalExtracted, result.TotalProcessed, result.TotalSkipped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "图片Pipeline失败: {DocId}", documentId);
                result.Status = "failed";
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<int> RegenerateDescriptionsAsync(
            string documentId, int? visionModelId = null,
            CancellationToken cancellationToken = default)
        {
            var failedImages = await _imageRepository.GetFailedDescriptionsAsync(documentId, cancellationToken);
            if (failedImages.Count == 0) return 0;

            var successCount = 0;
            foreach (var image in failedImages)
            {
                try
                {
                    var data = await _storageService.GetDataAsync(image.StoragePath, cancellationToken);
                    if (data == null) continue;

                    var desc = await _descriptionService.DescribeAsync(
                        data, image.MimeType, image.Description, visionModelId, cancellationToken);

                    var status = desc.Success ? "completed" : "failed";
                    await _imageRepository.UpdateDescriptionAsync(
                        image.ImageId, desc.Description, desc.OcrText, status, cancellationToken);

                    if (desc.Success) successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "重新生成图片描述失败: {ImageId}", image.ImageId);
                }
            }

            return successCount;
        }

        private async Task<DocumentImageInfo?> ProcessSingleImageAsync(
            string documentId, ExtractedImage image,
            SemaphoreSlim semaphore, ImageProcessingOptions? options,
            CancellationToken ct)
        {
            try
            {
                // 先存储
                var storageInfo = await _storageService.SaveAsync(documentId, image, ct);

                // VLM描述（受信号量限流）
                await semaphore.WaitAsync(ct);
                ImageDescription description;
                try
                {
                    description = await _descriptionService.DescribeAsync(
                        image.Data, image.MimeType, image.SurroundingText,
                        options?.VisionModelId, ct);
                }
                finally
                {
                    semaphore.Release();
                }

                return new DocumentImageInfo
                {
                    DocumentId = documentId,
                    ImageId = image.ImageId,
                    PageNumber = image.PageNumber,
                    SequenceNumber = image.SequenceNumber,
                    OriginalFilename = image.OriginalFilename,
                    StoragePath = storageInfo.StoragePath,
                    StorageType = storageInfo.StorageType,
                    MimeType = image.MimeType,
                    FileSize = storageInfo.FileSize,
                    Width = image.Width,
                    Height = image.Height,
                    ContentHash = image.ContentHash,
                    Description = description.Description,
                    OcrText = description.OcrText,
                    DescriptionStatus = description.Success ? "completed" : "failed",
                    Metadata = new Dictionary<string, object>
                    {
                        { "tags", description.Tags },
                        { "content_type", description.ContentType ?? "unknown" },
                        { "summary", description.Summary ?? "" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "单张图片处理失败: {ImageId}", image.ImageId);
                return null;
            }
        }

        private List<ChunkImageRelation> LinkImagesToChunks(
            List<DocumentImageInfo> images, List<DocumentChunk> chunks)
        {
            var relations = new List<ChunkImageRelation>();
            if (chunks == null || chunks.Count == 0) return relations;

            foreach (var image in images)
            {
                // 优先按页码匹配：找到同一页范围内的chunks，取最近的
                // 如果没有页码信息，退化为按序号匹配
                List<DocumentChunk> candidateChunks;

                if (image.PageNumber.HasValue)
                {
                    // 每个chunk的总字符数决定了每页大概有多少chunk
                    // 按页码估算：假设每页大约每1000字符一个chunk，根据SequenceNumber推算页码范围
                    // 更可靠的方式：将图片SequenceNumber作为页内序号，匹配同序号附近的chunks
                    var totalChunks = chunks.Count;
                    var imgIdx = image.SequenceNumber;

                    // 取图片序号前后各2个chunk作为候选（图片与最近文本的关联范围）
                    var startIdx = Math.Max(0, imgIdx - 2);
                    var endIdx = Math.Min(totalChunks, imgIdx + 3);
                    candidateChunks = chunks.Skip(startIdx).Take(endIdx - startIdx).ToList();

                    if (candidateChunks.Count == 0)
                        candidateChunks = chunks.Take(3).ToList();
                }
                else
                {
                    // 无页码信息，用序号找最近的1个chunk
                    candidateChunks = chunks
                        .OrderBy(c => Math.Abs(c.SequenceNumber - image.SequenceNumber))
                        .Take(1)
                        .ToList();
                }

                // 每张图片最多关联2个chunk（图片可能跨越chunk边界）
                foreach (var chunk in candidateChunks.Take(2))
                {
                    relations.Add(new ChunkImageRelation
                    {
                        ChunkId = chunk.ChunkId,
                        ImageId = image.ImageId,
                        RelationType = "nearby"
                    });
                }
            }

            return relations;
        }
    }
}
