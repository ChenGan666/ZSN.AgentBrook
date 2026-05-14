using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using UglyToad.PdfPig;
using ZSN.AI.KnowledgeBase.Interface;
using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Services
{
    public class ImageExtractionService : IImageExtractionService
    {
        private readonly ILogger<ImageExtractionService> _logger;

        public ImageExtractionService(ILogger<ImageExtractionService> logger)
        {
            _logger = logger;
        }

        public async Task<List<ExtractedImage>> ExtractFromDocumentAsync(
            string filePath, ImageExtractionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new ImageExtractionOptions();
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var rawImages = ext switch
            {
                ".pdf" => ExtractFromPdf(filePath),
                ".docx" => ExtractFromWord(filePath),
                ".pptx" => ExtractFromPpt(filePath),
                _ => new List<ExtractedImage>()
            };
            return FilterAndDeduplicate(rawImages, options);
        }

        public Task<ExtractedImage> ProcessStandaloneImageAsync(
            string filePath, CancellationToken cancellationToken = default)
        {
            var data = File.ReadAllBytes(filePath);
            var mimeType = DetectMimeTypeFromExtension(filePath);
            var hash = ComputeSha256(data);

            int? width = null, height = null;
            try
            {
                using var image = SixLabors.ImageSharp.Image.Load(data);
                width = image.Width;
                height = image.Height;
            }
            catch
            {
                // 无法识别的图片格式
            }

            return Task.FromResult(new ExtractedImage
            {
                Data = data,
                MimeType = mimeType,
                OriginalFilename = Path.GetFileName(filePath),
                Width = width,
                Height = height,
                ContentHash = hash
            });
        }

        private List<ExtractedImage> ExtractFromPdf(string filePath)
        {
            var images = new List<ExtractedImage>();
            try
            {
                using var document = PdfDocument.Open(filePath);
                var globalSeq = 0;
                foreach (var page in document.GetPages())
                {
                    var pageText = page.Text ?? string.Empty;
                    var surroundingText = pageText.Length > 500
                        ? pageText[..500]
                        : pageText;

                    foreach (var image in page.GetImages())
                    {
                        try
                        {
                            byte[]? imageData = null;
                            string? mimeType = null;

                            if (image.TryGetPng(out var pngBytes))
                            {
                                imageData = pngBytes;
                                mimeType = "image/png";
                            }
                            else
                            {
                                var rawBytes = image.RawBytes.ToArray();
                                if (rawBytes.Length > 0)
                                {
                                    var detected = DetectMimeType(rawBytes);
                                    if (detected != null)
                                    {
                                        imageData = rawBytes;
                                        mimeType = detected;
                                    }
                                }
                            }

                            if (imageData != null)
                            {
                                images.Add(new ExtractedImage
                                {
                                    Data = imageData,
                                    MimeType = mimeType,
                                    PageNumber = page.Number,
                                    SequenceNumber = globalSeq++,
                                    Width = (int)image.Bounds.Width,
                                    Height = (int)image.Bounds.Height,
                                    ContentHash = ComputeSha256(imageData),
                                    SurroundingText = surroundingText
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "PDF页面{Page}图片提取失败", page.Number);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF文件打开失败: {FilePath}", filePath);
            }
            return images;
        }

        private List<ExtractedImage> ExtractFromWord(string filePath)
        {
            var images = new List<ExtractedImage>();
            try
            {
                using var doc = WordprocessingDocument.Open(filePath, false);
                if (doc.MainDocumentPart == null) return images;

                var seq = 0;
                foreach (var imagePart in doc.MainDocumentPart.ImageParts)
                {
                    try
                    {
                        using var stream = imagePart.GetStream();
                        var data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);

                        var mimeType = imagePart.ContentType;
                        if (string.IsNullOrEmpty(mimeType) || mimeType.Contains("emf") || mimeType.Contains("wmf"))
                        {
                            var detected = DetectMimeType(data);
                            if (detected == null) continue;
                            mimeType = detected;
                        }

                        images.Add(new ExtractedImage
                        {
                            Data = data,
                            MimeType = mimeType,
                            SequenceNumber = seq++,
                            ContentHash = ComputeSha256(data)
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Word图片提取失败");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Word文件打开失败: {FilePath}", filePath);
            }
            return images;
        }

        private List<ExtractedImage> ExtractFromPpt(string filePath)
        {
            var images = new List<ExtractedImage>();
            try
            {
                using var presentation = PresentationDocument.Open(filePath, false);
                if (presentation.PresentationPart == null) return images;

                var seq = 0;
                foreach (var slidePart in presentation.PresentationPart.SlideParts)
                {
                    foreach (var imagePart in slidePart.ImageParts)
                    {
                        try
                        {
                            using var stream = imagePart.GetStream();
                            var data = new byte[stream.Length];
                            stream.Read(data, 0, data.Length);

                            var mimeType = imagePart.ContentType;
                            if (string.IsNullOrEmpty(mimeType))
                            {
                                var detected = DetectMimeType(data);
                                if (detected == null) continue;
                                mimeType = detected;
                            }

                            images.Add(new ExtractedImage
                            {
                                Data = data,
                                MimeType = mimeType,
                                SequenceNumber = seq++,
                                ContentHash = ComputeSha256(data)
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "PPT图片提取失败");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PPT文件打开失败: {FilePath}", filePath);
            }
            return images;
        }

        private List<ExtractedImage> FilterAndDeduplicate(
            List<ExtractedImage> images, ImageExtractionOptions options)
        {
            var seen = new HashSet<string>();
            var result = new List<ExtractedImage>();

            foreach (var img in images)
            {
                if (string.IsNullOrEmpty(img.ContentHash) || !seen.Add(img.ContentHash))
                    continue;

                var area = (img.Width ?? 0) * (img.Height ?? 0);
                if (area > 0 && area < options.MinImageArea) continue;
                if ((img.Width ?? int.MaxValue) < options.MinDimension) continue;
                if ((img.Height ?? int.MaxValue) < options.MinDimension) continue;

                if (img.Width > 0 && img.Height > 0)
                {
                    var ratio = Math.Max((float)img.Width / img.Height.Value, (float)img.Height / img.Width.Value);
                    if (ratio > options.MaxAspectRatio) continue;
                }

                if (img.FileSize < 1024) continue;

                result.Add(img);
                if (result.Count >= options.MaxImagesPerDocument)
                {
                    _logger.LogWarning("图片数量达到上限{Max}，截断", options.MaxImagesPerDocument);
                    break;
                }
            }
            return result;
        }

        private static string ComputeSha256(byte[] data)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(data)).ToLowerInvariant();
        }

        private static string? DetectMimeType(byte[] data)
        {
            if (data.Length < 4) return null;
            try
            {
                var format = SixLabors.ImageSharp.Image.DetectFormat(data);
                return format?.DefaultMimeType;
            }
            catch
            {
                return null;
            }
        }

        private static string DetectMimeTypeFromExtension(string filePath)
        {
            return Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".tiff" or ".tif" => "image/tiff",
                _ => "application/octet-stream"
            };
        }
    }
}
