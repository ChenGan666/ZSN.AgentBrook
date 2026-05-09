using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IWorkflowEdgeInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_workflow_edge_info
        string WorkflowEdgeInfo_Add(WorkflowEdgeInfo model);
        bool WorkflowEdgeInfo_Update(WorkflowEdgeInfo model);
        bool WorkflowEdgeInfo_Delete(string edgeID); 
        bool WorkflowEdgeInfo_DeleteList(string edgeIDlist);
        bool WorkflowEdgeInfo_DeleteAbsentList(string edgeIDlist, string WorkflowID);
        bool WorkflowEdgeInfo_DeleteByWorkflowID( string WorkflowID);
        WorkflowEdgeInfo WorkflowEdgeInfo_DataRowToModel(DataRow row);
        WorkflowEdgeInfo WorkflowEdgeInfo_GetModel(string edgeID);
        DataSet WorkflowEdgeInfo_GetListBySourceNodeId(string NodeId);
        DataSet WorkflowEdgeInfo_GetListByTargetNodeId(string NodeId);
        DataSet WorkflowEdgeInfo_GetList(string strWhere);
        DataSet WorkflowEdgeInfo_GetList(int top, string strWhere, string filedOrder);
        int WorkflowEdgeInfo_GetRecordCount(string strWhere);
        DataSet WorkflowEdgeInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable WorkflowEdgeInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
