using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IStaffInfoManage StaffInfoInstance;
        private static readonly object StaffInfoLockObj = new object();
        public static IStaffInfoManage GetStaffInfo(string connectionName)
        {
            if (StaffInfoInstance == null)
            {
                lock (StaffInfoLockObj)
                {
                    if (StaffInfoInstance == null)
                    {
                        GetStaffInfoProvider(connectionName);
                    }
                }
            }
            return StaffInfoInstance;
        }
        private static void GetStaffInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".StaffInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IStaffInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                StaffInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
