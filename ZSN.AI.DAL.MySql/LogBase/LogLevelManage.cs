using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.DAL.MySql
{
    public partial class LogLevelManage : ILogLevelManage
    {
        ///������
        private const string LogLevelConnectionName = "LogBaseDb";
        ///������
        private const string LogLevelTableName = "log_level";
        ///���ֶ�
        private const string LogLevelTableField = "Id,LevelName,LevelRemarks,Status,CreateTime,UpdateTime";
        ///�����ñ��ֶ�
        private const string LogLevelTableFieldForAdd = "LevelName,LevelRemarks,Status,CreateTime,UpdateTime";
        ///�����ñ��ֶ�value
        private const string LogLevelTableFieldAltForAdd = "@LevelName,@LevelRemarks,@Status,@CreateTime,@UpdateTime";
		/// <summary>
        /// ����һ������
        /// </summary>
        public int LogLevel_Add(LogLevel model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(LogLevelTableName);
			strSql.Append(" (");
            strSql.Append(LogLevelTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(LogLevelTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@LevelName", MySqlDbType.VarChar,50),
 new MySqlParameter("@LevelRemarks", MySqlDbType.VarChar,255),
 new MySqlParameter("@Status", MySqlDbType.Byte,3),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime)

					};
			 parameters[0].Value = model.LevelName;
 parameters[1].Value = model.LevelRemarks;
 parameters[2].Value = model.Status;
 parameters[3].Value = model.CreateTime;
 parameters[4].Value = model.UpdateTime;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(LogLevelConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        /// ����һ������
        /// </summary>
        public bool LogLevel_Update(LogLevel model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(LogLevelTableName);
            strSql.Append(" set ");
			strSql.Append("LevelName=@LevelName,");
strSql.Append("LevelRemarks=@LevelRemarks,");
strSql.Append("Status=@Status,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("UpdateTime=@UpdateTime");

            strSql.Append(" where Id=@Id");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@Id", MySqlDbType.Int32,10),
 new MySqlParameter("@LevelName", MySqlDbType.VarChar,50),
 new MySqlParameter("@LevelRemarks", MySqlDbType.VarChar,255),
 new MySqlParameter("@Status", MySqlDbType.Byte,3),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime)

			};
			 parameters[0].Value = model.Id;
 parameters[1].Value = model.LevelName;
 parameters[2].Value = model.LevelRemarks;
 parameters[3].Value = model.Status;
 parameters[4].Value = model.CreateTime;
 parameters[5].Value = model.UpdateTime;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(LogLevelConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        /// ɾ��һ������
        /// </summary>
        public bool LogLevel_Delete(Int32 id)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(LogLevelTableName);
            strSql.Append(" where Id=@Id");
            MySqlParameter[] parameters = {
					new MySqlParameter("@Id", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = id;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(LogLevelConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        /// ����ɾ������
        /// </summary>
        public bool LogLevel_DeleteList(string idlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(LogLevelTableName);
            strSql.Append(" where Id in (" + idlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(LogLevelConnectionName), CommandType.Text,strSql.ToString());
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
        /// �õ�һ������ʵ��
        /// </summary>
        public LogLevel LogLevel_GetModel(Int32 id)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LogLevelTableField);
            strSql.Append(" from ");
            strSql.Append(LogLevelTableName);
            strSql.Append(" where Id=@Id");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@Id", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = id;
            LogLevel model = new LogLevel();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LogLevelConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return LogLevel_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// �õ�һ������ʵ��
        /// </summary>
        public LogLevel LogLevel_DataRowToModel(DataRow row)
        {
            LogLevel model = new LogLevel();
            if (row != null)
            {
				if (row["Id"] != null )
                {
                        model.Id = int.Parse(row["Id"].ToString());
                }
				if (row["LevelName"] != null )
                {
					model.LevelName = row["LevelName"].ToString();
                }
				if (row["LevelRemarks"] != null )
                {
					model.LevelRemarks = row["LevelRemarks"].ToString();
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
        /// <summary>
        /// ��������б�
        /// </summary>
        public DataSet LogLevel_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LogLevelTableField);
            strSql.Append(" FROM ");
            strSql.Append(LogLevelTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LogLevelConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// ���ǰ��������
        /// </summary>
        public DataSet LogLevel_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LogLevelTableField);
            strSql.Append(" FROM ");
            strSql.Append(LogLevelTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LogLevelConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// ��ȡ��¼����
        /// </summary>
        public int LogLevel_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(LogLevelTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(LogLevelConnectionName),CommandType.Text,strSql.ToString());
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
        /// ��ҳ��ȡ�����б�
        /// </summary>
        public DataSet LogLevel_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LogLevelTableField);
            strSql.Append(" FROM ");
            strSql.Append(LogLevelTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LogLevelConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// ��ҳ��ȡ�����б�
        /// </summary>
        /// <param name="pageSize">ÿҳ��С</param>
        /// <param name="pageIndex">ҳ��</param>
        /// <param name="strWhere">��ѯ����</param>
        /// <param name="pagetotal">��ҳ��</param>
        /// <param name="total">����</param>
        /// <param name="orderType">������� Ĭ�Ͻ���1����0����</param>
        /// <param name="showName">��ʾ�ֶΣ�Ĭ��ȫ��</param>
        /// <param name="orderKey">����key��Ĭ������</param>
        public DataTable LogLevel_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "Id")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "log_level";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LogLevelConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
