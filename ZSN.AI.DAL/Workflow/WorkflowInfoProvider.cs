using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IWorkflowInfoManage WorkflowInfoInstance;
        private static readonly object WorkflowInfoLockObj = new object();
        public static IWorkflowInfoManage GetWorkflowInfo(string connectionName)
        {
            if (WorkflowInfoInstance == null)
            {
                lock (WorkflowInfoLockObj)
                {
                    if (WorkflowInfoInstance == null)
                    {
                        GetWorkflowInfoProvider(connectionName);
                    }
                }
            }
            return WorkflowInfoInstance;
        }
        private static void GetWorkflowInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".WorkflowInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IWorkflowInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                WorkflowInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
