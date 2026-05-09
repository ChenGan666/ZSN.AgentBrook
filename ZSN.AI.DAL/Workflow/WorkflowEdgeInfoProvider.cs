using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IWorkflowEdgeInfoManage WorkflowEdgeInfoInstance;
        private static readonly object WorkflowEdgeInfoLockObj = new object();
        public static IWorkflowEdgeInfoManage GetWorkflowEdgeInfo(string connectionName)
        {
            if (WorkflowEdgeInfoInstance == null)
            {
                lock (WorkflowEdgeInfoLockObj)
                {
                    if (WorkflowEdgeInfoInstance == null)
                    {
                        GetWorkflowEdgeInfoProvider(connectionName);
                    }
                }
            }
            return WorkflowEdgeInfoInstance;
        }
        private static void GetWorkflowEdgeInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".WorkflowEdgeInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IWorkflowEdgeInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                WorkflowEdgeInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
