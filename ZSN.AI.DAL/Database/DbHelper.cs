using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.DAL
{

    #region 数据访问助手类  For NET CORE

    /// <summary>
    /// 数据访问助手类
    /// </summary>
    public class DbHelper
    {

        #region 私有变量

        /// <summary>
        /// DbProviderFactory实例
        /// </summary>
        private static DbProviderFactory _sqlServerFactory = null;

        private static DbProviderFactory _mySqlFactory = null;

        private static DbProviderFactory _postgresFactory = null;

        /// <summary>
        /// 数据接口
        /// </summary>
        private static IDbProvider _sqlServerProvider = null;

        private static IDbProvider _mySqlProvider = null;

        private static IDbProvider _postgresProvider = null;

        /// <summary>
        /// Parameters缓存哈希表
        /// </summary>
        private static readonly Hashtable MParamcache = Hashtable.Synchronized(new Hashtable());

        public static object LockHelper = new object();

        public static object ConnectionOpenLock = new object();

        #endregion

        #region 属性

        /// <summary>
        /// 查询次数统计
        /// </summary>
        public static int QueryCount { get; set; } = 0;

        /// <summary>
        /// IDbProvider接口
        /// </summary>
        public static IDbProvider SqlServerProvider
        {
            get
            {
                if (_sqlServerProvider == null)
                {
                    lock (LockHelper)
                    {
                        if (_sqlServerProvider == null)
                        {
                            try
                            {
                                // ReSharper disable once AssignNullToNotNullAttribute
                                _sqlServerProvider = (IDbProvider)Activator.CreateInstance(Type.GetType($"ZSN.AI.DAL.SqlServer.SqlServerProvider, ZSN.AI.DAL", false, true));
                            }
                            catch
                            {
                                throw new DbException();
                            }

                        }
                    }
                }
                return _sqlServerProvider;
            }
        }

        public static IDbProvider MySqlProvider
        {
            get
            {
                if (_mySqlProvider == null)
                {
                    lock (LockHelper)
                    {
                        if (_mySqlProvider == null)
                        {
                            try
                            {
                                // ReSharper disable once AssignNullToNotNullAttribute
                                _mySqlProvider = (IDbProvider)Activator.CreateInstance(Type.GetType($"ZSN.AI.DAL.MySql.MySqlProvider, ZSN.AI.DAL", false, true));
                            }
                            catch
                            {
                                throw new DbException();
                            }

                        }
                    }
                }
                return _mySqlProvider;
            }
        }

        public static IDbProvider PostgresProvider
        {
            get
            {
                if (_postgresProvider == null)
                {
                    lock (LockHelper)
                    {
                        if (_postgresProvider == null)
                        {
                            try
                            {
                                var _type = Type.GetType($"ZSN.AI.DAL.Postgres.PostgresSqlProvider, ZSN.AI.DAL", false, true);
                                _postgresProvider = (IDbProvider)Activator.CreateInstance(_type);
                            }
                            catch
                            {
                                throw new DbException();
                            }

                        }
                    }
                }
                return _postgresProvider;
            }
        }

        /// <summary>
        /// DbFactory实例
        /// </summary>
        public static DbProviderFactory SqlServerFactory
        {
            get
            {
                if (_sqlServerFactory == null)
                {
                    _sqlServerFactory = SqlServerProvider.Instance();
                }
                return _sqlServerFactory;
            }
        }

        public static DbProviderFactory MySqlFactory
        {
            get
            {
                if (_mySqlFactory == null)
                {
                    _mySqlFactory = MySqlProvider.Instance();
                }
                return _mySqlFactory;
            }
        }
        public static DbProviderFactory PostgresFactory
        {
            get
            {
                if (_postgresFactory == null)
                {
                    _postgresFactory = PostgresProvider.Instance();
                }
                return _postgresFactory;
            }
        }
        

        public static IDbProvider GetProvider(DbInfo db)
        {
            if (db.IsSqlServer)
                return SqlServerProvider;
            if (db.IsMyServer)
                return MySqlProvider; 
            if (db.IsPostgres)
                return PostgresProvider;
            else
                throw new DbException($"暂不支持{db.DbType}类型数据库");
        }

        public static DbProviderFactory GetFactory(DbInfo db)
        {
            if (db.IsSqlServer)
                return SqlServerFactory;
            if (db.IsMyServer)
                return MySqlFactory; 
            if (db.IsPostgres)
                return PostgresFactory;
            else
                throw new DbException($"暂不支持{db.DbType}类型数据库");
        }

        /// <summary>
        /// 刷新数据库提供者
        /// </summary>
        public static void ResetDbProvider()
        {
            DatabaseProvider.ResetDbProvider();
            _sqlServerProvider = null;
            _sqlServerFactory = null;
            _mySqlProvider = null;
            _mySqlFactory = null;
            _postgresProvider = null;
            _postgresFactory = null;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将DbParameter参数数组(参数值)分配给DbCommand命令.
        /// 这个方法将给任何一个参数分配DBNull.Value;
        /// 该操作将阻止默认值的使用.
        /// </summary>
        /// <param name="command">命令名</param>
        /// <param name="commandParameters">DbParameters数组</param>
        private static void AttachParameters(DbCommand command, DbParameter[] commandParameters)
        {
            if (command == null) throw new ArgumentNullException("command");
            if (commandParameters != null)
            {
                foreach (DbParameter p in commandParameters)
                {
                    if (p != null)
                    {
                        // 检查未分配值的输出参数,将其分配以DBNull.Value.
                        if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) &&
                            (p.Value == null))
                        {
                            p.Value = DBNull.Value;
                        }
                        command.Parameters.Add(p);
                    }
                }
            }
        }

        /// <summary>
        /// 将DataRow类型的列值分配到DbParameter参数数组.
        /// </summary>
        /// <param name="commandParameters">要分配值的DbParameter参数数组</param>
        /// <param name="dataRow">将要分配给存储过程参数的DataRow</param>
        private static void AssignParameterValues(DbParameter[] commandParameters, DataRow dataRow)
        {
            if ((commandParameters == null) || (dataRow == null))
            {
                return;
            }

            int i = 0;
            // 设置参数值
            foreach (DbParameter commandParameter in commandParameters)
            {
                // 创建参数名称,如果不存在,只抛出一个异常.
                if (commandParameter.ParameterName == null ||
                    commandParameter.ParameterName.Length <= 1)
                    throw new Exception(
                        string.Format("请提供参数{0}一个有效的名称{1}.", i, commandParameter.ParameterName));
                // 从dataRow的表中获取为参数数组中数组名称的列的索引.
                // 如果存在和参数名称相同的列,则将列值赋给当前名称的参数.
                if (dataRow.Table.Columns.IndexOf(commandParameter.ParameterName.Substring(1)) != -1)
                    commandParameter.Value = dataRow[commandParameter.ParameterName.Substring(1)];
                i++;
            }
        }

        /// <summary>
        /// 将一个对象数组分配给DbParameter参数数组.
        /// </summary>
        /// <param name="commandParameters">要分配值的DbParameter参数数组</param>
        /// <param name="parameterValues">将要分配给存储过程参数的对象数组</param>
        private static void AssignParameterValues(DbParameter[] commandParameters, object[] parameterValues)
        {
            if ((commandParameters == null) || (parameterValues == null))
            {
                return;
            }

            // 确保对象数组个数与参数个数匹配,如果不匹配,抛出一个异常.
            if (commandParameters.Length != parameterValues.Length)
            {
                throw new ArgumentException("参数值个数与参数不匹配.");
            }

            // 给参数赋值
            for (int i = 0, j = commandParameters.Length; i < j; i++)
            {
                // If the current array value derives from IDbDataParameter, then assign its Value property
                if (parameterValues[i] is IDbDataParameter)
                {
                    IDbDataParameter paramInstance = (IDbDataParameter)parameterValues[i];
                    if (paramInstance.Value == null)
                    {
                        commandParameters[i].Value = DBNull.Value;
                    }
                    else
                    {
                        commandParameters[i].Value = paramInstance.Value;
                    }
                }
                else if (parameterValues[i] == null)
                {
                    commandParameters[i].Value = DBNull.Value;
                }
                else
                {
                    commandParameters[i].Value = parameterValues[i];
                }
            }
        }

        /// <summary>
        /// 预处理用户提供的命令,数据库连接/事务/命令类型/参数
        /// </summary>
        /// <param name="command">要处理的DbCommand</param>
        /// <param name="connection">数据库连接</param>
        /// <param name="transaction">一个有效的事务或者是null值</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本, 其它.)</param>
        /// <param name="commandText">存储过程名或都SQL命令文本</param>
        /// <param name="commandParameters">和命令相关联的DbParameter参数数组,如果没有参数为'null'</param>
        /// <param name="mustCloseConnection"><c>true</c> 如果连接是打开的,则为true,其它情况下为false.</param>
        private static void PrepareCommand(DbCommand command, DbConnection connection, DbTransaction transaction, CommandType commandType, string commandText, DbParameter[] commandParameters, out bool mustCloseConnection)
        {
            if (command == null) throw new ArgumentNullException("command");
            if (commandText == null || commandText.Length == 0) throw new ArgumentNullException("commandText");

            // If the provided connection is not open, we will open it
            if (connection.State != ConnectionState.Open)
            {
                mustCloseConnection = true;
                connection.Open();
            }
            else
            {
                mustCloseConnection = false;
            }

            // 给命令分配一个数据库连接.
            command.Connection = connection;

            // 设置命令文本(存储过程名或SQL语句)
            command.CommandText = commandText;

            // 分配事务
            if (transaction != null)
            {
                if (transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
                command.Transaction = transaction;
            }

            // 设置命令类型.
            command.CommandType = commandType;

            // 分配命令参数
            if (commandParameters != null)
            {
                AttachParameters(command, commandParameters);
            }
            return;
        }

        /// <summary>
        /// 探索运行时的存储过程,返回DbParameter参数数组.
        /// 初始化参数值为 DBNull.Value.
        /// </summary>
        /// <param name="connection">一个有效的数据库连接</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="includeReturnValueParameter">是否包含返回值参数</param>
        /// <returns>返回DbParameter参数数组</returns>
        private static DbParameter[] DiscoverSpParameterSet(DbInfo db, DbConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            DbCommand cmd = connection.CreateCommand();
            cmd.CommandTimeout = 0;
            cmd.CommandText = spName;
            cmd.CommandType = CommandType.StoredProcedure;

            connection.Open();
            // 检索cmd指定的存储过程的参数信息,并填充到cmd的Parameters参数集中.
            GetProvider(db).DeriveParameters(cmd);
            connection.Close();
            // 如果不包含返回值参数,将参数集中的每一个参数删除.
            if (!includeReturnValueParameter)
            {
                cmd.Parameters.RemoveAt(0);
            }

            // 创建参数数组
            DbParameter[] discoveredParameters = new DbParameter[cmd.Parameters.Count];
            // 将cmd的Parameters参数集复制到discoveredParameters数组.
            cmd.Parameters.CopyTo(discoveredParameters, 0);

            // 初始化参数值为 DBNull.Value.
            foreach (DbParameter discoveredParameter in discoveredParameters)
            {
                discoveredParameter.Value = DBNull.Value;
            }
            return discoveredParameters;
        }

        /// <summary>
        /// DbParameter参数数组的深层拷贝.
        /// </summary>
        /// <param name="originalParameters">原始参数数组</param>
        /// <returns>返回一个同样的参数数组</returns>
        private static DbParameter[] CloneParameters(DbParameter[] originalParameters)
        {
            DbParameter[] clonedParameters = new DbParameter[originalParameters.Length];

            for (int i = 0, j = originalParameters.Length; i < j; i++)
            {
                clonedParameters[i] = (DbParameter)((ICloneable)originalParameters[i]).Clone();
            }

            return clonedParameters;
        }

        #endregion 私有方法结束

        #region ExecuteNonQuery方法

        /// <summary>
        /// 执行指定连接字符串,类型的DbCommand.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery("SELECT * FROM [table123]");
        /// </remarks>
        /// <param name="db"></param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回命令影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, string commandText)
        {
            return ExecuteNonQuery(db, CommandType.Text, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定连接字符串,类型的DbCommand.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery("SELECT * FROM [table123]");
        /// </remarks>
        /// <param name="id"></param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回命令影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, out int id, string commandText)
        {
            return ExecuteNonQuery(db, out id, CommandType.Text, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定连接字符串,类型的DbCommand.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders");
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本, 其它.)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回命令影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(db, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定连接字符串,并返回刚插入的自增ID
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders");
        /// </remarks>
        /// <param name="commandType">命令类型 (存储过程,命令文本, 其它.)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回命令影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, out int id, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(db, out id, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定连接字符串,类型的DbCommand.如果没有提供参数,不返回结果.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本, 其它.)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="commandParameters">DbParameter参数数组</param>
        /// <returns>返回命令影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(db.ConnectionString))
                throw new ArgumentNullException("ConnectionString");

            using (DbConnection connection = GetFactory(db).CreateConnection())
            {
                connection.ConnectionString = db.ConnectionString;
                connection.Open();

                return ExecuteNonQuery(db, connection, commandType, commandText, commandParameters);
            }
        }

        /// <summary>
        /// 执行指定连接字符串并返回刚插入的自增ID,类型的DbCommand.如果没有提供参数,不返回结果.
        /// </summary>
        /// <param name="commandType">命令类型 (存储过程,命令文本, 其它.)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="commandParameters">DbParameter参数数组</param>
        /// <returns>返回命令影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, out int id, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(db.ConnectionString))
                throw new ArgumentNullException("ConnectionString");

            using (DbConnection connection = GetFactory(db).CreateConnection())
            {
                connection.ConnectionString = db.ConnectionString;
                connection.Open();

                return ExecuteNonQuery(db, out id, connection, commandType, commandText, commandParameters);
            }
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令 
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders");
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型(存储过程,命令文本或其它.)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(db, connection, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令并返回自增ID 
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders");
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型(存储过程,命令文本或其它.)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, out int id, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(db, out id, connection, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型(存储过程,命令文本或其它.)</param>
        /// <param name="commandText">T存储过程名称或SQL语句</param>
        /// <param name="commandParameters">SqlParamter参数数组</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, DbConnection connection, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (connection == null) 
                throw new ArgumentNullException("connection");

            // 记录sql
            DbLoger.AddLog(commandText);

            // 创建DbCommand命令,并进行预处理
            DbCommand cmd = GetFactory(db).CreateCommand();
            cmd.CommandTimeout = 0;
            bool mustCloseConnection = false;

            PrepareCommand(cmd, connection, (DbTransaction)null, commandType, commandText, commandParameters, out mustCloseConnection);

            // Finally, execute the command
            int retval = cmd.ExecuteNonQuery();

            // 清除参数,以便再次使用.
            cmd.Parameters.Clear();
            if (mustCloseConnection)
                connection.Close();
            return retval;
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型(存储过程,命令文本或其它.)</param>
        /// <param name="commandText">T存储过程名称或SQL语句</param>
        /// <param name="commandParameters">SqlParamter参数数组</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, out int id, DbConnection connection, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (GetProvider(db).GetLastIdSql().Trim() == "")
                throw new ArgumentNullException("GetLastIdSql is \"\"");

            // 记录sql
            DbLoger.AddLog(commandText);

            // 创建DbCommand命令,并进行预处理
            DbCommand cmd = GetFactory(db).CreateCommand();
            cmd.CommandTimeout = 0;
            bool mustCloseConnection = false;
            PrepareCommand(cmd, connection, (DbTransaction)null, commandType, commandText, commandParameters, out mustCloseConnection);

            // 执行命令
            int retval = cmd.ExecuteNonQuery();
            // 清除参数,以便再次使用.
            cmd.Parameters.Clear();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = GetProvider(db).GetLastIdSql();

            id = int.Parse(cmd.ExecuteScalar().ToString());

            QueryCount++;


            if (mustCloseConnection)
            {
                connection.Close();
            }
            return retval;
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,将对象数组的值赋给存储过程参数.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输出参数和返回值
        /// 示例:  
        ///  int result = ExecuteNonQuery(conn, "PublishOrders", 24, 36);
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名</param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException("spName");

            // 如果有参数值
            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                // 从缓存中加载存储过程参数
                DbParameter[] commandParameters = GetSpParameterSet(db, connection, spName);

                // 给存储过程分配参数值
                AssignParameterValues(commandParameters, parameterValues);

                return ExecuteNonQuery(db, connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return ExecuteNonQuery(db, connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行带事务的DbCommand.
        /// </summary>
        /// <remarks>
        /// 示例.:  
        ///  int result = ExecuteNonQuery(trans, CommandType.StoredProcedure, "PublishOrders");
        /// </remarks>
        /// <param name="transaction">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型(存储过程,命令文本或其它.)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回影响的行数/returns>
        public static int ExecuteNonQuery(DbInfo db, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(db, transaction, commandType, commandText, (DbParameter[])null);
        }


        /// <summary>
        /// 执行带事务的DbCommand.
        /// </summary>
        /// <remarks>
        /// 示例.:  
        ///  int result = ExecuteNonQuery(trans, CommandType.StoredProcedure, "PublishOrders");
        /// </remarks>
        /// <param name="transaction">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型(存储过程,命令文本或其它.)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回影响的行数/returns>
        public static int ExecuteNonQuery(DbInfo db, out int id, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteNonQuery(db, out id, transaction, commandType, commandText, (DbParameter[])null);
        }


        /// <summary>
        /// 执行带事务的DbCommand(指定参数).
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery(trans, CommandType.StoredProcedure, "GetOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型(存储过程,命令文本或其它.)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="commandParameters">SqlParamter参数数组</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, DbTransaction transaction, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");

            // 记录sql
            DbLoger.AddLog(commandText);

            // 预处理
            DbCommand cmd = GetFactory(db).CreateCommand();
            cmd.CommandTimeout = 0;
            bool mustCloseConnection = false;
            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

            // 执行
            int retval = cmd.ExecuteNonQuery();

            // 清除参数集,以便再次使用.
            cmd.Parameters.Clear();
            return retval;
        }

        /// <summary>
        /// 执行带事务的DbCommand(指定参数).
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int result = ExecuteNonQuery(trans, CommandType.StoredProcedure, "GetOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型(存储过程,命令文本或其它.)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="commandParameters">SqlParamter参数数组</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, out int id, DbTransaction transaction, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");

            // 记录sql
            DbLoger.AddLog(commandText);

            // 预处理
            DbCommand cmd = GetFactory(db).CreateCommand();
            cmd.CommandTimeout = 0;
            bool mustCloseConnection = false;
            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

            // 执行
            int retval = cmd.ExecuteNonQuery();
            // 清除参数,以便再次使用.
            cmd.Parameters.Clear();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = GetProvider(db).GetLastIdSql();
            id = int.Parse(cmd.ExecuteScalar().ToString());
            return retval;
        }

        /// <summary>
        /// 执行带事务的DbCommand(指定参数值).
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输出参数和返回值
        /// 示例:  
        ///  int result = ExecuteNonQuery(conn, trans, "PublishOrders", 24, 36);
        /// </remarks>
        /// <param name="transaction">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名</param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        /// <returns>返回受影响的行数</returns>
        public static int ExecuteNonQuery(DbInfo db, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果有参数值
            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, transaction.Connection, spName);

                // 给存储过程参数赋值
                AssignParameterValues(commandParameters, parameterValues);

                // 调用重载方法
                return ExecuteNonQuery(db, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // 没有参数值
                return ExecuteNonQuery(db, transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteNonQuery方法结束

        #region ExecuteCommandWithSplitter方法
        /// <summary>
        /// 运行含有GO命令的多条SQL命令
        /// </summary>
        /// <param name="commandText">SQL命令字符串</param>
        /// <param name="splitter">分割字符串</param>
        public static void ExecuteCommandWithSplitter(DbInfo db, string commandText, string splitter)
        {
            int startPos = 0;

            do
            {
                int lastPos = commandText.IndexOf(splitter, startPos);
                int len = (lastPos > startPos ? lastPos : commandText.Length) - startPos;
                string query = commandText.Substring(startPos, len);

                if (query.Trim().Length > 0)
                {
                    try
                    {
                        ExecuteNonQuery(db, CommandType.Text, query);
                    }
                    catch {; }
                }

                if (lastPos == -1)
                    break;
                else
                    startPos = lastPos + splitter.Length;
            } while (startPos < commandText.Length);

        }

        /// <summary>
        /// 运行含有GO命令的多条SQL命令
        /// </summary>
        /// <param name="commandText">SQL命令字符串</param>
        public static void ExecuteCommandWithSplitter(DbInfo db, string commandText)
        {
            ExecuteCommandWithSplitter(db, commandText, "\r\nGO\r\n");
        }

        #endregion ExecuteCommandWithSplitter方法结束

        #region ExecuteDataset方法


        /// <summary>
        /// 执行指定数据库连接字符串的命令,返回DataSet.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DataSet ds = ExecuteDataset("SELECT * FROM [table1]");
        /// </remarks>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回一个包含结果集的DataSet</returns>
        public static DataSet ExecuteDataset(DbInfo db, string commandText)
        {
            return ExecuteDataset(db, CommandType.Text, commandText, (DbParameter[])null);
        }


        /// <summary>
        /// 执行指定数据库连接字符串的命令,返回DataSet.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DataSet ds = ExecuteDataset(connString, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回一个包含结果集的DataSet</returns>
        public static DataSet ExecuteDataset(DbInfo db, CommandType commandType, string commandText)
        {
            return ExecuteDataset(db, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定数据库连接字符串的命令,返回DataSet.
        /// </summary>
        /// <remarks>
        /// 示例: 
        ///  DataSet ds = ExecuteDataset(connString, CommandType.StoredProcedure, "GetOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="commandParameters">SqlParamters参数数组</param>
        /// <returns>返回一个包含结果集的DataSet</returns>
        public static DataSet ExecuteDataset(DbInfo db, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(db.ConnectionString))
                throw new ArgumentNullException("ConnectionString");

            // 创建并打开数据库连接对象,操作完成释放对象.

            //using (DbConnection connection = (DbConnection)new System.Data.SqlClient.SqlConnection(ConnectionString))
            using (DbConnection connection = GetFactory(db).CreateConnection())
            {
                connection.ConnectionString = db.ConnectionString;
                
                // 处理MySQL连接池初始化时的并发冲突，使用重试机制
                int retryCount = 0;
                const int maxRetries = 3;
                
                lock (ConnectionOpenLock)
                {
                    while (retryCount < maxRetries)
                    {
                        try
                        {
                            connection.Open();
                            break; // 成功则跳出循环
                        }
                        catch (ArgumentException ex) when (ex.Message.Contains("An item with the same key has already been added"))
                        {
                            retryCount++;
                            NLogHelper.WriteCustom($"MySQL连接并发冲突重试 {retryCount}/{maxRetries}: {ex.Message}", "/DB/Concurrent/");
                            if (retryCount >= maxRetries)
                            {
                                throw; // 达到最大重试次数，抛出异常
                            }
                            // 指数退避策略，等待后重试
                            System.Threading.Thread.Sleep(50 * retryCount);
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("Operations that change non-concurrent collections must have exclusive access"))
                        {
                            retryCount++;
                            NLogHelper.WriteCustom($"MySQL集合并发访问重试 {retryCount}/{maxRetries}: {ex.Message}", "/DB/Concurrent/");
                            if (retryCount >= maxRetries)
                            {
                                throw; // 达到最大重试次数，抛出异常
                            }
                            // 指数退避策略，等待后重试
                            System.Threading.Thread.Sleep(50 * retryCount);
                        }
                    }
                }

                // 调用指定数据库连接字符串重载方法.
                return ExecuteDataset(db, connection, commandType, commandText, commandParameters);
            }
        }

        ///// <summary>
        ///// 执行指定数据库连接字符串的命令,直接提供参数值,返回DataSet.
        ///// </summary>
        ///// <remarks>
        ///// 此方法不提供访问存储过程输出参数和返回值.
        ///// 示例: 
        /////  DataSet ds = ExecuteDataset(connString, "GetOrders", 24, 36);
        ///// </remarks>
        ///// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        ///// <param name="spName">存储过程名</param>
        ///// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        ///// <returns>返回一个包含结果集的DataSet</returns>
        //public static DataSet ExecuteDataset(string spName, params object[] parameterValues)
        //{
        //    if (db.ConnectionString == null || db.ConnectionString.Length == 0) throw new ArgumentNullException("ConnectionString");
        //    if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

        //    if ((parameterValues != null) && (parameterValues.Length > 0))
        //    {
        //        // 从缓存中检索存储过程参数
        //        DbParameter[] commandParameters = GetSpParameterSet(spName);

        //        // 给存储过程参数分配值
        //        AssignParameterValues(commandParameters, parameterValues);

        //        return ExecuteDataset(ConnectionString, CommandType.StoredProcedure, spName, commandParameters);
        //    }
        //    else
        //    {
        //        return ExecuteDataset(ConnectionString, CommandType.StoredProcedure, spName);
        //    }
        //}

        /// <summary>
        /// 执行指定数据库连接对象的命令,返回DataSet.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DataSet ds = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名或SQL语句</param>
        /// <returns>返回一个包含结果集的DataSet</returns>
        public static DataSet ExecuteDataset(DbInfo db, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteDataset(db, connection, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,指定存储过程参数,返回DataSet.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DataSet ds = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名或SQL语句</param>
        /// <param name="commandParameters">SqlParamter参数数组</param>
        /// <returns>返回一个包含结果集的DataSet</returns>
        public static DataSet ExecuteDataset(DbInfo db, DbConnection connection, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            // 记录sql
            DbLoger.AddLog(commandText);

            // 预处理
            DbCommand cmd = GetFactory(db).CreateCommand();
            cmd.CommandTimeout = 0;
            bool mustCloseConnection = false;
            PrepareCommand(cmd, connection, (DbTransaction)null, commandType, commandText, commandParameters, out mustCloseConnection);

            // 创建DbDataAdapter和DataSet.
            using (DbDataAdapter da = GetFactory(db).CreateDataAdapter())
            {
                da.SelectCommand = cmd;
                DataSet ds = new DataSet();

                // Lock wait timeout 重试机制
                int lockRetryCount = 0;
                const int maxLockRetries = 3;
                while (true)
                {
                    try
                    {
                        da.Fill(ds);
                        break;
                    }
                    catch (MySqlException ex) when (ex.Number == 1205 && lockRetryCount < maxLockRetries)
                    {
                        lockRetryCount++;
                        NLogHelper.WriteCustom($"MySQL Lock wait timeout 重试 {lockRetryCount}/{maxLockRetries}: {ex.Message}", "/DB/LockRetry/");
                        System.Threading.Thread.Sleep(200 * lockRetryCount);
                    }
                }

                QueryCount++;

                cmd.Parameters.Clear();

                if (mustCloseConnection)
                    connection.Close();

                return ds;
            }
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,指定参数值,返回DataSet.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输入参数和返回值.
        /// 示例.:  
        ///  DataSet ds = ExecuteDataset(conn, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名</param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        /// <returns>返回一个包含结果集的DataSet</returns>
        public static DataSet ExecuteDataset(DbInfo db, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (string.IsNullOrEmpty(spName))
                throw new ArgumentNullException("spName");

            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                // 比缓存中加载存储过程参数
                DbParameter[] commandParameters = GetSpParameterSet(db, connection, spName);

                // 给存储过程参数分配值
                AssignParameterValues(commandParameters, parameterValues);

                return ExecuteDataset(db, connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return ExecuteDataset(db, connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行指定事务的命令,返回DataSet.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DataSet ds = ExecuteDataset(trans, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="transaction">事务</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名或SQL语句</param>
        /// <returns>返回一个包含结果集的DataSet</returns>
        public static DataSet ExecuteDataset(DbInfo db, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteDataset(db, transaction, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定事务的命令,指定参数,返回DataSet.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DataSet ds = ExecuteDataset(trans, CommandType.StoredProcedure, "GetOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">事务</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名或SQL语句</param>
        /// <param name="commandParameters">SqlParamter参数数组</param>
        /// <returns>返回一个包含结果集的DataSet</returns>
        public static DataSet ExecuteDataset(DbInfo db, DbTransaction transaction, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");

            // 记录sql
            DbLoger.AddLog(commandText);

            // 预处理
            DbCommand cmd = GetFactory(db).CreateCommand();
            cmd.CommandTimeout = 0;
            bool mustCloseConnection = false;
            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

            // 创建 DataAdapter & DataSet
            using (DbDataAdapter da = GetFactory(db).CreateDataAdapter())
            {
                da.SelectCommand = cmd;
                DataSet ds = new DataSet();
                da.Fill(ds);
                cmd.Parameters.Clear();
                return ds;
            }
        }

        /// <summary>
        /// 执行指定事务的命令,指定参数值,返回DataSet.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输入参数和返回值.
        /// 示例.:  
        ///  DataSet ds = ExecuteDataset(trans, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="transaction">事务</param>
        /// <param name="spName">存储过程名</param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        /// <returns>返回一个包含结果集的DataSet</returns>
        public static DataSet ExecuteDataset(DbInfo db, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                // 从缓存中加载存储过程参数
                DbParameter[] commandParameters = GetSpParameterSet(db, transaction.Connection, spName);

                // 给存储过程参数分配值
                AssignParameterValues(commandParameters, parameterValues);

                return ExecuteDataset(db, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return ExecuteDataset(db, transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteDataset数据集命令结束

        #region ExecuteReader 数据阅读器

        /// <summary>
        /// 枚举,标识数据库连接是由BaseDbHelper提供还是由调用者提供
        /// </summary>
        private enum DbConnectionOwnership
        {
            /// <summary>由BaseDbHelper提供连接</summary>
            Internal,
            /// <summary>由调用者提供连接</summary>
            External
        }

        /// <summary>
        /// 执行指定数据库连接对象的数据阅读器.
        /// </summary>
        /// <remarks>
        /// 如果是BaseDbHelper打开连接,当连接关闭DataReader也将关闭.
        /// 如果是调用都打开连接,DataReader由调用都管理.
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="transaction">一个有效的事务,或者为 'null'</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名或SQL语句</param>
        /// <param name="commandParameters">DbParameters参数数组,如果没有参数则为'null'</param>
        /// <param name="connectionOwnership">标识数据库连接对象是由调用者提供还是由BaseDbHelper提供</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        private static DbDataReader ExecuteReader(DbInfo db, DbConnection connection, DbTransaction transaction, CommandType commandType, string commandText, DbParameter[] commandParameters, DbConnectionOwnership connectionOwnership)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            // 记录sql
            DbLoger.AddLog(commandText);

            bool mustCloseConnection = false;
            // 创建命令
            DbCommand cmd = GetFactory(db).CreateCommand();
            cmd.CommandTimeout = 0;
            try
            {
                PrepareCommand(cmd, connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

                // 创建数据阅读器
                DbDataReader dataReader;

                if (connectionOwnership == DbConnectionOwnership.External)
                {
                    dataReader = cmd.ExecuteReader();
                }
                else
                {
                    dataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }

                QueryCount++;
                // 清除参数,以便再次使用..
                // HACK: There is a problem here, the output parameter values are fletched 
                // when the reader is closed, so if the parameters are detached from the command
                // then the SqlReader can磘 set its values. 
                // When this happen, the parameters can磘 be used again in other command.
                bool canClear = true;
                foreach (DbParameter commandParameter in cmd.Parameters)
                {
                    if (commandParameter.Direction != ParameterDirection.Input)
                        canClear = false;
                }

                if (canClear)
                {
                    //cmd.Dispose();
                    cmd.Parameters.Clear();
                }


                return dataReader;
            }
            catch
            {
                if (mustCloseConnection)
                    connection.Close();
                throw;
            }
        }

        /// <summary>
        /// 执行指定数据库连接字符串的数据阅读器.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DbDataReader dr = ExecuteReader(connString, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名或SQL语句</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReader(DbInfo db, CommandType commandType, string commandText)
        {
            return ExecuteReader(db, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定数据库连接字符串的数据阅读器,指定参数.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DbDataReader dr = ExecuteReader(connString, CommandType.StoredProcedure, "GetOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名或SQL语句</param>
        /// <param name="commandParameters">SqlParamter参数数组(new DbParameter("@prodid", 24))</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReader(DbInfo db, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(db.ConnectionString)) throw new ArgumentNullException("ConnectionString");
            DbConnection connection = null;
            try
            {
                connection = GetFactory(db).CreateConnection();
                connection.ConnectionString = db.ConnectionString;
                connection.Open();

                return ExecuteReader(db, connection, null, commandType, commandText, commandParameters, DbConnectionOwnership.Internal);
            }
            catch
            {
                // If we fail to return the SqlDatReader, we need to close the connection ourselves
                if (connection != null) connection.Close();
                throw;
            }

        }

        /// <summary>
        /// 执行指定数据库连接字符串的数据阅读器,指定参数值.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输出参数和返回值参数.
        /// 示例:  
        ///  DbDataReader dr = ExecuteReader(connString, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="spName">存储过程名</param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReader(DbInfo db, string spName, params object[] parameterValues)
        {
            if (string.IsNullOrEmpty(db.ConnectionString))
                throw new ArgumentNullException("ConnectionString");
            if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException("spName");

            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                DbParameter[] commandParameters = GetSpParameterSet(db, spName);

                AssignParameterValues(commandParameters, parameterValues);

                return ExecuteReader(db, db.ConnectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return ExecuteReader(db, db.ConnectionString, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行指定数据库连接对象的数据阅读器.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DbDataReader dr = ExecuteReader(conn, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名或SQL语句</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReader(DbInfo db, DbConnection connection, CommandType commandType, string commandText)
        {
            return ExecuteReader(db, connection, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// [调用者方式]执行指定数据库连接对象的数据阅读器,指定参数.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DbDataReader dr = ExecuteReader(conn, CommandType.StoredProcedure, "GetOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandParameters">SqlParamter参数数组</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReader(DbInfo db, DbConnection connection, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            return ExecuteReader(db, connection, (DbTransaction)null, commandType, commandText, commandParameters, DbConnectionOwnership.External);
        }

        /// <summary>
        /// [调用者方式]执行指定数据库连接对象的数据阅读器,指定参数值.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输出参数和返回值参数.
        /// 示例:  
        ///  DbDataReader dr = ExecuteReader(conn, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">T存储过程名</param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReader(DbInfo db, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                DbParameter[] commandParameters = GetSpParameterSet(db, connection, spName);

                AssignParameterValues(commandParameters, parameterValues);

                return ExecuteReader(db, connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return ExecuteReader(db, connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// [调用者方式]执行指定数据库事务的数据阅读器,指定参数值.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DbDataReader dr = ExecuteReader(trans, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="transaction">一个有效的连接事务</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReader(DbInfo db, DbTransaction transaction, CommandType commandType, string commandText)
        {
            return ExecuteReader(db, transaction, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// [调用者方式]执行指定数据库事务的数据阅读器,指定参数.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///   DbDataReader dr = ExecuteReader(trans, CommandType.StoredProcedure, "GetOrders", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">一个有效的连接事务</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="commandParameters">分配给命令的SqlParamter参数数组</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReader(DbInfo db, DbTransaction transaction, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");

            return ExecuteReader(db, transaction.Connection, transaction, commandType, commandText, commandParameters, DbConnectionOwnership.External);
        }

        /// <summary>
        /// [调用者方式]执行指定数据库事务的数据阅读器,指定参数值.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输出参数和返回值参数.
        /// 
        /// 示例:  
        ///  DbDataReader dr = ExecuteReader(trans, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="transaction">一个有效的连接事务</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReader(DbInfo db, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果有参数值
            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                DbParameter[] commandParameters = GetSpParameterSet(db, transaction.Connection, spName);

                AssignParameterValues(commandParameters, parameterValues);

                return ExecuteReader(db, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // 没有参数值
                return ExecuteReader(db, transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteReader数据阅读器

        #region ExecuteScalar 返回结果集中的第一行第一列

        /// <summary>
        /// 执行指定数据库连接字符串的命令,返回结果集中的第一行第一列.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int orderCount = (int)ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount");
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalar(DbInfo db, CommandType commandType, string commandText)
        {
            // 执行参数为空的方法
            return ExecuteScalar(db, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定数据库连接字符串的命令,指定参数,返回结果集中的第一行第一列.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int orderCount = (int)ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="commandParameters">分配给命令的SqlParamter参数数组</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalar(DbInfo db, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(db.ConnectionString))
                throw new ArgumentNullException("ConnectionString");
            // 创建并打开数据库连接对象,操作完成释放对象.
            using (DbConnection connection = GetFactory(db).CreateConnection())
            {
                connection.ConnectionString = db.ConnectionString;
                connection.Open();
                // 调用指定数据库连接字符串重载方法.
                return ExecuteScalar(db, connection, commandType, commandText, commandParameters);
            }
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,返回结果集中的第一行第一列.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount");
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalar(DbInfo db, DbConnection connection, CommandType commandType, string commandText)
        {
            // 执行参数为空的方法
            return ExecuteScalar(db, connection, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,指定参数,返回结果集中的第一行第一列.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="commandParameters">分配给命令的SqlParamter参数数组</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalar(DbInfo db, DbConnection connection, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            // 记录sql
            DbLoger.AddLog(commandText);

            // 创建DbCommand命令,并进行预处理
            DbCommand cmd = GetFactory(db).CreateCommand();
            cmd.CommandTimeout = 0;
            bool mustCloseConnection = false;
            PrepareCommand(cmd, connection, (DbTransaction)null, commandType, commandText, commandParameters, out mustCloseConnection);

            // 执行DbCommand命令,并返回结果.
            object retval = cmd.ExecuteScalar();

            // 清除参数,以便再次使用.
            cmd.Parameters.Clear();

            if (mustCloseConnection)
                connection.Close();

            return retval;
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,指定参数值,返回结果集中的第一行第一列.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输出参数和返回值参数.
        /// 
        /// 示例:  
        ///  int orderCount = (int)ExecuteScalar(conn, "GetOrderCount", 24, 36);
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalar(DbInfo db, DbConnection connection, string spName, params object[] parameterValues)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果有参数值
            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, connection, spName);

                // 给存储过程参数赋值
                AssignParameterValues(commandParameters, parameterValues);

                // 调用重载方法
                return ExecuteScalar(db, connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // 没有参数值
                return ExecuteScalar(db, connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行指定数据库事务的命令,返回结果集中的第一行第一列.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int orderCount = (int)ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount");
        /// </remarks>
        /// <param name="transaction">一个有效的连接事务</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalar(DbInfo db, DbTransaction transaction, CommandType commandType, string commandText)
        {
            // 执行参数为空的方法
            return ExecuteScalar(db, transaction, commandType, commandText, (DbParameter[])null);
        }

        /// <summary>
        /// 执行指定数据库事务的命令,指定参数,返回结果集中的第一行第一列.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int orderCount = (int)ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount", new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">一个有效的连接事务</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="commandParameters">分配给命令的SqlParamter参数数组</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalar(DbInfo db, DbTransaction transaction, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");

            // 记录sql
            DbLoger.AddLog(commandText);

            // 创建DbCommand命令,并进行预处理
            DbCommand cmd = GetFactory(db).CreateCommand();
            cmd.CommandTimeout = 0;
            bool mustCloseConnection = false;
            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

            // 执行DbCommand命令,并返回结果.
            object retval = cmd.ExecuteScalar();

            QueryCount++;
            // 清除参数,以便再次使用.
            cmd.Parameters.Clear();
            return retval;
        }

        /// <summary>
        /// 执行指定数据库事务的命令,指定参数值,返回结果集中的第一行第一列.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输出参数和返回值参数.
        /// 
        /// 示例:  
        ///  int orderCount = (int)ExecuteScalar(trans, "GetOrderCount", 24, 36);
        /// </remarks>
        /// <param name="transaction">一个有效的连接事务</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalar(DbInfo db, DbTransaction transaction, string spName, params object[] parameterValues)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果有参数值
            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                // PPull the parameters for this stored procedure from the parameter cache ()
                DbParameter[] commandParameters = GetSpParameterSet(db, transaction.Connection, spName);

                // 给存储过程参数赋值
                AssignParameterValues(commandParameters, parameterValues);

                // 调用重载方法
                return ExecuteScalar(db, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // 没有参数值
                return ExecuteScalar(db, transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteScalar

        #region FillDataset 填充数据集
        /// <summary>
        /// 执行指定数据库连接字符串的命令,映射数据表并填充数据集.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  FillDataset(connString, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"});
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="dataSet">要填充结果集的DataSet实例</param>
        /// <param name="tableNames">表映射的数据表数组
        /// 用户定义的表名 (可有是实际的表名.)</param>
        public static void FillDataset(DbInfo db, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames)
        {
            if (string.IsNullOrEmpty(db.ConnectionString))
                throw new ArgumentNullException("ConnectionString");
            if (dataSet == null) throw new ArgumentNullException("dataSet");

            // 创建并打开数据库连接对象,操作完成释放对象.
            using (DbConnection connection = GetFactory(db).CreateConnection())
            {
                connection.ConnectionString = db.ConnectionString;
                connection.Open();

                // 调用指定数据库连接字符串重载方法.
                FillDataset(db, connection, commandType, commandText, dataSet, tableNames);
            }
        }

        /// <summary>
        /// 执行指定数据库连接字符串的命令,映射数据表并填充数据集.指定命令参数.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  FillDataset(connString, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="commandParameters">分配给命令的SqlParamter参数数组</param>
        /// <param name="dataSet">要填充结果集的DataSet实例</param>
        /// <param name="tableNames">表映射的数据表数组
        /// 用户定义的表名 (可有是实际的表名.)
        /// </param>
        public static void FillDataset(DbInfo db, CommandType commandType,
            string commandText, DataSet dataSet, string[] tableNames,
            params DbParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(db.ConnectionString))
                throw new ArgumentNullException("ConnectionString");
            if (dataSet == null) throw new ArgumentNullException("dataSet");
            // 创建并打开数据库连接对象,操作完成释放对象.
            using (DbConnection connection = GetFactory(db).CreateConnection())
            {
                connection.ConnectionString = db.ConnectionString;
                connection.Open();

                // 调用指定数据库连接字符串重载方法.
                FillDataset(db, connection, commandType, commandText, dataSet, tableNames, commandParameters);
            }
        }

        /// <summary>
        /// 执行指定数据库连接字符串的命令,映射数据表并填充数据集,指定存储过程参数值.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输出参数和返回值参数.
        /// 
        /// 示例:  
        ///  FillDataset(connString, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, 24);
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataSet">要填充结果集的DataSet实例</param>
        /// <param name="tableNames">表映射的数据表数组
        /// 用户定义的表名 (可有是实际的表名.)
        /// </param>    
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        public static void FillDataset(DbInfo db, string spName,
            DataSet dataSet, string[] tableNames,
            params object[] parameterValues)
        {
            if (db.ConnectionString == null || db.ConnectionString.Length == 0) throw new ArgumentNullException("ConnectionString");
            if (dataSet == null) throw new ArgumentNullException("dataSet");
            // 创建并打开数据库连接对象,操作完成释放对象.
            using (DbConnection connection = GetFactory(db).CreateConnection())
            {
                connection.ConnectionString = db.ConnectionString;
                connection.Open();

                // 调用指定数据库连接字符串重载方法.
                FillDataset(db, connection, spName, dataSet, tableNames, parameterValues);
            }
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,映射数据表并填充数据集.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  FillDataset(conn, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"});
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="dataSet">要填充结果集的DataSet实例</param>
        /// <param name="tableNames">表映射的数据表数组
        /// 用户定义的表名 (可有是实际的表名.)
        /// </param>    
        public static void FillDataset(DbInfo db, DbConnection connection, CommandType commandType,
            string commandText, DataSet dataSet, string[] tableNames)
        {
            FillDataset(db, connection, commandType, commandText, dataSet, tableNames, null);
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,映射数据表并填充数据集,指定参数.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  FillDataset(conn, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="dataSet">要填充结果集的DataSet实例</param>
        /// <param name="tableNames">表映射的数据表数组
        /// 用户定义的表名 (可有是实际的表名.)
        /// </param>
        /// <param name="commandParameters">分配给命令的SqlParamter参数数组</param>
        public static void FillDataset(DbInfo db, DbConnection connection, CommandType commandType,
            string commandText, DataSet dataSet, string[] tableNames,
            params DbParameter[] commandParameters)
        {
            FillDataset(db, connection, null, commandType, commandText, dataSet, tableNames, commandParameters);
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,映射数据表并填充数据集,指定存储过程参数值.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输出参数和返回值参数.
        /// 
        /// 示例:  
        ///  FillDataset(conn, "GetOrders", ds, new string[] {"orders"}, 24, 36);
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataSet">要填充结果集的DataSet实例</param>
        /// <param name="tableNames">表映射的数据表数组
        /// 用户定义的表名 (可有是实际的表名.)
        /// </param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        public static void FillDataset(DbInfo db, DbConnection connection, string spName,
            DataSet dataSet, string[] tableNames,
            params object[] parameterValues)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (dataSet == null) throw new ArgumentNullException("dataSet");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果有参数值
            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, connection, spName);

                // 给存储过程参数赋值
                AssignParameterValues(commandParameters, parameterValues);

                // 调用重载方法
                FillDataset(db, connection, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters);
            }
            else
            {
                // 没有参数值
                FillDataset(db, connection, CommandType.StoredProcedure, spName, dataSet, tableNames);
            }
        }

        /// <summary>
        /// 执行指定数据库事务的命令,映射数据表并填充数据集.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  FillDataset(trans, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"});
        /// </remarks>
        /// <param name="transaction">一个有效的连接事务</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="dataSet">要填充结果集的DataSet实例</param>
        /// <param name="tableNames">表映射的数据表数组
        /// 用户定义的表名 (可有是实际的表名.)
        /// </param>
        public static void FillDataset(DbInfo db, DbTransaction transaction, CommandType commandType,
            string commandText,
            DataSet dataSet, string[] tableNames)
        {
            FillDataset(db, transaction, commandType, commandText, dataSet, tableNames, null);
        }

        /// <summary>
        /// 执行指定数据库事务的命令,映射数据表并填充数据集,指定参数.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  FillDataset(trans, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">一个有效的连接事务</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="dataSet">要填充结果集的DataSet实例</param>
        /// <param name="tableNames">表映射的数据表数组
        /// 用户定义的表名 (可有是实际的表名.)
        /// </param>
        /// <param name="commandParameters">分配给命令的SqlParamter参数数组</param>
        public static void FillDataset(DbInfo db, DbTransaction transaction, CommandType commandType,
            string commandText, DataSet dataSet, string[] tableNames,
            params DbParameter[] commandParameters)
        {
            FillDataset(db, transaction.Connection, transaction, commandType, commandText, dataSet, tableNames, commandParameters);
        }

        /// <summary>
        /// 执行指定数据库事务的命令,映射数据表并填充数据集,指定存储过程参数值.
        /// </summary>
        /// <remarks>
        /// 此方法不提供访问存储过程输出参数和返回值参数.
        /// 
        /// 示例:  
        ///  FillDataset(trans, "GetOrders", ds, new string[]{"orders"}, 24, 36);
        /// </remarks>
        /// <param name="transaction">一个有效的连接事务</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataSet">要填充结果集的DataSet实例</param>
        /// <param name="tableNames">表映射的数据表数组
        /// 用户定义的表名 (可有是实际的表名.)
        /// </param>
        /// <param name="parameterValues">分配给存储过程输入参数的对象数组</param>
        public static void FillDataset(DbInfo db, DbTransaction transaction, string spName,
            DataSet dataSet, string[] tableNames,
            params object[] parameterValues)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
            if (dataSet == null) throw new ArgumentNullException("dataSet");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果有参数值
            if ((parameterValues != null) && (parameterValues.Length > 0))
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, transaction.Connection, spName);

                // 给存储过程参数赋值
                AssignParameterValues(commandParameters, parameterValues);

                // 调用重载方法
                FillDataset(db, transaction, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters);
            }
            else
            {
                // 没有参数值
                FillDataset(db, transaction, CommandType.StoredProcedure, spName, dataSet, tableNames);
            }
        }

        /// <summary>
        /// [私有方法][内部调用]执行指定数据库连接对象/事务的命令,映射数据表并填充数据集,DataSet/TableNames/DbParameters.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  FillDataset(conn, trans, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new DbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="transaction">一个有效的连接事务</param>
        /// <param name="commandType">命令类型 (存储过程,命令文本或其它)</param>
        /// <param name="commandText">存储过程名称或SQL语句</param>
        /// <param name="dataSet">要填充结果集的DataSet实例</param>
        /// <param name="tableNames">表映射的数据表数组
        /// 用户定义的表名 (可有是实际的表名.)
        /// </param>
        /// <param name="commandParameters">分配给命令的SqlParamter参数数组</param>
        private static void FillDataset(DbInfo db, DbConnection connection, DbTransaction transaction, CommandType commandType,
            string commandText, DataSet dataSet, string[] tableNames,
            params DbParameter[] commandParameters)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (dataSet == null) throw new ArgumentNullException("dataSet");

            // 记录sql
            DbLoger.AddLog(commandText);

            // 创建DbCommand命令,并进行预处理
            DbCommand command = GetFactory(db).CreateCommand();
            command.CommandTimeout = 0;
            bool mustCloseConnection = false;
            PrepareCommand(command, connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

            // 执行命令
            using (DbDataAdapter dataAdapter = GetFactory(db).CreateDataAdapter())
            {
                dataAdapter.SelectCommand = command;
                // 追加表映射
                if (tableNames != null && tableNames.Length > 0)
                {
                    string tableName = "Table";
                    for (int index = 0; index < tableNames.Length; index++)
                    {
                        if (tableNames[index] == null || tableNames[index].Length == 0) throw new ArgumentException("The tableNames parameter must contain a list of tables, a value was provided as null or empty string.", "tableNames");
                        dataAdapter.TableMappings.Add(tableName, tableNames[index]);
                        tableName += (index + 1).ToString();
                    }
                }

                // 填充数据集使用默认表名称
                dataAdapter.Fill(dataSet);

                // 清除参数,以便再次使用.
                command.Parameters.Clear();
            }

            if (mustCloseConnection)
                connection.Close();
        }
        #endregion

        #region UpdateDataset 更新数据集
        /// <summary>
        /// 执行数据集更新到数据库,指定inserted, updated, or deleted命令.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  UpdateDataset(conn, insertCommand, deleteCommand, updateCommand, dataSet, "Order");
        /// </remarks>
        /// <param name="insertCommand">[追加记录]一个有效的SQL语句或存储过程</param>
        /// <param name="deleteCommand">[删除记录]一个有效的SQL语句或存储过程</param>
        /// <param name="updateCommand">[更新记录]一个有效的SQL语句或存储过程</param>
        /// <param name="dataSet">要更新到数据库的DataSet</param>
        /// <param name="tableName">要更新到数据库的DataTable</param>
        public static void UpdateDataset(DbInfo db, DbCommand insertCommand, DbCommand deleteCommand, DbCommand updateCommand, DataSet dataSet, string tableName)
        {
            if (insertCommand == null) throw new ArgumentNullException("insertCommand");
            if (deleteCommand == null) throw new ArgumentNullException("deleteCommand");
            if (updateCommand == null) throw new ArgumentNullException("updateCommand");
            if (tableName == null || tableName.Length == 0) throw new ArgumentNullException("tableName");

            // 创建DbDataAdapter,当操作完成后释放.
            using (DbDataAdapter dataAdapter = GetFactory(db).CreateDataAdapter())
            {
                // 设置数据适配器命令
                dataAdapter.UpdateCommand = updateCommand;
                dataAdapter.InsertCommand = insertCommand;
                dataAdapter.DeleteCommand = deleteCommand;

                // 更新数据集改变到数据库
                dataAdapter.Update(dataSet, tableName);

                // 提交所有改变到数据集.
                dataSet.AcceptChanges();
            }
        }
        #endregion

        #region CreateCommand 创建一条DbCommand命令
        /// <summary>
        /// 创建DbCommand命令,指定数据库连接对象,存储过程名和参数.
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  DbCommand command = CreateCommand(conn, "AddCustomer", "CustomerID", "CustomerName");
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="sourceColumns">源表的列名称数组</param>
        /// <returns>返回DbCommand命令</returns>
        public static DbCommand CreateCommand(DbInfo db, DbConnection connection, string spName, params string[] sourceColumns)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 创建命令
            DbCommand cmd = GetFactory(db).CreateCommand();
            cmd.CommandTimeout = 0;
            cmd.CommandText = spName;
            cmd.Connection = connection;
            cmd.CommandType = CommandType.StoredProcedure;

            // 如果有参数值
            if ((sourceColumns != null) && (sourceColumns.Length > 0))
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, connection, spName);

                // 将源表的列到映射到DataSet命令中.
                for (int index = 0; index < sourceColumns.Length; index++)
                    commandParameters[index].SourceColumn = sourceColumns[index];

                // Attach the discovered parameters to the DbCommand object
                AttachParameters(cmd, commandParameters);
            }

            return cmd;
        }
        #endregion

        #region ExecuteNonQueryTypedParams 类型化参数(DataRow)
        /// <summary>
        /// 执行指定连接数据库连接字符串的存储过程,使用DataRow做为参数值,返回受影响的行数.
        /// </summary>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteNonQueryTypedParams(DbInfo db, String spName, DataRow dataRow)
        {
            if (string.IsNullOrEmpty(db.ConnectionString)) throw new ArgumentNullException("ConnectionString");
            if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException("spName");

            // 如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteNonQuery(db, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteNonQuery(db, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行指定连接数据库连接对象的存储过程,使用DataRow做为参数值,返回受影响的行数.
        /// </summary>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteNonQueryTypedParams(DbInfo db, DbConnection connection, String spName, DataRow dataRow)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, connection, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteNonQuery(db, connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteNonQuery(db, connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行指定连接数据库事物的存储过程,使用DataRow做为参数值,返回受影响的行数.
        /// </summary>
        /// <param name="transaction">一个有效的连接事务 object</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回影响的行数</returns>
        public static int ExecuteNonQueryTypedParams(DbInfo db, DbTransaction transaction, String spName, DataRow dataRow)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // Sf the row has values, the store procedure parameters must be initialized
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, transaction.Connection, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteNonQuery(db, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteNonQuery(db, transaction, CommandType.StoredProcedure, spName);
            }
        }
        #endregion

        #region ExecuteDatasetTypedParams 类型化参数(DataRow)
        /// <summary>
        /// 执行指定连接数据库连接字符串的存储过程,使用DataRow做为参数值,返回DataSet.
        /// </summary>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回一个包含结果集的DataSet.</returns>
        public static DataSet ExecuteDatasetTypedParams(DbInfo db, String spName, DataRow dataRow)
        {
            if (db.ConnectionString == null || db.ConnectionString.Length == 0) throw new ArgumentNullException("ConnectionString");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            //如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteDataset(db, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteDataset(db, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行指定连接数据库连接对象的存储过程,使用DataRow做为参数值,返回DataSet.
        /// </summary>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回一个包含结果集的DataSet.</returns>
        /// 
        public static DataSet ExecuteDatasetTypedParams(DbInfo db, DbConnection connection, String spName, DataRow dataRow)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, connection, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteDataset(db, connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteDataset(db, connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行指定连接数据库事务的存储过程,使用DataRow做为参数值,返回DataSet.
        /// </summary>
        /// <param name="transaction">一个有效的连接事务 object</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回一个包含结果集的DataSet.</returns>
        public static DataSet ExecuteDatasetTypedParams(DbInfo db, DbTransaction transaction, String spName, DataRow dataRow)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, transaction.Connection, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteDataset(db, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteDataset(db, transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion

        #region ExecuteReaderTypedParams 类型化参数(DataRow)
        /// <summary>
        /// 执行指定连接数据库连接字符串的存储过程,使用DataRow做为参数值,返回DataReader.
        /// </summary>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReaderTypedParams(DbInfo db, string spName, DataRow dataRow)
        {
            if (db.ConnectionString == null || db.ConnectionString.Length == 0) throw new ArgumentNullException("ConnectionString");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteReader(db, db.ConnectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteReader(db, db.ConnectionString, CommandType.StoredProcedure, spName);
            }
        }


        /// <summary>
        /// 执行指定连接数据库连接对象的存储过程,使用DataRow做为参数值,返回DataReader.
        /// </summary>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReaderTypedParams(DbInfo db, DbConnection connection, String spName, DataRow dataRow)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, connection, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteReader(db, connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteReader(db, connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行指定连接数据库事物的存储过程,使用DataRow做为参数值,返回DataReader.
        /// </summary>
        /// <param name="transaction">一个有效的连接事务 object</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回包含结果集的DbDataReader</returns>
        public static DbDataReader ExecuteReaderTypedParams(DbInfo db, DbTransaction transaction, String spName, DataRow dataRow)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, transaction.Connection, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteReader(db, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteReader(db, transaction, CommandType.StoredProcedure, spName);
            }
        }
        #endregion

        #region ExecuteScalarTypedParams 类型化参数(DataRow)
        /// <summary>
        /// 执行指定连接数据库连接字符串的存储过程,使用DataRow做为参数值,返回结果集中的第一行第一列.
        /// </summary>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalarTypedParams(DbInfo db, String spName, DataRow dataRow)
        {
            if (db.ConnectionString == null || db.ConnectionString.Length == 0) throw new ArgumentNullException("ConnectionString");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteScalar(db, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteScalar(db, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行指定连接数据库连接对象的存储过程,使用DataRow做为参数值,返回结果集中的第一行第一列.
        /// </summary>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalarTypedParams(DbInfo db, DbConnection connection, String spName, DataRow dataRow)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, connection, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteScalar(db, connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteScalar(db, connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// 执行指定连接数据库事务的存储过程,使用DataRow做为参数值,返回结果集中的第一行第一列.
        /// </summary>
        /// <param name="transaction">一个有效的连接事务 object</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="dataRow">使用DataRow作为参数值</param>
        /// <returns>返回结果集中的第一行第一列</returns>
        public static object ExecuteScalarTypedParams(DbInfo db, DbTransaction transaction, String spName, DataRow dataRow)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (transaction != null && transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            // 如果row有值,存储过程必须初始化.
            if (dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // 从缓存中加载存储过程参数,如果缓存中不存在则从数据库中检索参数信息并加载到缓存中. ()
                DbParameter[] commandParameters = GetSpParameterSet(db, transaction.Connection, spName);

                // 分配参数值
                AssignParameterValues(commandParameters, dataRow);

                return DbHelper.ExecuteScalar(db, transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return DbHelper.ExecuteScalar(db, transaction, CommandType.StoredProcedure, spName);
            }
        }
        #endregion

        #region 缓存方法

        /// <summary>
        /// 追加参数数组到缓存.
        /// </summary>
        /// <param name="ConnectionString">一个有效的数据库连接字符串</param>
        /// <param name="commandText">存储过程名或SQL语句</param>
        /// <param name="commandParameters">要缓存的参数数组</param>
        public static void CacheParameterSet(DbInfo db, string commandText, params DbParameter[] commandParameters)
        {
            if (db.ConnectionString == null || db.ConnectionString.Length == 0) throw new ArgumentNullException("ConnectionString");
            if (commandText == null || commandText.Length == 0) throw new ArgumentNullException("commandText");

            string hashKey = db.ConnectionString + ":" + commandText;

            MParamcache[hashKey] = commandParameters;
        }

        /// <summary>
        /// 从缓存中获取参数数组.
        /// </summary>
        /// <param name="ConnectionString">一个有效的数据库连接字符</param>
        /// <param name="commandText">存储过程名或SQL语句</param>
        /// <returns>参数数组</returns>
        public static DbParameter[] GetCachedParameterSet(DbInfo db, string commandText)
        {
            if (db.ConnectionString == null || db.ConnectionString.Length == 0) throw new ArgumentNullException("ConnectionString");
            if (commandText == null || commandText.Length == 0) throw new ArgumentNullException("commandText");

            string hashKey = db.ConnectionString + ":" + commandText;

            DbParameter[] cachedParameters = MParamcache[hashKey] as DbParameter[];
            if (cachedParameters == null)
            {
                return null;
            }
            else
            {
                return CloneParameters(cachedParameters);
            }
        }

        #endregion 缓存方法结束

        #region 检索指定的存储过程的参数集

        /// <summary>
        /// 返回指定的存储过程的参数集
        /// </summary>
        /// <remarks>
        /// 这个方法将查询数据库,并将信息存储到缓存.
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符</param>
        /// <param name="spName">存储过程名</param>
        /// <returns>返回DbParameter参数数组</returns>
        public static DbParameter[] GetSpParameterSet(DbInfo db, string spName)
        {
            return GetSpParameterSet(db, spName, false);
        }

        /// <summary>
        /// 返回指定的存储过程的参数集
        /// </summary>
        /// <remarks>
        /// 这个方法将查询数据库,并将信息存储到缓存.
        /// </remarks>
        /// <param name="ConnectionString">一个有效的数据库连接字符.</param>
        /// <param name="spName">存储过程名</param>
        /// <param name="includeReturnValueParameter">是否包含返回值参数</param>
        /// <returns>返回DbParameter参数数组</returns>
        public static DbParameter[] GetSpParameterSet(DbInfo db, string spName, bool includeReturnValueParameter)
        {
            if (db.ConnectionString == null || db.ConnectionString.Length == 0) throw new ArgumentNullException("ConnectionString");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            using (DbConnection connection = GetFactory(db).CreateConnection())
            {
                connection.ConnectionString = db.ConnectionString;
                return GetSpParameterSetInternal(db, connection, spName, includeReturnValueParameter);
            }
        }

        /// <summary>
        /// [内部]返回指定的存储过程的参数集(使用连接对象).
        /// </summary>
        /// <remarks>
        /// 这个方法将查询数据库,并将信息存储到缓存.
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接字符</param>
        /// <param name="spName">存储过程名</param>
        /// <returns>返回DbParameter参数数组</returns>
        internal static DbParameter[] GetSpParameterSet(DbInfo db, DbConnection connection, string spName)
        {
            return GetSpParameterSet(db, connection, spName, false);
        }

        /// <summary>
        /// [内部]返回指定的存储过程的参数集(使用连接对象)
        /// </summary>
        /// <remarks>
        /// 这个方法将查询数据库,并将信息存储到缓存.
        /// </remarks>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名</param>
        /// <param name="includeReturnValueParameter">
        /// 是否包含返回值参数
        /// </param>
        /// <returns>返回DbParameter参数数组</returns>
        internal static DbParameter[] GetSpParameterSet(DbInfo db, DbConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            using (DbConnection clonedConnection = (DbConnection)((ICloneable)connection).Clone())
            {
                return GetSpParameterSetInternal(db, clonedConnection, spName, includeReturnValueParameter);
            }
        }

        /// <summary>
        /// [私有]返回指定的存储过程的参数集(使用连接对象)
        /// </summary>
        /// <param name="connection">一个有效的数据库连接对象</param>
        /// <param name="spName">存储过程名</param>
        /// <param name="includeReturnValueParameter">是否包含返回值参数</param>
        /// <returns>返回DbParameter参数数组</returns>
        private static DbParameter[] GetSpParameterSetInternal(DbInfo db, DbConnection connection, string spName, bool includeReturnValueParameter)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (spName == null || spName.Length == 0) throw new ArgumentNullException("spName");

            string hashKey = connection.ConnectionString + ":" + spName + (includeReturnValueParameter ? ":include ReturnValue Parameter" : "");

            DbParameter[] cachedParameters;

            cachedParameters = MParamcache[hashKey] as DbParameter[];
            if (cachedParameters == null)
            {
                DbParameter[] spParameters = DiscoverSpParameterSet(db, connection, spName, includeReturnValueParameter);
                MParamcache[hashKey] = spParameters;
                cachedParameters = spParameters;
            }

            return CloneParameters(cachedParameters);
        }

        #endregion 参数集检索结束

        #region 生成参数

        public static DbParameter MakeInParam(DbInfo db, string paramName, DbType dbType, int size, object value)
        {
            return MakeParam(db, paramName, dbType, size, ParameterDirection.Input, value);
        }

        public static DbParameter MakeOutParam(DbInfo db, string paramName, DbType dbType, int size)
        {
            return MakeParam(db, paramName, dbType, size, ParameterDirection.Output, null);
        }

        public static DbParameter MakeParam(DbInfo db, string paramName, DbType dbType, Int32 size, ParameterDirection direction, object value)
        {
            DbParameter param;

            param = GetProvider(db).MakeParam(paramName, dbType, size);

            param.Direction = direction;
            if (!(direction == ParameterDirection.Output && value == null))
                param.Value = value;

            return param;
        }

        #endregion 生成参数结束

        #region 执行ExecuteScalar,将结果以字符串类型输出。

        public static string ExecuteScalarToStr(DbInfo db, CommandType commandType, string commandText)
        {
            object ec = ExecuteScalar(db, commandType, commandText);
            if (ec == null)
            {
                return "";
            }
            return ec.ToString();
        }


        public static string ExecuteScalarToStr(DbInfo db, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            object ec = ExecuteScalar(db, commandType, commandText, commandParameters);
            if (ec == null)
            {
                return "";
            }
            return ec.ToString();
        }


        #endregion

    }

    #endregion

}
