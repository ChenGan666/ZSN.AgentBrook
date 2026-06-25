using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Office.SpreadSheetML.Y2023.MsForms;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using Google.Protobuf.WellKnownTypes;
using Lucene.Net.Util.Fst;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Client;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Utilities;
using SqlSugar;
using System.Reflection;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Models.Image;
using ZSN.AI.Core.Models.Video;
using ZSN.AI.Core.Repositories;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Entity.Model;
using ZSN.AI.Entity.Model.Enum;
using ZSN.AI.Plugins;
using ZSN.AI.Service.Helpers;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using ZSN.Utils.Core.Utils;
using ZSN.AI.Node.Utils;
using ZSN.AI.Node.Utils.Pipeline;
using ZSN.AI.Node.Claw;

namespace ZSN.AI.Node
{
    public class Execution: BaseExecution
    {
        
        public Execution(IChatService chatService, IServiceProvider provider, ILogger<Execution> logger)
            : base(chatService, provider, logger)
        {
        }


        public static MessageData ExecutionNode(string AppID, string SessionID, string ProcessesID, string nodeId, string MemberID, string TopicSummary, List<Inputs> inputs, string FromTaskID, string FromMainTaskID, string AgentNodeID, string WorkflowID)
        {
            AppChatSessionInfo appChatSession = new AppChatSessionInfo();
            MessageData messageData = new MessageData();
            messageData.AppID = AppID;
            messageData.ProcessesID = ProcessesID;

            string TaskID = Guid.NewGuid().ToString();

            WorkflowNodeInfo info = WorkflowNodeInfoBussiness.GetModel(nodeId);

            string _nodeConfigStr = info != null ? info.Config.ToString() : null;

            NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(_nodeConfigStr);

            if (nodeConfig?.data != null)
            {
                if (SessionID.IsNullOrEmpty())
                {
                    SessionID = Guid.NewGuid().ToString();
                    appChatSession.AppID = AppID;
                    appChatSession.ChatSessionID = SessionID;
                    appChatSession.MemberID = MemberID;
                    appChatSession.TopicSummary = "话题:" + DateTime.Now.ToString();
                    appChatSession.IsCoCreate = 0;
                    appChatSession.SystemStatus = 0;
                    appChatSession.CreateTime = DateTime.Now;

                    AppChatSessionInfoBussiness.Add(appChatSession);
                }
                else
                {
                    appChatSession = AppChatSessionInfoBussiness.GetModel(SessionID);
                    if (appChatSession == null)
                    {
                        appChatSession = new AppChatSessionInfo();
                        appChatSession.AppID = AppID;
                        appChatSession.ChatSessionID = SessionID;
                        appChatSession.MemberID = MemberID;
                        appChatSession.TopicSummary = "话题:" + DateTime.Now.ToString();
                        appChatSession.IsCoCreate = 0;
                        appChatSession.SystemStatus = 0;
                        appChatSession.CreateTime = DateTime.Now;
                        AppChatSessionInfoBussiness.Add(appChatSession);
                    }
                    else
                    {
                        appChatSession.AppID = AppID;
                        appChatSession.ChatSessionID = SessionID;
                        appChatSession.MemberID = MemberID;
                        appChatSession.TopicSummary = "话题:" + DateTime.Now.ToString();
                        appChatSession.IsCoCreate = 0;
                        appChatSession.SystemStatus = 0;
                        appChatSession.CreateTime = DateTime.Now;
                        AppChatSessionInfoBussiness.Update(appChatSession);
                    }
                    //删除同SessionID的Task记录
                    TaskInfoBussiness.DeleteBySessionID(SessionID);

                    //删除该节点及其下级节点的运行记录
                    List<WorkflowNodeInfo> nodeInfos = WorkflowNodeInfoBussiness.GetAllNextNodeListByNodeID(nodeId);
                    if (nodeInfos?.Count > 0)
                    {
                        foreach (var node in nodeInfos)
                        {
                            WorkflowNodeExecutionRecordInfoBussiness.DeleteByNodeID(SessionID, node.NodeID);
                        }
                    }
                }

                messageData.SessionID = SessionID;

                TaskData data = new TaskData() { AppID = AppID, SessionID = SessionID, ProcessesID = messageData.ProcessesID, AgentNodeID = "" };
                data.Inputs = inputs;
                Inputs? attachmentInput = null;
                Inputs? additionalOptions = null;
                if (inputs?.Count > 0)
                {
                    attachmentInput = inputs.FirstOrDefault(i => i.varname == "attachments");
                    additionalOptions = inputs.FirstOrDefault(i => i.varname == "additionalOptions");
                }

                if (attachmentInput != null)
                {
                    data.AttachmentItems = JsonConvert.DeserializeObject<List<AttachmentItem>>(attachmentInput.value);
                    if (data.AttachmentItems != null)
                    {
                        //处理附件信息加入附件的URI
                        string previewHost = ConfigHelper.GetString("previewHost");
                        foreach (var item in data.AttachmentItems)
                        {
                            item.FileURI = string.Format(previewHost, item.FileCode);
                        }
                    }
                }
                if (additionalOptions != null)
                {
                    data.AdditionalOptions = JsonConvert.DeserializeObject(additionalOptions.value);
                }


                TaskInfo taskInfo = new TaskInfo();
                taskInfo.TaskID = TaskID;
                taskInfo.TaskType = nodeConfig.type;
                taskInfo.TaskConfig = new TaskConfig();
                taskInfo.TaskConfig.NodeConfig = nodeConfig;
                taskInfo.TaskConfig.Data = data;
                taskInfo.LoopType = LoopType.NOLoop;
                taskInfo.RepeatValue = 1;
                taskInfo.RedoCount = 0;
                taskInfo.CreateTime = DateTime.Now;
                taskInfo.UpdateTime = DateTime.Now;
                taskInfo.FromTaskID = FromTaskID;
                taskInfo.FromMainTaskID = FromMainTaskID;

                TaskInfoBussiness.Add(taskInfo);

            }

            MessageData message = new MessageData();
            message.AppID = AppID;
            message.SessionID = SessionID;
            message.ProcessesID = ProcessesID;
            message.TaskID = TaskID;
            message.Content = "";
            message.Role = "Task";
            message.Timestamp = DateTime.Now;

            return message;
        }

        public async Task<string> StartNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;

            List<Inputs> inputs = data.Inputs;

            List<AttachmentItem> AttachmentItems = ZSN.AI.Node.Utils.Utils.updateAttachmentItemsFilePath(data.AttachmentItems);
            dynamic AdditionalOptions = data.AdditionalOptions;

            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    StartData nodeData = JsonConvert.DeserializeObject<StartData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        nodeData.prompt = await this.ReplacePromptValue(nodeData.prompt, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
                        var _attachmentsString = JsonConvert.SerializeObject(AttachmentItems);
                        var _additionalOptionsString = JsonConvert.SerializeObject(AdditionalOptions);

                        inputs.Add(new Inputs() { varname = "attachments", type = "List<AttachmentItem>", value = _attachmentsString, sourceId = $"{config.id}_attachments" });
                        inputs.Add(new Inputs() { varname = "additionalOptions", type = "dynamic", value = _additionalOptionsString, sourceId = $"{config.id}_additionalOptions" });

                        outputs.Add(new Output { varname = "prompt", value = nodeData.prompt, nodeId = config.id, sourceId = $"{config.id}_prompt" });
                        outputs.Add(new Output { varname = "currentTime", value = DateTime.Now.ToDateTimeString(), nodeId = config.id, sourceId = $"{config.id}_currentTime" });

                        outputs.Add(new Output() { varname = "attachments", type = "List<AttachmentItem>", value = _attachmentsString, nodeId = config.id, sourceId = $"{config.id}_attachments" });
                        outputs.Add(new Output() { varname = "additionalOptions", type = "dynamic", value = _additionalOptionsString, nodeId = config.id, sourceId = $"{config.id}_additionalOptions" });
                        outputs.Add(new Output() { varname = "sessionId", type = "string", value = SessionID, nodeId = config.id, sourceId = $"{config.id}_sessionId" });

                        WorkflowNodeInfoBussiness.NextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, config, inputs, outputs, Logs);

