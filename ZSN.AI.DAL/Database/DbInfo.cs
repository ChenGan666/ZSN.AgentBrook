namespace ZSN.AI.DAL
{
    /// <summary>
    /// 快速开发框架数据库信息标准格式
    /// </summary>
    public class DbInfo
    {
        public string ConnectionName { get; set; }

        public string DbType { get; set; }

        public bool IsBaseLog { get; set; }

        public string ConnectionString { get; set; }
        public string TableNamePrefix { get; set; }

        public bool IsSqlServer => DbType.ToLower() == "sqlserver";
        
        public bool IsMyServer => DbType.ToLower() == "mysql";

        public bool IsPostgres => DbType.ToLower() == "postgres";
    }
}
