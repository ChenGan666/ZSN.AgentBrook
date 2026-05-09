using System;
using System.Data;
using Npgsql;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using NpgsqlTypes;

namespace ZSN.AI.DAL.Postgres
{
    public partial class AIPersonalityStateManage : IAIPersonalityStateManage
    {
        /// 表链接
        private string AIPersonalityStateConnectionName = "KnowledgeBaseDb";
        /// 表名称
        private string AIPersonalityStateTableName = "tb_ai_personality_state";
        /// 表字段
        private const string AIPersonalityStateTableField = "state_id,session_id,app_id,personality_traits,emotional_state,current_goals,interaction_count,success_rate,create_time,last_update_time";
        /// 添加用表字段
        private const string AIPersonalityStateTableFieldForAdd = "state_id,session_id,app_id,personality_traits,emotional_state,current_goals,interaction_count,success_rate,create_time,last_update_time";
        /// 添加用表字段value
        private const string AIPersonalityStateTableFieldAltForAdd = "@state_id,@session_id,@app_id,@personality_traits,@emotional_state,@current_goals,@interaction_count,@success_rate,@create_time,@last_update_time";

        public string SetConnectionName(string connName)
        {
            return AIPersonalityStateConnectionName = connName;
        }

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public string AIPersonalityState_Add(AIPersonalityStateInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(AIPersonalityStateTableName);
            strSql.Append(" (");
            strSql.Append(AIPersonalityStateTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(AIPersonalityStateTableFieldAltForAdd);
            strSql.Append(");");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@state_id", NpgsqlDbType.Varchar) { Value = model.StateID },
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = model.SessionID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@personality_traits", NpgsqlDbType.Text) { Value = model.PersonalityTraits },
                new NpgsqlParameter("@emotional_state", NpgsqlDbType.Text) { Value = model.EmotionalState },
                new NpgsqlParameter("@current_goals", NpgsqlDbType.Text) { Value = model.CurrentGoals },
                new NpgsqlParameter("@interaction_count", NpgsqlDbType.Integer) { Value = model.InteractionCount },
                new NpgsqlParameter("@success_rate", NpgsqlDbType.Numeric) { Value = model.SuccessRate },
                new NpgsqlParameter("@create_time", NpgsqlDbType.Timestamp) { Value = model.CreateTime },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = model.LastUpdateTime }
            };

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                return model.StateID;
            }
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool AIPersonalityState_Update(AIPersonalityStateInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(AIPersonalityStateTableName);
            strSql.Append(" set ");
            strSql.Append("session_id=@session_id,");
            strSql.Append("app_id=@app_id,");
            strSql.Append("personality_traits=@personality_traits,");
            strSql.Append("emotional_state=@emotional_state,");
            strSql.Append("current_goals=@current_goals,");
            strSql.Append("interaction_count=@interaction_count,");
            strSql.Append("success_rate=@success_rate,");
            strSql.Append("last_update_time=@last_update_time ");
            strSql.Append(" where state_id=@state_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@state_id", NpgsqlDbType.Varchar) { Value = model.StateID },
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = model.SessionID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@personality_traits", NpgsqlDbType.Text) { Value = model.PersonalityTraits },
                new NpgsqlParameter("@emotional_state", NpgsqlDbType.Text) { Value = model.EmotionalState },
                new NpgsqlParameter("@current_goals", NpgsqlDbType.Text) { Value = model.CurrentGoals },
                new NpgsqlParameter("@interaction_count", NpgsqlDbType.Integer) { Value = model.InteractionCount },
                new NpgsqlParameter("@success_rate", NpgsqlDbType.Numeric) { Value = model.SuccessRate },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = model.LastUpdateTime }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool AIPersonalityState_Delete(string StateID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AIPersonalityStateTableName);
            strSql.Append(" where state_id=@state_id");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@state_id", NpgsqlDbType.Varchar) { Value = StateID }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public bool AIPersonalityState_DeleteList(string StateIDlist)
        {
            if (string.IsNullOrWhiteSpace(StateIDlist))
            {
                return false;
            }

            // 分割ID列表并去除空白
            string[] items = StateIDlist.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
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
            strSql.Append(AIPersonalityStateTableName);
            strSql.Append(" where state_id = ANY(@state_ids)");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@state_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar) { Value = items }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public AIPersonalityStateInfo AIPersonalityState_GetModel(string StateID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AIPersonalityStateTableField);
            strSql.Append(" from ");
            strSql.Append(AIPersonalityStateTableName);
            strSql.Append(" where state_id=@state_id");
            strSql.Append(" limit 1");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@state_id", NpgsqlDbType.Varchar) { Value = StateID }
            };

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return AIPersonalityState_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 根据会话ID获取状态
        /// </summary>
        public AIPersonalityStateInfo AIPersonalityState_GetBySessionID(string SessionID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AIPersonalityStateTableField);
            strSql.Append(" from ");
            strSql.Append(AIPersonalityStateTableName);
            strSql.Append(" where session_id=@session_id");
            strSql.Append(" limit 1");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = SessionID }
            };

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return AIPersonalityState_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public AIPersonalityStateInfo AIPersonalityState_DataRowToModel(DataRow row)
        {
            AIPersonalityStateInfo model = new AIPersonalityStateInfo();
            if (row != null)
            {
                if (row["state_id"] != null)
                {
                    model.StateID = row["state_id"].ToString();
                }
                if (row["session_id"] != null)
                {
                    model.SessionID = row["session_id"].ToString();
                }
                if (row["app_id"] != null)
                {
                    model.AppID = row["app_id"].ToString();
                }
                if (row["personality_traits"] != null)
                {
                    model.PersonalityTraits = row["personality_traits"].ToString();
                }
                if (row["emotional_state"] != null)
                {
                    model.EmotionalState = row["emotional_state"].ToString();
                }
                if (row["current_goals"] != null)
                {
                    model.CurrentGoals = row["current_goals"].ToString();
                }
                if (row["interaction_count"] != null)
                {
                    model.InteractionCount = int.Parse(row["interaction_count"].ToString());
                }
                if (row["success_rate"] != null)
                {
                    model.SuccessRate = decimal.Parse(row["success_rate"].ToString());
                }
                if (row["create_time"] != null)
                {
                    model.CreateTime = DateTime.Parse(row["create_time"].ToString());
                }
                if (row["last_update_time"] != null)
                {
                    model.LastUpdateTime = DateTime.Parse(row["last_update_time"].ToString());
                }
            }
            return model;
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet AIPersonalityState_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AIPersonalityStateTableField);
            strSql.Append(" FROM ");
            strSql.Append(AIPersonalityStateTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet AIPersonalityState_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AIPersonalityStateTableField);
            strSql.Append(" FROM ");
            strSql.Append(AIPersonalityStateTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int AIPersonalityState_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(AIPersonalityStateTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString());
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
        /// 增加交互次数
        /// </summary>
        public bool AIPersonalityState_IncrementInteractions(string StateID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(AIPersonalityStateTableName);
            strSql.Append(" set interaction_count=interaction_count+1, last_update_time=@last_update_time");
            strSql.Append(" where state_id=@state_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@state_id", NpgsqlDbType.Varchar) { Value = StateID },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = DateTime.Now }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 更新成功率
        /// </summary>
        public bool AIPersonalityState_UpdateSuccessRate(string StateID, decimal successRate)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(AIPersonalityStateTableName);
            strSql.Append(" set success_rate=@success_rate, last_update_time=@last_update_time");
            strSql.Append(" where state_id=@state_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@state_id", NpgsqlDbType.Varchar) { Value = StateID },
                new NpgsqlParameter("@success_rate", NpgsqlDbType.Numeric) { Value = successRate },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = DateTime.Now }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AIPersonalityStateConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }
    }
}
