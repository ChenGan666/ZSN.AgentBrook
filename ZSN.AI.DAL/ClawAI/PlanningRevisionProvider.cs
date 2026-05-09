using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IPlanningRevisionManage PlanningRevisionInstance;
        private static readonly object PlanningRevisionLockObj = new object();

        public static IPlanningRevisionManage GetPlanningRevision(string connectionName)
        {
            if (PlanningRevisionInstance == null)
            {
                lock (PlanningRevisionLockObj)
                {
                    if (PlanningRevisionInstance == null)
                    {
                        GetPlanningRevisionProvider(connectionName);
                    }
                }
            }
            return PlanningRevisionInstance;
        }

        private static void GetPlanningRevisionProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL.{db.DbType}.PlanningRevisionManage, ZSN.AI.DAL.{db.DbType}",
                    false, true);
                var provider = (IPlanningRevisionManage)Activator.CreateInstance(type);
                provider.SetConnectionName(connectionName);
                PlanningRevisionInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
