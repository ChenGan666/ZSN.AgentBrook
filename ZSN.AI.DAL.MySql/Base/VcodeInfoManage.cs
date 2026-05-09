using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class VcodeInfoManage : IVcodeInfoManage
    {
        ///表链接
        private string VcodeInfoConnectionName = "BaseDb";
        ///表名称
        private string VcodeInfoTableName = "tb_vcode_info";
        ///表字段
        private const string VcodeInfoTableField = "VCodeID,PhoneNumber,VCode,VAppendTime,VState";
        ///添加用表字段
        private const string VcodeInfoTableFieldForAdd = "PhoneNumber,VCode,VAppendTime,VState";
        ///添加用表字段value
        private const string VcodeInfoTableFieldAltForAdd = "@PhoneNumber,@VCode,@VAppendTime,@VState";
        public string SetConnectionName(string connName)
        {
            return VcodeInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public Int32 VcodeInfo_Add(VcodeInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(VcodeInfoTableName);
			strSql.Append(" (");
            strSql.Append(VcodeInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(VcodeInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@PhoneNumber", MySqlDbType.VarChar,20),
 new MySqlParameter("@VCode", MySqlDbType.VarChar,6),
 new MySqlParameter("@VAppendTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@VState", MySqlDbType.Int32,10)

					};
			 parameters[0].Value = model.PhoneNumber;
 parameters[1].Value = model.VCode;
 parameters[2].Value = model.VAppendTime;
 parameters[3].Value = model.VState;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(VcodeInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return 0;
            }
            else
            {
                 model.VCodeID = Convert.ToInt32(obj);
                 return model.VCodeID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool VcodeInfo_Update(VcodeInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(VcodeInfoTableName);
            strSql.Append(" set ");
			strSql.Append("PhoneNumber=@PhoneNumber,");
strSql.Append("VCode=@VCode,");
strSql.Append("VAppendTime=@VAppendTime,");
strSql.Append("VState=@VState");

            strSql.Append(" where VCodeID=@VCodeID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@VCodeID", MySqlDbType.Int32,10),
 new MySqlParameter("@PhoneNumber", MySqlDbType.VarChar,20),
 new MySqlParameter("@VCode", MySqlDbType.VarChar,6),
 new MySqlParameter("@VAppendTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@VState", MySqlDbType.Int32,10)

			};
			 parameters[0].Value = model.VCodeID;
 parameters[1].Value = model.PhoneNumber;
 parameters[2].Value = model.VCode;
 parameters[3].Value = model.VAppendTime;
 parameters[4].Value = model.VState;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(VcodeInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool VcodeInfo_Delete(Int32 vCodeID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(VcodeInfoTableName);
            strSql.Append(" where VCodeID=@VCodeID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@VCodeID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = vCodeID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(VcodeInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool VcodeInfo_DeleteList(string vCodeIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(VcodeInfoTableName);
            strSql.Append(" where VCodeID in (" + vCodeIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(VcodeInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public VcodeInfo VcodeInfo_GetModel(Int32 vCodeID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(VcodeInfoTableField);
            strSql.Append(" from ");
            strSql.Append(VcodeInfoTableName);
            strSql.Append(" where VCodeID=@VCodeID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@VCodeID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = vCodeID;
            VcodeInfo model = new VcodeInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(VcodeInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return VcodeInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public VcodeInfo VcodeInfo_DataRowToModel(DataRow row)
        {
            VcodeInfo model = new VcodeInfo();
            if (row != null)
            {
				if (row["VCodeID"] != null )
                {
                    model.VCodeID = int.Parse(row["VCodeID"].ToString());
                }
				if (row["PhoneNumber"] != null )
                {
					model.PhoneNumber = row["PhoneNumber"].ToString();
                }
				if (row["VCode"] != null )
                {
					model.VCode = row["VCode"].ToString();
                }
				if (row["VAppendTime"] != null )
                {
					model.VAppendTime = DateTime.Parse(row["VAppendTime"].ToString());
                }
				if (row["VState"] != null )
                {
                    model.VState = int.Parse(row["VState"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet VcodeInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(VcodeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(VcodeInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(VcodeInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet VcodeInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(VcodeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(VcodeInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(VcodeInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int VcodeInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(VcodeInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(VcodeInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet VcodeInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(VcodeInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(VcodeInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(VcodeInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable VcodeInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "VCodeID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_vcode_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(VcodeInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
