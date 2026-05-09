using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IKnowledgeBaseTagInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_knowledge_base_tag_info
        int KnowledgeBaseTagInfo_Add(KnowledgeBaseTagInfo model);
        bool KnowledgeBaseTagInfo_Update(KnowledgeBaseTagInfo model);
        bool KnowledgeBaseTagInfo_Delete(Int32 knowledgeBaseTagID); 
        bool KnowledgeBaseTagInfo_DeleteList(string knowledgeBaseTagIDlist);
        KnowledgeBaseTagInfo KnowledgeBaseTagInfo_DataRowToModel(DataRow row);
        KnowledgeBaseTagInfo KnowledgeBaseTagInfo_GetModel(Int32 knowledgeBaseTagID);
        KnowledgeBaseTagInfo KnowledgeBaseTagInfo_GetModel(string Tag);
        DataSet KnowledgeBaseTagInfo_GetList(string strWhere);
        DataSet KnowledgeBaseTagInfo_GetList(int top, string strWhere, string filedOrder);
        int KnowledgeBaseTagInfo_GetRecordCount(string strWhere);
        DataSet KnowledgeBaseTagInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable KnowledgeBaseTagInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
