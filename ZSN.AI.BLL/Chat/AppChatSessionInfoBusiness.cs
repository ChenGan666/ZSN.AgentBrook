using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class AppChatSessionInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "ChatDb";
        #endregion
		#region tb_app_chat_session_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(AppChatSessionInfo model)
		{
			return DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(AppChatSessionInfo model)
		{
			return DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string chatSessionID)
		{
			return DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_Delete(chatSessionID);
		}
        public static bool Delete(string chatSessionID,string MemberID)
        {
            return DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_Delete(chatSessionID, MemberID);
        }
        public static bool CleanUp(string MemberID)
        {
            return DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_CleanUp(MemberID);
        }
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string chatSessionIDlist)
		{
            chatSessionIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(chatSessionIDlist, ',', '\'');
            return DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_DeleteList(chatSessionIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.AppChatSessionInfo GetModel(string chatSessionID)
		{
			return DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_GetModel(chatSessionID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<AppChatSessionInfo> GetList(string strWhere = "")
		{
            return AppChatSessionInfoDataSet_ToList(DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_GetList(strWhere).Tables[0]);
		}
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<AppChatSessionInfo> GetList(int top, string strWhere, string filedOrder)
		{
            return AppChatSessionInfoDataSet_ToList(DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_GetList(top, strWhere, filedOrder).Tables[0]);
		}
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
		{
            return DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_GetRecordCount(strWhere);
		}
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<AppChatSessionInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
		{
            return AppChatSessionInfoDataSet_ToList(DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<AppChatSessionInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "ChatSessionID")
		{
            return AppChatSessionInfoDataSet_ToList(DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<AppChatSessionInfo> AppChatSessionInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<AppChatSessionInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_DataRowToModel(r));
            }
            return list;
		}
		/// <summary>
        /// 更新会话状态
        /// </summary>
		public static bool UpdateSessionStatus(string chatSessionID, int sessionStatus)
		{
			return DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_UpdateSessionStatus(chatSessionID, sessionStatus);
		}
		/// <summary>
        /// 根据会话ID列表批量查询会话状态
        /// </summary>
		public static List<AppChatSessionInfo> GetSessionStatusList(string sessionIDs)
		{
			return DatabaseProvider.GetAppChatSessionInfo(ConnectionName).AppChatSessionInfo_GetSessionStatusList(sessionIDs);
		}
		/// <summary>
        /// 获取会话最后一条assistant消息的摘要（用于通知）
        /// </summary>
		public static string GetLastAssistantSummary(string appID, string sessionID, int maxLength = 120)
		{
			try
			{
				var logs = AppChatLogInfoBussiness.GetListBySessionID(appID, sessionID);
				if (logs == null || logs.Count == 0) return string.Empty;
				var lastAssistant = logs.LastOrDefault(l => l.Role != null && l.Role.Equals("Assistant", StringComparison.OrdinalIgnoreCase));
				if (lastAssistant == null) return string.Empty;
				string content = lastAssistant.Content?.ToString() ?? string.Empty;
				if (content.Length > maxLength)
					content = content.Substring(0, maxLength) + "...";
				return content;
			}
			catch
			{
				return string.Empty;
			}
		}
		#endregion
	}
}
