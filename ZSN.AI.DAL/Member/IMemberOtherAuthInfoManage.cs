using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IMemberOtherAuthInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_member_other_auth_info
        int MemberOtherAuthInfo_Add(MemberOtherAuthInfo model);
        bool MemberOtherAuthInfo_Update(MemberOtherAuthInfo model);
        bool MemberOtherAuthInfo_Delete(Int32 memberOtherAuthID); 
        bool MemberOtherAuthInfo_DeleteList(string memberOtherAuthIDlist);
        MemberOtherAuthInfo MemberOtherAuthInfo_DataRowToModel(DataRow row);
        MemberOtherAuthInfo MemberOtherAuthInfo_GetModel(Int32 memberOtherAuthID);
        MemberOtherAuthInfo MemberOtherAuthInfo_GetModel(string memberID);
        MemberOtherAuthInfo MemberOtherAuthInfo_GetModelByOpenid(string OpenID);
        DataSet MemberOtherAuthInfo_GetList(string strWhere);
        DataSet MemberOtherAuthInfo_GetList(int top, string strWhere, string filedOrder);
        int MemberOtherAuthInfo_GetRecordCount(string strWhere);
        DataSet MemberOtherAuthInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable MemberOtherAuthInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
