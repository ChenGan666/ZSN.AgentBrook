using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using NPOI.HSSF.Record;
using System;
using System.Data;
using System.Text;
using ZSN.AI.DAL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class WorkflowNodeExecutionRecordInfoManage : IWorkflowNodeExecutionRecordInfoManage
    {
        ///表链接
        private string WorkflowNodeExecutionRecordInfoConnectionName = "WorkflowDb";
        ///表名称
        private string WorkflowNodeExecutionRecordInfoTableName = "tb_workflow_node_execution_record_info";
        ///表字段
        private const string WorkflowNodeExecutionRecordInfoTableField = "RecordID,SessionID,WorkflowID,NodeID,NextNodeID,StartTime,EndTime,Status,Inputs,Outputs,Logs,ProcessesID,NodeName,TaskID,FromMainTaskID";
        ///添加用表字段
        private const string WorkflowNodeExecutionRecordInfoTableFieldForAdd = "RecordID,SessionID,WorkflowID,NodeID,NextNodeID,StartTime,EndTime,Status,Inputs,Outputs,Logs,ProcessesID,NodeName,TaskID,FromMainTaskID";
        ///添加用表字段value
        private const string WorkflowNodeExecutionRecordInfoTableFieldAltForAdd = "@RecordID,@SessionID,@WorkflowID,@NodeID,@NextNodeID,@StartTime,@EndTime,@Status,@Inputs,@Outputs,@Logs,@ProcessesID,@NodeName,@TaskID,@FromMainTaskID";
        public string SetConnectionName(string connName)
        {
            return WorkflowNodeExecutionRecordInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string WorkflowNodeExecutionRecordInfo_Add(WorkflowNodeExecutionRecordInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
			strSql.Append(" (");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@RecordID", MySqlDbType.VarChar,64),
 new MySqlParameter("@SessionID", MySqlDbType.VarChar,64),
 new MySqlParameter("@WorkflowID", MySqlDbType.VarChar,64),
 new MySqlParameter("@NodeID", MySqlDbType.VarChar,64),
 new MySqlParameter("@NextNodeID", MySqlDbType.VarChar,64),
 new MySqlParameter("@StartTime", MySqlDbType.DateTime),
 new MySqlParameter("@EndTime", MySqlDbType.DateTime),
 new MySqlParameter("@Status", MySqlDbType.Int32,10),
 new MySqlParameter("@Inputs", MySqlDbType.JSON),
 new MySqlParameter("@Outputs", MySqlDbType.JSON),
 new MySqlParameter("@Logs", MySqlDbType.JSON),
 new MySqlParameter("@ProcessesID", MySqlDbType.VarChar,64),
 new MySqlParameter("@NodeName", MySqlDbType.VarChar,128),
 new MySqlParameter("@TaskID", MySqlDbType.VarChar,64),
 new MySqlParameter("@FromMainTaskID", MySqlDbType.VarChar,64),

                    };
			 parameters[0].Value = model.RecordID;
 parameters[1].Value = model.SessionID;
 parameters[2].Value = model.WorkflowID;
 parameters[3].Value = model.NodeID;
            parameters[4].Value = model.NextNodeID;
            parameters[5].Value = model.StartTime;
 parameters[6].Value = model.EndTime;
 parameters[7].Value = model.Status;
 parameters[8].Value = JsonConvert.SerializeObject(model.Inputs);
 parameters[9].Value = JsonConvert.SerializeObject(model.Outputs);
 parameters[10].Value = model.Logs;
            parameters[11].Value = model.ProcessesID;
            parameters[12].Value = model.NodeName;
            parameters[13].Value = model.TaskID;
            parameters[14].Value = model.FromMainTaskID;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.RecordID;
            }
        }
        public bool WorkflowNodeExecutionRecordInfo_Update(string RecordID, ExecutionRecordStatus Status, object Outputs, object Logs)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" set ");

            strSql.Append("EndTime=@EndTime,");
            strSql.Append("Status=@Status,");
            strSql.Append("Outputs=@Outputs,");
            strSql.Append("Logs=@Logs");

            strSql.Append(" where RecordID=@RecordID");
            MySqlParameter[] parameters = {
                 new MySqlParameter("@RecordID", MySqlDbType.VarChar,64),
 new MySqlParameter("@EndTime", MySqlDbType.DateTime),
 new MySqlParameter("@Status", MySqlDbType.Int32,10),
 new MySqlParameter("@Outputs", MySqlDbType.JSON),
 new MySqlParameter("@Logs", MySqlDbType.JSON)

            };
            parameters[0].Value = RecordID;
            parameters[1].Value = DateTime.Now;
            parameters[2].Value = Status;
            parameters[3].Value = JsonConvert.SerializeObject(Outputs);
            parameters[4].Value = JsonConvert.SerializeObject(Logs);

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 只更新 Outputs 和 Logs，不修改 Status（避免后台后处理覆盖已写入的最终状态）
        /// </summary>
        public bool WorkflowNodeExecutionRecordInfo_UpdateLogs(string RecordID, object Outputs, object Logs)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" set ");
            strSql.Append("Outputs=@Outputs,");
            strSql.Append("Logs=@Logs");
            strSql.Append(" where RecordID=@RecordID");
            MySqlParameter[] parameters = {
                new MySqlParameter("@RecordID", MySqlDbType.VarChar,64),
                new MySqlParameter("@Outputs", MySqlDbType.JSON),
                new MySqlParameter("@Logs", MySqlDbType.JSON)
            };
            parameters[0].Value = RecordID;
            parameters[1].Value = JsonConvert.SerializeObject(Outputs);
            parameters[2].Value = JsonConvert.SerializeObject(Logs);

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool WorkflowNodeExecutionRecordInfo_Update(WorkflowNodeExecutionRecordInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" set ");
			strSql.Append("SessionID=@SessionID,");
