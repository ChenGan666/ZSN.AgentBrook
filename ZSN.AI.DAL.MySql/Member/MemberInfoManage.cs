using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class MemberInfoManage : IMemberInfoManage
    {
        ///表链接
        private string MemberInfoConnectionName = "MemberDb";
        ///表名称
        private string MemberInfoTableName = "tb_member_info";
        ///表字段
        private const string MemberInfoTableField = "MemberID,mPhoneNumber,mNickName,mPWD,mIcon,mBirthday,mState,mPoints,mLevel,mIntroducer,mAppendTime";
        ///添加用表字段
        private const string MemberInfoTableFieldForAdd = "MemberID,mPhoneNumber,mNickName,mPWD,mIcon,mBirthday,mState,mPoints,mLevel,mIntroducer,mAppendTime";
        ///添加用表字段value
        private const string MemberInfoTableFieldAltForAdd = "@MemberID,@mPhoneNumber,@mNickName,@mPWD,@mIcon,@mBirthday,@mState,@mPoints,@mLevel,@mIntroducer,@mAppendTime";
        public string SetConnectionName(string connName)
        {
            return MemberInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string MemberInfo_Add(MemberInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(MemberInfoTableName);
			strSql.Append(" (");
            strSql.Append(MemberInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(MemberInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@mPhoneNumber", MySqlDbType.VarChar,50),
 new MySqlParameter("@mNickName", MySqlDbType.VarChar,50),
 new MySqlParameter("@mPWD", MySqlDbType.VarChar,32),
 new MySqlParameter("@mIcon", MySqlDbType.VarChar,128),
 new MySqlParameter("@mBirthday", MySqlDbType.Date,16),
 new MySqlParameter("@mState", MySqlDbType.Int32,10),
 new MySqlParameter("@mPoints", MySqlDbType.Int32,10),
 new MySqlParameter("@mLevel", MySqlDbType.Int32,10),
 new MySqlParameter("@mIntroducer", MySqlDbType.VarChar,64),
 new MySqlParameter("@mAppendTime", MySqlDbType.DateTime,16)

					};
			 parameters[0].Value = model.MemberID;
 parameters[1].Value = model.MPhoneNumber;
 parameters[2].Value = model.MNickName;
 parameters[3].Value = model.MPWD;
 parameters[4].Value = model.MIcon;
 parameters[5].Value = model.MBirthday;
 parameters[6].Value = model.MState;
 parameters[7].Value = model.MPoints;
 parameters[8].Value = model.MLevel;
 parameters[9].Value = model.MIntroducer;
 parameters[10].Value = model.MAppendTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MemberInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.MemberID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool MemberInfo_Update(MemberInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(MemberInfoTableName);
            strSql.Append(" set ");
strSql.Append("mPhoneNumber=@mPhoneNumber,");
strSql.Append("mNickName=@mNickName,");
strSql.Append("mPWD=@mPWD,");
strSql.Append("mIcon=@mIcon,");
strSql.Append("mBirthday=@mBirthday,");
strSql.Append("mState=@mState,");
strSql.Append("mPoints=@mPoints,");
strSql.Append("mLevel=@mLevel,");
strSql.Append("mIntroducer=@mIntroducer,");
strSql.Append("mAppendTime=@mAppendTime");

            strSql.Append(" where MemberID=@MemberID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@mPhoneNumber", MySqlDbType.VarChar,50),
 new MySqlParameter("@mNickName", MySqlDbType.VarChar,50),
 new MySqlParameter("@mPWD", MySqlDbType.VarChar,32),
 new MySqlParameter("@mIcon", MySqlDbType.VarChar,128),
 new MySqlParameter("@mBirthday", MySqlDbType.Date,16),
 new MySqlParameter("@mState", MySqlDbType.Int32,10),
 new MySqlParameter("@mPoints", MySqlDbType.Int32,10),
 new MySqlParameter("@mLevel", MySqlDbType.Int32,10),
 new MySqlParameter("@mIntroducer", MySqlDbType.VarChar,64),
 new MySqlParameter("@mAppendTime", MySqlDbType.DateTime,16)

			};
			 parameters[0].Value = model.MemberID;
 parameters[1].Value = model.MPhoneNumber;
 parameters[2].Value = model.MNickName;
 parameters[3].Value = model.MPWD;
 parameters[4].Value = model.MIcon;
 parameters[5].Value = model.MBirthday;
 parameters[6].Value = model.MState;
 parameters[7].Value = model.MPoints;
 parameters[8].Value = model.MLevel;
 parameters[9].Value = model.MIntroducer;
 parameters[10].Value = model.MAppendTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MemberInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool MemberInfo_Delete(string memberID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(MemberInfoTableName);
            strSql.Append(" where MemberID=@MemberID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@MemberID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = memberID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MemberInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool MemberInfo_DeleteList(string memberIDlist)
        {
            if (memberIDlist.IndexOf(",") > -1)
            {
                string[] items = memberIDlist.Split(',');
                memberIDlist = String.Join("','", items.Select(item => item.Trim()));
            }
            memberIDlist = "'" + memberIDlist + "'";

            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(MemberInfoTableName);
            strSql.Append(" where MemberID in (" + memberIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MemberInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public MemberInfo MemberInfo_GetModel(string memberID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberInfoTableField);
            strSql.Append(" from ");
            strSql.Append(MemberInfoTableName);
            strSql.Append(" where MemberID=@MemberID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@MemberID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = memberID;
            MemberInfo model = new MemberInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MemberInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public MemberInfo MemberInfo_GetModelByPhoneNumber(string PhoneNumber)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberInfoTableField);
            strSql.Append(" from ");
            strSql.Append(MemberInfoTableName);
            strSql.Append(" where mPhoneNumber=@mPhoneNumber");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@mPhoneNumber", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = PhoneNumber;
            MemberInfo model = new MemberInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MemberInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public MemberInfo MemberInfo_DataRowToModel(DataRow row)
        {
            MemberInfo model = new MemberInfo();
            if (row != null)
            {
				if (row["MemberID"] != null )
                {
					model.MemberID = row["MemberID"].ToString();
                }
				
				if (row["mPhoneNumber"] != null )
                {
					model.MPhoneNumber = row["mPhoneNumber"].ToString();
                }
				if (row["mNickName"] != null )
                {
					model.MNickName = row["mNickName"].ToString();
                }
				if (row["mPWD"] != null )
                {
					model.MPWD = row["mPWD"].ToString();
                }
				if (row["mIcon"] != null )
                {
					model.MIcon = row["mIcon"].ToString();
                }
				if (row["mBirthday"] != null )
                {
					model.MBirthday = DateTime.Parse(row["mBirthday"].ToString());
                }
				if (row["mState"] != null )
                {
                    model.MState = int.Parse(row["mState"].ToString());
                }
				if (row["mPoints"] != null )
                {
                    model.MPoints = int.Parse(row["mPoints"].ToString());
                }
				if (row["mLevel"] != null )
                {
                    model.MLevel = int.Parse(row["mLevel"].ToString());
                }
				if (row["mIntroducer"] != null )
                {
					model.MIntroducer = row["mIntroducer"].ToString();
                }
				if (row["mAppendTime"] != null )
                {
					model.MAppendTime = DateTime.Parse(row["mAppendTime"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet MemberInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MemberInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet MemberInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MemberInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int MemberInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(MemberInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MemberInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet MemberInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MemberInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable MemberInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "MemberID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_member_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
