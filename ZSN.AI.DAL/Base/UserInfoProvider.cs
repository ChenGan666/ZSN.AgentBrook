using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IUserInfoManage UserInfoInstance;
        private static readonly object UserInfoLockObj = new object();
        public static IUserInfoManage GetUserInfo(string connectionName)
        {
            if (UserInfoInstance == null)
            {
                lock (UserInfoLockObj)
                {
                    if (UserInfoInstance == null)
                    {
                        GetUserInfoProvider(connectionName);
                    }
                }
            }
            return UserInfoInstance;
        }
        private static void GetUserInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".UserInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IUserInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                UserInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
