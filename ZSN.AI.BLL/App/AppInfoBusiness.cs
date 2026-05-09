using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Extensions;
using NPOI.SS.Formula.Functions;
namespace ZSN.AI.BLL
{
    public partial class AppInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "AppDb";
        private const string ConnectionName_WorkflowDb = "WorkflowDb";
        
        #endregion
        #region tb_app_info
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(AppInfo model)
		{
            string Appid = DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_Add(model);
            
            return Appid;
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(AppInfo model)
		{
            return DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_Update(model);
            
			
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string appID)
		{
			return DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_Delete(appID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string appIDlist)
		{
            appIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(appIDlist, ',', '\'');

            return DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_DeleteList(appIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.AppInfo GetModel(string appID)
		{
            AppInfo _app = DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_GetModel(appID);
            

            WorkflowInfo _workflow = DatabaseProvider.GetWorkflowInfo(ConnectionName_WorkflowDb).WorkflowInfo_GetModelByMainID(appID,1);
            _app.WorkFlowID = _workflow?.WorkflowID;

            return _app;

        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<AppInfo> GetList(string strWhere = "")
        {
            return AppInfoDataSet_ToList(DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<AppInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return AppInfoDataSet_ToList(DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<AppInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return AppInfoDataSet_ToList(DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<AppInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "AppID")
		{
            return AppInfoDataSet_ToList(DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<AppInfo> AppInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<AppInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetAppInfo(ConnectionName).AppInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
