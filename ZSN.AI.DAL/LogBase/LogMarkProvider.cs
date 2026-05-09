using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ILogMarkManage LogMarkInstance;
        private static readonly object LogMarkLockObj = new object();
        public static ILogMarkManage GetLogMark(string connectionName)
        {
            if (LogMarkInstance == null)
            {
                lock (LogMarkLockObj)
                {
                    if (LogMarkInstance == null)
                    {
                        GetLogMarkProvider(connectionName);
                    }
                }
            }
            return LogMarkInstance;
        }
        private static void GetLogMarkProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".LogMarkManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (ILogMarkManage)Activator.CreateInstance(type);
                LogMarkInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
