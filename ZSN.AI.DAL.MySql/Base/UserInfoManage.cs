using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class UserInfoManage : IUserInfoManage
    {
        ///表链接
        private string UserInfoConnectionName = "BaseDb";
        ///表名称
        private string UserInfoTableName = "tb_user_info";
        ///表字段
        private const string UserInfoTableField = "UserID,uName,uPWD,PermissionCode,uState,uAppendTime";
        ///添加用表字段
        private const string UserInfoTableFieldForAdd = "uName,uPWD,PermissionCode,uState,uAppendTime";
        ///添加用表字段value
        private const string UserInfoTableFieldAltForAdd = "@uName,@uPWD,@PermissionCode,@uState,@uAppendTime";
        public string SetConnectionName(string connName)
        {
            return UserInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public Int32 UserInfo_Add(UserInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(UserInfoTableName);
			strSql.Append(" (");
            strSql.Append(UserInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(UserInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@uName", MySqlDbType.VarChar,50),
 new MySqlParameter("@uPWD", MySqlDbType.VarChar,32),
 new MySqlParameter("@PermissionCode", MySqlDbType.VarChar,2048),
 new MySqlParameter("@uState", MySqlDbType.Int32,10),
 new MySqlParameter("@uAppendTime", MySqlDbType.DateTime,16)

					};
			 parameters[0].Value = model.UName;
 parameters[1].Value = model.UPWD;
 parameters[2].Value = model.PermissionCode;
 parameters[3].Value = model.UState;
 parameters[4].Value = model.UAppendTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(UserInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return 0;
            }
            else
            {
                 model.UserID = Convert.ToInt32(obj);
                 return model.UserID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool UserInfo_Update(UserInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(UserInfoTableName);
            strSql.Append(" set ");
			strSql.Append("uName=@uName,");
strSql.Append("uPWD=@uPWD,");
strSql.Append("PermissionCode=@PermissionCode,");
strSql.Append("uState=@uState,");
strSql.Append("uAppendTime=@uAppendTime");

            strSql.Append(" where UserID=@UserID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@UserID", MySqlDbType.Int32,10),
 new MySqlParameter("@uName", MySqlDbType.VarChar,50),
 new MySqlParameter("@uPWD", MySqlDbType.VarChar,32),
 new MySqlParameter("@PermissionCode", MySqlDbType.VarChar,2048),
 new MySqlParameter("@uState", MySqlDbType.Int32,10),
 new MySqlParameter("@uAppendTime", MySqlDbType.DateTime,16)

			};
			 parameters[0].Value = model.UserID;
 parameters[1].Value = model.UName;
 parameters[2].Value = model.UPWD;
 parameters[3].Value = model.PermissionCode;
 parameters[4].Value = model.UState;
 parameters[5].Value = model.UAppendTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(UserInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool UserInfo_UpdatePassword(UserInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(UserInfoTableName);
            strSql.Append(" set ");
            strSql.Append("uPWD=@uPWD ");

            strSql.Append(" where UserID=@UserID");
            MySqlParameter[] parameters = {
                 new MySqlParameter("@UserID", MySqlDbType.Int32,10),
 new MySqlParameter("@uPWD", MySqlDbType.VarChar,32),

            };
            parameters[0].Value = model.UserID;
            parameters[1].Value = model.UPWD;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(UserInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
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
        public bool UserInfo_Delete(Int32 userID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(UserInfoTableName);
            strSql.Append(" where UserID=@UserID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@UserID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = userID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(UserInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool UserInfo_DeleteList(string userIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(UserInfoTableName);
            strSql.Append(" where UserID in (" + userIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(UserInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public UserInfo UserInfo_GetModel(Int32 userID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(UserInfoTableField);
            strSql.Append(" from ");
            strSql.Append(UserInfoTableName);
            strSql.Append(" where UserID=@UserID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@UserID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = userID;
            UserInfo model = new UserInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(UserInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return UserInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public UserInfo UserInfo_GetModel(string username)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(UserInfoTableField);
            strSql.Append(" from ");
            strSql.Append(UserInfoTableName);
            strSql.Append(" where uName=@uName");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@uName", MySqlDbType.VarChar, 50)
            };
            parameters[0].Value = username;
            UserInfo model = new UserInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(UserInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return UserInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public UserInfo UserInfo_DataRowToModel(DataRow row)
        {
            UserInfo model = new UserInfo();
            if (row != null)
            {
				if (row["UserID"] != null )
                {
                    model.UserID = int.Parse(row["UserID"].ToString());
                }
				if (row["uName"] != null )
                {
					model.UName = row["uName"].ToString();
                }
				if (row["uPWD"] != null )
                {
					model.UPWD = row["uPWD"].ToString();
                }
				if (row["PermissionCode"] != null )
                {
					model.PermissionCode = row["PermissionCode"].ToString();
                }
				if (row["uState"] != null )
                {
                    model.UState = int.Parse(row["uState"].ToString());
                }
				if (row["uAppendTime"] != null )
                {
					model.UAppendTime = DateTime.Parse(row["uAppendTime"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet UserInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(UserInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(UserInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(UserInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet UserInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(UserInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(UserInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(UserInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int UserInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(UserInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(UserInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet UserInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(UserInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(UserInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(UserInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable UserInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "UserID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_user_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(UserInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
