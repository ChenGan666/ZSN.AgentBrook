using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Node.ServiceDesk.Interfaces;
using ZSN.AI.Node.ServiceDesk.Models;
using ZSN.AI.Node.ServiceDesk.Services;
using ZSN.AI.Node.Utils;
using ZSN.AI.Node.Utils.Pipeline;
using ZSN.AI.Node.Claw.Pipeline;
using ZSN.AI.Service.Helpers;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AI.Node.ServiceDesk
{
    /// <summary>
    /// ServiceDesk 节点执行器
    /// 通过 FunctionCall 让 LLM 自主调用知识库检索工具，实现检索+生成一体化
    /// </summary>
    public class ExecutionServiceDesk : BaseExecution
    {
        private readonly IRequestClassifier _requestClassifier;
        private readonly IResponseGenerator _responseGenerator;
        private readonly ISessionStateManager _sessionStateManager;

        public ExecutionServiceDesk(
            IChatService chatService,
            IServiceProvider provider,
            ILogger<ExecutionServiceDesk> logger,
            IRequestClassifier requestClassifier,
            IResponseGenerator responseGenerator,
            ISessionStateManager sessionStateManager)
            : base(chatService, provider, logger)
        {
            _requestClassifier = requestClassifier;
            _responseGenerator = responseGenerator;
            _sessionStateManager = sessionStateManager;
        }

        /// <summary>
        /// ServiceDesk 节点主执行方法
        /// </summary>
        public async Task<string> ServiceDeskNodeAsync(NodeConfig config, TaskData data)
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

            // 流式输出
            string streamKey = StreamKey.Build(SessionID, ProcessesID);
            using var batchWriter = new StreamBatchWriter(
                _streamSync, streamKey, SessionID, ProcessesID, TaskID, config.id, intervalMs: 200);

            try
            {
                Logs.Enqueue("=== ServiceDesk 节点开始执行 ===");
                batchWriter.Append("\n=== ServiceDesk 节点开始执行 ===");
                throttler.MarkDirty();

                // ── 1. 初始化：解析配置 ──
                var nodeData = JsonConvert.DeserializeObject<ServiceDeskData>(config.data.ToString());
                if (nodeData == null)
                    throw new Exception("ServiceDesk 节点配置解析失败");

                // 替换 prompt 中的变量
                var promptCache = this.BuildPromptReplaceCache(inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
                nodeData.prompt = await this.ReplacePromptValueCached(nodeData.prompt, promptCache, SessionID, AppID, ProcessesID);
                string userMessage = nodeData.prompt;

                if (string.IsNullOrWhiteSpace(userMessage))
                {
                    throw new Exception("用户消息为空,无法处理请求。请检查 prompt 配置或输入参数。");
                }

                Logs.Enqueue($"[Init] 用户消息: {userMessage}");
                throttler.MarkDirty();

                // ── 2. 获取会话状态 ──
                var sessionState = await _sessionStateManager.GetOrCreateSessionStateAsync(
                    SessionID, AppID, MemberID);

                // 如果当前在信息收集状态，先处理信息收集
                if (sessionState.CurrentState == SessionState.InformationGathering)
                {
                    var intentRule = nodeData.IntentRules?.FirstOrDefault(r => r.IntentName == sessionState.DetectedIntent);
                    if (intentRule != null)
                    {
                        sessionState = await _sessionStateManager.CollectInformationAsync(
                            sessionState, userMessage, intentRule);

                        if (sessionState.MissingFields?.Count > 0)
                        {
                            var promptMsg = _sessionStateManager.GeneratePromptForMissingFields(sessionState);
                            batchWriter.Append(promptMsg);
                            batchWriter.Flush();

                            outputs.Add(new Output { varname = "response", value = promptMsg, nodeId = config.id });
                            outputs.Add(new Output { varname = "strategy", value = "InformationGathering", nodeId = config.id });

                            throttler.FlushWithStatus(ExecutionRecordStatus);
                            return RecordID;
                        }
                    }
                }

                // ── 3. 快速分类（问候/闲聊直接回复，其余走 FunctionCall） ──
                var memoryContext = BuildSimpleMemoryContext(sessionState);
                var classification = await _requestClassifier.ClassifyRequestAsync(
                    userMessage, memoryContext, nodeData);

                Logs.Enqueue($"[Classification] Type={classification.Type}, Strategy={classification.Strategy}, Confidence={classification.Confidence:F2}");
                throttler.MarkDirty();

                ServiceDeskResponse response;

                if (classification.Strategy == ProcessingStrategy.DirectReply)
                {
                    // 问候/闲聊快速回复，不需要知识库
                    response = _responseGenerator.GenerateDirectReply(
                        userMessage, classification, batchWriter);

                    Logs.Enqueue($"[Response] DirectReply, Length={response.Content?.Length ?? 0}");
                }
                else
                {
                    // 加载对话历史（多轮对话上下文）
                    var chatHistory = new List<ChatMessageRecord>();
                    try
                    {
                        var appChatLogs = AppChatLogInfoBussiness.GetListBySessionID(AppID, SessionID);
                        if (appChatLogs != null && appChatLogs.Count > 0)
                        {
                            foreach (var log in appChatLogs.TakeLast(10))
                            {
                                var role = log.Role ?? "user";
                                var content = log.Content?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(content))
                                {
                                    chatHistory.Add(new ChatMessageRecord
                                    {
                                        Role = role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user",
                                        Content = content
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[ServiceDesk] 加载对话历史失败");
                    }

                    // FunctionCall 路径：LLM 自主调用知识库检索 + 生成回答
                    response = await _responseGenerator.GenerateFunctionCallResponseAsync(
                        userQuery: userMessage,
                        config: nodeData,
                        streamWriter: batchWriter,
                        chatHistory: chatHistory);

                    Logs.Enqueue($"[Response] FunctionCall, Length={response.Content?.Length ?? 0}, Elapsed={response.ElapsedMs}ms");
                }

                throttler.MarkDirty();

                // ── 4. 状态更新（检测到意图时） ──
                if (!string.IsNullOrEmpty(classification.Intent) && nodeData.IntentRules?.Count > 0)
                {
                    var intentRule = nodeData.IntentRules.FirstOrDefault(r => r.IntentName == classification.Intent);
                    if (intentRule != null && intentRule.RequiredFields?.Count > 0)
                    {
                        sessionState.DetectedIntent = classification.Intent;
                        sessionState = await _sessionStateManager.CollectInformationAsync(
                            sessionState, userMessage, intentRule);

                        if (sessionState.MissingFields?.Count > 0)
                        {
                            var missingPrompt = _sessionStateManager.GeneratePromptForMissingFields(sessionState);
                            if (!string.IsNullOrEmpty(missingPrompt))
                            {
                                batchWriter.Append("\n\n" + missingPrompt);
                                batchWriter.Flush();
                            }
                        }

                        Logs.Enqueue($"[SessionState] State={sessionState.CurrentState}, Missing={sessionState.MissingFields?.Count ?? 0}");
                        throttler.MarkDirty();
                    }
                }

                // ── 5. 输出变量 ──
                outputs.Add(new Output { varname = "response", value = response.Content ?? "", nodeId = config.id });
                outputs.Add(new Output { varname = "confidence", value = response.Confidence.ToString("F2"), nodeId = config.id });
                outputs.Add(new Output { varname = "strategy", value = response.Strategy.ToString(), nodeId = config.id });
                outputs.Add(new Output { varname = "needsEscalation", value = response.NeedsEscalation.ToString(), nodeId = config.id });

                throttler.MarkDirty();

                // ── 6. 触发下一节点 ──
                Logs.Enqueue("[NextNode] 准备触发下一节点");
                WorkflowNodeInfoBussiness.NextNode(
                    AppID, SessionID, ProcessesID, TaskID, FromMainTaskID,
                    AgentNodeID: "", config, inputs, outputs, Logs.ToList());
                Logs.Enqueue("[NextNode] 下一节点已触发");
                throttler.MarkDirty();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ServiceDesk] 执行失败 - SessionID: {data.SessionID}");
                Logs.Enqueue($"\n执行失败: {ex.Message}");

                outputs.Add(new Output { varname = "response", value = "抱歉，系统处理出现异常，请稍后重试。", nodeId = config.id });
                outputs.Add(new Output { varname = "isFallback", value = "true", nodeId = config.id });

                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            throttler.FlushWithStatus(ExecutionRecordStatus);
            return RecordID;
        }

        /// <summary>从会话状态构建简化的记忆上下文</summary>
        private MemoryContext BuildSimpleMemoryContext(SessionStateContext sessionState)
        {
            var context = new MemoryContext();

            if (sessionState?.CollectedInfo?.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var kvp in sessionState.CollectedInfo)
                {
                    sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
                context.LastTopic = sessionState.DetectedIntent;
            }

            return context;
        }
    }
}
