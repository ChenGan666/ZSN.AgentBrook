using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
namespace ZSN.AI.DAL.MySql
{
    public partial class AppInfoManage : IAppInfoManage
    {
        ///表链接
        private string AppInfoConnectionName = "AppDb";
        ///表名称
        private string AppInfoTableName = "tb_app_info";
        ///表字段
        private const string AppInfoTableField = "AppID,MemberID,MemberName,Name,AICON,DicIDList,DicNameList,Description,SystemStatus,CreateTime,LastUpdateTime,Prompt,SessionModelID,SessionModelName,TemperatureCoefficient,TopPCoefficient";
        ///添加用表字段
        private const string AppInfoTableFieldForAdd = "AppID,MemberID,MemberName,Name,AICON,DicIDList,DicNameList,Description,SystemStatus,CreateTime,LastUpdateTime,Prompt,SessionModelID,SessionModelName,TemperatureCoefficient,TopPCoefficient";
        ///添加用表字段value
        private const string AppInfoTableFieldAltForAdd = "@AppID,@MemberID,@MemberName,@Name,@AICON,@DicIDList,@DicNameList,@Description,@SystemStatus,@CreateTime,@LastUpdateTime,@Prompt,@SessionModelID,@SessionModelName,@TemperatureCoefficient,@TopPCoefficient";
        public string SetConnectionName(string connName)
        {
            return AppInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string AppInfo_Add(AppInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(AppInfoTableName);
			strSql.Append(" (");
            strSql.Append(AppInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(AppInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");


            MySqlParameter[] parameters = {
			 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Name", MySqlDbType.VarChar,128),
 new MySqlParameter("@DicIDList", MySqlDbType.VarChar,512),
 new MySqlParameter("@DicNameList", MySqlDbType.VarChar,1024),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime,16),
             new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
             new MySqlParameter("@MemberName", MySqlDbType.VarChar,128),
             new MySqlParameter("@AICON", MySqlDbType.VarChar,64),
             new MySqlParameter("@Prompt",MySqlDbType.Text),
             new MySqlParameter("@SessionModelID", MySqlDbType.Int32,10),
             new MySqlParameter("@SessionModelName", MySqlDbType.VarChar,128),
 new MySqlParameter("@TemperatureCoefficient", MySqlDbType.Decimal,10),
 new MySqlParameter("@TopPCoefficient", MySqlDbType.Decimal,10),
                    };
			 parameters[0].Value = model.AppID;
 parameters[1].Value = model.Name;
 parameters[2].Value = model.DicIDList;
 parameters[3].Value = model.DicNameList;
 parameters[4].Value = model.Description;
 parameters[5].Value = model.SystemStatus;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.LastUpdateTime;
            parameters[8].Value = model.MemberID;
            parameters[9].Value = model.MemberName;
            parameters[10].Value = model.AICON;
            parameters[11].Value = model.Prompt;
            parameters[12].Value = model.SessionModelID;
            parameters[13].Value = model.SessionModelName;
            parameters[14].Value = model.TemperatureCoefficient;
            parameters[15].Value = model.TopPCoefficient;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AppInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.AppID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool AppInfo_Update(AppInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(AppInfoTableName);
            strSql.Append(" set ");
			strSql.Append("Name=@Name,");
            strSql.Append("MemberID=@MemberID,");
            strSql.Append("DicIDList=@DicIDList,");
strSql.Append("DicNameList=@DicNameList,");
strSql.Append("Description=@Description,");

            strSql.Append("Prompt=@Prompt,");
            strSql.Append("SystemStatus=@SystemStatus,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("LastUpdateTime=@LastUpdateTime,");
            strSql.Append("MemberName=@MemberName,");

            strSql.Append("SessionModelID=@SessionModelID,");
            strSql.Append("SessionModelName=@SessionModelName,");
            strSql.Append("TemperatureCoefficient=@TemperatureCoefficient,");
            strSql.Append("TopPCoefficient=@TopPCoefficient,");

            strSql.Append("AICON=@AICON ");

            strSql.Append(" where AppID=@AppID;");


            MySqlParameter[] parameters = {
				 new MySqlParameter("@AppID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Name", MySqlDbType.VarChar,128),
 new MySqlParameter("@DicIDList", MySqlDbType.VarChar,512),
 new MySqlParameter("@DicNameList", MySqlDbType.VarChar,1024),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime,16),
                 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@MemberName", MySqlDbType.VarChar,128),
                 new MySqlParameter("@AICON", MySqlDbType.VarChar,64),
             new MySqlParameter("@Prompt",MySqlDbType.Text),

             new MySqlParameter("@SessionModelID", MySqlDbType.Int32,10),
             new MySqlParameter("@SessionModelName", MySqlDbType.VarChar,128),
 new MySqlParameter("@TemperatureCoefficient", MySqlDbType.Decimal,10),
 new MySqlParameter("@TopPCoefficient", MySqlDbType.Decimal,10),
            };
			 parameters[0].Value = model.AppID;
 parameters[1].Value = model.Name;
 parameters[2].Value = model.DicIDList;
 parameters[3].Value = model.DicNameList;
 parameters[4].Value = model.Description;
 parameters[5].Value = model.SystemStatus;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.LastUpdateTime;
            parameters[8].Value = model.MemberID;
            parameters[9].Value = model.MemberName;
            parameters[10].Value = model.AICON;
            parameters[11].Value = model.Prompt;

            parameters[12].Value = model.SessionModelID;
            parameters[13].Value = model.SessionModelName;
            parameters[14].Value = model.TemperatureCoefficient;
            parameters[15].Value = model.TopPCoefficient;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool AppInfo_Delete(string appID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AppInfoTableName);
            strSql.Append(" where AppID=@AppID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@AppID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = appID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool AppInfo_DeleteList(string appIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(AppInfoTableName);
            strSql.Append(" where AppID in (" + appIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(AppInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public AppInfo AppInfo_GetModel(string appID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppInfoTableField);
            strSql.Append(" from ");
            strSql.Append(AppInfoTableName);
            strSql.Append(" where AppID=@AppID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@AppID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = appID;
            AppInfo model = new AppInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return AppInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public AppInfo AppInfo_DataRowToModel(DataRow row)
        {
            AppInfo model = new AppInfo();
            if (row != null)
            {
				if (row["AppID"] != null )
                {
					model.AppID = row["AppID"].ToString();
                }
                if (row["MemberID"] != null)
                {
                    model.MemberID = row["MemberID"].ToString();
                }
                if (row["MemberName"] != null)
                {
                    model.MemberName = row["MemberName"].ToString();
                }
                if (row["Name"] != null )
                {
					model.Name = row["Name"].ToString();
                }
                if (row["AICON"] != null)
                {
                    model.AICON = row["AICON"].ToString();
                }
                if (row["DicIDList"] != null )
                {
					model.DicIDList = row["DicIDList"].ToString();
                }
				if (row["DicNameList"] != null )
                {
					model.DicNameList = row["DicNameList"].ToString();
                }
				if (row["Description"] != null )
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
                    model.TemperatureCoefficient = int.Parse(row["TemperatureCoefficient"].ToString());
                }
                if (row["TopPCoefficient"] != null)
                {
                    model.TopPCoefficient = int.Parse(row["TopPCoefficient"].ToString());
                }

                if (row["SystemStatus"] != null )
                {
                    model.SystemStatus = (ZSN.AI.Entity.AppStatus) int.Parse(row["SystemStatus"].ToString());
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["LastUpdateTime"] != null )
                {
					model.LastUpdateTime = DateTime.Parse(row["LastUpdateTime"].ToString());
                }

            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet AppInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet AppInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int AppInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(AppInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(AppInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet AppInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(AppInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(AppInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable AppInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_app_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(AppInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
