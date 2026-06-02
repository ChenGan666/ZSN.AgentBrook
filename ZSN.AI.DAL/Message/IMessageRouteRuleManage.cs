using System;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial interface IMessageRouteRuleManage
    {
        string SetConnectionName(string connName);
        string MessageRouteRule_Add(MessageRouteRuleInfo model);
        bool MessageRouteRule_Update(MessageRouteRuleInfo model);
        bool MessageRouteRule_Delete(string ruleID);
        MessageRouteRuleInfo MessageRouteRule_DataRowToModel(DataRow row);
        MessageRouteRuleInfo MessageRouteRule_GetModel(string ruleID);
        DataSet MessageRouteRule_GetList(string strWhere);
        DataTable MessageRouteRule_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "Priority");
    }
}
