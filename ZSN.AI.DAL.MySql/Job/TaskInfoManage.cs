using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using Newtonsoft.Json;
using System.Threading.Tasks;
using NPOI.SS.Formula.Functions;
namespace ZSN.AI.DAL.MySql
{
    public partial class TaskInfoManage : ITaskInfoManage
    {

        private string TaskInfoConnectionName = "JobDb";

        private const string TaskInfoTableName = "tb_task_info";

        private const string TaskInfoTableField = "TaskID,TaskType,TaskConfig,CreateTime,UpdateTime,State,Results,LoopType,IntervalValue,RepeatValue,RedoCount,FromTaskID,FromMainTaskID,WorkflowID,SessionID,ProcessesID ";
        private const string TaskInfoTableFieldForAdd = "TaskID,TaskType,TaskConfig,CreateTime,UpdateTime,State,Results,LoopType,IntervalValue,RepeatValue,RedoCount,FromTaskID,FromMainTaskID,WorkflowID,SessionID,ProcessesID";
        private const string TaskInfoTableFieldAltForAdd = "@TaskID,@TaskType,@TaskConfig,@CreateTime,@UpdateTime,@State,@Results,@LoopType,@IntervalValue,@RepeatValue,@RedoCount,@FromTaskID,@FromMainTaskID,@WorkflowID,@SessionID,@ProcessesID";
        public string SetConnectionName(string connName)
        {
            return TaskInfoConnectionName = connName;
        }

