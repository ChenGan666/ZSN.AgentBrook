using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IMemberInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_member_info
        string MemberInfo_Add(MemberInfo model);
        bool MemberInfo_Update(MemberInfo model);
        bool MemberInfo_Delete(string memberID); 
        bool MemberInfo_DeleteList(string memberIDlist);
        MemberInfo MemberInfo_DataRowToModel(DataRow row);
        MemberInfo MemberInfo_GetModel(string memberID);
        MemberInfo MemberInfo_GetModelByPhoneNumber(string PhoneNumber);
        DataSet MemberInfo_GetList(string strWhere);
        DataSet MemberInfo_GetList(int top, string strWhere, string filedOrder);
        int MemberInfo_GetRecordCount(string strWhere);
        DataSet MemberInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable MemberInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
