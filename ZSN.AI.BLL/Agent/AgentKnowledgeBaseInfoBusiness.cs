using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class AgentKnowledgeBaseInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "AgentDb";
        #endregion
		#region tb_app_knowledge_base_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(AgentKnowledgeBaseInfo model)
		{
			return DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(AgentKnowledgeBaseInfo model)
		{
			return DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 AgentKnowledgeBaseID)
		{
			return DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_Delete(AgentKnowledgeBaseID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string AgentKnowledgeBaseIDlist)
		{
			return DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_DeleteList(AgentKnowledgeBaseIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.AgentKnowledgeBaseInfo GetModel(Int32 AgentKnowledgeBaseID)
		{
			return DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_GetModel(AgentKnowledgeBaseID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<AgentKnowledgeBaseInfo> GetList(string strWhere = "")
        {
            return AgentKnowledgeBaseInfoDataSet_ToList(DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<AgentKnowledgeBaseInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return AgentKnowledgeBaseInfoDataSet_ToList(DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<AgentKnowledgeBaseInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return AgentKnowledgeBaseInfoDataSet_ToList(DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<AgentKnowledgeBaseInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "AgentKnowledgeBaseID")
		{
            return AgentKnowledgeBaseInfoDataSet_ToList(DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<AgentKnowledgeBaseInfo> AgentKnowledgeBaseInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<AgentKnowledgeBaseInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetAgentKnowledgeBaseInfo(ConnectionName).AgentKnowledgeBaseInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
