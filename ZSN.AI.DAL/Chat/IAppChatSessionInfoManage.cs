using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IAppChatSessionInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_app_chat_session_info
        string AppChatSessionInfo_Add(AppChatSessionInfo model);
        bool AppChatSessionInfo_Update(AppChatSessionInfo model);
        bool AppChatSessionInfo_Delete(string chatSessionID);
        bool AppChatSessionInfo_Delete(string chatSessionID,string MemberID);
        bool AppChatSessionInfo_CleanUp(string MemberID);
        bool AppChatSessionInfo_DeleteList(string chatSessionIDlist);
        AppChatSessionInfo AppChatSessionInfo_DataRowToModel(DataRow row);
        AppChatSessionInfo AppChatSessionInfo_GetModel(string chatSessionID); 
        DataSet AppChatSessionInfo_GetList(string strWhere);
        DataSet AppChatSessionInfo_GetList(int top, string strWhere, string filedOrder);
        int AppChatSessionInfo_GetRecordCount(string strWhere);
        DataSet AppChatSessionInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable AppChatSessionInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        bool AppChatSessionInfo_UpdateSessionStatus(string chatSessionID, int sessionStatus);
        List<AppChatSessionInfo> AppChatSessionInfo_GetSessionStatusList(string sessionIDs);
        #endregion
    }
}
