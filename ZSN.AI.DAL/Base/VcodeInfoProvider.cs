using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IVcodeInfoManage VcodeInfoInstance;
        private static readonly object VcodeInfoLockObj = new object();
        public static IVcodeInfoManage GetVcodeInfo(string connectionName)
        {
            if (VcodeInfoInstance == null)
            {
                lock (VcodeInfoLockObj)
                {
                    if (VcodeInfoInstance == null)
                    {
                        GetVcodeInfoProvider(connectionName);
                    }
                }
            }
            return VcodeInfoInstance;
        }
        private static void GetVcodeInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".VcodeInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IVcodeInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                VcodeInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
