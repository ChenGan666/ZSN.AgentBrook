using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Data;
using ZSN.AI.Entity.Model.Enum;
namespace ZSN.AI.DAL.MySql
{
    public partial class LargeModelInfoManage : ILargeModelInfoManage
    {
        ///表链接
        private string LargeModelInfoConnectionName = "ModelDb";
        ///表名称
        private string LargeModelInfoTableName = "tb_large_model_info";
        ///表字段
        private const string LargeModelInfoTableField = "LargeModelID,Name,MICON,TypeCode,TypeName,EndPoint,MConfig,Description,SystemStatus,CreateTime,UpdateTime,ModelOrganizationID,ModelOrganizationName,ModelName,ModelKey,IsDefaultModel";
        ///添加用表字段
        private const string LargeModelInfoTableFieldForAdd = "Name,MICON,TypeCode,TypeName,EndPoint,MConfig,Description,SystemStatus,CreateTime,UpdateTime,ModelOrganizationID,ModelOrganizationName,ModelName,ModelKey,IsDefaultModel";
        ///添加用表字段value
        private const string LargeModelInfoTableFieldAltForAdd = "@Name,@MICON,@TypeCode,@TypeName,@EndPoint,@MConfig,@Description,@SystemStatus,@CreateTime,@UpdateTime,@ModelOrganizationID,@ModelOrganizationName,@ModelName,@ModelKey,@IsDefaultModel";
        public string SetConnectionName(string connName)
        {
            return LargeModelInfoConnectionName = connName;
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
        public Int32 LargeModelInfo_Add(LargeModelInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ");
            strSql.Append(LargeModelInfoTableName);
			strSql.Append(" (");
            strSql.Append(LargeModelInfoTableFieldForAdd);
            strSql.Append(") values (");
            strSql.Append(LargeModelInfoTableFieldAltForAdd);
            strSql.Append(")");
            strSql.Append(";select @@IDENTITY");
            MySqlParameter[] parameters = {
			 new MySqlParameter("@Name", MySqlDbType.VarChar,50),
 new MySqlParameter("@TypeCode", MySqlDbType.Int32),
 new MySqlParameter("@TypeName", MySqlDbType.VarChar,50),
 new MySqlParameter("@EndPoint", MySqlDbType.VarChar,255),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),

             new MySqlParameter("@MICON", MySqlDbType.VarChar,64),
             new MySqlParameter("@MConfig", MySqlDbType.VarChar,512),
 new MySqlParameter("@ModelOrganizationID", MySqlDbType.Int32,10),
 new MySqlParameter("@ModelOrganizationName", MySqlDbType.VarChar,128),


 new MySqlParameter("@ModelName", MySqlDbType.VarChar,50),
 new MySqlParameter("@ModelKey", MySqlDbType.VarChar,128),

 new MySqlParameter("@IsDefaultModel", MySqlDbType.Int32),
                    };
			 parameters[0].Value = model.Name;
 parameters[1].Value = model.TypeCode;
 parameters[2].Value = model.TypeName;
 parameters[3].Value = model.EndPoint;
 parameters[4].Value = model.Description;
 parameters[5].Value = model.SystemStatus;
 parameters[6].Value = model.CreateTime;
 parameters[7].Value = model.UpdateTime;
            parameters[8].Value = model.MICON;
            parameters[9].Value = model.MConfig;
            parameters[10].Value = model.ModelOrganizationID;
            parameters[11].Value = model.ModelOrganizationName;

            parameters[12].Value = model.ModelName;
            parameters[13].Value = model.ModelKey;

            parameters[14].Value = model.IsDefaultModel;

            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(LargeModelInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (obj == null)
            {
                return 0;
            }
            else
            {
                 model.LargeModelID = Convert.ToInt32(obj);
                 return model.LargeModelID;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool LargeModelInfo_Update(LargeModelInfo model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ");
            strSql.Append(LargeModelInfoTableName);
            strSql.Append(" set ");
			strSql.Append("Name=@Name,");
strSql.Append("TypeCode=@TypeCode,");
strSql.Append("TypeName=@TypeName,");
strSql.Append("EndPoint=@EndPoint,");
strSql.Append("Description=@Description,");
strSql.Append("SystemStatus=@SystemStatus,");
strSql.Append("CreateTime=@CreateTime,");
strSql.Append("UpdateTime=@UpdateTime,");
            strSql.Append("MICON=@MICON,");
            strSql.Append("MConfig=@MConfig,");
            strSql.Append("ModelOrganizationID=@ModelOrganizationID,");
            strSql.Append("ModelOrganizationName=@ModelOrganizationName,");

            strSql.Append("ModelName=@ModelName, ");
            strSql.Append("ModelKey=@ModelKey, ");
            strSql.Append("IsDefaultModel=@IsDefaultModel ");

            strSql.Append(" where LargeModelID=@LargeModelID");
            MySqlParameter[] parameters = {
				 new MySqlParameter("@LargeModelID", MySqlDbType.Int32),
 new MySqlParameter("@Name", MySqlDbType.VarChar,50),
 new MySqlParameter("@TypeCode", MySqlDbType.Int32),
 new MySqlParameter("@TypeName", MySqlDbType.VarChar,50),
 new MySqlParameter("@EndPoint", MySqlDbType.VarChar,255),
 new MySqlParameter("@Description", MySqlDbType.VarChar,512),
 new MySqlParameter("@SystemStatus", MySqlDbType.Int32,10),
 new MySqlParameter("@CreateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@UpdateTime", MySqlDbType.DateTime,16),
 new MySqlParameter("@MICON", MySqlDbType.VarChar,64),
 new MySqlParameter("@MConfig", MySqlDbType.VarChar,512),
 new MySqlParameter("@ModelOrganizationID", MySqlDbType.Int32,10),
 new MySqlParameter("@ModelOrganizationName", MySqlDbType.VarChar,128),

 new MySqlParameter("@ModelName", MySqlDbType.VarChar,50),
 new MySqlParameter("@ModelKey", MySqlDbType.VarChar,128),

 new MySqlParameter("@IsDefaultModel", MySqlDbType.Int32),
            };
			 parameters[0].Value = model.LargeModelID;
 parameters[1].Value = model.Name;
 parameters[2].Value = model.TypeCode;
 parameters[3].Value = model.TypeName;
 parameters[4].Value = model.EndPoint;
 parameters[5].Value = model.Description;
 parameters[6].Value = model.SystemStatus;
 parameters[7].Value = model.CreateTime;
 parameters[8].Value = model.UpdateTime;
            parameters[9].Value = model.MICON;
            parameters[10].Value = model.MConfig;
            parameters[11].Value = model.ModelOrganizationID;
            parameters[12].Value = model.ModelOrganizationName;

            parameters[13].Value = model.ModelName;
            parameters[14].Value = model.ModelKey;
            parameters[15].Value = model.IsDefaultModel;

            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(LargeModelInfoConnectionName),CommandType.Text,strSql.ToString(), parameters);
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
        public bool LargeModelInfo_Delete(Int32 largeModelID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(LargeModelInfoTableName);
            strSql.Append(" where LargeModelID=@LargeModelID");
            MySqlParameter[] parameters = {
					new MySqlParameter("@LargeModelID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = largeModelID;
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(LargeModelInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
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
        public bool LargeModelInfo_DeleteList(string largeModelIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ");
            strSql.Append(LargeModelInfoTableName);
            strSql.Append(" where LargeModelID in (" + largeModelIDlist + ")  ");
            int rows = DbHelper.ExecuteNonQuery(DbConfig.GetDbInfo(LargeModelInfoConnectionName), CommandType.Text,strSql.ToString());
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
        public LargeModelInfo LargeModelInfo_GetModel(Int32 largeModelID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LargeModelInfoTableField);
            strSql.Append(" from ");
            strSql.Append(LargeModelInfoTableName);
            strSql.Append(" where LargeModelID=@LargeModelID");
            strSql.Append(" limit 1");
            MySqlParameter[] parameters = {
					new MySqlParameter("@LargeModelID", MySqlDbType.Int32, 10)
			};
            parameters[0].Value = largeModelID;
            LargeModelInfo model = new LargeModelInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LargeModelInfoConnectionName), CommandType.Text,strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return LargeModelInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        public LargeModelInfo LargeModelInfo_GetDefaultModel()
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LargeModelInfoTableField);
            strSql.Append(" from ");
            strSql.Append(LargeModelInfoTableName);
            strSql.Append(" where IsDefaultModel=1 and SystemStatus=0");
            strSql.Append(" limit 1");

            LargeModelInfo model = new LargeModelInfo();
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LargeModelInfoConnectionName), CommandType.Text, strSql.ToString());
            if (ds.Tables[0].Rows.Count > 0)
            {
                return LargeModelInfo_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public LargeModelInfo LargeModelInfo_DataRowToModel(DataRow row)
        {
            LargeModelInfo model = new LargeModelInfo();
            if (row != null)
            {
				if (row["LargeModelID"] != null )
                {
                    model.LargeModelID = int.Parse(row["LargeModelID"].ToString());
                }
				if (row["Name"] != null )
                {
					model.Name = row["Name"].ToString();
                }
                if (row["ModelName"] != null)
                {
                    model.ModelName = row["ModelName"].ToString();
                }
                if (row["ModelKey"] != null)
                {
                    model.ModelKey = row["ModelKey"].ToString();
                }
                if (row["MICON"] != null)
                {
                    model.MICON = row["MICON"].ToString();
                }
                if (row["TypeCode"] != null )
                {
                    model.TypeCode =(AIModelType)int.Parse(row["TypeCode"].ToString());
                }
				if (row["TypeName"] != null )
                {
					model.TypeName = row["TypeName"].ToString();
                }
				if (row["EndPoint"] != null )
                {
					model.EndPoint = row["EndPoint"].ToString();
                }
                if (row["MConfig"] != null)
                {
                    model.MConfig = row["MConfig"].ToString();
                }
                if (row["Description"] != null )
                {
					model.Description = row["Description"].ToString();
                }
				if (row["SystemStatus"] != null )
                {
                    model.SystemStatus = (LargeModelStatus) int.Parse(row["SystemStatus"].ToString());
                }
				if (row["CreateTime"] != null )
                {
					model.CreateTime = DateTime.Parse(row["CreateTime"].ToString());
                }
				if (row["UpdateTime"] != null )
                {
					model.UpdateTime = DateTime.Parse(row["UpdateTime"].ToString());
                }
                if (row["ModelOrganizationID"] != null)
                {
                    model.ModelOrganizationID = (AIType)int.Parse(row["ModelOrganizationID"].ToString());
                }
                if (row["ModelOrganizationName"] != null)
                {
                    model.ModelOrganizationName = row["ModelOrganizationName"].ToString();
                }
                if (row["IsDefaultModel"] != null)
                {
                    model.IsDefaultModel = int.Parse(row["IsDefaultModel"].ToString());
                }
            }
            return model;
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet LargeModelInfo_GetList(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LargeModelInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(LargeModelInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LargeModelInfoConnectionName), CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet LargeModelInfo_GetList(int top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LargeModelInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(LargeModelInfoTableName);
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
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LargeModelInfoConnectionName),CommandType.Text,strSql.ToString());
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int LargeModelInfo_GetRecordCount(string strWhere = "")
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ");
            strSql.Append(LargeModelInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelper.ExecuteScalar(DbConfig.GetDbInfo(LargeModelInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataSet LargeModelInfo_GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            strSql.Append(LargeModelInfoTableField);
            strSql.Append(" FROM ");
            strSql.Append(LargeModelInfoTableName);
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + orderby);
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                strSql.Append(" limit " + startIndex + ", " + (endIndex - startIndex));
            }
            return DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LargeModelInfoConnectionName),CommandType.Text,strSql.ToString());
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
        public DataTable LargeModelInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "LargeModelID")
        {
            MySqlParameter[] parameters = {
                    new MySqlParameter("@tableName", MySqlDbType.VarChar, 255),
                    new MySqlParameter("@showFName", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectWhere", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@selectOrder", MySqlDbType.VarChar, 500),
                    new MySqlParameter("@pageNo", MySqlDbType.Int32),
                    new MySqlParameter("@pageSize", MySqlDbType.Int32)
            };
            parameters[0].Value = "tb_large_model_info";
            parameters[1].Value = showName;
            parameters[2].Value = strWhere;
            parameters[3].Value = orderKey + (orderType == 0 ? " ASC" : " DESC");
            parameters[4].Value = pageIndex;
            parameters[5].Value = pageSize;
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(LargeModelInfoConnectionName),CommandType.StoredProcedure, "CommonPagenation", parameters);
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
