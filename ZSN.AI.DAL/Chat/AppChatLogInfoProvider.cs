using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IAppChatLogInfoManage AppChatLogInfoInstance;
        private static readonly object AppChatLogInfoLockObj = new object();
        public static IAppChatLogInfoManage GetAppChatLogInfo(string connectionName)
        {
            if (AppChatLogInfoInstance == null)
            {
                lock (AppChatLogInfoLockObj)
                {
                    if (AppChatLogInfoInstance == null)
                    {
                        GetAppChatLogInfoProvider(connectionName);
                    }
                }
            }
            return AppChatLogInfoInstance;
        }
        private static void GetAppChatLogInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".AppChatLogInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IAppChatLogInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                AppChatLogInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
