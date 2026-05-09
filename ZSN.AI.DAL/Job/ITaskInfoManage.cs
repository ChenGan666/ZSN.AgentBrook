using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface ITaskInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_task_info
        string TaskInfo_Add(TaskInfo model);
        bool TaskInfo_Update(TaskInfo model);
        bool TaskInfo_Update(string taskID, TaskState state, Results results);
        bool TaskInfo_SetState(List<string> TaskID, TaskState ToState);
        bool TaskInfo_DeleteBySessionID(string SessionID);
        bool TaskInfo_Delete(string taskID);
        bool TaskInfo_DeleteByWhere(string where);
        bool TaskInfo_DeleteList(string taskIDlist);
        TaskInfo TaskInfo_DataRowToModel(DataRow row);
        TaskInfo TaskInfo_GetModel(string taskID);
        TaskInfo TaskInfo_GetModelByFromTaskID(string FromTaskID);
        DataSet TaskInfo_GetList(string strWhere);
        DataSet TaskInfo_GetList(NodeType nodeType, string WorkflowID);
        DataSet TaskInfo_GetList(int State, int taskType, DateTime StartTime, int ToState, int length);
        DataSet TaskInfo_GetList(int State, string taskTypeStr, DateTime StartTime, int ToState, int length);
        DataSet TaskInfo_GetList(int top, string strWhere, string filedOrder);
        int TaskInfo_GetRecordCount(string strWhere);
        DataSet TaskInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable TaskInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
