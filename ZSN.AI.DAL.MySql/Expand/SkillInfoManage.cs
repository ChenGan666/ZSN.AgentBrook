using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class SkillInfoManage : ISkillInfoManage
    {
        ///表链接
        private string SkillInfoConnectionName = "ExpandDb";
        ///表名称
        private string SkillInfoTableName = "tb_skill_info";
        ///表字段
        private const string SkillInfoTableField = "SkillID,sName,sDescription,SkillDirectory,CreateTime,UpdateTime,SystemStatus";
        ///添加用表字段
        private const string SkillInfoTableFieldForAdd = "SkillID,sName,sDescription,SkillDirectory,CreateTime,UpdateTime,SystemStatus";
        ///添加用表字段value
        private const string SkillInfoTableFieldAltForAdd = "@SkillID,@sName,@sDescription,@SkillDirectory,@CreateTime,@UpdateTime,@SystemStatus";
        public string SetConnectionName(string connName)
        {
            return SkillInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string SkillInfo_Add(SkillInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(SkillInfoTableName);
			strSql.Append(" (");
            strSql.Append(SkillInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(SkillInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@SkillID", MySqlDbType.VarChar,64),
 new MySqlParameter("@sName", MySqlDbType.VarChar,50),
 new MySqlParameter("@sDescription", MySqlDbType.VarChar,1024),
 new MySqlParameter("@SkillDirectory", MySqlDbType.VarChar,512),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10)

					};
			 parameters[0].Value = model.SkillID;
 parameters[1].Value = model.SName;
 parameters[2].Value = model.SDescription;
 parameters[3].Value = model.SkillDirectory;
 parameters[4].Value = model.CreateTime;
 parameters[5].Value = model.UpdateTime;
 parameters[6].Value = model.SystemStatus;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(SkillInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 model.SkillID = obj.ToString();
                 return model.SkillID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool SkillInfo_Update(SkillInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(SkillInfoTableName);
            strSql.Append(" set ");
			strSql.Append("sName=@sName,");
strSql.Append("sDescription=@sDescription,");
strSql.Append("SkillDirectory=@SkillDirectory,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("UpdateTime=@UpdateTime,");
strSql.Append("SystemStatus=@SystemStatus");

            strSql.Append(" where SkillID=@SkillID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@SkillID", MySqlDbType.VarChar,64),
 new MySqlParameter("@sName", MySqlDbType.VarChar,50),
 new MySqlParameter("@sDescription", MySqlDbType.VarChar,1024),
 new MySqlParameter("@SkillDirectory", MySqlDbType.VarChar,512),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10)

			};
			 parameters[0].Value = model.SkillID;
 parameters[1].Value = model.SName;
 parameters[2].Value = model.SDescription;
 parameters[3].Value = model.SkillDirectory;
 parameters[4].Value = model.CreateTime;
 parameters[5].Value = model.UpdateTime;
 parameters[6].Value = model.SystemStatus;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(SkillInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool SkillInfo_Delete(string skillID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(SkillInfoTableName);
            strSql.Append(" where SkillID=@SkillID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@SkillID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = skillID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(SkillInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool SkillInfo_DeleteList(string skillIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(SkillInfoTableName);
            strSql.Append(" where SkillID in (" + skillIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(SkillInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public SkillInfo SkillInfo_GetModel(string skillID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(SkillInfoTableField);
            strSql.Append(" from ");
            strSql.Append(SkillInfoTableName);
            strSql.Append(" where SkillID=@SkillID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@SkillID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = skillID;
            SkillInfo model = new SkillInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(SkillInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return SkillInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public SkillInfo SkillInfo_DataRowToModel(DataRow row)
        {
            SkillInfo model = new SkillInfo();
            if (row != null)
            {
				if (row["SkillID"] != null )
                {
					model.SkillID = row["SkillID"].ToString();
                }
				if (row["sName"] != null )
                {
					model.SName = row["sName"].ToString();
                }
				if (row["sDescription"] != null )
                {
					model.SDescription = row["sDescription"].ToString();
                }
				if (row["SkillDirectory"] != null )
                {
					model.SkillDirectory = row["SkillDirectory"].ToString();
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["UpdateTime"] != null )
                {
					model.UpdateTime = DateTime.Parse(row["UpdateTime"].ToString());
                }
				if (row["SystemStatus"] != null )
                {
                    model.SystemStatus = int.Parse(row["SystemStatus"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet SkillInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(SkillInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(SkillInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(SkillInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet SkillInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(SkillInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(SkillInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(SkillInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int SkillInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(SkillInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(SkillInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet SkillInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(SkillInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(SkillInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(SkillInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable SkillInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "SkillID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_skill_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(SkillInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
