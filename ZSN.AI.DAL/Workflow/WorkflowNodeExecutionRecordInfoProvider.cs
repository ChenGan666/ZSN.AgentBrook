using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IWorkflowNodeExecutionRecordInfoManage WorkflowNodeExecutionRecordInfoInstance;
        private static readonly object WorkflowNodeExecutionRecordInfoLockObj = new object();
        public static IWorkflowNodeExecutionRecordInfoManage GetWorkflowNodeExecutionRecordInfo(string connectionName)
        {
            if (WorkflowNodeExecutionRecordInfoInstance == null)
            {
                lock (WorkflowNodeExecutionRecordInfoLockObj)
                {
                    if (WorkflowNodeExecutionRecordInfoInstance == null)
                    {
                        GetWorkflowNodeExecutionRecordInfoProvider(connectionName);
                    }
                }
            }
            return WorkflowNodeExecutionRecordInfoInstance;
        }
        private static void GetWorkflowNodeExecutionRecordInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".WorkflowNodeExecutionRecordInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IWorkflowNodeExecutionRecordInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                WorkflowNodeExecutionRecordInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
