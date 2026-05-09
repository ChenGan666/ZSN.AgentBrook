using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IUserFeedbackManage UserFeedbackInstance;
        private static readonly object UserFeedbackLockObj = new object();

        public static IUserFeedbackManage GetUserFeedback(string connectionName)
        {
            if (UserFeedbackInstance == null)
            {
                lock (UserFeedbackLockObj)
                {
                    if (UserFeedbackInstance == null)
                    {
                        GetUserFeedbackProvider(connectionName);
                    }
                }
            }
            return UserFeedbackInstance;
        }

        private static void GetUserFeedbackProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL.{db.DbType}.UserFeedbackManage, ZSN.AI.DAL.{db.DbType}",
                    false, true);
                var provider = (IUserFeedbackManage)Activator.CreateInstance(type);
                provider.SetConnectionName(connectionName);
                UserFeedbackInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
