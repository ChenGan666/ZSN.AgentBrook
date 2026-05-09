using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ILogRecordManage LogRecordInstance;
        private static readonly object LogRecordLockObj = new object();
        public static ILogRecordManage GetLogRecord(string connectionName)
        {
            if (LogRecordInstance == null)
            {
                lock (LogRecordLockObj)
                {
                    if (LogRecordInstance == null)
                    {
                        GetLogRecordProvider(connectionName);
                    }
                }
            }
            return LogRecordInstance;
        }
        private static void GetLogRecordProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".LogRecordManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (ILogRecordManage)Activator.CreateInstance(type);
                LogRecordInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
