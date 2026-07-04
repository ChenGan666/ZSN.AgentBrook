using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace ZSN.AgentBrook.AutoPublishJob.Pipeline
{
    /// <summary>
    /// 产物归档器：把构建产物(安装包/web目录)整理到产物输出根目录，
    /// 回写 PublishTaskInfo.ArtifactPath。
    ///
    /// 注：FilesInfo 入库(生成 FileCode)需要文件服务依赖，AutoPublishJob 为轻量独立服务，
    /// 首版只落盘 + 记录磁盘路径，ArtifactFileCode 留空，由 Web.Manage 下载端用 PhysicalFile 直接发。
    /// 二期可接入 FilesInfo 入库统一管理。
    /// </summary>
    public class ArtifactPublisher
    {
        private readonly IOptions<PublishJobOptions> _options;
        private readonly ILogger<ArtifactPublisher> _logger;

        public ArtifactPublisher(IOptions<PublishJobOptions> options, ILogger<ArtifactPublisher> logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// 归档产物。
        /// </summary>
        /// <param name="taskID">任务ID(用于命名归档子目录)</param>
        /// <param name="productName">应用产品名(用于命名产物文件)</param>
        /// <param name="artifactFiles">安装包文件绝对路径列表(可能为空，如纯 web)</param>
        /// <param name="webDir">web 产物目录(纯 web 构建时提供，将打包成 zip)</param>
        /// <returns>归档根目录绝对路径(含所有产物)</returns>
        public Task<string> PublishAsync(string taskID, string productName, System.Collections.Generic.List<string> artifactFiles, string? webDir, Action<LogEntry>? onLog, CancellationToken ct)
        {
            string outRoot = _options.Value.ArtifactOutputRoot;
            Directory.CreateDirectory(outRoot);
            string archiveDir = Path.Combine(outRoot, taskID);
            if (Directory.Exists(archiveDir)) Directory.Delete(archiveDir, true);
            Directory.CreateDirectory(archiveDir);

            // 安装包直接复制(保留原文件名)
            foreach (var f in artifactFiles)
            {
                ct.ThrowIfCancellationRequested();
                string dest = Path.Combine(archiveDir, Path.GetFileName(f));
                File.Copy(f, dest, overwrite: true);
                onLog?.Invoke(new LogEntry { Line = $"[Publish] 归档安装包: {Path.GetFileName(f)}" });
            }

            // web 产物压缩成 zip
            if (!string.IsNullOrWhiteSpace(webDir) && Directory.Exists(webDir))
            {
                ct.ThrowIfCancellationRequested();
                string safeName = string.IsNullOrWhiteSpace(productName) ? "app" : SanitizeFileName(productName);
                string zipPath = Path.Combine(archiveDir, $"{safeName}-web.zip");
                ZipFile.CreateFromDirectory(webDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
                onLog?.Invoke(new LogEntry { Line = $"[Publish] 归档 Web 站点: {Path.GetFileName(zipPath)}" });
            }

            onLog?.Invoke(new LogEntry { Line = $"[Publish] 产物归档完成: {archiveDir}" });
            return Task.FromResult(archiveDir);
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            return sb.ToString().Trim();
        }
    }
}
