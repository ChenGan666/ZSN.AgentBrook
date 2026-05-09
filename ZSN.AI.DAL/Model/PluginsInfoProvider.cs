using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.DAL;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IPluginsInfoManage PluginsInfoInstance;
        private static readonly object PluginsInfoLockObj = new object();
        public static IPluginsInfoManage GetPluginsInfo(string connectionName)
        {
            if (PluginsInfoInstance == null)
            {
                lock (PluginsInfoLockObj)
                {
                    if (PluginsInfoInstance == null)
                    {
                        GetPluginsInfoProvider(connectionName);
                    }
                }
            }
            return PluginsInfoInstance;
        }
        private static void GetPluginsInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".PluginsInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IPluginsInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                PluginsInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
