using System;
using System.Data;
using Npgsql;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using NpgsqlTypes;
using Pgvector;

namespace ZSN.AI.DAL.Postgres
{
    public partial class EpisodicMemoryManage : IEpisodicMemoryManage
    {
        /// 表链接
        private string EpisodicMemoryConnectionName = "KnowledgeBaseDb";
        /// 表名称
        private string EpisodicMemoryTableName = "tb_episodic_memory";
        /// 表字段
        private const string EpisodicMemoryTableField = "memory_id,app_id,session_id,member_id,event_type,event_context,event_result,summary,embedding,importance,access_count,last_access_time,create_time";
        /// 查询用表字段(不包含vector类型的embedding字段)
        private const string EpisodicMemoryTableFieldForQuery = "memory_id,app_id,session_id,member_id,event_type,event_context,event_result,summary,importance,access_count,last_access_time,create_time";
        /// 添加用表字段
        private const string EpisodicMemoryTableFieldForAdd = "memory_id,app_id,session_id,member_id,event_type,event_context,event_result,summary,embedding,importance,access_count,last_access_time,create_time";
        /// 添加用表字段value
        private const string EpisodicMemoryTableFieldAltForAdd = "@memory_id,@app_id,@session_id,@member_id,@event_type,@event_context,@event_result,@summary,@embedding,@importance,@access_count,@last_access_time,@create_time";

