using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IAgentInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_app_info
        string AgentInfo_Add(AgentInfo model);
        bool AgentInfo_Update(AgentInfo model);
        bool AgentInfo_Delete(string AgentID); 
        bool AgentInfo_DeleteList(string AgentIDlist);
        AgentInfo AgentInfo_DataRowToModel(DataRow row);
        AgentInfo AgentInfo_GetModel(string AgentID); 
        DataSet AgentInfo_GetList(string strWhere);
        DataSet AgentInfo_GetList(int top, string strWhere, string filedOrder);
        int AgentInfo_GetRecordCount(string strWhere);
        DataSet AgentInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable AgentInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
