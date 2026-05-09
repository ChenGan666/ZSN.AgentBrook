using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.DAL.MySql
{
    public partial class StaffInfoManage : IStaffInfoManage
    {
        ///表链接
        private string StaffInfoConnectionName = "BaseDb";
        ///表名称
        private const string StaffInfoTableName = "tb_staff_info";
        ///表字段
        private const string StaffInfoTableField = "StaffID,sCode,sName,sTitle,DepartmentID,dName,sEntryTime,sState,sAppendTime,sEmail,sPhone,MemberID,UserID";
        ///添加用表字段
        private const string StaffInfoTableFieldForAdd = "sCode,sName,sTitle,DepartmentID,dName,sEntryTime,sState,sAppendTime,sEmail,sPhone,MemberID,UserID";
        ///添加用表字段value
        private const string StaffInfoTableFieldAltForAdd = "@sCode,@sName,@sTitle,@DepartmentID,@dName,@sEntryTime,@sState,@sAppendTime,@sEmail,@sPhone,@MemberID,@UserID";
        public string SetConnectionName(string connName)
        {
            return StaffInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public int StaffInfo_Add(StaffInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(StaffInfoTableName);
			strSql.Append(" (");
            strSql.Append(StaffInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(StaffInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@sCode", MySqlDbType.VarChar,50),
 new MySqlParameter("@sName", MySqlDbType.VarChar,50),
 new MySqlParameter("@sTitle", MySqlDbType.VarChar,50),
 new MySqlParameter("@DepartmentID", MySqlDbType.Int32,10),
 new MySqlParameter("@dName", MySqlDbType.VarChar,50),
 new MySqlParameter("@sEntryTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@sState", MySqlDbType.Int32,10),
 new MySqlParameter("@sAppendTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@sEmail", MySqlDbType.VarChar,50),
 new MySqlParameter("@sPhone", MySqlDbType.VarChar,50),
 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@UserID", MySqlDbType.Int32)

                    };
			 parameters[0].Value = model.SCode;
 parameters[1].Value = model.SName;
 parameters[2].Value = model.STitle;
 parameters[3].Value = model.DepartmentID;
 parameters[4].Value = model.DName;
 parameters[5].Value = model.SEntryTime;
 parameters[6].Value = model.SState;
 parameters[7].Value = model.SAppendTime;
 parameters[8].Value = model.SEmail;
 parameters[9].Value = model.SPhone;
 parameters[10].Value = model.MemberID;
            parameters[11].Value = model.UserID;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(StaffInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool StaffInfo_Update(StaffInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(StaffInfoTableName);
            strSql.Append(" set ");
			strSql.Append("sCode=@sCode,");
strSql.Append("sName=@sName,");
strSql.Append("sTitle=@sTitle,");
strSql.Append("DepartmentID=@DepartmentID,");
strSql.Append("dName=@dName,");
strSql.Append("sEntryTime=@sEntryTime,");
strSql.Append("sState=@sState,");
strSql.Append("sAppendTime=@sAppendTime,");
strSql.Append("sEmail=@sEmail,");
strSql.Append("sPhone=@sPhone,");
strSql.Append("MemberID=@MemberID,");
            strSql.Append("UserID=@UserID");

            strSql.Append(" where StaffID=@StaffID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@StaffID", MySqlDbType.Int32,10),
 new MySqlParameter("@sCode", MySqlDbType.VarChar,50),
 new MySqlParameter("@sName", MySqlDbType.VarChar,50),
 new MySqlParameter("@sTitle", MySqlDbType.VarChar,50),
 new MySqlParameter("@DepartmentID", MySqlDbType.Int32,10),
 new MySqlParameter("@dName", MySqlDbType.VarChar,50),
 new MySqlParameter("@sEntryTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@sState", MySqlDbType.Int32,10),
 new MySqlParameter("@sAppendTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@sEmail", MySqlDbType.VarChar,50),
 new MySqlParameter("@sPhone", MySqlDbType.VarChar,50),
 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@UserID", MySqlDbType.Int32)

            };
			 parameters[0].Value = model.StaffID;
 parameters[1].Value = model.SCode;
 parameters[2].Value = model.SName;
 parameters[3].Value = model.STitle;
 parameters[4].Value = model.DepartmentID;
 parameters[5].Value = model.DName;
 parameters[6].Value = model.SEntryTime;
 parameters[7].Value = model.SState;
 parameters[8].Value = model.SAppendTime;
 parameters[9].Value = model.SEmail;
 parameters[10].Value = model.SPhone;
 parameters[11].Value = model.MemberID;
 parameters[12].Value = model.UserID;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(StaffInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool StaffInfo_Delete(Int32 staffID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(StaffInfoTableName);
            strSql.Append(" where StaffID=@StaffID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@StaffID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = staffID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(StaffInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool StaffInfo_DeleteList(string staffIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(StaffInfoTableName);
            strSql.Append(" where StaffID in (" + staffIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(StaffInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public StaffInfo StaffInfo_GetModel(Int32 staffID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(StaffInfoTableField);
            strSql.Append(" from ");
            strSql.Append(StaffInfoTableName);
            strSql.Append(" where StaffID=@StaffID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@StaffID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = staffID;
            StaffInfo model = new StaffInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(StaffInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return StaffInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public StaffInfo StaffInfo_GetModel(string MemberID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(StaffInfoTableField);
            strSql.Append(" from ");
            strSql.Append(StaffInfoTableName);
            strSql.Append(" where MemberID=@MemberID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@MemberID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = MemberID;
            StaffInfo model = new StaffInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(StaffInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return StaffInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public StaffInfo StaffInfo_DataRowToModel(DataRow row)
        {
            StaffInfo model = new StaffInfo();
            if (row != null)
            {
				if (row["StaffID"] != null )
                {
                        model.StaffID = int.Parse(row["StaffID"].ToString());
                }
				if (row["sCode"] != null )
                {
					model.SCode = row["sCode"].ToString();
                }
				if (row["sName"] != null )
                {
					model.SName = row["sName"].ToString();
                }
				if (row["sTitle"] != null )
                {
					model.STitle = row["sTitle"].ToString();
                }
				if (row["DepartmentID"] != null )
                {
                        model.DepartmentID = int.Parse(row["DepartmentID"].ToString());
                }
				if (row["dName"] != null )
                {
					model.DName = row["dName"].ToString();
                }
				if (row["sEntryTime"] != null )
                {
					model.SEntryTime = DateTime.Parse(row["sEntryTime"].ToString());
                }
				if (row["sState"] != null )
                {
                        model.SState = int.Parse(row["sState"].ToString());
                }
				if (row["sAppendTime"] != null )
                {
					model.SAppendTime = DateTime.Parse(row["sAppendTime"].ToString());
                }
				if (row["sEmail"] != null )
                {
					model.SEmail = row["sEmail"].ToString();
                }
				if (row["sPhone"] != null )
                {
					model.SPhone = row["sPhone"].ToString();
                }
				if (row["MemberID"] != null )
                {
					model.MemberID = row["MemberID"].ToString();
                }
                if (row["UserID"] != null)
                {
                    model.UserID = int.Parse(row["UserID"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet StaffInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(StaffInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(StaffInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(StaffInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet StaffInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(StaffInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(StaffInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(StaffInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int StaffInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(StaffInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(StaffInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet StaffInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(StaffInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(StaffInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(StaffInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable StaffInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "StaffID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_staff_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(StaffInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
