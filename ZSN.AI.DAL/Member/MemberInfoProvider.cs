using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IMemberInfoManage MemberInfoInstance;
        private static readonly object MemberInfoLockObj = new object();
        public static IMemberInfoManage GetMemberInfo(string connectionName)
        {
            if (MemberInfoInstance == null)
            {
                lock (MemberInfoLockObj)
                {
                    if (MemberInfoInstance == null)
                    {
                        GetMemberInfoProvider(connectionName);
                    }
                }
            }
            return MemberInfoInstance;
        }
        private static void GetMemberInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".MemberInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IMemberInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                MemberInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
