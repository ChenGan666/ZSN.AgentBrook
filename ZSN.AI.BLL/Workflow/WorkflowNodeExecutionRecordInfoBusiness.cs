using JiebaNet.Segmenter;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.DAL;
using ZSN.AI.Entity;
using static NpgsqlTypes.NpgsqlTsQuery;
namespace ZSN.AI.BLL
{
    public partial class WorkflowNodeExecutionRecordInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "WorkflowDb";
        #endregion
		#region tb_workflow_node_excution_record_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(WorkflowNodeExecutionRecordInfo model)
		{
			return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_Add(model);
		}
        public static bool Update(string RecordID, ExecutionRecordStatus Status, object Outputs, object Logs)
        {
            return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_Update( RecordID,  Status,  Outputs,  Logs);
        }
        /// <summary>
        /// 只更新 Outputs 和 Logs，不修改 Status（避免后台后处理覆盖已写入的最终状态）
        /// </summary>
        public static bool UpdateLogs(string RecordID, object Outputs, object Logs)
        {
            return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_UpdateLogs(RecordID, Outputs, Logs);
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(WorkflowNodeExecutionRecordInfo model)
		{
			return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_Update(model);
		}
        public static bool DeleteByNodeID(string SessionID, string NodeID)
        {
            return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).DeleteByNodeID(SessionID, NodeID);
        }
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string recordID)
		{
			return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_Delete(recordID);
		}
        public static bool DeleteByWhere(string where)
        {
            return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_DeleteByWhere(where);
        }
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string recordIDlist)
		{
			return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_DeleteList(recordIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.WorkflowNodeExecutionRecordInfo GetModel(string recordID)
		{
			return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetModel(recordID);
		}
        public static WorkflowNodeExecutionRecordInfo GetModelByTaskID(string testID,string sessionID)
        {
            return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetModelByTaskID(testID, sessionID);
        }

        public static List<WorkflowNodeExecutionRecordInfo> GetListBySessionID(string SessionID)
        {
            string strWhere = $" SessionID='{SessionID}' order by StartTime asc";
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetList(strWhere).Tables[0]);
        }
        public static List<WorkflowNodeExecutionRecordInfo> GetListBySessionIDProcessesID(string SessionID,string ProcessesID)
        {
            string strWhere = $" SessionID='{SessionID}' and ProcessesID LIKE '{ProcessesID}%' order by StartTime asc";
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<WorkflowNodeExecutionRecordInfo> GetList(string strWhere = "")
        {
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<WorkflowNodeExecutionRecordInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        public static List<WorkflowNodeExecutionRecordInfo> GetListByNodeId(string SessionID, string NodeID)
        {
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetListByNodeId(SessionID, NodeID).Tables[0]);
        }
        public static List<WorkflowNodeExecutionRecordInfo> GetListByNodeId(string SessionID, string NodeID, ExecutionRecordStatus recordStatus)
        {
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetListByNodeId(SessionID, NodeID, recordStatus).Tables[0]);
        }
        public static List<WorkflowNodeExecutionRecordInfo> GetListByNodeId(string SessionID, List<string> nodeIDList)
        {
            var idString = string.Join(",", nodeIDList.Where(i => !string.IsNullOrEmpty(i)).Select(i => $"'{i}'"));
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetListByNodeId(SessionID, idString).Tables[0]);

        }
        public static List<WorkflowNodeExecutionRecordInfo> GetListByNodeId(string SessionID, string NodeID, string ProcessesID)
        {
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetListByNodeId(SessionID, NodeID, ProcessesID).Tables[0]);
        }
        public static List<WorkflowNodeExecutionRecordInfo> GetListByNodeId(string SessionID, string NodeID, ExecutionRecordStatus recordStatus, string ProcessesID)
        {
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetListByNodeId(SessionID, NodeID, recordStatus, ProcessesID).Tables[0]);
        }
        public static List<WorkflowNodeExecutionRecordInfo> GetListByNodeId(string SessionID, List<string> nodeIDList, string ProcessesID)
        {
            var idString = string.Join(",", nodeIDList.Where(i => !string.IsNullOrEmpty(i)).Select(i => $"'{i}'"));
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetListByNodeId(SessionID, idString, ProcessesID).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<WorkflowNodeExecutionRecordInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<WorkflowNodeExecutionRecordInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "RecordID")
		{
            return WorkflowNodeExecutionRecordInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<WorkflowNodeExecutionRecordInfo> WorkflowNodeExecutionRecordInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<WorkflowNodeExecutionRecordInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetWorkflowNodeExecutionRecordInfo(ConnectionName).WorkflowNodeExecutionRecordInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
