using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IAppChatSummaryInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_app_chat_summary_info
        string AppChatSummaryInfo_Add(AppChatSummaryInfo model);
        bool AppChatSummaryInfo_Update(AppChatSummaryInfo model);
        bool AppChatSummaryInfo_Delete(string summaryID); 
        bool AppChatSummaryInfo_DeleteList(string summaryIDlist);
        AppChatSummaryInfo AppChatSummaryInfo_DataRowToModel(DataRow row);
        AppChatSummaryInfo AppChatSummaryInfo_GetModel(string summaryID);
        DataSet AppChatSummaryInfo_GetListBySessionID(string AppID, string SessionID);
        DataSet AppChatSummaryInfo_GetList(string strWhere);
        DataSet AppChatSummaryInfo_GetList(string strWhere,string SessionID,string MemberID);
        DataSet AppChatSummaryInfo_GetList(int top, string strWhere, string filedOrder);
        int AppChatSummaryInfo_GetRecordCount(string strWhere);
        DataSet AppChatSummaryInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable AppChatSummaryInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
