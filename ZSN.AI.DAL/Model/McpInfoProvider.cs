using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IMcpInfoManage McpInfoInstance;
        private static readonly object McpInfoLockObj = new object();
        public static IMcpInfoManage GetMcpInfo(string connectionName)
        {
            if (McpInfoInstance == null)
            {
                lock (McpInfoLockObj)
                {
                    if (McpInfoInstance == null)
                    {
                        GetMcpInfoProvider(connectionName);
                    }
                }
            }
            return McpInfoInstance;
        }
        private static void GetMcpInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".McpInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IMcpInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                McpInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
