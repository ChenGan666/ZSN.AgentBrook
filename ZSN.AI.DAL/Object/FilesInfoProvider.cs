using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IFilesInfoManage FilesInfoInstance;
        private static readonly object FilesInfoLockObj = new object();
        public static IFilesInfoManage GetFilesInfo(string connectionName)
        {
            if (FilesInfoInstance == null)
            {
                lock (FilesInfoLockObj)
                {
                    if (FilesInfoInstance == null)
                    {
                        GetFilesInfoProvider(connectionName);
                    }
                }
            }
            return FilesInfoInstance;
        }
        private static void GetFilesInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".FilesInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IFilesInfoManage)Activator.CreateInstance(type);
                provider.SetConnectionName(connectionName);
                FilesInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
