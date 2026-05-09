using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IAppInfoManage AppInfoInstance;
        private static readonly object AppInfoLockObj = new object();
        public static IAppInfoManage GetAppInfo(string connectionName)
        {
            if (AppInfoInstance == null)
            {
                lock (AppInfoLockObj)
                {
                    if (AppInfoInstance == null)
                    {
                        GetAppInfoProvider(connectionName);
                    }
                }
            }
            return AppInfoInstance;
        }
        private static void GetAppInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".AppInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IAppInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                AppInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
