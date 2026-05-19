using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZSN.AI.Node.VoiceNode.Interfaces;
using ZSN.AI.Node.VoiceNode.Models;

namespace ZSN.AI.Node.VoiceNode.Services
{
    /// <summary>
    /// 音频预处理器实现
    /// </summary>
    public class AudioPreprocessor : IAudioPreprocessor
    {
        private readonly VoiceNodeOptions _options;
        private readonly ILogger<AudioPreprocessor> _logger;

        public AudioPreprocessor(IOptions<VoiceNodeOptions> options, ILogger<AudioPreprocessor> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<AudioPreprocessResult> PreprocessAsync(
            string inputPath,
            AudioPreprocessOptions options,
            CancellationToken cancellationToken = default)
        {
            var tempDir = string.IsNullOrEmpty(_options.TempFileDirectory)
                ? Path.GetTempPath()
                : _options.TempFileDirectory;
            Directory.CreateDirectory(tempDir);

            // 下载远程文件
            string localPath = inputPath;
            bool downloaded = false;
            if (inputPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                inputPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                localPath = await DownloadFileAsync(inputPath, tempDir, cancellationToken);
                downloaded = true;
            }

            if (!File.Exists(localPath))
                throw new FileNotFoundException($"音频文件不存在: {localPath}");

            // 文件大小校验
            var fileInfo = new FileInfo(localPath);
            if (fileInfo.Length > options.MaxFileSizeMb * 1024 * 1024)
                throw new Exception($"音频文件大小 ({fileInfo.Length / 1024.0 / 1024.0:F1}MB) 超过限制 ({options.MaxFileSizeMb}MB)");

            // 检测原始格式
            string originalFormat = Path.GetExtension(localPath).ToLowerInvariant();
            bool needsConversion = NeedsConversion(originalFormat);

            string processedPath = localPath;
            bool wasConverted = false;

            if (needsConversion)
            {
                processedPath = Path.Combine(tempDir, $"voice_{Guid.NewGuid():N}.wav");
                await ConvertToWavAsync(localPath, processedPath, options, cancellationToken);
                wasConverted = true;
            }

            // 获取音频时长
            double durationSeconds = await GetAudioDurationAsync(processedPath, cancellationToken);

            // 时长校验
            if (options.MaxDurationSeconds > 0 && durationSeconds > options.MaxDurationSeconds)
                throw new Exception($"音频时长 ({durationSeconds:F1}s) 超过限制 ({options.MaxDurationSeconds}s)");

            // 清理下载的临时文件（如果已转换为新文件）
            if (downloaded && wasConverted)
            {
                try { File.Delete(localPath); } catch { }
            }

            // VAD 分段（长音频超过阈值时自动分段）
            List<AudioSegmentInfo> segments = null;
            if (options.AutoSegmentThresholdSeconds > 0 && durationSeconds > options.AutoSegmentThresholdSeconds)
            {
                _logger.LogInformation("长音频 {Duration:F1}s 超过阈值 {Threshold}s，开始分段",
                    durationSeconds, options.AutoSegmentThresholdSeconds);
                segments = await SegmentAudioAsync(processedPath, durationSeconds, tempDir, cancellationToken);
            }

            var result = new AudioPreprocessResult
            {
                ProcessedFilePath = processedPath,
                DurationSeconds = durationSeconds,
                OriginalFormat = originalFormat,
                WasConverted = wasConverted,
                Segments = segments,
                RequiresCleanup = wasConverted || downloaded || (segments != null && segments.Count > 0)
            };

            return result;
        }

        private bool NeedsConversion(string extension)
        {
            return extension != ".wav" && extension != ".pcm";
        }

        private async Task<List<AudioSegmentInfo>> SegmentAudioAsync(
            string audioPath, double totalDuration, string tempDir, CancellationToken ct)
        {
            var segments = await SegmentBySilenceDetectionAsync(audioPath, totalDuration, tempDir, ct);
            if (segments != null && segments.Count > 1)
                return segments;

            return SegmentByFixedDuration(audioPath, totalDuration, tempDir);
        }

        private async Task<List<AudioSegmentInfo>> SegmentBySilenceDetectionAsync(
            string audioPath, double totalDuration, string tempDir, CancellationToken ct)
        {
            var ffmpeg = FindFFmpeg();
            var args = $"-i \"{audioPath}\" -af silencedetect=noise=-30dB:d=1.0 -f null -";

            string stderr;
            try
            {
                using var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                var errorTask = process.StandardError.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);
                stderr = await errorTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "silencedetect 执行失败");
                return null;
            }

            var silenceRanges = new List<(double Start, double End)>();
            double? silenceStart = null;

            foreach (var line in stderr.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("silence_start:"))
                {
                    var idx = trimmed.IndexOf("silence_start:");
                    var val = trimmed[(idx + "silence_start:".Length)..].Trim();
                    if (double.TryParse(val.Split('|', ' ')[0], out var start))
                        silenceStart = start;
                }
                else if (trimmed.Contains("silence_end:"))
                {
                    var idx = trimmed.IndexOf("silence_end:");
                    var val = trimmed[(idx + "silence_end:".Length)..].Trim();
                    if (double.TryParse(val.Split('|', ' ')[0], out var end) && silenceStart.HasValue)
                    {
                        silenceRanges.Add((silenceStart.Value, end));
                        silenceStart = null;
                    }
                }
            }

