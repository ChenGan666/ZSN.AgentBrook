using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Entity.Model
{
    public class FilesExtension
    {
        /// <summary>
        /// 图片扩展名及对应的MIME类型字典
        /// </summary>
        public static Dictionary<string, string> ImageExtensionMimeTypes = new()
        {
            { "jpg", "image/jpeg" },
            { "jpeg", "image/jpeg" },
            { "png", "image/png" },
            { "gif", "image/gif" },
            { "bmp", "image/bmp" },
            { "webp", "image/webp" }
        };

        /// <summary>
        /// 常见文件扩展名及对应的MIME类型字典
        /// </summary>
        public static Dictionary<string, string> FilesExtensionMimeTypes = new()
        {
            // 文本文件
            { "txt", "text/plain" },
            { "html", "text/html" },
            { "htm", "text/html" },
            { "css", "text/css" },
            { "csv", "text/csv" },
            { "xml", "text/xml" },
            { "json", "application/json" },
            
            // 文档文件
            { "pdf", "application/pdf" },
            { "doc", "application/msword" },
            { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { "xls", "application/vnd.ms-excel" },
            { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { "ppt", "application/vnd.ms-powerpoint" },
            { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { "odt", "application/vnd.oasis.opendocument.text" },
            { "ods", "application/vnd.oasis.opendocument.spreadsheet" },
            { "odp", "application/vnd.oasis.opendocument.presentation" },
            
            // 压缩文件
            { "zip", "application/zip" },
            { "rar", "application/vnd.rar" },
            { "7z", "application/x-7z-compressed" },
            { "tar", "application/x-tar" },
            { "gz", "application/gzip" },
            
            // 音频文件
            { "mp3", "audio/mpeg" },
            { "wav", "audio/wav" },
            { "ogg", "audio/ogg" },
            { "flac", "audio/flac" },
            { "aac", "audio/aac" },
            { "m4a", "audio/mp4" },
            
            // 视频文件
            { "mp4", "video/mp4" },
            { "avi", "video/x-msvideo" },
            { "mkv", "video/x-matroska" },
            { "mov", "video/quicktime" },
            { "wmv", "video/x-ms-wmv" },
            { "webm", "video/webm" },
            
            // 源代码文件
            { "js", "text/javascript" },
            { "py", "text/x-python" },
            { "java", "text/x-java" },
            { "c", "text/x-c" },
            { "cpp", "text/x-c++" },
            { "cs", "text/x-csharp" },
            { "php", "application/x-httpd-php" },
            { "rb", "text/x-ruby" },
            { "go", "text/x-go" },
            { "ts", "text/typescript" },
            
            // 字体文件
            { "ttf", "font/ttf" },
            { "otf", "font/otf" },
            { "woff", "font/woff" },
            { "woff2", "font/woff2" },
            
            // 其他常见文件
            { "svg", "image/svg+xml" },
            { "ico", "image/x-icon" },
            { "markdown", "text/markdown" },
            { "md", "text/markdown" }
        };
    }
}
