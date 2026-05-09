using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.DAL.MySql
{
    public partial class KnowledgeBaseTagInfoManage : IKnowledgeBaseTagInfoManage
    {
        ///表链接
        private string KnowledgeBaseTagInfoConnectionName = "ModelDb";
        ///表名称
        private const string KnowledgeBaseTagInfoTableName = "tb_knowledge_base_tag_info";
        ///表字段
        private const string KnowledgeBaseTagInfoTableField = "KnowledgeBaseTagID,TagClassID,TagClassName,Tag,tAppendTime,tCount,tSummary";
        ///添加用表字段
        private const string KnowledgeBaseTagInfoTableFieldForAdd = "TagClassID,TagClassName,Tag,tAppendTime,tCount,tSummary";
        ///添加用表字段value
        private const string KnowledgeBaseTagInfoTableFieldAltForAdd = "@TagClassID,@TagClassName,@Tag,@tAppendTime,@tCount,@tSummary";
        public string SetConnectionName(string connName)
        {
            return KnowledgeBaseTagInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public int KnowledgeBaseTagInfo_Add(KnowledgeBaseTagInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(KnowledgeBaseTagInfoTableName);
			strSql.Append(" (");
            strSql.Append(KnowledgeBaseTagInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(KnowledgeBaseTagInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@TagClassID", MySqlDbType.Int32,10),
 new MySqlParameter("@TagClassName", MySqlDbType.VarChar,50),
 new MySqlParameter("@Tag", MySqlDbType.VarChar,50),
 new MySqlParameter("@tAppendTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@tCount", MySqlDbType.Int32,10),
 new MySqlParameter("@tSummary", MySqlDbType.VarChar,1024)

					};
			 parameters[0].Value = model.TagClassID;
            parameters[1].Value = model.TagClassName;
            parameters[2].Value = model.Tag;
 parameters[3].Value = model.TAppendTime;
 parameters[4].Value = model.TCount;
 parameters[5].Value = model.TSummary;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool KnowledgeBaseTagInfo_Update(KnowledgeBaseTagInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(KnowledgeBaseTagInfoTableName);
            strSql.Append(" set ");
			strSql.Append("TagClassID=@TagClassID,");
            strSql.Append("TagClassName=@TagClassName,");
            strSql.Append("Tag=@Tag,");
strSql.Append("tAppendTime=@tAppendTime,");
strSql.Append("tCount=@tCount,");
strSql.Append("tSummary=@tSummary");

            strSql.Append(" where KnowledgeBaseTagID=@KnowledgeBaseTagID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@KnowledgeBaseTagID", MySqlDbType.Int32,10),
 new MySqlParameter("@TagClassID", MySqlDbType.Int32,10),
 new MySqlParameter("@TagClassName", MySqlDbType.VarChar,50),
 new MySqlParameter("@Tag", MySqlDbType.VarChar,50),
 new MySqlParameter("@tAppendTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@tCount", MySqlDbType.Int32,10),
 new MySqlParameter("@tSummary", MySqlDbType.VarChar,1024)

			};
			 parameters[0].Value = model.KnowledgeBaseTagID;
 parameters[1].Value = model.TagClassID;
            parameters[2].Value = model.TagClassName;
            parameters[3].Value = model.Tag;
 parameters[4].Value = model.TAppendTime;
 parameters[5].Value = model.TCount;
 parameters[6].Value = model.TSummary;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool KnowledgeBaseTagInfo_Delete(Int32 knowledgeBaseTagID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(KnowledgeBaseTagInfoTableName);
            strSql.Append(" where KnowledgeBaseTagID=@KnowledgeBaseTagID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@KnowledgeBaseTagID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = knowledgeBaseTagID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool KnowledgeBaseTagInfo_DeleteList(string knowledgeBaseTagIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(KnowledgeBaseTagInfoTableName);
            strSql.Append(" where KnowledgeBaseTagID in (" + knowledgeBaseTagIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public KnowledgeBaseTagInfo KnowledgeBaseTagInfo_GetModel(Int32 knowledgeBaseTagID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseTagInfoTableField);
            strSql.Append(" from ");
            strSql.Append(KnowledgeBaseTagInfoTableName);
            strSql.Append(" where KnowledgeBaseTagID=@KnowledgeBaseTagID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@KnowledgeBaseTagID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = knowledgeBaseTagID;
            KnowledgeBaseTagInfo model = new KnowledgeBaseTagInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return KnowledgeBaseTagInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public KnowledgeBaseTagInfo KnowledgeBaseTagInfo_GetModel(string Tag)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseTagInfoTableField);
            strSql.Append(" from ");
            strSql.Append(KnowledgeBaseTagInfoTableName);
            strSql.Append(" where Tag=@Tag");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@Tag", MySqlDbType.VarChar, 50)
            };
            parameters[0].Value = Tag;
            KnowledgeBaseTagInfo model = new KnowledgeBaseTagInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return KnowledgeBaseTagInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public KnowledgeBaseTagInfo KnowledgeBaseTagInfo_DataRowToModel(DataRow row)
        {
            KnowledgeBaseTagInfo model = new KnowledgeBaseTagInfo();
            if (row != null)
            {
				if (row["KnowledgeBaseTagID"] != null )
                {
                        model.KnowledgeBaseTagID = int.Parse(row["KnowledgeBaseTagID"].ToString());
                }
				if (row["TagClassID"] != null )
                {
                        model.TagClassID = int.Parse(row["TagClassID"].ToString());
                }
                if (row["TagClassName"] != null)
                {
                    model.TagClassName = row["TagClassName"].ToString();
                }
                if (row["Tag"] != null )
                {
					model.Tag = row["Tag"].ToString();
                }
				if (row["tAppendTime"] != null )
                {
					model.TAppendTime = DateTime.Parse(row["tAppendTime"].ToString());
                }
				if (row["tCount"] != null )
                {
                        model.TCount = int.Parse(row["tCount"].ToString());
                }
				if (row["tSummary"] != null )
                {
					model.TSummary = row["tSummary"].ToString();
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet KnowledgeBaseTagInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseTagInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(KnowledgeBaseTagInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet KnowledgeBaseTagInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseTagInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(KnowledgeBaseTagInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int KnowledgeBaseTagInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(KnowledgeBaseTagInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet KnowledgeBaseTagInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseTagInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(KnowledgeBaseTagInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable KnowledgeBaseTagInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "KnowledgeBaseTagID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_knowledge_base_tag_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseTagInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
