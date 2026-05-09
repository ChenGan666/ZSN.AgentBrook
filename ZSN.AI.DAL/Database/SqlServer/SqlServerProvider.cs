using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

// ReSharper disable once CheckNamespace
namespace ZSN.AI.DAL.SqlServer
{
    public class SqlServerProvider : IDbProvider
    {
        public DbProviderFactory Instance()
        {
            return SqlClientFactory.Instance;
        }

        public void DeriveParameters(IDbCommand cmd)
        {
            if ((cmd as SqlCommand) != null)
            {
                SqlCommandBuilder.DeriveParameters(cmd as SqlCommand);
            }
        }

        public DbParameter MakeParam(string paramName, DbType dbType, Int32 size)
        {
            SqlParameter param;

            if (size > 0)
                param = new SqlParameter(paramName, (SqlDbType)dbType, size);
            else
                param = new SqlParameter(paramName, (SqlDbType)dbType);

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
            return "SELECT SCOPE_IDENTITY()";
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
