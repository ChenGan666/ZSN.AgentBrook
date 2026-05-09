using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using Google.Protobuf.WellKnownTypes;
namespace ZSN.AI.BLL
{
    public partial class ApisettingsInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "AppDb";
        #endregion
		#region tb_apisettings_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(ApisettingsInfo model)
		{
			return DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(ApisettingsInfo model)
		{
			return DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 apiID)
		{
			return DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_Delete(apiID);
		}
        public static bool DeleteByAppID(string AppID)
        {
            return DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_DeleteByAppID(AppID);
        }
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string apiIDlist)
		{
			return DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_DeleteList(apiIDlist);
		}
        public static bool DeleteListByAppID(string AppIDlist)
        {
            AppIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(AppIDlist, ',', '\'');
            return DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_DeleteListByAppID(AppIDlist);
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.ApisettingsInfo GetModel(Int32 apiID)
		{
			return DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_GetModel(apiID);
		}
        public static ZSN.AI.Entity.ApisettingsInfo GetModelByAppID(string appID)
        {
            return DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_GetModelByAppID(appID);
        }
        public static ZSN.AI.Entity.ApisettingsInfo GetModelByCompanyID(int CompanyID) {
            return DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_GetModelByCompanyID(CompanyID);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<ApisettingsInfo> GetList(string strWhere = "")
        {
            return ApisettingsInfoDataSet_ToList(DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<ApisettingsInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return ApisettingsInfoDataSet_ToList(DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<ApisettingsInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return ApisettingsInfoDataSet_ToList(DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<ApisettingsInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "ApiID")
		{
            return ApisettingsInfoDataSet_ToList(DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<ApisettingsInfo> ApisettingsInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<ApisettingsInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetApisettingsInfo(ConnectionName).ApisettingsInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
