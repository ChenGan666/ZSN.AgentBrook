using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using ZSN.AI.DAL.Agent;
namespace ZSN.AI.DAL.MySql
{
    public partial class AgentKnowledgeBaseInfoManage : IAgentKnowledgeBaseInfoManage
    {
        ///表链接
        private string AgentKnowledgeBaseInfoConnectionName = "AgentDb";
        ///表名称
        private string AgentKnowledgeBaseInfoTableName = "tb_agent_knowledge_base_info";
        ///表字段
        private const string AgentKnowledgeBaseInfoTableField = "AgentKnowledgeBaseID,AgentID,KnowledgeBaseID,Weight";
        ///添加用表字段
        private const string AgentKnowledgeBaseInfoTableFieldForAdd = "AgentID,KnowledgeBaseID,Weight";
        ///添加用表字段value
        private const string AgentKnowledgeBaseInfoTableFieldAltForAdd = "@AgentID,@KnowledgeBaseID,@Weight";
        public string SetConnectionName(string connName)
        {
            return AgentKnowledgeBaseInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public int AgentKnowledgeBaseInfo_Add(AgentKnowledgeBaseInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(AgentKnowledgeBaseInfoTableName);
			strSql.Append(" (");
            strSql.Append(AgentKnowledgeBaseInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(AgentKnowledgeBaseInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@AgentID", MySqlDbType.VarChar,64),
 new MySqlParameter("@KnowledgeBaseID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Weight", MySqlDbType.Int32,10)

					};
			 parameters[0].Value = model.AgentID;
 parameters[1].Value = model.KnowledgeBaseID;
 parameters[2].Value = model.Weight;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AgentKnowledgeBaseInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return 0;
            }
            else
            {
                 model.AgentKnowledgeBaseID = Convert.ToInt32(obj);
                 return model.AgentKnowledgeBaseID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool AgentKnowledgeBaseInfo_Update(AgentKnowledgeBaseInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(AgentKnowledgeBaseInfoTableName);
            strSql.Append(" set ");
			strSql.Append("AppID=@AppID,");
strSql.Append("KnowledgeBaseID=@KnowledgeBaseID,");
strSql.Append("Weight=@Weight");

            strSql.Append(" where AgentKnowledgeBaseID=@AgentKnowledgeBaseID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@AgentKnowledgeBaseID", MySqlDbType.Int32,10),
 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
 new MySqlParameter("@KnowledgeBaseID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Weight", MySqlDbType.Int32,10)

			};
			 parameters[0].Value = model.AgentKnowledgeBaseID;
 parameters[1].Value = model.AgentID;
 parameters[2].Value = model.KnowledgeBaseID;
 parameters[3].Value = model.Weight;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AgentKnowledgeBaseInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool AgentKnowledgeBaseInfo_Delete(Int32 AgentKnowledgeBaseID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AgentKnowledgeBaseInfoTableName);
            strSql.Append(" where AgentKnowledgeBaseID=@AgentKnowledgeBaseID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@AgentKnowledgeBaseID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = AgentKnowledgeBaseID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AgentKnowledgeBaseInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool AgentKnowledgeBaseInfo_DeleteList(string AgentKnowledgeBaseIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AgentKnowledgeBaseInfoTableName);
            strSql.Append(" where AgentKnowledgeBaseID in (" + AgentKnowledgeBaseIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AgentKnowledgeBaseInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public AgentKnowledgeBaseInfo AgentKnowledgeBaseInfo_GetModel(Int32 AgentKnowledgeBaseID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AgentKnowledgeBaseInfoTableField);
            strSql.Append(" from ");
            strSql.Append(AgentKnowledgeBaseInfoTableName);
            strSql.Append(" where AgentKnowledgeBaseID=@AgentKnowledgeBaseID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@AgentKnowledgeBaseID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = AgentKnowledgeBaseID;
            AgentKnowledgeBaseInfo model = new AgentKnowledgeBaseInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AgentKnowledgeBaseInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return AgentKnowledgeBaseInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public AgentKnowledgeBaseInfo AgentKnowledgeBaseInfo_DataRowToModel(DataRow row)
        {
            AgentKnowledgeBaseInfo model = new AgentKnowledgeBaseInfo();
            if (row != null)
            {
				if (row["AgentKnowledgeBaseID"] != null )
                {
                    model.AgentKnowledgeBaseID = int.Parse(row["AgentKnowledgeBaseID"].ToString());
                }
				if (row["AgentID"] != null )
                {
					model.AgentID = row["AgentID"].ToString();
                }
				if (row["KnowledgeBaseID"] != null )
                {
					model.KnowledgeBaseID = row["KnowledgeBaseID"].ToString();
                }
				if (row["Weight"] != null )
                {
                    model.Weight = int.Parse(row["Weight"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet AgentKnowledgeBaseInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AgentKnowledgeBaseInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AgentKnowledgeBaseInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AgentKnowledgeBaseInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet AgentKnowledgeBaseInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AgentKnowledgeBaseInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AgentKnowledgeBaseInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AgentKnowledgeBaseInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int AgentKnowledgeBaseInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(AgentKnowledgeBaseInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AgentKnowledgeBaseInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet AgentKnowledgeBaseInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AgentKnowledgeBaseInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AgentKnowledgeBaseInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AgentKnowledgeBaseInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable AgentKnowledgeBaseInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "AgentKnowledgeBaseID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_app_knowledge_base_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AgentKnowledgeBaseInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
