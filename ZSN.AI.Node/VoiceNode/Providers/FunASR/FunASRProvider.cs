using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Text;
using ZSN.AI.Node.VoiceNode.Interfaces;
using ZSN.AI.Node.VoiceNode.Models;

namespace ZSN.AI.Node.VoiceNode.Providers.FunASR
{
    /// <summary>
    /// FunASR WebSocket 转写 Provider（离线模式）
    /// </summary>
    public class FunASRProvider : IVoiceTranscriptionProvider
    {
        private readonly FunASROptions _options;
        private readonly ILogger<FunASRProvider> _logger;

        public FunASRProvider(IOptions<FunASROptions> options, ILogger<FunASRProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public string ProviderName => "FunASR";

        public bool SupportsFeature(VoiceFeature feature) =>
            feature.HasFlag(VoiceFeature.FileTranscription) ||
            feature.HasFlag(VoiceFeature.SpeakerDiarization) ||
            feature.HasFlag(VoiceFeature.EmotionDetection) ||
            feature.HasFlag(VoiceFeature.PunctuationRestoration) ||
            feature.HasFlag(VoiceFeature.HotwordBoosting);

        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var ws = new ClientWebSocket();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.ConnectTimeoutSeconds));
                await ws.ConnectAsync(new Uri(_options.ServerUrl), cts.Token);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[FunASR] 健康检查失败");
                return false;
            }
        }

        public async Task<TranscriptionResult> TranscribeAsync(
            TranscribeRequest request,
            IProgress<VoiceProgress> progress,
            CancellationToken cancellationToken)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            using var ws = new ClientWebSocket();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(_options.TranscribeTimeoutMinutes));

            // 1. 连接
            progress?.Report(new VoiceProgress
            {
                Stage = "Transcribing",
                Message = "连接 FunASR Server...",
                Percentage = 5
            });

            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            connectCts.CancelAfter(TimeSpan.FromSeconds(_options.ConnectTimeoutSeconds));
            await ws.ConnectAsync(new Uri(_options.ServerUrl), connectCts.Token);

            _logger.LogInformation("[FunASR] WebSocket 已连接: {Url}", _options.ServerUrl);

            // 2. 发送初始化 JSON
            var initMessage = new Dictionary<string, object>
            {
                ["mode"] = "offline",
                ["wav_name"] = Path.GetFileName(request.AudioFilePath),
                ["is_speaking"] = true,
                ["wav_format"] = "pcm",
                ["audio_fs"] = 16000,
                ["itn"] = true,
                ["sentence_time"] = true
            };

            if (request.Options?.Hotwords?.Count > 0)
                initMessage["hotwords"] = request.Options.Hotwords;

            var initJson = JsonConvert.SerializeObject(initMessage);
            var initBytes = Encoding.UTF8.GetBytes(initJson);
            await ws.SendAsync(new ArraySegment<byte>(initBytes), WebSocketMessageType.Text, true, cts.Token);

            // 3. 分片发送音频
            progress?.Report(new VoiceProgress
            {
                Stage = "Transcribing",
                Message = "发送音频数据...",
                Percentage = 20
            });

            var audioData = await File.ReadAllBytesAsync(request.AudioFilePath, cts.Token);
            int totalChunks = (int)Math.Ceiling((double)audioData.Length / _options.ChunkSize);
            int chunksSent = 0;

            for (int offset = 0; offset < audioData.Length; offset += _options.ChunkSize)
            {
                int length = Math.Min(_options.ChunkSize, audioData.Length - offset);
                await ws.SendAsync(
                    new ArraySegment<byte>(audioData, offset, length),
                    WebSocketMessageType.Binary, true, cts.Token);
                chunksSent++;

                int pct = 20 + (int)(60.0 * chunksSent / totalChunks);
                progress?.Report(new VoiceProgress
                {
                    Stage = "Transcribing",
                    Message = $"发送音频 {chunksSent}/{totalChunks}",
                    Percentage = pct
                });
            }

            // 4. 发送结束标记
            var endJson = JsonConvert.SerializeObject(new { is_speaking = false });
            await ws.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(endJson)),
                WebSocketMessageType.Text, true, cts.Token);

            _logger.LogInformation("[FunASR] 音频发送完成，等待转写结果...");

            // 5. 接收结果
            progress?.Report(new VoiceProgress
            {
                Stage = "Transcribing",
                Message = "等待转写结果...",
                Percentage = 85
            });

            var resultBuilder = new StringBuilder();
            var buffer = new byte[1024 * 256];

            while (ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult receiveResult;
                try
                {
                    receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                }
                catch (WebSocketException)
                {
                    break;
                }

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                    break;

                resultBuilder.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));

                if (receiveResult.EndOfMessage)
                    break;
            }

            var responseJson = resultBuilder.ToString();
            _logger.LogDebug("[FunASR] 收到响应: {Json}", responseJson.Length > 500 ? responseJson[..500] + "..." : responseJson);

            // 6. 解析结果
            progress?.Report(new VoiceProgress
            {
                Stage = "Transcribing",
                Message = "解析转写结果...",
                Percentage = 95
            });

            var result = ParseResponse(responseJson, request.Options?.EnableSpeakerDiarization ?? false);
            result.Provider = ProviderName;
            result.DurationSeconds = request.DurationSeconds;
            result.ProcessingTimeMs = sw.ElapsedMilliseconds;

            // 关闭连接
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                catch { /* 忽略关闭异常 */ }
            }

            progress?.Report(new VoiceProgress
            {
                Stage = "Transcribing",
                Message = "转写完成",
                Percentage = 100
            });

            _logger.LogInformation("[FunASR] 转写完成: 时长={Duration:F1}s, 耗时={Ms}ms, 段数={Segs}",
                result.DurationSeconds, result.ProcessingTimeMs, result.Segments.Count);

            return result;
        }

        private TranscriptionResult ParseResponse(string json, bool enableSpeakerDiarization)
        {
            var result = new TranscriptionResult();

            try
            {
                var response = JObject.Parse(json);
                result.FullText = response["text"]?.ToString() ?? "";

                // 解析 sentence_info（包含说话人分离和时间戳）
                var sentenceInfo = response["sentence_info"] as JArray;
                if (sentenceInfo != null)
                {
                    var speakerSet = new Dictionary<int, SpeakerInfo>();

                    foreach (var sentence in sentenceInfo)
                    {
                        int spk = sentence["spk"]?.Value<int>() ?? 0;
                        var segment = new TranscriptionSegment
                        {
                            Text = sentence["text"]?.ToString() ?? "",
                            StartTimeMs = sentence["start"]?.Value<long>() ?? 0,
                            EndTimeMs = sentence["end"]?.Value<long>() ?? 0,
                            SpeakerId = enableSpeakerDiarization ? $"Speaker_{spk}" : null,
                            Confidence = sentence["confidence"]?.Value<double>() ?? 1.0
                        };
                        result.Segments.Add(segment);

                        if (enableSpeakerDiarization)
                        {
                            if (!speakerSet.ContainsKey(spk))
                                speakerSet[spk] = new SpeakerInfo { SpeakerId = $"Speaker_{spk}" };

                            speakerSet[spk].SegmentCount++;
                            speakerSet[spk].TotalSpeakingSeconds += (segment.EndTimeMs - segment.StartTimeMs) / 1000.0;
                        }
                    }

                    result.Speakers = speakerSet.Values.ToList();
                }
                else
                {
                    // 没有 sentence_info，尝试从 timestamp 构建
                    var timestamps = response["timestamp"] as JArray;
                    if (timestamps != null && timestamps.Count > 0)
                    {
                        result.Segments.Add(new TranscriptionSegment
                        {
                            Text = result.FullText,
                            StartTimeMs = timestamps.First?.First?.Value<long>() ?? 0,
                            EndTimeMs = timestamps.Last?.Last?.Value<long>() ?? 0,
                            Confidence = 1.0
                        });
                    }
                }

                result.RawResponse = json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FunASR] 解析响应失败: {Json}", json.Length > 200 ? json[..200] : json);
                result.FullText = json;
            }

            return result;
        }
    }
}
