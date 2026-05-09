using Elastic.Clients.Elasticsearch;
using Google.Protobuf.WellKnownTypes;
using JiebaNet.Segmenter.Common;
using log4net.Core;
using Lucene.Net.Util.Fst;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Client;
using MongoDB.Driver;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using ZSN.AgentBrook.API.Attributes;
using ZSN.AI.Service.Helpers;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Node;
using ZSN.AI.Service.Attributes;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using ErrorCode = ZSN.AI.Entity.ErrorCode;

namespace ZSN.AgentBrook.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-Member")]
    [Route("api/[controller]/[action]")]
    public class ChatController: ApiBaseController
    {
        private readonly IChatService _chatService;
        private readonly TaskManager _taskManager;
        private readonly IServiceProvider _provider;
        public ChatController(IChatService chatService, TaskManager taskManager, IServiceProvider provider)
        {
            _chatService = chatService;
            _taskManager = taskManager;
            _provider = provider;
        }

        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }

        
        /// <summary>
        /// 获取会话内容
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public JsonMsg<List<AppChatLogInfo>> GetList([FromBody] PostData paramValue) 
        {

            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string SessionID = jObject.JsonGetValue<string>("sessionID", "");
                string MemberID = memberSetting.FullMember.Member.MemberID;
                string _sql = $"";

                List<AppChatLogInfo> _list = AppChatLogInfoBussiness.GetList(_sql,SessionID, MemberID);


                return JsonMsg<List<AppChatLogInfo>>.OK(_list);
            }
            else
            {
                return JsonMsg<List<AppChatLogInfo>>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 获取会话摘要列表
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public JsonMsg<List<AppChatSummaryInfo>> GetSummaryList([FromBody] PostData paramValue)
        {

            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string SessionID = jObject.JsonGetValue<string>("sessionID", "");
                string MemberID = memberSetting.FullMember.Member.MemberID;
                string _sql = $"";

                List<AppChatSummaryInfo> _list = AppChatSummaryInfoBussiness.GetList(_sql, SessionID, MemberID);

                return JsonMsg<List<AppChatSummaryInfo>>.OK(_list);
            }
            else
            {
                return JsonMsg<List<AppChatSummaryInfo>>.Error(null, ErrorCode.DataFormatError);
            }
        }


        /// <summary>
        /// 生成对话completions
        /// </summary>
        /// <param name="paramValue"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public async Task<IActionResult> completions([FromBody] PostData paramValue, CancellationToken cancellationToken)
        {
            List<MessageData> messageDataList = new List<MessageData>();

            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                bool stream = jObject.JsonGetValue<bool>("stream", true);
                GptMsg Inputs = jObject.JsonGetValue<GptMsg>("messages");
                string SessionID = jObject.JsonGetValue<string>("sessionID", "");
                string MemberID = memberSetting.FullMember.Member.MemberID;
                string AppID = jObject.JsonGetValue<string>("appid", "");

                int SSE_TimeOut = jObject.JsonGetValue<int>("SSE_TimeOut", ConfigHelper.GetInt("SSE_TimeOut"));


                if (AppID.IsNullOrEmpty())
                {
                    messageDataList.Add(new MessageData() { Content = "AppID IsNullOrEmpty", Role = "system" });
                    return BadRequest(JsonMsg<IReadOnlyList<MessageData>>.Error(messageDataList, ErrorCode.DataFormatError));
                }
                else
                {
                    string ProcessesID = Guid.NewGuid().ToString();
                    string ChannelCode = Guid.NewGuid().ToString();

                    AppChatSessionInfo appChatSession = new AppChatSessionInfo();
                    MessageData messageData = new MessageData();
                    messageData.AppID = AppID;
                    messageData.ProcessesID = ProcessesID;

                    WorkflowNodeInfo nodeInfo = WorkflowNodeInfoBussiness.GetAppStartNode(AppID);
                    if (nodeInfo != null)
                    {
                        if (nodeInfo.Config != null)
                        {
                            NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(nodeInfo.Config.ToString());
                            if (nodeConfig != null)
                            {
                                if (nodeConfig.data != null)
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
                                    }

                                    messageData.SessionID = SessionID;

                                    TaskData data = new TaskData() { AppID = AppID, SessionID = SessionID, ProcessesID = messageData.ProcessesID, AgentNodeID = "" };
                                    data.Inputs = new List<Inputs>();
                                    data.Inputs.Add(new Inputs() { value = Inputs.content, varname = "input" });

                                    //处理附件信息加入附件的URI
                                    string previewHost = ConfigHelper.GetString("previewHost");
                                    if (Inputs.Attachments != null)
                                    {
                                        foreach (var item in Inputs.Attachments)
                                        {
                                            item.FileURI = string.Format(previewHost, item.FileCode);
                                        }

                                        data.AttachmentItems = Inputs.Attachments;
                                    }
                                    data.AdditionalOptions = Inputs.AdditionalOptions;

                                    AppChatLogInfoBussiness.Add(AppID, SessionID, AuthorRole.User.ToString(), Inputs);

                                    //查找SessionID下是否有未完成的HumanInTheLoop任务
                                    bool hasRunningTask = false;

                                    List<WorkflowNodeExecutionRecordInfo> _HumanTasksRecords = WorkflowNodeExecutionRecordInfoBussiness.GetList($" SessionID='{SessionID}' and NodeName in('{NodeType.HumanInTheLoop.ToString()}','{NodeType.HumanInTheLoopInput.ToString()}') and Status = {(int)ExecutionRecordStatus.Running} ");

                                    hasRunningTask = _HumanTasksRecords?.Count > 0 ? true : false;

                                    if (hasRunningTask == false)
                                    {
                                        TaskInfo taskInfo = new TaskInfo();
                                        taskInfo.SessionID = SessionID;
                                        taskInfo.TaskID = Guid.NewGuid().ToString();
                                        taskInfo.TaskType = nodeConfig.type;
                                        taskInfo.TaskConfig = new TaskConfig();
                                        taskInfo.TaskConfig.NodeConfig = nodeConfig;
                                        taskInfo.TaskConfig.Data = data;
                                        taskInfo.LoopType = LoopType.NOLoop;
                                        taskInfo.RepeatValue = 1;
                                        taskInfo.RedoCount = 0;
                                        taskInfo.CreateTime = DateTime.Now;
                                        taskInfo.UpdateTime = DateTime.Now;
                                        taskInfo.FromTaskID = "";
                                        taskInfo.FromMainTaskID = "";

                                        TaskInfoBussiness.Add(taskInfo);
                                    }
                                    else
                                    {
                                        //直接将用户输入数据发送给等待处理的HumanInTheLoop任务
                                        ErrorCode errorCode = ErrorCode.None;
                                        TaskController _taskController = new TaskController();
                                        _taskController.execHumanInTheLoopByUserInput(_HumanTasksRecords, Inputs, data, SessionID, out errorCode);
                                    }

                                    messageData.Content = messageData.ProcessesID;

                                    _ = Task.Run(() => _taskManager.RunProcessAsync(SessionID, ProcessesID, TimeSpan.FromMinutes(SSE_TimeOut), channelCode: ChannelCode));
                                }
                            }
                        }
                        messageDataList.Add(messageData);
                    }
                    var channel = _taskManager.GetChannel(SessionID, ProcessesID, ChannelCode);
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(SSE_TimeOut));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                    var ct = linkedCts.Token;
                    var _jsonSb = new StringBuilder();
                    if (stream)
                    {
                        Response.Headers.Append("Content-Type", "text/event-stream");
                        Response.Headers.Append("Cache-Control", "no-cache");
                        Response.Headers.Append("Connection", "keep-alive");

                        try
                        {
                            await foreach (var message in channel.Reader.ReadAllAsync(ct))
                            {
                                if (ct.IsCancellationRequested)
                                {
                                    break;
                                }
                                var json = JsonConvert.SerializeObject(message);
                                _jsonSb.Append(json);
                                var sseLine = $"data: {json}\n\n";
                                var bytes = Encoding.UTF8.GetBytes(sseLine);
                                await Response.Body.WriteAsync(bytes, ct);
                                await Response.Body.FlushAsync(ct);
                            }

                            return new EmptyResult();
                        }
                        catch (OperationCanceledException)
                        {
                            return new EmptyResult();
                        }
                    }
                    else
                    {
                        try
                        {
                            await foreach (var message in channel.Reader.ReadAllAsync(ct))
                            {
                                if (ct.IsCancellationRequested)
                                {
                                    break;
                                }
                                _jsonSb.Append(JsonConvert.SerializeObject(message));
                            }

                            messageDataList.Add(new MessageData() { AppID = AppID, SessionID = SessionID, ProcessesID = ProcessesID, Content = _jsonSb.ToString(), Role = "assistant" });
                            return Ok(JsonMsg<IReadOnlyList<MessageData>>.OK(messageDataList, SessionID: SessionID));
                        }
                        catch (OperationCanceledException)
                        {
                            messageDataList.Add(new MessageData() { AppID = AppID, SessionID = SessionID, ProcessesID = ProcessesID, Content = _jsonSb.ToString(), Role = "assistant" });
                            return Ok(JsonMsg<IReadOnlyList<MessageData>>.OK(messageDataList, SessionID: SessionID));
                        }
                    }
                }
            }
            else
            {
                messageDataList.Add(new MessageData() {  Content = JsonConvert.SerializeObject( jObject), Role = "system" });
                return BadRequest(JsonMsg<IReadOnlyList<MessageData>>.Error(messageDataList, ErrorCode.DataFormatError));
            }
        }

        /// <summary>
        /// 单节点执行
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public async Task<JsonMsg<MessageData>> ExecuteNode([FromBody] PostData paramValue)
        {
            List<MessageData> messageDataList = new List<MessageData>();
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string nodeId = jObject.JsonGetValue<string>("NodeID");
                List<Inputs> inputs = jObject.JsonGetValue<List<Inputs>>("Inputs");

                string MemberID = memberSetting.FullMember.Member.MemberID;

                string SessionID = jObject.JsonGetValue<string>("SessionID", "");
                string ProcessesID = jObject.JsonGetValue<string>("ProcessesID", Guid.NewGuid().ToString());
                string FromTaskID = jObject.JsonGetValue<string>("FromTaskID", "");
                string FromMainTaskID = jObject.JsonGetValue<string>("FromMainTaskID", "");
                string AgentNodeID = jObject.JsonGetValue<string>("AgentNodeID", "");
                string WorkflowID = jObject.JsonGetValue<string>("WorkflowID", "");

                //AppInfo的ID
                string AppID = jObject.JsonGetValue<string>("AppID", "");

                string _json = "";


                List<string> Outputs = new List<string>();
                List<string> Logs = new List<string>();

                MessageData message = Execution.ExecutionNode(AppID, SessionID, ProcessesID, nodeId, MemberID, "调试", inputs, FromTaskID, FromMainTaskID, AgentNodeID, WorkflowID);

                return JsonMsg<MessageData>.OK(message, SessionID: SessionID);
            }
            else
            {
                return JsonMsg<MessageData>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 重新执行指定节点
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>

        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public async Task<JsonMsg<MessageData>> ReExecuteNode([FromBody] PostData paramValue)
        {
            List<MessageData> messageDataList = new List<MessageData>();
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string nodeId = jObject.JsonGetValue<string>("NodeID");

                string MemberID = memberSetting.FullMember.Member.MemberID;

                string SessionID = jObject.JsonGetValue<string>("SessionID", "");
                string ProcessesID = jObject.JsonGetValue<string>("ProcessesID", "");
                string TaskID = jObject.JsonGetValue<string>("TaskID", "");

                //AppInfo的ID
                string AppID = jObject.JsonGetValue<string>("AppID", "");

                TaskInfo taskInfo = TaskInfoBussiness.GetModel(TaskID);
                if (taskInfo == null)
                {
                    return JsonMsg<MessageData>.Error(null, ErrorCode.TaskNotExists);
                }
                else
                {
                    taskInfo.State = TaskState.Waiting;

                    TaskInfoBussiness.Update(taskInfo);


                    //删除该节点及其下级节点的运行记录
                    List<WorkflowNodeInfo> nodeInfos = WorkflowNodeInfoBussiness.GetAllNextNodeListByNodeID(nodeId);
                    if (nodeInfos?.Count > 0)
                    {
                        foreach (var node in nodeInfos)
                        {
                            WorkflowNodeExecutionRecordInfoBussiness.DeleteByNodeID(SessionID, node.NodeID);
                        }
                    }


                    MessageData message = new MessageData();
                    message.AppID = AppID;
                    message.SessionID = SessionID;
                    message.ProcessesID = ProcessesID;
                    message.Content = "节点已重新加入执行队列";
                    message.Role = "system";
                    message.TaskID = TaskID;

                    return JsonMsg<MessageData>.OK(message, SessionID: SessionID);
                }

            }
            else
            {
                return JsonMsg<MessageData>.Error(null, ErrorCode.DataFormatError);
            }
        }


        /// <summary>
        /// 获取Node执行结果
        /// </summary>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public async Task GetNodeExecutionRecord([FromBody] PostData paramValue, CancellationToken cancellationToken)
        {
            try
            {
                List<MessageData> messageDataList = new List<MessageData>();
                JObject jObject = this.JsonObj;

                // 先检查状态，如果状态错误，立即返回错误响应
                if (jObject.JsonGetValue<int>("status") == -1)
                {
                    // 设置错误响应
                    Response.StatusCode = 400;
                    Response.ContentType = "application/json";
                    var errorResult = JsonMsg<IReadOnlyList<MessageData>>.Error(null, ErrorCode.DataFormatError);
                    await Response.WriteAsync(JsonConvert.SerializeObject(errorResult), cancellationToken);
                    return;
                }

                // 从请求中获取参数
                //AppInfo的ID
                string AppID = jObject.JsonGetValue<string>("AppID", "");
                string MemberID = memberSetting.FullMember.Member.MemberID;
                bool stream = jObject.JsonGetValue<bool>("stream", true);
                string SessionID = jObject.JsonGetValue<string>("sessionID", "");
                string ProcessesID = jObject.JsonGetValue<string>("processesID", "");
                string workflowID = jObject.JsonGetValue<string>("workflowID", "");
                int SSE_TimeOut = jObject.JsonGetValue<int>("SSE_TimeOut", ConfigHelper.GetInt("SSE_TimeOut"));
                bool isAgentNode = jObject.JsonGetValue<bool>("isAgentNode", false);
                    
                // 链接取消令牌（请求取消 + 超时）
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(SSE_TimeOut));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                var ct = linkedCts.Token;

                // 处理工作流ID
                if (!workflowID.IsNullOrEmpty())
                {
                    WorkflowInfo workflowInfo = WorkflowInfoBussiness.GetModel(workflowID);
                    if (workflowInfo != null)
                    {
                        isAgentNode = workflowInfo.MainType == MainType.Agent ? true : false;
                    }
                }
                string ChannelCode = Guid.NewGuid().ToString();
                // 异步运行处理任务
                _ = Task.Run(() => _taskManager.RunProcessAsync(SessionID, ProcessesID, TimeSpan.FromMinutes(SSE_TimeOut), isAgentNode, ChannelCode));

                // 获取通道
                var channel = _taskManager.GetChannel(SessionID, ProcessesID, ChannelCode);


                // 根据流式请求类型处理
                if (stream)
                {
                    // 流式响应 - 设置SSE头部
                    Response.StatusCode = 200; // 明确设置状态码
                    Response.Headers.Append("Content-Type", "text/event-stream");
                    Response.Headers.Append("Cache-Control", "no-cache");
                    Response.Headers.Append("Connection", "keep-alive");
                    try
                    {
                        // 流式发送数据
                        await foreach (var message in channel.Reader.ReadAllAsync(ct))
                        {
                            if (ct.IsCancellationRequested)
                            {
                                break;
                            }
                            var json = JsonConvert.SerializeObject(message);

                            var sseLine = $"data: {json}\n\n";
                            var bytes = Encoding.UTF8.GetBytes(sseLine);
                            await Response.Body.WriteAsync(bytes, ct);
                            await Response.Body.FlushAsync(ct);
                            //await System.Threading.Tasks.Task.Delay(500);
                        }

                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        // 客户端断开/超时：属正常情况，静默结束
                        return;
                    }
                }
                else
                {
                    try
                    {
                        var _json = new StringBuilder();
                        // 非流式响应 - 收集所有数据
                        await foreach (var message in channel.Reader.ReadAllAsync(ct))
                        {
                            if (ct.IsCancellationRequested)
                            {
                                break;
                            }
                            _json.Append(JsonConvert.SerializeObject(message));
                        }

                        // 准备标准JSON响应
                        messageDataList.Add(new MessageData() { AppID = AppID, SessionID = SessionID, ProcessesID = ProcessesID, Content = _json.ToString(), Role = "assistant" });
                        var result = JsonMsg<IReadOnlyList<MessageData>>.OK(messageDataList, SessionID: SessionID);

                        if (!Response.HasStarted)
                        {
                            // 设置JSON响应头
                            Response.StatusCode = 200;
                            Response.ContentType = "application/json";
                            await Response.WriteAsync(JsonConvert.SerializeObject(result), ct);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消：直接返回
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                // 异常处理 - 只在响应尚未开始时设置响应头
                if (!Response.HasStarted)
                {
                    Response.StatusCode = 500;
                    Response.ContentType = "application/json";
                    var errorResult = new { error = ex.Message, detail = ex.ToString() };
                    await Response.WriteAsync(JsonConvert.SerializeObject(errorResult), cancellationToken);
                }

                // 记录错误到控制台/日志
                Console.Error.WriteLine($"Error in GetNodeExcutionRecord: {ex}");
            }
        }

        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public async Task<JsonMsg<List<object>>> GetMCPTools([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                MCPConfig mcpConfig = jObject.JsonGetValue<MCPConfig>("MCPConfig");
                if (mcpConfig != null)
                {
                    var logger = _provider.GetRequiredService<ILogger<Execution>>();
                    Execution excution = new Execution(_chatService, _provider, logger);

                    IList<McpClientTool> mcpClientTools = await excution.GetMcpClientToolsAsync(mcpConfig);
                    List<McpClientTool> _list = mcpClientTools?.ToList();
                    
                    // 创建简化版本的McpClientTool列表，只保留基础信息
                    var simplifiedTools = new List<object>();
                    if (_list != null)
                    {
                        foreach(var tool in _list)
                        {
                            // 创建简化的工具对象，只包含基本属性
                            // 处理JsonElement类型的Schema
                            string inputSchema = null;
                            string outputSchema = null;
                            
                            try
                            {
                                // 将JsonElement转为JSON字符串
                                if (tool.JsonSchema.IsNotNull())
                                {
                                    inputSchema = JsonConvert.SerializeObject(tool.JsonSchema);
                                }
                                
                                if (tool.ReturnJsonSchema.IsNotNull())
                                {
                                    outputSchema = JsonConvert.SerializeObject(tool.ReturnJsonSchema);
                                }
                            }
                            catch (Exception ex)
                            {
                                // 异常处理，记录错误并继续
                                inputSchema = "{\"解析失败\": \"无法解析输入Schema\"}";
                                outputSchema = "{\"解析失败\": \"无法解析输出Schema\"}";
                            }
                            
                            // 深度解析并提取工具属性
                            var toolDetails = new Dictionary<string, object>();
                            
                            // 基本属性
                            toolDetails["Name"] = tool.Name;
                            toolDetails["Title"] = tool.Title;
                            toolDetails["Description"] = tool.Description;
                            
                            
                            // 其他属性
                            try {
                                var allProps = tool.GetType().GetProperties();
                                foreach (var prop in allProps) {
                                    if (!toolDetails.ContainsKey(prop.Name)) {
                                        var value = prop.GetValue(tool);
                                        if (value != null) {
                                            // 尝试将复杂对象序列化为字符串
                                            try {
                                                toolDetails[prop.Name] = JsonConvert.SerializeObject(value);
                                            } catch {
                                                toolDetails[prop.Name] = value.ToString();
                                            }
                                        }
                                    }
                                }
                            } catch (Exception ex) {
                                toolDetails["PropertyError"] = ex.Message;
                            }
                            
                            // 序列化全部属性
                            var simplifiedTool = toolDetails;
                            simplifiedTools.Add(simplifiedTool);
                        }
                    }
                    
                    // 返回简化版本的工具列表
                    return JsonMsg<List<object>>.OK(simplifiedTools);
                }
                else
                {
                    return JsonMsg<List<object>>.Error(null, ErrorCode.InvalidParameter);
                }
            }
            else
            {
                return JsonMsg<List<object>>.Error(null, ErrorCode.DataFormatError);
            }
        }

        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public JsonMsg<string> execHumanInTheLoop([FromQuery] string sessionID, [FromQuery] string taskID, [FromQuery] string recordID)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                ErrorCode errorCode = ErrorCode.None;
                var _option = System.Text.Json.JsonSerializer.Deserialize<AI.Entity.Option>(jObject.ToString());
                
                TaskController  _taskController = new TaskController();
                if (_taskController.execHumanInTheLoop(sessionID, taskID, recordID, _option, out errorCode))
                {
                    return JsonMsg<string>.OK(null, SessionID:sessionID);
                }
                else
                {
                    return JsonMsg<string>.Error(null, errorCode);
                }
            }
            else
            {
                return JsonMsg<string>.Error(null, ErrorCode.DataFormatError);
            }
        }

        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public JsonMsg<string> execHumanInTheLoopByForm([FromQuery] string sessionID, [FromQuery] string taskID, [FromQuery] string recordID)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                ErrorCode errorCode = ErrorCode.None;
                var inputOptionsToken = jObject["inputOptions"];
                List<AI.Entity.InputOption> options = null;

                if (inputOptionsToken != null && inputOptionsToken.Type != JTokenType.Null)
                {
                    string _optionsJsonStr = inputOptionsToken.ToString();
                    if (ZSN.Utils.Core.Utils.Utils.TryExtractStrictJson(_optionsJsonStr, out var __cleaned))
                    {
                        _optionsJsonStr = __cleaned;
                    }
                    // 用 System.Text.Json 反序列化为 List<InputOption>
                    options = System.Text.Json.JsonSerializer.Deserialize<List<AI.Entity.InputOption>>(
                        _optionsJsonStr
                    );
                }
                else
                {
                    options = new List<AI.Entity.InputOption>();
                }

                TaskController _taskController = new TaskController();
                if (_taskController.execHumanInTheLoopByForm(sessionID, taskID, recordID, options, out errorCode))
                {
                    return JsonMsg<string>.OK(null, SessionID: sessionID);
                }
                else
                {
                    return JsonMsg<string>.Error(null, errorCode);
                }
            }
            else
            {
                return JsonMsg<string>.Error(null, ErrorCode.DataFormatError);
            }
        }
    }
}