            if (silenceRanges.Count == 0)
                return null;

            var segments = new List<AudioSegmentInfo>();
            double segStart = 0;
            var baseName = Path.GetFileNameWithoutExtension(audioPath);
            var ext = Path.GetExtension(audioPath);

            for (int i = 0; i < silenceRanges.Count; i++)
            {
                var (silStart, silEnd) = silenceRanges[i];
                var cutPoint = (silStart + silEnd) / 2.0;
                var segDuration = cutPoint - segStart;

                if (segDuration < 5.0)
                    continue;

                var segPath = Path.Combine(tempDir, $"{baseName}_seg{i}{ext}");
                var ffmpegArgs = $"-y -i \"{audioPath}\" -ss {segStart:F3} -to {cutPoint:F3} -c copy \"{segPath}\"";

                try
                {
                    await RunProcessAsync(ffmpeg, ffmpegArgs, ct);
                    segments.Add(new AudioSegmentInfo
                    {
                        FilePath = segPath,
                        StartTimeSeconds = segStart,
                        EndTimeSeconds = cutPoint,
                        DurationSeconds = segDuration
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "分段 {Index} 切割失败", i);
                }

                segStart = cutPoint;
            }

            if (segStart < totalDuration - 1.0)
            {
                var segPath = Path.Combine(tempDir, $"{baseName}_seg{silenceRanges.Count}{ext}");
                var segDuration = totalDuration - segStart;
                var ffmpegArgs = $"-y -i \"{audioPath}\" -ss {segStart:F3} -c copy \"{segPath}\"";

                try
                {
                    await RunProcessAsync(ffmpeg, ffmpegArgs, ct);
                    segments.Add(new AudioSegmentInfo
                    {
                        FilePath = segPath,
                        StartTimeSeconds = segStart,
                        EndTimeSeconds = totalDuration,
                        DurationSeconds = segDuration
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "最后一段切割失败");
                }
            }

            return segments.Count > 0 ? segments : null;
        }

        private List<AudioSegmentInfo> SegmentByFixedDuration(
            string audioPath, double totalDuration, string tempDir)
        {
            var targetSeconds = 240.0;
            var segments = new List<AudioSegmentInfo>();
            var baseName = Path.GetFileNameWithoutExtension(audioPath);
            var ext = Path.GetExtension(audioPath);

            for (double start = 0; start < totalDuration; start += targetSeconds)
            {
                var end = Math.Min(start + targetSeconds, totalDuration);
                var segPath = Path.Combine(tempDir, $"{baseName}_fixed_{segments.Count}{ext}");

                segments.Add(new AudioSegmentInfo
                {
                    FilePath = segPath,
                    StartTimeSeconds = start,
                    EndTimeSeconds = end,
                    DurationSeconds = end - start
                });
            }

            return segments;
        }

        private async Task<string> DownloadFileAsync(string url, string tempDir, CancellationToken ct)
        {
            var fileName = Guid.NewGuid().ToString("N");
            try
            {
                var uri = new Uri(url);
                var ext = Path.GetExtension(uri.AbsolutePath);
                if (!string.IsNullOrEmpty(ext) && _options.SupportedFormats.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    fileName += ext;
                else
                    fileName += ".tmp";
            }
            catch
            {
                fileName += ".tmp";
            }

            var outputPath = Path.Combine(tempDir, fileName);

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            var data = await httpClient.GetByteArrayAsync(url, ct);
            await File.WriteAllBytesAsync(outputPath, data, ct);

            return outputPath;
        }

        private async Task ConvertToWavAsync(string inputPath, string outputPath,
            AudioPreprocessOptions options, CancellationToken ct)
        {
            var ffmpeg = FindFFmpeg();
            var args = $"-y -i \"{inputPath}\" -ar {options.TargetSampleRate} -ac {options.TargetChannels} " +
                       $"-sample_fmt s{options.TargetBitDepth} \"{outputPath}\"";

            await RunProcessAsync(ffmpeg, args, ct);
        }

        private async Task<double> GetAudioDurationAsync(string filePath, CancellationToken ct)
        {
            var ffprobe = FindFFprobe();
            var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"";

            var output = await RunProcessAsync(ffprobe, args, ct);
            if (double.TryParse(output?.Trim(), out var duration))
                return duration;

            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length / (16000.0 * 2);
        }

        private string FindFFmpeg()
        {
            if (!string.IsNullOrEmpty(_options.FFmpegPath))
                return _options.FFmpegPath;
            return "ffmpeg";
        }

        private string FindFFprobe()
        {
            if (!string.IsNullOrEmpty(_options.FFmpegPath))
            {
                var dir = Path.GetDirectoryName(_options.FFmpegPath);
                var name = Path.GetFileNameWithoutExtension(_options.FFmpegPath);
                return Path.Combine(dir, name.Replace("ffmpeg", "ffprobe") + Path.GetExtension(_options.FFmpegPath));
            }
            return "ffprobe";
        }

        private async Task<string> RunProcessAsync(string fileName, string arguments, CancellationToken ct)
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            using var reg = ct.Register(() =>
            {
                try { process.Kill(true); } catch { }
            });

            await process.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new Exception($"音频处理进程失败 (ExitCode={process.ExitCode}): {error}");
            }

            return output;
        }
    }
}
