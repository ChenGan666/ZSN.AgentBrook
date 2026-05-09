using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IWordTemplateInfoManage WordTemplateInfoInstance;
        private static readonly object WordTemplateInfoLockObj = new object();
        public static IWordTemplateInfoManage GetWordTemplateInfo(string connectionName)
        {
            if (WordTemplateInfoInstance == null)
            {
                lock (WordTemplateInfoLockObj)
                {
                    if (WordTemplateInfoInstance == null)
                    {
                        GetWordTemplateInfoProvider(connectionName);
                    }
                }
            }
            return WordTemplateInfoInstance;
        }
        private static void GetWordTemplateInfoProvider(string connectionName)
        {
            try
            {
                var db = DbConfig.GetDbInfo(connectionName);
                var type = Type.GetType(
                    $"ZSN.AI.DAL." + db.DbType + ".WordTemplateInfoManage, ZSN.AI.DAL." + db.DbType,
                    false, true);
                var provider = (IWordTemplateInfoManage)Activator.CreateInstance(type);
	provider.SetConnectionName(connectionName);
                WordTemplateInfoInstance = provider;
            }
            catch (Exception e)
            {
                throw new DbException();
            }
        }
    }
}
