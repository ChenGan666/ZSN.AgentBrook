using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using Newtonsoft.Json;
namespace ZSN.AI.DAL.MySql
{
    public partial class WorkflowNodeInfoManage : IWorkflowNodeInfoManage
    {
        ///表链接
        private string WorkflowNodeInfoConnectionName = "WorkflowDb";
        ///表名称
        private string WorkflowNodeInfoTableName = "tb_workflow_node_info";
        ///表字段
        private const string WorkflowNodeInfoTableField = "NodeID,WorkflowID,NodeType,NodeName,Description,Config,CreateTime,LastUpdateTime";
        ///添加用表字段
        private const string WorkflowNodeInfoTableFieldForAdd = "NodeID,WorkflowID,NodeType,NodeName,Description,Config,CreateTime,LastUpdateTime";
        ///添加用表字段value
        private const string WorkflowNodeInfoTableFieldAltForAdd = "@NodeID,@WorkflowID,@NodeType,@NodeName,@Description,@Config,@CreateTime,@LastUpdateTime";
        public string SetConnectionName(string connName)
        {
            return WorkflowNodeInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string WorkflowNodeInfo_Add(WorkflowNodeInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(WorkflowNodeInfoTableName);
			strSql.Append(" (");
            strSql.Append(WorkflowNodeInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(WorkflowNodeInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@NodeID", MySqlDbType.VarChar,64),
 new MySqlParameter("@WorkflowID", MySqlDbType.VarChar,64),
 new MySqlParameter("@NodeType", MySqlDbType.VarChar,50),
 new MySqlParameter("@NodeName", MySqlDbType.VarChar,50),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 new MySqlParameter("@Config", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime)

					};
			 parameters[0].Value = model.NodeID;
 parameters[1].Value = model.WorkflowID;
 parameters[2].Value = model.NodeType;
 parameters[3].Value = model.NodeName;
 parameters[4].Value = model.Description;
 parameters[5].Value = model.Config;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.LastUpdateTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.NodeID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool WorkflowNodeInfo_Update(WorkflowNodeInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(WorkflowNodeInfoTableName);
            strSql.Append(" set ");
			strSql.Append("WorkflowID=@WorkflowID,");
strSql.Append("NodeType=@NodeType,");
strSql.Append("NodeName=@NodeName,");
strSql.Append("Description=@Description,");
strSql.Append("Config=@Config,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("LastUpdateTime=@LastUpdateTime");

            strSql.Append(" where NodeID=@NodeID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@NodeID", MySqlDbType.VarChar,64),
 new MySqlParameter("@WorkflowID", MySqlDbType.VarChar,64),
 new MySqlParameter("@NodeType", MySqlDbType.VarChar,50),
 new MySqlParameter("@NodeName", MySqlDbType.VarChar,50),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 new MySqlParameter("@Config", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime)

			};
			 parameters[0].Value = model.NodeID;
 parameters[1].Value = model.WorkflowID;
 parameters[2].Value = model.NodeType;
 parameters[3].Value = model.NodeName;
 parameters[4].Value = model.Description;
 parameters[5].Value = model.Config;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.LastUpdateTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool WorkflowNodeInfo_Delete(string nodeID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowNodeInfoTableName);
            strSql.Append(" where NodeID=@NodeID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@NodeID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = nodeID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool WorkflowNodeInfo_DeleteList(string nodeIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowNodeInfoTableName);
            strSql.Append(" where NodeID in (" + nodeIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName), CommandType.Text,strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool WorkflowNodeInfo_DeleteAbsentList(string nodeIDlist,string WorkflowID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowNodeInfoTableName);
            strSql.Append(" where WorkflowID='"+ WorkflowID + "' and NodeID not in (" + nodeIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName), CommandType.Text, strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool WorkflowNodeInfo_DeleteByWorkflowID(string WorkflowID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WorkflowNodeInfoTableName);
            strSql.Append(" where WorkflowID in(" + WorkflowID + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName), CommandType.Text, strSql.ToString());
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
        public WorkflowNodeInfo WorkflowNodeInfo_GetModel(string nodeID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeInfoTableField);
            strSql.Append(" from ");
            strSql.Append(WorkflowNodeInfoTableName);
            strSql.Append(" where NodeID=@NodeID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@NodeID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = nodeID;
            WorkflowNodeInfo model = new WorkflowNodeInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return WorkflowNodeInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public WorkflowNodeInfo WorkflowNodeInfo_DataRowToModel(DataRow row)
        {
            WorkflowNodeInfo model = new WorkflowNodeInfo();
            if (row != null)
            {
				if (row["NodeID"] != null )
                {
					model.NodeID = row["NodeID"].ToString();
                }
				if (row["WorkflowID"] != null )
                {
					model.WorkflowID = row["WorkflowID"].ToString();
                }
				if (row["NodeType"] != null )
                {
                    model.NodeType = (NodeType)System.Enum.Parse(typeof(NodeType), row["NodeType"].ToString());
                }
				if (row["NodeName"] != null )
                {
					model.NodeName = row["NodeName"].ToString();
                }
				if (row["Description"] != null )
                {
					model.Description = row["Description"].ToString();
                }
				if (row["Config"] != null )
                {
					model.Config = JsonConvert.DeserializeObject(row["Config"].ToString());
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
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet WorkflowNodeInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowNodeInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName), CommandType.Text,strSql.ToString());
        }

        public DataSet GetAllNextNodeListByNodeID(string NodeID)
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@p_start_node_id", MySqlDbType.VarChar, 64),
                    new MySqlParameter("@p_workflow_id", MySqlDbType.VarChar, 64),
            };
            parameters[0].Value = NodeID;
            parameters[1].Value = null;
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName), CommandType.StoredProcedure, "GetDescendantNodes", parameters);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet WorkflowNodeInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowNodeInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int WorkflowNodeInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(WorkflowNodeInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet WorkflowNodeInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WorkflowNodeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WorkflowNodeInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable WorkflowNodeInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "NodeID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_workflow_node_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WorkflowNodeInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
