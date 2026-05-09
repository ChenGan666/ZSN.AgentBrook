using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class FilesInfoManage : IFilesInfoManage
    {
        ///表链接
        private string FilesInfoConnectionName = "ObjectDb";
        ///表名称
        private string FilesInfoTableName = "tb_files_info";
        ///表字段
        private const string FilesInfoTableField = "FileCode,fFilePath,fName,fOriginName,fType,fAppendTime";
        ///添加用表字段
        private const string FilesInfoTableFieldForAdd = "FileCode,fFilePath,fName,fOriginName,fType,fAppendTime";
        ///添加用表字段value
        private const string FilesInfoTableFieldAltForAdd = "@FileCode,@fFilePath,@fName,@fOriginName,@fType,@fAppendTime";
        public string SetConnectionName(string connName)
        {
            return FilesInfoConnectionName = connName;
        }
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public int FilesInfo_Add(FilesInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(FilesInfoTableName);
            strSql.Append(" (");
            strSql.Append(FilesInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(FilesInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select ROW_COUNT()");
            MySqlParameter[] parameters = {
             new MySqlParameter("@FileCode", MySqlDbType.VarChar,64),
 new MySqlParameter("@fFilePath", MySqlDbType.VarChar,128),
 new MySqlParameter("@fName", MySqlDbType.VarChar,128),
 new MySqlParameter("@fOriginName", MySqlDbType.VarChar,128),
 new MySqlParameter("@fType", MySqlDbType.VarChar,128),
 new MySqlParameter("@fAppendTime", MySqlDbType.DateTime,16)

                    };
            parameters[0].Value = model.FileCode;
            parameters[1].Value = model.FFilePath;
            parameters[2].Value = model.FName;
            parameters[3].Value = model.FOriginName;
            parameters[4].Value = model.FType;
            parameters[5].Value = model.FAppendTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(FilesInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
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
        public bool FilesInfo_Update(FilesInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(FilesInfoTableName);
            strSql.Append(" set ");
			strSql.Append("fFilePath=@fFilePath,");
strSql.Append("fName=@fName,");
strSql.Append("fOriginName=@fOriginName,");
strSql.Append("fType=@fType,");
strSql.Append("fAppendTime=@fAppendTime");

            strSql.Append(" where FileCode=@FileCode");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@FileCode", MySqlDbType.VarChar,64),
 new MySqlParameter("@fFilePath", MySqlDbType.VarChar,128),
 new MySqlParameter("@fName", MySqlDbType.VarChar,128),
 new MySqlParameter("@fOriginName", MySqlDbType.VarChar,128),
 new MySqlParameter("@fType", MySqlDbType.VarChar,128),
 new MySqlParameter("@fAppendTime", MySqlDbType.DateTime,16)

			};
			 parameters[0].Value = model.FileCode;
 parameters[1].Value = model.FFilePath;
 parameters[2].Value = model.FName;
 parameters[3].Value = model.FOriginName;
 parameters[4].Value = model.FType;
 parameters[5].Value = model.FAppendTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(FilesInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool FilesInfo_Delete(string FileCode)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(FilesInfoTableName);
            strSql.Append(" where FileCode=@FileCode");
            MySqlParameter[] parameters = {
					new MySqlParameter("@FileCode", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = FileCode;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(FilesInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool FilesInfo_DeleteList(string FileCodelist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(FilesInfoTableName);
            strSql.Append(" where FileCode in (" + FileCodelist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(FilesInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public FilesInfo FilesInfo_GetModel(string FileCode)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(FilesInfoTableField);
            strSql.Append(" from ");
            strSql.Append(FilesInfoTableName);
            strSql.Append(" where FileCode=@FileCode");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@FileCode", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = FileCode;
            FilesInfo model = new FilesInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(FilesInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return FilesInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public FilesInfo FilesInfo_DataRowToModel(DataRow row)
        {
            FilesInfo model = new FilesInfo();
            if (row != null)
            {
				if (row["FileCode"] != null )
                {
					model.FileCode = row["FileCode"].ToString();
                }
				if (row["fFilePath"] != null )
                {
					model.FFilePath = row["fFilePath"].ToString();
                }
				if (row["fName"] != null )
                {
					model.FName = row["fName"].ToString();
                }
				if (row["fOriginName"] != null )
                {
					model.FOriginName = row["fOriginName"].ToString();
                }
				if (row["fType"] != null )
                {
					model.FType = row["fType"].ToString();
                }
				if (row["fAppendTime"] != null )
                {
					model.FAppendTime = DateTime.Parse(row["fAppendTime"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet FilesInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(FilesInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(FilesInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(FilesInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet FilesInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(FilesInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(FilesInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(FilesInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int FilesInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(FilesInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(FilesInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet FilesInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(FilesInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(FilesInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(FilesInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable FilesInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "FileCode")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_files_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(FilesInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
