using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class MessageReceiveRecordManage : IMessageReceiveRecordManage
    {
        private string MessageReceiveRecordConnectionName = "MessageDb";
        private string MessageReceiveRecordTableName = "tb_msg_receive_record";
        private const string MessageReceiveRecordTableField = "RecordID,ChannelID,EventId,ProviderType,FromUser,FromUserName,MessageType,Content,RawPayload,RoutedWorkflowID,RoutedTaskID,RouteStatus,ReceiveTime,CreateTime";
        private const string MessageReceiveRecordTableFieldForAdd = "RecordID,ChannelID,EventId,ProviderType,FromUser,FromUserName,MessageType,Content,RawPayload,RoutedWorkflowID,RoutedTaskID,RouteStatus,ReceiveTime,CreateTime";
        private const string MessageReceiveRecordTableFieldAltForAdd = "@RecordID,@ChannelID,@EventId,@ProviderType,@FromUser,@FromUserName,@MessageType,@Content,@RawPayload,@RoutedWorkflowID,@RoutedTaskID,@RouteStatus,@ReceiveTime,@CreateTime";
        public string SetConnectionName(string connName)
        {
            return MessageReceiveRecordConnectionName = connName;
        }
        public string MessageReceiveRecord_Add(MessageReceiveRecordInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(MessageReceiveRecordTableName);
            strSql.Append(" (");
            strSql.Append(MessageReceiveRecordTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(MessageReceiveRecordTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");

            MySqlParameter[] parameters = {
                new MySqlParameter("@RecordID", MySqlDbType.VarChar,36),
                new MySqlParameter("@ChannelID", MySqlDbType.VarChar,36),
                new MySqlParameter("@EventId", MySqlDbType.VarChar,128),
                new MySqlParameter("@ProviderType", MySqlDbType.VarChar,64),
                new MySqlParameter("@FromUser", MySqlDbType.VarChar,128),
                new MySqlParameter("@FromUserName", MySqlDbType.VarChar,128),
                new MySqlParameter("@MessageType", MySqlDbType.VarChar,64),
                new MySqlParameter("@Content", MySqlDbType.Text),
                new MySqlParameter("@RawPayload", MySqlDbType.Text),
                new MySqlParameter("@RoutedWorkflowID", MySqlDbType.VarChar,36),
                new MySqlParameter("@RoutedTaskID", MySqlDbType.VarChar,36),
                new MySqlParameter("@RouteStatus", MySqlDbType.Int32,10),
                new MySqlParameter("@ReceiveTime", MySqlDbType.DateTime,16),
                new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
            };
            parameters[0].Value = model.RecordID;
            parameters[1].Value = model.ChannelID;
            parameters[2].Value = model.EventId;
            parameters[3].Value = model.ProviderType;
            parameters[4].Value = model.FromUser;
            parameters[5].Value = model.FromUserName;
            parameters[6].Value = model.MessageType;
            parameters[7].Value = model.Content;
            parameters[8].Value = model.RawPayload;
            parameters[9].Value = model.RoutedWorkflowID;
            parameters[10].Value = model.RoutedTaskID;
            parameters[11].Value = model.RouteStatus;
            parameters[12].Value = (object)model.ReceiveTime ?? DBNull.Value;
            parameters[13].Value = model.CreateTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MessageReceiveRecordConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (obj == null) return String.Empty;
            else return model.RecordID;
        }
        public bool MessageReceiveRecord_Update(MessageReceiveRecordInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(MessageReceiveRecordTableName);
            strSql.Append(" set ");
            strSql.Append("RoutedWorkflowID=@RoutedWorkflowID,");
            strSql.Append("RoutedTaskID=@RoutedTaskID,");
            strSql.Append("RouteStatus=@RouteStatus");
            strSql.Append(" where RecordID=@RecordID");

            MySqlParameter[] parameters = {
                new MySqlParameter("@RecordID", MySqlDbType.VarChar,36),
                new MySqlParameter("@RoutedWorkflowID", MySqlDbType.VarChar,36),
                new MySqlParameter("@RoutedTaskID", MySqlDbType.VarChar,36),
                new MySqlParameter("@RouteStatus", MySqlDbType.Int32,10),
            };
            parameters[0].Value = model.RecordID;
            parameters[1].Value = model.RoutedWorkflowID;
            parameters[2].Value = model.RoutedTaskID;
            parameters[3].Value = model.RouteStatus;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MessageReceiveRecordConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }
        public MessageReceiveRecordInfo MessageReceiveRecord_GetByEventId(string eventId)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MessageReceiveRecordTableField);
            strSql.Append(" from ");
            strSql.Append(MessageReceiveRecordTableName);
            strSql.Append(" where EventId=@EventId");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
                new MySqlParameter("@EventId", MySqlDbType.VarChar, 128)
            };
            parameters[0].Value = eventId;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MessageReceiveRecordConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
                return MessageReceiveRecord_DataRowToModel(ds.Tables[0].Rows[0]);
            else
                return null;
        }
        public MessageReceiveRecordInfo MessageReceiveRecord_DataRowToModel(DataRow row)
        {
            MessageReceiveRecordInfo model = new MessageReceiveRecordInfo();
            if (row != null)
            {
                if (row["RecordID"] != null) model.RecordID = row["RecordID"].ToString();
                if (row["ChannelID"] != null) model.ChannelID = row["ChannelID"].ToString();
                if (row["EventId"] != null) model.EventId = row["EventId"].ToString();
                if (row["ProviderType"] != null) model.ProviderType = row["ProviderType"].ToString();
                if (row["FromUser"] != null) model.FromUser = row["FromUser"].ToString();
                if (row["FromUserName"] != null) model.FromUserName = row["FromUserName"].ToString();
                if (row["MessageType"] != null) model.MessageType = row["MessageType"].ToString();
                if (row["Content"] != null) model.Content = row["Content"].ToString();
                if (row["RawPayload"] != null) model.RawPayload = row["RawPayload"].ToString();
                if (row["RoutedWorkflowID"] != null) model.RoutedWorkflowID = row["RoutedWorkflowID"].ToString();
                if (row["RoutedTaskID"] != null) model.RoutedTaskID = row["RoutedTaskID"].ToString();
                if (row["RouteStatus"] != null) model.RouteStatus = int.Parse(row["RouteStatus"].ToString());
                if (row["ReceiveTime"] != null && row["ReceiveTime"].ToString() != "") model.ReceiveTime = DateTime.Parse(row["ReceiveTime"].ToString());
                if (row["CreateTime"] != null) model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
            }
            return model;
        }
        public DataSet MessageReceiveRecord_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MessageReceiveRecordTableField);
            strSql.Append(" FROM ");
            strSql.Append(MessageReceiveRecordTableName);
            if (strWhere.Trim() != "") strSql.Append(" where " + strWhere);
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MessageReceiveRecordConnectionName), CommandType.Text, strSql.ToString());
        }
        public DataTable MessageReceiveRecord_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime")
        {
            MySqlParameter[] parameters = {
                new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                new MySqlParameter("@pageNo", MySqlDbType.Int32),
                new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_msg_receive_record";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MessageReceiveRecordConnectionName), CommandType.StoredProcedure, "CommonPagenation", parameters);
            total = 0;
            if (ds.Tables.Count > 1)
            {
                total = Convert.ToInt32((long)ds.Tables[1].Rows[0][0]);
                pagetotal = (total % pageSize == 0) ? total / pageSize : total / pageSize + 1;
                return ds.Tables[0];
            }
            else { pagetotal = 0; return null; }
        }
    }
}
