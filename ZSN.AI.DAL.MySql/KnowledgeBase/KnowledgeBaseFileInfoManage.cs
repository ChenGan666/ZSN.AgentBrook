using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using ZSN.AI.Entity.Model.Enum;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL.MySql
{
    public partial class KnowledgeBaseFileInfoManage : IKnowledgeBaseFileInfoManage
    {
        ///表链接
        private string KnowledgeBaseFileInfoConnectionName = "ModelDb";
        ///表名称
        private string KnowledgeBaseFileInfoTableName = "tb_knowledge_base_file_info";
        ///表字段
        private const string KnowledgeBaseFileInfoTableField = "FileID,FileName,FilePath,Type,ParserConfig,CreateTime,SystemStatus,KnowledgeBaseID,DataCount";
        ///添加用表字段
        private const string KnowledgeBaseFileInfoTableFieldForAdd = "FileID,FileName,FilePath,Type,ParserConfig,CreateTime,SystemStatus,KnowledgeBaseID,DataCount";
        ///添加用表字段value
        private const string KnowledgeBaseFileInfoTableFieldAltForAdd = "@FileID,@FileName,@FilePath,@Type,@ParserConfig,@CreateTime,@SystemStatus,@KnowledgeBaseID,@DataCount";
        public string SetConnectionName(string connName)
        {
            return KnowledgeBaseFileInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string KnowledgeBaseFileInfo_Add(KnowledgeBaseFileInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(KnowledgeBaseFileInfoTableName);
			strSql.Append(" (");
            strSql.Append(KnowledgeBaseFileInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(KnowledgeBaseFileInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@FileID", MySqlDbType.VarChar,64),
             new MySqlParameter("@KnowledgeBaseID", MySqlDbType.VarChar,64),
 new MySqlParameter("@FileName", MySqlDbType.VarChar,256),
 new MySqlParameter("@FilePath", MySqlDbType.VarChar,512),
 new MySqlParameter("@Type", MySqlDbType.VarChar,128),
 new MySqlParameter("@ParserConfig", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32),
 new MySqlParameter("@DataCount", MySqlDbType.Int32)

                    };
			 parameters[0].Value = model.FileID;
            parameters[1].Value = model.KnowledgeBaseID;
            parameters[2].Value = model.FileName;
 parameters[3].Value = model.FilePath;
 parameters[4].Value = model.Type;
 parameters[5].Value = model.ParserConfig;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.SystemStatus;
            parameters[8].Value = model.DataCount;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(KnowledgeBaseFileInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 model.FileID = obj.ToString();
                 return model.FileID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool KnowledgeBaseFileInfo_Update(KnowledgeBaseFileInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(KnowledgeBaseFileInfoTableName);
            strSql.Append(" set ");
			strSql.Append("FileName=@FileName,");
strSql.Append("FilePath=@FilePath,");
strSql.Append("Type=@Type,");
strSql.Append("ParserConfig=@ParserConfig,");
strSql.Append("CreateTime=@CreateTime,");
            strSql.Append("KnowledgeBaseID=@KnowledgeBaseID,");
            strSql.Append("DataCount=@DataCount,");
            strSql.Append("SystemStatus=@SystemStatus");

            strSql.Append(" where FileID=@FileID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@FileID", MySqlDbType.VarChar,64),
                 new MySqlParameter("@KnowledgeBaseID", MySqlDbType.VarChar,64),
 new MySqlParameter("@FileName", MySqlDbType.VarChar,256),
 new MySqlParameter("@FilePath", MySqlDbType.VarChar,512),
 new MySqlParameter("@Type", MySqlDbType.VarChar,128),
 new MySqlParameter("@ParserConfig", MySqlDbType.JSON),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32),
 new MySqlParameter("@DataCount", MySqlDbType.Int32)

            };
			 parameters[0].Value = model.FileID;
            parameters[1].Value = model.KnowledgeBaseID;
            parameters[2].Value = model.FileName;
 parameters[3].Value = model.FilePath;
 parameters[4].Value = model.Type;
 parameters[5].Value = model.ParserConfig;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.SystemStatus;
            parameters[8].Value = model.DataCount;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(KnowledgeBaseFileInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool KnowledgeBaseFileInfo_Delete(string fileID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(KnowledgeBaseFileInfoTableName);
            strSql.Append(" where FileID=@FileID;");
            strSql.Append("delete from tb_knowledge_base_file_chunk_info where FileID=@FileID  ;");
            MySqlParameter[] parameters = {
					new MySqlParameter("@FileID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = fileID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(KnowledgeBaseFileInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool KnowledgeBaseFileInfo_DeleteList(string fileIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(KnowledgeBaseFileInfoTableName);
            strSql.Append(" where FileID in (" + fileIDlist + ")  ;");

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(KnowledgeBaseFileInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public KnowledgeBaseFileInfo KnowledgeBaseFileInfo_GetModel(string fileID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseFileInfoTableField);
            strSql.Append(" from ");
            strSql.Append(KnowledgeBaseFileInfoTableName);
            strSql.Append(" where FileID=@FileID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@FileID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = fileID;
            KnowledgeBaseFileInfo model = new KnowledgeBaseFileInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseFileInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return KnowledgeBaseFileInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public KnowledgeBaseFileInfo KnowledgeBaseFileInfo_DataRowToModel(DataRow row)
        {
            KnowledgeBaseFileInfo model = new KnowledgeBaseFileInfo();
            if (row != null)
            {
				if (row["FileID"] != null )
                {
					model.FileID = row["FileID"].ToString();
                }
                if (row["KnowledgeBaseID"] != null)
                {
                    model.KnowledgeBaseID = row["KnowledgeBaseID"].ToString();
                }
                if (row["FileName"] != null )
                {
					model.FileName = row["FileName"].ToString();
                }
				if (row["FilePath"] != null )
                {
					model.FilePath = row["FilePath"].ToString();
                }
				if (row["Type"] != null )
                {
					model.Type = row["Type"].ToString();
                }
				if (row["ParserConfig"] != null )
                {
					model.ParserConfig = row["ParserConfig"].ToString();
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["SystemStatus"] != null )
                {
                    model.SystemStatus =(ImportKmsStatus) int.Parse(row["SystemStatus"].ToString());
                }
                if (row["DataCount"] != null)
                {
                    model.DataCount = int.Parse(row["DataCount"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet KnowledgeBaseFileInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseFileInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(KnowledgeBaseFileInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseFileInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet KnowledgeBaseFileInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseFileInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(KnowledgeBaseFileInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseFileInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int KnowledgeBaseFileInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(KnowledgeBaseFileInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(KnowledgeBaseFileInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet KnowledgeBaseFileInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseFileInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(KnowledgeBaseFileInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseFileInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable KnowledgeBaseFileInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "FileID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_knowledge_base_file_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseFileInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
