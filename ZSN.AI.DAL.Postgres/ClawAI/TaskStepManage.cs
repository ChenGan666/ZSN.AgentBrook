using System;
using System.Data;
using System.Linq;
using Npgsql;
using System.Text;
using System.Collections.Generic;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using NpgsqlTypes;

namespace ZSN.AI.DAL.Postgres
{
    public partial class TaskStepManage : ITaskStepManage
    {
        /// 表链接
        private string TaskStepConnectionName = "KnowledgeBaseDb";
        /// 表名称
        private string TaskStepTableName = "tb_task_step";
        /// 表字段
        private const string TaskStepTableField = "step_id,planning_id,step_index,step_description,step_type,assigned_workflow_ids,step_status,depends_on_step_ids,step_inputs,expected_output,actual_output,execution_result,quality_score,retry_count,error_message,start_time,end_time,create_time";
        /// 添加用表字段
        private const string TaskStepTableFieldForAdd = "step_id,planning_id,step_index,step_description,step_type,assigned_workflow_ids,step_status,depends_on_step_ids,step_inputs,expected_output,actual_output,execution_result,quality_score,retry_count,error_message,start_time,end_time,create_time";
        /// 添加用表字段value
        private const string TaskStepTableFieldAltForAdd = "@step_id,@planning_id,@step_index,@step_description,@step_type,@assigned_workflow_ids,@step_status,@depends_on_step_ids,@step_inputs,@expected_output,@actual_output,@execution_result,@quality_score,@retry_count,@error_message,@start_time,@end_time,@create_time";

        public string SetConnectionName(string connName)
        {
            return TaskStepConnectionName = connName;
        }

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public string TaskStep_Add(TaskStepInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(TaskStepTableName);
            strSql.Append(" (");
            strSql.Append(TaskStepTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(TaskStepTableFieldAltForAdd);
            strSql.Append(");");

            NpgsqlParameter[] parameters = GetTaskStepParameters(model);

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                return model.StepID;
            }
        }

