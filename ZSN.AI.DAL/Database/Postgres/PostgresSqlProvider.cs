using System;
using System.Data;
using System.Data.Common;
using Npgsql;
using NpgsqlTypes;

namespace ZSN.AI.DAL.Postgres
{
    public class PostgresSqlProvider: IDbProvider
    {
        public DbProviderFactory Instance()
        {
            return NpgsqlFactory.Instance;
        }

        public void DeriveParameters(IDbCommand cmd)
        {
            if ((cmd as NpgsqlCommand) != null)
            {
                NpgsqlCommandBuilder.DeriveParameters(cmd as NpgsqlCommand);
            }
        }

        public DbParameter MakeParam(string paramName, DbType dbType, Int32 size)
        {
            NpgsqlParameter param;

            if (size > 0)
                param = new NpgsqlParameter(paramName, (NpgsqlDbType)dbType, size);
            else
                param = new NpgsqlParameter(paramName, (NpgsqlDbType)dbType);

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
