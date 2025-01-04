using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using Newtonsoft.Json;
using Senparc.CO2NET.Extensions;
namespace ZSN.AI.BLL
{
    public partial class TaskInfoBussiness
    {
        #region ������Ϣ
        private const string ConnectionName = "JobDb";
        #endregion
        #region tb_task_info
        /// <summary>
        /// ����һ������
        /// </summary>
        public static string Add(TaskInfo model)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_Add(model);
        }
        /// <summary>
        /// ����һ������
        /// </summary>
        public static bool Update(TaskInfo model)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_Update(model);
        }

        /// <summary>
        /// �����޸�״ֵ̬
        /// </summary>
        /// <param name="TaskID"></param>
        /// <param name="ToState"></param>
        /// <returns></returns>
        public static bool SetState(List<string> TaskID, int ToState)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_SetState(TaskID, ToState);
        }
        /// <summary>
        /// ɾ��һ������
        /// </summary>
        public static bool Delete(string taskID)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_Delete(taskID);
        }
        /// <summary>
        /// ����ɾ������
        /// </summary>
		public static bool DeleteList(string taskIDlist)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_DeleteList(taskIDlist);
        }
        /// <summary>
        /// �õ�һ������ʵ��
        /// </summary>
		public static ZSN.AI.Entity.TaskInfo GetModel(string taskID)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetModel(taskID);
        }
        public static ZSN.AI.Entity.TaskInfo GetModelByFromTaskID(string FromTaskID)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetModelByFromTaskID(FromTaskID);
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
        /// ��������б�
        /// </summary>
        public static List<TaskInfo> GetList(string strWhere = "")
        {
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// ���ǰ��������
        /// </summary>
		public static List<TaskInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// ��ȡ��¼����
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// ��ҳ��ȡ�����б�
        /// </summary>
		public static List<TaskInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return TaskInfoDataSet_ToList(DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }
        /// <summary>
        /// ��ҳ��ȡ�����б�
        /// </summary>
        /// <param name="pageSize">ÿҳ��С</param>
        /// <param name="pageIndex">ҳ��</param>
        /// <param name="strWhere">��ѯ����</param>
        /// <param name="pagetotal">��ҳ��</param>
        /// <param name="total">����</param>
        /// <param name="orderType">������� Ĭ�Ͻ���1����0����</param>
        /// <param name="showName">��ʾ�ֶΣ�Ĭ��ȫ��</param>
        /// <param name="orderKey">����key��Ĭ������</param>
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
        /// �����ڵ��첽����
        /// </summary>
        /// <param name="SourceNode">��һ�ڵ�</param>
        /// <param name="CurrentNode">��ǰ�ڵ�</param>
        /// <param name="outputs"></param>
        /// <param name="AppID"></param>
        /// <param name="SessionID"></param>
        /// <param name="ProcessesID">�������ʶ</param>
        /// <param name="FromTaskID">Դ��ʶ</param>
        /// <param name="AgentNodeID"></param>
        /// <returns></returns>
        public static string toTask(NodeConfig SourceNode, List<Output> outputs, NodeConfig CurrentNode, string AppID, string SessionID, string ProcessesID, string FromTaskID = "", string FromMainTaskID = "", string AgentNodeID = "")
        {
            string TaskID = Guid.NewGuid().ToString();

            NodeType nodeType = CurrentNode.type;


            NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(CurrentNode.data.ToString());

#pragma warning disable CS8600 // �� null �����������Ϊ null ��ֵת��Ϊ�� null ���͡�
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
                _ => new List<Inputs>()
            };
#pragma warning restore CS8600 // �� null �����������Ϊ null ��ֵת��Ϊ�� null ���͡�



            //�Ͻڵ�������ƥ�䵱ǰ�ڵ������,SourceNode.type = NodeType.Agent,ʱֻ��Ҫƥ��varname
            var updatedInputsList = inputs.GroupJoin(
                outputs,
                input => SourceNode != null && SourceNode.type != NodeType.Agent ? input.sourceId.IsNullOrEmpty() ? input.varname : input.sourceId : input.varname ,
                output => SourceNode != null && SourceNode.type != NodeType.Agent ? $"{SourceNode.id}_{output.varname}" : output.varname,
                (input, matchingOutputs) => new Inputs
                {
                    sourceId = input.sourceId,
                    varname = input.varname,
                    value = matchingOutputs.FirstOrDefault()?.value ?? input.value, // ƥ������� value��������ԭʼֵ
                    type = input.type,
                    txt = input.txt
                }
            ).ToList();

            // ���δƥ��� outputs �� inputs �б�
            var matchedKeys = new HashSet<string>(
                inputs.Select(input => input.sourceId.IsNullOrEmpty() ? input.varname : input.sourceId)
            );

            var unmatchedOutputs = outputs.Where(output =>
                !matchedKeys.Contains(SourceNode != null
                    ? $"{SourceNode.id}_{output.varname}"
                    : $"{output.varname}")
            );

            updatedInputsList.AddRange(unmatchedOutputs.Select(output => new Inputs
            {
                sourceId = SourceNode != null ? $"{SourceNode.id}_{output.varname}" : $"{output.varname}",
                varname = output.varname,
                value = output.value,
                type = output.type,
                txt = output.txt
            }));

            nodeConfig.fromNodeType = SourceNode.type;

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

            TaskInfoBussiness.Add(taskInfo);

            TaskID = taskInfo.TaskID;

            return TaskID;
        }

        public static bool updateTask(string taskID, TaskState state, Results results)
        {
            return DatabaseProvider.GetTaskInfo(ConnectionName).TaskInfo_Update(taskID, state, results);
        }
    }
}
