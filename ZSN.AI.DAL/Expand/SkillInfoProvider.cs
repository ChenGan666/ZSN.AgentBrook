using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ISkillInfoManage SkillInfoInstance;
        private static readonly object SkillInfoLockObj = new object();
        public static ISkillInfoManage GetSkillInfo(string connectionName)
        {
            if (SkillInfoInstance == null)
            {
                lock (SkillInfoLockObj)
                {
                    if (SkillInfoInstance == null)
                    {
                        GetSkillInfoProvider(connectionName);
                    }
                }
            }
            return SkillInfoInstance;
        }
        private static void GetSkillInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".SkillInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (ISkillInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                SkillInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
