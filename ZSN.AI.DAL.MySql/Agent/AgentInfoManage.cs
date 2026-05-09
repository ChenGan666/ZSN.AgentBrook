using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class AgentInfoManage : IAgentInfoManage
    {
        ///表链接
        private string AgentInfoConnectionName = "AgentDb";
        ///表名称
        private string AgentInfoTableName = "tb_agent_info";
        ///表字段
        private const string AgentInfoTableField = "AgentID,MemberID,MemberName,Name,AICON,DicIDList,DicNameList,Description,SystemStatus,CreateTime,LastUpdateTime,SessionModelID,SessionModelName,Prompt,TemperatureCoefficient,TopPCoefficient";
        ///添加用表字段
        private const string AgentInfoTableFieldForAdd = "AgentID,MemberID,MemberName,Name,AICON,DicIDList,DicNameList,Description,SystemStatus,CreateTime,LastUpdateTime,SessionModelID,SessionModelName,Prompt,TemperatureCoefficient,TopPCoefficient";
        ///添加用表字段value
        private const string AgentInfoTableFieldAltForAdd = "@AgentID,@MemberID,@MemberName,@Name,@AICON,@DicIDList,@DicNameList,@Description,@SystemStatus,@CreateTime,@LastUpdateTime,@SessionModelID,@SessionModelName,@Prompt,@TemperatureCoefficient,@TopPCoefficient";
        public string SetConnectionName(string connName)
        {
            return AgentInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string AgentInfo_Add(AgentInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(AgentInfoTableName);
			strSql.Append(" (");
            strSql.Append(AgentInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(AgentInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";");
            /*
            if (model.KnowledgeBases != null)
            {
                foreach (var _KnowledgeBase in model.KnowledgeBases) {
                    strSql.Append(" insert into tb_Agent_Knowledge_Base_Info(AgentID,KnowledgeBaseID,Weight) " +
                        "values(@AgentID,'" + _KnowledgeBase.KnowledgeBaseID + "',"+ _KnowledgeBase.Weight + ");");
                }
            }
            */
            MySqlParameter[] parameters = {
			 new MySqlParameter("@AgentID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Name", MySqlDbType.VarChar,128),
 new MySqlParameter("@DicIDList", MySqlDbType.VarChar,512),
 new MySqlParameter("@DicNameList", MySqlDbType.VarChar,1024),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),

 new MySqlParameter("@SessionModelID", MySqlDbType.Int32,10),

             new MySqlParameter("@SessionModelName", MySqlDbType.VarChar,128),

 //new MySqlParameter("@VectorModelID", MySqlDbType.Int32,10),
 new MySqlParameter("@Prompt", MySqlDbType.VarChar,2048),
 new MySqlParameter("@TemperatureCoefficient", MySqlDbType.Decimal,10),
 new MySqlParameter("@TopPCoefficient", MySqlDbType.Decimal,10),
 
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime,16),
             new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
             new MySqlParameter("@MemberName", MySqlDbType.VarChar,128),
             //new MySqlParameter("@VectorModelName", MySqlDbType.VarChar,128),
             new MySqlParameter("@AICON", MySqlDbType.VarChar,64),

                    };
			 parameters[0].Value = model.AgentID;
                parameters[1].Value = model.Name;
                parameters[2].Value = model.DicIDList;
                parameters[3].Value = model.DicNameList;
                parameters[4].Value = model.Description;

                parameters[5].Value = model.SessionModelID;
                parameters[6].Value = model.SessionModelName;
                parameters[7].Value = model.Prompt;
                parameters[8].Value = model.TemperatureCoefficient;
                parameters[9].Value = model.TopPCoefficient;

                parameters[10].Value = model.SystemStatus;
                parameters[11].Value = model.CreateTime;
                parameters[12].Value = model.LastUpdateTime;
                parameters[13].Value = model.MemberID;
                parameters[14].Value = model.MemberName;
                parameters[15].Value = model.AICON;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AgentInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.AgentID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool AgentInfo_Update(AgentInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(AgentInfoTableName);
            strSql.Append(" set ");
			strSql.Append("Name=@Name,");
            strSql.Append("MemberID=@MemberID,");
            strSql.Append("DicIDList=@DicIDList,");
strSql.Append("DicNameList=@DicNameList,");
strSql.Append("Description=@Description,");

            strSql.Append("SessionModelID=@SessionModelID,");
            strSql.Append("SessionModelName=@SessionModelName,");
            strSql.Append("Prompt=@Prompt,");
            strSql.Append("TemperatureCoefficient=@TemperatureCoefficient,");
            strSql.Append("TopPCoefficient=@TopPCoefficient,");

            strSql.Append("SystemStatus=@SystemStatus,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("LastUpdateTime=@LastUpdateTime,");
            strSql.Append("MemberName=@MemberName,");
            
            strSql.Append("AICON=@AICON ");

            strSql.Append(" where AgentID=@AgentID;");

            /*
            strSql.Append("delete from tb_Agent_Knowledge_Base_Info where AgentID=@AgentID;");

            if (model.KnowledgeBases != null)
            {
                foreach (var _KnowledgeBase in model.KnowledgeBases)
                {
                    strSql.Append(" insert into tb_Agent_Knowledge_Base_Info(AgentID,KnowledgeBaseID,Weight) " +
                        "values(@AgentID,'" + _KnowledgeBase.KnowledgeBaseID + "'," + _KnowledgeBase.Weight + ");");
                }
            }
            */

            MySqlParameter[] parameters = {
				 new MySqlParameter("@AgentID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Name", MySqlDbType.VarChar,128),
 new MySqlParameter("@DicIDList", MySqlDbType.VarChar,512),
 new MySqlParameter("@DicNameList", MySqlDbType.VarChar,1024),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),

    new MySqlParameter("@SessionModelID", MySqlDbType.Int32,10),
    new MySqlParameter("@SessionModelName", MySqlDbType.VarChar,128),
    new MySqlParameter("@Prompt", MySqlDbType.VarChar,2048),
    new MySqlParameter("@TemperatureCoefficient", MySqlDbType.Decimal,10),
    new MySqlParameter("@TopPCoefficient", MySqlDbType.Decimal,10),

 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime,16),
                 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@MemberName", MySqlDbType.VarChar,128),
 

                 new MySqlParameter("@AICON", MySqlDbType.VarChar,64),

            };
			 parameters[0].Value = model.AgentID;
 parameters[1].Value = model.Name;
 parameters[2].Value = model.DicIDList;
 parameters[3].Value = model.DicNameList;
 parameters[4].Value = model.Description;

            parameters[5].Value = model.SessionModelID;
            parameters[6].Value = model.SessionModelName;
            parameters[7].Value = model.Prompt;
            parameters[8].Value = model.TemperatureCoefficient;
            parameters[9].Value = model.TopPCoefficient;

            parameters[10].Value = model.SystemStatus;
            parameters[11].Value = model.CreateTime;
            parameters[12].Value = model.LastUpdateTime;
            parameters[13].Value = model.MemberID;
            parameters[14].Value = model.MemberName;
            parameters[15].Value = model.AICON;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AgentInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool AgentInfo_Delete(string AgentID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AgentInfoTableName);
            strSql.Append(" where AgentID=@AgentID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@AgentID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = AgentID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AgentInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool AgentInfo_DeleteList(string AgentIDlist)
        {
            if (AgentIDlist.IndexOf(",") > -1)
            {
                string[] items = AgentIDlist.Split(',');
                AgentIDlist = String.Join("','", items.Select(item => item.Trim()));
            }
            AgentIDlist = "'" + AgentIDlist + "'";

            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AgentInfoTableName);
            strSql.Append(" where AgentID in (" + AgentIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AgentInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public AgentInfo AgentInfo_GetModel(string AgentID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AgentInfoTableField);
            strSql.Append(" from ");
            strSql.Append(AgentInfoTableName);
            strSql.Append(" where AgentID=@AgentID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@AgentID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = AgentID;
            AgentInfo model = new AgentInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AgentInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return AgentInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public AgentInfo AgentInfo_DataRowToModel(DataRow row)
        {
            AgentInfo model = new AgentInfo();
            if (row != null)
            {
                if (row["AgentID"] != null)
                {
                    model.AgentID = row["AgentID"].ToString();
                }
                if (row["MemberID"] != null)
                {
                    model.MemberID = row["MemberID"].ToString();
                }
                if (row["MemberName"] != null)
                {
                    model.MemberName = row["MemberName"].ToString();
                }
                if (row["Name"] != null)
                {
                    model.Name = row["Name"].ToString();
                }
                if (row["AICON"] != null)
                {
                    model.AICON = row["AICON"].ToString();
                }
                if (row["DicIDList"] != null)
                {
                    model.DicIDList = row["DicIDList"].ToString();
                }
                if (row["DicNameList"] != null)
                {
                    model.DicNameList = row["DicNameList"].ToString();
                }
                if (row["Description"] != null)
                {
                    model.Description = row["Description"].ToString();
                }

                if (row["SessionModelID"] != null)
                {
                    model.SessionModelID = int.Parse(row["SessionModelID"].ToString());
                }
                if (row["SessionModelName"] != null)
                {
                    model.SessionModelName = row["SessionModelName"].ToString();
                }
                if (row["Prompt"] != null)
                {
                    model.Prompt = row["Prompt"].ToString();
                }

                if (row["TemperatureCoefficient"] != null)
                {
                    model.TemperatureCoefficient = double.Parse(row["TemperatureCoefficient"].ToString());
                }
                if (row["TopPCoefficient"] != null)
                {
                    model.TopPCoefficient = double.Parse(row["TopPCoefficient"].ToString());
                }

                if (row["SystemStatus"] != null)
                {
                    model.SystemStatus = (AgentStatus) int.Parse(row["SystemStatus"].ToString());
                }
                if (row["CreateTime"] != null)
                {
                    model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
                if (row["LastUpdateTime"] != null)
                {
                    model.LastUpdateTime = DateTime.Parse(row["LastUpdateTime"].ToString());
                }

            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet AgentInfo_GetList(string strWhere = "")
        {

            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AgentInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AgentInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AgentInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet AgentInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AgentInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AgentInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AgentInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int AgentInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(AgentInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AgentInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet AgentInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AgentInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AgentInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AgentInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable AgentInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_agent_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AgentInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
