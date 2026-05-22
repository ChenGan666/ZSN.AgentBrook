using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using JiebaNet.Segmenter.Common;
namespace ZSN.AI.DAL.MySql
{
    public partial class AppChatSessionInfoManage : IAppChatSessionInfoManage
    {
        ///表链接
        private string AppChatSessionInfoConnectionName = "ChatDb";
        ///表名称
        private string AppChatSessionInfoTableName = "tb_app_chat_session_info";
        ///表字段
        private const string AppChatSessionInfoTableField = "ChatSessionID,MemberID,TopicSummary,IsCoCreate,SystemStatus,SessionStatus,CreateTime,AppID";
        ///添加用表字段
        private const string AppChatSessionInfoTableFieldForAdd = "ChatSessionID,MemberID,TopicSummary,IsCoCreate,SystemStatus,SessionStatus,CreateTime,AppID";
        ///添加用表字段value
        private const string AppChatSessionInfoTableFieldAltForAdd = "@ChatSessionID,@MemberID,@TopicSummary,@IsCoCreate,@SystemStatus,@SessionStatus,@CreateTime,@AppID";
        public string SetConnectionName(string connName)
        {
            return AppChatSessionInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string AppChatSessionInfo_Add(AppChatSessionInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(AppChatSessionInfoTableName);
				strSql.Append(" (");
            strSql.Append(AppChatSessionInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(AppChatSessionInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar,64),
	 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
	 new MySqlParameter("@TopicSummary", MySqlDbType.VarChar,255),
	 new MySqlParameter("@IsCoCreate", MySqlDbType.Int32,10),
	 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
	 new MySqlParameter("@SessionStatus", MySqlDbType.Int32,10),
	 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
	 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
            };
			parameters[0].Value = model.ChatSessionID;
			parameters[1].Value = model.MemberID;
			parameters[2].Value = model.TopicSummary;
			parameters[3].Value = model.IsCoCreate;
			parameters[4].Value = model.SystemStatus;
			parameters[5].Value = model.SessionStatus;
			parameters[6].Value = model.CreateTime;
			parameters[7].Value = model.AppID;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.ChatSessionID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool AppChatSessionInfo_Update(AppChatSessionInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(AppChatSessionInfoTableName);
            strSql.Append(" set ");
				strSql.Append("MemberID=@MemberID,");
				strSql.Append("TopicSummary=@TopicSummary,");
				strSql.Append("IsCoCreate=@IsCoCreate,");
				strSql.Append("SystemStatus=@SystemStatus,");
				strSql.Append("SessionStatus=@SessionStatus,");
				strSql.Append("AppID=@AppID,");
				strSql.Append("CreateTime=@CreateTime");

				strSql.Append(" where ChatSessionID=@ChatSessionID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar,64),
	 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
	 new MySqlParameter("@TopicSummary", MySqlDbType.VarChar,255),
	 new MySqlParameter("@IsCoCreate", MySqlDbType.Int32,10),
	 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
	 new MySqlParameter("@SessionStatus", MySqlDbType.Int32,10),
	 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
	 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
            };
			parameters[0].Value = model.ChatSessionID;
			parameters[1].Value = model.MemberID;
			parameters[2].Value = model.TopicSummary;
			parameters[3].Value = model.IsCoCreate;
			parameters[4].Value = model.SystemStatus;
			parameters[5].Value = model.SessionStatus;
			parameters[6].Value = model.CreateTime;
			parameters[7].Value = model.AppID;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool AppChatSessionInfo_Delete(string chatSessionID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AppChatSessionInfoTableName);
            strSql.Append(" where ChatSessionID=@ChatSessionID");
            MySqlParameter[] parameters = {
						new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar, 64)
				};
            parameters[0].Value = chatSessionID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool AppChatSessionInfo_Delete(string chatSessionID, string MemberID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AppChatSessionInfoTableName);
            strSql.Append(" where ChatSessionID=@ChatSessionID and MemberID=@MemberID");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar, 64),
                new MySqlParameter("@MemberID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = chatSessionID;
            parameters[1].Value = MemberID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool AppChatSessionInfo_CleanUp(string MemberID) {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AppChatSessionInfoTableName);
            strSql.Append(" where  MemberID=@MemberID");
            MySqlParameter[] parameters = {
                new MySqlParameter("@MemberID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = MemberID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
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
        public bool AppChatSessionInfo_DeleteList(string chatSessionIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AppChatSessionInfoTableName);
            strSql.Append(" where ChatSessionID in (" + chatSessionIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public AppChatSessionInfo AppChatSessionInfo_GetModel(string chatSessionID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatSessionInfoTableField);
            strSql.Append(" from ");
            strSql.Append(AppChatSessionInfoTableName);
            strSql.Append(" where ChatSessionID=@ChatSessionID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
						new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar, 64)
				};
            parameters[0].Value = chatSessionID;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return AppChatSessionInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public AppChatSessionInfo AppChatSessionInfo_DataRowToModel(DataRow row)
        {
            AppChatSessionInfo model = new AppChatSessionInfo();
            if (row != null)
            {
				if (row["ChatSessionID"] != null )
                {
						model.ChatSessionID = row["ChatSessionID"].ToString();
                }
                if (row["AppID"] != null)
                {
                    model.AppID = row["AppID"].ToString();
                }
                if (row["MemberID"] != null )
                {
						model.MemberID = row["MemberID"].ToString();
                }
				if (row["TopicSummary"] != null )
                {
						model.TopicSummary = row["TopicSummary"].ToString();
                }
				if (row["IsCoCreate"] != null )
                {
                    model.IsCoCreate = int.Parse(row["IsCoCreate"].ToString());
                }
				if (row["SystemStatus"] != null )
                {
                    model.SystemStatus = int.Parse(row["SystemStatus"].ToString());
                }
				if (row["SessionStatus"].IsNotNull())
                {
                    int.TryParse(row["SessionStatus"].ToString(), out int sessionStatus);
                    model.SessionStatus = sessionStatus;
                }
				if (row["CreateTime"] != null )
                {
						model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet AppChatSessionInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatSessionInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatSessionInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet AppChatSessionInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatSessionInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatSessionInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int AppChatSessionInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(AppChatSessionInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet AppChatSessionInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatSessionInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatSessionInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public DataTable AppChatSessionInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "ChatSessionID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_app_chat_session_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
        /// <summary>
        /// 更新会话状态
        /// </summary>
        public bool AppChatSessionInfo_UpdateSessionStatus(string chatSessionID, int sessionStatus)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(AppChatSessionInfoTableName);
            strSql.Append(" set SessionStatus=@SessionStatus where ChatSessionID=@ChatSessionID");
            MySqlParameter[] parameters = {
                new MySqlParameter("@SessionStatus", MySqlDbType.Int32, 10),
                new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = sessionStatus;
            parameters[1].Value = chatSessionID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }
        /// <summary>
        /// 根据会话ID列表批量查询会话状态
        /// </summary>
        public List<AppChatSessionInfo> AppChatSessionInfo_GetSessionStatusList(string sessionIDs)
        {
            var list = new List<AppChatSessionInfo>();
            if (string.IsNullOrWhiteSpace(sessionIDs)) return list;

            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ChatSessionID,SessionStatus,AppID,TopicSummary,MemberID from ");
            strSql.Append(AppChatSessionInfoTableName);
            strSql.Append(" where ChatSessionID in (");
            strSql.Append(sessionIDs);
            strSql.Append(")");

            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatSessionInfoConnectionName), CommandType.Text, strSql.ToString());
            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    var model = new AppChatSessionInfo();
                    if (row["ChatSessionID"] != null)
                        model.ChatSessionID = row["ChatSessionID"].ToString();
                    if (row["SessionStatus"] != null)
                        model.SessionStatus = int.Parse(row["SessionStatus"].ToString());
                    if (row["AppID"] != null)
                        model.AppID = row["AppID"].ToString();
                    if (row["TopicSummary"] != null)
                        model.TopicSummary = row["TopicSummary"].ToString();
                    if (row["MemberID"] != null)
                        model.MemberID = row["MemberID"].ToString();
                    list.Add(model);
                }
            }
            return list;
        }
	}
}
