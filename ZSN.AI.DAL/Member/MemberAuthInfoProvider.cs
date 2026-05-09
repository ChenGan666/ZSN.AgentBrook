using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IMemberAuthInfoManage MemberAuthInfoInstance;
        private static readonly object MemberAuthInfoLockObj = new object();
        public static IMemberAuthInfoManage GetMemberAuthInfo(string connectionName)
        {
            if (MemberAuthInfoInstance == null)
            {
                lock (MemberAuthInfoLockObj)
                {
                    if (MemberAuthInfoInstance == null)
                    {
                        GetMemberAuthInfoProvider(connectionName);
                    }
                }
            }
            return MemberAuthInfoInstance;
        }
        private static void GetMemberAuthInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".MemberAuthInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IMemberAuthInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                MemberAuthInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
