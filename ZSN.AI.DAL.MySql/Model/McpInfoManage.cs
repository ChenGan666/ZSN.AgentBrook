using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Data;
using System.Text;
using ZSN.AI.DAL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class McpInfoManage : IMcpInfoManage
    {
        ///表链接
        private string McpInfoConnectionName = "ModelDb";
        ///表名称
        private string McpInfoTableName = "tb_mcp_info";
        ///表字段
        private const string McpInfoTableField = "MCPID,Name,Description,Tag,ICON,Config,EnvironmentVar,CreateTime,SystemStatus,RunHost,OutputConfig";
        ///添加用表字段
        private const string McpInfoTableFieldForAdd = "MCPID,Name,Description,Tag,ICON,Config,EnvironmentVar,CreateTime,SystemStatus,RunHost,OutputConfig";
        ///添加用表字段value
        private const string McpInfoTableFieldAltForAdd = "@MCPID,@Name,@Description,@Tag,@ICON,@Config,@EnvironmentVar,@CreateTime,@SystemStatus,@RunHost,@OutputConfig";
        public string SetConnectionName(string connName)
        {
            return McpInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string McpInfo_Add(McpInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(McpInfoTableName);
			strSql.Append(" (");
            strSql.Append(McpInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(McpInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@MCPID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Name", MySqlDbType.VarChar,50),
 new MySqlParameter("@Description", MySqlDbType.VarChar,1024),
 new MySqlParameter("@Tag", MySqlDbType.VarChar,1024),
 new MySqlParameter("@ICON", MySqlDbType.VarChar,64),
 new MySqlParameter("@Config", MySqlDbType.JSON),
 new MySqlParameter("@EnvironmentVar", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@RunHost", MySqlDbType.Int32,10),

 new MySqlParameter("@OutputConfig", MySqlDbType.JSON),

                    };
			 parameters[0].Value = model.MCPID;
 parameters[1].Value = model.Name;
 parameters[2].Value = model.Description;
 parameters[3].Value = model.Tag;
 parameters[4].Value = model.ICON;
 parameters[5].Value = model.Config;
 parameters[6].Value = model.EnvironmentVar;
 parameters[7].Value = model.CreateTime;
 parameters[8].Value = model.SystemStatus;
 parameters[9].Value = model.RunHost;
            parameters[10].Value = JsonConvert.SerializeObject(model.OutputConfig);

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(McpInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.MCPID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool McpInfo_Update(McpInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(McpInfoTableName);
            strSql.Append(" set ");
			strSql.Append("Name=@Name,");
strSql.Append("Description=@Description,");
strSql.Append("Tag=@Tag,");
strSql.Append("ICON=@ICON,");
strSql.Append("Config=@Config,");
strSql.Append("EnvironmentVar=@EnvironmentVar,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("SystemStatus=@SystemStatus,");
strSql.Append("RunHost=@RunHost,");
            strSql.Append("OutputConfig=@OutputConfig");

            strSql.Append(" where MCPID=@MCPID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@MCPID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Name", MySqlDbType.VarChar,50),
 new MySqlParameter("@Description", MySqlDbType.VarChar,1024),
 new MySqlParameter("@Tag", MySqlDbType.VarChar,1024),
 new MySqlParameter("@ICON", MySqlDbType.VarChar,64),
 new MySqlParameter("@Config", MySqlDbType.JSON),
 new MySqlParameter("@EnvironmentVar", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@RunHost", MySqlDbType.Int32,10),

 new MySqlParameter("@OutputConfig", MySqlDbType.JSON),

            };
			 parameters[0].Value = model.MCPID;
 parameters[1].Value = model.Name;
 parameters[2].Value = model.Description;
 parameters[3].Value = model.Tag;
 parameters[4].Value = model.ICON;
 parameters[5].Value = model.Config;
 parameters[6].Value = model.EnvironmentVar;
 parameters[7].Value = model.CreateTime;
 parameters[8].Value = model.SystemStatus;
 parameters[9].Value = model.RunHost;
            parameters[10].Value = JsonConvert.SerializeObject(model.OutputConfig);

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(McpInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool McpInfo_Delete(string mCPID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(McpInfoTableName);
            strSql.Append(" where MCPID=@MCPID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@MCPID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = mCPID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(McpInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool McpInfo_DeleteList(string mCPIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(McpInfoTableName);
            strSql.Append(" where MCPID in (" + mCPIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(McpInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public McpInfo McpInfo_GetModel(string mCPID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(McpInfoTableField);
            strSql.Append(" from ");
            strSql.Append(McpInfoTableName);
            strSql.Append(" where MCPID=@MCPID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@MCPID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = mCPID;
            McpInfo model = new McpInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(McpInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return McpInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public McpInfo McpInfo_DataRowToModel(DataRow row)
        {
            McpInfo model = new McpInfo();
            if (row != null)
            {
				if (row["MCPID"] != null )
                {
					model.MCPID = row["MCPID"].ToString();
                }
				if (row["Name"] != null )
                {
					model.Name = row["Name"].ToString();
                }
				if (row["Description"] != null )
                {
					model.Description = row["Description"].ToString();
                }
				if (row["Tag"] != null )
                {
					model.Tag = row["Tag"].ToString();
                }
				if (row["ICON"] != null )
                {
					model.ICON = row["ICON"].ToString();
                }
				if (row["Config"] != null )
                {
					model.Config = row["Config"].ToString();
                }
				if (row["EnvironmentVar"] != null )
                {
					model.EnvironmentVar = row["EnvironmentVar"].ToString();
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["SystemStatus"] != null )
                {
                    model.SystemStatus = (McpState) int.Parse(row["SystemStatus"].ToString());
                }
				if (row["RunHost"] != null )
                {
                    model.RunHost = (RunHostType)int.Parse(row["RunHost"].ToString());
                }
                if (row["OutputConfig"] != null)
                {
                    model.OutputConfig = JsonConvert.DeserializeObject<List<Output>>((row["OutputConfig"]??new List<Output>() { new Output()}).ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet McpInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(McpInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(McpInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(McpInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet McpInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(McpInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(McpInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(McpInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int McpInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(McpInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(McpInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet McpInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(McpInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(McpInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(McpInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable McpInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "MCPID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_mcp_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(McpInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
