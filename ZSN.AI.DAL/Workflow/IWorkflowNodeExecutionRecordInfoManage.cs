using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IWorkflowNodeExecutionRecordInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_workflow_node_excution_record_info
        string WorkflowNodeExecutionRecordInfo_Add(WorkflowNodeExecutionRecordInfo model);
        bool WorkflowNodeExecutionRecordInfo_Update(string RecordID, ExecutionRecordStatus Status, object Outputs, object Logs);
        bool WorkflowNodeExecutionRecordInfo_UpdateLogs(string RecordID, object Outputs, object Logs);
        bool WorkflowNodeExecutionRecordInfo_Update(WorkflowNodeExecutionRecordInfo model);
        bool DeleteByNodeID(string SessionID, string NodeID);
        bool WorkflowNodeExecutionRecordInfo_Delete(string recordID);
        bool WorkflowNodeExecutionRecordInfo_DeleteByWhere(string where);
        bool WorkflowNodeExecutionRecordInfo_DeleteList(string recordIDlist);
        WorkflowNodeExecutionRecordInfo WorkflowNodeExecutionRecordInfo_DataRowToModel(DataRow row);
        WorkflowNodeExecutionRecordInfo WorkflowNodeExecutionRecordInfo_GetModel(string recordID);

        WorkflowNodeExecutionRecordInfo WorkflowNodeExecutionRecordInfo_GetModelByTaskID(string testID, string sessionID);
        DataSet WorkflowNodeExecutionRecordInfo_GetList(string strWhere);
        DataSet WorkflowNodeExecutionRecordInfo_GetList(int top, string strWhere, string filedOrder);
        DataSet WorkflowNodeExecutionRecordInfo_GetListByNodeId(string SessionID, string NodeI);
        DataSet WorkflowNodeExecutionRecordInfo_GetListByNodeId(string SessionID, string NodeID, ExecutionRecordStatus recordStatus);
        DataSet WorkflowNodeExecutionRecordInfo_GetListByNodeId(string SessionID, string NodeID, string ProcessesID);
        DataSet WorkflowNodeExecutionRecordInfo_GetListByNodeId(string SessionID, string NodeID, ExecutionRecordStatus recordStatus, string ProcessesID);

        int WorkflowNodeExecutionRecordInfo_GetRecordCount(string strWhere);
        DataSet WorkflowNodeExecutionRecordInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable WorkflowNodeExecutionRecordInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
