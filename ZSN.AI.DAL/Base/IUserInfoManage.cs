using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IUserInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_user_info
        int UserInfo_Add(UserInfo model);
        bool UserInfo_Update(UserInfo model);
        bool UserInfo_UpdatePassword(UserInfo model);
        bool UserInfo_Delete(Int32 userID); 
        bool UserInfo_DeleteList(string userIDlist);
        UserInfo UserInfo_DataRowToModel(DataRow row);
        UserInfo UserInfo_GetModel(Int32 userID);
        UserInfo UserInfo_GetModel(string username);
        DataSet UserInfo_GetList(string strWhere);
        DataSet UserInfo_GetList(int top, string strWhere, string filedOrder);
        int UserInfo_GetRecordCount(string strWhere);
        DataSet UserInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable UserInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
