using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IMemberOtherAuthInfoManage MemberOtherAuthInfoInstance;
        private static readonly object MemberOtherAuthInfoLockObj = new object();
        public static IMemberOtherAuthInfoManage GetMemberOtherAuthInfo(string connectionName)
        {
            if (MemberOtherAuthInfoInstance == null)
            {
                lock (MemberOtherAuthInfoLockObj)
                {
                    if (MemberOtherAuthInfoInstance == null)
                    {
                        GetMemberOtherAuthInfoProvider(connectionName);
                    }
                }
            }
            return MemberOtherAuthInfoInstance;
        }
        private static void GetMemberOtherAuthInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".MemberOtherAuthInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IMemberOtherAuthInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                MemberOtherAuthInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
