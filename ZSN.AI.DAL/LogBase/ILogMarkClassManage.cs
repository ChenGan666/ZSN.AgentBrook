using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface ILogMarkClassManage
    {
        #region log_mark_class
        int LogMarkClass_Add(LogMarkClass model);
        bool LogMarkClass_Update(LogMarkClass model);
        bool LogMarkClass_Delete(Int32 id); 
        bool LogMarkClass_DeleteList(string idlist);
        LogMarkClass LogMarkClass_DataRowToModel(DataRow row);
        LogMarkClass LogMarkClass_GetModel(Int32 id); 
        DataSet LogMarkClass_GetList(string strWhere);
        DataSet LogMarkClass_GetList(int top, string strWhere, string filedOrder);
        int LogMarkClass_GetRecordCount(string strWhere);
        DataSet LogMarkClass_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable LogMarkClass_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
