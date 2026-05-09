using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ILogLevelManage LogLevelInstance;
        private static readonly object LogLevelLockObj = new object();
        public static ILogLevelManage GetLogLevel(string connectionName)
        {
            if (LogLevelInstance == null)
            {
                lock (LogLevelLockObj)
                {
                    if (LogLevelInstance == null)
                    {
                        GetLogLevelProvider(connectionName);
                    }
                }
            }
            return LogLevelInstance;
        }
        private static void GetLogLevelProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".LogLevelManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (ILogLevelManage)Activator.CreateInstance(type);
                LogLevelInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
