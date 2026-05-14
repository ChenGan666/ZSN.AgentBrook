using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZSN.AI.KnowledgeBase.Interface;
using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Services
{
    public class FileImageStorageService : IImageStorageService
    {
        private readonly string _imageRootPath;
        private readonly ILogger<FileImageStorageService> _logger;

        public FileImageStorageService(IConfiguration configuration, ILogger<FileImageStorageService> logger)
        {
            _imageRootPath = configuration["ImageProcessing:Storage:RootPath"] ?? "./DocumentImages";
            _logger = logger;
            Directory.CreateDirectory(_imageRootPath);
        }

        public async Task<ImageStorageInfo> SaveAsync(string documentId, ExtractedImage image, CancellationToken ct = default)
        {
            var safeDocId = SanitizePath(documentId);
            var dir = Path.Combine(_imageRootPath, "images", safeDocId);
            Directory.CreateDirectory(dir);

            var ext = GetExtensionFromMimeType(image.MimeType);
            var filePath = Path.Combine(dir, $"{image.ImageId}{ext}");

            await File.WriteAllBytesAsync(filePath, image.Data, ct);

            return new ImageStorageInfo
            {
                StoragePath = filePath,
                StorageType = "file",
                FileSize = image.Data.Length
            };
        }

        public Task<string?> GetUrlAsync(string storagePath, CancellationToken ct = default)
        {
            if (!File.Exists(storagePath)) return Task.FromResult<string?>(null);
            return Task.FromResult<string?>(storagePath);
        }

        public Task<byte[]?> GetDataAsync(string storagePath, CancellationToken ct = default)
        {
            if (!File.Exists(storagePath)) return Task.FromResult<byte[]?>(null);
            return Task.FromResult(File.ReadAllBytes(storagePath));
        }

        public Task DeleteByDocumentAsync(string documentId, CancellationToken ct = default)
        {
            var safeDocId = SanitizePath(documentId);
            var dir = Path.Combine(_imageRootPath, "images", safeDocId);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
                _logger.LogInformation("已删除文档图片目录: {Dir}", dir);
            }
            return Task.CompletedTask;
        }

        private static string SanitizePath(string input)
        {
            foreach (var c in Path.GetInvalidPathChars())
                input = input.Replace(c, '_');
            foreach (var c in new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' })
                input = input.Replace(c, '_');
            return input;
        }

        private static string GetExtensionFromMimeType(string? mimeType)
        {
            return mimeType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                "image/webp" => ".webp",
                "image/tiff" => ".tiff",
                _ => ".png"
            };
        }
    }
}
