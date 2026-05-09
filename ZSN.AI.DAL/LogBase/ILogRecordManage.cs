using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface ILogRecordManage
    {
        #region log_record
        int LogRecord_Add(LogRecord model);
        bool LogRecord_Update(LogRecord model);
        bool LogRecord_Delete(Int64 id);
        bool LogRecord_DeleteByWhere(string where);
        bool LogRecord_DeleteList(string idlist);
        LogRecord LogRecord_DataRowToModel(DataRow row);
        LogRecord LogRecord_GetModel(Int64 id); 
        DataSet LogRecord_GetList(string strWhere);
        DataSet LogRecord_GetList(int top, string strWhere, string filedOrder);
        int LogRecord_GetRecordCount(string strWhere);
        DataSet LogRecord_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable LogRecord_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
