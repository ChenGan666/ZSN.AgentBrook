using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IAppChatSummaryInfoManage AppChatSummaryInfoInstance;
        private static readonly object AppChatSummaryInfoLockObj = new object();
        public static IAppChatSummaryInfoManage GetAppChatSummaryInfo(string connectionName)
        {
            if (AppChatSummaryInfoInstance == null)
            {
                lock (AppChatSummaryInfoLockObj)
                {
                    if (AppChatSummaryInfoInstance == null)
                    {
                        GetAppChatSummaryInfoProvider(connectionName);
                    }
                }
            }
            return AppChatSummaryInfoInstance;
        }
        private static void GetAppChatSummaryInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".AppChatSummaryInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IAppChatSummaryInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                AppChatSummaryInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
