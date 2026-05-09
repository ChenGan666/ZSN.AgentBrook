using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IKnowledgeBaseTagInfoManage KnowledgeBaseTagInfoInstance;
        private static readonly object KnowledgeBaseTagInfoLockObj = new object();
        public static IKnowledgeBaseTagInfoManage GetKnowledgeBaseTagInfo(string connectionName)
        {
            if (KnowledgeBaseTagInfoInstance == null)
            {
                lock (KnowledgeBaseTagInfoLockObj)
                {
                    if (KnowledgeBaseTagInfoInstance == null)
                    {
                        GetKnowledgeBaseTagInfoProvider(connectionName);
                    }
                }
            }
            return KnowledgeBaseTagInfoInstance;
        }
        private static void GetKnowledgeBaseTagInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".KnowledgeBaseTagInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IKnowledgeBaseTagInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                KnowledgeBaseTagInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
