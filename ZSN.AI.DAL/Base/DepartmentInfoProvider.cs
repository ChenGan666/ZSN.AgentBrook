using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IDepartmentInfoManage DepartmentInfoInstance;
        private static readonly object DepartmentInfoLockObj = new object();
        public static IDepartmentInfoManage GetDepartmentInfo(string connectionName)
        {
            if (DepartmentInfoInstance == null)
            {
                lock (DepartmentInfoLockObj)
                {
                    if (DepartmentInfoInstance == null)
                    {
                        GetDepartmentInfoProvider(connectionName);
                    }
                }
            }
            return DepartmentInfoInstance;
        }
        private static void GetDepartmentInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".DepartmentInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IDepartmentInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                DepartmentInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
