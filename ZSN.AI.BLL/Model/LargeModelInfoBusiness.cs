using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class LargeModelInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "ModelDb";
        #endregion
		#region tb_large_model_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(LargeModelInfo model)
		{
			return DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(LargeModelInfo model)
		{
			return DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 largeModelID)
		{
			return DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_Delete(largeModelID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string largeModelIDlist)
		{
			return DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_DeleteList(largeModelIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.LargeModelInfo GetModel(Int32 largeModelID)
		{
			return DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_GetModel(largeModelID);
		}
        public static ZSN.AI.Entity.LargeModelInfo GetDefaultModel()
        {
            return DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_GetDefaultModel();
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<LargeModelInfo> GetList(string strWhere = "")
        {
            return LargeModelInfoDataSet_ToList(DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<LargeModelInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return LargeModelInfoDataSet_ToList(DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<LargeModelInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return LargeModelInfoDataSet_ToList(DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<LargeModelInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "LargeModelID")
		{
            return LargeModelInfoDataSet_ToList(DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<LargeModelInfo> LargeModelInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<LargeModelInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetLargeModelInfo(ConnectionName).LargeModelInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
