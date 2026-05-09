using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IMemberAuthInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_member_auth_info
        int MemberAuthInfo_Add(MemberAuthInfo model);
        bool MemberAuthInfo_Update(MemberAuthInfo model);
        bool MemberAuthInfo_Delete(Int64 memberAuthID); 
        bool MemberAuthInfo_DeleteList(string memberAuthIDlist);
        MemberAuthInfo MemberAuthInfo_DataRowToModel(DataRow row);
        MemberAuthInfo MemberAuthInfo_GetModel(Int64 memberAuthID); 
        DataSet MemberAuthInfo_GetList(string strWhere);
        DataSet MemberAuthInfo_GetList(int top, string strWhere, string filedOrder);
        int MemberAuthInfo_GetRecordCount(string strWhere);
        DataSet MemberAuthInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable MemberAuthInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        MemberAuthInfo MemberAuthInfo_GetModel(string memberID);
        MemberAuthInfo MemberAuthInfo_GetModelByAccessToken(string AccessToken);
        MemberAuthInfo MemberAuthInfo_GetModelByRefreshToken(string RefreshToken);
        MemberAuthInfo MemberAuthInfo_GetModelByMemberID(string MemberID);
        #endregion
    }
}