        /// <summary>
        /// 批量增加数据
        /// </summary>
        public int TaskStep_AddBatch(List<TaskStepInfo> models)
        {
            if (models == null || models.Count == 0)
                return 0;

            StringBuilder strSql = new StringBuilder();
            foreach (var model in models)
            {
                strSql.Append("insert into ");
                strSql.Append(TaskStepTableName);
                strSql.Append(" (");
                strSql.Append(TaskStepTableFieldForAdd);
                strSql.Append(") values (");
                strSql.Append(TaskStepTableFieldAltForAdd);
                strSql.Append(");");
            }

            // 使用第一个model的参数作为模板,实际应该为每个model创建参数
            // 这里简化处理,实际使用时建议逐条插入或使用事务
            int totalRows = 0;
            foreach (var model in models)
            {
                NpgsqlParameter[] parameters = GetTaskStepParameters(model);
                int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text,
                    "insert into " + TaskStepTableName + " (" + TaskStepTableFieldForAdd + ") values (" + TaskStepTableFieldAltForAdd + ");",
                    parameters);
                totalRows += rows;
            }
            return totalRows;
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool TaskStep_Update(TaskStepInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(TaskStepTableName);
            strSql.Append(" set ");
            strSql.Append("planning_id=@planning_id,");
            strSql.Append("step_index=@step_index,");
            strSql.Append("step_description=@step_description,");
            strSql.Append("step_type=@step_type,");
            strSql.Append("assigned_workflow_ids=@assigned_workflow_ids,");
            strSql.Append("step_status=@step_status,");
            strSql.Append("depends_on_step_ids=@depends_on_step_ids,");
            strSql.Append("step_inputs=@step_inputs,");
            strSql.Append("expected_output=@expected_output,");
            strSql.Append("actual_output=@actual_output,");
            strSql.Append("execution_result=@execution_result,");
            strSql.Append("quality_score=@quality_score,");
            strSql.Append("retry_count=@retry_count,");
            strSql.Append("error_message=@error_message,");
            strSql.Append("start_time=@start_time,");
            strSql.Append("end_time=@end_time ");
            strSql.Append(" where step_id=@step_id;");

            NpgsqlParameter[] parameters = GetTaskStepParameters(model);

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool TaskStep_Delete(string StepID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(TaskStepTableName);
            strSql.Append(" where step_id=@step_id");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@step_id", NpgsqlDbType.Varchar) { Value = StepID }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public bool TaskStep_DeleteList(string StepIDlist)
        {
            if (string.IsNullOrWhiteSpace(StepIDlist))
            {
                return false;
            }

            // 分割ID列表并去除空白
            string[] items = StepIDlist.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
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
            strSql.Append(TaskStepTableName);
            strSql.Append(" where step_id = ANY(@step_ids)");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@step_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar) { Value = items }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 根据规划ID删除所有步骤
        /// </summary>
        public bool TaskStep_DeleteByPlanningID(string PlanningID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(TaskStepTableName);
            strSql.Append(" where planning_id=@planning_id");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = PlanningID }
            };

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public TaskStepInfo TaskStep_GetModel(string StepID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskStepTableField);
            strSql.Append(" from ");
            strSql.Append(TaskStepTableName);
            strSql.Append(" where step_id=@step_id");
            strSql.Append(" limit 1");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@step_id", NpgsqlDbType.Varchar) { Value = StepID }
            };

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return TaskStep_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public TaskStepInfo TaskStep_DataRowToModel(DataRow row)
        {
            TaskStepInfo model = new TaskStepInfo();
            if (row != null)
            {
                if (row["step_id"] != null)
                {
                    model.StepID = row["step_id"].ToString();
                }
                if (row["planning_id"] != null)
                {
                    model.PlanningID = row["planning_id"].ToString();
                }
                if (row["step_index"] != null)
                {
                    model.StepIndex = int.Parse(row["step_index"].ToString());
                }
                if (row["step_description"] != null)
                {
                    model.StepDescription = row["step_description"].ToString();
                }
                if (row["step_type"] != null)
                {
                    model.StepType = row["step_type"].ToString();
                }
                if (row["assigned_workflow_ids"] != null)
                {
                    model.AssignedWorkflowIds = row["assigned_workflow_ids"].ToString();
                }
                if (row["step_status"] != null)
                {
                    model.StepStatus = row["step_status"].ToString();
                }
                if (row["depends_on_step_ids"] != null)
                {
                    model.DependsOnStepIds = row["depends_on_step_ids"].ToString();
                }
                if (row["step_inputs"] != null)
                {
                    model.StepInputs = row["step_inputs"].ToString();
                }
                if (row["expected_output"] != null)
                {
                    model.ExpectedOutput = row["expected_output"].ToString();
                }
                if (row["actual_output"] != null)
                {
                    model.ActualOutput = row["actual_output"].ToString();
                }
                if (row["execution_result"] != null)
                {
                    model.ExecutionResult = row["execution_result"].ToString();
                }
                if (row["quality_score"] != null)
                {
                    model.QualityScore = int.Parse(row["quality_score"].ToString());
                }
                if (row["retry_count"] != null)
                {
                    model.RetryCount = int.Parse(row["retry_count"].ToString());
                }
                if (row["error_message"] != null)
                {
                    model.ErrorMessage = row["error_message"].ToString();
                }
                if (row["start_time"] != null && row["start_time"] != DBNull.Value)
                {
                    model.StartTime = DateTime.Parse(row["start_time"].ToString());
                }
                if (row["end_time"] != null && row["end_time"] != DBNull.Value)
                {
                    model.EndTime = DateTime.Parse(row["end_time"].ToString());
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
        public DataSet TaskStep_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskStepTableField);
            strSql.Append(" FROM ");
            strSql.Append(TaskStepTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet TaskStep_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskStepTableField);
            strSql.Append(" FROM ");
            strSql.Append(TaskStepTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString());
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int TaskStep_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(TaskStepTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString());
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
        /// 更新步骤状态
        /// </summary>
        public bool TaskStep_UpdateStatus(string StepID, string status)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(TaskStepTableName);
            strSql.Append(" set step_status=@step_status");
            strSql.Append(" where step_id=@step_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@step_id", NpgsqlDbType.Varchar) { Value = StepID },
                new NpgsqlParameter("@step_status", NpgsqlDbType.Varchar) { Value = status }
            };
            parameters[0].Value = StepID;
            parameters[1].Value = status;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 更新步骤执行结果
        /// </summary>
        public bool TaskStep_UpdateExecutionResult(string StepID, string actualOutput, string executionResult, int qualityScore)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(TaskStepTableName);
            strSql.Append(" set actual_output=@actual_output, execution_result=@execution_result, quality_score=@quality_score, end_time=@end_time");
            strSql.Append(" where step_id=@step_id;");

            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@step_id", NpgsqlDbType.Varchar) { Value = StepID },
                new NpgsqlParameter("@actual_output", NpgsqlDbType.Text) { Value = actualOutput },
                new NpgsqlParameter("@execution_result", NpgsqlDbType.Text) { Value = executionResult },
                new NpgsqlParameter("@quality_score", NpgsqlDbType.Integer) { Value = qualityScore },
                new NpgsqlParameter("@end_time", NpgsqlDbType.Timestamp) { Value = DateTime.Now }
            };
            parameters[0].Value = StepID;
            parameters[1].Value = actualOutput;
            parameters[2].Value = executionResult;
            parameters[3].Value = qualityScore;
            parameters[4].Value = DateTime.Now;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskStepConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }

