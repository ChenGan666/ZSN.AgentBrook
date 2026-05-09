using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IAIPersonalityStateManage AIPersonalityStateInstance;
        private static readonly object AIPersonalityStateLockObj = new object();

        public static IAIPersonalityStateManage GetAIPersonalityState(string connectionName)
        {
            if (AIPersonalityStateInstance == null)
            {
                lock (AIPersonalityStateLockObj)
                {
                    if (AIPersonalityStateInstance == null)
                    {
                        GetAIPersonalityStateProvider(connectionName);
                    }
                }
            }
            return AIPersonalityStateInstance;
        }

        private static void GetAIPersonalityStateProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL.{db.DbType}.AIPersonalityStateManage, ZSN.AI.DAL.{db.DbType}",
                    false, true);
                var provider = (IAIPersonalityStateManage)Activator.CreateInstance(type);
                provider.SetConnectionName(connectionName);
                AIPersonalityStateInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
