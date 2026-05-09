using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL.Agent
{
    public partial interface IAgentKnowledgeBaseInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_app_knowledge_base_info
        int AgentKnowledgeBaseInfo_Add(AgentKnowledgeBaseInfo model);
        bool AgentKnowledgeBaseInfo_Update(AgentKnowledgeBaseInfo model);
        bool AgentKnowledgeBaseInfo_Delete(int AgentKnowledgeBaseID);
        bool AgentKnowledgeBaseInfo_DeleteList(string AgentKnowledgeBaseIDlist);
        AgentKnowledgeBaseInfo AgentKnowledgeBaseInfo_DataRowToModel(DataRow row);
        AgentKnowledgeBaseInfo AgentKnowledgeBaseInfo_GetModel(int AgentKnowledgeBaseID);
        DataSet AgentKnowledgeBaseInfo_GetList(string strWhere);
        DataSet AgentKnowledgeBaseInfo_GetList(int top, string strWhere, string filedOrder);
        int AgentKnowledgeBaseInfo_GetRecordCount(string strWhere);
        DataSet AgentKnowledgeBaseInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable AgentKnowledgeBaseInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
