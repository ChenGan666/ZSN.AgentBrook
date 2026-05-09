using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IAppChatSessionInfoManage AppChatSessionInfoInstance;
        private static readonly object AppChatSessionInfoLockObj = new object();
        public static IAppChatSessionInfoManage GetAppChatSessionInfo(string connectionName)
        {
            if (AppChatSessionInfoInstance == null)
            {
                lock (AppChatSessionInfoLockObj)
                {
                    if (AppChatSessionInfoInstance == null)
                    {
                        GetAppChatSessionInfoProvider(connectionName);
                    }
                }
            }
            return AppChatSessionInfoInstance;
        }
        private static void GetAppChatSessionInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".AppChatSessionInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IAppChatSessionInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                AppChatSessionInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
