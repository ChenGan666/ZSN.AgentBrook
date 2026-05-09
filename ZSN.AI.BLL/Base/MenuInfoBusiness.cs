using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class MenuInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "BaseDb";
        #endregion
		#region tb_menu_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(MenuInfo model)
		{
			return DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(MenuInfo model)
		{
			return DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string iD)
		{
			return DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_Delete(iD);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string iDlist)
		{
			return DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_DeleteList(iDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.MenuInfo GetModel(string iD)
		{
			return DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_GetModel(iD);
		}

        public static int UpdateSort(string id, int sort)
        {
            return DatabaseProvider.GetMenuInfo(ConnectionName).UpdateSort(id, sort);
        }
        public static int GetMaxSort(string id)
        {
            return DatabaseProvider.GetMenuInfo(ConnectionName).GetMaxSort( id);
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<MenuInfo> GetList(string strWhere = "")
        {
            return MenuInfoDataSet_ToList(DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<MenuInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return MenuInfoDataSet_ToList(DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<MenuInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return MenuInfoDataSet_ToList(DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<MenuInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "ID")
		{
            return MenuInfoDataSet_ToList(DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<MenuInfo> MenuInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<MenuInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetMenuInfo(ConnectionName).MenuInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
