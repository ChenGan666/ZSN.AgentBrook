using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using NPOI.SS.UserModel;
namespace ZSN.AI.DAL.MySql
{
    public partial class BaseDictionaryInfoManage : IBaseDictionaryInfoManage
    {
        ///表链接
        private string BaseDictionaryInfoConnectionName = "BaseDb";
        ///表名称
        private string BaseDictionaryInfoTableName = "base_dictionary_info";
        ///表字段
        private const string BaseDictionaryInfoTableField = "DicId,DicName,DicTitle,DicValue,DicRemark,Remark,Status,Sort,Pid,Cid,CreateTime,UpdateTime,Icon";
        ///添加用表字段
        private const string BaseDictionaryInfoTableFieldForAdd = "DicName,DicTitle,DicValue,DicRemark,Remark,Status,Sort,Pid,Cid,CreateTime,UpdateTime,Icon";
        ///添加用表字段value
        private const string BaseDictionaryInfoTableFieldAltForAdd = "@DicName,@DicTitle,@DicValue,@DicRemark,@Remark,@Status,@Sort,@Pid,@Cid,@CreateTime,@UpdateTime,@Icon";
        public string SetConnectionName(string connName)
        {
            return BaseDictionaryInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public Int32 BaseDictionaryInfo_Add(BaseDictionaryInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(BaseDictionaryInfoTableName);
			strSql.Append(" (");
            strSql.Append(BaseDictionaryInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(BaseDictionaryInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@DicName", MySqlDbType.VarChar,255),
 new MySqlParameter("@DicTitle", MySqlDbType.VarChar,255),
 new MySqlParameter("@DicValue", MySqlDbType.VarChar,255),
 new MySqlParameter("@DicRemark", MySqlDbType.Text),
 new MySqlParameter("@Remark", MySqlDbType.VarChar,512),
 new MySqlParameter("@Status", MySqlDbType.Int32,10),
 new MySqlParameter("@Sort", MySqlDbType.Int32,10),
 new MySqlParameter("@Pid", MySqlDbType.Int32,10),
 new MySqlParameter("@Cid", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),

 new MySqlParameter("@Icon", MySqlDbType.VarChar,255),

                    };
			 parameters[0].Value = model.DicName;
 parameters[1].Value = model.DicTitle;
 parameters[2].Value = model.DicValue;
 parameters[3].Value = model.DicRemark;
 parameters[4].Value = model.Remark;
 parameters[5].Value = model.Status;
 parameters[6].Value = model.Sort;
 parameters[7].Value = model.Pid;
 parameters[8].Value = model.Cid;
 parameters[9].Value = model.CreateTime;
 parameters[10].Value = model.UpdateTime;
            parameters[11].Value = model.Icon;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return 0;
            }
            else
            {
                 model.DicId = Convert.ToInt32(obj);
                 return model.DicId;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool BaseDictionaryInfo_Update(BaseDictionaryInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(BaseDictionaryInfoTableName);
            strSql.Append(" set ");
			strSql.Append("DicName=@DicName,");
strSql.Append("DicTitle=@DicTitle,");
strSql.Append("DicValue=@DicValue,");
strSql.Append("DicRemark=@DicRemark,");
strSql.Append("Remark=@Remark,");
strSql.Append("Status=@Status,");
strSql.Append("Sort=@Sort,");
strSql.Append("Pid=@Pid,");
strSql.Append("Cid=@Cid,");
            strSql.Append("Icon=@Icon,");
            strSql.Append("CreateTime=@CreateTime,");
strSql.Append("UpdateTime=@UpdateTime");

            strSql.Append(" where DicId=@DicId");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@DicId", MySqlDbType.Int32,10),
 new MySqlParameter("@DicName", MySqlDbType.VarChar,255),
 new MySqlParameter("@DicTitle", MySqlDbType.VarChar,255),
 new MySqlParameter("@DicValue", MySqlDbType.VarChar,255),
 new MySqlParameter("@DicRemark", MySqlDbType.Text),
 new MySqlParameter("@Remark", MySqlDbType.VarChar,512),
 new MySqlParameter("@Status", MySqlDbType.Int32,10),
 new MySqlParameter("@Sort", MySqlDbType.Int32,10),
 new MySqlParameter("@Pid", MySqlDbType.Int32,10),
 new MySqlParameter("@Cid", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@Icon", MySqlDbType.VarChar,512),

            };
			 parameters[0].Value = model.DicId;
 parameters[1].Value = model.DicName;
 parameters[2].Value = model.DicTitle;
 parameters[3].Value = model.DicValue;
 parameters[4].Value = model.DicRemark;
 parameters[5].Value = model.Remark;
 parameters[6].Value = model.Status;
 parameters[7].Value = model.Sort;
 parameters[8].Value = model.Pid;
 parameters[9].Value = model.Cid;
 parameters[10].Value = model.CreateTime;
 parameters[11].Value = model.UpdateTime;
            parameters[12].Value = model.Icon;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool BaseDictionaryInfo_Delete(Int32 dicId)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(BaseDictionaryInfoTableName);
            strSql.Append(" where DicId=@DicId");
            MySqlParameter[] parameters = {
					new MySqlParameter("@DicId", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = dicId;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool BaseDictionaryInfo_DeleteList(string dicIdlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(BaseDictionaryInfoTableName);
            strSql.Append(" where DicId in (" + dicIdlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public BaseDictionaryInfo BaseDictionaryInfo_GetModel(Int32 dicId)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(BaseDictionaryInfoTableField);
            strSql.Append(" from ");
            strSql.Append(BaseDictionaryInfoTableName);
            strSql.Append(" where DicId=@DicId");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@DicId", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = dicId;
            BaseDictionaryInfo model = new BaseDictionaryInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return BaseDictionaryInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public BaseDictionaryInfo BaseDictionaryInfo_DataRowToModel(DataRow row)
        {
            BaseDictionaryInfo model = new BaseDictionaryInfo();
            if (row != null)
            {
				if (row["DicId"] != null )
                {
                    model.DicId = int.Parse(row["DicId"].ToString());
                }
				if (row["DicName"] != null )
                {
					model.DicName = row["DicName"].ToString();
                }
				if (row["DicTitle"] != null )
                {
					model.DicTitle = row["DicTitle"].ToString();
                }
				if (row["DicValue"] != null )
                {
					model.DicValue = row["DicValue"].ToString();
                }
				if (row["DicRemark"] != null )
                {
					model.DicRemark = row["DicRemark"].ToString();
                }
				if (row["Remark"] != null )
                {
					model.Remark = row["Remark"].ToString();
                }
				if (row["Status"] != null )
                {
                    model.Status = int.Parse(row["Status"].ToString());
                }
				if (row["Sort"] != null )
                {
                    model.Sort = DbConvert.ToInt32Nullable(row["Sort"].ToString());
                }
				if (row["Pid"] != null )
                {
                    model.Pid = DbConvert.ToInt32Nullable(row["Pid"].ToString());
                }
				if (row["Cid"] != null )
                {
                    model.Cid = DbConvert.ToInt32Nullable(row["Cid"].ToString());
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["UpdateTime"] != null )
                {
					model.UpdateTime = DateTime.Parse(row["UpdateTime"].ToString());
                }
                if (row["Icon"] != null)
                {
                    model.Icon = row["Icon"].ToString();
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet BaseDictionaryInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(BaseDictionaryInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(BaseDictionaryInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        public DataSet BaseDictionaryInfo_GetChildList(string name = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(BaseDictionaryInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(BaseDictionaryInfoTableName);

            strSql.Append(" where Pid in(select DicId from "+ BaseDictionaryInfoTableName + " where DicName=@DicName);");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@DicName", MySqlDbType.VarChar, 255)
            };
            parameters[0].Value = name;

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        /// <summary>
        /// 获得子级数据列表,需MySQL 8.0+
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataSet BaseDictionaryInfo_GetAllChildList(string name = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("WITH RECURSIVE RecursiveTree AS ( "+
                            " SELECT * "+
                            " FROM "+ BaseDictionaryInfoTableName + " "+
                            " WHERE DicName = @DicName "+
                            " UNION ALL "+
                            " SELECT child.* "+
                            " FROM "+ BaseDictionaryInfoTableName + " child "+
                            " INNER JOIN RecursiveTree parent ON child.Pid = parent.DicId "+
                            " ) "+
                            " SELECT * FROM RecursiveTree;");
            
            MySqlParameter[] parameters = {
                    new MySqlParameter("@DicName", MySqlDbType.VarChar, 255)
            };
            parameters[0].Value = name;

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        public DataSet BaseDictionaryInfo_GetAllChildList(int DicId = 0)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("WITH RECURSIVE RecursiveTree AS ( " +
                            " SELECT * " +
                            " FROM " + BaseDictionaryInfoTableName + " " +
                            " WHERE DicId = @DicId " +
                            " UNION ALL " +
                            " SELECT child.* " +
                            " FROM " + BaseDictionaryInfoTableName + " child " +
                            " INNER JOIN RecursiveTree parent ON child.Pid = parent.DicId " +
                            " ) " +
                            " SELECT * FROM RecursiveTree;");

            MySqlParameter[] parameters = {
                    new MySqlParameter("@DicId", MySqlDbType.VarChar, 255)
            };
            parameters[0].Value = DicId;

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        /// <summary>
        /// 获得父级数据列表,需MySQL 8.0+
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataSet BaseDictionaryInfo_GetAllParentList(string name = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("WITH RECURSIVE ParentTree AS ( " +
                            " SELECT * " +
                            " FROM "+ BaseDictionaryInfoTableName + " " +
                            " WHERE DicName = @DicName " +
                            " UNION ALL " +
                            " SELECT parent.* " +
                            " FROM "+ BaseDictionaryInfoTableName + " parent " +
                            " INNER JOIN ParentTree parent ON parent.DicId = parent.Pid " +
                            " ) " +
                            " SELECT * FROM ParentTree;");

            MySqlParameter[] parameters = {
                    new MySqlParameter("@DicName", MySqlDbType.VarChar, 255)
            };
            parameters[0].Value = name;

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet BaseDictionaryInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(BaseDictionaryInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(BaseDictionaryInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int BaseDictionaryInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(BaseDictionaryInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet BaseDictionaryInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(BaseDictionaryInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(BaseDictionaryInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable BaseDictionaryInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "DicId")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "base_dictionary_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(BaseDictionaryInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
