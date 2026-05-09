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
    public partial class UserProfileManage : IUserProfileManage
    {
        /// 表链接
        private string UserProfileConnectionName = "KnowledgeBaseDb";
        /// 表名称
        private string UserProfileTableName = "tb_user_profile";
        /// 表字段
        private const string UserProfileTableField = "profile_id,member_id,app_id,preferences_summary,preferences_detail,interaction_patterns_summary,interaction_patterns_detail,personalization_strength,total_interactions,create_time,last_update_time";
        /// 添加用表字段
        private const string UserProfileTableFieldForAdd = "profile_id,member_id,app_id,preferences_summary,preferences_detail,interaction_patterns_summary,interaction_patterns_detail,personalization_strength,total_interactions,create_time,last_update_time";
        /// 添加用表字段value
        private const string UserProfileTableFieldAltForAdd = "@profile_id,@member_id,@app_id,@preferences_summary,@preferences_detail,@interaction_patterns_summary,@interaction_patterns_detail,@personalization_strength,@total_interactions,@create_time,@last_update_time";

        public string SetConnectionName(string connName)
        {
            return UserProfileConnectionName = connName;
        }

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public string UserProfile_Add(UserProfileInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(UserProfileTableName);
            strSql.Append(" (");
            strSql.Append(UserProfileTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(UserProfileTableFieldAltForAdd);
            strSql.Append(");");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@profile_id", NpgsqlDbType.Varchar) { Value = model.ProfileID },
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = model.MemberID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@preferences_summary", NpgsqlDbType.Text) { Value = model.PreferencesSummary },
                new NpgsqlParameter("@preferences_detail", NpgsqlDbType.Text) { Value = model.PreferencesDetail },
                new NpgsqlParameter("@interaction_patterns_summary", NpgsqlDbType.Text) { Value = model.InteractionPatternsSummary },
                new NpgsqlParameter("@interaction_patterns_detail", NpgsqlDbType.Text) { Value = model.InteractionPatternsDetail },
                new NpgsqlParameter("@personalization_strength", NpgsqlDbType.Integer) { Value = model.PersonalizationStrength },
                new NpgsqlParameter("@total_interactions", NpgsqlDbType.Integer) { Value = model.TotalInteractions },
                new NpgsqlParameter("@create_time", NpgsqlDbType.Timestamp) { Value = model.CreateTime },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = model.LastUpdateTime }
            };

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(UserProfileConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                return model.ProfileID;
            }
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool UserProfile_Update(UserProfileInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(UserProfileTableName);
            strSql.Append(" set ");
            strSql.Append("member_id=@member_id,");
            strSql.Append("app_id=@app_id,");
            strSql.Append("preferences_summary=@preferences_summary,");
            strSql.Append("preferences_detail=@preferences_detail,");
            strSql.Append("interaction_patterns_summary=@interaction_patterns_summary,");
            strSql.Append("interaction_patterns_detail=@interaction_patterns_detail,");
            strSql.Append("personalization_strength=@personalization_strength,");
            strSql.Append("total_interactions=@total_interactions,");
            strSql.Append("last_update_time=@last_update_time ");
            strSql.Append(" where profile_id=@profile_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@profile_id", NpgsqlDbType.Varchar) { Value = model.ProfileID },
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = model.MemberID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@preferences_summary", NpgsqlDbType.Text) { Value = model.PreferencesSummary },
                new NpgsqlParameter("@preferences_detail", NpgsqlDbType.Text) { Value = model.PreferencesDetail },
                new NpgsqlParameter("@interaction_patterns_summary", NpgsqlDbType.Text) { Value = model.InteractionPatternsSummary },
                new NpgsqlParameter("@interaction_patterns_detail", NpgsqlDbType.Text) { Value = model.InteractionPatternsDetail },
                new NpgsqlParameter("@personalization_strength", NpgsqlDbType.Integer) { Value = model.PersonalizationStrength },
                new NpgsqlParameter("@total_interactions", NpgsqlDbType.Integer) { Value = model.TotalInteractions },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = model.LastUpdateTime }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(UserProfileConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool UserProfile_Delete(string ProfileID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(UserProfileTableName);
            strSql.Append(" where profile_id=@profile_id");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@profile_id", NpgsqlDbType.Varchar) { Value = ProfileID }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(UserProfileConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public bool UserProfile_DeleteList(string ProfileIDlist)
        {
            if (string.IsNullOrWhiteSpace(ProfileIDlist))
            {
                return false;
            }

            // 分割ID列表并去除空白
            string[] items = ProfileIDlist.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
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
            strSql.Append(UserProfileTableName);
            strSql.Append(" where profile_id = ANY(@profile_ids)");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@profile_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar) { Value = items }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(UserProfileConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public UserProfileInfo UserProfile_GetModel(string ProfileID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(UserProfileTableField);
            strSql.Append(" from ");
            strSql.Append(UserProfileTableName);
            strSql.Append(" where profile_id=@profile_id");
            strSql.Append(" limit 1");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@profile_id", NpgsqlDbType.Varchar) { Value = ProfileID }
            };

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(UserProfileConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return UserProfile_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 根据用户ID和应用ID获取画像
        /// </summary>
        public UserProfileInfo UserProfile_GetByMemberAndApp(string MemberID, string AppID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(UserProfileTableField);
            strSql.Append(" from ");
            strSql.Append(UserProfileTableName);
            strSql.Append(" where member_id=@member_id AND app_id=@app_id");
            strSql.Append(" limit 1");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = MemberID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = AppID }
            };

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(UserProfileConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return UserProfile_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public UserProfileInfo UserProfile_DataRowToModel(DataRow row)
        {
            UserProfileInfo model = new UserProfileInfo();
            if (row != null)
            {
                if (row["profile_id"] != null)
                {
                    model.ProfileID = row["profile_id"].ToString();
                }
                if (row["member_id"] != null)
                {
                    model.MemberID = row["member_id"].ToString();
                }
                if (row["app_id"] != null)
                {
                    model.AppID = row["app_id"].ToString();
                }
                if (row["preferences_summary"] != null)
                {
                    model.PreferencesSummary = row["preferences_summary"].ToString();
                }
                if (row["preferences_detail"] != null)
                {
                    model.PreferencesDetail = row["preferences_detail"].ToString();
                }
                if (row["interaction_patterns_summary"] != null)
                {
                    model.InteractionPatternsSummary = row["interaction_patterns_summary"].ToString();
                }
                if (row["interaction_patterns_detail"] != null)
                {
                    model.InteractionPatternsDetail = row["interaction_patterns_detail"].ToString();
                }
                if (row["personalization_strength"] != null)
                {
                    model.PersonalizationStrength = int.Parse(row["personalization_strength"].ToString());
                }
                if (row["total_interactions"] != null)
                {
                    model.TotalInteractions = int.Parse(row["total_interactions"].ToString());
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
        public DataSet UserProfile_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(UserProfileTableField);
            strSql.Append(" FROM ");
            strSql.Append(UserProfileTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(UserProfileConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet UserProfile_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(UserProfileTableField);
            strSql.Append(" FROM ");
            strSql.Append(UserProfileTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(UserProfileConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int UserProfile_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(UserProfileTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(UserProfileConnectionName), CommandType.Text, strSql.ToString());
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
        public bool UserProfile_IncrementInteractions(string ProfileID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(UserProfileTableName);
            strSql.Append(" set total_interactions=total_interactions+1, last_update_time=@last_update_time");
            strSql.Append(" where profile_id=@profile_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@profile_id", NpgsqlDbType.Varchar) { Value = ProfileID },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = DateTime.Now }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(UserProfileConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }
    }
}
