using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class AppChatSummaryInfoManage : IAppChatSummaryInfoManage
    {
        ///表链接
        private string AppChatSummaryInfoConnectionName = "ChatDb";
        ///表名称
        private string AppChatSummaryInfoTableName = "tb_app_chat_summary_info";
        ///表字段
        private const string AppChatSummaryInfoTableField = "SummaryID,AppID,ChatSessionID,Content,CreateTime,ChatLogIDList";
        ///添加用表字段
        private const string AppChatSummaryInfoTableFieldForAdd = "SummaryID,AppID,ChatSessionID,Content,CreateTime,ChatLogIDList";
        ///添加用表字段value
        private const string AppChatSummaryInfoTableFieldAltForAdd = "@SummaryID,@AppID,@ChatSessionID,@Content,@CreateTime,@ChatLogIDList";
        public string SetConnectionName(string connName)
        {
            return AppChatSummaryInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string AppChatSummaryInfo_Add(AppChatSummaryInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(AppChatSummaryInfoTableName);
			strSql.Append(" (");
            strSql.Append(AppChatSummaryInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(AppChatSummaryInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@SummaryID", MySqlDbType.VarChar,64),
 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
 new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Content", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@ChatLogIDList", MySqlDbType.JSON)

					};
			 parameters[0].Value = model.SummaryID;
 parameters[1].Value = model.AppID;
 parameters[2].Value = model.ChatSessionID;
 parameters[3].Value = model.Content;
 parameters[4].Value = model.CreateTime;
 parameters[5].Value = model.ChatLogIDList;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.SummaryID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool AppChatSummaryInfo_Update(AppChatSummaryInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(AppChatSummaryInfoTableName);
            strSql.Append(" set ");
			strSql.Append("AppID=@AppID,");
strSql.Append("ChatSessionID=@ChatSessionID,");
strSql.Append("Content=@Content,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("ChatLogIDList=@ChatLogIDList");

            strSql.Append(" where SummaryID=@SummaryID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@SummaryID", MySqlDbType.VarChar,64),
 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
 new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Content", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@ChatLogIDList", MySqlDbType.JSON)

			};
			 parameters[0].Value = model.SummaryID;
 parameters[1].Value = model.AppID;
 parameters[2].Value = model.ChatSessionID;
 parameters[3].Value = model.Content;
 parameters[4].Value = model.CreateTime;
 parameters[5].Value = model.ChatLogIDList;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool AppChatSummaryInfo_Delete(string summaryID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AppChatSummaryInfoTableName);
            strSql.Append(" where SummaryID=@SummaryID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@SummaryID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = summaryID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool AppChatSummaryInfo_DeleteList(string summaryIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AppChatSummaryInfoTableName);
            strSql.Append(" where SummaryID in (" + summaryIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public AppChatSummaryInfo AppChatSummaryInfo_GetModel(string summaryID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatSummaryInfoTableField);
            strSql.Append(" from ");
            strSql.Append(AppChatSummaryInfoTableName);
            strSql.Append(" where SummaryID=@SummaryID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@SummaryID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = summaryID;
            AppChatSummaryInfo model = new AppChatSummaryInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return AppChatSummaryInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public AppChatSummaryInfo AppChatSummaryInfo_DataRowToModel(DataRow row)
        {
            AppChatSummaryInfo model = new AppChatSummaryInfo();
            if (row != null)
            {
				if (row["SummaryID"] != null )
                {
					model.SummaryID = row["SummaryID"].ToString();
                }
				if (row["AppID"] != null )
                {
					model.AppID = row["AppID"].ToString();
                }
				if (row["ChatSessionID"] != null )
                {
					model.ChatSessionID = row["ChatSessionID"].ToString();
                }
				if (row["Content"] != null )
                {
					model.Content = row["Content"].ToString();
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["ChatLogIDList"] != null )
                {
					model.ChatLogIDList = row["ChatLogIDList"].ToString();
                }
            }
            return model;
        }

        public DataSet AppChatSummaryInfo_GetListBySessionID(string AppID, string SessionID = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatSummaryInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatSummaryInfoTableName);

            strSql.Append(" where ChatSessionID=@ChatSessionID and AppID=@AppID order by CreateTime asc");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar, 64),
                    new MySqlParameter("@AppID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = SessionID;
            parameters[1].Value = AppID;
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet AppChatSummaryInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatSummaryInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatSummaryInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        public DataSet AppChatSummaryInfo_GetList(string strWhere = "",string SessionID = "", string MemberID = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatSummaryInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatSummaryInfoTableName);
            strSql.Append(" where 1=1 ");
            if (strWhere.Trim() != "")
            {
                strSql.Append(" and " + strWhere);
            }
            if (SessionID.Trim() != "")
            {
                strSql.Append($" and ChatSessionID='{SessionID}'");
            }
            if (MemberID.Trim() != "")
            {
                strSql.Append($" and ChatSessionID=(select ChatSessionID from tb_app_chat_session_info where MemberID='{MemberID}')");
            }
            strSql.Append(" order by CreateTime asc");
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName), CommandType.Text, strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet AppChatSummaryInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatSummaryInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatSummaryInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int AppChatSummaryInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(AppChatSummaryInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet AppChatSummaryInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatSummaryInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatSummaryInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable AppChatSummaryInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "SummaryID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_app_chat_summary_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSummaryInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
