using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class WordTemplateInfoManage : IWordTemplateInfoManage
    {
        ///表链接
        private string WordTemplateInfoConnectionName = "ExpandDb";
        ///表名称
        private string WordTemplateInfoTableName = "tb_word_template_Info";
        ///表字段
        private const string WordTemplateInfoTableField = "WordTemplateID,wName,wDescription,FileCode,wLabel,CreateTime,UpdateTime,SystemStatus";
        ///添加用表字段
        private const string WordTemplateInfoTableFieldForAdd = "WordTemplateID,wName,wDescription,FileCode,wLabel,CreateTime,UpdateTime,SystemStatus";
        ///添加用表字段value
        private const string WordTemplateInfoTableFieldAltForAdd = "@WordTemplateID,@wName,@wDescription,@FileCode,@wLabel,@CreateTime,@UpdateTime,@SystemStatus";
        public string SetConnectionName(string connName)
        {
            return WordTemplateInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string WordTemplateInfo_Add(WordTemplateInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(WordTemplateInfoTableName);
			strSql.Append(" (");
            strSql.Append(WordTemplateInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(WordTemplateInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@WordTemplateID", MySqlDbType.VarChar,64),
 new MySqlParameter("@wName", MySqlDbType.VarChar,50),
 new MySqlParameter("@wDescription", MySqlDbType.VarChar,1024),
 new MySqlParameter("@FileCode", MySqlDbType.VarChar,64),
 new MySqlParameter("@wLabel", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10)

					};
			 parameters[0].Value = model.WordTemplateID;
 parameters[1].Value = model.WName;
 parameters[2].Value = model.WDescription;
 parameters[3].Value = model.FileCode;
 parameters[4].Value = model.WLabel;
 parameters[5].Value = model.CreateTime;
 parameters[6].Value = model.UpdateTime;
 parameters[7].Value = model.SystemStatus;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(WordTemplateInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 model.WordTemplateID = obj.ToString();
                 return model.WordTemplateID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool WordTemplateInfo_Update(WordTemplateInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(WordTemplateInfoTableName);
            strSql.Append(" set ");
			strSql.Append("wName=@wName,");
strSql.Append("wDescription=@wDescription,");
strSql.Append("FileCode=@FileCode,");
strSql.Append("wLabel=@wLabel,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("UpdateTime=@UpdateTime,");
strSql.Append("SystemStatus=@SystemStatus");

            strSql.Append(" where WordTemplateID=@WordTemplateID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@WordTemplateID", MySqlDbType.VarChar,64),
 new MySqlParameter("@wName", MySqlDbType.VarChar,50),
 new MySqlParameter("@wDescription", MySqlDbType.VarChar,1024),
 new MySqlParameter("@FileCode", MySqlDbType.VarChar,64),
 new MySqlParameter("@wLabel", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10)

			};
			 parameters[0].Value = model.WordTemplateID;
 parameters[1].Value = model.WName;
 parameters[2].Value = model.WDescription;
 parameters[3].Value = model.FileCode;
 parameters[4].Value = model.WLabel;
 parameters[5].Value = model.CreateTime;
 parameters[6].Value = model.UpdateTime;
 parameters[7].Value = model.SystemStatus;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WordTemplateInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool WordTemplateInfo_Delete(string wordTemplateID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WordTemplateInfoTableName);
            strSql.Append(" where WordTemplateID=@WordTemplateID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@WordTemplateID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = wordTemplateID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WordTemplateInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool WordTemplateInfo_DeleteList(string wordTemplateIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(WordTemplateInfoTableName);
            strSql.Append(" where WordTemplateID in (" + wordTemplateIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(WordTemplateInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public WordTemplateInfo WordTemplateInfo_GetModel(string wordTemplateID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WordTemplateInfoTableField);
            strSql.Append(" from ");
            strSql.Append(WordTemplateInfoTableName);
            strSql.Append(" where WordTemplateID=@WordTemplateID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@WordTemplateID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = wordTemplateID;
            WordTemplateInfo model = new WordTemplateInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WordTemplateInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return WordTemplateInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public WordTemplateInfo WordTemplateInfo_DataRowToModel(DataRow row)
        {
            WordTemplateInfo model = new WordTemplateInfo();
            if (row != null)
            {
				if (row["WordTemplateID"] != null )
                {
					model.WordTemplateID = row["WordTemplateID"].ToString();
                }
				if (row["wName"] != null )
                {
					model.WName = row["wName"].ToString();
                }
				if (row["wDescription"] != null )
                {
					model.WDescription = row["wDescription"].ToString();
                }
				if (row["FileCode"] != null )
                {
					model.FileCode = row["FileCode"].ToString();
                }
				if (row["wLabel"] != null )
                {
					model.WLabel = row["wLabel"].ToString();
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["UpdateTime"] != null )
                {
					model.UpdateTime = DateTime.Parse(row["UpdateTime"].ToString());
                }
				if (row["SystemStatus"] != null )
                {
                    model.SystemStatus = int.Parse(row["SystemStatus"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet WordTemplateInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WordTemplateInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WordTemplateInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WordTemplateInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet WordTemplateInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WordTemplateInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WordTemplateInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WordTemplateInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int WordTemplateInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(WordTemplateInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(WordTemplateInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet WordTemplateInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(WordTemplateInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(WordTemplateInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WordTemplateInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable WordTemplateInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "WordTemplateID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_word_template_Info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(WordTemplateInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
