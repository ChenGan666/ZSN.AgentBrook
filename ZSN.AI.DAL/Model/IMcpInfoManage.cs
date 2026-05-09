using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IMcpInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_mcp_info
        string McpInfo_Add(McpInfo model);
        bool McpInfo_Update(McpInfo model);
        bool McpInfo_Delete(string mCPID); 
        bool McpInfo_DeleteList(string mCPIDlist);
        McpInfo McpInfo_DataRowToModel(DataRow row);
        McpInfo McpInfo_GetModel(string mCPID); 
        DataSet McpInfo_GetList(string strWhere);
        DataSet McpInfo_GetList(int top, string strWhere, string filedOrder);
        int McpInfo_GetRecordCount(string strWhere);
        DataSet McpInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable McpInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
