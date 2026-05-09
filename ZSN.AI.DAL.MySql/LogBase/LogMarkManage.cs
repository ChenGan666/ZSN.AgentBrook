using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.DAL.MySql
{
    public partial class LogMarkManage : ILogMarkManage
    {

        private const string LogMarkConnectionName = "LogBaseDb";
        private const string LogMarkTableName = "log_mark";
        
        private const string LogMarkTableField = "Id,MarkName,MarkRemarks,ClassId,LevelId,Status,CreateTime,UpdateTime";
        private const string LogMarkTableFieldForAdd = "MarkName,MarkRemarks,ClassId,LevelId,Status,CreateTime,UpdateTime";
        private const string LogMarkTableFieldAltForAdd = "@MarkName,@MarkRemarks,@ClassId,@LevelId,@Status,@CreateTime,@UpdateTime";

        public int LogMark_Add(LogMark model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(LogMarkTableName);
			strSql.Append(" (");
            strSql.Append(LogMarkTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(LogMarkTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@MarkName", MySqlDbType.VarChar,50),
 new MySqlParameter("@MarkRemarks", MySqlDbType.VarChar,255),
 new MySqlParameter("@ClassId", MySqlDbType.Int32,10),
 new MySqlParameter("@LevelId", MySqlDbType.Int32,10),
 new MySqlParameter("@Status", MySqlDbType.Byte,3),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime)

					};
			 parameters[0].Value = model.MarkName;
 parameters[1].Value = model.MarkRemarks;
 parameters[2].Value = model.ClassId;
 parameters[3].Value = model.LevelId;
 parameters[4].Value = model.Status;
 parameters[5].Value = model.CreateTime;
 parameters[6].Value = model.UpdateTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(LogMarkConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(obj);
            }
        }

        public bool LogMark_Update(LogMark model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(LogMarkTableName);
            strSql.Append(" set ");
			strSql.Append("MarkName=@MarkName,");
strSql.Append("MarkRemarks=@MarkRemarks,");
strSql.Append("ClassId=@ClassId,");
strSql.Append("LevelId=@LevelId,");
strSql.Append("Status=@Status,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("UpdateTime=@UpdateTime");

            strSql.Append(" where Id=@Id");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@Id", MySqlDbType.Int32,10),
 new MySqlParameter("@MarkName", MySqlDbType.VarChar,50),
 new MySqlParameter("@MarkRemarks", MySqlDbType.VarChar,255),
 new MySqlParameter("@ClassId", MySqlDbType.Int32,10),
 new MySqlParameter("@LevelId", MySqlDbType.Int32,10),
 new MySqlParameter("@Status", MySqlDbType.Byte,3),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime)

			};
			 parameters[0].Value = model.Id;
 parameters[1].Value = model.MarkName;
 parameters[2].Value = model.MarkRemarks;
 parameters[3].Value = model.ClassId;
 parameters[4].Value = model.LevelId;
 parameters[5].Value = model.Status;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.UpdateTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(LogMarkConnectionName),CommandType.Text,strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool LogMark_Delete(Int32 id)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(LogMarkTableName);
            strSql.Append(" where Id=@Id");
            MySqlParameter[] parameters = {
					new MySqlParameter("@Id", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = id;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(LogMarkConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool LogMark_DeleteList(string idlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(LogMarkTableName);
            strSql.Append(" where Id in (" + idlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(LogMarkConnectionName), CommandType.Text,strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public LogMark LogMark_GetModel(Int32 id)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LogMarkTableField);
            strSql.Append(" from ");
            strSql.Append(LogMarkTableName);
            strSql.Append(" where Id=@Id");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@Id", MySqlDbType.Int32)
			};
            parameters[0].Value = id;
            LogMark model = new LogMark();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LogMarkConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return LogMark_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public LogMark LogMark_DataRowToModel(DataRow row)
        {
            LogMark model = new LogMark();
            if (row != null)
            {
				if (row["Id"] != null )
                {
                        model.Id = int.Parse(row["Id"].ToString());
                }
				if (row["MarkName"] != null )
                {
					model.MarkName = row["MarkName"].ToString();
                }
				if (row["MarkRemarks"] != null )
                {
					model.MarkRemarks = row["MarkRemarks"].ToString();
                }
				if (row["ClassId"] != null )
                {
                        model.ClassId = int.Parse(row["ClassId"].ToString());
                }
				if (row["LevelId"] != null )
                {
                        model.LevelId = int.Parse(row["LevelId"].ToString());
                }
				if (row["Status"] != null )
                {
					model.Status = bool.Parse(row["Status"].ToString());
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

        public DataSet LogMark_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LogMarkTableField);
            strSql.Append(" FROM ");
            strSql.Append(LogMarkTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LogMarkConnectionName), CommandType.Text,strSql.ToString());
        }

        public DataSet LogMark_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LogMarkTableField);
            strSql.Append(" FROM ");
            strSql.Append(LogMarkTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LogMarkConnectionName),CommandType.Text,strSql.ToString());
        }

        public int LogMark_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(LogMarkTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(LogMarkConnectionName),CommandType.Text,strSql.ToString());
            if (obj == null)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(obj);
            }
        }

        public DataSet LogMark_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LogMarkTableField);
            strSql.Append(" FROM ");
            strSql.Append(LogMarkTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LogMarkConnectionName),CommandType.Text,strSql.ToString());
        }

        public DataTable LogMark_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "Id")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "log_mark";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LogMarkConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
