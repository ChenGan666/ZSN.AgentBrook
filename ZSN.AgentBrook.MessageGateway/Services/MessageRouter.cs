using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using ZSN.AgentBrook.MessageGateway.Configuration;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.Utils.Core.Utils;

namespace ZSN.AgentBrook.MessageGateway.Services
{
    public class MessageRouter : IMessageRouter
    {
        private readonly IOptions<GatewayOptions> _options;
        private readonly ILogger<MessageRouter> _logger;

        public MessageRouter(IOptions<GatewayOptions> options, ILogger<MessageRouter> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<RouteResult> RouteAsync(ReceiveMessageEvent @event, ChannelConfigInfo channelConfig)
        {
            try
            {
                if (!string.IsNullOrEmpty(channelConfig.TargetAppID))
                    return await RouteDirectAsync(@event, channelConfig);

                var rules = MessageRouteRuleBussiness.GetList(
                    $"ChannelID='{@event.ChannelID}' AND Enabled=1");

                if (rules == null || rules.Count == 0)
                {
                    _logger.LogDebug("[Router] 无匹配路由规则: ChannelID={ChannelID}", @event.ChannelID);
                    return new RouteResult { Matched = false };
                }

                var sortedRules = rules.OrderByDescending(r => r.Priority).ToList();

                foreach (var rule in sortedRules)
                {
                    if (MatchRule(rule, @event))
                    {
                        _logger.LogInformation("[Router] 规则命中: RuleID={RuleID}, RuleName={Name}",
                            rule.RuleID, rule.RuleName);

                        string memberID = EnsureMember(@event.ChannelID, @event.FromUser, @event.FromUserName);
                        string sessionId = GetOrCreateSessionID(@event.ChannelID, memberID, rule.TargetAppID,
                            rule.SessionTimeoutMinutes > 0 ? rule.SessionTimeoutMinutes : _options.Value.DefaultSessionTimeoutMinutes);

                        var (ruleWorkflowID, ruleNodeID) = ResolveWorkflowAndNode(rule.TargetAppID);
                        if (string.IsNullOrEmpty(ruleWorkflowID))
                        {
                            _logger.LogWarning("[Router] 规则指向的应用未配置工作流: RuleID={RuleID}, AppID={AppID}", rule.RuleID, rule.TargetAppID);
                            continue;
                        }

                        var inputs = BuildWorkflowInputs(@event, rule);
                        var processesId = Guid.NewGuid().ToString();

                        string taskId = CreateWorkflowTask(rule.TargetAppID, ruleWorkflowID,
                            ruleNodeID, sessionId, processesId, memberID, inputs, @event);

                        _logger.LogInformation("[Router] 任务已创建: TaskID={TaskID}, WorkflowID={WorkflowID}, SessionID={SessionID}",
                            taskId, ruleWorkflowID, sessionId);

                        return new RouteResult
                        {
                            Matched = true,
                            MatchedRuleID = rule.RuleID,
                            CreatedTaskID = taskId,
                            SessionID = sessionId
                        };
                    }
                }

                _logger.LogDebug("[Router] 所有规则未命中");
                return new RouteResult { Matched = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Router] 路由异常: EventId={EventId}", @event.EventId);
                return new RouteResult { Matched = false, ErrorMessage = ex.Message };
            }
        }

        private async Task<RouteResult> RouteDirectAsync(ReceiveMessageEvent @event, ChannelConfigInfo channelConfig)
        {
            var timeoutMinutes = channelConfig.SessionTimeoutMinutes > 0
                ? channelConfig.SessionTimeoutMinutes
                : _options.Value.DefaultSessionTimeoutMinutes;

            string memberID = EnsureMember(@event.ChannelID, @event.FromUser, @event.FromUserName);
            string sessionId = GetOrCreateSessionID(@event.ChannelID, memberID, channelConfig.TargetAppID, timeoutMinutes);

            var (workflowID, nodeID) = ResolveWorkflowAndNode(channelConfig.TargetAppID);
            if (string.IsNullOrEmpty(workflowID))
            {
                _logger.LogError("[Router] 无法解析应用的工作流: AppID={AppID}", channelConfig.TargetAppID);
                return new RouteResult { Matched = false, ErrorMessage = "应用未配置工作流" };
            }

            var inputs = BuildStandardInputs(@event);
            var processesId = Guid.NewGuid().ToString();

            string taskId = CreateWorkflowTask(channelConfig.TargetAppID, workflowID,
                nodeID, sessionId, processesId, memberID, inputs, @event);

            _logger.LogInformation("[Router] 直连路由: TaskID={TaskID}, AppID={AppID}, WorkflowID={WorkflowID}",
                taskId, channelConfig.TargetAppID, workflowID);

            return await Task.FromResult(new RouteResult
            {
                Matched = true,
                MatchedRuleID = "",
                CreatedTaskID = taskId,
                SessionID = sessionId
            });
        }

        #region Member 管理

        private string EnsureMember(string channelID, string fromUser, string fromUserName)
        {
            // 确定性 MemberID：同一渠道同一用户始终生成相同 ID
            string memberID = new HashEncrypt().MD5System("im_" + channelID + "_" + fromUser);

            try
            {
                var existing = MemberInfoBussiness.GetModel(memberID);
                if (existing != null)
                    return memberID;

                var member = new MemberInfo
                {
                    MemberID = memberID,
                    MPhoneNumber = "",
                    MNickName = fromUserName ?? fromUser,
                    MPWD = "",
                    MIcon = "",
                    MBirthday = DateTime.Now,
                    MState = 0,
                    MPoints = 0,
                    MLevel = 0,
                    MIntroducer = channelID,
                    MAppendTime = DateTime.Now
                };

                MemberInfoBussiness.Add(member);
                _logger.LogInformation("[Router] 创建临时会员: MemberID={MemberID}, NickName={NickName}, Source={Channel}",
                    memberID, fromUserName, channelID);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Router] 会员创建失败（可能已存在）: MemberID={MemberID}", memberID);
            }

            return memberID;
        }

