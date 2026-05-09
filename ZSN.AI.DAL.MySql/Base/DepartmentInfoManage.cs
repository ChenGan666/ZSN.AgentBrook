using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.DAL.MySql
{
    public partial class DepartmentInfoManage : IDepartmentInfoManage
    {
        ///表链接
        private string DepartmentInfoConnectionName = "BaseDb";
        ///表名称
        private const string DepartmentInfoTableName = "tb_department_info";
        ///表字段
        private const string DepartmentInfoTableField = "DepartmentID,dName,dInfo,dAppendtime,dState";
        ///添加用表字段
        private const string DepartmentInfoTableFieldForAdd = "dName,dInfo,dAppendtime,dState";
        ///添加用表字段value
        private const string DepartmentInfoTableFieldAltForAdd = "@dName,@dInfo,@dAppendtime,@dState";
        public string SetConnectionName(string connName)
        {
            return DepartmentInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public int DepartmentInfo_Add(DepartmentInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(DepartmentInfoTableName);
			strSql.Append(" (");
            strSql.Append(DepartmentInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(DepartmentInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@dName", MySqlDbType.VarChar,50),
 new MySqlParameter("@dInfo", MySqlDbType.VarChar,512),
 new MySqlParameter("@dAppendtime", MySqlDbType.DateTime,16),
 new MySqlParameter("@dState", MySqlDbType.Int32,10)

					};
			 parameters[0].Value = model.DName;
 parameters[1].Value = model.DInfo;
 parameters[2].Value = model.DAppendtime;
 parameters[3].Value = model.DState;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(DepartmentInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        /// 更新一条数据
        /// </summary>
        public bool DepartmentInfo_Update(DepartmentInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(DepartmentInfoTableName);
            strSql.Append(" set ");
			strSql.Append("dName=@dName,");
strSql.Append("dInfo=@dInfo,");
strSql.Append("dAppendtime=@dAppendtime,");
strSql.Append("dState=@dState");

            strSql.Append(" where DepartmentID=@DepartmentID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@DepartmentID", MySqlDbType.Int32,10),
 new MySqlParameter("@dName", MySqlDbType.VarChar,50),
 new MySqlParameter("@dInfo", MySqlDbType.VarChar,512),
 new MySqlParameter("@dAppendtime", MySqlDbType.DateTime,16),
 new MySqlParameter("@dState", MySqlDbType.Int32,10)

			};
			 parameters[0].Value = model.DepartmentID;
 parameters[1].Value = model.DName;
 parameters[2].Value = model.DInfo;
 parameters[3].Value = model.DAppendtime;
 parameters[4].Value = model.DState;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(DepartmentInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool DepartmentInfo_Delete(Int32 departmentID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(DepartmentInfoTableName);
            strSql.Append(" where DepartmentID=@DepartmentID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@DepartmentID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = departmentID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(DepartmentInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool DepartmentInfo_DeleteList(string departmentIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(DepartmentInfoTableName);
            strSql.Append(" where DepartmentID in (" + departmentIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(DepartmentInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public DepartmentInfo DepartmentInfo_GetModel(Int32 departmentID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(DepartmentInfoTableField);
            strSql.Append(" from ");
            strSql.Append(DepartmentInfoTableName);
            strSql.Append(" where DepartmentID=@DepartmentID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@DepartmentID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = departmentID;
            DepartmentInfo model = new DepartmentInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(DepartmentInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return DepartmentInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public DepartmentInfo DepartmentInfo_DataRowToModel(DataRow row)
        {
            DepartmentInfo model = new DepartmentInfo();
            if (row != null)
            {
				if (row["DepartmentID"] != null )
                {
                        model.DepartmentID = int.Parse(row["DepartmentID"].ToString());
                }
				if (row["dName"] != null )
                {
					model.DName = row["dName"].ToString();
                }
				if (row["dInfo"] != null )
                {
					model.DInfo = row["dInfo"].ToString();
                }
				if (row["dAppendtime"] != null )
                {
					model.DAppendtime = DateTime.Parse(row["dAppendtime"].ToString());
                }
				if (row["dState"] != null )
                {
                        model.DState = int.Parse(row["dState"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet DepartmentInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(DepartmentInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(DepartmentInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(DepartmentInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet DepartmentInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(DepartmentInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(DepartmentInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(DepartmentInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int DepartmentInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(DepartmentInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(DepartmentInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet DepartmentInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(DepartmentInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(DepartmentInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(DepartmentInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable DepartmentInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "DepartmentID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_department_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(DepartmentInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
