using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Senparc.CO2NET.Extensions;
namespace ZSN.AI.BLL
{
    public partial class TaskInfoBussiness
    {
        #region 基础信息
        private const string ConnectionName = "JobDb";
        #endregion

        #region tb_task_info
        /// <summary>
        /// 解析sourceId，如果包含子参数，则提取基础sourceId和jsonPath，支持过滤前缀
        /// </summary>
        public static (string baseSourceId, string jsonPath) ParseSourceId(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId)) return (sourceId, null);
            string jsonPath = null;
            // 先提取jsonPath
            var bracketMatch = Regex.Match(sourceId, @"^(.+?)\((.+)\)$");
            if (bracketMatch.Success)
            {
                sourceId = bracketMatch.Groups[1].Value;
                jsonPath = bracketMatch.Groups[2].Value;
            }
            // 过滤前缀：假设前缀是UUID_格式，找到第一个UUID后跟非UUID的部分
            var parts = sourceId.Split('_');
            int startIndex = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (Regex.IsMatch(parts[i], @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$"))
                {
                    // 如果下一个不是UUID，则从这里开始
                    if (i + 1 >= parts.Length || !Regex.IsMatch(parts[i + 1], @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$"))
                    {
                        startIndex = i;
                        break;
                    }
                }
            }
            string actualSourceId = string.Join("_", parts.Skip(startIndex));
            return (actualSourceId, jsonPath);
        }
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(TaskInfo model)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_Add(model);
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(TaskInfo model)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_Update(model);
        }

        /// <summary>
        /// 批量修改状态值
        /// </summary>
        /// <param name="TaskID"></param>
        /// <param name="ToState"></param>
        /// <returns></returns>
        public static bool SetState(List<string> TaskID, TaskState ToState)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_SetState(TaskID, ToState);
        }
        public static bool DeleteBySessionID(string SessionID)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_DeleteBySessionID(SessionID);
        }
        /// <summary>
        /// 删除一条数据
        /// </summary>
        public static bool Delete(string taskID)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_Delete(taskID);
        }
        public static bool DeleteByWhere(string where)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_DeleteByWhere(where);
        }
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string taskIDlist)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_DeleteList(taskIDlist);
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.TaskInfo GetModel(string taskID)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetModel(taskID);
        }
        public static ZSN.AI.Entity.TaskInfo GetModelByFromTaskID(string FromTaskID)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetModelByFromTaskID(FromTaskID);
        }
        public  static List<TaskInfo> GetList(NodeType nodeType, string WorkflowID)
        {
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList( nodeType,  WorkflowID).Tables[0]);
        }
        public static List<TaskInfo> GetList(int State, NodeType nodeType, DateTime StartTime, int ToState = 1, int length = 100)
        {
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(State, (int)nodeType, StartTime, ToState, length).Tables[0]);
        }
        public static List<TaskInfo> GetList(int State, List<NodeType> nodeType, DateTime StartTime, int ToState = 1, int length = 100)
        {
            string nodeTypeStr = string.Join(",", nodeType.Select(x => (int)x).ToList());
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(State, nodeTypeStr, StartTime, ToState, length).Tables[0]);
        }
        public static List<TaskInfo> GetListBySessionID(string SessionID)
        {
            string strWhere = $" SessionID='{SessionID}' ";
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(strWhere).Tables[0]);
        }
        public static List<TaskInfo> GetListBySessionIDProcessesID(string SessionID,string ProcessesID)
        {
            string strWhere = $" SessionID='{SessionID}' and ProcessesID LIKE '{ProcessesID}%' ";
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(strWhere).Tables[0]);
        }
        public static List<TaskInfo> GetListByWorkflowID(string WorkflowID)
        {
            string strWhere = $" WorkflowID='{WorkflowID}'";
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(strWhere).Tables[0]);
        }
        public static List<TaskInfo> GetListByFromTaskID(string FromTaskID)
        {
            string strWhere = $" FromTaskID='{FromTaskID}'";
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(strWhere).Tables[0]);
        }
        public static List<TaskInfo> GetListByFromMainTaskID(string FromMainTaskID)
        {
            string strWhere = $" FromMainTaskID='{FromMainTaskID}'";
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<TaskInfo> GetList(string strWhere = "")
        {
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<TaskInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<TaskInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        /// <param name="pageSize">每页大小</param>
        /// <param name="pageIndex">页标</param>
        /// <param name="strWhere">查询条件</param>
        /// <param name="pagetotal">总页数</param>
        /// <param name="total">总数</param>
        /// <param name="orderType">排序规则， 默认降序，1降序，0升序</param>
        /// <param name="showName">显示字段，默认全部</param>
        /// <param name="orderKey">排序key，默认主键</param>
        /// <returns></returns>
        public static List<TaskInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "TaskID")
        {
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
        private static List<TaskInfo> TaskInfoDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<TaskInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_DataRowToModel(r));
            }
            return list;
        }
        #endregion

        /// <summary>
        /// 创建节点异步任务
        /// </summary>
        /// <param name="SourceNode">上一节点</param>
        /// <param name="CurrentNode">当前节点</param>
        /// <param name="outputs"></param>
        /// <param name="AppID"></param>
        /// <param name="SessionID"></param>
        /// <param name="ProcessesID">新任务标识</param>
        /// <param name="FromTaskID">源标识</param>
        /// <param name="AgentNodeID"></param>
        /// <returns></returns>
        public static string toTask(NodeConfig SourceNode, List<Output> outputs, NodeConfig CurrentNode, string AppID, string SessionID, string ProcessesID, string FromTaskID = "", string FromMainTaskID = "", string AgentNodeID = "",string WorkflowID="")
        {
            string TaskID = Guid.NewGuid().ToString();

            NodeType nodeType = CurrentNode.type;


            NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(JsonConvert.SerializeObject(CurrentNode.data));

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
            List<Inputs> inputs = nodeType switch
            {
                NodeType.Start => JsonConvert.DeserializeObject<StartData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.AgentStart => JsonConvert.DeserializeObject<AgentStartData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.LargeModel => JsonConvert.DeserializeObject<LargeModelData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.MainAI => JsonConvert.DeserializeObject<MainAIData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.KnowledgeBase => JsonConvert.DeserializeObject<KnowledgeBaseData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.Selector => JsonConvert.DeserializeObject<SelectorData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.Agent => JsonConvert.DeserializeObject<AgentData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.Plugins => JsonConvert.DeserializeObject<PluginsData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.End => JsonConvert.DeserializeObject<EndData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.AgentEnd => JsonConvert.DeserializeObject<AgentEndData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.Merge => JsonConvert.DeserializeObject<MergeData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.MCP => JsonConvert.DeserializeObject<MCPData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.FileToMarkdown => JsonConvert.DeserializeObject<FileToMarkdownData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.HumanInTheLoop => JsonConvert.DeserializeObject<HumanInTheLoopData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.SkillAgent => JsonConvert.DeserializeObject<SkillAgentData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.ImageGeneration => JsonConvert.DeserializeObject<ImageGenerationData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.VideoGeneration => JsonConvert.DeserializeObject<VideoGenerationData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.ClawAI => JsonConvert.DeserializeObject<ClawAIData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.ClawAIWorkflowStep => JsonConvert.DeserializeObject<ClawAIData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,
                NodeType.ServiceDesk => JsonConvert.DeserializeObject<ServiceDeskData>(JsonConvert.SerializeObject(nodeConfig.data))?.inputs,

                _ => new List<Inputs>()
            };
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。


            //上节点的输出，匹配当前节点的输入,SourceNode.type = NodeType.Agent,时只需要匹配varname
            // 如果匹配失败，将尝试从历史执行记录中获取值
            var updatedInputsList = inputs.GroupJoin(
                outputs,
                input => {
                    var (baseSourceId, _) = ParseSourceId(input.sourceId.IsNullOrEmpty() ? input.varname : input.sourceId);
                    return SourceNode != null //&& SourceNode.type != NodeType.Agent 
                        ? baseSourceId : input.varname;
                },
                
                output => {
                    // （MergeNode 输出特有）
                    if (!string.IsNullOrEmpty(output.sourceId))
                    {
                        var (baseSourceId, _) = ParseSourceId(output.sourceId);
                        return baseSourceId;
                    }
                    // 标准节点输出
                    return SourceNode != null //&& SourceNode.type != NodeType.Agent
                        ? $"{SourceNode.id}_{output.varname}" 
                        : output.varname;
                },
                (input, matchingOutputs) => {
                    var output = matchingOutputs.FirstOrDefault();
                    var value = output?.value ?? input.value;
                    var (_, jsonPath) = ParseSourceId(input.sourceId);
                    if (jsonPath != null && output != null)
                    {
                        try
                        {
                            var jToken = JToken.Parse(value);
                            var extracted = jToken.SelectToken(jsonPath);
                            value = extracted?.ToString() ?? value;
                        }
                        catch
                        {
                            // 如果解析失败，保留原值
                        }
                    }
                    return new Inputs
                    {
                        sourceId = input.sourceId,
                        varname = input.varname,
                        value = value,
                        type = input.type,
                        txt = input.txt
                    };
                }
            ).ToList();

            // 处理未匹配的输入：当sourceId不在当前outputs中时，从SessionID下的历史执行记录获取值
            var matchedSourceIds = new HashSet<string>(
                updatedInputsList.Where(ui => !string.IsNullOrEmpty(ui.sourceId) && 
                    inputs.Any(i => i.sourceId == ui.sourceId && i.value != ui.value))
                .Select(ui => ui.sourceId)
            );
            var unmatchedInputs = inputs.Where(i => !string.IsNullOrEmpty(i.sourceId) && !matchedSourceIds.Contains(i.sourceId)).ToList();
            if (unmatchedInputs.Any())
            {
                // 收集需要查询的节点ID，从过滤后的baseSourceId提取
                var nodeIDList = unmatchedInputs.Select(i => {
                    var (baseSourceId, _) = ParseSourceId(i.sourceId);
                    return baseSourceId.Split('_')[0];
                }).Distinct().ToList();
                try
                {
                    var workflowNodeExcutionRecords = !string.IsNullOrEmpty(ProcessesID)
                        ? WorkflowNodeExecutionRecordInfoBussiness.GetListByNodeId(SessionID, nodeIDList, ProcessesID)
                        : WorkflowNodeExecutionRecordInfoBussiness.GetListByNodeId(SessionID, nodeIDList);
                    var outputsDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var record in workflowNodeExcutionRecords)
                    {
                        if (record.Outputs == null) continue;
                        List<Output> recordOutputs = null;
                        try
                        {
                            recordOutputs = JsonConvert.DeserializeObject<List<Output>>(record.Outputs.ToString());
                        }
                        catch
                        {
                            recordOutputs = null;
                        }
                        if (recordOutputs == null) continue;
                        foreach (var output in recordOutputs)
                        {
                            if (string.IsNullOrEmpty(output?.sourceId)) continue;
                            outputsDict[output.sourceId] = output.value ?? string.Empty;
                        }
                    }
                    // 更新unmatchedInputs的值，支持JSONPath提取
                    foreach (var input in unmatchedInputs)
                    {
                        var (baseSourceId, jsonPath) = ParseSourceId(input.sourceId);
                        string rawValue = null;
                        if (outputsDict.TryGetValue(input.sourceId, out var v))
                        {
                            rawValue = v;
                        }
                        else if (outputsDict.TryGetValue(baseSourceId, out var v2))
                        {
                            rawValue = v2;
                        }
                        if (rawValue != null)
                        {
                            if (jsonPath != null)
                            {
                                try
                                {
                                    var jToken = JToken.Parse(rawValue);
                                    var extracted = jToken.SelectToken(jsonPath);
                                    rawValue = extracted?.ToString() ?? rawValue;
                                }
                                catch
                                {
                                    // JSONPath解析失败，保留原值
                                }
                            }
                            // 更新updatedInputsList中的值
                            var updatedInput = updatedInputsList.FirstOrDefault(ui => ui.sourceId == input.sourceId);
                            if (updatedInput != null)
                            {
                                updatedInput.value = rawValue;
                            }
                        }
                    }
                }
                catch
                {
                    // 容错：获取历史输出失败，保留原值
                }
            }

            // 添加未匹配的 outputs 到 inputs 列表
            var matchedKeys = new HashSet<string>(
                inputs.Select(input => {
                    var (baseSourceId, _) = ParseSourceId(input.sourceId.IsNullOrEmpty() ? input.varname : input.sourceId);
                    return baseSourceId;
                })
            );

            var unmatchedOutputs = outputs.Where(output =>
            {
                string matchKey = !string.IsNullOrEmpty(output.sourceId)
                    ? ParseSourceId(output.sourceId).baseSourceId
                    : (SourceNode != null ? $"{SourceNode.id}_{output.varname}" : $"{output.varname}");

                return !matchedKeys.Contains(matchKey);
            });

            updatedInputsList.AddRange(unmatchedOutputs.Select(output => new Inputs
            {
                sourceId = !string.IsNullOrEmpty(output.sourceId) 
                    ? output.sourceId 
                    : (SourceNode != null ? $"{SourceNode.id}_{output.varname}" : $"{output.varname}"),
                varname = output.varname,
                value = output.value,
                type = output.type,
                txt = output.txt
            }));

            nodeConfig.fromNodeType = SourceNode.type;
            nodeConfig.fromNodeId = SourceNode.id;

            TaskInfo taskInfo = new TaskInfo();
            taskInfo.TaskID = TaskID;
            taskInfo.TaskType = nodeType;
            taskInfo.TaskConfig = new TaskConfig();
            taskInfo.TaskConfig.NodeConfig = nodeConfig;
            taskInfo.TaskConfig.Data = new TaskData() { AppID = AppID, SessionID = SessionID, TaskID = TaskID, FromMainTaskID = FromMainTaskID, ProcessesID = ProcessesID, AgentNodeID = AgentNodeID, Inputs = updatedInputsList };
            taskInfo.LoopType = LoopType.NOLoop;
            taskInfo.RepeatValue = 1;
            taskInfo.RedoCount = 0;
            taskInfo.CreateTime = DateTime.Now;
            taskInfo.UpdateTime = DateTime.Now;
            taskInfo.FromTaskID = FromTaskID;
            taskInfo.FromMainTaskID = FromMainTaskID;
            taskInfo.WorkflowID = WorkflowID;
            taskInfo.SessionID = SessionID;
            taskInfo.ProcessesID = ProcessesID;
            taskInfo.State = TaskState.Waiting;

            TaskInfoBussiness.Add(taskInfo);

            TaskID = taskInfo.TaskID;

            return TaskID;
        }

        public static bool updateTask(string taskID, TaskState state, Results results)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_Update(taskID, state, results);
        }

        /// <summary>
        /// 将任务状态从Processing回退到Waiting
        /// 用于Redis入队失败时的恢复机制
        /// </summary>
        public static void ResetTasksToWaiting(List<string> taskIds)
        {
            if (taskIds == null || taskIds.Count == 0) return;
            
            try
            {
                foreach (var taskId in taskIds)
                {
                    var task = GetModel(taskId);
                    if (task != null && task.State == TaskState.Processing)
                    {
                        task.State = TaskState.Waiting;
                        task.UpdateTime = DateTime.Now;
                        Update(task);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
