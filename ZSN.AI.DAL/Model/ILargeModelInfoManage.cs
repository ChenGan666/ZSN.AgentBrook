using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface ILargeModelInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_large_model_info
        int LargeModelInfo_Add(LargeModelInfo model);
        bool LargeModelInfo_Update(LargeModelInfo model);
        bool LargeModelInfo_Delete(Int32 largeModelID); 
        bool LargeModelInfo_DeleteList(string largeModelIDlist);
        LargeModelInfo LargeModelInfo_DataRowToModel(DataRow row);
        LargeModelInfo LargeModelInfo_GetModel(Int32 largeModelID);
        LargeModelInfo LargeModelInfo_GetDefaultModel();
        
        DataSet LargeModelInfo_GetList(string strWhere);
        DataSet LargeModelInfo_GetList(int top, string strWhere, string filedOrder);
        int LargeModelInfo_GetRecordCount(string strWhere);
        DataSet LargeModelInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable LargeModelInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
