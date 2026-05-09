using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Text;
using ZSN.AI.DAL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class WorkflowInfoManage : IWorkflowInfoManage
    {
        ///表链接
        private string WorkflowInfoConnectionName = "WorkflowDb";
        ///表名称
        private string WorkflowInfoTableName = "tb_workflow_info";
        ///表字段
        private const string WorkflowInfoTableField = "WorkflowID,MainType,MainID,SystemStatus,CreateTime,LastUpdateTime,WorkflowName,Description,Config";
        ///添加用表字段
        private const string WorkflowInfoTableFieldForAdd = "WorkflowID,MainType,MainID,SystemStatus,CreateTime,LastUpdateTime,WorkflowName,Description,Config";
        ///添加用表字段value
        private const string WorkflowInfoTableFieldAltForAdd = "@WorkflowID,@MainType,@MainID,@SystemStatus,@CreateTime,@LastUpdateTime,@WorkflowName,@Description,@Config";
        public string SetConnectionName(string connName)
        {
            return WorkflowInfoConnectionName = connName;
        }
        
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public string WorkflowInfo_Add(WorkflowInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(WorkflowInfoTableName);
			strSql.Append(" (");
            strSql.Append(WorkflowInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(WorkflowInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@WorkflowID", MySqlDbType.VarChar,64),
 new MySqlParameter("@MainType", MySqlDbType.Int32,10),
 new MySqlParameter("@MainID", MySqlDbType.VarChar,64),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime),
 new MySqlParameter("@WorkflowName", MySqlDbType.VarChar,128),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 new MySqlParameter("@Config", MySqlDbType.JSON),

                    };
			 parameters[0].Value = model.WorkflowID;
 parameters[1].Value = model.MainType;
 parameters[2].Value = model.MainID;
 parameters[3].Value = model.SystemStatus;
 parameters[4].Value = model.CreateTime;
 parameters[5].Value = model.LastUpdateTime;
            parameters[6].Value = model.WorkflowName;
            parameters[7].Value = model.Description;
            parameters[8].Value = model.Config;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(WorkflowInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.WorkflowID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool WorkflowInfo_Update(WorkflowInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(WorkflowInfoTableName);
            strSql.Append(" set ");
			strSql.Append("MainType=@MainType,");
strSql.Append("MainID=@MainID,");
strSql.Append("SystemStatus=@SystemStatus,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("LastUpdateTime=@LastUpdateTime,");
            strSql.Append("WorkflowName=@WorkflowName,");
            strSql.Append("Description=@Description,");
            strSql.Append("Config=@Config");

            strSql.Append(" where WorkflowID=@WorkflowID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@WorkflowID", MySqlDbType.VarChar,64),
 new MySqlParameter("@MainType", MySqlDbType.Int32,10),
 new MySqlParameter("@MainID", MySqlDbType.VarChar,64),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime),
 new MySqlParameter("@WorkflowName", MySqlDbType.VarChar,128),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 new MySqlParameter("@Config", MySqlDbType.JSON),

            };
			 parameters[0].Value = model.WorkflowID;
 parameters[1].Value = model.MainType;
 parameters[2].Value = model.MainID;
 parameters[3].Value = model.SystemStatus;
 parameters[4].Value = model.CreateTime;
 parameters[5].Value = model.LastUpdateTime;
            parameters[6].Value = model.WorkflowName;
            parameters[7].Value = model.Description;
            parameters[8].Value = model.Config;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool WorkflowInfo_Delete(string workflowID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowInfoTableName);
            strSql.Append(" where WorkflowID=@WorkflowID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@WorkflowID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = workflowID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool WorkflowInfo_DeleteList(string workflowIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowInfoTableName);
            strSql.Append(" where WorkflowID in (" + workflowIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public WorkflowInfo WorkflowInfo_GetModel(string workflowID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowInfoTableField);
            strSql.Append(" from ");
            strSql.Append(WorkflowInfoTableName);
            strSql.Append(" where WorkflowID=@WorkflowID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@WorkflowID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = workflowID;
            WorkflowInfo model = new WorkflowInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return WorkflowInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public WorkflowInfo WorkflowInfo_GetModelByMainID(string MainID,int MainType)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowInfoTableField);
            strSql.Append(" from ");
            strSql.Append(WorkflowInfoTableName);
            strSql.Append(" where MainID=@MainID and MainType=@MainType");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@MainID", MySqlDbType.VarChar, 64),
                    new MySqlParameter("@MainType",MySqlDbType.Int32)
            };
            parameters[0].Value = MainID;
            parameters[1].Value = MainType;
            WorkflowInfo model = new WorkflowInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return WorkflowInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public WorkflowInfo WorkflowInfo_DataRowToModel(DataRow row)
        {
            WorkflowInfo model = new WorkflowInfo();
            if (row != null)
            {
				if (row["WorkflowID"] != null )
                {
					model.WorkflowID = row["WorkflowID"].ToString();
                }
				if (row["MainType"] != null )
                {
                    model.MainType =(MainType)int.Parse(row["MainType"].ToString());
                }
				if (row["MainID"] != null )
                {
					model.MainID = row["MainID"].ToString();
                }
                if (row["WorkflowName"] != null)
                {
                    model.WorkflowName = row["WorkflowName"].ToString();
                }
                if (row["Description"] != null)
                {
                    model.Description = row["Description"].ToString();
                }
                if (row["SystemStatus"] != null )
                {
                    model.SystemStatus =(WorkflowStatus)DbConvert.ToInt32Nullable(row["SystemStatus"].ToString());
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DbConvert.ToDateTimeNullable(row["CreateTime"].ToString());
                }
				if (row["LastUpdateTime"] != null )
                {
					model.LastUpdateTime = DbConvert.ToDateTimeNullable(row["LastUpdateTime"].ToString());
                }
                if (row["Config"] != null)
                {
                    model.Config = JsonConvert.DeserializeObject<WorkFlowConfig>(row["Config"].ToString()); ;
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet WorkflowInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet WorkflowInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int WorkflowInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(WorkflowInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(WorkflowInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet WorkflowInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable WorkflowInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "WorkflowID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_workflow_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
