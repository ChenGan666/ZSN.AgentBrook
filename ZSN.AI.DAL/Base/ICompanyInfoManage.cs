using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface ICompanyInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_company_info
        int CompanyInfo_Add(CompanyInfo model);
        bool CompanyInfo_Update(CompanyInfo model);
        bool CompanyInfo_Delete(Int32 companyID); 
        bool CompanyInfo_DeleteList(string companyIDlist);
        CompanyInfo CompanyInfo_DataRowToModel(DataRow row);
        CompanyInfo CompanyInfo_GetModel();
        CompanyInfo CompanyInfo_GetModel(Int32 companyID);
        DataSet CompanyInfo_GetList(string strWhere);
        DataSet CompanyInfo_GetList(int top, string strWhere, string filedOrder);
        int CompanyInfo_GetRecordCount(string strWhere);
        DataSet CompanyInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable CompanyInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
