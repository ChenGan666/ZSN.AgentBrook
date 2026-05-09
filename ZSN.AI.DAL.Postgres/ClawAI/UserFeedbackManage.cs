using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using NpgsqlTypes;

namespace ZSN.AI.DAL.Postgres
{
    /// <summary>
    /// 用户反馈数据访问类 - PostgreSQL实现
    /// </summary>
    public class UserFeedbackManage : IUserFeedbackManage
    {
        ///表链接
        private string ConnectionName = "KnowledgeBaseDb";
        ///表名称
        private string TableName = "tb_claw_user_feedback";
        ///表字段
        private const string TableField = "feedback_id,app_id,session_id,member_id,memory_id,user_query,ai_response,feedback_type,feedback_score,feedback_comment,used_memories,metadata,create_time";

        public string SetConnectionName(string connName)
        {
            return ConnectionName = connName;
        }

        #region 基础CRUD操作

        public string UserFeedback_Add(UserFeedbackInfo model)
        {
            if (string.IsNullOrEmpty(model.FeedbackID))
            {
                model.FeedbackID = Guid.NewGuid().ToString();
            }

            string sql = $@"
                INSERT INTO {TableName} (
                    feedback_id,app_id,session_id,member_id,memory_id,user_query,ai_response,
                    feedback_type,feedback_score,feedback_comment,used_memories,metadata,create_time
                ) VALUES (
                    @feedback_id,@app_id,@session_id,@member_id,@memory_id,@user_query,@ai_response,
                    @feedback_type,@feedback_score,@feedback_comment,@used_memories::jsonb,@metadata::jsonb,@create_time
                ) RETURNING feedback_id";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@feedback_id", NpgsqlDbType.Varchar) { Value = model.FeedbackID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = model.SessionID },
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = model.MemberID },
                new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = model.MemoryID ?? (object)DBNull.Value },
                new NpgsqlParameter("@user_query", NpgsqlDbType.Text) { Value = model.UserQuery ?? (object)DBNull.Value },
                new NpgsqlParameter("@ai_response", NpgsqlDbType.Text) { Value = model.AIResponse ?? (object)DBNull.Value },
                new NpgsqlParameter("@feedback_type", NpgsqlDbType.Varchar) { Value = model.FeedbackType },
                new NpgsqlParameter("@feedback_score", NpgsqlDbType.Integer) { Value = model.FeedbackScore },
                new NpgsqlParameter("@feedback_comment", NpgsqlDbType.Text) { Value = model.FeedbackComment ?? (object)DBNull.Value },
                new NpgsqlParameter("@used_memories", NpgsqlDbType.Text) { Value = model.UsedMemories ?? "[]" },
                new NpgsqlParameter("@metadata", NpgsqlDbType.Text) { Value = model.Metadata ?? "{}" },
                new NpgsqlParameter("@create_time", NpgsqlDbType.Timestamp) { Value = model.CreateTime }
            };

            var result = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return result?.ToString() ?? model.FeedbackID;
        }

        public bool UserFeedback_Update(UserFeedbackInfo model)
        {
            string sql = $@"
                UPDATE {TableName} SET
                    app_id=@app_id,session_id=@session_id,member_id=@member_id,memory_id=@memory_id,
                    user_query=@user_query,ai_response=@ai_response,feedback_type=@feedback_type,
                    feedback_score=@feedback_score,feedback_comment=@feedback_comment,
                    used_memories=@used_memories::jsonb,metadata=@metadata::jsonb
                WHERE feedback_id=@feedback_id";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@feedback_id", NpgsqlDbType.Varchar) { Value = model.FeedbackID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = model.SessionID },
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = model.MemberID },
                new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = model.MemoryID ?? (object)DBNull.Value },
                new NpgsqlParameter("@user_query", NpgsqlDbType.Text) { Value = model.UserQuery ?? (object)DBNull.Value },
                new NpgsqlParameter("@ai_response", NpgsqlDbType.Text) { Value = model.AIResponse ?? (object)DBNull.Value },
                new NpgsqlParameter("@feedback_type", NpgsqlDbType.Varchar) { Value = model.FeedbackType },
                new NpgsqlParameter("@feedback_score", NpgsqlDbType.Integer) { Value = model.FeedbackScore },
                new NpgsqlParameter("@feedback_comment", NpgsqlDbType.Text) { Value = model.FeedbackComment ?? (object)DBNull.Value },
                new NpgsqlParameter("@used_memories", NpgsqlDbType.Text) { Value = model.UsedMemories ?? "[]" },
                new NpgsqlParameter("@metadata", NpgsqlDbType.Text) { Value = model.Metadata ?? "{}" }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return rows > 0;
        }

        public bool UserFeedback_Delete(string FeedbackID)
        {
            string sql = $"DELETE FROM {TableName} WHERE feedback_id=@feedback_id";
            var param = new NpgsqlParameter("@feedback_id", NpgsqlDbType.Varchar) { Value = FeedbackID };
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return rows > 0;
        }

        public bool UserFeedback_DeleteList(string FeedbackIDlist)
        {
            string sql = $"DELETE FROM {TableName} WHERE feedback_id = ANY(@feedback_ids)";
            var param = new NpgsqlParameter("@feedback_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar) { Value = FeedbackIDlist.Split(',') };
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return rows > 0;
        }

        public UserFeedbackInfo UserFeedback_GetModel(string FeedbackID)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE feedback_id=@feedback_id";
            var param = new NpgsqlParameter("@feedback_id", NpgsqlDbType.Varchar) { Value = FeedbackID };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return UserFeedback_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            return null;
        }

        public DataSet UserFeedback_GetList(string strWhere)
        {
            string where = string.IsNullOrWhiteSpace(strWhere) ? "" : $"WHERE {strWhere}";
            string sql = $"SELECT {TableField} FROM {TableName} {where} ORDER BY create_time DESC";
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql);
        }

        public DataSet UserFeedback_GetList(int top, string strWhere, string filedOrder)
        {
            string where = string.IsNullOrWhiteSpace(strWhere) ? "" : $"WHERE {strWhere}";
            string order = string.IsNullOrWhiteSpace(filedOrder) ? "ORDER BY create_time DESC" : $"ORDER BY {filedOrder}";
            string sql = $"SELECT {TableField} FROM {TableName} {where} {order} LIMIT {top}";
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql);
        }

        public int UserFeedback_GetRecordCount(string strWhere)
        {
            string where = string.IsNullOrWhiteSpace(strWhere) ? "" : $"WHERE {strWhere}";
            string sql = $"SELECT COUNT(*) FROM {TableName} {where}";
            var result = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public DataTable UserFeedback_GetListByPage(int size, int index, string where, out int pagetotal, out int total)
        {
            pagetotal = 0;
            total = 0;

            string whereClause = string.IsNullOrWhiteSpace(where) ? "" : $"WHERE {where}";
            string countQuery = $"SELECT COUNT(*) FROM {TableName} {whereClause}";
            var totalResult = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, countQuery);
            total = totalResult != null ? Convert.ToInt32(totalResult) : 0;

            if (total == 0)
            {
                pagetotal = 0;
                return null;
            }

            pagetotal = (total % size == 0) ? (total / size) : (total / size + 1);

            string query = $"SELECT {TableField} FROM {TableName} {whereClause} ORDER BY create_time DESC LIMIT {size} OFFSET {(index - 1) * size}";
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, query);

            return ds.Tables.Count > 0 ? ds.Tables[0] : null;
        }

        public UserFeedbackInfo UserFeedback_DataRowToModel(DataRow row)
        {
            UserFeedbackInfo model = new UserFeedbackInfo();
            if (row != null)
            {
                if (row["feedback_id"] != null && row["feedback_id"] != DBNull.Value)
                    model.FeedbackID = row["feedback_id"].ToString();
                if (row["app_id"] != null && row["app_id"] != DBNull.Value)
                    model.AppID = row["app_id"].ToString();
                if (row["session_id"] != null && row["session_id"] != DBNull.Value)
                    model.SessionID = row["session_id"].ToString();
                if (row["member_id"] != null && row["member_id"] != DBNull.Value)
                    model.MemberID = row["member_id"].ToString();
                if (row["memory_id"] != null && row["memory_id"] != DBNull.Value)
                    model.MemoryID = row["memory_id"].ToString();
                if (row["user_query"] != null && row["user_query"] != DBNull.Value)
                    model.UserQuery = row["user_query"].ToString();
                if (row["ai_response"] != null && row["ai_response"] != DBNull.Value)
                    model.AIResponse = row["ai_response"].ToString();
                if (row["feedback_type"] != null && row["feedback_type"] != DBNull.Value)
                    model.FeedbackType = row["feedback_type"].ToString();
                if (row["feedback_score"] != null && row["feedback_score"] != DBNull.Value)
                    model.FeedbackScore = Convert.ToInt32(row["feedback_score"]);
                if (row["feedback_comment"] != null && row["feedback_comment"] != DBNull.Value)
                    model.FeedbackComment = row["feedback_comment"].ToString();
                if (row["used_memories"] != null && row["used_memories"] != DBNull.Value)
                    model.UsedMemories = row["used_memories"].ToString();
                if (row["metadata"] != null && row["metadata"] != DBNull.Value)
                    model.Metadata = row["metadata"].ToString();
                if (row["create_time"] != null && row["create_time"] != DBNull.Value)
                    model.CreateTime = Convert.ToDateTime(row["create_time"]);
            }
            return model;
        }

        #endregion

        #region 扩展方法

        public List<UserFeedbackInfo> UserFeedback_GetByMemoryId(string memoryId, int limit)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE memory_id=@memory_id ORDER BY create_time DESC LIMIT {limit}";
            var param = new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = memoryId };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return DataSetToList(ds);
        }

        public List<UserFeedbackInfo> UserFeedback_GetByMemberAndApp(string memberId, string appId, int limit)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE member_id=@member_id AND app_id=@app_id ORDER BY create_time DESC LIMIT {limit}";
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = memberId },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId }
            };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return DataSetToList(ds);
        }

        public List<UserFeedbackInfo> UserFeedback_GetBySessionId(string sessionId)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE session_id=@session_id ORDER BY create_time DESC";
            var param = new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = sessionId };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return DataSetToList(ds);
        }

        public Dictionary<string, int> UserFeedback_GetStatsByType(string appId, DateTime? startTime, DateTime? endTime)
        {
            string timeFilter = "";
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId }
            };

            if (startTime.HasValue)
            {
                timeFilter += " AND create_time >= @start_time";
                parameters.Add(new NpgsqlParameter("@start_time", NpgsqlDbType.Timestamp) { Value = startTime.Value });
            }
            if (endTime.HasValue)
            {
                timeFilter += " AND create_time <= @end_time";
                parameters.Add(new NpgsqlParameter("@end_time", NpgsqlDbType.Timestamp) { Value = endTime.Value });
            }

            string sql = $@"
                SELECT feedback_type, COUNT(*) as count
                FROM {TableName}
                WHERE app_id=@app_id{timeFilter}
                GROUP BY feedback_type";

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            var stats = new Dictionary<string, int>();

            if (ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    string feedbackType = row["feedback_type"].ToString();
                    int count = Convert.ToInt32(row["count"]);
                    stats[feedbackType] = count;
                }
            }

            return stats;
        }

        public List<UserFeedbackInfo> UserFeedback_GetRecentFeedbacks(string appId, int days, int limit)
        {
            string sql = $@"
                SELECT {TableField}
                FROM {TableName}
                WHERE app_id=@app_id AND create_time >= CURRENT_TIMESTAMP - INTERVAL '{days} days'
                ORDER BY create_time DESC
                LIMIT {limit}";

            var param = new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return DataSetToList(ds);
        }

        public int UserFeedback_AddBatch(List<UserFeedbackInfo> feedbacks)
        {
            int count = 0;
            foreach (var feedback in feedbacks)
            {
                if (UserFeedback_Add(feedback) != null)
                {
                    count++;
                }
            }
            return count;
        }

        public KnowledgeFeedbackStats UserFeedback_GetKnowledgeStats(string memoryId, int recentDays)
        {
            string sql = $@"
                SELECT
                    COUNT(*) as total_feedbacks,
                    COUNT(*) FILTER (WHERE feedback_type='positive') as positive_count,
                    COUNT(*) FILTER (WHERE feedback_type='negative') as negative_count,
                    COUNT(*) FILTER (WHERE feedback_type='neutral') as neutral_count,
                    AVG(feedback_score) as average_score,
                    MAX(create_time) as last_feedback_time
                FROM {TableName}
                WHERE memory_id=@memory_id
                  AND create_time >= CURRENT_TIMESTAMP - INTERVAL '{recentDays} days'";

            var param = new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = memoryId };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRow row = ds.Tables[0].Rows[0];
                int total = row["total_feedbacks"] != DBNull.Value ? Convert.ToInt32(row["total_feedbacks"]) : 0;

                return new KnowledgeFeedbackStats
                {
                    MemoryID = memoryId,
                    TotalFeedbacks = total,
                    PositiveCount = row["positive_count"] != DBNull.Value ? Convert.ToInt32(row["positive_count"]) : 0,
                    NegativeCount = row["negative_count"] != DBNull.Value ? Convert.ToInt32(row["negative_count"]) : 0,
                    NeutralCount = row["neutral_count"] != DBNull.Value ? Convert.ToInt32(row["neutral_count"]) : 0,
                    AverageScore = row["average_score"] != DBNull.Value ? Convert.ToSingle(row["average_score"]) : 0,
                    LastFeedbackTime = row["last_feedback_time"] != DBNull.Value ? Convert.ToDateTime(row["last_feedback_time"]) : (DateTime?)null,
                    PositiveRate = total > 0 ? (row["positive_count"] != DBNull.Value ? Convert.ToInt32(row["positive_count"]) : 0) / (float)total : 0
                };
            }

            return new KnowledgeFeedbackStats { MemoryID = memoryId };
        }

        public DataSet UserFeedback_GetStatsByAppAndTime(string appId, DateTime startTime, DateTime endTime)
        {
            string sql = $@"
                SELECT
                    DATE(create_time) as feedback_date,
                    feedback_type,
                    COUNT(*) as count,
                    AVG(feedback_score) as avg_score
                FROM {TableName}
                WHERE app_id=@app_id
                  AND create_time BETWEEN @start_time AND @end_time
                GROUP BY DATE(create_time), feedback_type
                ORDER BY feedback_date DESC, feedback_type";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId },
                new NpgsqlParameter("@start_time", NpgsqlDbType.Timestamp) { Value = startTime },
                new NpgsqlParameter("@end_time", NpgsqlDbType.Timestamp) { Value = endTime }
            };

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
        }

        private List<UserFeedbackInfo> DataSetToList(DataSet ds)
        {
            var list = new List<UserFeedbackInfo>();
            if (ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    list.Add(UserFeedback_DataRowToModel(row));
                }
            }
            return list;
        }

        #endregion
    }
}
