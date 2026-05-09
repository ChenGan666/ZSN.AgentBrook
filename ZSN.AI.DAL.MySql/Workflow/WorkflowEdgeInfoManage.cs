using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using Senparc.CO2NET.Extensions;
using Newtonsoft.Json;
using JiebaNet.Analyser;
namespace ZSN.AI.DAL.MySql
{
    public partial class WorkflowEdgeInfoManage : IWorkflowEdgeInfoManage
    {
        ///表链接
        private string WorkflowEdgeInfoConnectionName = "WorkflowDb";
        ///表名称
        private string WorkflowEdgeInfoTableName = "tb_workflow_edge_info";
        ///表字段
        private const string WorkflowEdgeInfoTableField = "EdgeID,WorkflowID,SourceNodeId,TargetNodeId,ConditionConfig,LName,CreateTime,LastUpdateTime,Config";
        ///添加用表字段
        private const string WorkflowEdgeInfoTableFieldForAdd = "EdgeID,WorkflowID,SourceNodeId,TargetNodeId,ConditionConfig,LName,CreateTime,LastUpdateTime,Config";
        ///添加用表字段value
        private const string WorkflowEdgeInfoTableFieldAltForAdd = "@EdgeID,@WorkflowID,@SourceNodeId,@TargetNodeId,@ConditionConfig,@LName,@CreateTime,@LastUpdateTime,@Config";
        public string SetConnectionName(string connName)
        {
            return WorkflowEdgeInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string WorkflowEdgeInfo_Add(WorkflowEdgeInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(WorkflowEdgeInfoTableName);
			strSql.Append(" (");
            strSql.Append(WorkflowEdgeInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(WorkflowEdgeInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@EdgeID", MySqlDbType.VarChar,64),
 new MySqlParameter("@WorkflowID", MySqlDbType.VarChar,64),
 new MySqlParameter("@SourceNodeId", MySqlDbType.VarChar,64),
 new MySqlParameter("@TargetNodeId", MySqlDbType.VarChar,64),
 new MySqlParameter("@ConditionConfig", MySqlDbType.JSON),
 new MySqlParameter("@LName", MySqlDbType.VarChar,50),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime),
 new MySqlParameter("@Config", MySqlDbType.JSON),

                    };
			 parameters[0].Value = model.EdgeID;
 parameters[1].Value = model.WorkflowID;
 parameters[2].Value = model.SourceNodeId;
 parameters[3].Value = model.TargetNodeId;
 parameters[4].Value = model.ConditionConfig;
 parameters[5].Value = model.LName;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.LastUpdateTime;
            parameters[8].Value = model.Config;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.EdgeID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool WorkflowEdgeInfo_Update(WorkflowEdgeInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(WorkflowEdgeInfoTableName);
            strSql.Append(" set ");
			strSql.Append("WorkflowID=@WorkflowID,");
strSql.Append("SourceNodeId=@SourceNodeId,");
strSql.Append("TargetNodeId=@TargetNodeId,");
strSql.Append("ConditionConfig=@ConditionConfig,");
strSql.Append("LName=@LName,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("LastUpdateTime=@LastUpdateTime,");
            strSql.Append("Config=@Config");

            strSql.Append(" where EdgeID=@EdgeID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@EdgeID", MySqlDbType.VarChar,64),
 new MySqlParameter("@WorkflowID", MySqlDbType.VarChar,64),
 new MySqlParameter("@SourceNodeId", MySqlDbType.VarChar,64),
 new MySqlParameter("@TargetNodeId", MySqlDbType.VarChar,64),
 new MySqlParameter("@ConditionConfig", MySqlDbType.JSON),
 new MySqlParameter("@LName", MySqlDbType.VarChar,50),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime),
 new MySqlParameter("@Config", MySqlDbType.JSON),

            };
			 parameters[0].Value = model.EdgeID;
 parameters[1].Value = model.WorkflowID;
 parameters[2].Value = model.SourceNodeId;
 parameters[3].Value = model.TargetNodeId;
 parameters[4].Value = model.ConditionConfig;
 parameters[5].Value = model.LName;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.LastUpdateTime;
            parameters[8].Value = model.Config;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool WorkflowEdgeInfo_Delete(string EdgeID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowEdgeInfoTableName);
            strSql.Append(" where EdgeID=@EdgeID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@EdgeID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = EdgeID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool WorkflowEdgeInfo_DeleteList(string EdgeIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowEdgeInfoTableName);
            strSql.Append(" where EdgeID in (" + EdgeIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName), CommandType.Text,strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool WorkflowEdgeInfo_DeleteAbsentList(string EdgeIDlist, string WorkflowID)
        {
            if (EdgeIDlist.IsNullOrEmpty()) {  return false; }
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowEdgeInfoTableName);
            strSql.Append(" where WorkflowID='"+ WorkflowID + "' and EdgeID not in (" + EdgeIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName), CommandType.Text, strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool WorkflowEdgeInfo_DeleteByWorkflowID( string WorkflowID)
        {
            if (WorkflowID.IsNullOrEmpty()) { return false; }
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowEdgeInfoTableName);
            strSql.Append(" where WorkflowID in(" + WorkflowID + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName), CommandType.Text, strSql.ToString());
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
        public WorkflowEdgeInfo WorkflowEdgeInfo_GetModel(string EdgeID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowEdgeInfoTableField);
            strSql.Append(" from ");
            strSql.Append(WorkflowEdgeInfoTableName);
            strSql.Append(" where EdgeID=@EdgeID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@EdgeID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = EdgeID;
            WorkflowEdgeInfo model = new WorkflowEdgeInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return WorkflowEdgeInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public WorkflowEdgeInfo WorkflowEdgeInfo_DataRowToModel(DataRow row)
        {
            WorkflowEdgeInfo model = new WorkflowEdgeInfo();
            if (row != null)
            {
				if (row["EdgeID"] != null )
                {
					model.EdgeID = row["EdgeID"].ToString();
                }
				if (row["WorkflowID"] != null )
                {
					model.WorkflowID = row["WorkflowID"].ToString();
                }
				if (row["SourceNodeId"] != null )
                {
					model.SourceNodeId = row["SourceNodeId"].ToString();
                }
				if (row["TargetNodeId"] != null )
                {
					model.TargetNodeId = row["TargetNodeId"].ToString();
                }
				if (row["ConditionConfig"] != null )
                {
					model.ConditionConfig = JsonConvert.DeserializeObject(row["ConditionConfig"].ToString());
                }
                if (row["Config"] != null)
                {
                    model.Config = JsonConvert.DeserializeObject(row["Config"].ToString());
                }
                if (row["LName"] != null )
                {
					model.LName = row["LName"].ToString();
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DbConvert.ToDateTimeNullable(row["CreateTime"].ToString());
                }
				if (row["LastUpdateTime"] != null )
                {
					model.LastUpdateTime = DbConvert.ToDateTimeNullable(row["LastUpdateTime"].ToString());
                }
            }
            return model;
        }
        public DataSet WorkflowEdgeInfo_GetListBySourceNodeId(string NodeId)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowEdgeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowEdgeInfoTableName);

            strSql.Append(" where SourceNodeId=@NodeId ;");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@NodeId", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = NodeId;

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        public DataSet WorkflowEdgeInfo_GetListByTargetNodeId(string NodeId)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowEdgeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowEdgeInfoTableName);

            strSql.Append(" where TargetNodeId=@NodeId ;");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@NodeId", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = NodeId;

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet WorkflowEdgeInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowEdgeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowEdgeInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet WorkflowEdgeInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowEdgeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowEdgeInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int WorkflowEdgeInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(WorkflowEdgeInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet WorkflowEdgeInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowEdgeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowEdgeInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable WorkflowEdgeInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "EdgeID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_workflow_edge_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowEdgeInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
