using System;

namespace ZSN.AI.DAL
{
    public class DbException : Exception
    {
        public DbException(string message)
            : base(message)
        {
        }

        public DbException()
            : base("请检查appsettings.json中Dbtype节点数据库类型是否正确，例如：SqlServer、MySql、Postgres")
        {
        }
    }
}
