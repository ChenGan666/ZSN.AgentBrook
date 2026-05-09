using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IMenuInfoManage MenuInfoInstance;
        private static readonly object MenuInfoLockObj = new object();
        public static IMenuInfoManage GetMenuInfo(string connectionName)
        {
            if (MenuInfoInstance == null)
            {
                lock (MenuInfoLockObj)
                {
                    if (MenuInfoInstance == null)
                    {
                        GetMenuInfoProvider(connectionName);
                    }
                }
            }
            return MenuInfoInstance;
        }
        private static void GetMenuInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".MenuInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IMenuInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                MenuInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