        public string TaskInfo_Add(TaskInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(TaskInfoTableName);
            strSql.Append(" (");
            strSql.Append(TaskInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(TaskInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
             new MySqlParameter("@TaskID", MySqlDbType.VarChar,64),
 new MySqlParameter("@TaskType", MySqlDbType.Int32),
 new MySqlParameter("@TaskConfig", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@State", MySqlDbType.Int32),
 new MySqlParameter("@Results", MySqlDbType.JSON),

 new MySqlParameter("@LoopType", MySqlDbType.Int32),
 new MySqlParameter("@IntervalValue", MySqlDbType.JSON),
 new MySqlParameter("@RepeatValue", MySqlDbType.Int32),
 new MySqlParameter("@RedoCount", MySqlDbType.Int32),
             new MySqlParameter("@FromTaskID", MySqlDbType.VarChar,64),
             new MySqlParameter("@FromMainTaskID", MySqlDbType.VarChar,64),
             new MySqlParameter("@WorkflowID", MySqlDbType.VarChar,64),
             new MySqlParameter("@SessionID", MySqlDbType.VarChar,64),
             new MySqlParameter("@ProcessesID", MySqlDbType.VarChar,128),

                    };
            parameters[0].Value = model.TaskID;
            parameters[1].Value = (int)model.TaskType;
            parameters[2].Value = JsonConvert.SerializeObject(model.TaskConfig);
            parameters[3].Value = model.CreateTime;
            parameters[4].Value = model.UpdateTime;
            parameters[5].Value = model.State;
            parameters[6].Value = JsonConvert.SerializeObject(model.Results);


            parameters[7].Value = model.LoopType;
            parameters[8].Value = JsonConvert.SerializeObject(model.IntervalValue);
            parameters[9].Value = model.RepeatValue;
            parameters[10].Value = model.RedoCount;
            parameters[11].Value = model.FromTaskID;
            parameters[12].Value = model.FromMainTaskID;
            parameters[13].Value = model.WorkflowID;
            parameters[14].Value = model.SessionID;
            parameters[15].Value = model.ProcessesID;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                return model.TaskID;
            }
        }

        public bool TaskInfo_Update(TaskInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(TaskInfoTableName);
            strSql.Append(" set ");
			strSql.Append("TaskType=@TaskType,");
strSql.Append("TaskConfig=@TaskConfig,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("UpdateTime=@UpdateTime,");
strSql.Append("State=@State,");
strSql.Append("Results=@Results,");
            strSql.Append("LoopType=@LoopType,");
            strSql.Append("IntervalValue=@IntervalValue,");
            strSql.Append("RepeatValue=@RepeatValue,");
            strSql.Append("RedoCount=@RedoCount, ");
            strSql.Append("FromTaskID=@FromTaskID, ");
            strSql.Append("FromMainTaskID=@FromMainTaskID, ");
            strSql.Append("WorkflowID=@WorkflowID, ");
            strSql.Append("SessionID=@SessionID, ");
            strSql.Append("ProcessesID=@ProcessesID ");

            strSql.Append(" where TaskID=@TaskID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@TaskID", MySqlDbType.VarChar,64),
 new MySqlParameter("@TaskType", MySqlDbType.Int32,10),
 new MySqlParameter("@TaskConfig", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@State", MySqlDbType.Int32,10),
 new MySqlParameter("@Results", MySqlDbType.JSON),

 new MySqlParameter("@LoopType", MySqlDbType.Int32),
 new MySqlParameter("@IntervalValue", MySqlDbType.JSON),
 new MySqlParameter("@RepeatValue", MySqlDbType.Int32),
 new MySqlParameter("@RedoCount", MySqlDbType.Int32),
                 new MySqlParameter("@FromTaskID", MySqlDbType.VarChar,64),
                 new MySqlParameter("@FromMainTaskID", MySqlDbType.VarChar,64),
                 new MySqlParameter("@WorkflowID", MySqlDbType.VarChar,64),
             new MySqlParameter("@SessionID", MySqlDbType.VarChar,64),
             new MySqlParameter("@ProcessesID", MySqlDbType.VarChar,128),

            };
			 parameters[0].Value = model.TaskID;
 parameters[1].Value = model.TaskType;
 parameters[2].Value = JsonConvert.SerializeObject(model.TaskConfig);
 parameters[3].Value = model.CreateTime;
 parameters[4].Value = model.UpdateTime;
 parameters[5].Value = model.State;
 parameters[6].Value = JsonConvert.SerializeObject(model.Results);

            parameters[7].Value = model.LoopType;
            parameters[8].Value = JsonConvert.SerializeObject(model.IntervalValue);
            parameters[9].Value = model.RepeatValue;
            parameters[10].Value = model.RedoCount;
            parameters[11].Value = model.FromTaskID;
            parameters[12].Value = model.FromMainTaskID;
            parameters[13].Value = model.WorkflowID;
            parameters[14].Value = model.SessionID;
            parameters[15].Value = model.ProcessesID;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TaskInfo_Update(string taskID, TaskState state, Results results)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(TaskInfoTableName);
            strSql.Append(" set ");

            strSql.Append("UpdateTime=@UpdateTime,");
            strSql.Append("State=@State,");
            strSql.Append("Results=@Results");

            strSql.Append(" where TaskID=@TaskID");
            MySqlParameter[] parameters = {
                 new MySqlParameter("@TaskID", MySqlDbType.VarChar,64),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@State", MySqlDbType.Int32),
 new MySqlParameter("@Results", MySqlDbType.JSON),
            };
            parameters[0].Value = taskID;
            parameters[1].Value = DateTime.Now;
            parameters[2].Value = state;
            parameters[3].Value = JsonConvert.SerializeObject(results);

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TaskInfo_DeleteBySessionID(string SessionID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(TaskInfoTableName);
            strSql.Append(" where SessionID=@SessionID");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@SessionID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = SessionID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TaskInfo_Delete(string taskID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(TaskInfoTableName);
            strSql.Append(" where TaskID=@TaskID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@TaskID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = taskID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool TaskInfo_DeleteByWhere(string where)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(TaskInfoTableName);
            strSql.Append(" where "+where);
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text, strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TaskInfo_DeleteList(string taskIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(TaskInfoTableName);
            strSql.Append(" where TaskID in (" + taskIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text,strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public TaskInfo TaskInfo_GetModel(string taskID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskInfoTableField);
            strSql.Append(" from ");
            strSql.Append(TaskInfoTableName);
            strSql.Append(" where TaskID=@TaskID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@TaskID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = taskID;
            TaskInfo model = new TaskInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return TaskInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public TaskInfo TaskInfo_GetModelByFromTaskID(string FromTaskID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskInfoTableField);
            strSql.Append(" from ");
            strSql.Append(TaskInfoTableName);
            strSql.Append(" where FromTaskID=@FromTaskID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@FromTaskID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = FromTaskID;
            TaskInfo model = new TaskInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return TaskInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public TaskInfo TaskInfo_DataRowToModel(DataRow row)
        {
            TaskInfo model = new TaskInfo();
            if (row != null)
            {
				if (row["TaskID"] != null )
                {
					model.TaskID = row["TaskID"].ToString();
                }
				if (row["TaskType"] != null )
                {
                        model.TaskType = (NodeType)int.Parse(row["TaskType"].ToString());
                }
				if (row["TaskConfig"] != null )
                {
					model.TaskConfig = JsonConvert.DeserializeObject<TaskConfig>(row["TaskConfig"].ToString());
                }
                if (row["LoopType"] != null)
                {
                    model.LoopType = (LoopType)int.Parse(row["LoopType"].ToString());
                }
                if (row["IntervalValue"] != null)
                {
                    model.IntervalValue = JsonConvert.DeserializeObject<IntervalValue>(row["IntervalValue"].ToString());
                }
                if (row["RepeatValue"] != null)
                {
                    model.RepeatValue = int.Parse(row["RepeatValue"].ToString());
                }
                if (row["RedoCount"] != null)
                {
                    model.RedoCount = int.Parse(row["RedoCount"].ToString());
                }
                if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["UpdateTime"] != null )
                {
					model.UpdateTime = DateTime.Parse(row["UpdateTime"].ToString());
                }
				if (row["State"] != null )
                {
                        model.State = (TaskState)int.Parse(row["State"].ToString());
                }
				if (row["Results"] != null )
                {
					model.Results = JsonConvert.DeserializeObject<Results>(row["Results"].ToString());
                }
                if (row["FromTaskID"] != null)
                {
                    model.FromTaskID = row["FromTaskID"].ToString();
                }
                if (row["FromMainTaskID"] != null)
                {
                    model.FromMainTaskID = row["FromMainTaskID"].ToString();
                }
                if (row["WorkflowID"] != null)
                {
                    model.WorkflowID = row["WorkflowID"].ToString();
                }
                if (row["SessionID"] != null)
                {
                    model.SessionID = row["SessionID"].ToString();
                }
                if (row["ProcessesID"] != null)
                {
                    model.ProcessesID = row["ProcessesID"].ToString();
                }
            }
            return model;
        }
        public DataSet TaskInfo_GetList(NodeType nodeType, string WorkflowID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(TaskInfoTableName);

            string _where = " where TaskType=@TaskType and WorkflowID=@WorkflowID";

            strSql.Append(_where + " ;");

            MySqlParameter[] parameters = {
                    new MySqlParameter("@TaskType",MySqlDbType.Int32),
                    new MySqlParameter("@WorkflowID",MySqlDbType.VarChar),
            };
            parameters[0].Value = nodeType;
            parameters[1].Value = WorkflowID;

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        public DataSet TaskInfo_GetList(int State, int taskType,DateTime StartTime, int ToState, int length)
        {
            DbInfo dbInfo = DbConfig.GetDbInfo(TaskInfoConnectionName);
            string _where = " where State=@State and TaskType=@TaskType and CreateTime<=@StartTime order by CreateTime ASC limit @length";

            MySqlParameter[] parameters = {
                    new MySqlParameter("@State", MySqlDbType.Int32),
                    new MySqlParameter("@TaskType",MySqlDbType.Int32),
                    new MySqlParameter("@ToState",MySqlDbType.Int32),
                    new MySqlParameter("@StartTime",MySqlDbType.DateTime,16),
                    new MySqlParameter("@length",MySqlDbType.Int32),
            };
            parameters[0].Value = State;
            parameters[1].Value = taskType;
            parameters[2].Value = ToState;
            parameters[3].Value = StartTime;
            parameters[4].Value = length;

            string selectSql = "select " + TaskInfoTableField + " FROM " + TaskInfoTableName + _where + " FOR UPDATE SKIP LOCKED ;";
            string updateSql = " update " + TaskInfoTableName + " set State=@ToState " + _where + " ;";

            return ExecuteWithRetry(dbInfo, selectSql, updateSql, parameters);
        }
        public DataSet TaskInfo_GetList(int State, string taskTypeStr, DateTime StartTime, int ToState, int length)
        {
            DbInfo dbInfo = DbConfig.GetDbInfo(TaskInfoConnectionName);
            string _where = $" where State=@State and TaskType in({taskTypeStr}) and CreateTime<=@StartTime order by CreateTime ASC limit @length";

            MySqlParameter[] parameters = {
                    new MySqlParameter("@State", MySqlDbType.Int32),
                    new MySqlParameter("@ToState",MySqlDbType.Int32),
                    new MySqlParameter("@StartTime",MySqlDbType.DateTime,16),
                    new MySqlParameter("@length",MySqlDbType.Int32),
            };
            parameters[0].Value = State;
            parameters[1].Value = ToState;
            parameters[2].Value = StartTime;
            parameters[3].Value = length;

            string selectSql = "select " + TaskInfoTableField + " FROM " + TaskInfoTableName + _where + " FOR UPDATE SKIP LOCKED ;";
            string updateSql = " update " + TaskInfoTableName + " set State=@ToState " + _where + " ;";

            return ExecuteWithRetry(dbInfo, selectSql, updateSql, parameters);
        }

        /// <summary>
        /// 带重试机制的事务执行，用于处理 MySQL 死锁等临时性错误
        /// </summary>
        private DataSet ExecuteWithRetry(DbInfo dbInfo, string selectSql, string updateSql, MySqlParameter[] parameters, int maxRetries = 3)
        {
            for (int attempt = 0; ; attempt++)
            {
                using (var conn = DbHelper.GetFactory(dbInfo).CreateConnection())
                {
                    conn.ConnectionString = dbInfo.ConnectionString;
                    lock (DbHelper.ConnectionOpenLock)
                    {
                        conn.Open();
                    }
                    using (var tran = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            DataSet ds = DbHelper.ExecuteDataset(dbInfo, tran, CommandType.Text, selectSql + updateSql, parameters);
                            tran.Commit();
                            return ds;
                        }
                        catch (MySqlException ex) when (ex.Number == 1213 && attempt < maxRetries)
                        {
                            // 1213 = Deadlock found when trying to get lock
                            tran.Rollback();
                            Console.WriteLine($"[TaskInfoManage] 死锁检测，第 {attempt + 1} 次重试...");
                            System.Threading.Thread.Sleep(100 * (attempt + 1));
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
        }

        public bool TaskInfo_SetState(List<string> TaskID, TaskState ToState)
        {
            bool re = true;
            if (TaskID.Count > 0)
            {
                StringBuilder strSql = new StringBuilder();
                strSql.Append($" update {TaskInfoTableName} set State={ToState}  where TaskID in({String.Join(",", TaskID.Select(n => $"'{n}'"))});");

                int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text, strSql.ToString());
                re = rows > 0;
            }

            return re;
        }


        public DataSet TaskInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(TaskInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskInfoConnectionName), CommandType.Text,strSql.ToString());
        }

        public DataSet TaskInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(TaskInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        public int TaskInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(TaskInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(TaskInfoConnectionName),CommandType.Text,strSql.ToString());
            if (obj == null)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(obj);
            }
        }
        public DataSet TaskInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(TaskInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(TaskInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskInfoConnectionName),CommandType.Text,strSql.ToString());
        }

        public DataTable TaskInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "TaskID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_task_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(TaskInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
	}
}
