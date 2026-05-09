using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IVcodeInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_vcode_info
        int VcodeInfo_Add(VcodeInfo model);
        bool VcodeInfo_Update(VcodeInfo model);
        bool VcodeInfo_Delete(Int32 vCodeID); 
        bool VcodeInfo_DeleteList(string vCodeIDlist);
        VcodeInfo VcodeInfo_DataRowToModel(DataRow row);
        VcodeInfo VcodeInfo_GetModel(Int32 vCodeID); 
        DataSet VcodeInfo_GetList(string strWhere);
        DataSet VcodeInfo_GetList(int top, string strWhere, string filedOrder);
        int VcodeInfo_GetRecordCount(string strWhere);
        DataSet VcodeInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable VcodeInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
