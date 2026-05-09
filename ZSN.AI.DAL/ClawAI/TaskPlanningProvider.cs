using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ITaskPlanningManage TaskPlanningInstance;
        private static readonly object TaskPlanningLockObj = new object();

        public static ITaskPlanningManage GetTaskPlanning(string connectionName)
        {
            if (TaskPlanningInstance == null)
            {
                lock (TaskPlanningLockObj)
                {
                    if (TaskPlanningInstance == null)
                    {
                        GetTaskPlanningProvider(connectionName);
                    }
                }
            }
            return TaskPlanningInstance;
        }

        private static void GetTaskPlanningProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL.{db.DbType}.TaskPlanningManage, ZSN.AI.DAL.{db.DbType}",
                    false, true);
                var provider = (ITaskPlanningManage)Activator.CreateInstance(type);
                provider.SetConnectionName(connectionName);
                TaskPlanningInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
