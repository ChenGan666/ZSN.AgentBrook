using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IAppInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_app_info
        string AppInfo_Add(AppInfo model);
        bool AppInfo_Update(AppInfo model);
        bool AppInfo_Delete(string appID); 
        bool AppInfo_DeleteList(string appIDlist);
        AppInfo AppInfo_DataRowToModel(DataRow row);
        AppInfo AppInfo_GetModel(string appID); 
        DataSet AppInfo_GetList(string strWhere);
        DataSet AppInfo_GetList(int top, string strWhere, string filedOrder);
        int AppInfo_GetRecordCount(string strWhere);
        DataSet AppInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable AppInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
