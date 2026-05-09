using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface ILogMarkManage
    {
        #region log_mark
        int LogMark_Add(LogMark model);
        bool LogMark_Update(LogMark model);
        bool LogMark_Delete(Int32 id); 
        bool LogMark_DeleteList(string idlist);
        LogMark LogMark_DataRowToModel(DataRow row);
        LogMark LogMark_GetModel(Int32 id); 
        DataSet LogMark_GetList(string strWhere);
        DataSet LogMark_GetList(int top, string strWhere, string filedOrder);
        int LogMark_GetRecordCount(string strWhere);
        DataSet LogMark_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable LogMark_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