strSql.Append("WorkflowID=@WorkflowID,");
strSql.Append("NodeID=@NodeID,");
            strSql.Append("NextNodeID=@NextNodeID,");
            strSql.Append("StartTime=@StartTime,");
strSql.Append("EndTime=@EndTime,");
strSql.Append("Status=@Status,");
strSql.Append("Inputs=@Inputs,");
strSql.Append("Outputs=@Outputs,");
            strSql.Append("ProcessesID=@ProcessesID,");
            strSql.Append("NodeName=@NodeName,");
            strSql.Append("TaskID=@TaskID,");
            strSql.Append("FromMainTaskID=@FromMainTaskID,");
            strSql.Append("Logs=@Logs");

            strSql.Append(" where RecordID=@RecordID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@RecordID", MySqlDbType.VarChar,64),
 new MySqlParameter("@SessionID", MySqlDbType.VarChar,64),
 new MySqlParameter("@WorkflowID", MySqlDbType.VarChar,64),
 new MySqlParameter("@NodeID", MySqlDbType.VarChar,64),
 new MySqlParameter("@NextNodeID", MySqlDbType.VarChar,64),
 new MySqlParameter("@StartTime", MySqlDbType.DateTime),
 new MySqlParameter("@EndTime", MySqlDbType.DateTime),
 new MySqlParameter("@Status", MySqlDbType.Int32,10),
 new MySqlParameter("@Inputs", MySqlDbType.JSON),
 new MySqlParameter("@Outputs", MySqlDbType.JSON),
 new MySqlParameter("@Logs", MySqlDbType.JSON),
 new MySqlParameter("@ProcessesID", MySqlDbType.VarChar,64),
 new MySqlParameter("@NodeName", MySqlDbType.VarChar,64),
 new MySqlParameter("@TaskID", MySqlDbType.VarChar,64),
 new MySqlParameter("@FromMainTaskID", MySqlDbType.VarChar,64),

            };
			 parameters[0].Value = model.RecordID;
 parameters[1].Value = model.SessionID;
 parameters[2].Value = model.WorkflowID;
 parameters[3].Value = model.NodeID;
            parameters[4].Value = model.NextNodeID;
            parameters[5].Value = model.StartTime;
 parameters[6].Value = model.EndTime;
 parameters[7].Value = model.Status;
 parameters[8].Value = JsonConvert.SerializeObject(model.Inputs);
 parameters[9].Value = JsonConvert.SerializeObject(model.Outputs);
 parameters[10].Value = JsonConvert.SerializeObject(model.Logs);
            parameters[11].Value = model.ProcessesID;
            parameters[12].Value = model.NodeName;
            parameters[13].Value = model.TaskID;
            parameters[14].Value = model.FromMainTaskID;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool DeleteByNodeID(string SessionID, string NodeID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" where SessionID=@SessionID and NodeID=@NodeID");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@SessionID", MySqlDbType.VarChar, 64),
                    new MySqlParameter("@NodeID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = SessionID;
            parameters[1].Value = NodeID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool WorkflowNodeExecutionRecordInfo_Delete(string recordID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" where RecordID=@RecordID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@RecordID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = recordID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool WorkflowNodeExecutionRecordInfo_DeleteByWhere(string where)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" where "+ where);
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text, strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 批量删除数据
        /// </summary>
        public bool WorkflowNodeExecutionRecordInfo_DeleteList(string recordIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" where RecordID in (" + recordIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text,strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public WorkflowNodeExecutionRecordInfo WorkflowNodeExecutionRecordInfo_GetModel(string recordID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableField);
            strSql.Append(" from ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" where RecordID=@RecordID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@RecordID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = recordID;
            WorkflowNodeExecutionRecordInfo model = new WorkflowNodeExecutionRecordInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return WorkflowNodeExecutionRecordInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public WorkflowNodeExecutionRecordInfo WorkflowNodeExecutionRecordInfo_GetModelByTaskID(string testID, string sessionID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableField);
            strSql.Append(" from ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" where SessionID=@SessionID and TaskID=@TaskID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@SessionID", MySqlDbType.VarChar, 64),
                    new MySqlParameter("@TaskID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = sessionID;
            parameters[1].Value = testID;
            WorkflowNodeExecutionRecordInfo model = new WorkflowNodeExecutionRecordInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return WorkflowNodeExecutionRecordInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public WorkflowNodeExecutionRecordInfo WorkflowNodeExecutionRecordInfo_DataRowToModel(DataRow row)
        {
            WorkflowNodeExecutionRecordInfo model = new WorkflowNodeExecutionRecordInfo();
            if (row != null)
            {
				if (row["RecordID"] != null )
                {
					model.RecordID = row["RecordID"].ToString();
                }
				if (row["SessionID"] != null )
                {
					model.SessionID = row["SessionID"].ToString();
                }
                if (row["ProcessesID"] != null)
                {
                    model.ProcessesID = row["ProcessesID"].ToString();
                }
                if (row["WorkflowID"] != null )
                {
					model.WorkflowID = row["WorkflowID"].ToString();
                }
                if (row["TaskID"] != null)
                {
                    model.TaskID = row["TaskID"].ToString();
                }
                if (row["NodeID"] != null )
                {
					model.NodeID = row["NodeID"].ToString();
                }
                if (row["NodeName"] != null)
                {
                    model.NodeName = row["NodeName"].ToString();
                }
                if (row["NextNodeID"] != null)
                {
                    model.NextNodeID = row["NextNodeID"].ToString();
                }
                if (row["StartTime"] != null )
                {
					model.StartTime = DbConvert.ToDateTimeNullable(row["StartTime"].ToString());
                }
				if (row["EndTime"] != null )
                {
					model.EndTime = DbConvert.ToDateTimeNullable(row["EndTime"].ToString());
                }
				if (row["Status"] != null )
                {
                    model.Status = (ExecutionRecordStatus)int.Parse(row["Status"].ToString());
                }
				if (row["Inputs"] != null )
                {
					model.Inputs = JsonConvert.DeserializeObject(row["Inputs"].ToString());
                }
				if (row["Outputs"] != null )
                {
					model.Outputs = JsonConvert.DeserializeObject(row["Outputs"].ToString());
                }
				if (row["Logs"] != null )
                {
					model.Logs = JsonConvert.DeserializeObject(row["Logs"].ToString());
                }
				if (row["FromMainTaskID"] != null)
				{
					model.FromMainTaskID = row["FromMainTaskID"].ToString();
				}
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet WorkflowNodeExecutionRecordInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        public DataSet WorkflowNodeExecutionRecordInfo_GetListByNodeId(string SessionID, string NodeID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" where SessionID='"+SessionID+"' and NodeID in("+ NodeID + ")");

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text, strSql.ToString());
        }
        public DataSet WorkflowNodeExecutionRecordInfo_GetListByNodeId(string SessionID, string NodeID, ExecutionRecordStatus recordStatus)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" where SessionID='" + SessionID + "' and NodeID in(" + NodeID + ") and Status="+ (int)recordStatus);

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text, strSql.ToString());
        }
        public DataSet WorkflowNodeExecutionRecordInfo_GetListByNodeId(string SessionID, string NodeID, string ProcessesID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" where SessionID=@SessionID and NodeID in(" + NodeID + ") and ProcessesID=@ProcessesID");

            MySqlParameter[] parameters = {
                new MySqlParameter("@SessionID", MySqlDbType.VarChar, 50) { Value = SessionID },
                new MySqlParameter("@ProcessesID", MySqlDbType.VarChar, 50) { Value = ProcessesID }
            };
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        public DataSet WorkflowNodeExecutionRecordInfo_GetListByNodeId(string SessionID, string NodeID, ExecutionRecordStatus recordStatus, string ProcessesID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            strSql.Append(" where SessionID=@SessionID and NodeID in(" + NodeID + ") and Status=@Status and ProcessesID=@ProcessesID");

            MySqlParameter[] parameters = {
                new MySqlParameter("@SessionID", MySqlDbType.VarChar, 50) { Value = SessionID },
                new MySqlParameter("@Status", MySqlDbType.Int32) { Value = (int)recordStatus },
                new MySqlParameter("@ProcessesID", MySqlDbType.VarChar, 50) { Value = ProcessesID }
            };
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet WorkflowNodeExecutionRecordInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int WorkflowNodeExecutionRecordInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet WorkflowNodeExecutionRecordInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowNodeExecutionRecordInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        /// <param name="pageSize">每页大小</param>
        /// <param name="pageIndex">页标</param>
        /// <param name="strWhere">查询条件</param>
        /// <param name="pagetotal">总页数</param>
        /// <param name="total">总数</param>
        /// <param name="orderType">排序规则， 默认降序，1降序，0升序</param>
        /// <param name="showName">显示字段，默认全部</param>
        /// <param name="orderKey">排序key，默认主键</param>
        public DataTable WorkflowNodeExecutionRecordInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "RecordID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_workflow_node_excution_record_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeExecutionRecordInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
