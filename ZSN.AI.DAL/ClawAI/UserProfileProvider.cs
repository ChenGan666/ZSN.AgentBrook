using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IUserProfileManage UserProfileInstance;
        private static readonly object UserProfileLockObj = new object();

        public static IUserProfileManage GetUserProfile(string connectionName)
        {
            if (UserProfileInstance == null)
            {
                lock (UserProfileLockObj)
                {
                    if (UserProfileInstance == null)
                    {
                        GetUserProfileProvider(connectionName);
                    }
                }
            }
            return UserProfileInstance;
        }

        private static void GetUserProfileProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL.{db.DbType}.UserProfileManage, ZSN.AI.DAL.{db.DbType}",
                    false, true);
                var provider = (IUserProfileManage)Activator.CreateInstance(type);
                provider.SetConnectionName(connectionName);
                UserProfileInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
