using DocumentFormat.OpenXml.Drawing.Charts;
using Elastic.Clients.Elasticsearch;
using log4net.Plugin;
using Markdig;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Repositories;
using ZSN.AI.Core.Service;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Workflow;
using System.Collections.Concurrent;
using ZSN.AI.Node.Utils;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Service.Token;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using Node = ZSN.AI.Node.Utils.Utils;


namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{

    public class WorkflowController : AdminBaseController
    {
        private readonly IChatService _chatService;
        private readonly ILogger _logger;

        public WorkflowController(IChatService chatService, ILogger<WorkflowController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [AdminAttributes]
        public IActionResult index(int type = 1, string mid = "", int index = 1, int size = 10)
        {
            string _where = " MainType=" + type + " and MainID='" + mid + "'";

            var lst = WorkflowInfoBussiness.GetListByPage(size, index, _where, out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.AppList = lst;
            ViewBag.MainType = type;
            ViewBag.MainID = mid;
            return View();
        }


        [HttpGet]
        [AdminAttributes(CheckLogin = false, CheckUrl = false)]
        public JsonMsg<BaseConfig> getBaseConfig()
        {
            BaseConfig baseConfig = new BaseConfig();
            baseConfig.largeModelList = LargeModelInfoBussiness.GetList(" SystemStatus= " + LargeModelStatus.Normal.ToInt32());
            baseConfig.knowledgeBaseList = KnowledgeBaseInfoBussiness.GetList(" SystemStatus= " + KnowledgeBaseStatus.Normal.ToInt32());
            baseConfig.pluginsList = PluginsInfoBussiness.GetList(" SystemStatus= " + PluginsState.Normal.ToInt32());
            baseConfig.agentList = AgentInfoBussiness.GetList(" SystemStatus= " + AgentStatus.Normal.ToInt32());
            baseConfig.mcpList = McpInfoBussiness.GetList(" SystemStatus= " + McpState.Normal.ToInt32());
            baseConfig.wordTemplateList = WordTemplateInfoBussiness.GetList(" SystemStatus= " + WordTemplateStatus.Normal.ToInt32());
            baseConfig.skillList = SkillInfoBussiness.GetList(" SystemStatus= " + SkillStatus.Normal.ToInt32());

            string previewHost = ConfigHelper.GetString("previewHost");
            if (baseConfig.largeModelList != null)
            {
                foreach (var model in baseConfig.largeModelList)
                {
                    if (!model.MICON.IsNullOrEmpty())
                    {
                        model.MICON = previewHost.Replace("{code}", model.MICON.Trim());
                    }
                }
            }
            if (baseConfig.agentList != null)
            {
                foreach (var agent in baseConfig.agentList)
                {
                    if (!agent.AICON.IsNullOrEmpty())
                    {
                        agent.AICON = previewHost.Replace("{code}", agent.AICON.Trim());
                    }
                }
            }
            if (baseConfig.mcpList != null)
            {
                foreach (var mcp in baseConfig.mcpList)
                {
                    if (!mcp.ICON.IsNullOrEmpty())
                    {
                        mcp.ICON = previewHost.Replace("{code}", mcp.ICON.Trim());
                    }
                }
            }
            return JsonMsg<BaseConfig>.OK(baseConfig);
        }

        [HttpPost]
        [AdminAttributes(CheckLogin = false, CheckUrl = false)]
        public JsonMsg<List<WorkFlow>> getWorkFlowListByAgentID(string AgentID)
        {
            List<WorkFlow> workFlows = new List<WorkFlow>();
            var workflowInfos = WorkflowInfoBussiness.GetList("SystemStatus=" + WorkflowStatus.Normal.ToInt32() + " and MainType=" + MainType.Agent.ToInt32() + " and MainID='" + AgentID + "' ");
            if (workflowInfos != null)
            {
                foreach (var info in workflowInfos)
                {
                    WorkFlow workflow = new WorkFlow();
                    workflow.Info = info;
                    workflow.WorkflowID = info.WorkflowID;
                    workflow.MainID = info.MainID;
                    workflow.MainType = info.MainType;
                    workflow.Config = (WorkFlowConfig)info.Config;
                    workFlows.Add(workflow);
                }
            }
            return JsonMsg<List<WorkFlow>>.OK(workFlows);
        }

        [HttpPost]
        [AdminAttributes(CheckLogin = false, CheckUrl = false)]
        public JsonMsg<WorkFlow> getWorkFlow(string WorkflowID, string MainID, MainType MainType = MainType.APP)
        {
            WorkFlow workflow = new WorkFlow();
            workflow.Info = WorkflowInfoBussiness.GetModel(WorkflowID);
            if (workflow.Info != null)
            {
                workflow.WorkflowID = workflow.Info.WorkflowID;
                workflow.MainID = workflow.Info.MainID;
                workflow.MainType = workflow.Info.MainType;
                workflow.Config = (WorkFlowConfig)workflow.Info.Config;

                //if (workflow.Info.SystemStatus != WorkflowStatus.Disable)
                {
                    workflow.Nodes = WorkflowNodeInfoBussiness.GetList(" WorkflowID='" + WorkflowID + "' ");
                    workflow.Edges = WorkflowEdgeInfoBussiness.GetList(" WorkflowID='" + WorkflowID + "' ");

                    return JsonMsg<WorkFlow>.OK(workflow);
                }
                //else
                {
                    //    return JsonMsg<WorkFlow>.Error(null, ErrorCode.Locked);
                }
            }
            else
            {
                if (!MainID.IsNullOrEmpty())
                {
                    workflow = Node.initWorkFlow(MainID, MainType);

                    return JsonMsg<WorkFlow>.OK(workflow);
                }
                else
                {
                    return JsonMsg<WorkFlow>.Error(null, ErrorCode.DataEmpty);
                }

            }
        }

        [HttpPost]
        [AdminAttributes(CheckLogin = false, CheckUrl = false)]
        public JsonMsg<WorkflowNodeInfo> addNode(string WorkflowID, string NodeType, string MainID)
        {

            WorkflowNodeInfo nodeInfo = Node.newNode(WorkflowID, (NodeType)Enum.Parse(typeof(NodeType), NodeType), MainID);

            return JsonMsg<WorkflowNodeInfo>.OK(nodeInfo);
        }

        public IActionResult Edit(string id = "", string MainID = "", int MainType = 0)
        {
            AppInfo appinfo = null;
            AgentInfo agentInfo = null;

            string MainName = "";
            WorkflowInfo Workflow = new WorkflowInfo();

            if (MainType == 1)
            {
                appinfo = AppInfoBussiness.GetModel(MainID);
                MainName = appinfo.Name;
                id = appinfo.WorkFlowID;
            }
            else
            {
                agentInfo = AgentInfoBussiness.GetModel(MainID);
                MainName = agentInfo.Name;
            }

            if (!id.IsNullOrEmpty())
            {
                Workflow = WorkflowInfoBussiness.GetModel(id);
            }
            else
            {
                id = Workflow.WorkflowID;
            }
            WorkflowTester workflowTester = WorkflowTester.Config;

            ViewBag.APIAppID = workflowTester.APIAppID;
            ViewBag.Workflow = Workflow;
            ViewBag.MainName = MainName;
            ViewBag.MainID = MainID;
            ViewBag.MainType = MainType;

            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");

            return View();
        }

        [HttpPost]
        public JsonMsg<string> Save([FromBody] WorkFlow workFlow)
        {
            if (workFlow != null)
            {
                string WorkFlowID = WorkflowInfoBussiness.Save(workFlow);

                return JsonMsg<string>.OK(WorkFlowID);
            }
            else
            {
                return JsonMsg<string>.Error(null, ErrorCode.DataEmpty);
            }

        }

        [HttpPost]
        public JsonMsg<string> Status(string mid, bool status)
        {
            var Workflow = WorkflowInfoBussiness.GetModel(mid);
            Workflow.SystemStatus = status ? WorkflowStatus.Normal : WorkflowStatus.Disable;

            WorkflowInfoBussiness.Update(Workflow);
            return JsonMsg<string>.OK("更新成功");
        }

        [HttpPost]
        public JsonMsg<ChatLog> getChatLog(string ChatSessionID)
        {
            ChatLog chatLog = new ChatLog();
            chatLog.Log = AppChatLogInfoBussiness.GetList(" ChatSessionID='" + ChatSessionID + "'");

            return JsonMsg<ChatLog>.OK(chatLog);
        }
        public JsonMsg<string> Del(string mid)
        {
            WorkflowInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功");
        }

        public JsonMsg<string> Copy(string mid, string name)
        {
            string FromWorkflowID = mid.SecureSQL();
            string NewWorkflowID = Guid.NewGuid().ToString();
            if (FromWorkflowID.IsNullOrEmpty())
            {
                return JsonMsg<string>.Error(null, ErrorCode.DataEmpty);
            }
            WorkFlow workflow = new WorkFlow();
            workflow.Info = WorkflowInfoBussiness.GetModel(FromWorkflowID);
            if (workflow.Info != null)
            {
                workflow.Nodes = WorkflowNodeInfoBussiness.GetList(" WorkflowID='" + FromWorkflowID + "' ");
                workflow.Edges = WorkflowEdgeInfoBussiness.GetList(" WorkflowID='" + FromWorkflowID + "' ");

                workflow.WorkflowID = NewWorkflowID;

                workflow.Info.WorkflowName = name;
                workflow.Info.WorkflowID = NewWorkflowID;
                workflow.Info.CreateTime = DateTime.Now;
                workflow.Info.LastUpdateTime = DateTime.Now;

                workflow.MainID = workflow.Info.MainID;
                workflow.MainType = workflow.Info.MainType;
                workflow.Config = (WorkFlowConfig)workflow.Info.Config;

                // 创建旧NodeID到新NodeID的映射字典
                Dictionary<string, string> nodeIdMapping = new Dictionary<string, string>();

                //替换所有Nodes中的WorkflowID和NodeID
                if (workflow.Nodes != null)
                {
                    foreach (var node in workflow.Nodes)
                    {
                        string oldNodeID = node.NodeID;
                        string newNodeID = Guid.NewGuid().ToString();

                        // 建立NodeID映射关系
                        nodeIdMapping[oldNodeID] = newNodeID;

                        node.NodeID = newNodeID;
                        node.WorkflowID = NewWorkflowID;
                        node.CreateTime = DateTime.Now;
                        node.LastUpdateTime = DateTime.Now;

                        // 替换所有Node.Config中的WorkflowID和NodeID
                        if (node.Config != null)
                        {
                            string configJson = JsonConvert.SerializeObject(node.Config);

                            // 替换WorkflowID
                            configJson = configJson.Replace(FromWorkflowID, NewWorkflowID);

                            // 替换所有旧NodeID为新NodeID
                            foreach (var mapping in nodeIdMapping)
                            {
                                configJson = configJson.Replace(mapping.Key, mapping.Value);
                            }

                            node.Config = JsonConvert.DeserializeObject(configJson);
                        }
                    }
                }

                //替换所有Edges中的WorkflowID、EdgeID和节点引用
                if (workflow.Edges != null)
                {
                    foreach (var edge in workflow.Edges)
                    {
                        string oldEdgeID = edge.EdgeID;
                        string newEdgeID = Guid.NewGuid().ToString();

                        edge.EdgeID = newEdgeID;
                        edge.WorkflowID = NewWorkflowID;
                        edge.CreateTime = DateTime.Now;
                        edge.LastUpdateTime = DateTime.Now;

                        // 替换源节点ID和目标节点ID
                        if (nodeIdMapping.ContainsKey(edge.SourceNodeId))
                        {
                            edge.SourceNodeId = nodeIdMapping[edge.SourceNodeId];
                        }
                        if (nodeIdMapping.ContainsKey(edge.TargetNodeId))
                        {
                            edge.TargetNodeId = nodeIdMapping[edge.TargetNodeId];
                        }

                        // 替换Edge.Config中的NodeID
                        if (edge.Config != null)
                        {
                            string edgeConfigJson = JsonConvert.SerializeObject(edge.Config);
                            foreach (var mapping in nodeIdMapping)
                            {
                                edgeConfigJson = edgeConfigJson.Replace(mapping.Key, mapping.Value);
                            }
                            edgeConfigJson = edgeConfigJson.Replace(oldEdgeID, newEdgeID);
                            edge.Config = JsonConvert.DeserializeObject(edgeConfigJson);
                        }

                        // 替换Edge.ConditionConfig中的NodeID
                        if (edge.ConditionConfig != null)
                        {
                            string conditionConfigJson = JsonConvert.SerializeObject(edge.ConditionConfig);
                            foreach (var mapping in nodeIdMapping)
                            {
                                conditionConfigJson = conditionConfigJson.Replace(mapping.Key, mapping.Value);
                            }
                            conditionConfigJson = conditionConfigJson.Replace(oldEdgeID, newEdgeID);
                            edge.ConditionConfig = JsonConvert.DeserializeObject(conditionConfigJson);
                        }
                    }
                }

                WorkflowInfoBussiness.Save(workflow);

                return JsonMsg<string>.OK("复制成功");
            }
            else
            {
                return JsonMsg<string>.Error(null, ErrorCode.DataEmpty);
            }
        }

        /// <summary>
        ///  组织用于前端测试使用的测试配置信息
        /// </summary>
        /// <param name="workflowID"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonMsg<WorkflowTester> GetTesterConfig(string workflowID)
        {
            WorkflowTester workflowTester = WorkflowTester.Config;

            WorkflowInfo workflowInfo = !workflowID.IsNullOrEmpty() ? WorkflowInfoBussiness.GetModel(workflowID) : null;
            if (workflowInfo != null)
            {
                string TesterAppID = workflowTester.APIAppID;
                string SecretKey = workflowTester.SecretKey;
                string AccessToken = "";
                string MemberToken = "";
                string RefreshToken = "";
                workflowTester.WorkflowID = workflowInfo.WorkflowID;
                workflowTester.ExpirationDate = DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut"));


                MemberTokenHelper.Set(workflowTester.MemberID, 0, null, out MemberToken, out RefreshToken);

                AccessToken = CommonApiBaseController.GetTokenByAPPID(TesterAppID);

                workflowTester.APIAppID = TesterAppID;
                workflowTester.SecretKey = SecretKey;

                if (workflowInfo.MainType == MainType.APP)
                {
                    workflowTester.AppID = workflowInfo.MainID;
                }
                else if (workflowInfo.MainType == MainType.Agent)
                {
                    workflowTester.AppID = workflowTester.AppID;
                }
                workflowTester.AccessToken = AccessToken;
                workflowTester.MemberToken = MemberToken;
                workflowTester.RefreshToken = RefreshToken;
            }

            return JsonMsg<WorkflowTester>.OK(workflowTester);
        }

        /// <summary>
        /// 自动生成下游节点（SSE 流式输出）
        /// Phase 1: 规划 → Phase 2: 并行生成节点详情 → Phase 3: 组装
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AutoGenerateDownstreamNodes(
            string sourceNodeId,
            string sourceNodeType,
            string upstreamContext,
            string userRequirement,
            string workflowId)
        {
            if (sourceNodeId.IsNullOrEmpty() || userRequirement.IsNullOrEmpty())
            {
                return Json(JsonMsg<string>.Error(null, ErrorCode.DataEmpty));
            }

            // 设置 SSE 响应头
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache, no-transform";
            Response.Headers["X-Accel-Buffering"] = "no";
            Response.Headers["Connection"] = "keep-alive";
            HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();

            using var sseCts = new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted, sseCts.Token);

            async Task SafeWriteAsync(string data)
            {
                try
                {
                    if (linkedCts.IsCancellationRequested) return;
                    await Response.WriteAsync(data, linkedCts.Token);
                }
                catch (ObjectDisposedException) { }
                catch (OperationCanceledException) { }
                catch (InvalidOperationException) { }
            }

            async Task SafeFlushAsync()
            {
                try
                {
                    if (linkedCts.IsCancellationRequested) return;
                    await Response.Body.FlushAsync(linkedCts.Token);
                }
                catch (ObjectDisposedException) { }
                catch (OperationCanceledException) { }
                catch (InvalidOperationException) { }
            }

            var queue = new ConcurrentQueue<string>();
            var signal = new SemaphoreSlim(0);
            var writerDone = new TaskCompletionSource<bool>();

            // 后台写入线程：用哨兵 __DONE__ 结束，而非 CancellationToken
            var writerTask = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        await signal.WaitAsync(linkedCts.Token);
                        while (queue.TryDequeue(out var data))
                        {
                            if (data == "__DONE__")
                            {
                                writerDone.TrySetResult(true);
                                return;
                            }
                            await SafeWriteAsync(data);
                            await SafeFlushAsync();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    writerDone.TrySetResult(true);
                }
            }, linkedCts.Token);

            try
            {
                // SSE 事件推送回调（放入队列）
                async Task OnEvent(StreamEvent evt)
                {
                    try
                    {
                        var json = JsonConvert.SerializeObject(evt.Data);
                        if (linkedCts.IsCancellationRequested) return;
                        queue.Enqueue($"event: {evt.EventType}\ndata: {json}\n\n");
                        try { signal.Release(); } catch { }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SSE OnEvent 序列化失败, EventType={Type}", evt.EventType);
                    }
                }

                // 获取工作流信息
                var workflowInfo = WorkflowInfoBussiness.GetModel(workflowId);
                if (workflowInfo == null)
                {
                    await OnEvent(new StreamEvent
                    {
                        EventType = "error",
                        Data = new { message = "工作流不存在" }
                    });
                    queue.Enqueue("__DONE__");
                    try { signal.Release(); } catch { }
                    try { await Task.WhenAny(writerTask, Task.Delay(5000)); } catch { }
                    return new EmptyResult();
                }
                var mainId = workflowInfo.MainID;

                // 执行三阶段流水线
                var generator = HttpContext.RequestServices.GetRequiredService<WorkflowAutoGenerator>();
                await generator.GenerateAsync(
                    sourceNodeId, sourceNodeType, upstreamContext, userRequirement,
                    workflowId, mainId, OnEvent, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SSE 连接被客户端取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动生成下游节点失败");
                if (!linkedCts.IsCancellationRequested)
                {
                    var json = JsonConvert.SerializeObject(new { message = ex.Message });
                    await SafeWriteAsync($"event: error\ndata: {json}\n\n");
                    await SafeFlushAsync();
                }
            }
            finally
            {
                // 写入 [DONE] 和哨兵，让 writerTask 依次处理完毕
                if (!linkedCts.IsCancellationRequested)
                {
                    queue.Enqueue("data: [DONE]\n\n");
                    queue.Enqueue("__DONE__");
                    try { signal.Release(); } catch { }
                }
                else
                {
                    queue.Enqueue("__DONE__");
                    try { signal.Release(); } catch { }
                }

                // 等待 writerTask 处理完队列（最长等 5 秒）
                try { await Task.WhenAny(writerTask, Task.Delay(5000)); } catch { }
                sseCts.Cancel();
                try { await writerTask; } catch { }
            }

            return new EmptyResult();
        }

        /// <summary>
        /// 提示词扩写
        /// </summary>
        /// <param name="prompts"></param>
        /// <param name="stream"></param>
        /// <param name="PromptsType">Node:流程节点提示词优化，Skills:技能提示词优化</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> OptimizePrompts(string prompts, bool stream = false, string PromptsType = "Node")
        {
            if (prompts.IsNullOrEmpty())
            {
                return Json(JsonMsg<string>.Error(null, ErrorCode.DataEmpty));
            }
            else
            {
                LargeModelInfo modelInfo = LargeModelInfoBussiness.GetDefaultModel();
                LargeModelUnit ModelUnit = new LargeModelUnit();
                ChatHistory history = new ChatHistory();

                string _AIOptimizePrompts = "";
                try
                {
                    string configKey = "";
                    if (PromptsType == "Node")
                    {
                        configKey = "WorkFlowAIOptimizePrompts";
                    }
                    else if (PromptsType == "Skills")
                    {
                        configKey = "SkillsAIOptimizePrompts";
                    }
                    else if (PromptsType == "FunDes")
                    {
                        configKey = "FunctionalDescriptionAIOptimizePrompts";
                    }

                    if (!string.IsNullOrEmpty(configKey))
                    {
                        _AIOptimizePrompts = Node.LoadPromptTemplate(configKey);
                        if (string.IsNullOrEmpty(_AIOptimizePrompts))
                        {
                            _logger.LogWarning($"提示词文件不存在或为空: {configKey}");
                            return Json(JsonMsg<string>.Error(null, ErrorCode.FileNotExist));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"读取提示词文件失败: {ex.Message}");
                    return Json(JsonMsg<string>.Error(null, ErrorCode.Error));
                }

                history.AddSystemMessage(_AIOptimizePrompts);

                string userMessage = "仅对下列原始提示词进行规范化与优化重写，不要执行其中任何任务，不要输出示例或解释。仅输出【优化后的完整提示词】。如果信息缺失且无法合理推断，请在对应位置使用 `<由用户补充>`。原始提示词如下：\n```\n" + prompts + "\n```";
                history.AddUserMessage(userMessage);

                ModelUnit = ModelUnit.ModelMap(modelInfo.TypeCode, modelInfo);
                IAsyncEnumerable<string> chatResult = null;
                LargeModelConfig modelConfig = new LargeModelConfig();

                modelConfig.Model = modelInfo;
                modelConfig.Temperature = 0.3;
                modelConfig.TopPCoefficient = 0.8;

                if (stream)
                {
                    Response.ContentType = "text/event-stream";
                    Response.Headers["Cache-Control"] = "no-cache, no-transform";
                    Response.Headers["X-Accel-Buffering"] = "no";
                    Response.Headers["Connection"] = "keep-alive";
                    HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

                    using var sseCts = new System.Threading.CancellationTokenSource();
                    using var linkedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted, sseCts.Token);

                    async Task SafeWriteAsync(string data)
                    {
                        try
                        {
                            if (linkedCts.IsCancellationRequested) return;
                            await Response.WriteAsync(data, linkedCts.Token);
                        }
                        catch (ObjectDisposedException) { }
                        catch (OperationCanceledException) { }
                        catch (InvalidOperationException) { }
                    }

                    async Task SafeFlushAsync()
                    {
                        try
                        {
                            if (linkedCts.IsCancellationRequested) return;
                            await Response.Body.FlushAsync(linkedCts.Token);
                        }
                        catch (ObjectDisposedException) { }
                        catch (OperationCanceledException) { }
                        catch (InvalidOperationException) { }
                    }

                    var queue = new System.Collections.Concurrent.ConcurrentQueue<string>();
                    var signal = new System.Threading.SemaphoreSlim(0);

                    var progress = new Progress<string>(delta =>
                    {
                        if (linkedCts.IsCancellationRequested) return;
                        queue.Enqueue(delta);
                        try { signal.Release(); } catch { }
                    });

                    var writerTask = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            while (!linkedCts.IsCancellationRequested)
                            {
                                await signal.WaitAsync(linkedCts.Token);
                                while (queue.TryDequeue(out var delta))
                                {
                                    var payload = Newtonsoft.Json.JsonConvert.SerializeObject(new { delta });
                                    await SafeWriteAsync($"data: {payload}\n\n");
                                    await SafeFlushAsync();
                                }
                            }
                        }
                        catch (OperationCanceledException) { }
                    }, linkedCts.Token);

                    try
                    {
                        chatResult = _chatService.SendChatAsync(
                                    modelConfig,
                                    history,
                                    Function: null,
                                    responseFormat: "text",
                                    progress: progress,
                                    enableStreamingObservation: true,
                                    ct: linkedCts.Token
                                );

                        await foreach (var _ in chatResult.WithCancellation(linkedCts.Token)) { }
                    }
                    finally
                    {
                        sseCts.Cancel();
                        try { await writerTask; } catch { }
                        if (!HttpContext.RequestAborted.IsCancellationRequested)
                        {
                            await SafeWriteAsync("data: [DONE]\\n\\n");
                            await SafeFlushAsync();
                        }
                    }
                    return new EmptyResult();
                }
                else
                {
                    StringBuilder rawContent = new StringBuilder();
                    chatResult = _chatService.SendChatAsync(modelConfig, history);
                    await foreach (var content in chatResult)
                    {
                        rawContent.Append(content.ConvertToString());
                    }

                    return Json(JsonMsg<string>.OK(rawContent.ToString()));
                }
            }
        }

        /// <summary>
        /// 强制停止工作流
        /// </summary>
        /// <param name="ChatSessionID"></param>
        /// <returns></returns>
        public JsonMsg<string> stopSession(string ChatSessionID)
        {

            List<TaskInfo> taskInfos = TaskInfoBussiness.GetList(" SessionID='" + ChatSessionID + "' and State!=" + TaskState.Completed.ToInt32());

            foreach (var task in taskInfos)
            {
                TaskInfoBussiness.updateTask(task.TaskID, TaskState.Failure, new AI.Entity.Results() { Data = new { Message = "工作流被强制停止" } });

                // 清理 ClawAI 异步步骤相关的 Redis 临时 key
                if (task.TaskType == NodeType.ClawAIWorkflowStep)
                {
                    try
                    {
                        var context = JsonConvert.DeserializeObject<ZSN.AI.Entity.ClawAI.ClawAIStepContext>(
                            JsonConvert.SerializeObject(task.TaskConfig.NotNodeConfig));
                        if (context != null)
                        {
                            var redis = new ZSN.Utils.Core.Helpers.RedisHelper()
                                .GetConnectionRedisMultiplexer().GetDatabase();
                            string processesID = context.ProcessesID;
                            if (!string.IsNullOrEmpty(processesID))
                            {
                                // 清理结果、计数器、上下文、锁
                                redis.KeyDelete($"clawai:result:{processesID}:{context.TriggeredStepId}");
                                redis.KeyDelete($"clawai:layer:{processesID}:{context.CurrentLayerIndex}");
                                redis.KeyDelete($"clawai:ctx:{processesID}:{context.CurrentLayerIndex}");
                                redis.KeyDelete($"clawai:lock:{processesID}");
                            }
                        }
                    }
                    catch
                    {
                        // Redis 清理失败不影响主流程
                    }
                }
            }

            return JsonMsg<string>.OK("停止成功");
        }
    }
}
