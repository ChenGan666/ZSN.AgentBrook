using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.DAL.Agent;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IAgentKnowledgeBaseInfoManage AgentKnowledgeBaseInfoInstance;
        private static readonly object AgentKnowledgeBaseInfoLockObj = new object();
        public static IAgentKnowledgeBaseInfoManage GetAgentKnowledgeBaseInfo(string connectionName)
        {
            if (AgentKnowledgeBaseInfoInstance == null)
            {
                lock (AgentKnowledgeBaseInfoLockObj)
                {
                    if (AgentKnowledgeBaseInfoInstance == null)
                    {
                        GetAgentKnowledgeBaseInfoProvider(connectionName);
                    }
                }
            }
            return AgentKnowledgeBaseInfoInstance;
        }
        private static void GetAgentKnowledgeBaseInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".AgentKnowledgeBaseInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IAgentKnowledgeBaseInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                AgentKnowledgeBaseInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
