using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IDepartmentInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_department_info
        int DepartmentInfo_Add(DepartmentInfo model);
        bool DepartmentInfo_Update(DepartmentInfo model);
        bool DepartmentInfo_Delete(Int32 departmentID); 
        bool DepartmentInfo_DeleteList(string departmentIDlist);
        DepartmentInfo DepartmentInfo_DataRowToModel(DataRow row);
        DepartmentInfo DepartmentInfo_GetModel(Int32 departmentID); 
        DataSet DepartmentInfo_GetList(string strWhere);
        DataSet DepartmentInfo_GetList(int top, string strWhere, string filedOrder);
        int DepartmentInfo_GetRecordCount(string strWhere);
        DataSet DepartmentInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable DepartmentInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
