using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IBaseDictionaryInfoManage
    {
        string SetConnectionName(string connName);
        #region base_dictionary_info
        int BaseDictionaryInfo_Add(BaseDictionaryInfo model);
        bool BaseDictionaryInfo_Update(BaseDictionaryInfo model);
        bool BaseDictionaryInfo_Delete(Int32 dicId); 
        bool BaseDictionaryInfo_DeleteList(string dicIdlist);
        BaseDictionaryInfo BaseDictionaryInfo_DataRowToModel(DataRow row);
        BaseDictionaryInfo BaseDictionaryInfo_GetModel(Int32 dicId); 
        DataSet BaseDictionaryInfo_GetList(string strWhere);
        DataSet BaseDictionaryInfo_GetChildList(string Name);
        DataSet BaseDictionaryInfo_GetAllChildList(string name = "");
        DataSet BaseDictionaryInfo_GetAllChildList(int DicId = 0);
        DataSet BaseDictionaryInfo_GetAllParentList(string name = "");
        DataSet BaseDictionaryInfo_GetList(int top, string strWhere, string filedOrder);
        int BaseDictionaryInfo_GetRecordCount(string strWhere);
        DataSet BaseDictionaryInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable BaseDictionaryInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
