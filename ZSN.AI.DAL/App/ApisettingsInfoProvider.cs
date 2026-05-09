using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IApisettingsInfoManage ApisettingsInfoInstance;
        private static readonly object ApisettingsInfoLockObj = new object();
        public static IApisettingsInfoManage GetApisettingsInfo(string connectionName)
        {
            if (ApisettingsInfoInstance == null)
            {
                lock (ApisettingsInfoLockObj)
                {
                    if (ApisettingsInfoInstance == null)
                    {
                        GetApisettingsInfoProvider(connectionName);
                    }
                }
            }
            return ApisettingsInfoInstance;
        }
        private static void GetApisettingsInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".ApisettingsInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IApisettingsInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                ApisettingsInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
