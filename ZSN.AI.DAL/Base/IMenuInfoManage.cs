using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IMenuInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_menu_info
        string MenuInfo_Add(MenuInfo model);
        bool MenuInfo_Update(MenuInfo model);
        bool MenuInfo_Delete(string iD); 
        bool MenuInfo_DeleteList(string iDlist);
        int UpdateSort(string id, int sort);
        int GetMaxSort(string id);
        MenuInfo MenuInfo_DataRowToModel(DataRow row);
        MenuInfo MenuInfo_GetModel(string iD); 
        DataSet MenuInfo_GetList(string strWhere);
        DataSet MenuInfo_GetList(int top, string strWhere, string filedOrder);
        int MenuInfo_GetRecordCount(string strWhere);
        DataSet MenuInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable MenuInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
