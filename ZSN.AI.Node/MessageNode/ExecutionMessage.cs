using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Collections.Concurrent;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Node.Utils;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AI.Node.MessageNode
{
    /// <summary>
    /// MessageNode 执行器 — 通过 Redis 队列与 MessageGateway 解耦通信
    /// </summary>
    public class ExecutionMessage : BaseExecution
    {
        private readonly IDatabase _redis;
        private readonly IOptions<MessageNodeOptions> _nodeOptions;
        private readonly ILogger<ExecutionMessage> _messageLogger;

        public ExecutionMessage(
            IChatService chatService,
            IServiceProvider provider,
            ILogger<ExecutionMessage> logger,
            IConnectionMultiplexer redis,
            IOptions<MessageNodeOptions> nodeOptions)
            : base(chatService, provider, logger)
        {
            _redis = redis.GetDatabase();
            _nodeOptions = nodeOptions;
            _messageLogger = logger;
        }

        public async Task<string> MessageNodeAsync(NodeConfig config, TaskData data)
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

            List<string> recordIds = new List<string>();

            try
            {
                // 1. 解析配置
                var nodeData = JsonConvert.DeserializeObject<MessageNodeData>(config.data.ToString());
                if (nodeData == null) throw new Exception("Message 节点配置解析失败");

                var promptCache = this.BuildPromptReplaceCache(inputs, config.fromNodeId, SessionID, AppID, ProcessesID);

                // 2. 解析目标用户列表
                var targetUsers = await ResolveTargetUsersAsync(nodeData.TargetUserConfig, promptCache, SessionID, AppID, ProcessesID, inputs);
                if (targetUsers == null || targetUsers.Count == 0)
                    throw new Exception("未找到目标用户");

                Logs.Enqueue($"[Init] TargetUsers: {targetUsers.Count}");

                // 3. 消息内容占位符替换
                string template = await this.ReplacePromptValueCached(nodeData.MessageTemplate, promptCache, SessionID, AppID, ProcessesID);

                int successCount = 0;
                int failedCount = 0;
                var platformMsgIdMap = new Dictionary<string, string>();
                var errorList = new List<string>();

                // 4. 循环处理每个目标用户
                foreach (var user in targetUsers)
                {
                    string userContent = !string.IsNullOrEmpty(user.ContentOverride)
                        ? await this.ReplacePromptValueCached(user.ContentOverride, promptCache, SessionID, AppID, ProcessesID)
                        : template;

                    string sendRecordId = Guid.NewGuid().ToString();
                    var sendRecord = new MessageSendRecordInfo
                    {
                        RecordID = sendRecordId,
                        ChannelID = nodeData.ChannelID,
                        SessionID = SessionID,
                        TaskID = TaskID,
                        NodeID = config.id,
                        MessageType = nodeData.MessageType,
                        Content = userContent,
                        TargetUser = user.IMUserID,
                        SendStatus = 0,
                        CreateTime = DateTime.Now
                    };
                    MessageSendRecordBussiness.Add(sendRecord);
                    recordIds.Add(sendRecordId);

                    var sendTask = new
                    {
                        RecordID = sendRecordId,
                        ChannelID = nodeData.ChannelID,
                        MessageType = nodeData.MessageType,
                        Content = userContent,
                        TargetUser = user.IMUserID,
                        TargetName = user.IMUserName,
                        ExtraParams = nodeData.ExtraParams,
                        SessionID = SessionID,
                        TaskID = TaskID,
                        NodeID = config.id,
                        EnqueueTime = DateTime.Now
                    };

                    string taskJson = JsonConvert.SerializeObject(sendTask);
                    await _redis.ListLeftPushAsync(_nodeOptions.Value.SendQueueName, taskJson);
                }

                // 5. 判断 WaitForConfirmation
                if (nodeData.WaitForConfirmation)
                {
                    var waitResults = await WaitForAllSendResultsAsync(recordIds);

                    successCount = waitResults.Count(r => r.SendStatus == 1);
                    failedCount = waitResults.Count(r => r.SendStatus == -1);

                    foreach (var r in waitResults)
                    {
                        platformMsgIdMap[r.TargetUser] = r.PlatformMessageId ?? "";
                        if (!string.IsNullOrEmpty(r.ErrorMessage))
                            errorList.Add($"{r.TargetUser}: {r.ErrorMessage}");
                    }

                    string sendStatus;
                    if (successCount == recordIds.Count) sendStatus = "AllSuccess";
                    else if (successCount > 0) sendStatus = "PartialSuccess";
                    else if (failedCount == recordIds.Count) sendStatus = "AllFailed";
                    else sendStatus = "Timeout";

                    outputs.Add(new Output { varname = "sendSuccess", value = sendStatus, nodeId = config.id });
                    outputs.Add(new Output { varname = "sendCount", value = recordIds.Count.ToString(), nodeId = config.id });
                    outputs.Add(new Output { varname = "successCount", value = successCount.ToString(), nodeId = config.id });
                    outputs.Add(new Output { varname = "failedCount", value = failedCount.ToString(), nodeId = config.id });
                    outputs.Add(new Output { varname = "targetUsers", value = JsonConvert.SerializeObject(targetUsers.Select(u => u.IMUserID)), nodeId = config.id });
                    outputs.Add(new Output { varname = "platformMessageIds", value = JsonConvert.SerializeObject(platformMsgIdMap), nodeId = config.id });
                    outputs.Add(new Output { varname = "errorMessage", value = string.Join("; ", errorList), nodeId = config.id });
                    outputs.Add(new Output { varname = "results", value = $"批量发送 {recordIds.Count} 人，成功 {successCount} 人，失败 {failedCount} 人", nodeId = config.id });

                    if (successCount == 0) ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                }
                else
                {
                    outputs.Add(new Output { varname = "sendSuccess", value = "Queued", nodeId = config.id });
                    outputs.Add(new Output { varname = "sendCount", value = recordIds.Count.ToString(), nodeId = config.id });
                    outputs.Add(new Output { varname = "successCount", value = "0", nodeId = config.id });
                    outputs.Add(new Output { varname = "failedCount", value = "0", nodeId = config.id });
                    outputs.Add(new Output { varname = "targetUsers", value = JsonConvert.SerializeObject(targetUsers.Select(u => u.IMUserID)), nodeId = config.id });
                    outputs.Add(new Output { varname = "platformMessageIds", value = "{}", nodeId = config.id });
                    outputs.Add(new Output { varname = "errorMessage", value = "", nodeId = config.id });
                    outputs.Add(new Output { varname = "results", value = $"消息已入队 {recordIds.Count} 人，异步发送中", nodeId = config.id });
                }

                WorkflowNodeInfoBussiness.NextNode(
                    AppID, SessionID, ProcessesID, TaskID, FromMainTaskID,
                    AgentNodeID: "", config, inputs, outputs, Logs.ToList());
            }
            catch (Exception ex)
            {
                _messageLogger.LogError(ex, "[MessageNode] 执行异常 - SessionID: {SessionID}, TaskID: {TaskID}", SessionID, TaskID);
                Logs.Enqueue($"[Error] {ex.Message}");
                outputs.Add(new Output { varname = "sendSuccess", value = "False", nodeId = config.id });
                outputs.Add(new Output { varname = "results", value = $"消息发送失败: {ex.Message}", nodeId = config.id });
                outputs.Add(new Output { varname = "errorMessage", value = ex.Message, nodeId = config.id });
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs.ToList());
            return RecordID;
        }

        private async Task<List<TargetUserItem>> ResolveTargetUsersAsync(
            TargetUserConfig userConfig, PromptReplaceCache promptCache,
            string SessionID, string AppID, string ProcessesID, List<Inputs> inputs)
        {
            var result = new List<TargetUserItem>();
            if (userConfig == null) return result;

            switch (userConfig.SourceMode?.ToLowerInvariant())
            {
                case "static":
                    result = userConfig.UserList ?? new List<TargetUserItem>();
                    break;

                case "dynamic":
                    if (!string.IsNullOrEmpty(userConfig.SourceValue))
                    {
                        string rawValue = await this.ReplacePromptValueCached(userConfig.SourceValue, promptCache, SessionID, AppID, ProcessesID);
                        result = ParseDynamicUserList(rawValue);
                    }
                    break;

                case "query":
                    _messageLogger.LogWarning("[MessageNode] Query 模式暂未实现，返回空列表");
                    break;

                default:
                    result = userConfig.UserList ?? new List<TargetUserItem>();
                    break;
            }

            return result;
        }

        private List<TargetUserItem> ParseDynamicUserList(string json)
        {
            var result = new List<TargetUserItem>();
            if (string.IsNullOrWhiteSpace(json)) return result;

            try
            {
                var array = JArray.Parse(json);
                foreach (var item in array)
                {
                    var user = new TargetUserItem
                    {
                        IMUserID = GetJTokenValue(item, "imUserId", "IMUserID") ?? string.Empty,
                        IMUserName = GetJTokenValue(item, "imUserName", "IMUserName") ?? string.Empty,
                        ContentOverride = GetJTokenValue(item, "contentOverride", "ContentOverride") ?? string.Empty
                    };
                    if (!string.IsNullOrEmpty(user.IMUserID))
                        result.Add(user);
                }
            }
            catch (Exception ex)
            {
                _messageLogger.LogWarning(ex, "[MessageNode] 解析动态用户列表失败");
            }

            return result;
        }

        private string? GetJTokenValue(JToken token, string key1, string key2)
        {
            if (token[key1] != null) return token[key1]?.ToString();
            if (token[key2] != null) return token[key2]?.ToString();
            return null;
        }

        private async Task<List<MessageSendRecordInfo>> WaitForAllSendResultsAsync(List<string> recordIds)
        {
            var timeout = TimeSpan.FromSeconds(_nodeOptions.Value.WaitTimeoutSeconds);
            var pollInterval = TimeSpan.FromMilliseconds(_nodeOptions.Value.PollIntervalMs);
            var deadline = DateTime.UtcNow + timeout;
            var results = new List<MessageSendRecordInfo>();

            while (DateTime.UtcNow < deadline)
            {
                results.Clear();
                bool allDone = true;

                foreach (var recordId in recordIds)
                {
                    var record = MessageSendRecordBussiness.GetModel(recordId);
                    if (record == null)
                    {
                        allDone = false;
                        continue;
                    }

                    results.Add(record);
                    if (record.SendStatus == 0)
                        allDone = false;
                }

                if (allDone) return results;
                await Task.Delay(pollInterval);
            }

            return results;
        }
    }
}
