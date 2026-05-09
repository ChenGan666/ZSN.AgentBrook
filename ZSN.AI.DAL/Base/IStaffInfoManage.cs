using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IStaffInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_staff_info
        int StaffInfo_Add(StaffInfo model);
        bool StaffInfo_Update(StaffInfo model);
        bool StaffInfo_Delete(Int32 staffID); 
        bool StaffInfo_DeleteList(string staffIDlist);
        StaffInfo StaffInfo_DataRowToModel(DataRow row);
        StaffInfo StaffInfo_GetModel(Int32 staffID);
        StaffInfo StaffInfo_GetModel(string MemberID);
        DataSet StaffInfo_GetList(string strWhere);
        DataSet StaffInfo_GetList(int top, string strWhere, string filedOrder);
        int StaffInfo_GetRecordCount(string strWhere);
        DataSet StaffInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable StaffInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
