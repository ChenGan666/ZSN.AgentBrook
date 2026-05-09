using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IKnowledgeBaseInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_knowledge_base_info
        string KnowledgeBaseInfo_Add(KnowledgeBaseInfo model);
        bool KnowledgeBaseInfo_Update(KnowledgeBaseInfo model);
        bool KnowledgeBaseInfo_Delete(string knowledgeBaseID); 
        bool KnowledgeBaseInfo_DeleteList(string knowledgeBaseIDlist);
        KnowledgeBaseInfo KnowledgeBaseInfo_DataRowToModel(DataRow row);
        KnowledgeBaseInfo KnowledgeBaseInfo_GetModel(string knowledgeBaseID); 
        DataSet KnowledgeBaseInfo_GetList(string strWhere);
        DataSet KnowledgeBaseInfo_GetList(int top, string strWhere, string filedOrder);
        int KnowledgeBaseInfo_GetRecordCount(string strWhere);
        DataSet KnowledgeBaseInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable KnowledgeBaseInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