        /// <summary>
        /// 获取参数数组
        /// </summary>
        private NpgsqlParameter[] GetTaskStepParameters(TaskStepInfo model)
        {
            NpgsqlParameter[] parameters = {
                new NpgsqlParameter("@step_id", NpgsqlDbType.Varchar) { Value = model.StepID },
                new NpgsqlParameter("@planning_id", NpgsqlDbType.Varchar) { Value = model.PlanningID },
                new NpgsqlParameter("@step_index", NpgsqlDbType.Integer) { Value = model.StepIndex },
                new NpgsqlParameter("@step_description", NpgsqlDbType.Text) { Value = model.StepDescription },
                new NpgsqlParameter("@step_type", NpgsqlDbType.Varchar) { Value = model.StepType },
                new NpgsqlParameter("@assigned_workflow_ids", NpgsqlDbType.Text) { Value = model.AssignedWorkflowIds },
                new NpgsqlParameter("@step_status", NpgsqlDbType.Varchar) { Value = model.StepStatus },
                new NpgsqlParameter("@depends_on_step_ids", NpgsqlDbType.Text) { Value = model.DependsOnStepIds },
                new NpgsqlParameter("@step_inputs", NpgsqlDbType.Text) { Value = model.StepInputs ?? (object)DBNull.Value },
                new NpgsqlParameter("@expected_output", NpgsqlDbType.Text) { Value = model.ExpectedOutput },
                new NpgsqlParameter("@actual_output", NpgsqlDbType.Text) { Value = model.ActualOutput },
                new NpgsqlParameter("@execution_result", NpgsqlDbType.Text) { Value = model.ExecutionResult },
                new NpgsqlParameter("@quality_score", NpgsqlDbType.Integer) { Value = model.QualityScore },
                new NpgsqlParameter("@retry_count", NpgsqlDbType.Integer) { Value = model.RetryCount },
                new NpgsqlParameter("@error_message", NpgsqlDbType.Text) { Value = model.ErrorMessage },
                new NpgsqlParameter("@start_time", NpgsqlDbType.Timestamp) { Value = model.StartTime.HasValue ? (object)model.StartTime.Value : DBNull.Value },
                new NpgsqlParameter("@end_time", NpgsqlDbType.Timestamp) { Value = model.EndTime.HasValue ? (object)model.EndTime.Value : DBNull.Value },
                new NpgsqlParameter("@create_time", NpgsqlDbType.Timestamp) { Value = model.CreateTime }
            };

            return parameters;
        }
    }
}
