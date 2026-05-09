using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IAgentInfoManage AgentInfoInstance;
        private static readonly object AgentInfoLockObj = new object();
        public static IAgentInfoManage GetAgentInfo(string connectionName)
        {
            if (AgentInfoInstance == null)
            {
                lock (AgentInfoLockObj)
                {
                    if (AgentInfoInstance == null)
                    {
                        GetAgentInfoProvider(connectionName);
                    }
                }
            }
            return AgentInfoInstance;
        }
        private static void GetAgentInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".AgentInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IAgentInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                AgentInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
