using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IKnowledgeRelationManage KnowledgeRelationInstance;
        private static readonly object KnowledgeRelationLockObj = new object();

        public static IKnowledgeRelationManage GetKnowledgeRelation(string connectionName)
        {
            if (KnowledgeRelationInstance == null)
            {
                lock (KnowledgeRelationLockObj)
                {
                    if (KnowledgeRelationInstance == null)
                    {
                        GetKnowledgeRelationProvider(connectionName);
                    }
                }
            }
            return KnowledgeRelationInstance;
        }

        private static void GetKnowledgeRelationProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL.{db.DbType}.KnowledgeRelationManage, ZSN.AI.DAL.{db.DbType}",
                    false, true);
                var provider = (IKnowledgeRelationManage)Activator.CreateInstance(type);
                provider.SetConnectionName(connectionName);
                KnowledgeRelationInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