        public string SetConnectionName(string connName)
        {
            return EpisodicMemoryConnectionName = connName;
        }

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public string EpisodicMemory_Add(EpisodicMemoryInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(EpisodicMemoryTableName);
            strSql.Append(" (");
            strSql.Append(EpisodicMemoryTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(EpisodicMemoryTableFieldAltForAdd);
            strSql.Append(");");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = model.MemoryID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = model.SessionID },
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = model.MemberID },
                new NpgsqlParameter("@event_type", NpgsqlDbType.Varchar) { Value = model.EventType },
                new NpgsqlParameter("@event_context", NpgsqlDbType.Text) { Value = model.EventContext },
                new NpgsqlParameter("@event_result", NpgsqlDbType.Text) { Value = model.EventResult },
                new NpgsqlParameter("@summary", NpgsqlDbType.Text) { Value = model.Summary },
                new NpgsqlParameter("@embedding", model.Embedding),
                new NpgsqlParameter("@importance", NpgsqlDbType.Integer) { Value = model.Importance },
                new NpgsqlParameter("@access_count", NpgsqlDbType.Integer) { Value = model.AccessCount },
                new NpgsqlParameter("@last_access_time", NpgsqlDbType.Timestamp) { Value = model.LastAccessTime.HasValue ? (object)model.LastAccessTime.Value : DBNull.Value },
                new NpgsqlParameter("@create_time", NpgsqlDbType.Timestamp) { Value = model.CreateTime }
            };

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(EpisodicMemoryConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                return model.MemoryID;
            }
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool EpisodicMemory_Update(EpisodicMemoryInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(EpisodicMemoryTableName);
            strSql.Append(" set ");
            strSql.Append("app_id=@app_id,");
            strSql.Append("session_id=@session_id,");
            strSql.Append("member_id=@member_id,");
            strSql.Append("event_type=@event_type,");
            strSql.Append("event_context=@event_context,");
            strSql.Append("event_result=@event_result,");
            strSql.Append("summary=@summary,");
            strSql.Append("embedding=@embedding,");
            strSql.Append("importance=@importance,");
            strSql.Append("access_count=@access_count,");
            strSql.Append("last_access_time=@last_access_time ");
            strSql.Append(" where memory_id=@memory_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = model.MemoryID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = model.SessionID },
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = model.MemberID },
                new NpgsqlParameter("@event_type", NpgsqlDbType.Varchar) { Value = model.EventType },
                new NpgsqlParameter("@event_context", NpgsqlDbType.Text) { Value = model.EventContext },
                new NpgsqlParameter("@event_result", NpgsqlDbType.Text) { Value = model.EventResult },
                new NpgsqlParameter("@summary", NpgsqlDbType.Text) { Value = model.Summary },
                new NpgsqlParameter("@embedding", model.Embedding),
                new NpgsqlParameter("@importance", NpgsqlDbType.Integer) { Value = model.Importance },
                new NpgsqlParameter("@access_count", NpgsqlDbType.Integer) { Value = model.AccessCount },
                new NpgsqlParameter("@last_access_time", NpgsqlDbType.Timestamp) { Value = model.LastAccessTime.HasValue ? (object)model.LastAccessTime.Value : DBNull.Value }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(EpisodicMemoryConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool EpisodicMemory_Delete(string MemoryID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(EpisodicMemoryTableName);
            strSql.Append(" where memory_id=@memory_id");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = MemoryID }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(EpisodicMemoryConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public bool EpisodicMemory_DeleteList(string MemoryIDlist)
        {
            if (string.IsNullOrWhiteSpace(MemoryIDlist))
            {
                return false;
            }

            // 分割ID列表并去除空白
            string[] items = MemoryIDlist.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(item => item.Trim())
                                       .Where(item => !string.IsNullOrEmpty(item))
                                       .ToArray();

            if (items.Length == 0)
            {
                return false;
            }

            // 使用PostgreSQL的ANY数组语法
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(EpisodicMemoryTableName);
            strSql.Append(" where memory_id = ANY(@memory_ids)");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@memory_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar) { Value = items }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(EpisodicMemoryConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public EpisodicMemoryInfo EpisodicMemory_GetModel(string MemoryID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(EpisodicMemoryTableField);
            strSql.Append(" from ");
            strSql.Append(EpisodicMemoryTableName);
            strSql.Append(" where memory_id=@memory_id");
            strSql.Append(" limit 1");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = MemoryID }
            };

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(EpisodicMemoryConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return EpisodicMemory_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public EpisodicMemoryInfo EpisodicMemory_DataRowToModel(DataRow row)
        {
            EpisodicMemoryInfo model = new EpisodicMemoryInfo();
            if (row != null)
            {
                if (row["memory_id"] != null)
                {
                    model.MemoryID = row["memory_id"].ToString();
                }
                if (row["app_id"] != null)
                {
                    model.AppID = row["app_id"].ToString();
                }
                if (row["session_id"] != null)
                {
                    model.SessionID = row["session_id"].ToString();
                }
                if (row["member_id"] != null)
                {
                    model.MemberID = row["member_id"].ToString();
                }
                if (row["event_type"] != null)
                {
                    model.EventType = row["event_type"].ToString();
                }
                if (row["event_context"] != null)
                {
                    model.EventContext = row["event_context"].ToString();
                }
                if (row["event_result"] != null)
                {
                    model.EventResult = row["event_result"].ToString();
                }
                if (row["summary"] != null)
                {
                    model.Summary = row["summary"].ToString();
                }
                if (row.Table.Columns.Contains("embedding") && row["embedding"] != null)
                {
                    model.Embedding = row["embedding"].ToString();
                }
                if (row["importance"] != null)
                {
                    model.Importance = int.Parse(row["importance"].ToString());
                }
                if (row["access_count"] != null)
                {
                    model.AccessCount = int.Parse(row["access_count"].ToString());
                }
                if (row["last_access_time"] != null && row["last_access_time"] != DBNull.Value)
                {
                    model.LastAccessTime = DateTime.Parse(row["last_access_time"].ToString());
                }
                if (row["create_time"] != null)
                {
                    model.CreateTime = DateTime.Parse(row["create_time"].ToString());
                }
            }
            return model;
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet EpisodicMemory_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(EpisodicMemoryTableFieldForQuery);
            strSql.Append(" FROM ");
            strSql.Append(EpisodicMemoryTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(EpisodicMemoryConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet EpisodicMemory_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(EpisodicMemoryTableFieldForQuery);
            strSql.Append(" FROM ");
            strSql.Append(EpisodicMemoryTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            if (filedOrder.Trim() != "")
            {
                strSql.Append(" order by " + filedOrder);
            }
            if (top > 0)
            {
                strSql.Append(" limit " + top.ToString() + " ");
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(EpisodicMemoryConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int EpisodicMemory_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(EpisodicMemoryTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(EpisodicMemoryConnectionName), CommandType.Text, strSql.ToString());
            if (obj == null)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(obj);
            }
        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public DataSet EpisodicMemory_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(EpisodicMemoryTableField);
            strSql.Append(" FROM ");
            strSql.Append(EpisodicMemoryTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(EpisodicMemoryConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 增加访问次数
        /// </summary>
        public bool EpisodicMemory_IncrementAccessCount(string MemoryID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(EpisodicMemoryTableName);
            strSql.Append(" set access_count=access_count+1, last_access_time=@last_access_time");
            strSql.Append(" where memory_id=@memory_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = MemoryID },
                new NpgsqlParameter("@last_access_time", NpgsqlDbType.Timestamp) { Value = DateTime.Now }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(EpisodicMemoryConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }
    }
}
