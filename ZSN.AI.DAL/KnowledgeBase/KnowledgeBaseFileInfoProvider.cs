using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IKnowledgeBaseFileInfoManage KnowledgeBaseFileInfoInstance;
        private static readonly object KnowledgeBaseFileInfoLockObj = new object();
        public static IKnowledgeBaseFileInfoManage GetKnowledgeBaseFileInfo(string connectionName)
        {
            if (KnowledgeBaseFileInfoInstance == null)
            {
                lock (KnowledgeBaseFileInfoLockObj)
                {
                    if (KnowledgeBaseFileInfoInstance == null)
                    {
                        GetKnowledgeBaseFileInfoProvider(connectionName);
                    }
                }
            }
            return KnowledgeBaseFileInfoInstance;
        }
        private static void GetKnowledgeBaseFileInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".KnowledgeBaseFileInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IKnowledgeBaseFileInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                KnowledgeBaseFileInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
