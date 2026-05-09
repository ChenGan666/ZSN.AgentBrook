using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.Utils.Core.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL.MySql
{
    public partial class PluginsInfoManage : IPluginsInfoManage
    {
        ///表链接
        private string PluginsInfoConnectionName = "ModelDb";
        ///表名称
        private string PluginsInfoTableName = "tb_plugins_info";
        ///表字段
        private const string PluginsInfoTableField = "PluginsID,Name,Description,Namespace,EntryFunctionName,ReturnValueDescription,SystemStatus,CreateTime,LastUpdateTime,PluginsType,Config,ClassName";
        ///添加用表字段
        private const string PluginsInfoTableFieldForAdd = "Name,Description,Namespace,EntryFunctionName,ReturnValueDescription,SystemStatus,CreateTime,LastUpdateTime,PluginsType,Config,ClassName";
        ///添加用表字段value
        private const string PluginsInfoTableFieldAltForAdd = "@Name,@Description,@Namespace,@EntryFunctionName,@ReturnValueDescription,@SystemStatus,@CreateTime,@LastUpdateTime,@PluginsType,@Config,@ClassName";
        public string SetConnectionName(string connName)
        {
            return PluginsInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public Int32 PluginsInfo_Add(PluginsInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(PluginsInfoTableName);
			strSql.Append(" (");
            strSql.Append(PluginsInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(PluginsInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@Name", MySqlDbType.VarChar,50),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 new MySqlParameter("@Namespace", MySqlDbType.VarChar,255),
 new MySqlParameter("@EntryFunctionName", MySqlDbType.VarChar,128),
 new MySqlParameter("@ReturnValueDescription", MySqlDbType.VarChar,2048),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime,16),
  new MySqlParameter("@PluginsType", MySqlDbType.Int32),
  new MySqlParameter("@Config",MySqlDbType.Text),
 new MySqlParameter("@ClassName", MySqlDbType.VarChar,128),

                    };
			 parameters[0].Value = model.Name;
 parameters[1].Value = model.Description;
 parameters[2].Value = model.Namespace;
 parameters[3].Value = model.EntryFunctionName;
 parameters[4].Value = model.ReturnValueDescription;
 parameters[5].Value = model.SystemStatus;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.LastUpdateTime;
            parameters[8].Value = model.PluginsType;
            parameters[9].Value = model.Config;
            parameters[10].Value = model.ClassName;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(PluginsInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return 0;
            }
            else
            {
                 model.PluginsID = Convert.ToInt32(obj);
                 return model.PluginsID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool PluginsInfo_Update(PluginsInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(PluginsInfoTableName);
            strSql.Append(" set ");
			strSql.Append("Name=@Name,");
strSql.Append("Description=@Description,");
strSql.Append("Namespace=@Namespace,");
            strSql.Append("ClassName=@ClassName,");
            strSql.Append("EntryFunctionName=@EntryFunctionName,");
strSql.Append("ReturnValueDescription=@ReturnValueDescription,");
strSql.Append("SystemStatus=@SystemStatus,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("LastUpdateTime=@LastUpdateTime,");
            strSql.Append("PluginsType=@PluginsType,");
            strSql.Append("Config=@Config");

            strSql.Append(" where PluginsID=@PluginsID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@PluginsID", MySqlDbType.Int32,10),
 new MySqlParameter("@Name", MySqlDbType.VarChar,50),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 new MySqlParameter("@Namespace", MySqlDbType.VarChar,255),
 new MySqlParameter("@EntryFunctionName", MySqlDbType.VarChar,128),
 new MySqlParameter("@ReturnValueDescription", MySqlDbType.VarChar,2048),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime,16),
   new MySqlParameter("@PluginsType", MySqlDbType.Int32),
   new MySqlParameter("@Config",MySqlDbType.Text),
 new MySqlParameter("@ClassName", MySqlDbType.VarChar,128),

            };
			 parameters[0].Value = model.PluginsID;
 parameters[1].Value = model.Name;
 parameters[2].Value = model.Description;
 parameters[3].Value = model.Namespace;
 parameters[4].Value = model.EntryFunctionName;
 parameters[5].Value = model.ReturnValueDescription;
 parameters[6].Value = model.SystemStatus;
 parameters[7].Value = model.CreateTime;
 parameters[8].Value = model.LastUpdateTime;
            parameters[9].Value = model.PluginsType;
            parameters[10].Value = model.Config;
            parameters[11].Value = model.ClassName;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(PluginsInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool PluginsInfo_Delete(Int32 pluginsID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(PluginsInfoTableName);
            strSql.Append(" where PluginsID=@PluginsID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@PluginsID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = pluginsID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(PluginsInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool PluginsInfo_DeleteList(string pluginsIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(PluginsInfoTableName);
            strSql.Append(" where PluginsID in (" + pluginsIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(PluginsInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public PluginsInfo PluginsInfo_GetModel(Int32 pluginsID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(PluginsInfoTableField);
            strSql.Append(" from ");
            strSql.Append(PluginsInfoTableName);
            strSql.Append(" where PluginsID=@PluginsID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@PluginsID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = pluginsID;
            PluginsInfo model = new PluginsInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(PluginsInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return PluginsInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public PluginsInfo PluginsInfo_DataRowToModel(DataRow row)
        {
            PluginsInfo model = new PluginsInfo();
            if (row != null)
            {
				if (row["PluginsID"] != null )
                {
                    model.PluginsID = int.Parse(row["PluginsID"].ToString());
                }
				if (row["Name"] != null )
                {
					model.Name = row["Name"].ToString();
                }
				if (row["Description"] != null )
                {
					model.Description = row["Description"].ToString();
                }
				if (row["Namespace"] != null )
                {
					model.Namespace = row["Namespace"].ToString();
                }
                if (row["ClassName"] != null)
                {
                    model.ClassName = row["ClassName"].ToString();
                }
                if (row["EntryFunctionName"] != null )
                {
					model.EntryFunctionName = row["EntryFunctionName"].ToString();
                }
				if (row["ReturnValueDescription"] != null )
                {
					model.ReturnValueDescription = row["ReturnValueDescription"].ToString();
                }
				if (row["SystemStatus"] != null )
                {
                    model.SystemStatus = (PluginsState) int.Parse(row["SystemStatus"].ToString());
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["LastUpdateTime"] != null )
                {
					model.LastUpdateTime = DateTime.Parse(row["LastUpdateTime"].ToString());
                }
                if (row["PluginsType"] != null)
                {
                    model.PluginsType = int.Parse(row["PluginsType"].ToString());
                }
                if (row["Config"] != null)
                {
                    model.Config = row["Config"].ToString();
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet PluginsInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(PluginsInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(PluginsInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(PluginsInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet PluginsInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(PluginsInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(PluginsInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(PluginsInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int PluginsInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(PluginsInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(PluginsInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet PluginsInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(PluginsInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(PluginsInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(PluginsInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable PluginsInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "PluginsID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_plugins_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(PluginsInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
