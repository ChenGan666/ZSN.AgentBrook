using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static ICompanyInfoManage CompanyInfoInstance;
        private static readonly object CompanyInfoLockObj = new object();
        public static ICompanyInfoManage GetCompanyInfo(string connectionName)
        {
            if (CompanyInfoInstance == null)
            {
                lock (CompanyInfoLockObj)
                {
                    if (CompanyInfoInstance == null)
                    {
                        GetCompanyInfoProvider(connectionName);
                    }
                }
            }
            return CompanyInfoInstance;
        }
        private static void GetCompanyInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".CompanyInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (ICompanyInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                CompanyInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
