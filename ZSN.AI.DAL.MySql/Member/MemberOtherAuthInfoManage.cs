using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class MemberOtherAuthInfoManage : IMemberOtherAuthInfoManage
    {
        ///表链接
        private string MemberOtherAuthInfoConnectionName = "MemberDb";
        ///表名称
        private string MemberOtherAuthInfoTableName = "tb_member_other_auth_info";
        ///表字段
        private const string MemberOtherAuthInfoTableField = "MemberOtherAuthID,MemberID,Server_Type,Server_ID,User_Nickname,OpenID,UnionID,Session_Key,AccessToken,RefreshToken,Phone,Sex,Language,Country,Province,City,Region,Head_img,Subscribe,Remake,Append_Time,Update_Time";
        ///添加用表字段
        private const string MemberOtherAuthInfoTableFieldForAdd = "MemberID,Server_Type,Server_ID,User_Nickname,OpenID,UnionID,Session_Key,AccessToken,RefreshToken,Phone,Sex,Language,Country,Province,City,Region,Head_img,Subscribe,Remake,Append_Time,Update_Time";
        ///添加用表字段value
        private const string MemberOtherAuthInfoTableFieldAltForAdd = "@MemberID,@Server_Type,@Server_ID,@User_Nickname,@OpenID,@UnionID,@Session_Key,@AccessToken,@RefreshToken,@Phone,@Sex,@Language,@Country,@Province,@City,@Region,@Head_img,@Subscribe,@Remake,@Append_Time,@Update_Time";
        public string SetConnectionName(string connName)
        {
            return MemberOtherAuthInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public Int32 MemberOtherAuthInfo_Add(MemberOtherAuthInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(MemberOtherAuthInfoTableName);
			strSql.Append(" (");
            strSql.Append(MemberOtherAuthInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(MemberOtherAuthInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Server_Type", MySqlDbType.Int32,10),
 new MySqlParameter("@Server_ID", MySqlDbType.Int32,10),
 new MySqlParameter("@User_Nickname", MySqlDbType.VarChar,50),
 new MySqlParameter("@OpenID", MySqlDbType.VarChar,255),
 new MySqlParameter("@UnionID", MySqlDbType.VarChar,255),
 new MySqlParameter("@Session_Key", MySqlDbType.VarChar,255),
 new MySqlParameter("@AccessToken", MySqlDbType.VarChar,255),
 new MySqlParameter("@RefreshToken", MySqlDbType.VarChar,255),
 new MySqlParameter("@Phone", MySqlDbType.VarChar,50),
 new MySqlParameter("@Sex", MySqlDbType.Int32,10),
 new MySqlParameter("@Language", MySqlDbType.VarChar,50),
 new MySqlParameter("@Country", MySqlDbType.VarChar,100),
 new MySqlParameter("@Province", MySqlDbType.VarChar,100),
 new MySqlParameter("@City", MySqlDbType.VarChar,100),
 new MySqlParameter("@Region", MySqlDbType.VarChar,100),
 new MySqlParameter("@Head_img", MySqlDbType.VarChar,255),
 new MySqlParameter("@Subscribe", MySqlDbType.Int32,10),
 new MySqlParameter("@Remake", MySqlDbType.VarChar,255),
 new MySqlParameter("@Append_Time", MySqlDbType.DateTime,16),
 new MySqlParameter("@Update_Time", MySqlDbType.DateTime,16)

					};
			 parameters[0].Value = model.MemberID;
 parameters[1].Value = model.ServerType;
 parameters[2].Value = model.ServerID;
 parameters[3].Value = model.UserNickname;
 parameters[4].Value = model.OpenID;
 parameters[5].Value = model.UnionID;
 parameters[6].Value = model.SessionKey;
 parameters[7].Value = model.AccessToken;
 parameters[8].Value = model.RefreshToken;
 parameters[9].Value = model.Phone;
 parameters[10].Value = model.Sex;
 parameters[11].Value = model.Language;
 parameters[12].Value = model.Country;
 parameters[13].Value = model.Province;
 parameters[14].Value = model.City;
 parameters[15].Value = model.Region;
 parameters[16].Value = model.HeadImg;
 parameters[17].Value = model.Subscribe;
 parameters[18].Value = model.Remake;
 parameters[19].Value = model.AppendTime;
 parameters[20].Value = model.UpdateTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return 0;
            }
            else
            {
                 model.MemberOtherAuthID = Convert.ToInt32(obj);
                 return model.MemberOtherAuthID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool MemberOtherAuthInfo_Update(MemberOtherAuthInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(MemberOtherAuthInfoTableName);
            strSql.Append(" set ");
			strSql.Append("MemberID=@MemberID,");
strSql.Append("Server_Type=@Server_Type,");
strSql.Append("Server_ID=@Server_ID,");
strSql.Append("User_Nickname=@User_Nickname,");
strSql.Append("OpenID=@OpenID,");
strSql.Append("UnionID=@UnionID,");
strSql.Append("Session_Key=@Session_Key,");
strSql.Append("AccessToken=@AccessToken,");
strSql.Append("RefreshToken=@RefreshToken,");
strSql.Append("Phone=@Phone,");
strSql.Append("Sex=@Sex,");
strSql.Append("Language=@Language,");
strSql.Append("Country=@Country,");
strSql.Append("Province=@Province,");
strSql.Append("City=@City,");
strSql.Append("Region=@Region,");
strSql.Append("Head_img=@Head_img,");
strSql.Append("Subscribe=@Subscribe,");
strSql.Append("Remake=@Remake,");
strSql.Append("Append_Time=@Append_Time,");
strSql.Append("Update_Time=@Update_Time");

            strSql.Append(" where MemberOtherAuthID=@MemberOtherAuthID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@MemberOtherAuthID", MySqlDbType.Int32,10),
 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Server_Type", MySqlDbType.Int32,10),
 new MySqlParameter("@Server_ID", MySqlDbType.Int32,10),
 new MySqlParameter("@User_Nickname", MySqlDbType.VarChar,50),
 new MySqlParameter("@OpenID", MySqlDbType.VarChar,255),
 new MySqlParameter("@UnionID", MySqlDbType.VarChar,255),
 new MySqlParameter("@Session_Key", MySqlDbType.VarChar,255),
 new MySqlParameter("@AccessToken", MySqlDbType.VarChar,255),
 new MySqlParameter("@RefreshToken", MySqlDbType.VarChar,255),
 new MySqlParameter("@Phone", MySqlDbType.VarChar,50),
 new MySqlParameter("@Sex", MySqlDbType.Int32,10),
 new MySqlParameter("@Language", MySqlDbType.VarChar,50),
 new MySqlParameter("@Country", MySqlDbType.VarChar,100),
 new MySqlParameter("@Province", MySqlDbType.VarChar,100),
 new MySqlParameter("@City", MySqlDbType.VarChar,100),
 new MySqlParameter("@Region", MySqlDbType.VarChar,100),
 new MySqlParameter("@Head_img", MySqlDbType.VarChar,255),
 new MySqlParameter("@Subscribe", MySqlDbType.Int32,10),
 new MySqlParameter("@Remake", MySqlDbType.VarChar,255),
 new MySqlParameter("@Append_Time", MySqlDbType.DateTime,16),
 new MySqlParameter("@Update_Time", MySqlDbType.DateTime,16)

			};
			 parameters[0].Value = model.MemberOtherAuthID;
 parameters[1].Value = model.MemberID;
 parameters[2].Value = model.ServerType;
 parameters[3].Value = model.ServerID;
 parameters[4].Value = model.UserNickname;
 parameters[5].Value = model.OpenID;
 parameters[6].Value = model.UnionID;
 parameters[7].Value = model.SessionKey;
 parameters[8].Value = model.AccessToken;
 parameters[9].Value = model.RefreshToken;
 parameters[10].Value = model.Phone;
 parameters[11].Value = model.Sex;
 parameters[12].Value = model.Language;
 parameters[13].Value = model.Country;
 parameters[14].Value = model.Province;
 parameters[15].Value = model.City;
 parameters[16].Value = model.Region;
 parameters[17].Value = model.HeadImg;
 parameters[18].Value = model.Subscribe;
 parameters[19].Value = model.Remake;
 parameters[20].Value = model.AppendTime;
 parameters[21].Value = model.UpdateTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool MemberOtherAuthInfo_Delete(Int32 memberOtherAuthID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(MemberOtherAuthInfoTableName);
            strSql.Append(" where MemberOtherAuthID=@MemberOtherAuthID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@MemberOtherAuthID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = memberOtherAuthID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool MemberOtherAuthInfo_DeleteList(string memberOtherAuthIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(MemberOtherAuthInfoTableName);
            strSql.Append(" where MemberOtherAuthID in (" + memberOtherAuthIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName), CommandType.Text,strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public MemberOtherAuthInfo MemberOtherAuthInfo_GetModel(string memberID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberOtherAuthInfoTableField);
            strSql.Append(" from ");
            strSql.Append(MemberOtherAuthInfoTableName);
            strSql.Append(" where MemberID=@MemberID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@MemberID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = memberID;
            MemberOtherAuthInfo model = new MemberOtherAuthInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MemberOtherAuthInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public MemberOtherAuthInfo MemberOtherAuthInfo_GetModel(Int32 memberOtherAuthID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberOtherAuthInfoTableField);
            strSql.Append(" from ");
            strSql.Append(MemberOtherAuthInfoTableName);
            strSql.Append(" where MemberOtherAuthID=@MemberOtherAuthID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@MemberOtherAuthID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = memberOtherAuthID;
            MemberOtherAuthInfo model = new MemberOtherAuthInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MemberOtherAuthInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public MemberOtherAuthInfo MemberOtherAuthInfo_GetModelByOpenid(string OpenID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberOtherAuthInfoTableField);
            strSql.Append(" from ");
            strSql.Append(MemberOtherAuthInfoTableName);
            strSql.Append(" where OpenID=@OpenID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@OpenID", MySqlDbType.VarChar, 256)
            };
            parameters[0].Value = OpenID;
            MemberAuthInfo model = new MemberAuthInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MemberOtherAuthInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public MemberOtherAuthInfo MemberOtherAuthInfo_DataRowToModel(DataRow row)
        {
            MemberOtherAuthInfo model = new MemberOtherAuthInfo();
            if (row != null)
            {
				if (row["MemberOtherAuthID"] != null )
                {
                    model.MemberOtherAuthID = int.Parse(row["MemberOtherAuthID"].ToString());
                }
				if (row["MemberID"] != null )
                {
					model.MemberID = row["MemberID"].ToString();
                }
				if (row["Server_Type"] != null )
                {
                    model.ServerType = int.Parse(row["Server_Type"].ToString());
                }
				if (row["Server_ID"] != null )
                {
                    model.ServerID = int.Parse(row["Server_ID"].ToString());
                }
				if (row["User_Nickname"] != null )
                {
					model.UserNickname = row["User_Nickname"].ToString();
                }
				if (row["OpenID"] != null )
                {
					model.OpenID = row["OpenID"].ToString();
                }
				if (row["UnionID"] != null )
                {
					model.UnionID = row["UnionID"].ToString();
                }
				if (row["Session_Key"] != null )
                {
					model.SessionKey = row["Session_Key"].ToString();
                }
				if (row["AccessToken"] != null )
                {
					model.AccessToken = row["AccessToken"].ToString();
                }
				if (row["RefreshToken"] != null )
                {
					model.RefreshToken = row["RefreshToken"].ToString();
                }
				if (row["Phone"] != null )
                {
					model.Phone = row["Phone"].ToString();
                }
				if (row["Sex"] != null )
                {
                    model.Sex = int.Parse(row["Sex"].ToString());
                }
				if (row["Language"] != null )
                {
					model.Language = row["Language"].ToString();
                }
				if (row["Country"] != null )
                {
					model.Country = row["Country"].ToString();
                }
				if (row["Province"] != null )
                {
					model.Province = row["Province"].ToString();
                }
				if (row["City"] != null )
                {
					model.City = row["City"].ToString();
                }
				if (row["Region"] != null )
                {
					model.Region = row["Region"].ToString();
                }
				if (row["Head_img"] != null )
                {
					model.HeadImg = row["Head_img"].ToString();
                }
				if (row["Subscribe"] != null )
                {
                    model.Subscribe = int.Parse(row["Subscribe"].ToString());
                }
				if (row["Remake"] != null )
                {
					model.Remake = row["Remake"].ToString();
                }
				if (row["Append_Time"] != null )
                {
					model.AppendTime = DateTime.Parse(row["Append_Time"].ToString());
                }
				if (row["Update_Time"] != null )
                {
					model.UpdateTime = DateTime.Parse(row["Update_Time"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet MemberOtherAuthInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberOtherAuthInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MemberOtherAuthInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet MemberOtherAuthInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberOtherAuthInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MemberOtherAuthInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int MemberOtherAuthInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(MemberOtherAuthInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet MemberOtherAuthInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MemberOtherAuthInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MemberOtherAuthInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable MemberOtherAuthInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "MemberOtherAuthID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_member_other_auth_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MemberOtherAuthInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
