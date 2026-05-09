using System.Collections.Generic;
using System.Linq;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.DAL
{
    public class DbConfig
    {
        private static List<DbInfo> _dbInfos;

        public static List<DbInfo> GetDbInfos()
        {
            return _dbInfos ?? (_dbInfos = InitDbInfos());
        }

        public static DbInfo GetDbInfo(string connectionName)
        {
            var infos = GetDbInfos();
            return infos.First(t => t.ConnectionName == connectionName);
        }

        private static List<DbInfo> InitDbInfos()
        {
            var dbSections = ConfigHelper.GetSection("DbConnectionStrings").GetChildren();
            var infos = new List<DbInfo>();
            foreach (var section in dbSections)
            {
                var db = new DbInfo();
                db.ConnectionName = section.Key;
                db.DbType = section["DbType"];
                db.IsBaseLog = section["IsBaseLog"] == "True";
                db.ConnectionString = section["Connection"];
                db.TableNamePrefix = section["TableNamePrefix"];
                infos.Add(db);
            }
            return infos;
        }

    }
}
