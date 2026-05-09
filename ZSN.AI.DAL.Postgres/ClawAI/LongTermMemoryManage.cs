using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using Newtonsoft.Json;
using NpgsqlTypes;

namespace ZSN.AI.DAL.Postgres
{
    /// <summary>
    /// 长期记忆数据访问类 - PostgreSQL实现（支持pgvector向量搜索）
    /// </summary>
    public class LongTermMemoryManage : ILongTermMemoryManage
    {
        ///表链接
        private string ConnectionName = "KnowledgeBaseDb";
        ///表名称
        private string TableName = "tb_claw_long_term_memory";
        ///表字段（向量列转换为text）
        private const string TableField = "memory_id,app_id,claw_id,session_id,member_id,knowledge_type,topic,summary,content,embedding::text as embedding,importance,access_count,last_access_time,source_type,source_id,metadata,create_time,last_update_time";

        public string SetConnectionName(string connName)
        {
            return ConnectionName = connName;
        }

        #region 基础CRUD操作

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public string LongTermMemory_Add(LongTermMemoryInfo model)
        {
            if (string.IsNullOrEmpty(model.MemoryID))
            {
                model.MemoryID = Guid.NewGuid().ToString();
            }

            string sql = $@"
                INSERT INTO {TableName} (
                    memory_id,app_id,claw_id,session_id,member_id,knowledge_type,topic,summary,content,
                    embedding,importance,access_count,last_access_time,source_type,source_id,metadata,create_time,last_update_time
                ) VALUES (
                    @memory_id,@app_id,@claw_id,@session_id,@member_id,@knowledge_type,@topic,@summary,@content,
                    CASE WHEN @embedding IS NULL OR @embedding = '' THEN NULL ELSE @embedding::vector END,@importance,@access_count,@last_access_time,@source_type,@source_id,@metadata::jsonb,@create_time,@last_update_time
                ) RETURNING memory_id";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = model.MemoryID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@claw_id", NpgsqlDbType.Varchar) { Value = model.ClawID },
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = model.SessionID ?? (object)DBNull.Value },
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = model.MemberID ?? (object)DBNull.Value },
                new NpgsqlParameter("@knowledge_type", NpgsqlDbType.Varchar) { Value = model.KnowledgeType },
                new NpgsqlParameter("@topic", NpgsqlDbType.Varchar) { Value = model.Topic ?? (object)DBNull.Value },
                new NpgsqlParameter("@summary", NpgsqlDbType.Text) { Value = model.Summary ?? (object)DBNull.Value },
                new NpgsqlParameter("@content", NpgsqlDbType.Text) { Value = model.Content ?? (object)DBNull.Value },
                new NpgsqlParameter("@embedding", NpgsqlDbType.Varchar) { Value = model.Embedding ?? "" },
                new NpgsqlParameter("@importance", NpgsqlDbType.Integer) { Value = model.Importance },
                new NpgsqlParameter("@access_count", NpgsqlDbType.Integer) { Value = model.AccessCount },
                new NpgsqlParameter("@last_access_time", NpgsqlDbType.Timestamp) { Value = model.LastAccessTime ?? (object)DBNull.Value },
                new NpgsqlParameter("@source_type", NpgsqlDbType.Varchar) { Value = model.SourceType ?? (object)DBNull.Value },
                new NpgsqlParameter("@source_id", NpgsqlDbType.Varchar) { Value = model.SourceID ?? (object)DBNull.Value },
                new NpgsqlParameter("@metadata", NpgsqlDbType.Text) { Value = string.IsNullOrEmpty(model.Metadata) ? "{}" : model.Metadata },
                new NpgsqlParameter("@create_time", NpgsqlDbType.Timestamp) { Value = model.CreateTime },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = model.LastUpdateTime }
            };

            var result = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return result?.ToString() ?? model.MemoryID;
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool LongTermMemory_Update(LongTermMemoryInfo model)
        {
            string sql = $@"
                UPDATE {TableName} SET
                    app_id=@app_id,claw_id=@claw_id,session_id=@session_id,member_id=@member_id,
                    knowledge_type=@knowledge_type,topic=@topic,summary=@summary,content=@content,
                    embedding=CASE WHEN @embedding IS NULL OR @embedding = '' THEN embedding ELSE @embedding::vector END,
                    importance=@importance,access_count=@access_count,last_access_time=@last_access_time,
                    source_type=@source_type,source_id=@source_id,metadata=@metadata::jsonb,
                    last_update_time=@last_update_time
                WHERE memory_id=@memory_id";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = model.MemoryID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@claw_id", NpgsqlDbType.Varchar) { Value = model.ClawID },
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = model.SessionID ?? (object)DBNull.Value },
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = model.MemberID ?? (object)DBNull.Value },
                new NpgsqlParameter("@knowledge_type", NpgsqlDbType.Varchar) { Value = model.KnowledgeType },
                new NpgsqlParameter("@topic", NpgsqlDbType.Varchar) { Value = model.Topic ?? (object)DBNull.Value },
                new NpgsqlParameter("@summary", NpgsqlDbType.Text) { Value = model.Summary ?? (object)DBNull.Value },
                new NpgsqlParameter("@content", NpgsqlDbType.Text) { Value = model.Content ?? (object)DBNull.Value },
                new NpgsqlParameter("@embedding", NpgsqlDbType.Varchar) { Value = model.Embedding ?? "" },
                new NpgsqlParameter("@importance", NpgsqlDbType.Integer) { Value = model.Importance },
                new NpgsqlParameter("@access_count", NpgsqlDbType.Integer) { Value = model.AccessCount },
                new NpgsqlParameter("@last_access_time", NpgsqlDbType.Timestamp) { Value = model.LastAccessTime ?? (object)DBNull.Value },
                new NpgsqlParameter("@source_type", NpgsqlDbType.Varchar) { Value = model.SourceType ?? (object)DBNull.Value },
                new NpgsqlParameter("@source_id", NpgsqlDbType.Varchar) { Value = model.SourceID ?? (object)DBNull.Value },
                new NpgsqlParameter("@metadata", NpgsqlDbType.Text) { Value = model.Metadata ?? "{}" },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = model.LastUpdateTime }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return rows > 0;
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool LongTermMemory_Delete(string MemoryID)
        {
            string sql = $"DELETE FROM {TableName} WHERE memory_id=@memory_id";
            var param = new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = MemoryID };
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return rows > 0;
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public bool LongTermMemory_DeleteList(string MemoryIDlist)
        {
            string sql = $"DELETE FROM {TableName} WHERE memory_id = ANY(@memory_ids)";
            var param = new NpgsqlParameter("@memory_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar) { Value = MemoryIDlist.Split(',') };
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return rows > 0;
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public LongTermMemoryInfo LongTermMemory_GetModel(string MemoryID)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE memory_id=@memory_id";
            var param = new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = MemoryID };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return LongTermMemory_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            return null;
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet LongTermMemory_GetList(string strWhere)
        {
            string where = string.IsNullOrWhiteSpace(strWhere) ? "" : $"WHERE {strWhere}";
            string sql = $"SELECT {TableField} FROM {TableName} {where} ORDER BY create_time DESC";
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql);
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet LongTermMemory_GetList(int top, string strWhere, string filedOrder)
        {
            string where = string.IsNullOrWhiteSpace(strWhere) ? "" : $"WHERE {strWhere}";
            string order = string.IsNullOrWhiteSpace(filedOrder) ? "ORDER BY create_time DESC" : $"ORDER BY {filedOrder}";
            string sql = $"SELECT {TableField} FROM {TableName} {where} {order} LIMIT {top}";
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql);
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int LongTermMemory_GetRecordCount(string strWhere)
        {
            string where = string.IsNullOrWhiteSpace(strWhere) ? "" : $"WHERE {strWhere}";
            string sql = $"SELECT COUNT(*) FROM {TableName} {where}";
            var result = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public DataTable LongTermMemory_GetListByPage(int size, int index, string where, out int pagetotal, out int total)
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

        /// <summary>
        /// DataRow转Model
        /// </summary>
        public LongTermMemoryInfo LongTermMemory_DataRowToModel(DataRow row)
        {
            LongTermMemoryInfo model = new LongTermMemoryInfo();
            if (row != null)
            {
                if (row["memory_id"] != null && row["memory_id"] != DBNull.Value)
                    model.MemoryID = row["memory_id"].ToString();
                if (row["app_id"] != null && row["app_id"] != DBNull.Value)
                    model.AppID = row["app_id"].ToString();
                if (row["claw_id"] != null && row["claw_id"] != DBNull.Value)
                    model.ClawID = row["claw_id"].ToString();
                if (row["session_id"] != null && row["session_id"] != DBNull.Value)
                    model.SessionID = row["session_id"].ToString();
                if (row["member_id"] != null && row["member_id"] != DBNull.Value)
                    model.MemberID = row["member_id"].ToString();
                if (row["knowledge_type"] != null && row["knowledge_type"] != DBNull.Value)
                    model.KnowledgeType = row["knowledge_type"].ToString();
                if (row["topic"] != null && row["topic"] != DBNull.Value)
                    model.Topic = row["topic"].ToString();
                if (row["summary"] != null && row["summary"] != DBNull.Value)
                    model.Summary = row["summary"].ToString();
                if (row["content"] != null && row["content"] != DBNull.Value)
                    model.Content = row["content"].ToString();
                if (row["embedding"] != null && row["embedding"] != DBNull.Value)
                    model.Embedding = row["embedding"].ToString();
                if (row["importance"] != null && row["importance"] != DBNull.Value)
                    model.Importance = Convert.ToInt32(row["importance"]);
                if (row["access_count"] != null && row["access_count"] != DBNull.Value)
                    model.AccessCount = Convert.ToInt32(row["access_count"]);
                if (row["last_access_time"] != null && row["last_access_time"] != DBNull.Value)
                    model.LastAccessTime = Convert.ToDateTime(row["last_access_time"]);
                if (row["source_type"] != null && row["source_type"] != DBNull.Value)
                    model.SourceType = row["source_type"].ToString();
                if (row["source_id"] != null && row["source_id"] != DBNull.Value)
                    model.SourceID = row["source_id"].ToString();
                if (row["metadata"] != null && row["metadata"] != DBNull.Value)
                    model.Metadata = row["metadata"].ToString();
                if (row["create_time"] != null && row["create_time"] != DBNull.Value)
                    model.CreateTime = Convert.ToDateTime(row["create_time"]);
                if (row["last_update_time"] != null && row["last_update_time"] != DBNull.Value)
                    model.LastUpdateTime = Convert.ToDateTime(row["last_update_time"]);
            }
            return model;
        }

        #endregion

        #region 扩展方法

        /// <summary>
        /// 根据AppID获取长期记忆列表
        /// </summary>
        public List<LongTermMemoryInfo> LongTermMemory_GetByApp(string AppID, int limit)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE app_id=@app_id ORDER BY importance DESC, access_count DESC LIMIT {limit}";
            var param = new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = AppID };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return DataSetToList(ds);
        }

        /// <summary>
        /// 根据AppID和主题获取长期记忆
        /// </summary>
        public List<LongTermMemoryInfo> LongTermMemory_GetByTopic(string AppID, string Topic, int limit)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE app_id=@app_id AND topic=@topic ORDER BY importance DESC, access_count DESC LIMIT {limit}";
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = AppID },
                new NpgsqlParameter("@topic", NpgsqlDbType.Varchar) { Value = Topic }
            };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return DataSetToList(ds);
        }

        /// <summary>
        /// 根据知识类型获取长期记忆
        /// </summary>
        public List<LongTermMemoryInfo> LongTermMemory_GetByKnowledgeType(string AppID, string KnowledgeType, int limit)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE app_id=@app_id AND knowledge_type=@knowledge_type ORDER BY importance DESC LIMIT {limit}";
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = AppID },
                new NpgsqlParameter("@knowledge_type", NpgsqlDbType.Varchar) { Value = KnowledgeType }
            };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return DataSetToList(ds);
        }

        /// <summary>
        /// 增加访问次数
        /// </summary>
        public bool LongTermMemory_IncrementAccessCount(string MemoryID)
        {
            string sql = $"UPDATE {TableName} SET access_count = access_count + 1, last_access_time = CURRENT_TIMESTAMP WHERE memory_id=@memory_id";
            var param = new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = MemoryID };
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return rows > 0;
        }

        /// <summary>
        /// 更新向量嵌入
        /// </summary>
        public bool LongTermMemory_UpdateEmbedding(string MemoryID, string Embedding)
        {
            string sql = $"UPDATE {TableName} SET embedding = @embedding::vector WHERE memory_id=@memory_id";
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = MemoryID },
                new NpgsqlParameter("@embedding", NpgsqlDbType.Varchar) { Value = Embedding }
            };
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return rows > 0;
        }

        /// <summary>
        /// 批量插入长期记忆
        /// </summary>
        public int LongTermMemory_AddBatch(List<LongTermMemoryInfo> memories)
        {
            int count = 0;
            foreach (var memory in memories)
            {
                if (LongTermMemory_Add(memory) != null)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 根据重要性和访问频率获取热门知识
        /// </summary>
        public List<LongTermMemoryInfo> LongTermMemory_GetHotKnowledge(string AppID, int limit)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE app_id=@app_id ORDER BY (importance * 0.6 + access_count * 0.4) DESC LIMIT {limit}";
            var param = new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = AppID };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return DataSetToList(ds);
        }

        private List<LongTermMemoryInfo> DataSetToList(DataSet ds)
        {
            var list = new List<LongTermMemoryInfo>();
            if (ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    list.Add(LongTermMemory_DataRowToModel(row));
                }
            }
            return list;
        }

        #endregion

        #region P3 优化 - PostgreSQL 向量搜索

        /// <summary>
        /// 语义相似度检索（使用pgvector）
        /// 注意：此方法需要先生成query的向量嵌入，可以通过外部服务或本地模型生成
        /// </summary>
        public List<LongTermMemoryInfo> LongTermMemory_SearchBySimilarity(
            string query, string appId, string memberId, string clawId,
            int topK, float minSimilarity)
        {
            // 构建基础WHERE条件
            List<string> conditions = new List<string> { "app_id=@app_id" };
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId }
            };

            if (!string.IsNullOrEmpty(memberId))
            {
                conditions.Add("member_id=@member_id");
                parameters.Add(new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = memberId });
            }

            if (!string.IsNullOrEmpty(clawId))
            {
                conditions.Add("claw_id=@claw_id");
                parameters.Add(new NpgsqlParameter("@claw_id", NpgsqlDbType.Varchar) { Value = clawId });
            }

            string whereClause = string.Join(" AND ", conditions);

            // 尝试使用向量相似度搜索（pgvector）
            // 首先检查是否有embedding字段且不为NULL
            string vectorSql = $@"
                SELECT {TableField}
                FROM {TableName}
                WHERE {whereClause}
                  AND embedding IS NOT NULL
                  AND 1 - (embedding <=> (SELECT embedding FROM {TableName} WHERE {whereClause} AND (summary ILIKE @query_pattern OR content ILIKE @query_pattern OR topic ILIKE @query_pattern) LIMIT 1)) >= @min_similarity
                ORDER BY embedding <=> (SELECT embedding FROM {TableName} WHERE {whereClause} AND (summary ILIKE @query_pattern OR content ILIKE @query_pattern OR topic ILIKE @query_pattern) LIMIT 1)
                LIMIT {topK}";

            parameters.Add(new NpgsqlParameter("@query_pattern", NpgsqlDbType.Text) { Value = $"%{query}%" });
            parameters.Add(new NpgsqlParameter("@min_similarity", NpgsqlDbType.Real) { Value = minSimilarity });

            try
            {
                DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, vectorSql, parameters.ToArray());

                // 如果向量搜索返回结果，直接返回
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    return DataSetToList(ds);
                }
            }
            catch (Exception ex)
            {
                // 如果向量搜索失败（如没有embedding数据），回退到文本搜索
            }

            // 回退方案：使用文本相似度搜索
            string textSql = $@"
                SELECT {TableField}
                FROM {TableName}
                WHERE {whereClause}
                  AND (summary ILIKE @query_pattern OR content ILIKE @query_pattern OR topic ILIKE @query_pattern)
                ORDER BY
                    CASE
                        WHEN summary ILIKE @query_pattern THEN 1
                        WHEN topic ILIKE @query_pattern THEN 2
                        ELSE 3
                    END,
                    importance DESC,
                    access_count DESC
                LIMIT {topK}";

            DataSet dsText = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, textSql, parameters.ToArray());
            return DataSetToList(dsText);
        }

        /// <summary>
        /// 批量更新向量嵌入
        /// </summary>
        public int LongTermMemory_UpdateEmbeddingBatch(string[] memoryIds, string[] embeddings)
        {
            if (memoryIds == null || embeddings == null || memoryIds.Length != embeddings.Length)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < memoryIds.Length; i++)
            {
                if (LongTermMemory_UpdateEmbedding(memoryIds[i], embeddings[i]))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 根据向量相似度和重要性获取记忆（混合排序）
        /// </summary>
        public List<LongTermMemoryInfo> LongTermMemory_GetByVectorAndImportance(
            string query, string appId, string clawId, int limit)
        {
            // 简化实现：使用文本匹配和重要性排序
            string sql = $@"
                SELECT {TableField}
                FROM {TableName}
                WHERE app_id=@app_id AND claw_id=@claw_id
                  AND (summary ILIKE @query_pattern OR content ILIKE @query_pattern OR topic ILIKE @query_pattern)
                ORDER BY importance DESC, access_count DESC
                LIMIT {limit}";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId },
                new NpgsqlParameter("@claw_id", NpgsqlDbType.Varchar) { Value = clawId },
                new NpgsqlParameter("@query_pattern", NpgsqlDbType.Text) { Value = $"%{query}%" }
            };

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return DataSetToList(ds);
        }

        #endregion

        #region 统计方法

        /// <summary>
        /// 按知识类型统计数量
        /// </summary>
        public DataTable LongTermMemory_GetCountByKnowledgeType(string appId = "")
        {
            string sql = $@"
                SELECT knowledge_type as type, COUNT(*) as count
                FROM {TableName}
                WHERE 1=1";

            if (!string.IsNullOrEmpty(appId))
            {
                sql += " AND app_id = @app_id";
            }

            sql += " GROUP BY knowledge_type ORDER BY count DESC";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
            if (!string.IsNullOrEmpty(appId))
            {
                parameters.Add(new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId });
            }

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray()).Tables[0];
        }

        /// <summary>
        /// 按主题统计数量（Top N）
        /// </summary>
        public DataTable LongTermMemory_GetCountByTopic(string appId = "", int topN = 10)
        {
            string sql = $@"
                SELECT topic, COUNT(*) as count
                FROM {TableName}
                WHERE topic IS NOT NULL AND topic != ''";

            if (!string.IsNullOrEmpty(appId))
            {
                sql += " AND app_id = @app_id";
            }

            sql += $" GROUP BY topic ORDER BY count DESC LIMIT {topN}";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
            if (!string.IsNullOrEmpty(appId))
            {
                parameters.Add(new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId });
            }

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray()).Tables[0];
        }

        /// <summary>
        /// 获取基础统计数据
        /// </summary>
        public DataTable LongTermMemory_GetStatistics(string appId = "")
        {
            string sql = $@"
                SELECT
                    COUNT(*) as total_count,
                    AVG(importance) as avg_importance,
                    AVG(access_count) as avg_access_count,
                    MAX(importance) as max_importance,
                    MIN(importance) as min_importance
                FROM {TableName}
                WHERE 1=1";

            if (!string.IsNullOrEmpty(appId))
            {
                sql += " AND app_id = @app_id";
            }

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
            if (!string.IsNullOrEmpty(appId))
            {
                parameters.Add(new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId });
            }

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray()).Tables[0];
        }

        /// <summary>
        /// 获取重要性分布统计
        /// </summary>
        public DataTable LongTermMemory_GetImportanceDistribution(string appId = "")
        {
            string sql = $@"
                SELECT
                    CASE
                        WHEN importance >= 80 THEN 'high'
                        WHEN importance >= 50 THEN 'medium'
                        ELSE 'low'
                    END as level,
                    COUNT(*) as count
                FROM {TableName}
                WHERE 1=1";

            if (!string.IsNullOrEmpty(appId))
            {
                sql += " AND app_id = @app_id";
            }

            sql += " GROUP BY level ORDER BY level DESC";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
            if (!string.IsNullOrEmpty(appId))
            {
                parameters.Add(new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId });
            }

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray()).Tables[0];
        }

        #endregion
    }
}
