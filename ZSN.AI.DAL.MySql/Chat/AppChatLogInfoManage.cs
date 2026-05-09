using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using Newtonsoft.Json;
namespace ZSN.AI.DAL.MySql
{
    public partial class AppChatLogInfoManage : IAppChatLogInfoManage
    {
        ///表链接
        private string AppChatLogInfoConnectionName = "ChatDb";
        ///表名称
        private string AppChatLogInfoTableName = "tb_app_chat_log_info";
        ///表字段
        private const string AppChatLogInfoTableField = "ChatLogID,ChatSessionID,Direction,LargeModelID,Content,CreateTime,LogOrder,AppID,Role";
        ///添加用表字段
        private const string AppChatLogInfoTableFieldForAdd = "ChatLogID,ChatSessionID,Direction,LargeModelID,Content,CreateTime,LogOrder,AppID,Role";
        ///添加用表字段value
        private const string AppChatLogInfoTableFieldAltForAdd = "@ChatLogID,@ChatSessionID,@Direction,@LargeModelID,@Content,@CreateTime,@LogOrder,@AppID,@Role";
        public string SetConnectionName(string connName)
        {
            return AppChatLogInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string AppChatLogInfo_Add(AppChatLogInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(AppChatLogInfoTableName);
			strSql.Append(" (");
            strSql.Append(AppChatLogInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(AppChatLogInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@ChatLogID", MySqlDbType.VarChar,64),
 new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Direction", MySqlDbType.Int32,10),
 new MySqlParameter("@LargeModelID", MySqlDbType.Int32,10),
 new MySqlParameter("@Content", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@LogOrder", MySqlDbType.Int32,10),
 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Role", MySqlDbType.VarChar,50),

                    };
			 parameters[0].Value = model.ChatLogID;
 parameters[1].Value = model.ChatSessionID;
 parameters[2].Value = model.Direction;
 parameters[3].Value = model.LargeModelID;
 parameters[4].Value = JsonConvert.SerializeObject(model.Content);
 parameters[5].Value = model.CreateTime;
            parameters[6].Value = model.LogOrder;
            parameters[7].Value = model.AppID;
            parameters[8].Value = model.Role;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AppChatLogInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.ChatLogID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool AppChatLogInfo_Update(AppChatLogInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(AppChatLogInfoTableName);
            strSql.Append(" set ");
			strSql.Append("ChatSessionID=@ChatSessionID,");
strSql.Append("Direction=@Direction,");
strSql.Append("LargeModelID=@LargeModelID,");
strSql.Append("Content=@Content,");
            strSql.Append("LogOrder=@LogOrder,");
            strSql.Append("AppID=@AppID,");
            strSql.Append("Role=@Role,");
            strSql.Append("CreateTime=@CreateTime");

            strSql.Append(" where ChatLogID=@ChatLogID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@ChatLogID", MySqlDbType.VarChar,64),
 new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Direction", MySqlDbType.Int32,10),
 new MySqlParameter("@LargeModelID", MySqlDbType.Int32,10),
 new MySqlParameter("@Content", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@LogOrder", MySqlDbType.Int32,10),
 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Role", MySqlDbType.VarChar,50),

            };
			 parameters[0].Value = model.ChatLogID;
 parameters[1].Value = model.ChatSessionID;
 parameters[2].Value = model.Direction;
 parameters[3].Value = model.LargeModelID;
 parameters[4].Value = JsonConvert.SerializeObject(model.Content);
 parameters[5].Value = model.CreateTime;
            parameters[6].Value = model.LogOrder;
            parameters[7].Value = model.AppID;
            parameters[8].Value = model.Role;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatLogInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool AppChatLogInfo_Delete(string chatLogID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AppChatLogInfoTableName);
            strSql.Append(" where ChatLogID=@ChatLogID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@ChatLogID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = chatLogID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatLogInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool AppChatLogInfo_DeleteList(string chatLogIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AppChatLogInfoTableName);
            strSql.Append(" where ChatLogID in (" + chatLogIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppChatLogInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public AppChatLogInfo AppChatLogInfo_GetModel(string chatLogID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatLogInfoTableField);
            strSql.Append(" from ");
            strSql.Append(AppChatLogInfoTableName);
            strSql.Append(" where ChatLogID=@ChatLogID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@ChatLogID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = chatLogID;
            AppChatLogInfo model = new AppChatLogInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatLogInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return AppChatLogInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public AppChatLogInfo AppChatLogInfo_DataRowToModel(DataRow row)
        {
            AppChatLogInfo model = new AppChatLogInfo();
            if (row != null)
            {
				if (row["ChatLogID"] != null )
                {
					model.ChatLogID = row["ChatLogID"].ToString();
                }
                if (row["AppID"] != null)
                {
                    model.AppID = row["AppID"].ToString();
                }
                if (row["ChatSessionID"] != null )
                {
					model.ChatSessionID = row["ChatSessionID"].ToString();
                }
				if (row["Direction"] != null )
                {
                    model.Direction = int.Parse(row["Direction"].ToString());
                }
                if (row["Role"] != null)
                {
                    model.Role = row["Role"].ToString();
                }
                if (row["LargeModelID"] != null )
                {
                    model.LargeModelID = DbConvert.ToInt32Nullable(row["LargeModelID"].ToString());
                }
				if (row["Content"] != null )
                {
                    try
                    {
                        model.Content = JsonConvert.DeserializeObject(row["Content"].ToString());
                    }
                    catch {
                        model.Content = row["Content"].ToString();
                    }
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
                if (row["LogOrder"] != null)
                {
                    model.LogOrder = int.Parse(row["LogOrder"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet AppChatLogInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatLogInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatLogInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatLogInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        public DataSet AppChatLogInfo_GetListBySessionID(string AppID, string SessionID = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatLogInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatLogInfoTableName);

            strSql.Append(" where ChatSessionID=@ChatSessionID and AppID=@AppID order by LogOrder asc");
            MySqlParameter[] parameters = {
                    new MySqlParameter("@ChatSessionID", MySqlDbType.VarChar, 64),
                    new MySqlParameter("@AppID", MySqlDbType.VarChar, 64)
            };
            parameters[0].Value = SessionID;
            parameters[1].Value = AppID;
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatLogInfoConnectionName), CommandType.Text, strSql.ToString(), parameters);
        }
        public DataSet AppChatLogInfo_GetList(string strWhere = "",string SessionID="",string MemberID="")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("SELECT ");
            strSql.Append(" c.ChatLogID,c.ChatSessionID,c.Direction,c.LargeModelID,c.Content,c.CreateTime,c.LogOrder,c.AppID,c.Role ");
            strSql.Append(" FROM ");
            strSql.Append(AppChatLogInfoTableName);
            strSql.Append(" c JOIN tb_app_chat_session_info s ON c.ChatSessionID = s.ChatSessionID WHERE 1=1 ");

            if (!string.IsNullOrWhiteSpace(strWhere))
            {
                strSql.Append(" AND " + strWhere);
            }

            if (!string.IsNullOrWhiteSpace(SessionID))
            {
                strSql.Append($" AND c.ChatSessionID='{SessionID}'");
            }

            if (!string.IsNullOrWhiteSpace(MemberID))
            {
                strSql.Append($" AND s.MemberID='{MemberID}'");
            }

            strSql.Append(" ORDER BY c.CreateTime ASC");

            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatLogInfoConnectionName), CommandType.Text, strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet AppChatLogInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatLogInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatLogInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatLogInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int AppChatLogInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(AppChatLogInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AppChatLogInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet AppChatLogInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppChatLogInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppChatLogInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatLogInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable AppChatLogInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "ChatLogID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_app_chat_log_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppChatLogInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
