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
        private const string ConnectionName = "MessageDb";
        public static string Add(MessageRouteRuleInfo model)
        {
            return DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_Add(model);
        }
        public static bool Update(MessageRouteRuleInfo model)
        {
            return DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_Update(model);
        }
        public static bool Delete(string ruleID)
        {
            return DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_Delete(ruleID);
        }
        public static MessageRouteRuleInfo GetModel(string ruleID)
        {
            return DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_GetModel(ruleID);
        }
        public static List<MessageRouteRuleInfo> GetList(string strWhere = "")
        {
            return MessageRouteRuleDataSet_ToList(DatabaseProvider.GetMessageRouteRule(ConnectionName).MessageRouteRule_GetList(strWhere).Tables[0]);
        }
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
    }
}
