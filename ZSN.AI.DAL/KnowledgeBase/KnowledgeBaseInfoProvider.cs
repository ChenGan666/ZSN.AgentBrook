using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IKnowledgeBaseInfoManage KnowledgeBaseInfoInstance;
        private static readonly object KnowledgeBaseInfoLockObj = new object();
        public static IKnowledgeBaseInfoManage GetKnowledgeBaseInfo(string connectionName)
        {
            if (KnowledgeBaseInfoInstance == null)
            {
                lock (KnowledgeBaseInfoLockObj)
                {
                    if (KnowledgeBaseInfoInstance == null)
                    {
                        GetKnowledgeBaseInfoProvider(connectionName);
                    }
                }
            }
            return KnowledgeBaseInfoInstance;
        }
        private static void GetKnowledgeBaseInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".KnowledgeBaseInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IKnowledgeBaseInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                KnowledgeBaseInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
