using System;
using System.Collections.Generic;

namespace ZSN.AI.KnowledgeBase.Models
{
    public class ExtractedImage
    {
        public string ImageId { get; set; } = Guid.NewGuid().ToString();
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string? MimeType { get; set; }
        public string? OriginalFilename { get; set; }
        public int? PageNumber { get; set; }
        public int SequenceNumber { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string ContentHash { get; set; } = string.Empty;
        public long FileSize => Data.Length;
        public string? SurroundingText { get; set; }
    }

    public class ImageExtractionOptions
    {
        public int MinImageArea { get; set; } = 2500;
        public int MinDimension { get; set; } = 30;
        public float MaxAspectRatio { get; set; } = 20f;
        public int MaxImagesPerDocument { get; set; } = 100;
        public bool FilterDecorative { get; set; } = true;
    }

    public class ImageDescription
    {
        public string Description { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? ContentType { get; set; }
        public string? OcrText { get; set; }
        public float Confidence { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    public class ImageStorageInfo
    {
        public string StoragePath { get; set; } = string.Empty;
        public string StorageType { get; set; } = "file";
        public long FileSize { get; set; }
    }

    public class ImageProcessingOptions
    {
        public ImageExtractionOptions? ExtractionOptions { get; set; }
        public int VlmConcurrency { get; set; } = 3;
        public int? VisionModelId { get; set; }
    }

    public class ImageProcessingResult
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public int TotalExtracted { get; set; }
        public int TotalProcessed { get; set; }
        public int TotalSkipped { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
