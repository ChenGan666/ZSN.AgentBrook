using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class MessageRouteRuleManage : IMessageRouteRuleManage
    {
        ///表链接
        private string MessageRouteRuleConnectionName = "MessageDb";
        ///表名称
        private string MessageRouteRuleTableName = "tb_msg_route_rule";
        ///表字段
        private const string MessageRouteRuleTableField = "RuleID,ChannelID,RuleName,MatchType,MatchCondition,TargetAppID,InputMapping,SessionTimeoutMinutes,EnableAutoReply,AutoReplyContent,Priority,Enabled,CreateTime,UpdateTime";
        ///添加用表字段
        private const string MessageRouteRuleTableFieldForAdd = "RuleID,ChannelID,RuleName,MatchType,MatchCondition,TargetAppID,InputMapping,SessionTimeoutMinutes,EnableAutoReply,AutoReplyContent,Priority,Enabled,CreateTime,UpdateTime";
        ///添加用表字段value
        private const string MessageRouteRuleTableFieldAltForAdd = "@RuleID,@ChannelID,@RuleName,@MatchType,@MatchCondition,@TargetAppID,@InputMapping,@SessionTimeoutMinutes,@EnableAutoReply,@AutoReplyContent,@Priority,@Enabled,@CreateTime,@UpdateTime";
        public string SetConnectionName(string connName)
        {
            return MessageRouteRuleConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string MessageRouteRule_Add(MessageRouteRuleInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(MessageRouteRuleTableName);
				strSql.Append(" (");
            strSql.Append(MessageRouteRuleTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(MessageRouteRuleTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");

            MySqlParameter[] parameters = {
                new MySqlParameter("@RuleID", MySqlDbType.VarChar,36),
                new MySqlParameter("@ChannelID", MySqlDbType.VarChar,36),
                new MySqlParameter("@RuleName", MySqlDbType.VarChar,128),
                new MySqlParameter("@MatchType", MySqlDbType.VarChar,64),
                new MySqlParameter("@MatchCondition", MySqlDbType.Text),
                new MySqlParameter("@TargetAppID", MySqlDbType.VarChar,36),
                new MySqlParameter("@InputMapping", MySqlDbType.Text),
                new MySqlParameter("@SessionTimeoutMinutes", MySqlDbType.Int32,10),
                new MySqlParameter("@EnableAutoReply", MySqlDbType.Int32,10),
                new MySqlParameter("@AutoReplyContent", MySqlDbType.Text),
                new MySqlParameter("@Priority", MySqlDbType.Int32,10),
                new MySqlParameter("@Enabled", MySqlDbType.Int32,10),
                new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
                new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
            };
            parameters[0].Value = model.RuleID;
            parameters[1].Value = model.ChannelID;
            parameters[2].Value = model.RuleName;
            parameters[3].Value = model.MatchType;
            parameters[4].Value = model.MatchCondition;
            parameters[5].Value = model.TargetAppID;
            parameters[6].Value = model.InputMapping;
            parameters[7].Value = model.SessionTimeoutMinutes;
            parameters[8].Value = model.EnableAutoReply;
            parameters[9].Value = model.AutoReplyContent;
            parameters[10].Value = model.Priority;
            parameters[11].Value = model.Enabled;
            parameters[12].Value = model.CreateTime;
            parameters[13].Value = model.UpdateTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(MessageRouteRuleConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.RuleID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool MessageRouteRule_Update(MessageRouteRuleInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(MessageRouteRuleTableName);
            strSql.Append(" set ");
				strSql.Append("ChannelID=@ChannelID,");
				strSql.Append("RuleName=@RuleName,");
				strSql.Append("MatchType=@MatchType,");
				strSql.Append("MatchCondition=@MatchCondition,");
				strSql.Append("TargetAppID=@TargetAppID,");
				strSql.Append("InputMapping=@InputMapping,");
				strSql.Append("SessionTimeoutMinutes=@SessionTimeoutMinutes,");
				strSql.Append("EnableAutoReply=@EnableAutoReply,");
				strSql.Append("AutoReplyContent=@AutoReplyContent,");
				strSql.Append("Priority=@Priority,");
				strSql.Append("Enabled=@Enabled,");
				strSql.Append("CreateTime=@CreateTime,");
				strSql.Append("UpdateTime=@UpdateTime");

				strSql.Append(" where RuleID=@RuleID");

            MySqlParameter[] parameters = {
                new MySqlParameter("@RuleID", MySqlDbType.VarChar,36),
                new MySqlParameter("@ChannelID", MySqlDbType.VarChar,36),
                new MySqlParameter("@RuleName", MySqlDbType.VarChar,128),
                new MySqlParameter("@MatchType", MySqlDbType.VarChar,64),
                new MySqlParameter("@MatchCondition", MySqlDbType.Text),
                new MySqlParameter("@TargetAppID", MySqlDbType.VarChar,36),
                new MySqlParameter("@InputMapping", MySqlDbType.Text),
                new MySqlParameter("@SessionTimeoutMinutes", MySqlDbType.Int32,10),
                new MySqlParameter("@EnableAutoReply", MySqlDbType.Int32,10),
                new MySqlParameter("@AutoReplyContent", MySqlDbType.Text),
                new MySqlParameter("@Priority", MySqlDbType.Int32,10),
                new MySqlParameter("@Enabled", MySqlDbType.Int32,10),
                new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
                new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
            };
            parameters[0].Value = model.RuleID;
            parameters[1].Value = model.ChannelID;
            parameters[2].Value = model.RuleName;
            parameters[3].Value = model.MatchType;
            parameters[4].Value = model.MatchCondition;
            parameters[5].Value = model.TargetAppID;
            parameters[6].Value = model.InputMapping;
            parameters[7].Value = model.SessionTimeoutMinutes;
            parameters[8].Value = model.EnableAutoReply;
            parameters[9].Value = model.AutoReplyContent;
            parameters[10].Value = model.Priority;
            parameters[11].Value = model.Enabled;
            parameters[12].Value = model.CreateTime;
            parameters[13].Value = model.UpdateTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MessageRouteRuleConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool MessageRouteRule_Delete(string ruleID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(MessageRouteRuleTableName);
            strSql.Append(" where RuleID=@RuleID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@RuleID", MySqlDbType.VarChar, 36)
			};
            parameters[0].Value = ruleID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(MessageRouteRuleConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public MessageRouteRuleInfo MessageRouteRule_GetModel(string ruleID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MessageRouteRuleTableField);
            strSql.Append(" from ");
            strSql.Append(MessageRouteRuleTableName);
            strSql.Append(" where RuleID=@RuleID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@RuleID", MySqlDbType.VarChar, 36)
			};
            parameters[0].Value = ruleID;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MessageRouteRuleConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return MessageRouteRule_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public MessageRouteRuleInfo MessageRouteRule_DataRowToModel(DataRow row)
        {
            MessageRouteRuleInfo model = new MessageRouteRuleInfo();
            if (row != null)
            {
				if (row["RuleID"] != null )
                {
						model.RuleID = row["RuleID"].ToString();
                }
				if (row["ChannelID"] != null )
                {
						model.ChannelID = row["ChannelID"].ToString();
                }
				if (row["RuleName"] != null )
                {
						model.RuleName = row["RuleName"].ToString();
                }
				if (row["MatchType"] != null )
                {
						model.MatchType = row["MatchType"].ToString();
                }
				if (row["MatchCondition"] != null )
                {
						model.MatchCondition = row["MatchCondition"].ToString();
                }
				if (row["TargetAppID"] != null )
                {
						model.TargetAppID = row["TargetAppID"].ToString();
                }
				if (row["InputMapping"] != null )
                {
						model.InputMapping = row["InputMapping"].ToString();
                }
				if (row["SessionTimeoutMinutes"] != null )
                {
						model.SessionTimeoutMinutes = int.Parse(row["SessionTimeoutMinutes"].ToString());
                }
				if (row["EnableAutoReply"] != null )
                {
						model.EnableAutoReply = int.Parse(row["EnableAutoReply"].ToString());
                }
				if (row["AutoReplyContent"] != null )
                {
						model.AutoReplyContent = row["AutoReplyContent"].ToString();
                }
				if (row["Priority"] != null )
                {
						model.Priority = int.Parse(row["Priority"].ToString());
                }
				if (row["Enabled"] != null )
                {
						model.Enabled = int.Parse(row["Enabled"].ToString());
                }
				if (row["CreateTime"] != null )
                {
						model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["UpdateTime"] != null )
                {
						model.UpdateTime = DateTime.Parse(row["UpdateTime"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet MessageRouteRule_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(MessageRouteRuleTableField);
            strSql.Append(" FROM ");
            strSql.Append(MessageRouteRuleTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MessageRouteRuleConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public DataTable MessageRouteRule_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "Priority")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_msg_route_rule";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(MessageRouteRuleConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
