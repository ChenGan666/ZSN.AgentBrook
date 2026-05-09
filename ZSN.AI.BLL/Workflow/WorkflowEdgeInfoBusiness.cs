using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class WorkflowEdgeInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "WorkflowDb";
        #endregion
        #region tb_workflow_edge_info
        public static void Save(WorkflowEdgeInfo model) {
            if (GetModel(model.EdgeID) != null)
            {
                Update(model);
            }
            else
            {
                Add(model);
            }
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(WorkflowEdgeInfo model)
		{
			return DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(WorkflowEdgeInfo model)
		{
			return DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string edgeID)
		{
			return DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_Delete(edgeID);
		}
        
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string edgeIDlist)
		{
			return DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_DeleteList(edgeIDlist);
		}
        public static bool DeleteAbsentList(string edgeID, string WorkflowID)
        {
            return DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_DeleteAbsentList(edgeID, WorkflowID);
        }
        public static bool DeleteByWorkflowID(string edgeID, string WorkflowID)
        {
            return DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_DeleteByWorkflowID( WorkflowID);
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.WorkflowEdgeInfo GetModel(string edgeID)
		{
			return DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_GetModel(edgeID);
		}
        /// <summary>
        /// 获取来源是NodeId的边列表
        /// </summary>
        /// <param name="NodeId"></param>
        /// <returns></returns>
        public static List<WorkflowEdgeInfo>  GetListBySourceNodeId(string NodeId) {
            return WorkflowEdgeInfoDataSet_ToList(DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_GetListBySourceNodeId(NodeId).Tables[0]);
        }
        /// <summary>
        /// 获取去目标是NodeId的边列表
        /// </summary>
        /// <param name="NodeId"></param>
        /// <returns></returns>
        public static List<WorkflowEdgeInfo> GetListByTargetNodeId(string NodeId) {
            return WorkflowEdgeInfoDataSet_ToList(DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_GetListByTargetNodeId(NodeId).Tables[0]);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<WorkflowEdgeInfo> GetList(string strWhere = "")
        {
            return WorkflowEdgeInfoDataSet_ToList(DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<WorkflowEdgeInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return WorkflowEdgeInfoDataSet_ToList(DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<WorkflowEdgeInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return WorkflowEdgeInfoDataSet_ToList(DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<WorkflowEdgeInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "edgeID")
		{
            return WorkflowEdgeInfoDataSet_ToList(DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<WorkflowEdgeInfo> WorkflowEdgeInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<WorkflowEdgeInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetWorkflowEdgeInfo(ConnectionName).WorkflowEdgeInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
