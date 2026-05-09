using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using Lucene.Net.Search;
namespace ZSN.AI.DAL.MySql
{
    public partial class MenuInfoManage : IMenuInfoManage
    {
        ///表链接
        private string MenuInfoConnectionName = "BaseDb";
        ///表名称
        private string MenuInfoTableName = "tb_menu_info";
        ///表字段
        private const string MenuInfoTableField = "ID,ParentID,Url,Title,Params,Ico,Sort,IcoColor,MState";
        ///添加用表字段
        private const string MenuInfoTableFieldForAdd = "ID,ParentID,Url,Title,Params,Ico,Sort,IcoColor,MState";
        ///添加用表字段value
        private const string MenuInfoTableFieldAltForAdd = "@ID,@ParentID,@Url,@Title,@Params,@Ico,@Sort,@IcoColor,@MState";
        public string SetConnectionName(string connName)
        {
            return MenuInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string MenuInfo_Add(MenuInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(MenuInfoTableName);
			strSql.Append(" (");
            strSql.Append(MenuInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(MenuInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@ID", MySqlDbType.VarChar,128),
 new MySqlParameter("@ParentID", MySqlDbType.VarChar,128),
 new MySqlParameter("@Url", MySqlDbType.VarChar,255),
 new MySqlParameter("@Title", MySqlDbType.VarChar,128),
 new MySqlParameter("@Params", MySqlDbType.VarChar,512),
 new MySqlParameter("@Ico", MySqlDbType.VarChar,50),
 new MySqlParameter("@Sort", MySqlDbType.Int32,10),
 new MySqlParameter("@IcoColor", MySqlDbType.VarChar,50),
 new MySqlParameter("@MState", MySqlDbType.Int32,10)

					};
			 parameters[0].Value = model.ID;
 parameters[1].Value = model.ParentID;
 parameters[2].Value = model.Url;
 parameters[3].Value = model.Title;
 parameters[4].Value = model.Params;
 parameters[5].Value = model.Ico;
 parameters[6].Value = model.Sort;
 parameters[7].Value = model.IcoColor;
 parameters[8].Value = model.MState;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MenuInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.ID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool MenuInfo_Update(MenuInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(MenuInfoTableName);
            strSql.Append(" set ");
			strSql.Append("ParentID=@ParentID,");
strSql.Append("Url=@Url,");
strSql.Append("Title=@Title,");
strSql.Append("Params=@Params,");
strSql.Append("Ico=@Ico,");
strSql.Append("Sort=@Sort,");
strSql.Append("IcoColor=@IcoColor,");
strSql.Append("MState=@MState");

            strSql.Append(" where ID=@ID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@ID", MySqlDbType.VarChar,128),
 new MySqlParameter("@ParentID", MySqlDbType.VarChar,128),
 new MySqlParameter("@Url", MySqlDbType.VarChar,255),
 new MySqlParameter("@Title", MySqlDbType.VarChar,128),
 new MySqlParameter("@Params", MySqlDbType.VarChar,512),
 new MySqlParameter("@Ico", MySqlDbType.VarChar,50),
 new MySqlParameter("@Sort", MySqlDbType.Int32,10),
 new MySqlParameter("@IcoColor", MySqlDbType.VarChar,50),
 new MySqlParameter("@MState", MySqlDbType.Int32,10)

			};
			 parameters[0].Value = model.ID;
 parameters[1].Value = model.ParentID;
 parameters[2].Value = model.Url;
 parameters[3].Value = model.Title;
 parameters[4].Value = model.Params;
 parameters[5].Value = model.Ico;
 parameters[6].Value = model.Sort;
 parameters[7].Value = model.IcoColor;
 parameters[8].Value = model.MState;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MenuInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public int UpdateSort(string id, int sort)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(MenuInfoTableName);
            strSql.Append(" set ");
            strSql.Append("Sort=@Sort ");

            strSql.Append(" where ID=@ID");
            MySqlParameter[] parameters = {
                 new MySqlParameter("@ID", MySqlDbType.VarChar,128),
                 new MySqlParameter("@Sort", MySqlDbType.Int32,10)
            };
            parameters[0].Value = id;
            parameters[1].Value = sort;

            return DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MenuInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);

        }
        public int GetMaxSort(string id)
        {
            string sql = "SELECT IFNULL(MAX(Sort),0)+1 FROM tb_menu_info WHERE ParentID=@ParentID";
            MySqlParameter[] parameters = {
                 new MySqlParameter("@ParentID", MySqlDbType.VarChar,128),
            };
            parameters[0].Value = id;
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MenuInfoConnectionName), CommandType.Text, sql, parameters);
            if(obj == null)
            {
                return 0;
            }
            else
            {
                return Int32.Parse(obj.ToString());
            }
        }
            /// <summary>
            /// 删除一条数据
            /// </summary>
            public bool MenuInfo_Delete(string iD)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(MenuInfoTableName);
            strSql.Append(" where ID=@ID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@ID", MySqlDbType.VarChar, 128)
			};
            parameters[0].Value = iD;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MenuInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool MenuInfo_DeleteList(string iDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(MenuInfoTableName);
            strSql.Append(" where ID in (" + iDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MenuInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public MenuInfo MenuInfo_GetModel(string iD)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MenuInfoTableField);
            strSql.Append(" from ");
            strSql.Append(MenuInfoTableName);
            strSql.Append(" where ID=@ID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@ID", MySqlDbType.VarChar, 128)
			};
            parameters[0].Value = iD;
            MenuInfo model = new MenuInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MenuInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MenuInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public MenuInfo MenuInfo_DataRowToModel(DataRow row)
        {
            MenuInfo model = new MenuInfo();
            if (row != null)
            {
				if (row["ID"] != null )
                {
					model.ID = row["ID"].ToString();
                }
				if (row["ParentID"] != null )
                {
					model.ParentID = row["ParentID"].ToString();
                }
				if (row["Url"] != null )
                {
					model.Url = row["Url"].ToString();
                }
				if (row["Title"] != null )
                {
					model.Title = row["Title"].ToString();
                }
				if (row["Params"] != null )
                {
					model.Params = row["Params"].ToString();
                }
				if (row["Ico"] != null )
                {
					model.Ico = row["Ico"].ToString();
                }
				if (row["Sort"] != null )
                {
                    model.Sort = DbConvert.ToInt32Nullable(row["Sort"].ToString());
                }
				if (row["IcoColor"] != null )
                {
					model.IcoColor = row["IcoColor"].ToString();
                }
				if (row["MState"] != null )
                {
                    model.MState = DbConvert.ToInt32Nullable(row["MState"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet MenuInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MenuInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MenuInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by Sort asc");
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MenuInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet MenuInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MenuInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MenuInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MenuInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int MenuInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(MenuInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MenuInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet MenuInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MenuInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(MenuInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MenuInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable MenuInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "ID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_menu_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MenuInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
