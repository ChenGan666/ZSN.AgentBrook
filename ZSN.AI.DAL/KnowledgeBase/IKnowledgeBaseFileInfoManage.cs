using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IKnowledgeBaseFileInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_knowledge_base_file_info
        string KnowledgeBaseFileInfo_Add(KnowledgeBaseFileInfo model);
        bool KnowledgeBaseFileInfo_Update(KnowledgeBaseFileInfo model);
        bool KnowledgeBaseFileInfo_Delete(string fileID); 
        bool KnowledgeBaseFileInfo_DeleteList(string fileIDlist);
        KnowledgeBaseFileInfo KnowledgeBaseFileInfo_DataRowToModel(DataRow row);
        KnowledgeBaseFileInfo KnowledgeBaseFileInfo_GetModel(string fileID); 
        DataSet KnowledgeBaseFileInfo_GetList(string strWhere);
        DataSet KnowledgeBaseFileInfo_GetList(int top, string strWhere, string filedOrder);
        int KnowledgeBaseFileInfo_GetRecordCount(string strWhere);
        DataSet KnowledgeBaseFileInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable KnowledgeBaseFileInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
