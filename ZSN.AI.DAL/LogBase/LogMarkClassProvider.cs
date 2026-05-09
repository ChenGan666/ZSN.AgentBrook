using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ILogMarkClassManage LogMarkClassInstance;
        private static readonly object LogMarkClassLockObj = new object();
        public static ILogMarkClassManage GetLogMarkClass(string connectionName)
        {
            if (LogMarkClassInstance == null)
            {
                lock (LogMarkClassLockObj)
                {
                    if (LogMarkClassInstance == null)
                    {
                        GetLogMarkClassProvider(connectionName);
                    }
                }
            }
            return LogMarkClassInstance;
        }
        private static void GetLogMarkClassProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".LogMarkClassManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (ILogMarkClassManage)Activator.CreateInstance(type);
                LogMarkClassInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
