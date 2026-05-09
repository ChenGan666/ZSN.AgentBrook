using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IWorkflowNodeInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_workflow_node_info
        string WorkflowNodeInfo_Add(WorkflowNodeInfo model);
        bool WorkflowNodeInfo_Update(WorkflowNodeInfo model);
        bool WorkflowNodeInfo_Delete(string nodeID); 
        bool WorkflowNodeInfo_DeleteList(string nodeIDlist);
        bool WorkflowNodeInfo_DeleteAbsentList(string nodeIDlist, string WorkflowID);
        bool WorkflowNodeInfo_DeleteByWorkflowID(string WorkflowID);
        WorkflowNodeInfo WorkflowNodeInfo_DataRowToModel(DataRow row);
        WorkflowNodeInfo WorkflowNodeInfo_GetModel(string nodeID); 
        DataSet WorkflowNodeInfo_GetList(string strWhere);
        DataSet GetAllNextNodeListByNodeID(string NodeID);
        DataSet WorkflowNodeInfo_GetList(int top, string strWhere, string filedOrder);
        int WorkflowNodeInfo_GetRecordCount(string strWhere);
        DataSet WorkflowNodeInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable WorkflowNodeInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
