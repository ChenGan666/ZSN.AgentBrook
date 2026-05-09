using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Google.Protobuf.WellKnownTypes;
using Lucene.Net.Util.Fst;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlSugar.DistributedSystem.Snowflake;
using System.Text.Json;
using ZSN.AgentBrook.API.Attributes;
using ZSN.AI.API.Pages;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Functions;
using ZSN.AI.Service.Attributes;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-Public")]
    [Route("api/[controller]/[action]")]
    public class TaskController : ApiBaseController
    {
        public TaskController()
        {
        }
        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }

        /// <summary>
        /// 回调
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = false, MemberToken = false, Sign = false, Timestamp = false)]
        public JsonMsg<string> ReCall([FromQuery] string sessionID, [FromQuery] string taskID, [FromQuery] string recordID)
        {
            string SessionID = sessionID.SecureSQL();
            string TaskID = taskID.SecureSQL();
            string RecordID = recordID.SecureSQL();

            string json = HttpContextHelper.Session.GetString("BodyParams");

            if (SessionID.IsNullOrEmpty() || TaskID.IsNullOrEmpty() || json.IsNullOrEmpty())
            {
                return JsonMsg<string>.Error(null, ErrorCode.DataEmpty);
            }
            else
            {
                List<Output> outputs = new List<Output>();
                List<string> Logs = new List<string>();
                TaskInfo taskInfo = TaskInfoBussiness.GetModel(TaskID);
                TaskConfig taskConfig = taskInfo.TaskConfig;
                TaskData data = taskConfig.Data;
                NodeConfig config = taskConfig.NodeConfig;

                if (taskInfo == null)
                {
                    return JsonMsg<string>.Error(null, ErrorCode.TaskNotExists);
                }
                if (taskInfo.State != TaskState.Failure)
                {
                    //驱动下一节点必要参数
                    string AppID = data.AppID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
                    ErrorCode errorCode = ErrorCode.None;
                    switch (taskInfo.TaskType)
                    {
                        case NodeType.FileToMarkdown:
                            List<ConvertToMarkdownFiles> markdownFiles = System.Text.Json.JsonSerializer.Deserialize<List<ConvertToMarkdownFiles>>(json);

                            List<Inputs> inputs = data.Inputs;

                            List<AttachmentItem> AttachmentItems = AI.Node.Utils.Utils.updateAttachmentItemsFilePath(data.AttachmentItems);
                            var _attachmentsString = JsonConvert.SerializeObject(AttachmentItems);
                            inputs.Add(new Inputs() { varname = "attachments", type = "List<AttachmentItem>", value = _attachmentsString, sourceId = $"{config.id}_attachments" });

                            outputs.Add(new Output { varname = "currentTime", value = DateTime.Now.ToDateTimeString(), nodeId = config.id, sourceId = $"{config.id}_currentTime" });
                            outputs.Add(new Output { varname = "markdownFiles", type = "List<ConvertToMarkdownFiles>", value = JsonConvert.SerializeObject(markdownFiles), nodeId = config.id, sourceId = $"{config.id}_markdownFiles" });


                            WorkflowNodeInfoBussiness.NextNode(AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID, config, inputs, outputs, Logs);

                            taskInfo.State = TaskState.Completed;
                            taskInfo.Results = new AI.Entity.Results() { Data = markdownFiles };

                            TaskInfoBussiness.Update(taskInfo);

                            AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus.Success, outputs, Logs);
                            break;

                        case NodeType.HumanInTheLoop:
                            this.execHumanInTheLoop(SessionID, TaskID, RecordID, System.Text.Json.JsonSerializer.Deserialize<AI.Entity.Option>(json), out errorCode);
                            break;
                        case NodeType.HumanInTheLoopInput:
                            this.execHumanInTheLoopByForm(SessionID, TaskID, RecordID, System.Text.Json.JsonSerializer.Deserialize<List<AI.Entity.InputOption>>(json), out errorCode);
                            break;
                    }
                    if (errorCode != ErrorCode.None)
                    {
                        return JsonMsg<string>.Error(null, errorCode);
                    }
                    else
                    {
                        return JsonMsg<string>.OK(null);
                    }
                }
                else
                {
                    AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus.Fail, outputs, Logs);
                    return JsonMsg<string>.Error(null, ErrorCode.TaskStateError);
                }

            }
        }

        [HiddenApi]
        public bool execHumanInTheLoop(string sessionID, string taskID, string recordID, AI.Entity.Option option, out ErrorCode errorCode)
        {
            errorCode = ErrorCode.None;
            string SessionID = sessionID.SecureSQL();
            string TaskID = taskID.SecureSQL();
            string RecordID = recordID.SecureSQL();

            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            TaskInfo taskInfo = TaskInfoBussiness.GetModel(TaskID);
            TaskConfig taskConfig = taskInfo.TaskConfig;
            TaskData data = taskConfig.Data;
            NodeConfig config = taskConfig.NodeConfig;

            if (taskInfo == null)
            {
                errorCode = ErrorCode.TaskNotExists;
                return false;
            }
            if (taskInfo.State != TaskState.Failure)
            {
                //驱动下一节点必要参数
                string AppID = data.AppID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
                HumanInTheLoopData nodeData = System.Text.Json.JsonSerializer.Deserialize<HumanInTheLoopData>(config.data.ToString());
                List<AI.Entity.Option> options = new List<AI.Entity.Option>();

                try
                {
                    if (nodeData.optionMode == "dynamic")
                    {
                        string dynamicOptionsVar = nodeData.dynamicOptionsVar;
                        var optionInput = data.Inputs.Where(m => m.sourceId == dynamicOptionsVar).FirstOrDefault();
                        if (optionInput != null)
                        {
                            options = System.Text.Json.JsonSerializer.Deserialize<List<AI.Entity.Option>>(optionInput.value);
                        }
                    }
                    else
                    {
                        options = nodeData.options;
                    }
                }
                catch (Exception ex)
                {
                    Logs.Add($"{ex.Message}");
                }


                //驱动选中的下一节点,找出对应的下一节点ID

                List<WorkflowEdgeInfo> edgeList = WorkflowEdgeInfoBussiness.GetListBySourceNodeId(config.id);
                if (edgeList != null && edgeList.Count > 0)
                {
                    List<string> TargetNodeId = new();
                    if (option != null)
                    {
                        foreach (var edge in edgeList)
                        {
                            var cfg = edge.Config as JObject ?? JObject.FromObject(edge.Config);
                            if ((string?)cfg["sourceHandle"] == option.id)
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
                            outputs.Add(new Output { varname = "results", value = JsonConvert.SerializeObject(option), nodeId = config.id, sourceId = $"{config.id}_results" });

                            foreach (var node in targetNodeList)
                            {
                                NodeConfig targetNode = new NodeConfig() { id = node.NodeID, mainid = config.mainid, workflowid = node.WorkflowID, type = node.NodeType, data = node.Config };

                                string newTaskID = TaskInfoBussiness.toTask(config, outputs, targetNode, AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID);

                                Logs.Add($"{newTaskID}");
                            }
                        }
                    }
                }

                taskInfo.State = TaskState.Completed;
                taskInfo.Results = new AI.Entity.Results() { Data = option };

                TaskInfoBussiness.Update(taskInfo);

                AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus.Success, outputs, Logs);
            }
            return true;
        }

        [HiddenApi]
        public bool execHumanInTheLoopByForm(string sessionID, string taskID, string recordID, List<AI.Entity.InputOption> inputOption, out ErrorCode errorCode)
        {
            errorCode = ErrorCode.None;
            string SessionID = sessionID.SecureSQL();
            string TaskID = taskID.SecureSQL();
            string RecordID = recordID.SecureSQL();

            List<Output> outputs = new List<Output>();
            List<string> Logs = new List<string>();
            TaskInfo taskInfo = TaskInfoBussiness.GetModel(TaskID);
            TaskConfig taskConfig = taskInfo.TaskConfig;
            TaskData data = taskConfig.Data;
            NodeConfig config = taskConfig.NodeConfig;

            if (taskInfo == null)
            {
                errorCode = ErrorCode.TaskNotExists;
                return false;
            }
            if (taskInfo.State != TaskState.Failure)
            {
                //驱动下一节点必要参数
                string AppID = data.AppID, ProcessesID = data.ProcessesID, AgentNodeID = data.AgentNodeID, FromMainTaskID = data.FromMainTaskID;
                HumanInTheLoopInputData nodeData = System.Text.Json.JsonSerializer.Deserialize<HumanInTheLoopInputData>(config.data.ToString());
                List<AI.Entity.InputOption> options = new List<AI.Entity.InputOption>();

                try
                {
                    if (nodeData.optionMode == "dynamic")
                    {
                        string dynamicOptionsVar = nodeData.dynamicOptionsVar;
                        var optionInput = data.Inputs.Where(m => m.sourceId == dynamicOptionsVar).FirstOrDefault();
                        if (optionInput != null)
                        {
                            if (ZSN.Utils.Core.Utils.Utils.TryExtractStrictJson(optionInput.value, out var ___cleaned))
                            {
                                optionInput.value = ___cleaned;
                            }
                            options = System.Text.Json.JsonSerializer.Deserialize<List<AI.Entity.InputOption>>(optionInput.value);
                        }
                    }
                    else
                    {
                        options = nodeData.inputOptions;
                    }
                }
                catch (Exception ex)
                {
                    Logs.Add($"{ex.Message}");
                }


                //校验表单填充项，执行本节点事务，并将执行结果转入下一个节点
                if (options == null || options.Count == 0)
                {
                    errorCode = ErrorCode.DataEmpty;
                    return false;
                }

                // 列表匹配与必填校验（方案A）
                List<AI.Entity.InputOption> matchedOptions = new List<AI.Entity.InputOption>();
                if (inputOption != null && inputOption.Count > 0)
                {
                    foreach (var userItem in inputOption)
                    {
                        AI.Entity.InputOption matched = null;
                        if (!string.IsNullOrEmpty(userItem.id))
                        {
                            matched = options.FirstOrDefault(o => o.id == userItem.id);
                        }
                        if (matched == null)
                        {
                            matched = options.FirstOrDefault(o => o.name == userItem.name);
                        }
                        if (matched != null)
                        {
                            matchedOptions.Add(new AI.Entity.InputOption
                            {
                                id = matched.id,
                                name = matched.name,
                                value = userItem.value,
                                isRequired = matched.isRequired
                            });
                        }
                    }
                }

                // 校验必填项
                var requiredNames = options.Where(o => o.isRequired).Select(o => o.name).ToList();
                bool requiredMissing = requiredNames.Any(req => !matchedOptions.Any(m => m.name == req && !string.IsNullOrEmpty(m.value)));
                if (requiredMissing || matchedOptions.Count == 0)
                {
                    errorCode = ErrorCode.InvalidParameter;
                    return false;
                }

                // 输出匹配列表
                outputs.Add(new Output { varname = "results", value = JsonConvert.SerializeObject(matchedOptions), nodeId = config.id, sourceId = $"{config.id}_results" });

                List<WorkflowEdgeInfo> edgeList = WorkflowEdgeInfoBussiness.GetListBySourceNodeId(config.id);
                if (edgeList != null && edgeList.Count > 0)
                {
                    List<string> TargetNodeId = new();

                    foreach (var edge in edgeList)
                    {
                        TargetNodeId.Add(edge.TargetNodeId);
                    }

                    if (TargetNodeId.Count > 0)
                    {
                        List<WorkflowNodeInfo> targetNodeList = WorkflowNodeInfoBussiness.GetListByNodeID(string.Join(",", TargetNodeId.Select(id => $"'{id}'")));
                        if (targetNodeList != null)
                        {
                            foreach (var node in targetNodeList)
                            {
                                NodeConfig targetNode = new NodeConfig() { id = node.NodeID, mainid = config.mainid, workflowid = node.WorkflowID, type = node.NodeType, data = node.Config };
                                string newTaskID = TaskInfoBussiness.toTask(config, outputs, targetNode, AppID, SessionID, ProcessesID, TaskID, FromMainTaskID, AgentNodeID);
                                Logs.Add($"{newTaskID}");
                            }
                        }
                    }
                }
                taskInfo.State = TaskState.Completed;
                taskInfo.Results = new AI.Entity.Results() { Data = matchedOptions };

                TaskInfoBussiness.Update(taskInfo);

                AI.Node.Utils.Utils.updateExcutionRecord(RecordID, ExecutionRecordStatus.Success, outputs, Logs);
            }
            return true;
        }
        [HiddenApi]
        public bool execHumanInTheLoopByUserInput(List<WorkflowNodeExecutionRecordInfo> _HumanTaskRecords, GptMsg Inputs, TaskData data, string sessionID, out ErrorCode errorCode)
        {
            bool result = false;
            errorCode = ErrorCode.None;
            string recordID = "";
            foreach (var humanTaskRecord in _HumanTaskRecords)
            {
                if (humanTaskRecord != null)
                {
                    string taskID = humanTaskRecord.TaskID;
                    humanTaskRecord.Status = ExecutionRecordStatus.Success;

                    WorkflowNodeExecutionRecordInfo _nodeRecordInfo = WorkflowNodeExecutionRecordInfoBussiness.GetModelByTaskID(taskID, sessionID);
                    if (_nodeRecordInfo != null)
                    {
                        recordID = _nodeRecordInfo.RecordID;

                        //用户不选则直接输入的内容作为选项
                        if (humanTaskRecord?.NodeName == NodeType.HumanInTheLoop.ToString())
                        {
                            TaskController _taskController = new TaskController();
                            var _option = new AI.Entity.Option();
                            _option.id = "user_input";
                            _option.name = "user_input";
                            _option.value = Inputs.content;

                            result = _taskController.execHumanInTheLoop(sessionID, taskID, recordID, _option, out errorCode);
                        }
                        //用户输入内容作为本节点表单处理的内容，通过本节点的大模型按照预设表单项进行解析，然后驱动下一节点
                        if (humanTaskRecord?.NodeName == NodeType.HumanInTheLoopInput.ToString())
                        {
                            TaskInfo taskInfo = TaskInfoBussiness.GetModel(taskID);
                            taskInfo.FromTaskID = taskID;
                            taskInfo.TaskID = Guid.NewGuid().ToString("N");
                            taskInfo.ProcessesID = data.ProcessesID;

                            TaskConfig taskConfig = taskInfo.TaskConfig;
                            taskConfig.Data.ProcessesID = data.ProcessesID;

                            NodeConfig config = taskConfig.NodeConfig;

                            HumanInTheLoopInputData nodeData = System.Text.Json.JsonSerializer.Deserialize<HumanInTheLoopInputData>(config.data.ToString());

                            nodeData.userInputContent = nodeData.userInputContent?.Count > 0 ? nodeData.userInputContent : new List<string>();
                            nodeData.userInputContent.Add(Inputs.content);

                            nodeData.toExecSwitch = true;

                            config.data = nodeData;

                            taskInfo.State = TaskState.Waiting;

                            result = !TaskInfoBussiness.Add(taskInfo).IsNullOrEmpty();
                        }
                    }

                    WorkflowNodeExecutionRecordInfoBussiness.Update(humanTaskRecord);
                }
                if (result == false)
                {
                    break;
                }
            }

            return result;
        }
    }
}
