using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class MemberAuthInfoManage : IMemberAuthInfoManage
    {
        ///表链接
        private string MemberAuthInfoConnectionName = "MemberDb";
        ///表名称
        private string MemberAuthInfoTableName = "tb_member_auth_info";
        ///表字段
        private const string MemberAuthInfoTableField = "MemberAuthID,MemberID,AccessToken,RefreshToken,maAppendTime,maUpdateTime";
        ///添加用表字段
        private const string MemberAuthInfoTableFieldForAdd = "MemberID,AccessToken,RefreshToken,maAppendTime,maUpdateTime";
        ///添加用表字段value
        private const string MemberAuthInfoTableFieldAltForAdd = "@MemberID,@AccessToken,@RefreshToken,@maAppendTime,@maUpdateTime";
        public string SetConnectionName(string connName)
        {
            return MemberAuthInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public int MemberAuthInfo_Add(MemberAuthInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(MemberAuthInfoTableName);
			strSql.Append(" (");
            strSql.Append(MemberAuthInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(MemberAuthInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@AccessToken", MySqlDbType.VarChar,256),
 new MySqlParameter("@RefreshToken", MySqlDbType.VarChar,256),
 new MySqlParameter("@maAppendTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@maUpdateTime", MySqlDbType.DateTime,16)

					};
			 parameters[0].Value = model.MemberID;
 parameters[1].Value = model.AccessToken;
 parameters[2].Value = model.RefreshToken;
 parameters[3].Value = model.MaAppendTime;
 parameters[4].Value = model.MaUpdateTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MemberAuthInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return 0;
            }
            else
            {
                 
                 return model.MemberAuthID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool MemberAuthInfo_Update(MemberAuthInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(MemberAuthInfoTableName);
            strSql.Append(" set ");
			strSql.Append("MemberID=@MemberID,");
strSql.Append("AccessToken=@AccessToken,");
strSql.Append("RefreshToken=@RefreshToken,");
strSql.Append("maAppendTime=@maAppendTime,");
strSql.Append("maUpdateTime=@maUpdateTime");

            strSql.Append(" where MemberAuthID=@MemberAuthID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@MemberAuthID", MySqlDbType.Int64,19),
 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@AccessToken", MySqlDbType.VarChar,256),
 new MySqlParameter("@RefreshToken", MySqlDbType.VarChar,256),
 new MySqlParameter("@maAppendTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@maUpdateTime", MySqlDbType.DateTime,16)

			};
			 parameters[0].Value = model.MemberAuthID;
 parameters[1].Value = model.MemberID;
 parameters[2].Value = model.AccessToken;
 parameters[3].Value = model.RefreshToken;
 parameters[4].Value = model.MaAppendTime;
 parameters[5].Value = model.MaUpdateTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MemberAuthInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool MemberAuthInfo_Delete(Int64 memberAuthID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(MemberAuthInfoTableName);
            strSql.Append(" where MemberAuthID=@MemberAuthID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@MemberAuthID", MySqlDbType.Int64, 19)
			};
            parameters[0].Value = memberAuthID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MemberAuthInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool MemberAuthInfo_DeleteList(string memberAuthIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(MemberAuthInfoTableName);
            strSql.Append(" where MemberAuthID in (" + memberAuthIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MemberAuthInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public MemberAuthInfo MemberAuthInfo_GetModel(Int64 memberAuthID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberAuthInfoTableField);
            strSql.Append(" from ");
            strSql.Append(MemberAuthInfoTableName);
            strSql.Append(" where MemberAuthID=@MemberAuthID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@MemberAuthID", MySqlDbType.Int64, 19)
			};
            parameters[0].Value = memberAuthID;
            MemberAuthInfo model = new MemberAuthInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberAuthInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MemberAuthInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public MemberAuthInfo MemberAuthInfo_GetModel(string memberID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberAuthInfoTableField);
            strSql.Append(" from ");
            strSql.Append(MemberAuthInfoTableName);
            strSql.Append(" where memberID=@memberID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@memberID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = memberID;
            MemberAuthInfo model = new MemberAuthInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberAuthInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MemberAuthInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public MemberAuthInfo MemberAuthInfo_GetModelByMemberID(string MemberID)
        {
            return MemberAuthInfo_GetModel(MemberID);
        }
        public MemberAuthInfo MemberAuthInfo_GetModelByAccessToken(string AccessToken)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberAuthInfoTableField);
            strSql.Append(" from ");
            strSql.Append(MemberAuthInfoTableName);
            strSql.Append(" where AccessToken=@AccessToken");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@AccessToken", MySqlDbType.VarChar, 256)
            };
            parameters[0].Value = AccessToken;
            MemberAuthInfo model = new MemberAuthInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberAuthInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MemberAuthInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public MemberAuthInfo MemberAuthInfo_GetModelByRefreshToken(string RefreshToken)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberAuthInfoTableField);
            strSql.Append(" from ");
            strSql.Append(MemberAuthInfoTableName);
            strSql.Append(" where RefreshToken=@RefreshToken");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@RefreshToken", MySqlDbType.VarChar, 256)
            };
            parameters[0].Value = RefreshToken;
            MemberAuthInfo model = new MemberAuthInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberAuthInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MemberAuthInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public MemberAuthInfo MemberAuthInfo_DataRowToModel(DataRow row)
        {
            MemberAuthInfo model = new MemberAuthInfo();
            if (row != null)
            {
				if (row["MemberAuthID"] != null )
                {
					model.MemberAuthID = int.Parse(row["MemberAuthID"].ToString());
                }
				if (row["MemberID"] != null )
                {
					model.MemberID = row["MemberID"].ToString();
                }
				if (row["AccessToken"] != null )
                {
					model.AccessToken = row["AccessToken"].ToString();
                }
				if (row["RefreshToken"] != null )
                {
					model.RefreshToken = row["RefreshToken"].ToString();
                }
				if (row["maAppendTime"] != null )
                {
					model.MaAppendTime = DateTime.Parse(row["maAppendTime"].ToString());
                }
				if (row["maUpdateTime"] != null )
                {
					model.MaUpdateTime = DateTime.Parse(row["maUpdateTime"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet MemberAuthInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberAuthInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MemberAuthInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberAuthInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet MemberAuthInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberAuthInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MemberAuthInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberAuthInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int MemberAuthInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(MemberAuthInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MemberAuthInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet MemberAuthInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberAuthInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MemberAuthInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberAuthInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable MemberAuthInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "MemberAuthID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_member_auth_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberAuthInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
