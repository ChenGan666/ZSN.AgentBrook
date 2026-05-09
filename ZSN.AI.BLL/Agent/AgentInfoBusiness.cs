using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class AgentInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "AgentDb";
        #endregion
		#region tb_agent_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(AgentInfo model)
		{
			return DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(AgentInfo model)
		{
			return DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string AgentID)
		{
			return DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_Delete(AgentID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string AgentIDlist)
		{
            AgentIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(AgentIDlist, ',', '\'');
            return DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_DeleteList(AgentIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.AgentInfo GetModel(string AgentID)
		{
            AgentInfo _agent = DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_GetModel(AgentID);
            _agent.KnowledgeBases = AgentKnowledgeBaseInfoBussiness.GetList("AgentID='"+ AgentID+"'");
            return _agent;

        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<AgentInfo> GetList(string strWhere = "")
        {
            return AgentInfoDataSet_ToList(DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<AgentInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return AgentInfoDataSet_ToList(DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<AgentInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return AgentInfoDataSet_ToList(DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<AgentInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime")
		{
            return AgentInfoDataSet_ToList(DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<AgentInfo> AgentInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<AgentInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetAgentInfo(ConnectionName).AgentInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
