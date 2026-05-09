using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IEpisodicMemoryManage EpisodicMemoryInstance;
        private static readonly object EpisodicMemoryLockObj = new object();

        public static IEpisodicMemoryManage GetEpisodicMemory(string connectionName)
        {
            if (EpisodicMemoryInstance == null)
            {
                lock (EpisodicMemoryLockObj)
                {
                    if (EpisodicMemoryInstance == null)
                    {
                        GetEpisodicMemoryProvider(connectionName);
                    }
                }
            }
            return EpisodicMemoryInstance;
        }

        private static void GetEpisodicMemoryProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL.{db.DbType}.EpisodicMemoryManage, ZSN.AI.DAL.{db.DbType}",
                    false, true);
                var provider = (IEpisodicMemoryManage)Activator.CreateInstance(type);
                provider.SetConnectionName(connectionName);
                EpisodicMemoryInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
