using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IPluginsInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_plugins_info
        int PluginsInfo_Add(PluginsInfo model);
        bool PluginsInfo_Update(PluginsInfo model);
        bool PluginsInfo_Delete(int pluginsID);
        bool PluginsInfo_DeleteList(string pluginsIDlist);
        PluginsInfo PluginsInfo_DataRowToModel(DataRow row);
        PluginsInfo PluginsInfo_GetModel(int pluginsID);
        DataSet PluginsInfo_GetList(string strWhere);
        DataSet PluginsInfo_GetList(int top, string strWhere, string filedOrder);
        int PluginsInfo_GetRecordCount(string strWhere);
        DataSet PluginsInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable PluginsInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
