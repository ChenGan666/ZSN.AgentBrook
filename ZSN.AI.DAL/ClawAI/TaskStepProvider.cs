using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ITaskStepManage TaskStepInstance;
        private static readonly object TaskStepLockObj = new object();

        public static ITaskStepManage GetTaskStep(string connectionName)
        {
            if (TaskStepInstance == null)
            {
                lock (TaskStepLockObj)
                {
                    if (TaskStepInstance == null)
                    {
                        GetTaskStepProvider(connectionName);
                    }
                }
            }
            return TaskStepInstance;
        }

        private static void GetTaskStepProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL.{db.DbType}.TaskStepManage, ZSN.AI.DAL.{db.DbType}",
                    false, true);
                var provider = (ITaskStepManage)Activator.CreateInstance(type);
                provider.SetConnectionName(connectionName);
                TaskStepInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
