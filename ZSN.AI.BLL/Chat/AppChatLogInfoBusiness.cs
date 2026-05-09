using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using Google.Protobuf.WellKnownTypes;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
namespace ZSN.AI.BLL
{
    public partial class AppChatLogInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "ChatDb";
        #endregion
        #region tb_app_chat_log_info


        public static string Add(string AppID,string SessionID,string Role, GptMsg Inputs, int LargeModelID = 0) {

            List<AppChatLogInfo> appChatLogs = AppChatLogInfoBussiness.GetListBySessionID(AppID, SessionID);

            int ChatCount = appChatLogs != null ? appChatLogs.Count : 0;

            AppChatLogInfo appChat = new AppChatLogInfo();
            appChat.ChatLogID = Guid.NewGuid().ToString();
            appChat.AppID = AppID;
            appChat.ChatSessionID = SessionID;
            appChat.Direction = string.Equals(Role, "Assistant", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            appChat.Role = Role;
            appChat.LargeModelID = LargeModelID;
            appChat.Content = JsonConvert.SerializeObject(Inputs);
            appChat.CreateTime = DateTime.Now;
            appChat.LogOrder = ChatCount++;

            string _re = AppChatLogInfoBussiness.Add(appChat);

            AppInfo App = AppInfoBussiness.GetModel(AppID);
            
            // 如果 App 不存在,直接返回,不执行记录员任务
            if (App == null)
            {
                return _re;
            }
            
            //向记录员AI下发工作任务
            WorkflowNodeInfo reporterNodeInfo = WorkflowNodeInfoBussiness.GetAppReporterNode(App.AppID);
            if (reporterNodeInfo != null)
            {
                if (reporterNodeInfo.Config != null)
                {
                    NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(reporterNodeInfo.Config.ToString());
                    if (nodeConfig != null)
                    {
                        if (nodeConfig.data != null)
                        {
                            ReporterData reporterData = JsonConvert.DeserializeObject<ReporterData>(nodeConfig.data.ToString());

                            if (reporterData.enable && appChatLogs.Count >= reporterData.recordslength)
                            {
                                TaskData taskData = new TaskData();
                                taskData.AppID = AppID;
                                taskData.SessionID = SessionID;

                                TaskInfo taskInfo = new TaskInfo();
                                taskInfo.TaskType = NodeType.Reporter;
                                taskInfo.TaskConfig = new TaskConfig();
                                taskInfo.TaskConfig.NodeConfig = nodeConfig;
                                taskInfo.LoopType = LoopType.NOLoop;
                                taskInfo.RepeatValue = 1;
                                taskInfo.RedoCount = 0;

                                taskInfo.TaskConfig.Data = taskData;

                                TaskInfoBussiness.Add(taskInfo);
                            }
                        }
                    }
                }
            }
            return _re;
        }

		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(AppChatLogInfo model)
		{
			return DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(AppChatLogInfo model)
		{
			return DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string chatLogID)
		{
			return DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_Delete(chatLogID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string chatLogIDlist)
		{
            chatLogIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(chatLogIDlist, ',', '\'');
            return DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_DeleteList(chatLogIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.AppChatLogInfo GetModel(string chatLogID)
		{
			return DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_GetModel(chatLogID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<AppChatLogInfo> GetListBySessionID(string AppID,string SessionID)
        {
            return AppChatLogInfoDataSet_ToList(DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_GetListBySessionID(AppID,SessionID).Tables[0]);
        }
        public static List<AppChatLogInfo> GetList(string strWhere = "")
        {
            return AppChatLogInfoDataSet_ToList(DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_GetList(strWhere).Tables[0]);
        }
        public static List<AppChatLogInfo> GetList(string SQL,string SessionID = "",string MemberID = "")
        {
            return AppChatLogInfoDataSet_ToList(DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_GetList(SQL, SessionID, MemberID).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<AppChatLogInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return AppChatLogInfoDataSet_ToList(DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<AppChatLogInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return AppChatLogInfoDataSet_ToList(DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<AppChatLogInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "ChatLogID")
		{
            return AppChatLogInfoDataSet_ToList(DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<AppChatLogInfo> AppChatLogInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<AppChatLogInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetAppChatLogInfo(ConnectionName).AppChatLogInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
