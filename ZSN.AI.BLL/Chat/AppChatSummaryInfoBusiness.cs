using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using MySqlX.XDevAPI;
namespace ZSN.AI.BLL
{
    public partial class AppChatSummaryInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "ChatDb";
        #endregion
		#region tb_app_chat_summary_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(AppChatSummaryInfo model)
		{
			return DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(AppChatSummaryInfo model)
		{
			return DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string summaryID)
		{
			return DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_Delete(summaryID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string summaryIDlist)
		{
			return DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_DeleteList(summaryIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.AppChatSummaryInfo GetModel(string summaryID)
		{
			return DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_GetModel(summaryID);
		}
        public static List<AppChatSummaryInfo> GetListBySessionID(string AppID, string SessionID)
        {
            return AppChatSummaryInfoDataSet_ToList(DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_GetListBySessionID(AppID, SessionID).Tables[0]);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<AppChatSummaryInfo> GetList(string strWhere = "")
        {
            return AppChatSummaryInfoDataSet_ToList(DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_GetList(strWhere).Tables[0]);
        }
        public static List<AppChatSummaryInfo> GetList(string strWhere = "", string SessionID = "", string MemberID = "")
        {
            return AppChatSummaryInfoDataSet_ToList(DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_GetList(strWhere, SessionID, MemberID).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<AppChatSummaryInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return AppChatSummaryInfoDataSet_ToList(DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<AppChatSummaryInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return AppChatSummaryInfoDataSet_ToList(DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<AppChatSummaryInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "SummaryID")
		{
            return AppChatSummaryInfoDataSet_ToList(DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<AppChatSummaryInfo> AppChatSummaryInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<AppChatSummaryInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetAppChatSummaryInfo(ConnectionName).AppChatSummaryInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
