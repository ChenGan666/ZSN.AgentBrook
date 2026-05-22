using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Node.Utils;
using ZSN.AI.Node.Utils.Pipeline;
using ZSN.AI.Node.Claw.Pipeline;
using ZSN.AI.Node.VoiceNode.Interfaces;
using ZSN.AI.Node.VoiceNode.Models;
using ZSN.AI.Node.VoiceNode.Services;
using ZSN.AI.Service.Helpers;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AI.Node.VoiceNode
{
    /// <summary>
    /// 语音节点执行器
    /// </summary>
    public class ExecutionVoice : BaseExecution
    {
        private readonly IVoiceProviderFactory _providerFactory;
        private readonly IAudioPreprocessor _audioPreprocessor;
        private readonly IOptions<VoiceNodeOptions> _nodeOptions;
        private readonly ILogger<ExecutionVoice> _voiceLogger;
        private readonly List<string> _tempFiles = new();

        public ExecutionVoice(
            IChatService chatService,
            IServiceProvider provider,
            ILogger<ExecutionVoice> logger,
            IVoiceProviderFactory providerFactory,
            IAudioPreprocessor audioPreprocessor,
            IOptions<VoiceNodeOptions> nodeOptions)
            : base(chatService, provider, logger)
        {
            _providerFactory = providerFactory;
            _audioPreprocessor = audioPreprocessor;
            _nodeOptions = nodeOptions;
            _voiceLogger = logger;
        }

        /// <summary>
        /// 语音节点主执行方法
        /// </summary>
        public async Task<string> VoiceNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            var outputs = new List<Output>();
            var Logs = new ConcurrentQueue<string>();
            ExecutionRecordStatus ExecutionRecordStatus = ExecutionRecordStatus.Success;

            string AppID = data.AppID;
            string TaskID = data.TaskID;
            string SessionID = data.SessionID;
            string ProcessesID = data.ProcessesID.IsNullOrEmpty() ? Guid.NewGuid().ToString() : data.ProcessesID;
            string MemberID = data.MemberID.IsNullOrEmpty() ? "system" : data.MemberID;
            string FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;

            RecordID = Utils.Utils.newExcutionRecord(
                SessionID, config, ProcessesID, TaskID,
                FromMainTaskID: FromMainTaskID, inputs: inputs);

            using var throttler = new RecordUpdateThrottler(
                RecordID, outputs, Logs,
                (rid, status, outs, logs) => Utils.Utils.updateExcutionRecord(rid, status, outs, logs),
                intervalMs: 500);

            string streamKey = StreamKey.Build(SessionID, ProcessesID);
            using var batchWriter = new StreamBatchWriter(
                _streamSync, streamKey, SessionID, ProcessesID, TaskID, config.id, intervalMs: 200);

            var progress = new Progress<VoiceProgress>(p =>
            {
                batchWriter.Append($"[{p.Stage}] {p.Message} ({p.Percentage}%)\n");
                Logs.Enqueue($"[{p.Stage}] {p.Message} ({p.Percentage}%)");
                throttler.MarkDirty();
            });

            // 超时保护
            var timeoutMinutes = _nodeOptions.Value.MaxProcessingTimeMinutes > 0
                ? _nodeOptions.Value.MaxProcessingTimeMinutes : 15;
            using var overallCts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));

            try
            {
                Logs.Enqueue("=== Voice 节点开始执行 ===");
                batchWriter.Append("\n=== Voice 节点开始执行 ===\n");
                throttler.MarkDirty();

                // ── 1. 解析配置 + 占位符替换 ──
                var nodeData = JsonConvert.DeserializeObject<VoiceNodeData>(config.data.ToString());
                if (nodeData == null) throw new Exception("Voice 节点配置解析失败");

                var promptCache = this.BuildPromptReplaceCache(inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
                nodeData.AudioSource = await this.ReplacePromptValueCached(nodeData.AudioSource, promptCache, SessionID, AppID, ProcessesID);
                nodeData.prompt = await this.ReplacePromptValueCached(nodeData.prompt, promptCache, SessionID, AppID, ProcessesID);

                Logs.Enqueue($"[Init] Provider: {nodeData.Provider ?? "默认"}, 语言: {nodeData.Language}");
                throttler.MarkDirty();

                // ── 2. 获取音频文件路径 ──
                string audioFilePath = ResolveAudioSource(nodeData, data);
                Logs.Enqueue($"[Init] 音频来源: {audioFilePath}");
                batchWriter.Append($"音频来源: {Path.GetFileName(audioFilePath)}\n");

                // ── 3. 音频预处理 ──
                batchWriter.Append("正在进行音频预处理...\n");
                throttler.MarkDirty();

                var preprocessResult = await _audioPreprocessor.PreprocessAsync(audioFilePath,
                    new AudioPreprocessOptions
                    {
                        MaxFileSizeMb = _nodeOptions.Value.MaxFileSizeMb,
                        MaxDurationSeconds = nodeData.MaxAudioDurationSeconds,
                        AutoSegmentThresholdSeconds = _nodeOptions.Value.AutoSegmentThresholdSeconds
                    }, overallCts.Token);

                if (preprocessResult.RequiresCleanup && !string.IsNullOrEmpty(preprocessResult.ProcessedFilePath))
                    _tempFiles.Add(preprocessResult.ProcessedFilePath);

                batchWriter.Append($"音频预处理完成: 时长={preprocessResult.DurationSeconds:F1}s, 格式转换={preprocessResult.WasConverted}\n");
                Logs.Enqueue($"[Preprocess] 时长={preprocessResult.DurationSeconds:F1}s, 转换={preprocessResult.WasConverted}");

                // ── 4. 获取可用 Provider ──
                var provider = await _providerFactory.GetAvailableProviderAsync(
                    nodeData.Provider ?? _nodeOptions.Value.DefaultProvider, overallCts.Token);

                batchWriter.Append($"使用转写服务: {provider.ProviderName}\n");
                Logs.Enqueue($"[Provider] 使用: {provider.ProviderName}");

                // ── 5. 执行转写 ──
                var transcribeRequest = BuildTranscribeRequest(nodeData, preprocessResult);
                TranscriptionResult result;

                if (preprocessResult.Segments?.Count > 1)
                {
                    batchWriter.Append($"长音频分段转写: {preprocessResult.Segments.Count} 段\n");
                    result = await TranscribeSegmentsAsync(provider, preprocessResult.Segments, transcribeRequest.Options, preprocessResult.ProcessedFilePath, progress, overallCts.Token);
                }
                else
                {
                    result = await provider.TranscribeAsync(transcribeRequest, progress, overallCts.Token);
                }

                _providerFactory.RecordSuccess(provider.ProviderName);

                batchWriter.Append($"\n转写完成: {result.Segments.Count} 个分段, {result.Speakers.Count} 个说话人\n");
                Logs.Enqueue($"[Transcribe] 完成: {result.Segments.Count}段, {result.Speakers.Count}说话人, 耗时{result.ProcessingTimeMs}ms");

                // ── 6. 后处理管道 ──

                // 6.1 说话人标签映射
                SpeakerLabelNormalizer.Normalize(result, nodeData.SpeakerLabelMap);

                // 6.2 格式化输出
                string formattedOutput = OutputFormatter.Format(result, nodeData.OutputFormat);

                // 6.3 LLM 后处理（可选）
                string finalResult = formattedOutput;
                if (nodeData.EnablePostProcessing && !string.IsNullOrWhiteSpace(nodeData.prompt))
                {
                    batchWriter.Append("\n--- LLM 后处理 ---\n");
                    throttler.MarkDirty();
                    finalResult = await LLMPostProcessAsync(nodeData, formattedOutput, result, batchWriter, throttler, overallCts.Token);
                }

                // ── 7. 写入 Output ──
                outputs.Add(new Output { varname = "results", value = finalResult, nodeId = config.id, sourceId = $"{config.id}_results" });
                outputs.Add(new Output { varname = "transcription", value = formattedOutput, nodeId = config.id, sourceId = $"{config.id}_transcription" });
                outputs.Add(new Output { varname = "duration", value = result.DurationSeconds.ToString("F1"), nodeId = config.id, sourceId = $"{config.id}_duration" });
                outputs.Add(new Output { varname = "speakerCount", value = result.Speakers.Count.ToString(), nodeId = config.id, sourceId = $"{config.id}_speakerCount" });
                outputs.Add(new Output { varname = "provider", value = result.Provider ?? provider.ProviderName, nodeId = config.id, sourceId = $"{config.id}_provider" });

                throttler.MarkDirty();

                // ── 8. 触发下游节点 ──
                Logs.Enqueue("[NextNode] 准备触发下一节点");
                WorkflowNodeInfoBussiness.NextNode(
                    AppID, SessionID, ProcessesID, TaskID, FromMainTaskID,
                    AgentNodeID: "", config, inputs, outputs, Logs.ToList());
                Logs.Enqueue("[NextNode] 下一节点已触发");
            }
            catch (OperationCanceledException)
            {
                _voiceLogger.LogWarning("[VoiceNode] 执行超时 ({Timeout}分钟)", timeoutMinutes);
                Logs.Enqueue($"[Error] Voice 节点执行超时（{timeoutMinutes}分钟）");
                batchWriter.Append($"\nVoice 节点执行超时（{timeoutMinutes}分钟）\n");

                outputs.Add(new Output { varname = "results", value = "语音处理超时", nodeId = config.id });
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }
            catch (Exception ex)
            {
                _voiceLogger.LogError(ex, "[VoiceNode] 执行异常 - SessionID: {SessionID}", data.SessionID);
                Logs.Enqueue($"[Error] {ex.Message}");
                batchWriter.Append($"\nVoice 节点执行失败: {ex.Message}\n");

                outputs.Add(new Output { varname = "results", value = $"语音处理失败: {ex.Message}", nodeId = config.id });
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }
            finally
            {
                CleanupTempFiles();
            }

            throttler.FlushWithStatus(ExecutionRecordStatus);
            return RecordID;
        }

        /// <summary>
        /// 解析音频来源
        /// </summary>
        private string ResolveAudioSource(VoiceNodeData nodeData, TaskData data)
        {
            // 优先使用 AudioSource 配置
            if (!string.IsNullOrWhiteSpace(nodeData.AudioSource))
                return nodeData.AudioSource;

            // 其次从 AttachmentItems 中获取音频文件
            if (data.AttachmentItems?.Count > 0)
            {
                var supportedFormats = _nodeOptions.Value.SupportedFormats ?? Array.Empty<string>();
                var audioAttachment = data.AttachmentItems.FirstOrDefault(a =>
                    supportedFormats.Any(f => (a.Name ?? "").EndsWith(f, StringComparison.OrdinalIgnoreCase)));
                if (audioAttachment != null)
                    return audioAttachment.FilePath ?? audioAttachment.FileURI;
            }

            throw new Exception("未找到音频来源，请配置 AudioSource 或上传音频附件");
        }

        /// <summary>
        /// 构建转写请求
        /// </summary>
        private TranscribeRequest BuildTranscribeRequest(VoiceNodeData nodeData, AudioPreprocessResult preprocessResult)
        {
            return new TranscribeRequest
            {
                AudioFilePath = preprocessResult.ProcessedFilePath,
                OriginalFileName = Path.GetFileName(preprocessResult.ProcessedFilePath),
                DurationSeconds = preprocessResult.DurationSeconds,
                Options = new VoiceTranscriptionOptions
                {
                    EnableSpeakerDiarization = nodeData.EnableSpeakerDiarization,
                    ExpectedSpeakerCount = nodeData.ExpectedSpeakerCount,
                    EnableEmotionDetection = nodeData.EnableEmotionDetection,
                    EnableAudioEventDetection = nodeData.EnableAudioEventDetection,
                    Language = nodeData.Language,
                    Hotwords = nodeData.Hotwords
                }
            };
        }

        /// <summary>
        /// 长音频多段并行转写
        /// </summary>
        private async Task<TranscriptionResult> TranscribeSegmentsAsync(
            IVoiceTranscriptionProvider provider,
            List<AudioSegmentInfo> segments,
            VoiceTranscriptionOptions options,
            string sourceFilePath,
            IProgress<VoiceProgress> progress,
            CancellationToken cancellationToken)
        {
            var semaphore = new SemaphoreSlim(_nodeOptions.Value.MaxConcurrentSegments);
            var results = new TranscriptionResult[segments.Count];
            int completed = 0;

            var tasks = segments.Select(async (segment, index) =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    // 如果分段文件不存在（固定时长降级模式），按需切割
                    var segPath = segment.FilePath;
                    if (!File.Exists(segPath))
                    {
                        segPath = await CutSegmentAsync(sourceFilePath, segment, cancellationToken);
                        segment.FilePath = segPath;
                        _tempFiles.Add(segPath);
                    }

                    var request = new TranscribeRequest
                    {
                        AudioFilePath = segPath,
                        DurationSeconds = segment.DurationSeconds,
                        Options = options
                    };
                    results[index] = await provider.TranscribeAsync(request, null, cancellationToken);
                    Interlocked.Increment(ref completed);
                    progress?.Report(new VoiceProgress
                    {
                        Stage = "Transcribing",
                        Message = $"分段转写 {completed}/{segments.Count}",
                        Percentage = 20 + (int)(60.0 * completed / segments.Count)
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            return MergeSegmentResults(results, segments);
        }

        /// <summary>
        /// 合并分段转写结果
        /// </summary>
        private TranscriptionResult MergeSegmentResults(TranscriptionResult[] results, List<AudioSegmentInfo> segments)
        {
            var merged = new TranscriptionResult
            {
                Segments = new List<TranscriptionSegment>(),
                Speakers = new List<SpeakerInfo>(),
                Provider = results.FirstOrDefault(r => r != null)?.Provider
            };

            var fullTextBuilder = new StringBuilder();
            double totalDuration = 0;

            for (int i = 0; i < results.Length; i++)
            {
                var result = results[i];
                if (result == null) continue;

                var timeOffset = (long)(segments[i].StartTimeSeconds * 1000);
                foreach (var seg in result.Segments)
                {
                    seg.StartTimeMs += timeOffset;
                    seg.EndTimeMs += timeOffset;
                    merged.Segments.Add(seg);
                }

                fullTextBuilder.AppendLine(result.FullText);
                totalDuration = Math.Max(totalDuration, segments[i].EndTimeSeconds);
            }

            merged.FullText = fullTextBuilder.ToString().Trim();
            merged.DurationSeconds = totalDuration;

            // 合并说话人信息
            var speakerMap = new Dictionary<string, SpeakerInfo>();
            foreach (var result in results.Where(r => r != null))
            {
                foreach (var speaker in result.Speakers)
                {
                    if (!speakerMap.ContainsKey(speaker.SpeakerId))
                        speakerMap[speaker.SpeakerId] = new SpeakerInfo { SpeakerId = speaker.SpeakerId };
                    speakerMap[speaker.SpeakerId].SegmentCount += speaker.SegmentCount;
                    speakerMap[speaker.SpeakerId].TotalSpeakingSeconds += speaker.TotalSpeakingSeconds;
                }
            }
            merged.Speakers = speakerMap.Values.ToList();

            return merged;
        }

        /// <summary>
        /// LLM 后处理
        /// </summary>
        private async Task<string> LLMPostProcessAsync(
            VoiceNodeData nodeData, string transcription, TranscriptionResult result,
            StreamBatchWriter batchWriter, RecordUpdateThrottler throttler,
            CancellationToken cancellationToken)
        {
            try
            {
                // 替换提示词内置占位符
                var prompt = nodeData.prompt
                    .Replace("{{transcription}}", transcription)
                    .Replace("{{duration}}", result.DurationSeconds.ToString("F1"))
                    .Replace("{{speakerCount}}", result.Speakers.Count.ToString())
                    .Replace("{{speakers}}", JsonConvert.SerializeObject(result.Speakers));

                // 构建 LLM 配置
                var modelConfig = BuildModelConfig(nodeData);
                if (modelConfig == null)
                {
                    _voiceLogger.LogWarning("[VoiceNode] 无可用 LLM 模型，跳过后处理");
                    return transcription;
                }

                // 构建聊天历史
                var history = new ChatHistory();
                history.AddSystemMessage(prompt);
                history.AddUserMessage("请开始处理");

                var sb = new StringBuilder();
                var streamProgress = new Progress<string>(delta =>
                {
                    sb.Append(delta);
                    batchWriter.Append(delta);
                    throttler.MarkDirty();
                });

                await foreach (var _ in _chatService.SendChatAsync(
                    modelConfig, history, enableStreamingObservation: true,
                    progress: streamProgress, ct: cancellationToken))
                {
                    // 流式 token 通过 progress 回调接收，此处仅驱动枚举
                }

                throttler.MarkDirty();
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _voiceLogger.LogWarning(ex, "[VoiceNode] LLM 后处理失败，降级为原始转写文本");
                batchWriter.Append("\n[LLM 后处理失败，使用原始转写文本]\n");
                return transcription;
            }
        }

        /// <summary>
        /// 构建模型配置，优先使用节点配置的模型，否则回退到系统默认模型
        /// </summary>
        private LargeModelConfig BuildModelConfig(VoiceNodeData nodeData)
        {
            LargeModelInfo modelInfo = null;

            // 优先使用节点配置的模型
            if (nodeData.model?.LargeModelID > 0)
            {
                modelInfo = LargeModelInfoBussiness.GetModel(nodeData.model.LargeModelID);
            }

            // 回退到系统默认模型
            if (modelInfo == null)
            {
                modelInfo = LargeModelInfoBussiness.GetDefaultModel();
            }

            if (modelInfo == null ||
                string.IsNullOrWhiteSpace(modelInfo.ModelName) ||
                string.IsNullOrWhiteSpace(modelInfo.ModelKey) ||
                string.IsNullOrWhiteSpace(modelInfo.EndPoint))
            {
                return null;
            }

            return new LargeModelConfig
            {
                Model = modelInfo,
                Temperature = nodeData.temperature,
                TopPCoefficient = nodeData.topp,
            };
        }

        /// <summary>
        /// 用 FFmpeg 按时间范围切割音频段（固定时长降级模式使用）
        /// </summary>
        private async Task<string> CutSegmentAsync(string sourcePath, AudioSegmentInfo segment, CancellationToken ct)
        {
            var tempDir = string.IsNullOrEmpty(_nodeOptions.Value.TempFileDirectory)
                ? Path.GetTempPath()
                : _nodeOptions.Value.TempFileDirectory;
            var outputPath = Path.Combine(tempDir, $"voice_seg_{Guid.NewGuid():N}.wav");

            var ffmpeg = string.IsNullOrEmpty(_nodeOptions.Value.FFmpegPath) ? "ffmpeg" : _nodeOptions.Value.FFmpegPath;
            var args = $"-y -i \"{sourcePath}\" -ss {segment.StartTimeSeconds:F3} -to {segment.EndTimeSeconds:F3} " +
                       $"-ar 16000 -ac 1 -sample_fmt s16 \"{outputPath}\"";

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
            using var reg = ct.Register(() => { try { process.Kill(true); } catch { } });
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0 || !File.Exists(outputPath))
                throw new Exception($"音频分段切割失败 (ExitCode={process.ExitCode})");

            return outputPath;
        }

        /// <summary>
        /// 清理临时文件
        /// </summary>
        private void CleanupTempFiles()
        {
            foreach (var file in _tempFiles)
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                catch (Exception ex)
                {
                    _voiceLogger.LogDebug(ex, "[VoiceNode] 清理临时文件失败: {File}", file);
                }
            }
            _tempFiles.Clear();
        }
    }
}
