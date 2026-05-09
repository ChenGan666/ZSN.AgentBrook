using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ITaskInfoManage TaskInfoInstance;
        private static readonly object TaskInfoLockObj = new object();
        public static ITaskInfoManage GetTaskInfo(string connectionName)
        {
            if (TaskInfoInstance == null)
            {
                lock (TaskInfoLockObj)
                {
                    if (TaskInfoInstance == null)
                    {
                        GetTaskInfoProvider(connectionName);
                    }
                }
            }
            return TaskInfoInstance;
        }
        private static void GetTaskInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".TaskInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (ITaskInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                TaskInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
