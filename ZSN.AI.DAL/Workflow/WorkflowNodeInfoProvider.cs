using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IWorkflowNodeInfoManage WorkflowNodeInfoInstance;
        private static readonly object WorkflowNodeInfoLockObj = new object();
        public static IWorkflowNodeInfoManage GetWorkflowNodeInfo(string connectionName)
        {
            if (WorkflowNodeInfoInstance == null)
            {
                lock (WorkflowNodeInfoLockObj)
                {
                    if (WorkflowNodeInfoInstance == null)
                    {
                        GetWorkflowNodeInfoProvider(connectionName);
                    }
                }
            }
            return WorkflowNodeInfoInstance;
        }
        private static void GetWorkflowNodeInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".WorkflowNodeInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IWorkflowNodeInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                WorkflowNodeInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
