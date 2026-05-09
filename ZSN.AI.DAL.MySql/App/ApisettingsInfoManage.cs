using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class ApisettingsInfoManage : IApisettingsInfoManage
    {
        ///表链接
        private string ApisettingsInfoConnectionName = "AppDb";
        ///表名称
        private string ApisettingsInfoTableName = "tb_apisettings_info";
        ///表字段
        private const string ApisettingsInfoTableField = "ApiID,CompanyID,AppID,SecretKey,SettingName,Remark,CreateTime,UpdateTime";
        ///添加用表字段
        private const string ApisettingsInfoTableFieldForAdd = "CompanyID,AppID,SecretKey,SettingName,Remark,CreateTime,UpdateTime";
        ///添加用表字段value
        private const string ApisettingsInfoTableFieldAltForAdd = "@CompanyID,@AppID,@SecretKey,@SettingName,@Remark,@CreateTime,@UpdateTime";
        public string SetConnectionName(string connName)
        {
            return ApisettingsInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public Int32 ApisettingsInfo_Add(ApisettingsInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(ApisettingsInfoTableName);
			strSql.Append(" (");
            strSql.Append(ApisettingsInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(ApisettingsInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@CompanyID", MySqlDbType.Int64),
 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
 new MySqlParameter("@SecretKey", MySqlDbType.VarChar,64),
 new MySqlParameter("@SettingName", MySqlDbType.VarChar,50),
 new MySqlParameter("@Remark", MySqlDbType.VarChar,512),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16)

					};
			 parameters[0].Value = model.CompanyID;
 parameters[1].Value = model.AppID;
 parameters[2].Value = model.SecretKey;
 parameters[3].Value = model.SettingName;
 parameters[4].Value = model.Remark;
 parameters[5].Value = model.CreateTime;
 parameters[6].Value = model.UpdateTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ApisettingsInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return 0;
            }
            else
            {
                 model.ApiID = Convert.ToInt32(obj);
                 return model.ApiID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool ApisettingsInfo_Update(ApisettingsInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(ApisettingsInfoTableName);
            strSql.Append(" set ");
			strSql.Append("CompanyID=@CompanyID,");
strSql.Append("AppID=@AppID,");
strSql.Append("SecretKey=@SecretKey,");
strSql.Append("SettingName=@SettingName,");
strSql.Append("Remark=@Remark,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("UpdateTime=@UpdateTime");

            strSql.Append(" where ApiID=@ApiID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@ApiID", MySqlDbType.Int32,10),
 new MySqlParameter("@CompanyID", MySqlDbType.Int64),
 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
 new MySqlParameter("@SecretKey", MySqlDbType.VarChar,64),
 new MySqlParameter("@SettingName", MySqlDbType.VarChar,50),
 new MySqlParameter("@Remark", MySqlDbType.VarChar,512),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16)

			};
			 parameters[0].Value = model.ApiID;
 parameters[1].Value = model.CompanyID;
 parameters[2].Value = model.AppID;
 parameters[3].Value = model.SecretKey;
 parameters[4].Value = model.SettingName;
 parameters[5].Value = model.Remark;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.UpdateTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ApisettingsInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool ApisettingsInfo_Delete(Int32 apiID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(ApisettingsInfoTableName);
            strSql.Append(" where ApiID=@ApiID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@ApiID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = apiID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ApisettingsInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ApisettingsInfo_DeleteByAppID(string AppID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(ApisettingsInfoTableName);
            strSql.Append(" where AppID=@AppID");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@AppID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = AppID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ApisettingsInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
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
        public bool ApisettingsInfo_DeleteList(string apiIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(ApisettingsInfoTableName);
            strSql.Append(" where ApiID in (" + apiIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ApisettingsInfoConnectionName), CommandType.Text,strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ApisettingsInfo_DeleteListByAppID(string AppIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(ApisettingsInfoTableName);
            strSql.Append(" where AppID in (" + AppIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ApisettingsInfoConnectionName), CommandType.Text, strSql.ToString());
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
        public ApisettingsInfo ApisettingsInfo_GetModel(Int32 apiID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(ApisettingsInfoTableField);
            strSql.Append(" from ");
            strSql.Append(ApisettingsInfoTableName);
            strSql.Append(" where ApiID=@ApiID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@ApiID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = apiID;
            ApisettingsInfo model = new ApisettingsInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ApisettingsInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return ApisettingsInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public ApisettingsInfo ApisettingsInfo_GetModelByAppID(string appID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(ApisettingsInfoTableField);
            strSql.Append(" from ");
            strSql.Append(ApisettingsInfoTableName);
            strSql.Append(" where AppID=@AppID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@AppID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = appID;
            ApisettingsInfo model = new ApisettingsInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ApisettingsInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return ApisettingsInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public ApisettingsInfo ApisettingsInfo_GetModelByCompanyID(int CompanyID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(ApisettingsInfoTableField);
            strSql.Append(" from ");
            strSql.Append(ApisettingsInfoTableName);
            strSql.Append(" where CompanyID=@CompanyID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@CompanyID", MySqlDbType.Int64)
            };
            parameters[0].Value = CompanyID;
            ApisettingsInfo model = new ApisettingsInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ApisettingsInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return ApisettingsInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public ApisettingsInfo ApisettingsInfo_DataRowToModel(DataRow row)
        {
            ApisettingsInfo model = new ApisettingsInfo();
            if (row != null)
            {
				if (row["ApiID"] != null )
                {
                    model.ApiID = int.Parse(row["ApiID"].ToString());
                }
				if (row["CompanyID"] != null )
                {
                    model.CompanyID = int.Parse(row["CompanyID"].ToString());
                }
				if (row["AppID"] != null )
                {
					model.AppID = row["AppID"].ToString();
                }
				if (row["SecretKey"] != null )
                {
					model.SecretKey = row["SecretKey"].ToString();
                }
				if (row["SettingName"] != null )
                {
					model.SettingName = row["SettingName"].ToString();
                }
				if (row["Remark"] != null )
                {
					model.Remark = row["Remark"].ToString();
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["UpdateTime"] != null )
                {
					model.UpdateTime = DateTime.Parse(row["UpdateTime"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet ApisettingsInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(ApisettingsInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(ApisettingsInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ApisettingsInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet ApisettingsInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(ApisettingsInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(ApisettingsInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ApisettingsInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int ApisettingsInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(ApisettingsInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ApisettingsInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet ApisettingsInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(ApisettingsInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(ApisettingsInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ApisettingsInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable ApisettingsInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "ApiID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_apisettings_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ApisettingsInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
