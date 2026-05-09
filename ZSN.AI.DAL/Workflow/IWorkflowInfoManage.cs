using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IWorkflowInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_workflow_info
        //string Save(WorkFlow model);
        string WorkflowInfo_Add(WorkflowInfo model);
        bool WorkflowInfo_Update(WorkflowInfo model);
        bool WorkflowInfo_Delete(string workflowID); 
        bool WorkflowInfo_DeleteList(string workflowIDlist);
        WorkflowInfo WorkflowInfo_DataRowToModel(DataRow row);
        WorkflowInfo WorkflowInfo_GetModel(string workflowID);
        WorkflowInfo WorkflowInfo_GetModelByMainID(string MainID, int MainType);
        DataSet WorkflowInfo_GetList(string strWhere);
        DataSet WorkflowInfo_GetList(int top, string strWhere, string filedOrder);
        int WorkflowInfo_GetRecordCount(string strWhere);
        DataSet WorkflowInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable WorkflowInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
