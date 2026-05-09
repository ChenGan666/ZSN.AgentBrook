using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IAppChatLogInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_app_chat_log_info
        string AppChatLogInfo_Add(AppChatLogInfo model);
        bool AppChatLogInfo_Update(AppChatLogInfo model);
        bool AppChatLogInfo_Delete(string chatLogID); 
        bool AppChatLogInfo_DeleteList(string chatLogIDlist);
        AppChatLogInfo AppChatLogInfo_DataRowToModel(DataRow row);
        AppChatLogInfo AppChatLogInfo_GetModel(string chatLogID); 
        DataSet AppChatLogInfo_GetList(string strWhere);
        DataSet AppChatLogInfo_GetList(string strWhere,string SessionID,string MemberID);
        DataSet AppChatLogInfo_GetListBySessionID(string AppID,string SessionID);
        DataSet AppChatLogInfo_GetList(int top, string strWhere, string filedOrder);
        int AppChatLogInfo_GetRecordCount(string strWhere);
        DataSet AppChatLogInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable AppChatLogInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