                        Logs.Add(nodeData.prompt);
                    }
                }
                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);

                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }
        public async Task<string> AgentStartNodeAsync(NodeConfig config, TaskData data)
        {
            // AgentStartData 继承自 StartData，共用启动逻辑
            return await StartNodeAsync(config, data);
        }
        public async Task<string> EndNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    EndData nodeData = JsonConvert.DeserializeObject<EndData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        nodeData.prompt = await this.ReplacePromptValue(nodeData.prompt, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
                        //outputs.Add(nodeData.prompt);
                        outputs.Add(new Output { varname = "results", value = nodeData.prompt, nodeId = config.id, sourceId = $"{config.id}_results" });
                        Logs.Add(nodeData.prompt);

                        GptMsg _msg = new GptMsg();
                        _msg.role = AuthorRole.Assistant.ToString();
                        _msg.content = nodeData.prompt;

                        AppChatLogInfoBussiness.Add(AppID, SessionID, AuthorRole.Assistant.ToString(), _msg);

                        TaskInfoBussiness.updateTask(TaskID, TaskState.Completed, new Results()
                        {
                            Data = nodeData.prompt
                        });
                    }

                    // ===== 检查是否有 ClawAI 步骤在等待此 WorkFlow 完成 =====
                    if (!string.IsNullOrEmpty(FromMainTaskID))
                    {
                        Console.WriteLine($"[ClawAI-Resume] EndNodeAsync 完成 - FromMainTaskID: {FromMainTaskID}");
                        await WorkflowNodeInfoBussiness.TryResumeClawAIStepAsync(FromMainTaskID, outputs, Logs);
                    }

                    // ===== IM 自动回传：由 MessageGateway 触发的工作流，End 节点自动回复给 IM 用户 =====
                    if (!string.IsNullOrEmpty(data.MsgChannelID) && data.MsgReplyMode == "end")
                    {
                        try
                        {
                            string replyContent = outputs.FirstOrDefault(o => o.varname == "results")?.value ?? "";
                            if (!string.IsNullOrEmpty(replyContent))
                            {
                                string sendRecordId = Guid.NewGuid().ToString();
                                var sendRecord = new MessageSendRecordInfo
                                {
                                    RecordID = sendRecordId,
                                    ChannelID = data.MsgChannelID,
                                    SessionID = SessionID,
                                    TaskID = TaskID,
                                    NodeID = config.id,
                                    MessageType = "text",
                                    Content = replyContent,
                                    TargetUser = data.MsgFromUser,
                                    SendStatus = 0,
                                    CreateTime = DateTime.Now
                                };
                                MessageSendRecordBussiness.Add(sendRecord);

                                var sendTask = new
                                {
                                    RecordID = sendRecordId,
                                    ChannelID = data.MsgChannelID,
                                    MessageType = "text",
                                    Content = replyContent,
                                    TargetUser = data.MsgFromUser,
                                    SessionID = SessionID,
                                    TaskID = TaskID,
                                    NodeID = config.id,
                                    EnqueueTime = DateTime.Now
                                };
                                var redis = new ZSN.Utils.Core.Helpers.RedisHelper();
                                redis.ListLeftPush("msg_send_queue", JsonConvert.SerializeObject(sendTask));

                                Logs.Add($"[IM-Reply] 已入队回传消息: ChannelID={data.MsgChannelID}, TargetUser={data.MsgFromUser}");
                            }
                        }
                        catch (Exception imEx)
                        {
                            Logs.Add($"[IM-Reply] 回传失败: {imEx.Message}");
                        }
                    }
                }

                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }
        public async Task<string> AgentEndNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    AgentEndData nodeData = JsonConvert.DeserializeObject<AgentEndData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        nodeData.prompt = await this.ReplacePromptValue(nodeData.prompt, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
                        //Outputs.Add(nodeData.prompt);
                        outputs.Add(new Output { varname = "results", value = nodeData.prompt, nodeId = config.id, sourceId = $"{config.id}_results" });
                        Logs.Add(nodeData.prompt);

                        TaskInfoBussiness.updateTask(TaskID, TaskState.Completed, new Results()
                        {
                            Data = nodeData.prompt
                        });

                        ExecutionRecordStatus = ExecutionRecordStatus.Success;

                        //处理子工作流返回的结果
                        Console.WriteLine($"[ClawAI-Debug] AgentEndNodeAsync - AgentNodeID: {AgentNodeID}, FromMainTaskID: {FromMainTaskID}");
                        Logs.Add($"[ClawAI-Debug] AgentEndNodeAsync - AgentNodeID: {AgentNodeID}, FromMainTaskID: {FromMainTaskID}");
                        
                        WorkflowNodeInfo nodeInfo = WorkflowNodeInfoBussiness.GetModel(AgentNodeID);
                        Console.WriteLine($"[ClawAI-Debug] nodeInfo is null: {nodeInfo == null}");
                        Logs.Add($"[ClawAI-Debug] nodeInfo is null: {nodeInfo == null}");
                        
                        if (nodeInfo != null)
                        {

                            NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(nodeInfo.Config.ToString());
                            AgentData agentData = JsonConvert.DeserializeObject<AgentData>(nodeConfig.data.ToString());

                            nodeConfig.fromNodeType = NodeType.AgentEnd;
                            string agentName = agentData.label;

                            outputs.Add(new Output { varname = "currentTime", value = DateTime.Now.ToDateTimeString(), nodeId = config.id, sourceId = $"{config.id}_currentTime" });
                            outputs.Add(new Output { varname = "agentName", value = agentName, nodeId = config.id, sourceId = $"{config.id}_agentName" });

                            Console.WriteLine($"[ClawAI-Debug] 调用 AgentEndToNextNode - FromMainTaskID: {FromMainTaskID}");
                            Logs.Add($"[ClawAI-Debug] 调用 AgentEndToNextNode - FromMainTaskID: {FromMainTaskID}");
                            await WorkflowNodeInfoBussiness.AgentEndToNextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, nodeConfig, outputs, Logs);
                            Console.WriteLine($"[ClawAI-Debug] AgentEndToNextNode 完成 - FromMainTaskID: {FromMainTaskID}");
                            Logs.Add($"[ClawAI-Debug] AgentEndToNextNode 完成 - FromMainTaskID: {FromMainTaskID}");
                        }
                        else
                        {
                            // AgentNodeID 为空时（如由 ClawAI 异步触发），直接尝试恢复 ClawAI 步骤
                            Console.WriteLine($"[ClawAI-Debug] nodeInfo 为 null, FromMainTaskID.IsNullOrEmpty: {FromMainTaskID.IsNullOrEmpty()}");
                            Logs.Add($"[ClawAI-Debug] nodeInfo 为 null, FromMainTaskID.IsNullOrEmpty: {FromMainTaskID.IsNullOrEmpty()}");
                            
                            if (!FromMainTaskID.IsNullOrEmpty())
                            {
                                Console.WriteLine($"[ClawAI-Resume] AgentEndNodeAsync (无AgentNodeID) 尝试恢复 - FromMainTaskID: {FromMainTaskID}");
                                Logs.Add($"[ClawAI-Resume] AgentEndNodeAsync (无AgentNodeID) 尝试恢复 - FromMainTaskID: {FromMainTaskID}");
                                await WorkflowNodeInfoBussiness.TryResumeClawAIStepAsync(FromMainTaskID, outputs, Logs);
                                Console.WriteLine($"[ClawAI-Resume] TryResumeClawAIStepAsync 完成 - FromMainTaskID: {FromMainTaskID}");
                                Logs.Add($"[ClawAI-Resume] TryResumeClawAIStepAsync 完成 - FromMainTaskID: {FromMainTaskID}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            /*

            //处理子的任务回溯，WorkFlow->Agent->MainAI
            //检查一次与当前节点相同ProcessesID的任务是否已完成，如果完成将结果整理反馈给MainAI
            bool _finished = true;
            bool AgentWorkflowCompleted = true;
            int CompletedTotal = 0;
            var dataMsg = new StringBuilder();
            List<TaskInfo> agentStart_Tasks = new List<TaskInfo>();
            List<TaskInfo> agentEnd_Tasks = new List<TaskInfo>();
            //有FromMainTaskID的是MainAI触发的Agent工作流
            if (FromMainTaskID.IsNullOrEmpty() == false)
            {
                _finished = false;
                List<TaskInfo> taskInfos = TaskInfoBussiness.GetListByFromMainTaskID(FromMainTaskID);

                agentStart_Tasks = taskInfos.FindAll(t => t.TaskType == NodeType.AgentStart);
                agentEnd_Tasks = taskInfos.FindAll(t => t.TaskType == NodeType.AgentEnd);

                _finished = agentStart_Tasks.Count == agentEnd_Tasks.Count;
                if (_finished)
                {
                    foreach (TaskInfo task in agentEnd_Tasks)
                    {
                        if (task.State == TaskState.Waiting || task.State == TaskState.Processing)
                        {
                            AgentWorkflowCompleted = false;
                        }
                        if (task.State == TaskState.Completed)
                        {
                            dataMsg.AppendLine("---start---");
                            dataMsg.AppendLine(task.Results.Data.ToString());
                            dataMsg.AppendLine("---end---");
                            dataMsg.AppendLine("");
                        }
                    }
                    outputs = new List<Output>();
                    outputs.Add(new Output { varname = "results", value = dataMsg.ToString(), nodeId = config.id, sourceId = $"{config.id}_results" });
                }
            }
            else
            {
                _finished = true;
            }

            Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);

            //开始和结束数量相等，继续
            if (_finished)
            {
                //已经全部完成，把结果反馈给上层节点（fromTask）的下一节点
                if (AgentWorkflowCompleted)
                {
                    //处理子工作流返回的结果
                    WorkflowNodeInfo nodeInfo = WorkflowNodeInfoBussiness.GetModel(AgentNodeID);
                    if (nodeInfo != null)
                    {

                        NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(nodeInfo.Config.ToString());
                        AgentData agentData = JsonConvert.DeserializeObject<AgentData>(nodeConfig.data.ToString());

                        nodeConfig.fromNodeType = NodeType.AgentEnd;
                        string agentName = agentData.label;

                        //List<Output> outputs = new List<Output>();

                        outputs.Add(new Output { varname = "currentTime", value = DateTime.Now.ToDateTimeString(), nodeId = config.id, sourceId = $"{config.id}_currentTime" });
                        outputs.Add(new Output { varname = "agentName", value = agentName, nodeId = config.id, sourceId = $"{config.id}_agentName" });

                        WorkflowNodeInfoBussiness.AgentEndToNextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, nodeConfig, outputs, Logs);
                    }

                }
            }
            */
            return RecordID;
        }
        public async Task<string> LargeModelNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            //将TaskData中的附件和附加信息取出来，单独处理
            Inputs attachmentInput = inputs.FirstOrDefault(i => i.varname == "attachments");
            Inputs additionalOptions = inputs.FirstOrDefault(i => i.varname == "additionalOptions");

            if (attachmentInput != null)
            {
                data.AttachmentItems = JsonConvert.DeserializeObject<List<AttachmentItem>>(attachmentInput.value);
            }
            if (additionalOptions != null)
            {
                data.AdditionalOptions = JsonConvert.DeserializeObject(additionalOptions.value);
            }

            // 过滤 ClawAI 子 WorkFlow 拼接的 _{StepID} 后缀，使用原始 ProcessesID 构建流式 Key
            string streamProcessesID = ProcessesID;
            int lastUnderscore = ProcessesID.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                streamProcessesID = ProcessesID.Substring(0, lastUnderscore);
            }
            var streamKey = StreamKey.Build(SessionID, streamProcessesID);

            // 流式输出：提升到方法级别，覆盖整个执行生命周期
            using var batchWriter = new StreamBatchWriter(
                _streamSync, streamKey, SessionID, ProcessesID, TaskID, config.id, intervalMs: 200);
            IProgress<string> progress = new Progress<string>(delta => {
                batchWriter.Append(delta);
            });

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    LargeModelData nodeData = JsonConvert.DeserializeObject<LargeModelData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        nodeData.prompt = await this.ReplacePromptValue(nodeData.prompt, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
                        progress?.Report("\n=== 开始执行大模型节点 ===");
                        batchWriter.Flush();
                        string results = "";

                        ChatHistory history = new ChatHistory();

                        if (nodeData.model?.LargeModelID > 0)
                        {
                            LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(nodeData.model.LargeModelID);

                            //设置系统提示词
                            if (!nodeData.prompt.IsNullOrEmpty())
                            {
                                history.AddSystemMessage(nodeData.prompt);
                            }
                            history.AddUserMessage("继续");//默认加一个继续，触发模型响应（有的模型必须有UserMessage）

                            LargeModelConfig modelConfig = new LargeModelConfig();
                            modelConfig.Id = largeModel.LargeModelID.ToString();
                            modelConfig.Model = largeModel;
                            modelConfig.SemanticFunction = nodeData.SemanticFunction;
                            modelConfig.NativeFunction = nodeData.NativeFunction;
                            modelConfig.Temperature = nodeData.temperature;
                            modelConfig.TopPCoefficient = nodeData.topp;
                            modelConfig.ResponseFormat = nodeData.ResponseFormat;
                            modelConfig.Thinking = nodeData.Thinking;


                            if (data.AttachmentItems?.Count > 0)
                            {
                                if (nodeData.CanReadPic || nodeData.CanReadDoc)
                                {
                                    history = await ZSN.AI.Node.Utils.Utils.AttachmentToChatHistoryAsync(data.AttachmentItems, history);
                                }
                            }

                            // 流式调用 + 消费，带重试（仅保护连接/首 token 阶段）
                            // 一旦首个 token 到达即开始向下游推送，此后出错不再重试（避免重复输出）
                            // 注意：首 token 前失败会重试；首 token 后失败直接向上抛出（由外层 catch 记录失败）
                            bool hasStartedStreaming = false;
                            Chats info = null;
                            results = await RetryPolicy.ExecuteWithConditionAsync(async () =>
                            {
                                StringBuilder rawContent = new StringBuilder();
                                Chats localInfo = null;
                                List<Chats> MessageList = [];

                                // 关闭服务内部观察，由本循环自行控制推送，保证重试时不会重复输出
                                var chatResult = _chatService.SendChatAsync(
                                    modelConfig,
                                    history,
                                    Function: null,
                                    responseFormat: nodeData.ResponseFormat.IsNullOrEmpty() ? "text" : nodeData.ResponseFormat,
                                    enableStreamingObservation: false,
                                    progress: null,
                                    ct: CancellationToken.None
                                );

                                await foreach (var content in chatResult)
                                {
                                    if (localInfo == null)
                                    {
                                        rawContent.Append(content.ConvertToString());
                                        localInfo = new Chats();
                                        localInfo.Id = Guid.NewGuid().ToString();
                                        localInfo.UserName = AuthorRole.Assistant.ToString();
                                        localInfo.AppId = AppID;
                                        localInfo.Context = content.ConvertToString();
                                        localInfo.CreateTime = DateTime.Now;

                                        MessageList.Add(localInfo);
                                        hasStartedStreaming = true; // 首 token 到达，流已建立，此后出错不再重试
                                        progress?.Report(content.ConvertToString());
                                    }
                                    else
                                    {
                                        rawContent.Append(content.ConvertToString());
                                        progress?.Report(content.ConvertToString());
                                    }
                                    localInfo.Context = rawContent.ToString();
                                }
                                info = localInfo;
                                return localInfo?.Context ?? "";
                            },
                            shouldRetry: ex => !hasStartedStreaming, // 只在尚未开始推送时重试
                            maxRetries: 3,
                            delayMs: 3000);
                            if (!string.IsNullOrEmpty(results))
                            {
                                progress?.Report($"\n✓ 大模型节点执行完成");
                                batchWriter.Flush();
                            }
                            await _streamSync.AppendDoneAsync(streamKey, SessionID, ProcessesID, TaskID, config.id, TimeSpan.FromMinutes(10));
                        }

                        Logs.Add(results);

                        outputs.Add(new Output { varname = "results", value = results, nodeId = config.id, sourceId = $"{config.id}_results" });

                        outputs.Add(new Output
                        {
                            varname = "prompt",
                            value = nodeData.prompt,
                            nodeId = config.id,
                            type = "string",
                            displayText = "使用的提示词"
                        });

                        outputs.Add(new Output
                        {
                            varname = "history",
                            value = JsonConvert.SerializeObject(history),
                            nodeId = config.id,
                            type = "string",
                            displayText = "对话记录"
                        });

                        WorkflowNodeInfoBussiness.NextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, config, inputs, outputs, Logs);

                    }
                }
                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        public async Task<string> MainAINodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID.IsNullOrEmpty() ? Guid.NewGuid().ToString() : data.ProcessesID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);
            int ChatCount = 0;

            //将TaskData中的附件和附加信息取出来，单独处理
            Inputs attachmentInput = inputs.FirstOrDefault(i => i.varname == "attachments");
            Inputs additionalOptions = inputs.FirstOrDefault(i => i.varname == "additionalOptions");

            if (attachmentInput != null)
            {
                data.AttachmentItems = JsonConvert.DeserializeObject<List<AttachmentItem>>(attachmentInput.value);
            }
            if (additionalOptions != null)
            {
                data.AdditionalOptions = JsonConvert.DeserializeObject(additionalOptions.value);
            }

            var streamKey = StreamKey.Build(SessionID, ProcessesID);

            List<ChatMessageContent> _Message = new List<ChatMessageContent>();
            AppChatSessionInfo appChatSession = new AppChatSessionInfo();

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    MainAIData nodeData = JsonConvert.DeserializeObject<MainAIData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        string results = "";
                        nodeData.prompt = await this.ReplacePromptValue(nodeData.prompt, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);


                        if (nodeData.model?.LargeModelID > 0)
                        {
                            ChatHistory history = new ChatHistory();
                            LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(nodeData.model.LargeModelID);

                            if (largeModel == null)
                            {
                                throw new Exception($"MainAI 模型未找到, LargeModelID: {nodeData.model.LargeModelID}");
                            }

                            //设置系统提示词
                            if (!nodeData.prompt.IsNullOrEmpty())
                            {
                                history.AddSystemMessage(nodeData.prompt);
                            }
                            LargeModelConfig modelConfig = new LargeModelConfig();
                            modelConfig.Id = largeModel.LargeModelID.ToString();
                            modelConfig.Model = largeModel;
                            modelConfig.SemanticFunction = nodeData.SemanticFunction;
                            modelConfig.NativeFunction = nodeData.NativeFunction;
                            modelConfig.Temperature = nodeData.temperature;
                            modelConfig.TopPCoefficient = nodeData.topp;
                            modelConfig.ResponseFormat = nodeData.ResponseFormat;
                            modelConfig.Thinking = nodeData.Thinking;

                            appChatSession = AppChatSessionInfoBussiness.GetModel(SessionID);
                            List<AppChatLogInfo> appChatLogs = AppChatLogInfoBussiness.GetListBySessionID(AppID, SessionID);
                            List<AppChatSummaryInfo> appChatSummaries = AppChatSummaryInfoBussiness.GetListBySessionID(AppID, SessionID);
                            var dataMsg = new StringBuilder();
                            CallFunction callFunction = new CallFunction();

                            ChatCount = appChatLogs.Count;
                            //过滤已被总结的记录
                            if (appChatLogs is not null && appChatLogs.Count > 0 && appChatSummaries is not null && appChatSummaries.Count > 0)
                            {
                                // 提取 appChatSummaries 中的所有 ChatLogIDList 并转为 HashSet<string>
                                var summaryIds = appChatSummaries
                                    .SelectMany(summary => summary.ChatLogIDList.Split(',', StringSplitOptions.RemoveEmptyEntries))
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase); // 不区分大小写

                                // 过滤 appChatLogs 中的记录
                                appChatLogs = appChatLogs.Where(log => !summaryIds.Contains("\"" + log.ChatLogID + "\"")).ToList();

                            }

                            history = await _chatService.GetChatHistory(appChatLogs, history);
                            history = await _chatService.GetChatHistory(appChatSummaries, history);

                            //是否存在用户输入
                            Inputs userInput = inputs.FirstOrDefault(input => input.varname == "prompt");
                            if (userInput != null)
                            {
                                history.AddUserMessage(userInput.value);
                            }

                            //是否执行Agent节点
                            bool excution_agent = true;

                            //上一节点是否为Agent节点
                            bool is_Agent_Return = config.fromNodeType == NodeType.Agent;

                            //先对用户提问进行预处理
                            if (!is_Agent_Return && userInput != null)
                            {
                                dataMsg.Clear();

                                dataMsg.AppendLine("#你现在是:提问范围判断引导人员");
                                dataMsg.AppendLine("#工作内容:对提问进行可回答范围的判断,严格控制输出内容");
                                dataMsg.AppendLine("#例子:如果是问候、寒暄之类非专业领域或者特定需要说明解释的内容，可以直接结合上下文信息进行回答，回答内容简洁，使用礼貌用语，其他内容一律只回答:\"no answer\"");

                                history.AddSystemMessage(dataMsg.ToString());

                                //dataMsg.AppendLine("#用户的提问:");
                                //dataMsg.AppendLine("{{$input}}");

                                callFunction = new CallFunction();
                                callFunction.Prompt = string.Join("\n", history.Select(x => x.Role + ": " + x.Content)) + "\n" + dataMsg.ToString();
                                callFunction.Input = userInput.value;

                                modelConfig.Temperature = 0;//不允许大模型自由发挥

                                var functionCallRe = _chatService.PromptFunctionCall(modelConfig, callFunction);
                                string response_str = "";
                                await foreach (var response in functionCallRe)
                                {
                                    response_str += response;
                                }
                                if (response_str.IndexOf("no answer") > -1)
                                {
                                    excution_agent = true;
                                }
                                else
                                {
                                    results = response_str;
                                    excution_agent = false;
                                }

                            }


                            //如果上一节点是Agent则对其反馈的结果进行梳理，判断是否满足要求，满足要求则返回梳理后的结果，不满足重新调用Agent，并提出进一步要求
                            if (is_Agent_Return)
                            {
                                var agentName = inputs.FirstOrDefault(a => a.varname == "agentName");
                                var agentResults = inputs.FirstOrDefault(a => a.varname == "results");
                                excution_agent = false;
                                string input_str = userInput.value;
                                dataMsg.Clear();
                                dataMsg.AppendLine($"##经过Agent({agentName.varname})的处理返回了如下结果，你可以判断返回的结果是否满足提出的问题，如果满足，可以根据结果再根据上下文组织优化后返回结果，如果无法满足，只需返回\"$$$无法满足$$$\"。");

                                dataMsg.AppendLine($"#Agent({agentName.varname})处理的结果:");
                                dataMsg.AppendLine($"{agentResults.value}");

                                dataMsg.AppendLine("#提出的问题:");
                                dataMsg.AppendLine("{{$input}}");
                                dataMsg.AppendLine("");

                                callFunction = new CallFunction();
                                callFunction.Prompt = dataMsg.ToString(); //string.Join("\n", history.Select(x => x.Role + ": " + x.Content))+"\n"+ dataMsg.ToString();
                                callFunction.Input = input_str;

                                var functionCallRe = _chatService.PromptFunctionCall(modelConfig, callFunction);
                                string response_str = "";
                                await foreach (var response in functionCallRe)
                                {
                                    response_str += response;
                                }

                                if (response_str.IndexOf("$$$无法满足$$$") > -1)
                                {

                                    excution_agent = true;
                                }
                                else
                                {
                                    results = response_str;
                                    excution_agent = false;
                                    history.AddAssistantMessage(response_str);
                                }
                            }

                            if (excution_agent)
                            {
                                //下一节点如果有Agent工作流，则由大模型自行判断执行其中的Agent工作流
                                KernelFunction function = null;
                                List<WorkflowEdgeInfo> edgeList = WorkflowEdgeInfoBussiness.GetListBySourceNodeId(config.id);
                                if (edgeList != null && edgeList.Count > 0)
                                {
                                    List<WorkflowNodeInfo> targetNodeList = WorkflowNodeInfoBussiness.GetListByNodeID(string.Join(",", edgeList.Select(x => $"'{x.TargetNodeId}'")));
                                    if (targetNodeList != null)
                                    {
                                        bool existAgentNode = false;
                                        dataMsg.Clear();
                                        dataMsg.AppendLine("#注意：");
                                        dataMsg.AppendLine("  1.当你无法准确的回答时，不能随意回答，需要调用特定的Agent进行处理。执行Agent，并传入相应的参数。");
                                        dataMsg.AppendLine("  2.可以通过Agent的参数列表中的Agent的Name和Description来进行判断，你觉得有需要某个或者多个Agent来帮你回答，就使用调用函数调用即可。");
                                        dataMsg.AppendLine("  3.同样的参数只允许调用一次!");

                                        dataMsg.AppendLine("##本次任务的共用标识参数 开始##");
                                        dataMsg.AppendLine($"AppID:{AppID}");
                                        dataMsg.AppendLine($"TaskID:{TaskID}");
                                        dataMsg.AppendLine($"FromMainTaskID:{TaskID}");
                                        dataMsg.AppendLine($"SessionID:{SessionID}");
                                        dataMsg.AppendLine($"Inputs:{userInput.value}");
                                        dataMsg.AppendLine("##本次任务的共用标识参数 结束##");

                                        dataMsg.AppendLine("##Agent的参数列表 开始##");

                                        foreach (WorkflowNodeInfo node in targetNodeList)
                                        {
                                            if (node != null && node.NodeType == NodeType.Agent)
                                            {
                                                existAgentNode = true;
                                                NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(node.Config.ToString());
                                                if (nodeConfig != null)
                                                {
                                                    AgentData agentData = JsonConvert.DeserializeObject<AgentData>(nodeConfig.data.ToString());
                                                    if (agentData != null)
                                                    {
                                                        dataMsg.AppendLine($"#{agentData.label}:");
                                                        dataMsg.AppendLine($"AgentName:{agentData.label}");
                                                        dataMsg.AppendLine($"Description:{agentData.agent.Description}");
                                                        dataMsg.AppendLine($"ProcessesID:{Guid.NewGuid().ToString()}");//每一个Agent分配一个ProcessesID
                                                        dataMsg.AppendLine($"AgentNodeID:{node.NodeID}");
                                                        dataMsg.AppendLine("");
                                                    }
                                                }
                                            }
                                        }

                                        dataMsg.AppendLine("##Agent的参数列表 结束##");
                                        dataMsg.AppendLine("");

                                        dataMsg.AppendLine("#返回\"AgentNodeID\"值，如果需要执行多个Agent,返回用逗号隔开的\"AgentNodeID\"");
                                        dataMsg.AppendLine("#返回例子:{\"AgentNodeID\":[\"AgentNodeID1\",\"AgentNodeID2\"]}");

                                        if (existAgentNode)
                                        {
                                            history.AddSystemMessage(dataMsg.ToString());
                                        }

                                        dataMsg.AppendLine("#用户的提问:");
                                        dataMsg.AppendLine("{{$input}}");

                                        callFunction = new CallFunction();
                                        callFunction.Prompt = string.Join("\n", history.Select(x => x.Role + ": " + x.Content));//  dataMsg.ToString();
                                        callFunction.Input = userInput.value;

                                        var functionCallRe = _chatService.PromptFunctionCall(modelConfig, callFunction);
                                        string response_str = "";
                                        await foreach (var response in functionCallRe)
                                        {
                                            response_str += response;
                                        }
                                        if (!response_str.IsNullOrEmpty())
                                        {
                                            if (response_str.IndexOf("AgentNodeID") > -1)
                                            {
                                                List<string> TaskIDs = new List<string>();

                                                var AgentNodeIDs = JsonConvert.DeserializeObject<AgentNode_return>(response_str);

                                                BasePlugin basePlugin = new BasePlugin();
                                                foreach (var AgentNodeID in AgentNodeIDs.AgentNodeID)
                                                {
                                                    TaskIDs.Add(basePlugin.excution_agent(AppID, TaskID, TaskID, SessionID, Guid.NewGuid().ToString(), AgentNodeID, userInput.value));
                                                }

                                                results = "{\"NewTaskID\":[" + string.Join(", ", TaskIDs.ConvertAll(s => $"\"{s}\"")) + "]}";
                                            }
                                        }
                                    }
                                }

                                //modelConfig.Temperature = 0;
                                //callFunction.FunctionClass = new BasePlugin();
                                //callFunction.FunctionName = "excution_agent";
                            }
                            else
                            {
                                callFunction = null;
                            }


                            if (results.IsNullOrEmpty())
                            {
                                using var batchWriter = new StreamBatchWriter(
                                    _streamSync, streamKey, SessionID, ProcessesID, TaskID, config.id, intervalMs: 200);
                                var progress = new Progress<string>(delta => {
                                    batchWriter.Append(delta);
                                });
                                var chatResult = _chatService.SendChatAsync(
                                    modelConfig,
                                    history,
                                    callFunction,
                                    responseFormat: nodeData.ResponseFormat.IsNullOrEmpty() ? "text" : nodeData.ResponseFormat,
                                    enableStreamingObservation: true,
                                    progress: progress,
                                    ct: CancellationToken.None
                                );
                                //var chatResult = _chatService.SendChatAsync(modelConfig, history, callFunction);
                                StringBuilder rawContent = new StringBuilder();
                                Chats info = null;
                                List<Chats> MessageList = [];

                                await foreach (var content in chatResult)
                                {
                                    if (info == null)
                                    {
                                        rawContent.Append(content.ConvertToString());
                                        info = new Chats();
                                        info.Id = Guid.NewGuid().ToString();
                                        info.UserName = AuthorRole.Assistant.ToString();
                                        info.AppId = AppID;
                                        info.Context = content.ConvertToString();
                                        info.CreateTime = DateTime.Now;

                                        MessageList.Add(info);
                                    }
                                    else
                                    {
                                        rawContent.Append(content.ConvertToString());
                                    }
                                    info.Context = rawContent.ToString();
                                }
                                results = info != null ? info.Context : "";

                                await _streamSync.AppendDoneAsync(streamKey, SessionID, ProcessesID, TaskID, config.id, TimeSpan.FromMinutes(10));
                            }
                        }

                        Logs.Add(results);

                        //当主AI返回正常结果则进入下一节点，否则丢弃
                        if (!results.IsNullOrEmpty() && !(results.IndexOf("{\"NewTaskID\":") > -1))
                        {
                            outputs.Add(new Output { varname = "results", value = results, nodeId = config.id, sourceId = $"{config.id}_results" });
                            outputs.Add(new Output { varname = "complete_type", value = "app", nodeId = config.id, sourceId = $"{config.id}_complete_type" });

                            WorkflowNodeInfoBussiness.NextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID: "", config, inputs, outputs, Logs);
                        }

                    }
                    else
                    {
                        throw new Exception("MainAI 模型未配置, LargeModelID 无效");
                    }
                }

                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        public async Task<string> PluginsNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    PluginsData nodeData = JsonConvert.DeserializeObject<PluginsData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        string results = "";

                        if (nodeData.plugins != null)
                        {
                            PluginsInfo pluginsInfo = PluginsInfoBussiness.GetModel(nodeData.plugins.PluginsID);
                            if (pluginsInfo != null)
                            {
                                string namespaceName = RemoveZeroWidthAndControl(pluginsInfo.Namespace);
                                string className = RemoveZeroWidthAndControl(pluginsInfo.ClassName);
                                string methodName = RemoveZeroWidthAndControl(pluginsInfo.EntryFunctionName);

                                // 方案2:从已加载的程序集中查找类型
                                System.Type type = GetTypeFromLoadedAssemblies(namespaceName, className);

                                if (type != null)
                                {
                                    MethodInfo methodInfo = type.GetMethod(methodName);
                                    if (methodInfo != null)
                                    {
                                        // 获取方法的参数信息
                                        ParameterInfo[] methodParams = methodInfo.GetParameters();

                                        // 参数匹配和组织逻辑:按照方法参数顺序匹配
                                        var parameters = new List<object>();

                                        foreach (var methodParam in methodParams)
                                        {
                                            object paramValue = null;

                                            // 在nodeData.inputs中查找匹配的参数定义(通过paramName匹配)
                                            var paramDef = nodeData.inputs.FirstOrDefault(p =>
                                                p.paramName.Equals(methodParam.Name, StringComparison.OrdinalIgnoreCase));

                                            if (paramDef != null)
                                            {
                                                // 通过 sourceId 匹配上一个节点的输出值
                                                var matchedInput = inputs.FirstOrDefault(i => i.sourceId == paramDef.sourceId);

                                                if (matchedInput != null && !string.IsNullOrEmpty(matchedInput.value) && matchedInput.value != "null")
                                                {
                                                    // 使用上一个节点的输出值
                                                    paramValue = ConvertToType(matchedInput.value, paramDef.paramType);
                                                }
                                                else if (!string.IsNullOrEmpty(paramDef.defaultValue) && paramDef.defaultValue != "null")
                                                {
                                                    // 使用默认值
                                                    paramValue = ConvertToType(paramDef.defaultValue, paramDef.paramType);
                                                }
                                            }
                                            else if (methodParam.HasDefaultValue)
                                            {
                                                // 如果方法参数有默认值,使用方法的默认值
                                                paramValue = methodParam.DefaultValue;
                                            }

                                            parameters.Add(paramValue);
                                        }

                                        object result = null;

                                        // 判断方法是静态方法还是实例方法
                                        object instance = null;
                                        if (methodInfo.IsStatic)
                                        {
                                            // 静态方法:直接调用
                                            result = methodInfo.Invoke(null, parameters.ToArray());
                                        }
                                        else
                                        {
                                            instance = _provider.GetService(type) ?? ActivatorUtilities.CreateInstance(_provider, type);
                                            result = methodInfo.Invoke(instance, parameters.ToArray());
                                        }

                                        // 判断是否为异步方法(返回Task或Task<T>)
                                        if (result is System.Threading.Tasks.Task task)
                                        {
                                            // 等待异步任务完成
                                            await task.ConfigureAwait(false);

                                            // 获取Task<T>的结果
                                            var resultProperty = task.GetType().GetProperty("Result");
                                            if (resultProperty != null)
                                            {
                                                result = resultProperty.GetValue(task);
                                            }
                                            else
                                            {
                                                result = null;
                                            }
                                        }

                                        results = JsonConvert.SerializeObject(result);

                                        ExecutionRecordStatus = ExecutionRecordStatus.Success;

                                    }
                                    else
                                    {
                                        results = "Method Not Found.";
                                        ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                                    }
                                }
                                else
                                {
                                    results = "Plugin Class Not Found.";
                                    ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                                }
                            }
                            else
                            {
                                results = "No Plugins Selected.";
                                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                            }
                        }
                        else
                        {
                            results = "No Plugins Selected.";
                        }


                        outputs.Add(new Output { varname = "results", value = results, nodeId = config.id, sourceId = $"{config.id}_results" });

                        if (ExecutionRecordStatus == ExecutionRecordStatus.Success)
                        {
                            WorkflowNodeInfoBussiness.NextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, config, inputs, outputs, Logs);
                        }
                    }
                    else
                    {
                        ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                    }
                }
                else
                {
                    ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                }


            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        public async Task<string> SelectorNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    SelectorData nodeData = JsonConvert.DeserializeObject<SelectorData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        string results = "";

                        //获取所有输出
                        List<WorkflowEdgeInfo> edgeList = WorkflowEdgeInfoBussiness.GetListBySourceNodeId(config.id);
                        if (edgeList != null && edgeList.Count > 0)
                        {
                            List<string> TargetNodeId = new List<string>();
                            string selectorId = "";

                            var comparisonActions = new Dictionary<string, Func<string, string, bool>>()
                                {
                                    { "=", (inputValue, selectorValue) => inputValue == selectorValue },
                                    { "!=", (inputValue, selectorValue) => inputValue != selectorValue },
                                    { ">", (inputValue, selectorValue) => decimal.TryParse(inputValue, out var d1) && decimal.TryParse(selectorValue, out var d2) && d1 > d2 },
                                    { "<", (inputValue, selectorValue) => decimal.TryParse(inputValue, out var d1) && decimal.TryParse(selectorValue, out var d2) && d1 < d2 },
                                    { "~=", (inputValue, selectorValue) => inputValue.Contains(selectorValue) },
                                    { "!~=", (inputValue, selectorValue) => !inputValue.Contains(selectorValue) }
                                };

                            //条件判断
                            foreach (var selector in nodeData.selector)
                            {
                                var input = inputs.FirstOrDefault(i => i.varname == selector.varname);
                                if (input == null) continue;

                                var comparison = selector.comparison;
                                var inputValue = input.value;
                                var selectorValue = selector.value;

                                if (comparisonActions.TryGetValue(comparison, out var action) && action(inputValue, selectorValue))
                                {
                                    selectorId = selector.id;
                                    break;
                                }
                            }
                            if (!selectorId.IsNullOrEmpty())
                            {
                                //获取TargetNodeId
                                foreach (var edge in edgeList)
                                {
                                    var _config = JObject.Parse(JsonConvert.SerializeObject(edge.Config));

                                    if (selectorId == (string)_config["sourceHandle"])
                                    {
                                        TargetNodeId.Add(edge.TargetNodeId);
                                    }
                                }

                                List<WorkflowNodeInfo> targetNodeList = WorkflowNodeInfoBussiness.GetListByNodeID(string.Join(",", TargetNodeId.Select(id => $"'{id}'")));
                                if (targetNodeList != null)
                                {
                                    //把选择器的输入参数转为输出参数
                                    outputs = inputs.Select(i => new Output { varname = i.varname, value = i.value, type = i.type, txt = i.txt }).ToList();

                                    foreach (var node in targetNodeList)
                                    {
                                        NodeConfig targetNode = new NodeConfig() { id = node.NodeID, mainid = config.mainid, workflowid = node.WorkflowID, type = node.NodeType, data = node.Config };

                                        string newTaskID = TaskInfoBussiness.toTask(config, outputs, targetNode, AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID);

                                        Logs.Add($"{newTaskID}");
                                    }
                                }
                            }
                        }
                    }
                }

                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        public async Task<string> MergeNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";

            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;

            // 单记录幂等：优先查找是否已有本轮唯一记录
            try
            {
                var existed = WorkflowNodeExecutionRecordInfoBussiness.GetListByNodeId(SessionID, $"'{config.id}'", ProcessesID);
                var existedRecord = existed?.FirstOrDefault();
                if (existedRecord != null)
                {
                    RecordID = existedRecord.RecordID;
                }
                else
                {
                    // 在锁下创建唯一记录
                    var dlock = new DistributedLock();
                    string createLockKey = $"MergeExec:{SessionID}:{ProcessesID}:{config.id}";
                    string createLockVal = Guid.NewGuid().ToString();
                    bool acquired = dlock.TryAcquire(createLockKey, createLockVal, TimeSpan.FromSeconds(20));
                    try
                    {
                        // 再查一次，避免重复创建
                        existed = WorkflowNodeExecutionRecordInfoBussiness.GetListByNodeId(SessionID, $"'{config.id}'", ProcessesID);
                        existedRecord = existed?.FirstOrDefault();
                        if (existedRecord != null)
                        {
                            RecordID = existedRecord.RecordID;
                        }
                        else if (acquired)
                        {
                            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);
                        }
                    }
                    finally
                    {
                        if (acquired)
                        {
                            dlock.Release(createLockKey, createLockVal);
                        }
                    }
                }
            }
            catch(Exception ex) {
                Logs.Add($"单记录幂等报错:{ex.Message}");
            }

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    MergeData nodeData = JsonConvert.DeserializeObject<MergeData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        //获取输入参数
                        List<WorkflowEdgeInfo> edgeList = WorkflowEdgeInfoBussiness.GetListByTargetNodeId(config.id);
                        if (edgeList?.Count > 0)
                        {
                            // 一次性构造上游节点列表并查询记录
                            var sourceNodeIds = edgeList.Select(e => e.SourceNodeId)
                                                        .Where(id => !string.IsNullOrEmpty(id))
                                                        .Distinct()
                                                        .ToList();

                            var records = WorkflowNodeExecutionRecordInfoBussiness.GetListByNodeId(SessionID, sourceNodeIds, ProcessesID);

                            if (records?.Count > 0)
                            {
                                // 按需求过滤状态
                                if (nodeData.allowFailure)
                                {
                                    records = records.Where(r => r.Status != ExecutionRecordStatus.Running).ToList();
                                }
                                else
                                {
                                    records = records.Where(r => r.Status == ExecutionRecordStatus.Success).ToList();
                                }

                                // 每个上游节点仅保留“最新一条”
                                var picked = records
                                    .GroupBy(r => r.NodeID)
                                    .Select(g => g
                                        .OrderByDescending(r => r.EndTime ?? DateTime.MinValue)
                                        .ThenByDescending(r => r.StartTime ?? DateTime.MinValue)
                                        .First())
                                    .ToList();

                                // 上游节点是否全部就绪
                                if (picked.Count == sourceNodeIds.Count)
                                {
                                    Logs.Add($"上游节点:{edgeList.Count},全部执行完成。");

                                    // 聚合上游输出
                                    foreach (var record in picked)
                                    {
                                        if (record.Outputs != null)
                                        {
                                            List<Output> recordOutputs = JsonConvert.DeserializeObject<List<Output>>(record.Outputs.ToString());
                                            foreach (var output in recordOutputs)
                                            {
                                                string originalSourceId = !string.IsNullOrEmpty(output.sourceId) ? output.sourceId : $"{record.NodeID}_{output.varname}";
                                                string mergeNodeOutputId = $"{config.id}_{originalSourceId}";
                                                var mergedOutput = new Output
                                                {
                                                    varname = output.varname,
                                                    type = output.type,
                                                    value = output.value,
                                                    txt = output.txt,
                                                    id = mergeNodeOutputId,
                                                    sourceId = mergeNodeOutputId,
                                                    nodeId = config.id,
                                                    displayText = output.displayText,
                                                    originalSourceId = originalSourceId,
                                                    originalNodeId = record.NodeID
                                                };
                                                outputs.Add(mergedOutput);
                                            }
                                        }
                                    }

                                    ExecutionRecordStatus = ExecutionRecordStatus.Success;
                                    Logs.Add($"合并整理上游输出:{JsonConvert.SerializeObject(outputs)}。");

                                    // 只触发一次下游
                                    var dlock = new DistributedLock();
                                    string doneLockKey = $"MergeDone:{SessionID}:{ProcessesID}:{config.id}";
                                    string doneLockVal = Guid.NewGuid().ToString();
                                    if (dlock.TryAcquire(doneLockKey, doneLockVal, TimeSpan.FromDays(1)))
                                    {
                                        try
                                        {
                                            if (!RecordID.IsNullOrEmpty())
                                            {
                                                WorkflowNodeExecutionRecordInfoBussiness.Update(RecordID, ExecutionRecordStatus, outputs, Logs);
                                            }
                                            WorkflowNodeInfoBussiness.NextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, config, inputs, outputs, Logs);
                                        }
                                        finally
                                        {
                                            dlock.Release(doneLockKey, doneLockVal);
                                        }
                                    }
                                    else
                                    {
                                        // 非首个完成者，仅更新日志/状态，不触发下游
                                        if (!RecordID.IsNullOrEmpty())
                                        {
                                            WorkflowNodeExecutionRecordInfoBussiness.Update(RecordID, ExecutionRecordStatus, outputs, Logs);
                                        }
                                    }
                                }
                                else
                                {
                                    // 上游节点未全部执行完毕
                                    Logs.Add($"上游节点:{edgeList.Count},已执行完成:{picked?.Count}");
                                    ExecutionRecordStatus = ExecutionRecordStatus.Running;
                                    if (!RecordID.IsNullOrEmpty())
                                    {
                                        WorkflowNodeExecutionRecordInfoBussiness.Update(RecordID, ExecutionRecordStatus, outputs, Logs);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                if (!RecordID.IsNullOrEmpty())
                {
                    WorkflowNodeExecutionRecordInfoBussiness.Update(RecordID, ExecutionRecordStatus, outputs, Logs);
                }
            }

            // 如果创建过记录，则统一一次更新，保证状态可见
            if (!RecordID.IsNullOrEmpty())
            {
                ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            }

            return RecordID;
        }
        public async Task<string> MCPNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";

            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);


            //将TaskData中的附件和附加信息取出来，单独处理
            Inputs attachmentInput = inputs.FirstOrDefault(i => i.varname == "attachments");
            Inputs additionalOptions = inputs.FirstOrDefault(i => i.varname == "additionalOptions");

            if (attachmentInput != null)
            {
                data.AttachmentItems = JsonConvert.DeserializeObject<List<AttachmentItem>>(attachmentInput.value);
            }
            if (additionalOptions != null)
            {
                data.AdditionalOptions = JsonConvert.DeserializeObject(additionalOptions.value);
            }


            var streamKey = StreamKey.Build(SessionID, ProcessesID);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    MCPData nodeData = JsonConvert.DeserializeObject<MCPData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        nodeData.prompt = await this.ReplacePromptValue(nodeData.prompt, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
                        string results = "";

                        string mcp_config = nodeData.config;

                        if (nodeData.model?.LargeModelID > 0)
                        {
                            ChatHistory history = new ChatHistory();
                            LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(nodeData.model.LargeModelID);

                            //设置系统提示词
                            if (!nodeData.prompt.IsNullOrEmpty())
                            {
                                history.AddSystemMessage(nodeData.prompt);
                            }
                            LargeModelConfig modelConfig = new LargeModelConfig();
                            modelConfig.Id = largeModel.LargeModelID.ToString();
                            modelConfig.Model = largeModel;
                            modelConfig.Mcp = McpInfoBussiness.GetModel(nodeData.mcp.MCPID);
                            if (modelConfig.Mcp != null)
                            {
                                modelConfig.Mcp.Config = mcp_config;
                            }
                            modelConfig.Temperature = nodeData.temperature;
                            modelConfig.TopPCoefficient = nodeData.topp;
                            modelConfig.ResponseFormat = nodeData.ResponseFormat;
                            modelConfig.Thinking = nodeData.Thinking;

                            if (data.AttachmentItems?.Count > 0)
                            {
                                if (nodeData.CanReadPic || nodeData.CanReadDoc)
                                {
                                    history = await ZSN.AI.Node.Utils.Utils.AttachmentToChatHistoryAsync(data.AttachmentItems, history);
                                }
                            }
                            using var batchWriter = new StreamBatchWriter(
                                _streamSync, streamKey, SessionID, ProcessesID, TaskID, config.id, intervalMs: 200);
                            var progress = new Progress<string>(delta => {
                                batchWriter.Append(delta);
                            });
                            var chatResult = _chatService.SendChatAsync(
                                modelConfig,
                                history,
                                Function: null,
                                responseFormat: nodeData.ResponseFormat.IsNullOrEmpty() ? "text" : nodeData.ResponseFormat,
                                enableStreamingObservation: true,
                                progress: progress,
                                ct: CancellationToken.None
                            );
                            //var chatResult = _chatService.SendChatAsync(modelConfig, history);
                            StringBuilder rawContent = new StringBuilder();
                            Chats info = null;
                            List<Chats> MessageList = [];

                            await foreach (var content in chatResult)
                            {
                                if (info == null)
                                {
                                    rawContent.Append(content.ConvertToString());
                                    info = new Chats();
                                    info.Id = Guid.NewGuid().ToString();
                                    info.UserName = AuthorRole.Assistant.ToString();
                                    info.AppId = AppID;
                                    info.Context = content.ConvertToString();
                                    info.CreateTime = DateTime.Now;

                                    MessageList.Add(info);
                                }
                                else
                                {
                                    rawContent.Append(content.ConvertToString());
                                }
                                info.Context = rawContent.ToString();
                            }
                            results = info?.Context ?? "";
                            await _streamSync.AppendDoneAsync(streamKey, SessionID, ProcessesID, TaskID, config.id, TimeSpan.FromMinutes(10));
                        }
                        Logs.Add(results);

                        //解析MCP结果并填充outputs中

                        outputs.Add(new Output { varname = "results", value = results, nodeId = config.id, sourceId = $"{config.id}_results" });

                        WorkflowNodeInfoBussiness.NextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, config, inputs, outputs, Logs);
                    }
                }

                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }
        public async Task<string> AgentNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    AgentData nodeData = JsonConvert.DeserializeObject<AgentData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        string results = "";

                        AgentNodeID = AgentNodeID.IsNullOrEmpty() ? config.id : AgentNodeID;
                        string AgentID = nodeData.agent.AgentID;

                        //判断是否为AgentEndTask，即Agent子任务结束的返回
                        if (config.fromNodeType == NodeType.AgentEnd)
                        {
                            //FromMainTaskID不为空代表是MainAI调用的Agent子任务,需要特殊处理
                            if (!FromMainTaskID.IsNullOrEmpty())
                            {
                                //获取任务开始MainAI的prompt
                                TaskInfo mainAITask = TaskInfoBussiness.GetModel(FromMainTaskID);
                                List<Inputs> mainAIInput = mainAITask.TaskConfig.Data.Inputs;

                                inputs = inputs.Concat(mainAIInput).ToList();

                                //在NextNode中将输入参数转为输出参数，所以这里只需要初始化一个outputs
                                //List<Output> outputs = new List<Output>();

                                ProcessesID = mainAITask.TaskConfig.Data.ProcessesID;//回顾主任务线
                            }
                            else
                            {
                                //outputs.Add(new Output { varname = "results", value = results, nodeId = config.id, sourceId = $"{config.id}_results" });
                            }

                            outputs = inputs.Select(inputs => new Output
                            {
                                id = Guid.NewGuid().ToString(),
                                sourceId = $"{config.id}_{inputs.varname}",
                                varname = inputs.varname,
                                value = inputs.value,
                                type = inputs.type,
                                txt = inputs.txt,
                                nodeId = config.id
                            }).ToList();

                            WorkflowNodeInfoBussiness.NextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, config, inputs, outputs, Logs);
                        }
                        else
                        {
                            //多个工作流的处理
                            var dataMsg = new StringBuilder();
                            List<WorkflowInfo> workflowInfos = WorkflowInfoBussiness.GetListByAgentID(AgentID);
                            if (workflowInfos != null)
                            {
                                List<string> WorkflowIDs = new List<string>();
                                //如果存在多个工作流，则由Agent主导模型判断调用工作流
                                if (workflowInfos.Count > 1)
                                {
                                    //List<WorkFlowProcesses> workFlowProcesses = new List<WorkFlowProcesses>();
                                    var kernelArguments = new KernelArguments();

                                    dataMsg.AppendLine("#你是一个任务分配能力极强的管理人员，可以合理的按照\"{{$input}}\"所描述的任务要求分析出需要使用一个或者多个工作流（Workflow）来完成工作。");
                                    dataMsg.AppendLine("");

                                    dataMsg.AppendLine("#这些是上个工作节点输出的一些参数");
                                    foreach (var input in inputs)
                                    {
                                        kernelArguments.Add(input.varname, input.value);

                                        dataMsg.AppendLine($"{input.varname}:");
                                        dataMsg.AppendLine($"{{${input.varname}}}");
                                        dataMsg.AppendLine("");
                                    }
                                    dataMsg.AppendLine("");

                                    dataMsg.AppendLine("#这些是本次任务的共用标识参数");
                                    dataMsg.AppendLine($"AppID:{AppID}");
                                    dataMsg.AppendLine($"SessionID:{SessionID}");
                                    dataMsg.AppendLine("");

                                    dataMsg.AppendLine("#这些是可以选择的工作流（Workflow）的参数列表，每一个工作流都有一个Workflow_Name来描述该工作流的名称，Description来描述该工作流具体可以处理解决的问题,WorkflowID是该工作流的标识符，是需要你判断后返回的参数。");
                                    dataMsg.AppendLine("WorkflowInfoList:[");
                                    foreach (var workflowInfo in workflowInfos)
                                    {
                                        //string _ProcessesID = Guid.NewGuid().ToString();
                                        dataMsg.AppendLine($"#{workflowInfo.WorkflowName}");
                                        dataMsg.AppendLine($"Workflow_Name:{workflowInfo.WorkflowName}");
                                        dataMsg.AppendLine($"Description:{workflowInfo.Description}");
                                        //dataMsg.AppendLine($"ProcessesID:{_ProcessesID}");
                                        dataMsg.AppendLine($"WorkflowID:{workflowInfo.WorkflowID}");

                                        //workFlowProcesses.Add(new WorkFlowProcesses() { WorkflowID = workflowInfo.WorkflowID, ProcessesID = _ProcessesID });
                                    }
                                    dataMsg.AppendLine("]");
                                    dataMsg.AppendLine("");

                                    dataMsg.AppendLine("#你可以根据任务要求判断是否与具体工作流(Workflow)相关（通过WorkflowInfoList中的Workflow_Name以及Description来分析是与合任务要求相关）,如果你觉得多个工作流相关则返回多个WorkflowID，并用逗号(\",\")隔开，不用返回WorkflowID以外的其他信息。");
                                    dataMsg.AppendLine("");

                                    CallFunction callFunction = new CallFunction();
                                    callFunction.Prompt = dataMsg.ToString();

                                    //查找默认的输入是varname = input
                                    callFunction.Input = "" + inputs.Find(x => x.varname == "input")?.value;
                                    //如果没有找到，则取varname = prompt
                                    callFunction.Input = callFunction.Input.IsNullOrEmpty() ? "" + inputs.FirstOrDefault(x => x.varname == "prompt")?.value : callFunction.Input;

                                    LargeModelConfig modelConfig = new LargeModelConfig();
                                    modelConfig.Model = LargeModelInfoBussiness.GetModel(nodeData.agent.SessionModelID ?? 0);
                                    modelConfig.Temperature = nodeData.agent.TemperatureCoefficient;
                                    modelConfig.TopPCoefficient = nodeData.agent.TopPCoefficient;

                                    var functionCallRe = _chatService.PromptFunctionCall(modelConfig, callFunction, kernelArguments);
                                    string response_str = "";
                                    await foreach (var response in functionCallRe)
                                    {
                                        response_str += response;
                                    }

                                    if (!response_str.IsNullOrEmpty())
                                    {
                                        WorkflowIDs = response_str.Trim()
                                                                 .Split(',', StringSplitOptions.RemoveEmptyEntries)   // 去掉空元素  
                                                                 .ToList();

                                    }
                                }
                                else
                                {
                                    WorkflowIDs.Add(workflowInfos[0].WorkflowID);
                                }


                                if (WorkflowIDs.Count > 0)
                                {
                                    List<string> TaskIDs = new List<string>();
                                    string inputVarValue = inputs.FirstOrDefault(x => x.varname == "input") != null ? inputs.FirstOrDefault(x => x.varname == "input")?.value : inputs.FirstOrDefault(x => x.varname == "prompt")?.value;

                                    foreach (var workflowID in WorkflowIDs)
                                    {
                                        string NewTaskID = "";
                                        WorkflowNodeInfo workflowNode = WorkflowNodeInfoBussiness.GetWorkFlowAgentStartNode(workflowID);
                                        if (workflowNode != null)
                                        {
                                            NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(workflowNode.Config.ToString());
                                            if (nodeConfig != null)
                                            {
                                                NodeConfig targetNode = new NodeConfig() { id = workflowNode.NodeID, mainid = nodeConfig.mainid, workflowid = workflowNode.WorkflowID, type = workflowNode.NodeType, data = nodeConfig };

                                                //每次循环创建独立的outputs，避免多工作流间输出累积
                                                var workflowOutputs = new List<Output>
                                                {
                                                    new Output() { varname = "input", value = inputVarValue ?? "" },
                                                    new Output { varname = "currentTime", value = DateTime.Now.ToDateTimeString(), nodeId = config.id, sourceId = $"{config.id}_currentTime" },
                                                    new Output { varname = "agentName", value = nodeData.label, nodeId = config.id, sourceId = $"{config.id}_agentName" }
                                                };

                                                //string _ProcessesID = Guid.NewGuid().ToString();

                                                NewTaskID = TaskInfoBussiness.toTask(config, workflowOutputs, targetNode, AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID);

                                                TaskIDs.Add(NewTaskID);
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        public async Task<string> KnowledgeBaseNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);
            string results = "";

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    KnowledgeBaseData nodeData = JsonConvert.DeserializeObject<KnowledgeBaseData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        nodeData.searchOptions?.VectorWeight = nodeData.searchOptions?.VectorWeight / 100 ?? 0.6f;
                        nodeData.searchOptions?.GraphWeight = nodeData.searchOptions?.GraphWeight / 100 ?? 0.4f;

                        List<KnowledgeBaseUnit> KnowledgeBaseUnits = new List<KnowledgeBaseUnit>();

                        foreach (var knowledgeBase in nodeData.knowledgeBase)
                        {
                            KnowledgeBaseUnit knowledgeBaseUnit = new KnowledgeBaseUnit();

                            knowledgeBaseUnit.KnowledgeBaseInfo = knowledgeBase;
                            var chatModel = LargeModelInfoBussiness.GetModel(knowledgeBase.PreprocessModelID);
                            var embedModel = LargeModelInfoBussiness.GetModel(knowledgeBase.VectorModelID);

                            knowledgeBaseUnit.LargeModelUnit = new LargeModelUnit();
                            knowledgeBaseUnit.LargeModelUnit.ChatModel = new LargeModelConfig()
                            {
                                Id = knowledgeBase.PreprocessModelID.ToString(),
                                Model = chatModel,
                                TopPCoefficient = nodeData.topp,
                                Temperature = nodeData.temperature,
                                //Prompt = nodeData.prompt
                            };

                            knowledgeBaseUnit.LargeModelUnit.VectorModel = new LargeModelConfig()
                            {
                                Id = knowledgeBase.VectorModelID.ToString(),
                                Model = embedModel,
                                TopPCoefficient = nodeData.topp,
                                Temperature = nodeData.temperature,
                                //Prompt = nodeData.prompt
                            };

                            KnowledgeBaseUnits.Add(knowledgeBaseUnit);
                        }

                        string questions = nodeData.prompt.IsNullOrEmpty() ? JsonConvert.SerializeObject(inputs) : await this.ReplacePromptValue(nodeData.prompt, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);

                        
                        // ========== 混合检索逻辑 ==========
                        Logs.Add($"[KnowledgeBase] 开始混合检索,查询: {questions}");
                        Logs.Add($"[KnowledgeBase] 检索配置 - MaxVectorResults: {nodeData.searchOptions.MaxVectorResults}, VectorWeight: {nodeData.searchOptions.VectorWeight}, GraphWeight: {nodeData.searchOptions.GraphWeight}");

                        // 获取混合检索服务
                        var hybridSearchService = _provider.GetService<IHybridSearchService>();
                        if (hybridSearchService == null)
                        {
                            throw new Exception("IHybridSearchService 服务未注册，请检查依赖注入配置");
                        }

                        // 存储所有知识库的检索结果
                        var allSearchResults = new List<ZSN.AI.Entity.KnowledgeBase.SearchResult>();
                        var allChunkImages = new Dictionary<string, List<ZSN.AI.Entity.KnowledgeBase.ImageSearchResult>>();
                        int totalChunks = 0;

                        // 遍历所有知识库进行检索
                        foreach (var knowledgeBase in nodeData.knowledgeBase)
                        {
                            try
                            {
                                Logs.Add($"[KnowledgeBase] 检索知识库: {knowledgeBase.Name} (ID: {knowledgeBase.KnowledgeBaseID})");

                                List<KnowledgeBaseFileInfo> knowledgeBaseFiles = KnowledgeBaseFileInfoBussiness.GetList($" KnowledgeBaseID = '{knowledgeBase.KnowledgeBaseID}' and SystemStatus={(int)ImportKmsStatus.Success}");

                                foreach(var  knowledgeBaseFileInfo in knowledgeBaseFiles)
                                {
                                    // 执行混合检索
                                    var searchResult = await hybridSearchService.SearchAsync(
                                        query: questions,
                                        knowledgeBaseId: knowledgeBaseFileInfo.FileID.ToString(),
                                        options: nodeData.searchOptions,
                                        cancellationToken: default);

                                    if (searchResult?.FusedResults != null && searchResult.FusedResults.Count > 0)
                                    {
                                        allSearchResults.AddRange(searchResult.FusedResults);
                                        totalChunks += searchResult.FusedResults.Count;
                                        Logs.Add($"[KnowledgeBase] 检索到 {searchResult.FusedResults.Count} 个相关文档块");

                                        // 收集图片信息
                                        if (searchResult.ChunkImages != null && searchResult.ChunkImages.Count > 0)
                                        {
                                            foreach (var kv in searchResult.ChunkImages)
                                                allChunkImages[kv.Key] = kv.Value;
                                        }

                                        // 记录前3个结果的得分
                                        for (int i = 0; i < Math.Min(3, searchResult.FusedResults.Count); i++)
                                        {
                                            var result = searchResult.FusedResults[i];
                                            Logs.Add($"  - 结果 {i + 1}: Score={result.FusedScore:F4}, Content={result.Content.Substring(0, Math.Min(50, result.Content.Length))}...");
                                        }
                                    }
                                    else
                                    {
                                        Logs.Add($"[KnowledgeBase] 未检索到相关内容");
                                    }
                                }
                                
                            }
                            catch (Exception ex)
                            {
                                Logs.Add($"[KnowledgeBase] 检索知识库 {knowledgeBase.Name} 失败: {ex.Message}");
                                _logger.LogError(ex, $"检索知识库 {knowledgeBase.KnowledgeBaseID} 失败");
                            }
                        }

                        // 按融合得分排序,取MaxVectorResults个结果
                        var topResults = allSearchResults
                            .OrderByDescending(r => r.FusedScore)
                            .Take(nodeData.searchOptions.MaxVectorResults)
                            .ToList();

                        Logs.Add($"[KnowledgeBase] 混合检索完成，共检索到 {totalChunks} 个文档块，取Top {topResults.Count} 个结果");

                        // 构建检索结果文本
                        var resultBuilder = new StringBuilder();
                        resultBuilder.AppendLine($"# 知识库检索结果 (共 {topResults.Count} 条)");
                        resultBuilder.AppendLine($"查询: {questions}");
                        resultBuilder.AppendLine();

                        for (int i = 0; i < topResults.Count; i++)
                        {
                            var result = topResults[i];
                            resultBuilder.AppendLine($"## 结果 {i + 1} (得分: {result.FusedScore:F4})");
                            resultBuilder.AppendLine($"**来源**: {result.Source}");
                            resultBuilder.AppendLine($"**文档ID**: {result.DocumentId}");
                            resultBuilder.AppendLine($"**块ID**: {result.ChunkId}");
                            resultBuilder.AppendLine();
                            resultBuilder.AppendLine($"**内容**:");
                            resultBuilder.AppendLine(result.Content);
                            resultBuilder.AppendLine();

                            // 如果有相关路径信息，也添加进去
                            if (result.RelatedPaths != null && result.RelatedPaths.Count > 0)
                            {
                                resultBuilder.AppendLine($"**相关路径**: {result.RelatedPaths.Count} 条");
                                foreach (var path in result.RelatedPaths.Take(3))
                                {
                                    resultBuilder.AppendLine($"  - 路径节点数: {path.Nodes.Count}, 相关性得分: {path.RelevanceScore:F4}");
                                }
                                resultBuilder.AppendLine();
                            }

                            // 关联图片信息
                            if (allChunkImages.TryGetValue(result.ChunkId, out var images) && images.Count > 0)
                            {
                                resultBuilder.AppendLine($"**关联图片**: {images.Count} 张");
                                foreach (var img in images)
                                {
                                    if (!string.IsNullOrEmpty(img.ImageUrl))
                                    {
                                        var altText = !string.IsNullOrEmpty(img.Description) ? img.Description : img.ImageId;
                                        resultBuilder.AppendLine($"  ![{altText}]({img.ImageUrl})");
                                    }
                                    else
                                    {
                                        resultBuilder.AppendLine($"  - 图片ID: {img.ImageId}");
                                    }
                                    if (!string.IsNullOrEmpty(img.Description))
                                        resultBuilder.AppendLine($"    描述: {img.Description}");
                                    if (!string.IsNullOrEmpty(img.OcrText))
                                        resultBuilder.AppendLine($"    OCR文字: {img.OcrText}");
                                }
                                resultBuilder.AppendLine();
                            }

                            resultBuilder.AppendLine("---");
                            resultBuilder.AppendLine();
                        }

                        results = resultBuilder.ToString();
                        Logs.Add($"[KnowledgeBase] 检索结果已格式化，总长度: {results.Length} 字符");
                    }
                    //Outputs.Add(results);
                    Logs.Add(results);

                    //<Output> outputs = new List<Output>();
                    outputs.Add(new Output { varname = "results", value = results, nodeId = config.id, sourceId = $"{config.id}_results" });

                    WorkflowNodeInfoBussiness.NextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, config, inputs, outputs, Logs);
                }

                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        public async Task<IList<McpClientTool>> GetMcpClientToolsAsync(MCPConfig mcpConfig)
        {
            return await _chatService.GetMcpClientToolsAsync(mcpConfig);
        }

        public string FileToMarkdownNode(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            //将TaskData中的附件和附加信息取出来，单独处理
            Inputs attachmentInput = inputs.FirstOrDefault(i => i.varname == "attachments");
            Inputs additionalOptions = inputs.FirstOrDefault(i => i.varname == "additionalOptions");

            if (attachmentInput != null)
            {
                data.AttachmentItems = JsonConvert.DeserializeObject<List<AttachmentItem>>(attachmentInput.value);
            }
            if (additionalOptions != null)
            {
                data.AdditionalOptions = JsonConvert.DeserializeObject(additionalOptions.value);
            }

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    FileToMarkdownData nodeData = JsonConvert.DeserializeObject<FileToMarkdownData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        var _attachmentsString = JsonConvert.SerializeObject(data.AttachmentItems);
                        var _additionalOptionsString = JsonConvert.SerializeObject(data.AdditionalOptions);

                        inputs.Add(new Inputs() { varname = "attachments", type = "List<AttachmentItem>", value = _attachmentsString, sourceId = $"{config.id}_attachments" });


                        List<ToMarkdownFile> sourceFile = new List<ToMarkdownFile>();

                        foreach (var item in data.AttachmentItems)
                        {
                            if (item != null && !item.FilePath.IsNullOrEmpty() && !item.Name.IsNullOrEmpty())
                            {
                                sourceFile.Add(new ToMarkdownFile()
                                {
                                    FilePath = item.FilePath,
                                    FileName = item.Name,
                                    FileCode = item.FileCode
                                });
                            }
                        }

                        string reCallUrl = ConfigHelper.GetString("FileToMarkdownReCallUrl").Replace("{sessionID}", SessionID).Replace("{taskID}", TaskID).Replace("{appID}", AppID).Replace("{recordID}", RecordID);

                        string _TaskID = Guid.NewGuid().ToString();

                        TaskInfo taskInfo = new TaskInfo();
                        taskInfo.TaskID = _TaskID;
                        taskInfo.TaskType = NodeType.NotNode_Markdown;
                        taskInfo.TaskConfig = new TaskConfig();
                        taskInfo.TaskConfig.NodeConfig = null;
                        taskInfo.TaskConfig.NotNodeConfig = new MarkdownConfig() { sourceFile = sourceFile, reCallUrl = reCallUrl, prompt = nodeData.prompt, reCallDataType = ReCallDataType.Markdown };
                        taskInfo.TaskConfig.Data = data;
                        taskInfo.LoopType = LoopType.NOLoop;
                        taskInfo.RepeatValue = 1;
                        taskInfo.RedoCount = 0;
                        taskInfo.CreateTime = DateTime.Now;
                        taskInfo.UpdateTime = DateTime.Now;
                        taskInfo.FromTaskID = TaskID;
                        taskInfo.FromMainTaskID = "";

                        TaskInfoBussiness.Add(taskInfo);
                    }
                }
                ExecutionRecordStatus = ExecutionRecordStatus.Running;
            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }

            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        public async Task<string> HumanInTheLoopNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();
            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    HumanInTheLoopData nodeData = JsonConvert.DeserializeObject<HumanInTheLoopData>(JsonConvert.SerializeObject(config.data));
                    if (nodeData != null)
                    {
                        Logs.Add($"等待人工干预处理。SessionID:{SessionID},ProcessesID:{ProcessesID}");

                        nodeData.askContent = await this.ReplacePromptValue(nodeData.askContent, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
                        outputs.Add(new Output { varname = "askContent", value = nodeData.askContent, nodeId = config.id, sourceId = $"{config.id}_askContent" });

                        outputs.Add(new Output { varname = "optionMode", value = nodeData.optionMode, nodeId = config.id, sourceId = $"{config.id}_optionMode" });

                        if (nodeData.optionMode == "fixed")
                        {
                            outputs.Add(new Output { varname = "options", value = JsonConvert.SerializeObject(nodeData.options), nodeId = config.id, sourceId = $"{config.id}_options" });
                        }
                        if (nodeData.optionMode == "dynamic")
                        {
                            try
                            {
                                nodeData.options = JsonConvert.DeserializeObject<List<Entity.Option>>(await this.ReplacePromptValue(nodeData.dynamicOptionsVar, inputs, config.fromNodeId, SessionID, AppID, ProcessesID));


                                outputs.Add(new Output { varname = "options", value = JsonConvert.SerializeObject(nodeData.options), nodeId = config.id, sourceId = $"{config.id}_options" });
                            }
                            catch (Exception _ex)
                            {
                                Logs.Add(_ex.Message);
                                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                            }
                        }


                        string reCallUrl = ConfigHelper.GetString("HumanOperationReCallUrl").Replace("{sessionID}", SessionID).Replace("{taskID}", TaskID).Replace("{appID}", AppID).Replace("{recordID}", RecordID);

                        //输出回调地址，前端人工处理完成后将结果post，调用该地址通知系统继续执行后续节点
                        outputs.Add(new Output { varname = "reCallUrl", value = reCallUrl, nodeId = config.id, sourceId = $"{config.id}_reCallUrl" });
                        outputs.Add(new Output { varname = "sessionID", value = SessionID, nodeId = config.id, sourceId = $"{config.id}_sessionID" });
                        outputs.Add(new Output { varname = "taskID", value = TaskID, nodeId = config.id, sourceId = $"{config.id}_taskID" });
                        outputs.Add(new Output { varname = "appID", value = AppID, nodeId = config.id, sourceId = $"{config.id}_appID" });
                        outputs.Add(new Output { varname = "recordID", value = RecordID, nodeId = config.id, sourceId = $"{config.id}_recordID" });
                        //设置当前节点执行状态为等待人工处理
                        ExecutionRecordStatus = ExecutionRecordStatus.Running;
                    }
                }

            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }
            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        public async Task<string> HumanInTheLoopInputNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();
            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    HumanInTheLoopInputData nodeData = JsonConvert.DeserializeObject<HumanInTheLoopInputData>(JsonConvert.SerializeObject(config.data));
                    if (nodeData != null)
                    {
                        Logs.Add($"等待人工干预输入表单内容处理。SessionID:{SessionID},ProcessesID:{ProcessesID}");

                        nodeData.askContent = await this.ReplacePromptValue(nodeData.askContent, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
                        outputs.Add(new Output { varname = "askContent", value = nodeData.askContent, nodeId = config.id, sourceId = $"{config.id}_askContent" });

                        outputs.Add(new Output { varname = "optionMode", value = nodeData.optionMode, nodeId = config.id, sourceId = $"{config.id}_optionMode" });

                        if (nodeData.optionMode == "fixed")
                        {
                            outputs.Add(new Output { varname = "inputOptions", value = JsonConvert.SerializeObject(nodeData.inputOptions), nodeId = config.id, sourceId = $"{config.id}_inputOptions" });
                        }
                        if (nodeData.optionMode == "dynamic")
                        {
                            try
                            {
                                nodeData.dynamicOptionsVar = "{{" + nodeData.dynamicOptionsVar + "}}";
                                string optionsJsonStr = await this.ReplacePromptValue(nodeData.dynamicOptionsVar, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);

                                if (ZSN.Utils.Core.Utils.Utils.TryExtractStrictJson(optionsJsonStr, out var __cleaned))
                                {
                                    optionsJsonStr = __cleaned;
                                }
                                bool _needLLMParse = false;
                                if (optionsJsonStr.IsNullOrEmpty() == false)
                                {
                                    try
                                    {
                                        nodeData.inputOptions = JsonConvert.DeserializeObject<List<Entity.InputOption>>(optionsJsonStr);
                                        _needLLMParse = false;
                                    }
                                    catch
                                    {
                                        _needLLMParse = true;
                                    }
                                }

                                if (_needLLMParse)
                                {
                                    LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(nodeData.model.LargeModelID);

                                    LargeModelConfig ChatModel = new LargeModelConfig();
                                    ChatModel.Id = nodeData.model.LargeModelID.ToString();
                                    ChatModel.Model = largeModel;
                                    ChatModel.TopPCoefficient = 1;
                                    ChatModel.Temperature = 0;
                                    ChatHistory history = new ChatHistory();
                                    CallFunction callFunction = new CallFunction();
                                    var dataMsg = new StringBuilder();
                                    dataMsg.AppendLine("#角色");
                                    dataMsg.AppendLine("你是“JSON格式校验和修正引擎”。你的唯一输出是严格 JSON 数组。");
                                    dataMsg.AppendLine("");
                                    dataMsg.AppendLine("#目标");
                                    dataMsg.AppendLine("-将给你的格式错误的JSON转换为正确的JSON;");
                                    dataMsg.AppendLine("-只输出严格 JSON 数组。");
                                    dataMsg.AppendLine("");
                                    dataMsg.AppendLine("#当前时间");
                                    dataMsg.AppendLine(DateTime.Now.ToString());
                                    dataMsg.AppendLine("");
                                    dataMsg.AppendLine("#输入内容");
                                    dataMsg.AppendLine(optionsJsonStr);
                                    dataMsg.AppendLine("");
                                    dataMsg.AppendLine("#输出通道（必须遵守）");
                                    dataMsg.AppendLine("- 只输出位于 <JSON_OUTPUT> 与 </JSON_OUTPUT> 之间的内容；");
                                    dataMsg.AppendLine("- 禁止输出任何其他文本、说明、Markdown、代码块围栏、注释或多余空格。");
                                    dataMsg.AppendLine("");
                                    dataMsg.AppendLine("#严格格式要求（必须满足全部）");
                                    dataMsg.AppendLine("- 输出必须是一个对象数组。");
                                    dataMsg.AppendLine("- 数组，长度与“预设参数清单”一致，且每一项仅包含 id,name,value,isRequired 四个键，类型分别为 string,string,string,boolean；value 取空字符串表示未提供。");
                                    dataMsg.AppendLine("- JSON 语法要求：所有键和值使用双引号；布尔值为 true/false（小写）；不允许 null/NaN/Infinity/undefined；不允许多余字段、尾随逗号、注释、单引号。");
                                    dataMsg.AppendLine("- 严禁臆造：无法确定的值一律置为空字符串。");
                                    dataMsg.AppendLine("");
                                    dataMsg.AppendLine("#正确示例（示例，非本题答案）");
                                    dataMsg.AppendLine("<JSON_OUTPUT>");
                                    dataMsg.AppendLine("[");
                                    dataMsg.AppendLine("    { \"id\": \"随机字符串编号\", \"name\": \"姓名\", \"value\": \"张三\", \"isRequired\": true }");
                                    dataMsg.AppendLine("]");
                                    dataMsg.AppendLine("</JSON_OUTPUT>");
                                    dataMsg.AppendLine("");
                                    dataMsg.AppendLine("#常见错误（不要这样输出）");
                                    dataMsg.AppendLine("- 使用单引号/缺少引号/尾随逗号/多余字段/markdown 代码块/自然语言说明等。");
                                    dataMsg.AppendLine("");
                                    dataMsg.AppendLine("#最终输出");
                                    dataMsg.AppendLine("- 只输出最终 JSON，且必须仅出现在 <JSON_OUTPUT> 与 </JSON_OUTPUT> 之间。");

                                    history.AddSystemMessage(dataMsg.ToString());
                                    history.AddUserMessage("继续");

                                    callFunction = new CallFunction();
                                    callFunction.Prompt = string.Join("\n", history.Select(x => x.Role + ": " + x.Content));
                                    callFunction.Input = string.Join("\r\n", nodeData.userInputContent);

                                    string llmRaw = string.Empty;
                                    var chatResult = _chatService.PromptFunctionCall(ChatModel, callFunction);
                                    var sb = new StringBuilder();
                                    await foreach (var chunk in chatResult)
                                    {
                                        sb.Append(chunk);
                                    }
                                    llmRaw = sb.ToString();
                                    // 优先提取 <JSON_OUTPUT> 标签内容
                                    var extract = llmRaw;
                                    int tagStart = llmRaw.IndexOf("<JSON_OUTPUT>", StringComparison.OrdinalIgnoreCase);
                                    int tagEnd = llmRaw.IndexOf("</JSON_OUTPUT>", StringComparison.OrdinalIgnoreCase);
                                    if (tagStart >= 0 && tagEnd > tagStart)
                                    {
                                        extract = llmRaw.Substring(tagStart + 13, tagEnd - (tagStart + 13)).Trim();
                                    }
                                    if (ZSN.Utils.Core.Utils.Utils.TryExtractStrictJson(extract, out var ___cleaned))
                                    {
                                        llmRaw = ___cleaned;
                                    }
                                    if (llmRaw.IsNullOrEmpty() == false)
                                    {
                                        nodeData.inputOptions = JsonConvert.DeserializeObject<List<Entity.InputOption>>(llmRaw);
                                    }

                                }

                                outputs.Add(new Output { varname = "inputOptions", value = JsonConvert.SerializeObject(nodeData.inputOptions), nodeId = config.id, sourceId = $"{config.id}_inputOptions" });
                            }
                            catch (Exception _ex)
                            {
                                Logs.Add(_ex.Message);
                                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                            }
                        }

                        bool execNextNode = false;
                        //开始解析用户输入，达到满足表单要求后，才继续往下执行
                        if (nodeData.toExecSwitch)
                        {
                            LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(nodeData.model.LargeModelID);

                            LargeModelConfig ChatModel = new LargeModelConfig();
                            ChatModel.Id = nodeData.model.LargeModelID.ToString();
                            ChatModel.Model = largeModel;
                            ChatModel.TopPCoefficient = 1;// nodeData.topp;
                            ChatModel.Temperature = 0;// nodeData.temperature;

                            ChatHistory history = new ChatHistory();
                            CallFunction callFunction = new CallFunction();
                            var dataMsg = new StringBuilder();
                            dataMsg.AppendLine("#角色");
                            dataMsg.AppendLine("你是“参数抽取与校验引擎”。你的唯一输出是严格 JSON。");
                            dataMsg.AppendLine("");
                            dataMsg.AppendLine("#目标");
                            dataMsg.AppendLine("- 根据“预设参数清单”，从“用户已提供的内容（多轮）”中为每个参数抽取字符串值；");
                            dataMsg.AppendLine("- 如有缺失必填项，仅围绕缺失项生成下一步中文提问；");
                            dataMsg.AppendLine("- 只输出严格 JSON，且字段、类型、顺序与示例完全一致。");
                            dataMsg.AppendLine("");
                            dataMsg.AppendLine("#当前时间");
                            dataMsg.AppendLine(DateTime.Now.ToString());
                            dataMsg.AppendLine("");
                            dataMsg.AppendLine("#预设参数清单");
                            foreach (var option in nodeData.inputOptions)
                            {
                                dataMsg.AppendLine($"参数ID:{option.id}, 参数名称:{option.name}, 必填:{(option.isRequired ? "true" : "false")} ");
                            }
                            dataMsg.AppendLine("");
                            dataMsg.AppendLine("#用户已提供的内容（多轮）");
                            dataMsg.AppendLine(string.Join("\r\n", nodeData.userInputContent));
                            dataMsg.AppendLine("");
                            dataMsg.AppendLine("#输出通道（必须遵守）");
                            dataMsg.AppendLine("- 只输出位于 <JSON_OUTPUT> 与 </JSON_OUTPUT> 之间的内容；");
                            dataMsg.AppendLine("- 禁止输出任何其他文本、说明、Markdown、代码块围栏、注释或多余空格。");
                            dataMsg.AppendLine("");
                            dataMsg.AppendLine("#严格格式要求（必须满足全部）");
                            dataMsg.AppendLine("- 输出必须是一个对象，键仅允许这四个：options, missing, ask, valid。");
                            dataMsg.AppendLine("- options: 数组，长度与“预设参数清单”一致，且每一项仅包含 id,name,value,isRequired 四个键，类型分别为 string,string,string,boolean；value 取空字符串表示未提供。");
                            dataMsg.AppendLine("- missing: 数组，仅包含缺失必填项的 id（isRequired==true 且 value==\"\"），否则为空数组。");
                            dataMsg.AppendLine("- ask: string，当 missing 非空时，给出一条只围绕缺失项、一次性问清的中文提问；当 missing 为空时，值为\"\"。");
                            dataMsg.AppendLine("- valid: boolean，当 missing 为空时为 true，否则为 false。");
                            dataMsg.AppendLine("- JSON 语法要求：所有键和值使用双引号；布尔值为 true/false（小写）；不允许 null/NaN/Infinity/undefined；不允许多余字段、尾随逗号、注释、单引号。");
                            dataMsg.AppendLine("- 严禁臆造：无法确定的值一律置为空字符串。");
                            dataMsg.AppendLine("");
                            dataMsg.AppendLine("#正确示例（示例，非本题答案）");
                            dataMsg.AppendLine("<JSON_OUTPUT>");
                            dataMsg.AppendLine("{");
                            dataMsg.AppendLine("  \"options\": [");
                            dataMsg.AppendLine("    { \"id\": \"name\", \"name\": \"姓名\", \"value\": \"张三\", \"isRequired\": true }");
                            dataMsg.AppendLine("  ],");
                            dataMsg.AppendLine("  \"missing\": [\"name\"],");
                            dataMsg.AppendLine("  \"ask\": \"请提供您的姓名，以便继续。\",");
                            dataMsg.AppendLine("  \"valid\": false");
                            dataMsg.AppendLine("}");
                            dataMsg.AppendLine("</JSON_OUTPUT>");
                            dataMsg.AppendLine("");
                            dataMsg.AppendLine("#常见错误（不要这样输出）");
                            dataMsg.AppendLine("- 使用单引号/缺少引号/尾随逗号/多余字段/markdown 代码块/自然语言说明等。");
                            dataMsg.AppendLine("");
                            dataMsg.AppendLine("#最终输出");
                            dataMsg.AppendLine("- 只输出最终 JSON，且必须仅出现在 <JSON_OUTPUT> 与 </JSON_OUTPUT> 之间。");

                            history.AddSystemMessage(dataMsg.ToString());
                            history.AddUserMessage("继续");

                            callFunction = new CallFunction();
                            callFunction.Prompt = string.Join("\n", history.Select(x => x.Role + ": " + x.Content));
                            callFunction.Input = string.Join("\r\n", nodeData.userInputContent);

                            string llmRaw = string.Empty;
                            try
                            {
                                var chatResult = _chatService.PromptFunctionCall(ChatModel, callFunction);
                                var sb = new StringBuilder();
                                await foreach (var chunk in chatResult)
                                {
                                    sb.Append(chunk);
                                }
                                llmRaw = sb.ToString();
                                // 优先提取 <JSON_OUTPUT> 标签内容
                                var extract = llmRaw;
                                int tagStart = llmRaw.IndexOf("<JSON_OUTPUT>", StringComparison.OrdinalIgnoreCase);
                                int tagEnd = llmRaw.IndexOf("</JSON_OUTPUT>", StringComparison.OrdinalIgnoreCase);
                                if (tagStart >= 0 && tagEnd > tagStart)
                                {
                                    extract = llmRaw.Substring(tagStart + 13, tagEnd - (tagStart + 13)).Trim();
                                }
                                if (ZSN.Utils.Core.Utils.Utils.TryExtractStrictJson(extract, out var __cleaned))
                                {
                                    llmRaw = __cleaned;
                                }

                                HumanInputParseResult parseResult = null;
                                try
                                {
                                    parseResult = JsonConvert.DeserializeObject<HumanInputParseResult>(llmRaw);
                                }
                                catch
                                {
                                    Logs.Add("JSON解析失败:" + llmRaw);
                                    parseResult = null;
                                }

                                if (parseResult != null)
                                {
                                    var ask = parseResult.ask ?? string.Empty;
                                    var valid = parseResult.valid;
                                    var missing = parseResult.missing;
                                    var options = parseResult.options;

                                    outputs.Add(new Output { varname = "newAskContent", value = ask, nodeId = config.id, sourceId = $"{config.id}_newAskContent" });
                                    outputs.Add(new Output { varname = "parsedParams", value = JsonConvert.SerializeObject(options), nodeId = config.id, sourceId = $"{config.id}_parsedParams" });
                                    outputs.Add(new Output { varname = "missingParamIds", value = JsonConvert.SerializeObject(missing), nodeId = config.id, sourceId = $"{config.id}_missingParamIds" });
                                    outputs.Add(new Output { varname = "valid", value = (valid ? "true" : "false"), nodeId = config.id, sourceId = $"{config.id}_valid" });

                                    execNextNode = parseResult.valid;
                                }
                                else
                                {
                                    outputs.Add(new Output { varname = "newAskContent", value = nodeData.askContent, nodeId = config.id, sourceId = $"{config.id}_newAskContent" });
                                    outputs.Add(new Output { varname = "parsedParams", value = JsonConvert.SerializeObject(new List<InputOption>()), nodeId = config.id, sourceId = $"{config.id}_parsedParams" });
                                    outputs.Add(new Output { varname = "missingParamIds", value = JsonConvert.SerializeObject(nodeData.inputOptions.Where(o => o.isRequired).Select(o => o.id).ToList()), nodeId = config.id, sourceId = $"{config.id}_missingParamIds" });
                                    outputs.Add(new Output { varname = "valid", value = "false", nodeId = config.id, sourceId = $"{config.id}_valid" });
                                    execNextNode = false;
                                }

                                Logs.Add(llmRaw);
                            }
                            catch (Exception _ex)
                            {
                                Logs.Add(_ex.Message);
                                execNextNode = false;
                            }
                        }

                        if (execNextNode == false)
                        {
                            string reCallUrl = ConfigHelper.GetString("HumanOperationReCallUrl").Replace("{sessionID}", SessionID).Replace("{taskID}", TaskID).Replace("{appID}", AppID).Replace("{recordID}", RecordID);

                            //输出回调地址，前端人工处理完成后将结果post，调用该地址通知系统继续执行后续节点
                            outputs.Add(new Output { varname = "reCallUrl", value = reCallUrl, nodeId = config.id, sourceId = $"{config.id}_reCallUrl" });
                            outputs.Add(new Output { varname = "sessionID", value = SessionID, nodeId = config.id, sourceId = $"{config.id}_sessionID" });
                            outputs.Add(new Output { varname = "taskID", value = TaskID, nodeId = config.id, sourceId = $"{config.id}_taskID" });
                            outputs.Add(new Output { varname = "appID", value = AppID, nodeId = config.id, sourceId = $"{config.id}_appID" });
                            outputs.Add(new Output { varname = "recordID", value = RecordID, nodeId = config.id, sourceId = $"{config.id}_recordID" });
                            //设置当前节点执行状态为等待人工处理
                            ExecutionRecordStatus = ExecutionRecordStatus.Running;
                        }
                        else
                        {
                            ExecutionRecordStatus = ExecutionRecordStatus.Success;
                            WorkflowNodeInfoBussiness.NextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, config, inputs, outputs, Logs);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }
            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        public async Task<string> IntentionRecognitionNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();
            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    IntentionRecognitionData nodeData = JsonConvert.DeserializeObject<IntentionRecognitionData>(JsonConvert.SerializeObject(config.data));
                    if (nodeData != null)
                    {
                        Logs.Add($"意图识别。SessionID:{SessionID}");
                        string results = "";

                        if (nodeData.model?.LargeModelID > 0)
                        {
                            LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(nodeData.model.LargeModelID);
                            ChatHistory history = new ChatHistory();
                            var dataMsg = new StringBuilder();
                            List<AppChatSummaryInfo> appChatSummaries = AppChatSummaryInfoBussiness.GetListBySessionID(AppID, SessionID);

                            history = await _chatService.GetChatHistory(appChatSummaries, history);


                            nodeData.prompt =await this.ReplacePromptValue(nodeData.prompt, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);

                            history.AddSystemMessage(nodeData.prompt);
                            history.AddUserMessage("继续");

                            dataMsg.AppendLine("#角色:意图识别工程师");
                            dataMsg.AppendLine("#工作内容:根据系统提示词中用户的提问，以及意图指引列表，进行匹配判断，输出意图指引的ID，无需输出其他内容");

                            //意图指引列表
                            dataMsg.AppendLine("#意图指引列表:");
                            foreach (var intent in nodeData.intentions)
                            {
                                dataMsg.AppendLine($"意图ID:{intent.id}, 意图规则描述:{intent.reognitionRules}\n\r");
                            }

                            LargeModelConfig modelConfig = new LargeModelConfig();
                            modelConfig.Id = largeModel.LargeModelID.ToString();
                            modelConfig.Model = largeModel;
                            modelConfig.Temperature = nodeData.temperature;
                            modelConfig.TopPCoefficient = nodeData.topp;
                            modelConfig.Prompt = nodeData.prompt;

                            CallFunction callFunction = new CallFunction();
                            callFunction.Prompt = dataMsg.ToString();

                            var chatResult = _chatService.PromptFunctionCall(modelConfig, callFunction);
                            StringBuilder rawContent = new StringBuilder();
                            await foreach (var content in chatResult)
                            {
                                rawContent.Append(content.ConvertToString());
                            }
                            results = rawContent.ToString();

                            //根据输出结果，匹配意图指引列表，找出对应的意图ID
                            var matchedIntent = nodeData.intentions.FirstOrDefault(i => results.Contains(i.id));

                            //驱动选中的下一节点,找出对应的下一节点ID

                            List<WorkflowEdgeInfo> edgeList = WorkflowEdgeInfoBussiness.GetListBySourceNodeId(config.id);
                            if (edgeList != null && edgeList.Count > 0)
                            {
                                List<string> TargetNodeId = new();
                                if (matchedIntent != null)
                                {
                                    foreach (var edge in edgeList)
                                    {
                                        var cfg = edge.Config as JObject ?? JObject.FromObject(edge.Config);
                                        if ((string?)cfg["sourceHandle"] == matchedIntent.id)
                                            TargetNodeId.Add(edge.TargetNodeId);
                                    }
                                }

                                //option为null或没找到匹配项时，用默认分支
                                if (TargetNodeId.Count == 0)
                                {
                                    foreach (var edge in edgeList)
                                    {
                                        var cfg = edge.Config as JObject ?? JObject.FromObject(edge.Config);
                                        if ((string?)cfg["sourceHandle"] == "other_branch")
                                            TargetNodeId.Add(edge.TargetNodeId);
                                    }
                                }


                                if (TargetNodeId.Count > 0)
                                {
                                    List<WorkflowNodeInfo> targetNodeList = WorkflowNodeInfoBussiness.GetListByNodeID(string.Join(",", TargetNodeId.Select(id => $"'{id}'")));
                                    if (targetNodeList != null)
                                    {
                                        outputs.Add(new Output { varname = "results", value = JsonConvert.SerializeObject(matchedIntent), nodeId = config.id, sourceId = $"{config.id}_results" });

                                        foreach (var node in targetNodeList)
                                        {
                                            NodeConfig targetNode = new NodeConfig() { id = node.NodeID, mainid = config.mainid, workflowid = node.WorkflowID, type = node.NodeType, data = node.Config };

                                            string newTaskID = TaskInfoBussiness.toTask(config, outputs, targetNode, AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID);

                                            Logs.Add($"{newTaskID}");
                                        }
                                    }
                                }
                            }
                            ExecutionRecordStatus = ExecutionRecordStatus.Success;
                        }
                        else
                        {
                            ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                        }
                        Logs.Add(results);
                    }
                }

            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }
            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        public async Task<string> SkillAgentNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();
            string AppID = data.AppID, TaskID = data.TaskID, SessionID = data.SessionID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;
            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    SkillAgentData nodeData = JsonConvert.DeserializeObject<SkillAgentData>(JsonConvert.SerializeObject(config.data));
                    if (nodeData != null)
                    {
                        Logs.Add($"Skill技能。SessionID:{SessionID}");
                        string results = "";
                        nodeData.prompt = await this.ReplacePromptValue(nodeData.prompt, inputs, config.fromNodeId, SessionID, AppID, ProcessesID);
//调用AgentSkillService
                        // 提取附件与附加参数，参考 LargeModelNodeAsync
                        Inputs attachmentInput = inputs.FirstOrDefault(i => i.varname == "attachments");
                        Inputs additionalOptions = inputs.FirstOrDefault(i => i.varname == "additionalOptions");
                        if (attachmentInput != null)
                        {
                            try { data.AttachmentItems = JsonConvert.DeserializeObject<List<AttachmentItem>>(attachmentInput.value); } catch { data.AttachmentItems = null; }
                        }
                        if (additionalOptions != null)
                        {
                            try { data.AdditionalOptions = JsonConvert.DeserializeObject(additionalOptions.value); } catch { data.AdditionalOptions = null; }
                        }

                        // 构建 LargeModelConfig（用于 plan 模式），参考 LargeModelNodeAsync
                        LargeModelConfig modelConfig = null;
                        if (nodeData.model?.LargeModelID > 0)
                        {
                            LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(nodeData.model.LargeModelID);
                            modelConfig = new LargeModelConfig
                            {
                                Id = largeModel.LargeModelID.ToString(),
                                Model = largeModel,
                                SemanticFunction = null,
                                NativeFunction = null,
                                Temperature = nodeData.temperature,
                                TopPCoefficient = nodeData.topp,
                                ResponseFormat = nodeData.ResponseFormat,
                                Thinking = nodeData.Thinking
                            };
                        }

                        // 构建进度流
                        var streamKey = StreamKey.Build(SessionID, ProcessesID);
                        using var batchWriter = new StreamBatchWriter(
                            _streamSync, streamKey, SessionID, ProcessesID, TaskID, config.id, intervalMs: 200);
                        var progress = new Progress<string>(delta => {
                            batchWriter.Append(delta);
                        });

                        var svc = _provider.GetService<IAgentSkillService>();
                        if (svc == null)
                        {
                            Logs.Add("IAgentSkillService 未注册");
                            ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                        }
                        else
                        {
                            //填充SkillDirectory为完整的路径
                            var baseDir = ConfigHelper.GetString("Skill:Directory");
                            if (string.IsNullOrWhiteSpace(baseDir))
                            {
                                Logs.Add("未配置Skill:Directory");

                                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                            }
                            else
                            {
                                SkillInfo skillInfo = SkillInfoBussiness.GetModel(nodeData.skill.SkillID);
                                if (skillInfo == null)
                                {
                                    Logs.Add("Skill不存在");

                                    ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                                }
                                else
                                {
                                    nodeData.skill.Name = skillInfo?.SName ?? string.Empty;
                                    nodeData.skill.Description = skillInfo?.SDescription ?? string.Empty;
                                    nodeData.skill.SkillDirectory = skillInfo?.SkillDirectory ?? string.Empty;

                                    nodeData.skill.SkillDirectory = System.IO.Path.Combine(baseDir, nodeData.skill.SkillDirectory);

                                    var resp = await svc.ExecuteWithPlanTrackingAsync(
                                        skill: nodeData.skill,
                                        options: nodeData.skillsToolsOptions,
                                        prompt: nodeData.prompt,
                                        attachments: data.AttachmentItems,
                                        progress: progress,
                                        modelConfig: modelConfig,
                                        sessionId: SessionID,
                                        processesId: ProcessesID,
                                        ct: CancellationToken.None);

                                    // 写入输出：主结果与项集合
                                    if (!string.IsNullOrEmpty(resp?.Output))
                                    {
                                        outputs.Add(new Output { varname = "results", value = resp.Output, nodeId = config.id, sourceId = $"{config.id}_results" });
                                        results = resp.Output;
                                    }
                                    if (resp?.Outputs != null)
                                    {
                                        foreach (var o in resp.Outputs)
                                        {
                                            if (o == null) continue;
                                            o.nodeId = config.id;
                                            o.sourceId = $"{config.id}_{o.varname}";
                                            outputs.Add(o);
                                        }
                                    }
                                    if (resp?.Logs != null && resp.Logs.Count > 0)
                                    {
                                        Logs.AddRange(resp.Logs);
                                    }

                                    // ExecuteAsync 内部已自动执行可执行计划，无需节点层再次调用

                                    ExecutionRecordStatus = ExecutionRecordStatus.Success;
                                }
                            }
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                Logs.Add(ex.Message);
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
            }
            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }
        private static string RemoveZeroWidthAndControl(string s)
        {
            if (s == null) return null;
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch == '\u200B' || ch == '\u200C' || ch == '\u200D' || ch == '\uFEFF')
                    continue;
                if (char.IsControl(ch))
                    continue;
                sb.Append(ch);
            }
            return sb.ToString().Trim();
        }
        /// <summary>
        /// 从已加载的程序集中查找类型(带缓存,包含未找到标记)
        /// </summary>
        private static readonly System.Type _typeNotFoundMarker = typeof(void);
        private System.Type GetTypeFromLoadedAssemblies(string namespaceName, string className)
        {
            namespaceName = RemoveZeroWidthAndControl(namespaceName);
            className = RemoveZeroWidthAndControl(className);
            string fullTypeName = $"{namespaceName}.{className}";

            // 先检查缓存（包括"未找到"标记）
            lock (_typeCacheLock)
            {
                if (_typeCache.TryGetValue(fullTypeName, out System.Type cachedType))
                {
                    return cachedType == _typeNotFoundMarker ? null : cachedType;
                }
            }

            System.Type type = null;

            // 方法1:先尝试直接获取(处理当前程序集和mscorlib)
            type = System.Type.GetType(fullTypeName, throwOnError: false);

            // 方法2:遍历所有已加载的程序集
            if (type == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        type = assembly.GetType(fullTypeName, throwOnError: false);
                        if (type != null) break;
                    }
                    catch { continue; }
                }
            }

            // 方法2.1: 通用兜底 - 在已加载程序集内按 FullName 或 Namespace+Name 枚举匹配
            if (type == null)
            {
                type = FindTypeByEnumeration(fullTypeName, namespaceName, className);
            }

            // 方法3:扫描运行目录下未加载的程序集并尝试加载后查找
            if (type == null)
            {
                type = FindTypeInUnloadedAssemblies(fullTypeName, namespaceName, className);
            }

            // 缓存结果（包括未找到标记，避免重复扫描）
            lock (_typeCacheLock)
            {
                _typeCache[fullTypeName] = type ?? _typeNotFoundMarker;
            }

            if (type == null)
            {
                Console.WriteLine($"[GetTypeFromLoadedAssemblies] NOT FOUND: {fullTypeName}");
            }

            return type;
        }

        private System.Type FindTypeByEnumeration(string fullTypeName, string namespaceName, string className)
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    System.Type[] types = null;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException rtle) { types = rtle.Types?.Where(t => t != null).ToArray(); }
                    catch { continue; }
                    if (types == null) continue;

                    var match = types.FirstOrDefault(t => string.Equals(t.FullName, fullTypeName, StringComparison.Ordinal))
                                ?? types.FirstOrDefault(t => string.Equals(t.Namespace, namespaceName, StringComparison.Ordinal) && string.Equals(t.Name, className, StringComparison.Ordinal));
                    if (match != null) return match;
                }
            }
            catch { }
            return null;
        }

        private System.Type FindTypeInUnloadedAssemblies(string fullTypeName, string namespaceName, string className)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                foreach (var dllPath in System.IO.Directory.EnumerateFiles(baseDir, "*.dll", System.IO.SearchOption.AllDirectories))
                {
                    try
                    {
                        string asmName = System.Reflection.AssemblyName.GetAssemblyName(dllPath).Name;
                        bool alreadyLoaded = false;
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try
                            {
                                if (string.Equals(asm.GetName().Name, asmName, StringComparison.OrdinalIgnoreCase))
                                {
                                    alreadyLoaded = true;
                                    break;
                                }
                            }
                            catch { }
                        }

                        if (alreadyLoaded) continue;

                        var loadedAsm = System.Reflection.Assembly.LoadFrom(dllPath);
                        if (loadedAsm == null) continue;

                        var type = loadedAsm.GetType(fullTypeName, throwOnError: false);
                        if (type != null) return type;

                        // 通用兜底枚举匹配
                        System.Type[] types = null;
                        try { types = loadedAsm.GetTypes(); }
                        catch (ReflectionTypeLoadException rtle) { types = rtle.Types?.Where(t => t != null).ToArray(); }
                        catch { continue; }

                        var match = types?.FirstOrDefault(t => string.Equals(t.FullName, fullTypeName, StringComparison.Ordinal))
                                    ?? types?.FirstOrDefault(t => string.Equals(t.Namespace, namespaceName, StringComparison.Ordinal) && string.Equals(t.Name, className, StringComparison.Ordinal));
                        if (match != null) return match;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetTypeFromLoadedAssemblies] scan baseDir failed: {ex}");
            }
            return null;
        }

        /// <summary>
        /// 收集LLM流式响应并返回完整文本
        /// </summary>
        private static async Task<string> CollectStreamResultAsync(IAsyncEnumerable<string> chatResult)
        {
            StringBuilder rawContent = new StringBuilder();
            Chats info = null;

            await foreach (var content in chatResult)
            {
                if (info == null)
                {
                    rawContent.Append(content.ConvertToString());
                    info = new Chats();
                    info.Context = content.ConvertToString();
                }
                else
                {
                    rawContent.Append(content.ConvertToString());
                }
                info.Context = rawContent.ToString();
            }

            return info?.Context ?? "";
        }

        /// <summary>
        /// 将字符串值转换为指定类型
        /// </summary>
        private object ConvertToType(string value, string typeName)
        {
            if (string.IsNullOrEmpty(value) || value == "null")
            {
                return null;
            }

            try
            {
                switch (typeName.ToLower())
                {
                    case "string":
                        return value;

                    case "int":
                    case "int32":
                        return int.Parse(value, CultureInfo.InvariantCulture);

                    case "long":
                    case "int64":
                        return long.Parse(value, CultureInfo.InvariantCulture);

                    case "bool":
                    case "boolean":
                        return bool.Parse(value);

                    case "double":
                        return double.Parse(value, CultureInfo.InvariantCulture);

                    case "decimal":
                        return decimal.Parse(value, CultureInfo.InvariantCulture);

                    case "float":
                        return float.Parse(value, CultureInfo.InvariantCulture);

                    case "datetime":
                        return DateTime.Parse(value, CultureInfo.InvariantCulture);

                    case "object":
                    case "jobject":
                        // 尝试解析为 JSON 对象
                        return JsonConvert.DeserializeObject(value);

                    case "attachments":
                        // attachments 类型特殊处理,可能是数组或对象
                        if (value.StartsWith("[") || value.StartsWith("{"))
                        {
                            return JsonConvert.DeserializeObject(value);
                        }
                        return value;

                    default:
                        // 尝试通过类型名称进行转换
                        System.Type targetType = System.Type.GetType(typeName);
                        if (targetType != null)
                        {
                            return Convert.ChangeType(value, targetType);
                        }
                        return value;
                }
            }
            catch (Exception)
            {
                // 转换失败时返回原始字符串
                return value;
            }
        }

        /// <summary>
        /// 图像生成节点执行
        /// </summary>
        public async Task<string> ImageGenerationNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID;
            string TaskID = data.TaskID;
            string SessionID = data.SessionID;
            string ProcessesID = data.ProcessesID;
            string AgentNodeID = data.AgentNodeID;
            string FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;

            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    // 1. 解析节点配置
                    ImageGenerationData nodeData = JsonConvert.DeserializeObject<ImageGenerationData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        // 2. 替换提示词中的占位符
                        nodeData.prompt = await this.ReplacePromptValue(
                            nodeData.prompt,
                            inputs,
                            config.fromNodeId,
                            SessionID,
                            AppID,
                            ProcessesID
                        );

                        Logs.Add($"[图像生成] 提示词: {nodeData.prompt}");

                        // 3. 处理输入图像（如果有）
                        string? resolvedImageInput = null;
                        if (!string.IsNullOrEmpty(nodeData.imageInput))
                        {
                            resolvedImageInput = await this.ReplacePromptValue(
                                nodeData.imageInput,
                                inputs,
                                config.fromNodeId,
                                SessionID,
                                AppID,
                                ProcessesID
                            );

                            var previewLength = Math.Min(100, resolvedImageInput?.Length ?? 0);
                            Logs.Add($"[图像生成] 输入图像: {resolvedImageInput?.Substring(0, previewLength)}...");

                            resolvedImageInput = resolvedImageInput != null ? resolvedImageInput.Trim() : "";
                            if (resolvedImageInput.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            resolvedImageInput.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            {
                                //不做处理
                            }
                            else
                            {
                                AttachmentItem attachment = null;
                                try
                                {
                                    // 尝试解析为数组，如果失败（返回 null 或报错）则尝试解析为单个对象
                                    var list = JsonConvert.DeserializeObject<List<AttachmentItem>>(resolvedImageInput);
                                    attachment = list?.FirstOrDefault();
                                }
                                catch
                                {
                                    try
                                    {
                                        attachment = JsonConvert.DeserializeObject<AttachmentItem>(resolvedImageInput);
                                    }
                                    catch
                                    {
                                        Logs.Add("[图像生成] 解析失败");
                                    }

                                }
                                if (attachment?.FileURI != null)
                                {
                                    resolvedImageInput = attachment.FileURI;
                                }
                            }
                        }

                        string imageUrl = "";

                        // 4. 检查模型配置
                        if (nodeData.model?.LargeModelID > 0)
                        {
                            LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(nodeData.model.LargeModelID);

                            if (largeModel == null)
                            {
                                throw new Exception($"未找到模型配置，模型ID: {nodeData.model.LargeModelID}");
                            }

                            // 5. 验证模型类型
                            if (largeModel.TypeCode == AIModelType.T2Image || largeModel.TypeCode == AIModelType.I2Image)
                            {
                                // 6. 判断生成类型(根据是否有输入图片自动判断)
                                var generationType = string.IsNullOrEmpty(resolvedImageInput) 
                                    ? ImageGenerationType.TextToImage 
                                    : ImageGenerationType.ImageToImage;

                                Logs.Add($"[图像生成] 生成类型: {(generationType == ImageGenerationType.TextToImage ? "文生图" : "图生图")}");
                                Logs.Add($"[图像生成] 使用模型: {largeModel.ModelName} (服务商: {largeModel.ModelOrganizationID})");
                                Logs.Add($"[图像生成] 参数: {nodeData.width}x{nodeData.height}, 质量: {nodeData.quality}, 风格: {nodeData.style}");

                                // 7. 构建请求对象
                                var imageRequest = new ImageGenerationRequest
                                {
                                    GenerationType = generationType,
                                    Prompt = nodeData.prompt,
                                    ImageInput = resolvedImageInput,
                                    Width = nodeData.width,
                                    Height = nodeData.height,
                                    Quality = nodeData.quality,
                                    Style = nodeData.style
                                };

                                // 8. 调用图像生成服务（带重试，应对网络抖动/限流/模型临时不可用）
                                var imageService = _provider.GetRequiredService<IImageService>();
                                imageUrl = await RetryPolicy.ExecuteAsync(
                                    () => imageService.GenerateImageAsync(largeModel, imageRequest),
                                    maxRetries: 3,
                                    delayMs: 3000,
                                    onRetry: (ex, attempt) =>
                                    {
                                        Logs.Add($"[图像生成] 第 {attempt} 次重试（间隔3秒），上次错误: {ex.Message}");
                                        _logger.LogWarning(ex, $"[图像生成] 重试 {attempt}/3 - NodeID: {config.id}");
                                    }
                                );

                                Logs.Add($"[图像生成] 成功生成图像");
                                Logs.Add($"[图像生成] 图像URL: {imageUrl}");
                            }
                            else
                            {
                                throw new Exception($"模型类型错误，期望: T2Image，I2Image，实际: {largeModel.TypeCode}");
                            }
                        }
                        else
                        {
                            throw new Exception("未配置图像模型");
                        }

                        // 7. 构建输出
                        outputs.Add(new Output
                        {
                            varname = "imageUrl",
                            value = imageUrl,
                            nodeId = config.id,
                            sourceId = $"{config.id}_imageUrl",
                            type = "string",
                            displayText = "生成的图像URL"
                        });

                        outputs.Add(new Output
                        {
                            varname = "prompt",
                            value = nodeData.prompt,
                            nodeId = config.id,
                            sourceId = $"{config.id}_prompt",
                            type = "string",
                            displayText = "使用的提示词"
                        });

                        outputs.Add(new Output
                        {
                            varname = "width",
                            value = nodeData.width.ToString(),
                            nodeId = config.id,
                            sourceId = $"{config.id}_width",
                            type = "int",
                            displayText = "图像宽度"
                        });

                        outputs.Add(new Output
                        {
                            varname = "height",
                            value = nodeData.height.ToString(),
                            nodeId = config.id,
                            sourceId = $"{config.id}_height",
                            type = "int",
                            displayText = "图像高度"
                        });

                        // 8. 触发下一节点
                        WorkflowNodeInfoBussiness.NextNode(
                            AppID,
                            SessionID,
                            ProcessesID,
                            TaskID,
                            FromMainTaskID,
                            AgentNodeID,
                            config,
                            inputs,
                            outputs,
                            Logs
                        );
                    }
                }

                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.Add($"[图像生成] 失败: {ex.Message}");
                Logs.Add($"[图像生成] 堆栈跟踪: {ex.StackTrace}");
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                
                _logger.LogError(ex, $"[图像生成] SessionID: {SessionID}, NodeID: {config.id}");
            }

            // 9. 更新执行记录
            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        /// <summary>
        /// 视频生成节点执行
        /// </summary>
        public async Task<string> VideoGenerationNodeAsync(NodeConfig config, TaskData data)
        {
            string RecordID = "";
            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            ExecutionRecordStatus ExecutionRecordStatus = new ExecutionRecordStatus();

            string AppID = data.AppID;
            string TaskID = data.TaskID;
            string SessionID = data.SessionID;
            string ProcessesID = data.ProcessesID;
            string AgentNodeID = data.AgentNodeID;
            string FromMainTaskID = data.FromMainTaskID;
            List<Inputs> inputs = data.Inputs;

            RecordID = ZSN.AI.Node.Utils.Utils.newExcutionRecord(SessionID, config, ProcessesID, TaskID, FromMainTaskID: FromMainTaskID, inputs: inputs);

            try
            {
                if (!AppID.IsNullOrEmpty() && !SessionID.IsNullOrEmpty())
                {
                    // 1. 解析节点配置
                    VideoGenerationData nodeData = JsonConvert.DeserializeObject<VideoGenerationData>(config.data.ToString());
                    if (nodeData != null)
                    {
                        // 2. 替换提示词中的占位符
                        nodeData.prompt = await this.ReplacePromptValue(
                            nodeData.prompt,
                            inputs,
                            config.fromNodeId,
                            SessionID,
                            AppID,
                            ProcessesID
                        );

                        Logs.Add($"[视频生成] 提示词: {nodeData.prompt}");

                        // 3. 处理负面提示词（如果有）
                        if (!string.IsNullOrEmpty(nodeData.negativePrompt))
                        {
                            nodeData.negativePrompt = await this.ReplacePromptValue(
                                nodeData.negativePrompt,
                                inputs,
                                config.fromNodeId,
                                SessionID,
                                AppID,
                                ProcessesID
                            );
                            Logs.Add($"[视频生成] 负面提示词: {nodeData.negativePrompt}");
                        }

                        // 4. 处理输入图像（根据生成类型）
                        string? resolvedImageInput = null;
                        List<string>? resolvedReferenceImages = null;
                        string? resolvedFirstFrameUrl = null;
                        string? resolvedLastFrameUrl = null;

                        // 4.1 处理单图输入（ImageToVideo）
                        if (!string.IsNullOrEmpty(nodeData.imageInput))
                        {
                            resolvedImageInput = await this.ReplacePromptValue(
                                nodeData.imageInput,
                                inputs,
                                config.fromNodeId,
                                SessionID,
                                AppID,
                                ProcessesID
                            );

                            resolvedImageInput = await ProcessImageInput(resolvedImageInput, Logs, "[视频生成-单图输入]");
                        }

                        // 4.2 处理参考图列表（ReferenceToVideo）
                        if (nodeData.referenceImages != null && nodeData.referenceImages.Count > 0)
                        {
                            resolvedReferenceImages = new List<string>();
                            foreach (var refImage in nodeData.referenceImages)
                            {
                                if (!string.IsNullOrEmpty(refImage))
                                {
                                    var resolved = await this.ReplacePromptValue(
                                        refImage,
                                        inputs,
                                        config.fromNodeId,
                                        SessionID,
                                        AppID,
                                        ProcessesID
                                    );
                                    var processedImage = await ProcessImageInput(resolved, Logs, "[视频生成-参考图]");
                                    if (!string.IsNullOrEmpty(processedImage))
                                    {
                                        resolvedReferenceImages.Add(processedImage);
                                    }
                                }
                            }
                            Logs.Add($"[视频生成] 参考图数量: {resolvedReferenceImages.Count}");
                        }

                        // 4.3 处理首尾帧（StartEndToVideo）
                        if (!string.IsNullOrEmpty(nodeData.firstFrameUrl))
                        {
                            resolvedFirstFrameUrl = await this.ReplacePromptValue(
                                nodeData.firstFrameUrl,
                                inputs,
                                config.fromNodeId,
                                SessionID,
                                AppID,
                                ProcessesID
                            );
                            resolvedFirstFrameUrl = await ProcessImageInput(resolvedFirstFrameUrl, Logs, "[视频生成-首帧]");
                        }

                        if (!string.IsNullOrEmpty(nodeData.lastFrameUrl))
                        {
                            resolvedLastFrameUrl = await this.ReplacePromptValue(
                                nodeData.lastFrameUrl,
                                inputs,
                                config.fromNodeId,
                                SessionID,
                                AppID,
                                ProcessesID
                            );
                            resolvedLastFrameUrl = await ProcessImageInput(resolvedLastFrameUrl, Logs, "[视频生成-尾帧]");
                        }

                        string videoUrl = "";
                        string taskId = "";

                        // 5. 检查模型配置
                        if (nodeData.model?.LargeModelID > 0)
                        {
                            LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(nodeData.model.LargeModelID);

                            if (largeModel == null)
                            {
                                throw new Exception($"未找到模型配置，模型ID: {nodeData.model.LargeModelID}");
                            }

                            // 6. 验证模型类型
                            if (largeModel.TypeCode == AIModelType.T2Video || largeModel.TypeCode == AIModelType.I2Video)
                            {
                                // 7. 判断生成类型
                                VideoGenerationType generationType = (VideoGenerationType)nodeData.generationType;
                                
                                string generationTypeText = generationType switch
                                {
                                    VideoGenerationType.TextToVideo => "文生视频",
                                    VideoGenerationType.ImageToVideo => "图生视频",
                                    VideoGenerationType.ReferenceToVideo => "参考图生成视频",
                                    VideoGenerationType.StartEndToVideo => "首尾帧生成视频",
                                    _ => "未知类型"
                                };

                                Logs.Add($"[视频生成] 生成类型: {generationTypeText}");
                                Logs.Add($"[视频生成] 使用模型: {largeModel.ModelName} (服务商: {largeModel.ModelOrganizationID})");
                                Logs.Add($"[视频生成] 参数: 时长={nodeData.duration}秒, 尺寸={nodeData.size}, 分辨率={nodeData.resolution}");

                                // 8. 构建请求对象
                                var videoRequest = new VideoGenerationRequest
                                {
                                    GenerationType = generationType,
                                    Prompt = nodeData.prompt,
                                    NegativePrompt = nodeData.negativePrompt,
                                    ImageInput = resolvedImageInput,
                                    ReferenceImages = resolvedReferenceImages,
                                    FirstFrameUrl = resolvedFirstFrameUrl,
                                    LastFrameUrl = resolvedLastFrameUrl,
                                    Duration = nodeData.duration,
                                    Size = nodeData.size,
                                    AspectRatio = nodeData.aspectRatio,
                                    Resolution = nodeData.resolution,
                                    Seed = nodeData.seed > 0 ? nodeData.seed : null
                                };

                                // 9. 调用视频生成服务
                                var videoService = _provider.GetRequiredService<IVideoService>();
                                
                                Logs.Add($"[视频生成] 开始提交视频生成任务...");
                                
                                // 提交任务（带重试，应对网络抖动/限流/服务临时不可用）
                                var submitResponse = await RetryPolicy.ExecuteAsync(
                                    () => videoService.SubmitVideoTaskAsync(largeModel, videoRequest),
                                    maxRetries: 3,
                                    delayMs: 3000,
                                    onRetry: (ex, attempt) =>
                                    {
                                        Logs.Add($"[视频生成] 提交任务第 {attempt} 次重试（间隔3秒），错误: {ex.Message}");
                                        _logger.LogWarning(ex, $"[视频生成] 提交重试 {attempt}/3 - NodeID: {config.id}");
                                    }
                                );
                                taskId = submitResponse.TaskId;
                                
                                Logs.Add($"[视频生成] 任务提交成功, 任务ID: {taskId}");
                                Logs.Add($"[视频生成] 任务状态: {submitResponse.TaskStatus}");

                                // 如果任务已完成，直接获取视频URL
                                if (submitResponse.TaskStatus == VideoTaskStatus.Success && submitResponse.VideoUrls?.Count > 0)
                                {
                                    videoUrl = submitResponse.VideoUrls[0];
                                    Logs.Add($"[视频生成] 视频生成成功");
                                    Logs.Add($"[视频生成] 视频URL: {videoUrl}");
                                }
                                else
                                {
                                    // 否则等待任务完成（最多等待600秒），带重试应对临时失败/超时
                                    Logs.Add($"[视频生成] 等待视频生成完成...");
                                    var finalResponse = await RetryPolicy.ExecuteAsync(
                                        () => videoService.GenerateVideoAsync(largeModel, videoRequest, maxWaitSeconds: 600),
                                        maxRetries: 3,
                                        delayMs: 3000,
                                        onRetry: (ex, attempt) =>
                                        {
                                            Logs.Add($"[视频生成] 等待完成第 {attempt} 次重试（间隔3秒），错误: {ex.Message}");
                                            _logger.LogWarning(ex, $"[视频生成] 等待重试 {attempt}/3 - NodeID: {config.id}");
                                        }
                                    );
                                    
                                    if (finalResponse.TaskStatus == VideoTaskStatus.Success && finalResponse.VideoUrls?.Count > 0)
                                    {
                                        videoUrl = finalResponse.VideoUrls[0];
                                        Logs.Add($"[视频生成] 视频生成成功");
                                        Logs.Add($"[视频生成] 视频URL: {videoUrl}");
                                    }
                                    else
                                    {
                                        throw new Exception($"视频生成失败或超时，任务状态: {finalResponse.TaskStatus}, 错误信息: {finalResponse.ErrorMessage}");
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception($"模型类型错误，期望: T2Video, I2Video，实际: {largeModel.TypeCode}");
                            }
                        }
                        else
                        {
                            throw new Exception("未配置视频模型");
                        }

                        // 10. 构建输出
                        outputs.Add(new Output
                        {
                            varname = "videoUrl",
                            value = videoUrl,
                            nodeId = config.id,
                            sourceId = $"{config.id}_videoUrl",
                            type = "string",
                            displayText = "生成的视频URL"
                        });

                        outputs.Add(new Output
                        {
                            varname = "taskId",
                            value = taskId,
                            nodeId = config.id,
                            sourceId = $"{config.id}_taskId",
                            type = "string",
                            displayText = "任务ID"
                        });

                        outputs.Add(new Output
                        {
                            varname = "prompt",
                            value = nodeData.prompt,
                            nodeId = config.id,
                            sourceId = $"{config.id}_prompt",
                            type = "string",
                            displayText = "使用的提示词"
                        });

                        outputs.Add(new Output
                        {
                            varname = "duration",
                            value = nodeData.duration.ToString(),
                            nodeId = config.id,
                            sourceId = $"{config.id}_duration",
                            type = "int",
                            displayText = "视频时长（秒）"
                        });

                        outputs.Add(new Output
                        {
                            varname = "resolution",
                            value = nodeData.resolution,
                            nodeId = config.id,
                            sourceId = $"{config.id}_resolution",
                            type = "string",
                            displayText = "视频分辨率"
                        });

                        // 11. 触发下一节点
                        WorkflowNodeInfoBussiness.NextNode(
                            AppID,
                            SessionID,
                            ProcessesID,
                            TaskID,
                            FromMainTaskID,
                            AgentNodeID,
                            config,
                            inputs,
                            outputs,
                            Logs
                        );
                    }
                }

                ExecutionRecordStatus = ExecutionRecordStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.Add($"[视频生成] 失败: {ex.Message}");
                Logs.Add($"[视频生成] 堆栈跟踪: {ex.StackTrace}");
                ExecutionRecordStatus = ExecutionRecordStatus.Fail;
                
                _logger.LogError(ex, $"[视频生成] SessionID: {SessionID}, NodeID: {config.id}");
            }

            // 12. 更新执行记录
            ZSN.AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus, outputs, Logs);
            return RecordID;
        }

        /// <summary>
        /// 处理图像输入（URL或AttachmentItem）
        /// </summary>
        private async Task<string> ProcessImageInput(string input, List<string> logs, string logPrefix)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = input.Trim();

            // 如果是HTTP URL，直接返回
            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }

            // 如果是Base64 Data URI，直接返回
            if (input.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }

            // 尝试解析为AttachmentItem
            AttachmentItem attachment = null;
            try
            {
                // 尝试解析为数组
                var list = JsonConvert.DeserializeObject<List<AttachmentItem>>(input);
                attachment = list?.FirstOrDefault();
            }
            catch
            {
                try
                {
                    // 尝试解析为单个对象
                    attachment = JsonConvert.DeserializeObject<AttachmentItem>(input);
                }
                catch
                {
                    logs.Add($"{logPrefix} 解析失败，将作为原始字符串使用");
                }
            }

            if (attachment?.FileURI != null)
            {
                logs.Add($"{logPrefix} 从附件中提取URL: {attachment.FileURI}");
                return attachment.FileURI;
            }

            return input;
        }

        /// <summary>
        /// Claw AI 节点执行方法
        /// </summary>
        public async Task<string> ClawAINodeAsync(NodeConfig config, TaskData data)
        {
            // 从依赖注入容器获取 ExcutionClaw 实例
            var excutionClaw = _provider.GetService<ExecutionClaw>();
            
            if (excutionClaw == null)
            {
                _logger.LogError("[ClawAI] ExcutionClaw 服务未注册");
                throw new Exception("ExcutionClaw 服务未注册,请在 Startup 中配置依赖注入");
            }

            // 调用 ExcutionClaw 的执行方法
            return await excutionClaw.ClawAINodeAsync(config, data);
        }
    }
}
