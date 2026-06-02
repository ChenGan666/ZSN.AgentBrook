using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class MessageSendRecordManage : IMessageSendRecordManage
    {
        private string MessageSendRecordConnectionName = "MessageDb";
        private string MessageSendRecordTableName = "tb_msg_send_record";
        private const string MessageSendRecordTableField = "RecordID,ChannelID,SessionID,TaskID,NodeID,MessageType,Content,TargetUser,SendStatus,PlatformMessageId,RetryCount,ErrorMessage,SendTime,CreateTime";
        private const string MessageSendRecordTableFieldForAdd = "RecordID,ChannelID,SessionID,TaskID,NodeID,MessageType,Content,TargetUser,SendStatus,PlatformMessageId,RetryCount,ErrorMessage,SendTime,CreateTime";
        private const string MessageSendRecordTableFieldAltForAdd = "@RecordID,@ChannelID,@SessionID,@TaskID,@NodeID,@MessageType,@Content,@TargetUser,@SendStatus,@PlatformMessageId,@RetryCount,@ErrorMessage,@SendTime,@CreateTime";
        public string SetConnectionName(string connName)
        {
            return MessageSendRecordConnectionName = connName;
        }
        public string MessageSendRecord_Add(MessageSendRecordInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(MessageSendRecordTableName);
            strSql.Append(" (");
            strSql.Append(MessageSendRecordTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(MessageSendRecordTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");

            MySqlParameter[] parameters = {
                new MySqlParameter("@RecordID", MySqlDbType.VarChar,36),
                new MySqlParameter("@ChannelID", MySqlDbType.VarChar,36),
                new MySqlParameter("@SessionID", MySqlDbType.VarChar,36),
                new MySqlParameter("@TaskID", MySqlDbType.VarChar,36),
                new MySqlParameter("@NodeID", MySqlDbType.VarChar,36),
                new MySqlParameter("@MessageType", MySqlDbType.VarChar,64),
                new MySqlParameter("@Content", MySqlDbType.Text),
                new MySqlParameter("@TargetUser", MySqlDbType.VarChar,128),
                new MySqlParameter("@SendStatus", MySqlDbType.Int32,10),
                new MySqlParameter("@PlatformMessageId", MySqlDbType.VarChar,128),
                new MySqlParameter("@RetryCount", MySqlDbType.Int32,10),
                new MySqlParameter("@ErrorMessage", MySqlDbType.Text),
                new MySqlParameter("@SendTime", MySqlDbType.DateTime,16),
                new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
            };
            parameters[0].Value = model.RecordID;
            parameters[1].Value = model.ChannelID;
            parameters[2].Value = model.SessionID;
            parameters[3].Value = model.TaskID;
            parameters[4].Value = model.NodeID;
            parameters[5].Value = model.MessageType;
            parameters[6].Value = model.Content;
            parameters[7].Value = model.TargetUser;
            parameters[8].Value = model.SendStatus;
            parameters[9].Value = model.PlatformMessageId;
            parameters[10].Value = model.RetryCount;
            parameters[11].Value = model.ErrorMessage;
            parameters[12].Value = (object)model.SendTime ?? DBNull.Value;
            parameters[13].Value = model.CreateTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MessageSendRecordConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                return model.RecordID;
            }
        }
        public bool MessageSendRecord_Update(MessageSendRecordInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(MessageSendRecordTableName);
            strSql.Append(" set ");
            strSql.Append("SendStatus=@SendStatus,");
            strSql.Append("PlatformMessageId=@PlatformMessageId,");
            strSql.Append("RetryCount=@RetryCount,");
            strSql.Append("ErrorMessage=@ErrorMessage,");
            strSql.Append("SendTime=@SendTime");
            strSql.Append(" where RecordID=@RecordID");

            MySqlParameter[] parameters = {
                new MySqlParameter("@RecordID", MySqlDbType.VarChar,36),
                new MySqlParameter("@SendStatus", MySqlDbType.Int32,10),
                new MySqlParameter("@PlatformMessageId", MySqlDbType.VarChar,128),
                new MySqlParameter("@RetryCount", MySqlDbType.Int32,10),
                new MySqlParameter("@ErrorMessage", MySqlDbType.Text),
                new MySqlParameter("@SendTime", MySqlDbType.DateTime,16),
            };
            parameters[0].Value = model.RecordID;
            parameters[1].Value = model.SendStatus;
            parameters[2].Value = model.PlatformMessageId;
            parameters[3].Value = model.RetryCount;
            parameters[4].Value = model.ErrorMessage;
            parameters[5].Value = (object)model.SendTime ?? DBNull.Value;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MessageSendRecordConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public MessageSendRecordInfo MessageSendRecord_GetModel(string recordID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MessageSendRecordTableField);
            strSql.Append(" from ");
            strSql.Append(MessageSendRecordTableName);
            strSql.Append(" where RecordID=@RecordID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                new MySqlParameter("@RecordID", MySqlDbType.VarChar, 36)
            };
            parameters[0].Value = recordID;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MessageSendRecordConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MessageSendRecord_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public MessageSendRecordInfo MessageSendRecord_DataRowToModel(DataRow row)
        {
            MessageSendRecordInfo model = new MessageSendRecordInfo();
            if (row != null)
            {
                if (row["RecordID"] != null)
                {
                    model.RecordID = row["RecordID"].ToString();
                }
                if (row["ChannelID"] != null)
                {
                    model.ChannelID = row["ChannelID"].ToString();
                }
                if (row["SessionID"] != null)
                {
                    model.SessionID = row["SessionID"].ToString();
                }
                if (row["TaskID"] != null)
                {
                    model.TaskID = row["TaskID"].ToString();
                }
                if (row["NodeID"] != null)
                {
                    model.NodeID = row["NodeID"].ToString();
                }
                if (row["MessageType"] != null)
                {
                    model.MessageType = row["MessageType"].ToString();
                }
                if (row["Content"] != null)
                {
                    model.Content = row["Content"].ToString();
                }
                if (row["TargetUser"] != null)
                {
                    model.TargetUser = row["TargetUser"].ToString();
                }
                if (row["SendStatus"] != null)
                {
                    model.SendStatus = int.Parse(row["SendStatus"].ToString());
                }
                if (row["PlatformMessageId"] != null)
                {
                    model.PlatformMessageId = row["PlatformMessageId"].ToString();
                }
                if (row["RetryCount"] != null)
                {
                    model.RetryCount = int.Parse(row["RetryCount"].ToString());
                }
                if (row["ErrorMessage"] != null)
                {
                    model.ErrorMessage = row["ErrorMessage"].ToString();
                }
                if (row["SendTime"] != null && row["SendTime"].ToString() != "")
                {
                    model.SendTime = DateTime.Parse(row["SendTime"].ToString());
                }
                if (row["CreateTime"] != null)
                {
                    model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
            }
            return model;
        }
        public DataSet MessageSendRecord_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MessageSendRecordTableField);
            strSql.Append(" FROM ");
            strSql.Append(MessageSendRecordTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MessageSendRecordConnectionName), CommandType.Text, strSql.ToString());
        }
        public DataTable MessageSendRecord_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime")
        {
            MySqlParameter[] parameters = {
                new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                new MySqlParameter("@pageNo", MySqlDbType.Int32),
                new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_msg_send_record";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MessageSendRecordConnectionName), CommandType.StoredProcedure, "CommonPagenation", parameters);
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
