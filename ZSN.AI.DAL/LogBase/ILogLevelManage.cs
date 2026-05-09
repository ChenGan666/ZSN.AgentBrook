using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface ILogLevelManage
    {
        #region log_level
        int LogLevel_Add(LogLevel model);
        bool LogLevel_Update(LogLevel model);
        bool LogLevel_Delete(Int32 id); 
        bool LogLevel_DeleteList(string idlist);
        LogLevel LogLevel_DataRowToModel(DataRow row);
        LogLevel LogLevel_GetModel(Int32 id); 
        DataSet LogLevel_GetList(string strWhere);
        DataSet LogLevel_GetList(int top, string strWhere, string filedOrder);
        int LogLevel_GetRecordCount(string strWhere);
        DataSet LogLevel_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable LogLevel_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
