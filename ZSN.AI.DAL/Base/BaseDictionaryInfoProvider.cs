using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IBaseDictionaryInfoManage BaseDictionaryInfoInstance;
        private static readonly object BaseDictionaryInfoLockObj = new object();
        public static IBaseDictionaryInfoManage GetBaseDictionaryInfo(string connectionName)
        {
            if (BaseDictionaryInfoInstance == null)
            {
                lock (BaseDictionaryInfoLockObj)
                {
                    if (BaseDictionaryInfoInstance == null)
                    {
                        GetBaseDictionaryInfoProvider(connectionName);
                    }
                }
            }
            return BaseDictionaryInfoInstance;
        }
        private static void GetBaseDictionaryInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".BaseDictionaryInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IBaseDictionaryInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                BaseDictionaryInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
