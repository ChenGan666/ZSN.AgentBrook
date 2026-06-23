using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class MessageRouteRuleBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "MessageDb";
        #endregion
		#region tb_message_route_rule
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(MessageRouteRuleInfo model)
		{
			return DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(MessageRouteRuleInfo model)
		{
			return DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string ruleID)
		{
			return DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_Delete(ruleID);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static MessageRouteRuleInfo GetModel(string ruleID)
		{
			return DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_GetModel(ruleID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<MessageRouteRuleInfo> GetList(string strWhere = "")
		{
            return MessageRouteRuleDataSet_ToList(DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_GetList(strWhere).Tables[0]);
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
        /// <param name="orderKey">排序key，默认Priority</param>
        /// <returns></returns>
		public static List<MessageRouteRuleInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "Priority")
		{
            return MessageRouteRuleDataSet_ToList(DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
		}
		private static List<MessageRouteRuleInfo> MessageRouteRuleDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<MessageRouteRuleInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_DataRowToModel(r));
            }
            return list;
		}
		#endregion
	}
}
