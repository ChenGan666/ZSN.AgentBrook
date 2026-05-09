using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.DAL;
using ZSN.AI.Entity;
namespace ZSN.AI.BLL
{
    public partial class PluginsInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "ModelDb";
        #endregion
		#region tb_plugins_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(PluginsInfo model)
		{
			return DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(PluginsInfo model)
		{
			return DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 pluginsID)
		{
			return DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_Delete(pluginsID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string pluginsIDlist)
		{
			return DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_DeleteList(pluginsIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static PluginsInfo GetModel(Int32 pluginsID)
		{
			return DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_GetModel(pluginsID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<PluginsInfo> GetList(string strWhere = "")
        {
            return PluginsInfoDataSet_ToList(DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<PluginsInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return PluginsInfoDataSet_ToList(DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<PluginsInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return PluginsInfoDataSet_ToList(DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }
		/// <summary>
        /// 分页获取数据列表
        /// </summary>
        /// <param name="pageSize">每页大小</param>
        /// <param name="pageIndex">页标</param>
        /// <param name="strWhere">查询条件</param>
        /// <param name="pagetotal">总页数</param>
        /// <param name="total">总数</param>
        /// <param name="orderType">排序规则， 默认降序，1降序，0升序</param>
        /// <param name="showName">显示字段，默认全部</param>
        /// <param name="orderKey">排序key，默认主键</param>
        /// <returns></returns>
		public static List<PluginsInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "PluginsID")
		{
            return PluginsInfoDataSet_ToList(DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<PluginsInfo> PluginsInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<PluginsInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetPluginsInfo(ConnectionName).PluginsInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
