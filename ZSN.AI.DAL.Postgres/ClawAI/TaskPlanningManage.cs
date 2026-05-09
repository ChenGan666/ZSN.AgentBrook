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
    public partial class TaskPlanningManage : ITaskPlanningManage
    {
        /// 表链接
        private string TaskPlanningConnectionName = "KnowledgeBaseDb";
        /// 表名称
        private string TaskPlanningTableName = "tb_task_planning";
        /// 表字段
        private const string TaskPlanningTableField = "planning_id,app_id,session_id,member_id,node_id,processes_id,original_task,planning_status,current_step_index,total_steps,strategy,confidence,estimated_duration,actual_duration,revision_count,create_time,last_update_time";
        /// 添加用表字段
        private const string TaskPlanningTableFieldForAdd = "planning_id,app_id,session_id,member_id,node_id,processes_id,original_task,planning_status,current_step_index,total_steps,strategy,confidence,estimated_duration,actual_duration,revision_count,create_time,last_update_time";
        /// 添加用表字段value
        private const string TaskPlanningTableFieldAltForAdd = "@planning_id,@app_id,@session_id,@member_id,@node_id,@processes_id,@original_task,@planning_status,@current_step_index,@total_steps,@strategy,@confidence,@estimated_duration,@actual_duration,@revision_count,@create_time,@last_update_time";

        public string SetConnectionName(string connName)
        {
            return TaskPlanningConnectionName = connName;
        }

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public string TaskPlanning_Add(TaskPlanningInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(TaskPlanningTableName);
            strSql.Append(" (");
            strSql.Append(TaskPlanningTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(TaskPlanningTableFieldAltForAdd);
            strSql.Append(");");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = model.PlanningID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = model.SessionID },
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = model.MemberID },
                new NpgsqlParameter("@node_id", NpgsqlDbType.Varchar) { Value = model.NodeID },
                new NpgsqlParameter("@processes_id", NpgsqlDbType.Varchar) { Value = model.ProcessesID },
                new NpgsqlParameter("@original_task", NpgsqlDbType.Text) { Value = model.OriginalTask },
                new NpgsqlParameter("@planning_status", NpgsqlDbType.Varchar) { Value = model.PlanningStatus },
                new NpgsqlParameter("@current_step_index", NpgsqlDbType.Integer) { Value = model.CurrentStepIndex },
                new NpgsqlParameter("@total_steps", NpgsqlDbType.Integer) { Value = model.TotalSteps },
                new NpgsqlParameter("@strategy", NpgsqlDbType.Varchar) { Value = model.Strategy },
                new NpgsqlParameter("@confidence", NpgsqlDbType.Integer) { Value = model.Confidence },
                new NpgsqlParameter("@estimated_duration", NpgsqlDbType.Integer) { Value = model.EstimatedDuration },
                new NpgsqlParameter("@actual_duration", NpgsqlDbType.Integer) { Value = model.ActualDuration },
                new NpgsqlParameter("@revision_count", NpgsqlDbType.Integer) { Value = model.RevisionCount },
                new NpgsqlParameter("@create_time", NpgsqlDbType.Timestamp) { Value = model.CreateTime },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = model.LastUpdateTime }
            };

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                return model.PlanningID;
            }
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool TaskPlanning_Update(TaskPlanningInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(TaskPlanningTableName);
            strSql.Append(" set ");
            strSql.Append("app_id=@app_id,");
            strSql.Append("session_id=@session_id,");
            strSql.Append("member_id=@member_id,");
            strSql.Append("node_id=@node_id,");
            strSql.Append("processes_id=@processes_id,");
            strSql.Append("original_task=@original_task,");
            strSql.Append("planning_status=@planning_status,");
            strSql.Append("current_step_index=@current_step_index,");
            strSql.Append("total_steps=@total_steps,");
            strSql.Append("strategy=@strategy,");
            strSql.Append("confidence=@confidence,");
            strSql.Append("estimated_duration=@estimated_duration,");
            strSql.Append("actual_duration=@actual_duration,");
            strSql.Append("revision_count=@revision_count,");
            strSql.Append("last_update_time=@last_update_time ");
            strSql.Append(" where planning_id=@planning_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = model.PlanningID },
                new NpgsqlParameter("@app_id", NpgsqlDbType.Varchar) { Value = model.AppID },
                new NpgsqlParameter("@session_id", NpgsqlDbType.Varchar) { Value = model.SessionID },
                new NpgsqlParameter("@member_id", NpgsqlDbType.Varchar) { Value = model.MemberID },
                new NpgsqlParameter("@node_id", NpgsqlDbType.Varchar) { Value = model.NodeID },
                new NpgsqlParameter("@processes_id", NpgsqlDbType.Varchar) { Value = model.ProcessesID },
                new NpgsqlParameter("@original_task", NpgsqlDbType.Text) { Value = model.OriginalTask },
                new NpgsqlParameter("@planning_status", NpgsqlDbType.Varchar) { Value = model.PlanningStatus },
                new NpgsqlParameter("@current_step_index", NpgsqlDbType.Integer) { Value = model.CurrentStepIndex },
                new NpgsqlParameter("@total_steps", NpgsqlDbType.Integer) { Value = model.TotalSteps },
                new NpgsqlParameter("@strategy", NpgsqlDbType.Varchar) { Value = model.Strategy },
                new NpgsqlParameter("@confidence", NpgsqlDbType.Integer) { Value = model.Confidence },
                new NpgsqlParameter("@estimated_duration", NpgsqlDbType.Integer) { Value = model.EstimatedDuration },
                new NpgsqlParameter("@actual_duration", NpgsqlDbType.Integer) { Value = model.ActualDuration },
                new NpgsqlParameter("@revision_count", NpgsqlDbType.Integer) { Value = model.RevisionCount },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = model.LastUpdateTime }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool TaskPlanning_Delete(string PlanningID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(TaskPlanningTableName);
            strSql.Append(" where planning_id=@planning_id");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = PlanningID }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public bool TaskPlanning_DeleteList(string PlanningIDlist)
        {
            if (string.IsNullOrWhiteSpace(PlanningIDlist))
            {
                return false;
            }

            // 分割ID列表并去除空白
            string[] items = PlanningIDlist.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
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
            strSql.Append(TaskPlanningTableName);
            strSql.Append(" where planning_id = ANY(@planning_ids)");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@planning_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar) { Value = items }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public TaskPlanningInfo TaskPlanning_GetModel(string PlanningID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskPlanningTableField);
            strSql.Append(" from ");
            strSql.Append(TaskPlanningTableName);
            strSql.Append(" where planning_id=@planning_id");
            strSql.Append(" limit 1");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = PlanningID }
            };

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return TaskPlanning_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public TaskPlanningInfo TaskPlanning_DataRowToModel(DataRow row)
        {
            TaskPlanningInfo model = new TaskPlanningInfo();
            if (row != null)
            {
                if (row["planning_id"] != null)
                {
                    model.PlanningID = row["planning_id"].ToString();
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
                if (row["node_id"] != null)
                {
                    model.NodeID = row["node_id"].ToString();
                }
                if (row["processes_id"] != null)
                {
                    model.ProcessesID = row["processes_id"].ToString();
                }
                if (row["original_task"] != null)
                {
                    model.OriginalTask = row["original_task"].ToString();
                }
                if (row["planning_status"] != null)
                {
                    model.PlanningStatus = row["planning_status"].ToString();
                }
                if (row["current_step_index"] != null)
                {
                    model.CurrentStepIndex = int.Parse(row["current_step_index"].ToString());
                }
                if (row["total_steps"] != null)
                {
                    model.TotalSteps = int.Parse(row["total_steps"].ToString());
                }
                if (row["strategy"] != null)
                {
                    model.Strategy = row["strategy"].ToString();
                }
                if (row["confidence"] != null)
                {
                    model.Confidence = int.Parse(row["confidence"].ToString());
                }
                if (row["estimated_duration"] != null)
                {
                    model.EstimatedDuration = int.Parse(row["estimated_duration"].ToString());
                }
                if (row["actual_duration"] != null)
                {
                    model.ActualDuration = int.Parse(row["actual_duration"].ToString());
                }
                if (row["revision_count"] != null)
                {
                    model.RevisionCount = int.Parse(row["revision_count"].ToString());
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
        public DataSet TaskPlanning_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskPlanningTableField);
            strSql.Append(" FROM ");
            strSql.Append(TaskPlanningTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet TaskPlanning_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskPlanningTableField);
            strSql.Append(" FROM ");
            strSql.Append(TaskPlanningTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int TaskPlanning_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(TaskPlanningTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString());
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
        public DataSet TaskPlanning_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskPlanningTableField);
            strSql.Append(" FROM ");
            strSql.Append(TaskPlanningTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public DataTable TaskPlanning_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime")
        {
            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@tableName", NpgsqlDbType.Varchar) { Value = "tb_task_planning" },
                new NpgsqlParameter("@showFName", NpgsqlDbType.Varchar) { Value = showName },
                new NpgsqlParameter("@selectWhere", NpgsqlDbType.Varchar) { Value = strWhere },
                new NpgsqlParameter("@selectOrder", NpgsqlDbType.Varchar) { Value = orderKey + (orderType == 0 ? " ASC" : " DESC") },
                new NpgsqlParameter("@pageNo", NpgsqlDbType.Integer) { Value = pageIndex },
                new NpgsqlParameter("@pageSize", NpgsqlDbType.Integer) { Value = pageSize }
            };

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.StoredProcedure, "CommonPagenation", parameters);
            total = 0;
            if (ds.Tables.Count > 1)
            {
                total = Convert.ToInt32((long)ds.Tables[1].Rows[0][0]);
                if (total % pageSize == 0)
                {
                    pagetotal = total / pageSize;
                }
                else
                {
                    pagetotal = total / pageSize + 1;
                }
                return ds.Tables[0];
            }
            else
            {
                pagetotal = 0;
                return null;
            }
        }

        /// <summary>
        /// 更新规划状态
        /// </summary>
        public bool TaskPlanning_UpdateStatus(string PlanningID, string status)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(TaskPlanningTableName);
            strSql.Append(" set planning_status=@planning_status, last_update_time=@last_update_time");
            strSql.Append(" where planning_id=@planning_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = PlanningID },
                new NpgsqlParameter("@planning_status", NpgsqlDbType.Varchar) { Value = status },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = DateTime.Now }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 增加修订次数
        /// </summary>
        public bool TaskPlanning_IncrementRevisionCount(string PlanningID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(TaskPlanningTableName);
            strSql.Append(" set revision_count=revision_count+1, last_update_time=@last_update_time");
            strSql.Append(" where planning_id=@planning_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = PlanningID },
                new NpgsqlParameter("@last_update_time", NpgsqlDbType.Timestamp) { Value = DateTime.Now }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskPlanningConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }
    }
}
