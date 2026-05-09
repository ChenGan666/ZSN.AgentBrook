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
    /// 知识关系数据访问类 - PostgreSQL实现
    /// </summary>
    public class KnowledgeRelationManage : IKnowledgeRelationManage
    {
        ///表链接
        private string ConnectionName = "KnowledgeBaseDb";
        ///表名称
        private string TableName = "tb_claw_knowledge_relation";
        ///表字段
        private const string TableField = "relation_id,app_id,source_memory_id,target_memory_id,relation_type,strength,metadata,create_time,last_update_time";

        public string SetConnectionName(string connName)
        {
            return ConnectionName = connName;
        }

        #region 基础CRUD操作

        public string KnowledgeRelation_Add(KnowledgeRelationInfo model)
        {
            if (string.IsNullOrEmpty(model.RelationID))
            {
                model.RelationID = Guid.NewGuid().ToString();
            }

            string sql = $@"
                INSERT INTO {TableName} (
                    relation_id,app_id,source_memory_id,target_memory_id,relation_type,strength,metadata,create_time,last_update_time
                ) VALUES (
                    @relation_id,@app_id,@source_memory_id,@target_memory_id,@relation_type,@strength,@metadata::jsonb,@create_time,@last_update_time
                ) RETURNING relation_id";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@relation_id", NpgsqlDbType.Varchar) { Value = model.RelationID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@source_memory_id", NpgsqlDbType.Varchar) { Value = model.SourceMemoryID },
                new NpgsqlParameter("@target_memory_id", NpgsqlDbType.Varchar) { Value = model.TargetMemoryID },
                new NpgsqlParameter("@relation_type", NpgsqlDbType.Varchar) { Value = model.RelationType },
                new NpgsqlParameter("@strength", NpgsqlDbType.Real) { Value = model.Strength },
                new NpgsqlParameter("@metadata", NpgsqlDbType.Text) { Value = model.Metadata ?? "{}" },
                new NpgsqlParameter("@create_time", NpgsqlDbType.Timestamp) { Value = model.CreateTime },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = model.LastUpdateTime }
            };

            var result = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return result?.ToString() ?? model.RelationID;
        }

        public bool KnowledgeRelation_Update(KnowledgeRelationInfo model)
        {
            string sql = $@"
                UPDATE {TableName} SET
                    app_id=@app_id,source_memory_id=@source_memory_id,target_memory_id=@target_memory_id,
                    relation_type=@relation_type,strength=@strength,metadata=@metadata::jsonb,
                    last_update_time=@last_update_time
                WHERE relation_id=@relation_id";

            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@relation_id", NpgsqlDbType.Varchar) { Value = model.RelationID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@source_memory_id", NpgsqlDbType.Varchar) { Value = model.SourceMemoryID },
                new NpgsqlParameter("@target_memory_id", NpgsqlDbType.Varchar) { Value = model.TargetMemoryID },
                new NpgsqlParameter("@relation_type", NpgsqlDbType.Varchar) { Value = model.RelationType },
                new NpgsqlParameter("@strength", NpgsqlDbType.Real) { Value = model.Strength },
                new NpgsqlParameter("@metadata", NpgsqlDbType.Text) { Value = model.Metadata ?? "{}" },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = model.LastUpdateTime }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return rows > 0;
        }

        public bool KnowledgeRelation_Delete(string RelationID)
        {
            string sql = $"DELETE FROM {TableName} WHERE relation_id=@relation_id";
            var param = new NpgsqlParameter("@relation_id", NpgsqlDbType.Varchar) { Value = RelationID };
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return rows > 0;
        }

        public bool KnowledgeRelation_DeleteList(string RelationIDlist)
        {
            string sql = $"DELETE FROM {TableName} WHERE relation_id = ANY(@relation_ids)";
            var param = new NpgsqlParameter("@relation_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar) { Value = RelationIDlist.Split(',') };
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return rows > 0;
        }

        public KnowledgeRelationInfo KnowledgeRelation_GetModel(string RelationID)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE relation_id=@relation_id";
            var param = new NpgsqlParameter("@relation_id", NpgsqlDbType.Varchar) { Value = RelationID };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return KnowledgeRelation_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            return null;
        }

        public DataSet KnowledgeRelation_GetList(string strWhere)
        {
            string where = string.IsNullOrWhiteSpace(strWhere) ? "" : $"WHERE {strWhere}";
            string sql = $"SELECT {TableField} FROM {TableName} {where} ORDER BY create_time DESC";
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql);
        }

        public DataSet KnowledgeRelation_GetList(int top, string strWhere, string filedOrder)
        {
            string where = string.IsNullOrWhiteSpace(strWhere) ? "" : $"WHERE {strWhere}";
            string order = string.IsNullOrWhiteSpace(filedOrder) ? "ORDER BY create_time DESC" : $"ORDER BY {filedOrder}";
            string sql = $"SELECT {TableField} FROM {TableName} {where} {order} LIMIT {top}";
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql);
        }

        public int KnowledgeRelation_GetRecordCount(string strWhere)
        {
            string where = string.IsNullOrWhiteSpace(strWhere) ? "" : $"WHERE {strWhere}";
            string sql = $"SELECT COUNT(*) FROM {TableName} {where}";
            var result = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public DataTable KnowledgeRelation_GetListByPage(int size, int index, string where, out int pagetotal, out int total)
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

        public KnowledgeRelationInfo KnowledgeRelation_DataRowToModel(DataRow row)
        {
            KnowledgeRelationInfo model = new KnowledgeRelationInfo();
            if (row != null)
            {
                if (row["relation_id"] != null && row["relation_id"] != DBNull.Value)
                    model.RelationID = row["relation_id"].ToString();
                if (row["app_id"] != null && row["app_id"] != DBNull.Value)
                    model.AppID = row["app_id"].ToString();
                if (row["source_memory_id"] != null && row["source_memory_id"] != DBNull.Value)
                    model.SourceMemoryID = row["source_memory_id"].ToString();
                if (row["target_memory_id"] != null && row["target_memory_id"] != DBNull.Value)
                    model.TargetMemoryID = row["target_memory_id"].ToString();
                if (row["relation_type"] != null && row["relation_type"] != DBNull.Value)
                    model.RelationType = row["relation_type"].ToString();
                if (row["strength"] != null && row["strength"] != DBNull.Value)
                    model.Strength = Convert.ToSingle(row["strength"]);
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

        public List<KnowledgeRelationInfo> KnowledgeRelation_GetBySourceId(string sourceMemoryId)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE source_memory_id=@source_memory_id ORDER BY strength DESC";
            var param = new NpgsqlParameter("@source_memory_id", NpgsqlDbType.Varchar) { Value = sourceMemoryId };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return DataSetToList(ds);
        }

        public List<KnowledgeRelationInfo> KnowledgeRelation_GetByTargetId(string targetMemoryId)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE target_memory_id=@target_memory_id ORDER BY strength DESC";
            var param = new NpgsqlParameter("@target_memory_id", NpgsqlDbType.Varchar) { Value = targetMemoryId };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return DataSetToList(ds);
        }

        public List<KnowledgeRelationInfo> KnowledgeRelation_GetByAppAndType(string appId, string relationType, int limit)
        {
            string sql = $"SELECT {TableField} FROM {TableName} WHERE app_id=@app_id AND relation_type=@relation_type ORDER BY strength DESC LIMIT {limit}";
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = appId },
                new NpgsqlParameter("@relation_type", NpgsqlDbType.Varchar) { Value = relationType }
            };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return DataSetToList(ds);
        }

        public int KnowledgeRelation_AddBatch(List<KnowledgeRelationInfo> relations)
        {
            int count = 0;
            foreach (var relation in relations)
            {
                if (KnowledgeRelation_Add(relation) != null)
                {
                    count++;
                }
            }
            return count;
        }

        public bool KnowledgeRelation_DeleteByMemoryId(string memoryId)
        {
            string sql = $"DELETE FROM {TableName} WHERE source_memory_id=@memory_id OR target_memory_id=@memory_id";
            var param = new NpgsqlParameter("@memory_id", NpgsqlDbType.Varchar) { Value = memoryId };
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, param);
            return rows > 0;
        }

        public float KnowledgeRelation_GetStrength(string sourceId, string targetId, string relationType)
        {
            string sql = $"SELECT strength FROM {TableName} WHERE source_memory_id=@source_id AND target_memory_id=@target_id AND relation_type=@relation_type LIMIT 1";
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@source_id", NpgsqlDbType.Varchar) { Value = sourceId },
                new NpgsqlParameter("@target_id", NpgsqlDbType.Varchar) { Value = targetId },
                new NpgsqlParameter("@relation_type", NpgsqlDbType.Varchar) { Value = relationType }
            };
            var result = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return result != null && result != DBNull.Value ? Convert.ToSingle(result) : 0f;
        }

        public bool KnowledgeRelation_UpdateStrength(string relationId, float newStrength)
        {
            string sql = $"UPDATE {TableName} SET strength=@strength, last_update_time=CURRENT_TIMESTAMP WHERE relation_id=@relation_id";
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@relation_id", NpgsqlDbType.Varchar) { Value = relationId },
                new NpgsqlParameter("@strength", NpgsqlDbType.Real) { Value = newStrength }
            };
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, parameters.ToArray());
            return rows > 0;
        }

        private List<KnowledgeRelationInfo> DataSetToList(DataSet ds)
        {
            var list = new List<KnowledgeRelationInfo>();
            if (ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    list.Add(KnowledgeRelation_DataRowToModel(row));
                }
            }
            return list;
        }

        #endregion

        #region 统计方法

        /// <summary>
        /// 按关系类型统计数量
        /// </summary>
        public DataTable KnowledgeRelation_GetCountByType(string appId = "")
        {
            string sql = $@"
                SELECT relation_type as type, COUNT(*) as count
                FROM {TableName}
                WHERE 1=1";

            if (!string.IsNullOrEmpty(appId))
            {
                sql += " AND app_id = @app_id";
            }

            sql += " GROUP BY relation_type ORDER BY count DESC";

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
        public DataTable KnowledgeRelation_GetStatistics(string appId = "")
        {
            string sql = $@"
                SELECT
                    COUNT(*) as total_count,
                    AVG(strength) as avg_strength,
                    MAX(strength) as max_strength,
                    MIN(strength) as min_strength
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
        /// 按强度区间统计数量
        /// </summary>
        public DataTable KnowledgeRelation_GetStrengthDistribution(string appId = "")
        {
            string sql = $@"
                SELECT
                    CASE
                        WHEN strength >= 0.8 THEN 'very_high'
                        WHEN strength >= 0.6 THEN 'high'
                        WHEN strength >= 0.4 THEN 'medium'
                        WHEN strength >= 0.2 THEN 'low'
                        ELSE 'very_low'
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
