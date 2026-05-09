using System;
using System.Data;
using System.Data.Common;

using MySql.Data.MySqlClient;

// ReSharper disable once CheckNamespace
namespace ZSN.AI.DAL.MySql
{
    public class MySqlProvider : IDbProvider
    {
        public DbProviderFactory Instance()
        {
            return MySqlClientFactory.Instance;
        }

        public void DeriveParameters(IDbCommand cmd)
        {
            if ((cmd as MySqlCommand) != null)
            {
                MySqlCommandBuilder.DeriveParameters(cmd as MySqlCommand);
            }
        }

        public DbParameter MakeParam(string paramName, DbType dbType, Int32 size)
        {
            MySqlParameter param;
            
            if (size > 0)
                param = new MySqlParameter(paramName, (MySqlDbType)dbType, size);
            else
                param = new MySqlParameter(paramName, (MySqlDbType)dbType);

            return param;
        }

        public bool IsFullTextSearchEnabled()
        {
            return true;
        }

        public bool IsCompactDatabase()
        {
            return true;
        }

        public bool IsBackupDatabase()
        {
            return true;
        }

        public string GetLastIdSql()
        {
            return "select LAST_INSERT_ID();";
        }
        public bool IsDbOptimize()
        {
            return false;
        }
        public bool IsShrinkData()
        {
            return true;
        }

        public bool IsStoreProc()
        {
            return true;
        }
    }

}
