using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IKnowledgeBaseFileChunkInfoManage KnowledgeBaseFileChunkInfoInstance;
        private static readonly object KnowledgeBaseFileChunkInfoLockObj = new object();
        public static IKnowledgeBaseFileChunkInfoManage GetKnowledgeBaseFileChunkInfo(string connectionName)
        {
            if (KnowledgeBaseFileChunkInfoInstance == null)
            {
                lock (KnowledgeBaseFileChunkInfoLockObj)
                {
                    if (KnowledgeBaseFileChunkInfoInstance == null)
                    {
                        GetKnowledgeBaseFileChunkInfoProvider(connectionName);
                    }
                }
            }
            return KnowledgeBaseFileChunkInfoInstance;
        }
        private static void GetKnowledgeBaseFileChunkInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".KnowledgeBaseFileChunkInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IKnowledgeBaseFileChunkInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                KnowledgeBaseFileChunkInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
