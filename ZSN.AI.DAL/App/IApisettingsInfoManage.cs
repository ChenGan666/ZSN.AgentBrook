using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IApisettingsInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_apisettings_info
        int ApisettingsInfo_Add(ApisettingsInfo model);
        bool ApisettingsInfo_Update(ApisettingsInfo model);
        bool ApisettingsInfo_Delete(Int32 apiID); 
        bool ApisettingsInfo_DeleteList(string apiIDlist);
        bool ApisettingsInfo_DeleteByAppID(string AppID);
        bool ApisettingsInfo_DeleteListByAppID(string AppIDlist);
        ApisettingsInfo ApisettingsInfo_DataRowToModel(DataRow row);
        ApisettingsInfo ApisettingsInfo_GetModel(Int32 apiID);
        ApisettingsInfo ApisettingsInfo_GetModelByAppID(string appID);
        ApisettingsInfo ApisettingsInfo_GetModelByCompanyID(int CompanyID);
        DataSet ApisettingsInfo_GetList(string strWhere);
        DataSet ApisettingsInfo_GetList(int top, string strWhere, string filedOrder);
        int ApisettingsInfo_GetRecordCount(string strWhere);
        DataSet ApisettingsInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable ApisettingsInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
