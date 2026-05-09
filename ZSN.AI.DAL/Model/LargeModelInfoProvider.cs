using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ILargeModelInfoManage LargeModelInfoInstance;
        private static readonly object LargeModelInfoLockObj = new object();
        public static ILargeModelInfoManage GetLargeModelInfo(string connectionName)
        {
            if (LargeModelInfoInstance == null)
            {
                lock (LargeModelInfoLockObj)
                {
                    if (LargeModelInfoInstance == null)
                    {
                        GetLargeModelInfoProvider(connectionName);
                    }
                }
            }
            return LargeModelInfoInstance;
        }
        private static void GetLargeModelInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".LargeModelInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (ILargeModelInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                LargeModelInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
