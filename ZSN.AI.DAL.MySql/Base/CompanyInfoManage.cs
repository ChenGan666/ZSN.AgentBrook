using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.DAL.MySql
{
    public partial class CompanyInfoManage : ICompanyInfoManage
    {
        ///表链接
        private string CompanyInfoConnectionName = "BaseDb";
        ///表名称
        private const string CompanyInfoTableName = "tb_company_info";
        ///表字段
        private const string CompanyInfoTableField = "CompanyID,cFullName,cTitle,cIDCode,cCity,cScale,cInfo,cLogo,cAppendTime";
        ///添加用表字段
        private const string CompanyInfoTableFieldForAdd = "cFullName,cTitle,cIDCode,cCity,cScale,cInfo,cLogo,cAppendTime";
        ///添加用表字段value
        private const string CompanyInfoTableFieldAltForAdd = "@cFullName,@cTitle,@cIDCode,@cCity,@cScale,@cInfo,@cLogo,@cAppendTime";
        public string SetConnectionName(string connName)
        {
            return CompanyInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public int CompanyInfo_Add(CompanyInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(CompanyInfoTableName);
			strSql.Append(" (");
            strSql.Append(CompanyInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(CompanyInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@cFullName", MySqlDbType.VarChar,128),
 new MySqlParameter("@cTitle", MySqlDbType.VarChar,128),
 new MySqlParameter("@cIDCode", MySqlDbType.VarChar,50),
 new MySqlParameter("@cCity", MySqlDbType.VarChar,128),
 new MySqlParameter("@cScale", MySqlDbType.VarChar,50),
 new MySqlParameter("@cInfo", MySqlDbType.VarChar,1024),
 new MySqlParameter("@cLogo", MySqlDbType.VarChar,128),
 new MySqlParameter("@cAppendTime", MySqlDbType.DateTime,16)

					};
			 parameters[0].Value = model.CFullName;
 parameters[1].Value = model.CTitle;
 parameters[2].Value = model.CIDCode;
 parameters[3].Value = model.CCity;
 parameters[4].Value = model.CScale;
 parameters[5].Value = model.CInfo;
 parameters[6].Value = model.CLogo;
 parameters[7].Value = model.CAppendTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(CompanyInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool CompanyInfo_Update(CompanyInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(CompanyInfoTableName);
            strSql.Append(" set ");
			strSql.Append("cFullName=@cFullName,");
strSql.Append("cTitle=@cTitle,");
strSql.Append("cIDCode=@cIDCode,");
strSql.Append("cCity=@cCity,");
strSql.Append("cScale=@cScale,");
strSql.Append("cInfo=@cInfo,");
strSql.Append("cLogo=@cLogo,");
strSql.Append("cAppendTime=@cAppendTime");

            strSql.Append(" where CompanyID=@CompanyID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@CompanyID", MySqlDbType.Int32,10),
 new MySqlParameter("@cFullName", MySqlDbType.VarChar,128),
 new MySqlParameter("@cTitle", MySqlDbType.VarChar,128),
 new MySqlParameter("@cIDCode", MySqlDbType.VarChar,50),
 new MySqlParameter("@cCity", MySqlDbType.VarChar,128),
 new MySqlParameter("@cScale", MySqlDbType.VarChar,50),
 new MySqlParameter("@cInfo", MySqlDbType.VarChar,1024),
 new MySqlParameter("@cLogo", MySqlDbType.VarChar,128),
 new MySqlParameter("@cAppendTime", MySqlDbType.DateTime,16)

			};
			 parameters[0].Value = model.CompanyID;
 parameters[1].Value = model.CFullName;
 parameters[2].Value = model.CTitle;
 parameters[3].Value = model.CIDCode;
 parameters[4].Value = model.CCity;
 parameters[5].Value = model.CScale;
 parameters[6].Value = model.CInfo;
 parameters[7].Value = model.CLogo;
 parameters[8].Value = model.CAppendTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(CompanyInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool CompanyInfo_Delete(Int32 companyID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(CompanyInfoTableName);
            strSql.Append(" where CompanyID=@CompanyID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@CompanyID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = companyID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(CompanyInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool CompanyInfo_DeleteList(string companyIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(CompanyInfoTableName);
            strSql.Append(" where CompanyID in (" + companyIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(CompanyInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public CompanyInfo CompanyInfo_GetModel(Int32 companyID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(CompanyInfoTableField);
            strSql.Append(" from ");
            strSql.Append(CompanyInfoTableName);
            strSql.Append(" where CompanyID=@CompanyID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@CompanyID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = companyID;
            CompanyInfo model = new CompanyInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(CompanyInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return CompanyInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public CompanyInfo CompanyInfo_GetModel()
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(CompanyInfoTableField);
            strSql.Append(" from ");
            strSql.Append(CompanyInfoTableName);
            strSql.Append(" limit 1");
           
            CompanyInfo model = new CompanyInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(CompanyInfoConnectionName), CommandType.Text, strSql.ToString());
            if (ds.Tables[0].Rows.Count > 0)
            {
                return CompanyInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public CompanyInfo CompanyInfo_DataRowToModel(DataRow row)
        {
            CompanyInfo model = new CompanyInfo();
            if (row != null)
            {
				if (row["CompanyID"] != null )
                {
                        model.CompanyID = int.Parse(row["CompanyID"].ToString());
                }
				if (row["cFullName"] != null )
                {
					model.CFullName = row["cFullName"].ToString();
                }
				if (row["cTitle"] != null )
                {
					model.CTitle = row["cTitle"].ToString();
                }
				if (row["cIDCode"] != null )
                {
					model.CIDCode = row["cIDCode"].ToString();
                }
				if (row["cCity"] != null )
                {
					model.CCity = row["cCity"].ToString();
                }
				if (row["cScale"] != null )
                {
					model.CScale = row["cScale"].ToString();
                }
				if (row["cInfo"] != null )
                {
					model.CInfo = row["cInfo"].ToString();
                }
				if (row["cLogo"] != null )
                {
					model.CLogo = row["cLogo"].ToString();
                }
				if (row["cAppendTime"] != null )
                {
					model.CAppendTime = DateTime.Parse(row["cAppendTime"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet CompanyInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(CompanyInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(CompanyInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(CompanyInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet CompanyInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(CompanyInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(CompanyInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(CompanyInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int CompanyInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(CompanyInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(CompanyInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet CompanyInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(CompanyInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(CompanyInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(CompanyInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable CompanyInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CompanyID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_company_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(CompanyInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