        #endregion

        #region Session 管理

        private string GetOrCreateSessionID(string channelId, string memberID, string appId, int timeoutMinutes)
        {
            if (_options.Value.EnableSessionReuse)
            {
                try
                {
                    var existingSessions = AppChatSessionInfoBussiness.GetList(
                        $"AppID='{appId}' AND MemberID='{memberID}' AND SessionStatus=1");

                    if (existingSessions != null && existingSessions.Count > 0)
                        return existingSessions[0].ChatSessionID;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Router] 查询活跃会话失败，创建新会话");
                }
            }

            // 创建新会话并写入 DB
            string sessionId = Guid.NewGuid().ToString();
            try
            {
                var session = new AppChatSessionInfo
                {
                    ChatSessionID = sessionId,
                    AppID = appId,
                    MemberID = memberID,
                    TopicSummary = "IM:" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                    IsCoCreate = 0,
                    SystemStatus = 0,
                    SessionStatus = 0,
                    CreateTime = DateTime.Now
                };
                AppChatSessionInfoBussiness.Add(session);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Router] 创建会话记录失败: SessionID={SessionID}", sessionId);
            }

            return sessionId;
        }

        #endregion

        #region Task 创建

        private string CreateWorkflowTask(string appID, string workflowID, string nodeID,
            string sessionId, string processesId, string memberID, List<Inputs> inputs,
            ReceiveMessageEvent @event)
        {
            var taskId = Guid.NewGuid().ToString();

            // 从数据库读取完整节点配置（包含 data），与 ChatController 一致
            NodeConfig nodeConfig;
            var nodeInfo = WorkflowNodeInfoBussiness.GetModel(nodeID);
            if (nodeInfo?.Config != null)
            {
                nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(nodeInfo.Config.ToString());
                if (nodeConfig == null)
                    nodeConfig = new NodeConfig { id = nodeID, type = NodeType.Start, workflowid = workflowID };
            }
            else
            {
                nodeConfig = new NodeConfig { id = nodeID, type = NodeType.Start, workflowid = workflowID };
            }

            // 构建 TaskData，对齐 ChatController
            var data = new TaskData
            {
                AppID = appID,
                TaskID = taskId,
                SessionID = sessionId,
                ProcessesID = processesId,
                AgentNodeID = nodeID,
                MemberID = memberID,
                Inputs = inputs,
                MsgChannelID = @event.ChannelID,
                MsgFromUser = @event.FromUser,
                MsgReplyMode = "end"
            };

            // 附件处理（对齐 ChatController:204-212）
            if (@event.Attachments != null && @event.Attachments.Count > 0)
            {
                string previewHost = ZSN.Utils.Core.Helpers.ConfigHelper.GetString("previewHost");
                foreach (var item in @event.Attachments)
                {
                    if (!string.IsNullOrEmpty(item.FileCode))
                        item.FileURI = string.Format(previewHost, item.FileCode);
                }
                data.AttachmentItems = @event.Attachments;
            }
            data.AdditionalOptions = @event.AdditionalOptions;

            var taskInfo = new TaskInfo
            {
                TaskID = taskId,
                TaskType = nodeConfig.type,
                TaskConfig = new TaskConfig
                {
                    NodeConfig = nodeConfig,
                    Data = data
                },
                LoopType = LoopType.NOLoop,
                RepeatValue = 1,
                RedoCount = 0,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                WorkflowID = workflowID,
                SessionID = sessionId,
                ProcessesID = processesId,
                State = TaskState.Waiting
            };

            TaskInfoBussiness.Add(taskInfo);

            // 写入聊天日志（对齐 ChatController:215）
            try
            {
                var gptMsg = new GptMsg
                {
                    role = "user",
                    content = @event.Content ?? "",
                    Attachments = @event.Attachments ?? new List<AttachmentItem>(),
                    AdditionalOptions = @event.AdditionalOptions
                };
                AppChatLogInfoBussiness.Add(appID, sessionId, "User", gptMsg);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Router] 写入聊天日志失败");
            }

            // 写入 Redis 队列供 NodeJob 消费
            try
            {
                var redis = new ZSN.Utils.Core.Helpers.RedisHelper();
                var taskJson = JsonConvert.SerializeObject(new { taskInfo.TaskID });
                redis.ListLeftPush("node_task_queue", taskJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Router] Redis入队失败: TaskID={TaskID}", taskId);
            }

            return taskId;
        }

        #endregion

        #region Inputs 构建

        private List<Inputs> BuildWorkflowInputs(ReceiveMessageEvent @event, MessageRouteRuleInfo rule)
        {
            if (!string.IsNullOrEmpty(rule.InputMapping))
            {
                try
                {
                    var mapping = JsonConvert.DeserializeObject<InputMappingConfig>(rule.InputMapping);
                    if (mapping?.Mappings != null && mapping.Mappings.Count > 0)
                    {
                        return mapping.Mappings.Select(m => new Inputs
                        {
                            varname = m.Input,
                            value = GetEventFieldValue(@event, m.Source),
                            type = "string"
                        }).ToList();
                    }
                }
                catch { }
            }

            return BuildStandardInputs(@event);
        }

        private List<Inputs> BuildStandardInputs(ReceiveMessageEvent @event)
        {
            return new List<Inputs>
            {
                new() { varname = "input", value = @event.Content ?? "", type = "string" },
                new() { varname = "msg_from_user", value = @event.FromUser ?? "", type = "string" },
                new() { varname = "msg_from_name", value = @event.FromUserName ?? "", type = "string" },
                new() { varname = "msg_type", value = @event.MessageType ?? "", type = "string" },
                new() { varname = "msg_channel_id", value = @event.ChannelID ?? "", type = "string" },
                new() { varname = "msg_provider_type", value = @event.ProviderType ?? "", type = "string" },
                new() { varname = "msg_raw_data", value = @event.RawData ?? "", type = "string" },
                new() { varname = "msg_event_id", value = @event.EventId ?? "", type = "string" }
            };
        }

        private string GetEventFieldValue(ReceiveMessageEvent @event, string source)
        {
            return source switch
            {
                "event.Content" => @event.Content ?? "",
                "event.FromUser" => @event.FromUser ?? "",
                "event.FromUserName" => @event.FromUserName ?? "",
                "event.MessageType" => @event.MessageType ?? "",
                "event.ChannelID" => @event.ChannelID ?? "",
                "event.ProviderType" => @event.ProviderType ?? "",
                "event.RawData" => @event.RawData ?? "",
                "event.EventId" => @event.EventId ?? "",
                _ => ""
            };
        }

        #endregion

        #region 工作流解析

        private (string workflowID, string nodeID) ResolveWorkflowAndNode(string appID)
        {
            if (string.IsNullOrEmpty(appID))
                return ("", "");

            try
            {
                var workflow = WorkflowInfoBussiness.GetModelByAppID(appID);
                if (workflow == null)
                {
                    _logger.LogWarning("[Router] 应用无工作流: AppID={AppID}", appID);
                    return ("", "");
                }

                var startNode = WorkflowNodeInfoBussiness.GetAppStartNode(appID);
                return (workflow.WorkflowID, startNode?.NodeID ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Router] 解析工作流失败: AppID={AppID}", appID);
                return ("", "");
            }
        }

        #endregion

        #region 规则匹配

        private bool MatchRule(MessageRouteRuleInfo rule, ReceiveMessageEvent @event)
        {
            switch (rule.MatchType)
            {
                case "All":
                    return true;

                case "Keyword":
                    var kwCondition = JsonConvert.DeserializeObject<KeywordCondition>(rule.MatchCondition ?? "{}");
                    if (kwCondition?.Keywords == null || kwCondition.Keywords.Count == 0) return false;
                    if (kwCondition.Logic == "AND")
                        return kwCondition.Keywords.All(kw => @event.Content?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true);
                    else
                        return kwCondition.Keywords.Any(kw => @event.Content?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true);

                case "Regex":
                    var regexCondition = JsonConvert.DeserializeObject<RegexCondition>(rule.MatchCondition ?? "{}");
                    if (string.IsNullOrEmpty(regexCondition?.Pattern)) return false;
                    return Regex.IsMatch(@event.Content ?? "", regexCondition.Pattern);

                case "Intent":
                    return false;

                default:
                    return false;
            }
        }

        #endregion
    }

    public class KeywordCondition
    {
        public List<string> Keywords { get; set; } = new();
        public string Logic { get; set; } = "OR";
    }

    public class RegexCondition
    {
        public string Pattern { get; set; }
    }

    public class InputMappingConfig
    {
        public List<InputMappingItem> Mappings { get; set; } = new();
    }

    public class InputMappingItem
    {
        public string Input { get; set; }
        public string Source { get; set; }
    }
}
