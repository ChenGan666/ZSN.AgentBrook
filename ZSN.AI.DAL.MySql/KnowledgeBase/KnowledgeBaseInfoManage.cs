using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL.MySql
{
    public partial class KnowledgeBaseInfoManage : IKnowledgeBaseInfoManage
    {
        ///表链接
        private string KnowledgeBaseInfoConnectionName = "ModelDb";
        ///表名称
        private string KnowledgeBaseInfoTableName = "tb_knowledge_base_info";
        ///表字段
        private const string KnowledgeBaseInfoTableField = "KnowledgeBaseID,Name,DicIDList,DicNameList,Description,PreprocessModelID,PreprocessModelName,VectorModelID,VectorModelName,ParagraphSlice,LineSliceCount,OverlapSection,SystemStatus,MemberID,ChargeType,CreateTime,LastUpdateTime";
        ///添加用表字段
        private const string KnowledgeBaseInfoTableFieldForAdd = "KnowledgeBaseID,Name,DicIDList,DicNameList,Description,PreprocessModelID,PreprocessModelName,VectorModelID,VectorModelName,ParagraphSlice,LineSliceCount,OverlapSection,SystemStatus,MemberID,ChargeType,CreateTime,LastUpdateTime";
        ///添加用表字段value
        private const string KnowledgeBaseInfoTableFieldAltForAdd = "@KnowledgeBaseID,@Name,@DicIDList,@DicNameList,@Description,@PreprocessModelID,@PreprocessModelName,@VectorModelID,@VectorModelName,@ParagraphSlice,@LineSliceCount,@OverlapSection,@SystemStatus,@MemberID,@ChargeType,@CreateTime,@LastUpdateTime";
        public string SetConnectionName(string connName)
        {
            return KnowledgeBaseInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public string KnowledgeBaseInfo_Add(KnowledgeBaseInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(KnowledgeBaseInfoTableName);
			strSql.Append(" (");
            strSql.Append(KnowledgeBaseInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(KnowledgeBaseInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@KnowledgeBaseID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Name", MySqlDbType.VarChar,128),
 new MySqlParameter("@DicIDList", MySqlDbType.VarChar,512),
 new MySqlParameter("@DicNameList", MySqlDbType.VarChar,1024),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 new MySqlParameter("@PreprocessModelID", MySqlDbType.Int32,10),
 new MySqlParameter("@VectorModelID", MySqlDbType.Int32,10),
 new MySqlParameter("@ParagraphSlice", MySqlDbType.Int32,10),
 new MySqlParameter("@LineSliceCount", MySqlDbType.Int32,10),
 new MySqlParameter("@OverlapSection", MySqlDbType.Int32,10),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@ChargeType", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@PreprocessModelName", MySqlDbType.VarChar,128),
 new MySqlParameter("@VectorModelName", MySqlDbType.VarChar,128),

                    };
			 parameters[0].Value = model.KnowledgeBaseID;
 parameters[1].Value = model.Name;
 parameters[2].Value = model.DicIDList;
 parameters[3].Value = model.DicNameList;
 parameters[4].Value = model.Description;
 parameters[5].Value = model.PreprocessModelID;
 parameters[6].Value = model.VectorModelID;
 parameters[7].Value = model.ParagraphSlice;
 parameters[8].Value = model.LineSliceCount;
 parameters[9].Value = model.OverlapSection;
 parameters[10].Value = model.SystemStatus;
 parameters[11].Value = model.MemberID;
 parameters[12].Value = model.ChargeType;
 parameters[13].Value = model.CreateTime;
 parameters[14].Value = model.LastUpdateTime;
            parameters[15].Value = model.PreprocessModelName;
            parameters[16].Value = model.VectorModelName;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(KnowledgeBaseInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return String.Empty;
            }
            else
            {
                 return model.KnowledgeBaseID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool KnowledgeBaseInfo_Update(KnowledgeBaseInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(KnowledgeBaseInfoTableName);
            strSql.Append(" set ");
			strSql.Append("Name=@Name,");
strSql.Append("DicIDList=@DicIDList,");
strSql.Append("DicNameList=@DicNameList,");
strSql.Append("Description=@Description,");
strSql.Append("PreprocessModelID=@PreprocessModelID,");
            strSql.Append("PreprocessModelName=@PreprocessModelName,");
            strSql.Append("VectorModelID=@VectorModelID,");
            strSql.Append("VectorModelName=@VectorModelName,");
            strSql.Append("ParagraphSlice=@ParagraphSlice,");
strSql.Append("LineSliceCount=@LineSliceCount,");
strSql.Append("OverlapSection=@OverlapSection,");
strSql.Append("SystemStatus=@SystemStatus,");
strSql.Append("MemberID=@MemberID,");
strSql.Append("ChargeType=@ChargeType,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("LastUpdateTime=@LastUpdateTime");

            strSql.Append(" where KnowledgeBaseID=@KnowledgeBaseID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@KnowledgeBaseID", MySqlDbType.VarChar,64),
 new MySqlParameter("@Name", MySqlDbType.VarChar,128),
 new MySqlParameter("@DicIDList", MySqlDbType.VarChar,512),
 new MySqlParameter("@DicNameList", MySqlDbType.VarChar,1024),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 new MySqlParameter("@PreprocessModelID", MySqlDbType.Int32,10),
 new MySqlParameter("@VectorModelID", MySqlDbType.Int32,10),
 new MySqlParameter("@ParagraphSlice", MySqlDbType.Int32,10),
 new MySqlParameter("@LineSliceCount", MySqlDbType.Int32,10),
 new MySqlParameter("@OverlapSection", MySqlDbType.Int32,10),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@MemberID", MySqlDbType.VarChar,64),
 new MySqlParameter("@ChargeType", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@LastUpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@PreprocessModelName", MySqlDbType.VarChar,128),
 new MySqlParameter("@VectorModelName", MySqlDbType.VarChar,128),

            };
			 parameters[0].Value = model.KnowledgeBaseID;
 parameters[1].Value = model.Name;
 parameters[2].Value = model.DicIDList;
 parameters[3].Value = model.DicNameList;
 parameters[4].Value = model.Description;
 parameters[5].Value = model.PreprocessModelID;
 parameters[6].Value = model.VectorModelID;
 parameters[7].Value = model.ParagraphSlice;
 parameters[8].Value = model.LineSliceCount;
 parameters[9].Value = model.OverlapSection;
 parameters[10].Value = model.SystemStatus;
 parameters[11].Value = model.MemberID;
 parameters[12].Value = model.ChargeType;
 parameters[13].Value = model.CreateTime;
 parameters[14].Value = model.LastUpdateTime;
            parameters[15].Value = model.PreprocessModelName;
            parameters[16].Value = model.VectorModelName;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(KnowledgeBaseInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool KnowledgeBaseInfo_Delete(string knowledgeBaseID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(KnowledgeBaseInfoTableName);
            strSql.Append(" where KnowledgeBaseID=@KnowledgeBaseID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@KnowledgeBaseID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = knowledgeBaseID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(KnowledgeBaseInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool KnowledgeBaseInfo_DeleteList(string knowledgeBaseIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(KnowledgeBaseInfoTableName);
            strSql.Append(" where KnowledgeBaseID in (" + knowledgeBaseIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(KnowledgeBaseInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public KnowledgeBaseInfo KnowledgeBaseInfo_GetModel(string knowledgeBaseID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseInfoTableField);
            strSql.Append(" from ");
            strSql.Append(KnowledgeBaseInfoTableName);
            strSql.Append(" where KnowledgeBaseID=@KnowledgeBaseID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@KnowledgeBaseID", MySqlDbType.VarChar, 64)
			};
            parameters[0].Value = knowledgeBaseID;
            KnowledgeBaseInfo model = new KnowledgeBaseInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return KnowledgeBaseInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public KnowledgeBaseInfo KnowledgeBaseInfo_DataRowToModel(DataRow row)
        {
            KnowledgeBaseInfo model = new KnowledgeBaseInfo();
            if (row != null)
            {
				if (row["KnowledgeBaseID"] != null )
                {
					model.KnowledgeBaseID = row["KnowledgeBaseID"].ToString();
                }
				if (row["Name"] != null )
                {
					model.Name = row["Name"].ToString();
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
				if (row["PreprocessModelID"] != null )
                {
                    model.PreprocessModelID = int.Parse(row["PreprocessModelID"].ToString());
                }
                if (row["PreprocessModelName"] != null)
                {
                    model.PreprocessModelName = row["PreprocessModelName"].ToString();
                }
                if (row["VectorModelID"] != null )
                {
                    model.VectorModelID = int.Parse(row["VectorModelID"].ToString());
                }
                if (row["VectorModelName"] != null)
                {
                    model.VectorModelName = row["VectorModelName"].ToString();
                }
                if (row["ParagraphSlice"] != null )
                {
                    model.ParagraphSlice = int.Parse(row["ParagraphSlice"].ToString());
                }
				if (row["LineSliceCount"] != null )
                {
                    model.LineSliceCount = int.Parse(row["LineSliceCount"].ToString());
                }
				if (row["OverlapSection"] != null )
                {
                    model.OverlapSection = int.Parse(row["OverlapSection"].ToString());
                }
				if (row["SystemStatus"] != null )
                {
                    model.SystemStatus = (KnowledgeBaseStatus) int.Parse(row["SystemStatus"].ToString());
                }
				if (row["MemberID"] != null )
                {
					model.MemberID = row["MemberID"].ToString();
                }
				if (row["ChargeType"] != null )
                {
                    model.ChargeType = int.Parse(row["ChargeType"].ToString());
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
        public DataSet KnowledgeBaseInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(KnowledgeBaseInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet KnowledgeBaseInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(KnowledgeBaseInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int KnowledgeBaseInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(KnowledgeBaseInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(KnowledgeBaseInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet KnowledgeBaseInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(KnowledgeBaseInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(KnowledgeBaseInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable KnowledgeBaseInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "KnowledgeBaseID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_knowledge_base_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(KnowledgeBaseInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
