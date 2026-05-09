using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ILongTermMemoryManage LongTermMemoryInstance;
        private static readonly object LongTermMemoryLockObj = new object();

        public static ILongTermMemoryManage GetLongTermMemory(string connectionName)
        {
            if (LongTermMemoryInstance == null)
            {
                lock (LongTermMemoryLockObj)
                {
                    if (LongTermMemoryInstance == null)
                    {
                        GetLongTermMemoryProvider(connectionName);
                    }
                }
            }
            return LongTermMemoryInstance;
        }

        private static void GetLongTermMemoryProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL.{db.DbType}.LongTermMemoryManage, ZSN.AI.DAL.{db.DbType}",
                    false, true);
                var provider = (ILongTermMemoryManage)Activator.CreateInstance(type);
                provider.SetConnectionName(connectionName);
                LongTermMemoryInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
