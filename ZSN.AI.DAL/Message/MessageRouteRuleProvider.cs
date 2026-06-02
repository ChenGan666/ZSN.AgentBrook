using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IMessageRouteRuleManage _messageRouteRuleInstance;
        private static readonly object _messageRouteRuleLockObj = new object();
        public static IMessageRouteRuleManage GetMessageRouteRule(string connectionName)
        {
            if (_messageRouteRuleInstance == null)
            {
                lock (_messageRouteRuleLockObj)
                {
                    if (_messageRouteRuleInstance == null)
                    {
                        var db = DbConfig.GetDbInfo(connectionName);
                        var type = Type.GetType(
                            $"ZSN.AI.DAL." + db.DbType + ".MessageRouteRuleManage, ZSN.AI.DAL." + db.DbType,
                            false, true);
                        var provider = (IMessageRouteRuleManage)Activator.CreateInstance(type);
                        provider.SetConnectionName(connectionName);
                        _messageRouteRuleInstance = provider;
                    }
                }
            }
            return _messageRouteRuleInstance;
        }
    }
}
