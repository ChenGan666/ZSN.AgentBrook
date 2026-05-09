using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Extensions;
namespace ZSN.AI.BLL
{
    public partial class WorkflowInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "WorkflowDb";
        #endregion
        #region tb_workflow_info
        public static string Save(WorkFlow workFlow) {

            if (workFlow.Info != null)
            {
                WorkflowInfo info = GetModel(workFlow.Info.WorkflowID);

                bool isOK = true;
                if (info == null)
                {
                    info = new WorkflowInfo();
                    info.WorkflowID = Add(workFlow.Info);
                }
                else
                {
                    isOK = Update(workFlow.Info);
                }
                if (isOK)
                {

                    string NodeIDList = String.Join(",", workFlow.Nodes.Select(n => $"'{n.NodeID}'"));
                    string EdgeIDList = String.Join(",", workFlow.Edges.Select(n => $"'{n.EdgeID}'"));

                    if (!NodeIDList.IsNullOrEmpty())
                    {
                        WorkflowNodeInfoBussiness.DeleteAbsentList(NodeIDList, workFlow.Info.WorkflowID);
                    }
                    if (EdgeIDList.IsNullOrEmpty())
                    {
                        //删除所有连线
                        EdgeIDList = "'x'";
                        
                    }
                    WorkflowEdgeInfoBussiness.DeleteAbsentList(EdgeIDList, workFlow.Info.WorkflowID);
                    
                    foreach (var nodes in workFlow.Nodes)
                    {
                        WorkflowNodeInfoBussiness.Save(nodes);
                    }
                    foreach (var edge in workFlow.Edges)
                    {
                        WorkflowEdgeInfoBussiness.Save(edge);
                    }
                }

                return workFlow.Info.WorkflowID;
            }
            else
            {
                return "";
            }
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(WorkflowInfo model)
		{
			return DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(WorkflowInfo model)
		{
			return DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string workflowID)
		{
			return DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_Delete(workflowID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string workflowIDlist)
		{
            workflowIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(workflowIDlist, ',', '\'');
            if (!workflowIDlist.IsNullOrEmpty())
            {
                DatabaseProvider.GetWorkflowNodeInfo(connectionName: ConnectionName).WorkflowNodeInfo_DeleteByWorkflowID(workflowIDlist);
                DatabaseProvider.GetWorkflowEdgeInfo(connectionName: ConnectionName).WorkflowEdgeInfo_DeleteByWorkflowID(workflowIDlist);
            }

            return DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_DeleteList(workflowIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.WorkflowInfo GetModel(string workflowID)
		{
			return DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_GetModel(workflowID);
		}
        public static ZSN.AI.Entity.WorkflowInfo GetModelByAppID(string AppID)
        {
            return DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_GetModelByMainID(AppID,1);
        }
        public static ZSN.AI.Entity.WorkflowInfo GetModelByAgentID(string AgentID)
        {
            return DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_GetModelByMainID(AgentID, 2);
        }
        public static List<WorkflowInfo> GetListByAgentID(string AgentID,int Status=1)
        {
            string strWhere = " MainType=2 and MainID='"+ AgentID + "' and SystemStatus=1 ";
            return WorkflowInfoDataSet_ToList(DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<WorkflowInfo> GetList(string strWhere = "")
        {
            return WorkflowInfoDataSet_ToList(DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<WorkflowInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return WorkflowInfoDataSet_ToList(DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<WorkflowInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return WorkflowInfoDataSet_ToList(DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<WorkflowInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime")
		{
            return WorkflowInfoDataSet_ToList(DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<WorkflowInfo> WorkflowInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<WorkflowInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetWorkflowInfo(ConnectionName).WorkflowInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
