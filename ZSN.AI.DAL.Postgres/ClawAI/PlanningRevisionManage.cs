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
    public partial class PlanningRevisionManage : IPlanningRevisionManage
    {
        /// 表链接
        private string PlanningRevisionConnectionName = "KnowledgeBaseDb";
        /// 表名称
        private string PlanningRevisionTableName = "tb_planning_revision";
        /// 表字段
        private const string PlanningRevisionTableField = "revision_id,planning_id,revision_version,revision_reason,content_before,content_after,create_time";
        /// 添加用表字段
        private const string PlanningRevisionTableFieldForAdd = "revision_id,planning_id,revision_version,revision_reason,content_before,content_after,create_time";
        /// 添加用表字段value
        private const string PlanningRevisionTableFieldAltForAdd = "@revision_id,@planning_id,@revision_version,@revision_reason,@content_before,@content_after,@create_time";

        public string SetConnectionName(string connName)
        {
            return PlanningRevisionConnectionName = connName;
        }

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public string PlanningRevision_Add(PlanningRevisionInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(PlanningRevisionTableName);
            strSql.Append(" (");
            strSql.Append(PlanningRevisionTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(PlanningRevisionTableFieldAltForAdd);
            strSql.Append(");");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@revision_id", NpgsqlDbType.Varchar) { Value = model.RevisionID },
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = model.PlanningID },
                new NpgsqlParameter("@revision_version", NpgsqlDbType.Integer) { Value = model.RevisionVersion },
                new NpgsqlParameter("@revision_reason", NpgsqlDbType.Text) { Value = model.RevisionReason },
                new NpgsqlParameter("@content_before", NpgsqlDbType.Text) { Value = model.ContentBefore },
                new NpgsqlParameter("@content_after", NpgsqlDbType.Text) { Value = model.ContentAfter },
                new NpgsqlParameter("@create_time", NpgsqlDbType.Timestamp) { Value = model.CreateTime }
            };

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(PlanningRevisionConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                return model.RevisionID;
            }
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool PlanningRevision_Update(PlanningRevisionInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(PlanningRevisionTableName);
            strSql.Append(" set ");
            strSql.Append("planning_id=@planning_id,");
            strSql.Append("revision_version=@revision_version,");
            strSql.Append("revision_reason=@revision_reason,");
            strSql.Append("content_before=@content_before,");
            strSql.Append("content_after=@content_after ");
            strSql.Append(" where revision_id=@revision_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@revision_id", NpgsqlDbType.Varchar) { Value = model.RevisionID },
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = model.PlanningID },
                new NpgsqlParameter("@revision_version", NpgsqlDbType.Integer) { Value = model.RevisionVersion },
                new NpgsqlParameter("@revision_reason", NpgsqlDbType.Text) { Value = model.RevisionReason },
                new NpgsqlParameter("@content_before", NpgsqlDbType.Text) { Value = model.ContentBefore },
                new NpgsqlParameter("@content_after", NpgsqlDbType.Text) { Value = model.ContentAfter }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(PlanningRevisionConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool PlanningRevision_Delete(string RevisionID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(PlanningRevisionTableName);
            strSql.Append(" where revision_id=@revision_id");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@revision_id", NpgsqlDbType.Varchar) { Value = RevisionID }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(PlanningRevisionConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public bool PlanningRevision_DeleteList(string RevisionIDlist)
        {
            if (string.IsNullOrWhiteSpace(RevisionIDlist))
            {
                return false;
            }

            // 分割ID列表并去除空白
            string[] items = RevisionIDlist.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
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
            strSql.Append(PlanningRevisionTableName);
            strSql.Append(" where revision_id = ANY(@revision_ids)");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@revision_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar) { Value = items }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(PlanningRevisionConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 根据规划ID删除所有修订
        /// </summary>
        public bool PlanningRevision_DeleteByPlanningID(string PlanningID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(PlanningRevisionTableName);
            strSql.Append(" where planning_id=@planning_id");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = PlanningID }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(PlanningRevisionConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public PlanningRevisionInfo PlanningRevision_GetModel(string RevisionID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(PlanningRevisionTableField);
            strSql.Append(" from ");
            strSql.Append(PlanningRevisionTableName);
            strSql.Append(" where revision_id=@revision_id");
            strSql.Append(" limit 1");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@revision_id", NpgsqlDbType.Varchar) { Value = RevisionID }
            };

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(PlanningRevisionConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return PlanningRevision_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public PlanningRevisionInfo PlanningRevision_DataRowToModel(DataRow row)
        {
            PlanningRevisionInfo model = new PlanningRevisionInfo();
            if (row != null)
            {
                if (row["revision_id"] != null)
                {
                    model.RevisionID = row["revision_id"].ToString();
                }
                if (row["planning_id"] != null)
                {
                    model.PlanningID = row["planning_id"].ToString();
                }
                if (row["revision_version"] != null)
                {
                    model.RevisionVersion = int.Parse(row["revision_version"].ToString());
                }
                if (row["revision_reason"] != null)
                {
                    model.RevisionReason = row["revision_reason"].ToString();
                }
                if (row["content_before"] != null)
                {
                    model.ContentBefore = row["content_before"].ToString();
                }
                if (row["content_after"] != null)
                {
                    model.ContentAfter = row["content_after"].ToString();
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
        public DataSet PlanningRevision_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(PlanningRevisionTableField);
            strSql.Append(" FROM ");
            strSql.Append(PlanningRevisionTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(PlanningRevisionConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet PlanningRevision_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(PlanningRevisionTableField);
            strSql.Append(" FROM ");
            strSql.Append(PlanningRevisionTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(PlanningRevisionConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int PlanningRevision_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(PlanningRevisionTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(PlanningRevisionConnectionName), CommandType.Text, strSql.ToString());
            if (obj == null)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(obj);
            }
        }
    }
}
