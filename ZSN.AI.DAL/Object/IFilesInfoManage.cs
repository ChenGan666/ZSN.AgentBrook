using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IFilesInfoManage
    {
        string SetConnectionName(string connName);
        #region tbFilesInfo
        int FilesInfo_Add(FilesInfo model);
        bool FilesInfo_Update(FilesInfo model);
        bool FilesInfo_Delete(string FilesCode); 
        bool FilesInfo_DeleteList(string FilesCodelist);
        FilesInfo FilesInfo_DataRowToModel(DataRow row);
        FilesInfo FilesInfo_GetModel(string FilesCode); 
        DataSet FilesInfo_GetList(string strWhere);
        DataSet FilesInfo_GetList(int top, string strWhere, string filedOrder);
        int FilesInfo_GetRecordCount(string strWhere);
        DataSet FilesInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable FilesInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
