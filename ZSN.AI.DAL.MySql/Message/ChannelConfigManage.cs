using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class ChannelConfigManage : IChannelConfigManage
    {
        private string ChannelConfigConnectionName = "MessageDb";
        private string ChannelConfigTableName = "tb_msg_channel_config";
        private const string ChannelConfigTableField = "ChannelID,ChannelName,ProviderType,ConfigJson,FlowDirection,TargetAppID,SessionTimeoutMinutes,Enabled,CreateTime,UpdateTime";
        private const string ChannelConfigTableFieldForAdd = "ChannelID,ChannelName,ProviderType,ConfigJson,FlowDirection,TargetAppID,SessionTimeoutMinutes,Enabled,CreateTime,UpdateTime";
        private const string ChannelConfigTableFieldAltForAdd = "@ChannelID,@ChannelName,@ProviderType,@ConfigJson,@FlowDirection,@TargetAppID,@SessionTimeoutMinutes,@Enabled,@CreateTime,@UpdateTime";
        public string SetConnectionName(string connName) { return ChannelConfigConnectionName = connName; }
        public string ChannelConfig_Add(ChannelConfigInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into "); strSql.Append(ChannelConfigTableName);
            strSql.Append(" ("); strSql.Append(ChannelConfigTableFieldForAdd);
            strSql.Append(") values ("); strSql.Append(ChannelConfigTableFieldAltForAdd);
            strSql.Append(");select @@IDENTITY");
            MySqlParameter[] parameters = {
                new MySqlParameter("@ChannelID", MySqlDbType.VarChar,36),
                new MySqlParameter("@ChannelName", MySqlDbType.VarChar,128),
                new MySqlParameter("@ProviderType", MySqlDbType.Int32,10),
                new MySqlParameter("@ConfigJson", MySqlDbType.Text),
                new MySqlParameter("@FlowDirection", MySqlDbType.Int32,10),
                new MySqlParameter("@TargetAppID", MySqlDbType.VarChar,36),
                new MySqlParameter("@SessionTimeoutMinutes", MySqlDbType.Int32,10),
                new MySqlParameter("@Enabled", MySqlDbType.Int32,10),
                new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
                new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
            };
            parameters[0].Value = model.ChannelID; parameters[1].Value = model.ChannelName;
            parameters[2].Value = model.ProviderType; parameters[3].Value = model.ConfigJson;
            parameters[4].Value = model.FlowDirection; parameters[5].Value = model.TargetAppID;
            parameters[6].Value = model.SessionTimeoutMinutes; parameters[7].Value = model.Enabled;
            parameters[8].Value = model.CreateTime; parameters[9].Value = model.UpdateTime;
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(ChannelConfigConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return obj == null ? String.Empty : model.ChannelID;
        }
        public bool ChannelConfig_Update(ChannelConfigInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update "); strSql.Append(ChannelConfigTableName); strSql.Append(" set ");
            strSql.Append("ChannelName=@ChannelName,ProviderType=@ProviderType,ConfigJson=@ConfigJson,FlowDirection=@FlowDirection,TargetAppID=@TargetAppID,SessionTimeoutMinutes=@SessionTimeoutMinutes,Enabled=@Enabled,CreateTime=@CreateTime,UpdateTime=@UpdateTime");
            strSql.Append(" where ChannelID=@ChannelID");
            MySqlParameter[] parameters = {
                new MySqlParameter("@ChannelID", MySqlDbType.VarChar,36),
                new MySqlParameter("@ChannelName", MySqlDbType.VarChar,128),
                new MySqlParameter("@ProviderType", MySqlDbType.Int32,10),
                new MySqlParameter("@ConfigJson", MySqlDbType.Text),
                new MySqlParameter("@FlowDirection", MySqlDbType.Int32,10),
                new MySqlParameter("@TargetAppID", MySqlDbType.VarChar,36),
                new MySqlParameter("@SessionTimeoutMinutes", MySqlDbType.Int32,10),
                new MySqlParameter("@Enabled", MySqlDbType.Int32,10),
                new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
                new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
            };
            parameters[0].Value = model.ChannelID; parameters[1].Value = model.ChannelName;
            parameters[2].Value = model.ProviderType; parameters[3].Value = model.ConfigJson;
            parameters[4].Value = model.FlowDirection; parameters[5].Value = model.TargetAppID;
            parameters[6].Value = model.SessionTimeoutMinutes; parameters[7].Value = model.Enabled;
            parameters[8].Value = model.CreateTime; parameters[9].Value = model.UpdateTime;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ChannelConfigConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }
        public bool ChannelConfig_Delete(string channelID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from "); strSql.Append(ChannelConfigTableName);
            strSql.Append(" where ChannelID=@ChannelID");
            MySqlParameter[] parameters = { new MySqlParameter("@ChannelID", MySqlDbType.VarChar, 36) };
            parameters[0].Value = channelID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(ChannelConfigConnectionName), CommandType.Text, strSql.ToString(), parameters);
            return rows > 0;
        }
        public ChannelConfigInfo ChannelConfig_GetModel(string channelID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select "); strSql.Append(ChannelConfigTableField);
            strSql.Append(" from "); strSql.Append(ChannelConfigTableName);
            strSql.Append(" where ChannelID=@ChannelID"); strSql.Append(" limit 1");
            MySqlParameter[] parameters = { new MySqlParameter("@ChannelID", MySqlDbType.VarChar, 36) };
            parameters[0].Value = channelID;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ChannelConfigConnectionName), CommandType.Text, strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0) return ChannelConfig_DataRowToModel(ds.Tables[0].Rows[0]);
            else return null;
        }
        public ChannelConfigInfo ChannelConfig_DataRowToModel(DataRow row)
        {
            ChannelConfigInfo model = new ChannelConfigInfo();
            if (row != null)
            {
                if (row["ChannelID"] != null) model.ChannelID = row["ChannelID"].ToString();
                if (row["ChannelName"] != null) model.ChannelName = row["ChannelName"].ToString();
                if (row["ProviderType"] != null) model.ProviderType = int.Parse(row["ProviderType"].ToString());
                if (row["ConfigJson"] != null) model.ConfigJson = row["ConfigJson"].ToString();
                if (row["FlowDirection"] != null) model.FlowDirection = int.Parse(row["FlowDirection"].ToString());
                if (row["TargetAppID"] != null) model.TargetAppID = row["TargetAppID"].ToString();
                if (row["SessionTimeoutMinutes"] != null) model.SessionTimeoutMinutes = int.Parse(row["SessionTimeoutMinutes"].ToString());
                if (row["Enabled"] != null)
                {
                    var enabledStr = row["Enabled"].ToString();
                    if (enabledStr.Equals("True", StringComparison.OrdinalIgnoreCase)) model.Enabled = 1;
                    else if (enabledStr.Equals("False", StringComparison.OrdinalIgnoreCase)) model.Enabled = 0;
                    else model.Enabled = int.Parse(enabledStr);
                }
                if (row["CreateTime"] != null) model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                if (row["UpdateTime"] != null) model.UpdateTime = DateTime.Parse(row["UpdateTime"].ToString());
            }
            return model;
        }
        public DataSet ChannelConfig_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select "); strSql.Append(ChannelConfigTableField);
            strSql.Append(" FROM "); strSql.Append(ChannelConfigTableName);
            if (strWhere.Trim() != "") strSql.Append(" where " + strWhere);
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ChannelConfigConnectionName), CommandType.Text, strSql.ToString());
        }
        public DataTable ChannelConfig_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime")
        {
            MySqlParameter[] parameters = {
                new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                new MySqlParameter("@pageNo", MySqlDbType.Int32),
                new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_msg_channel_config"; parameters[1].Value = showName;
            parameters[2].Value = strWhere; parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex; parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ChannelConfigConnectionName), CommandType.StoredProcedure, "CommonPagenation", parameters);
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
