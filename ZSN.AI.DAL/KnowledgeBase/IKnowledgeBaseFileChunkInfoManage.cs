using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IKnowledgeBaseFileChunkInfoManage
    {
        string SetConnectionName(string connName);
        bool KnowledgeBaseFileChunkInfo_Delete(string FileID,string KnowledgeBaseID);
        KnowledgeBaseFileChunkInfo KnowledgeBaseFileChunkInfo_DataRowToModel(DataRow row);
        DataTable KnowledgeBaseFileChunkInfo_GetListByPage(string KnowledgeBaseID, int size, int index, string where, out int pagetotal, out int total);
    }
}
