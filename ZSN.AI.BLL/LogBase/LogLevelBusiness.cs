using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class LogLevelBusiness
    {
	    #region 
        private const string ConnectionName = "LogBaseDb";
        #endregion
		#region log_level

		public static int Add(LogLevel model)
		{
			return DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_Add(model);
		}

		public static bool Update(LogLevel model)
		{
			return DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_Update(model);
		}

		public static bool Delete(Int32 id)
		{
			return DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_Delete(id);
		}

		public static bool DeleteList(string idlist)
		{
			return DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_DeleteList(idlist);
		}

		public static ZSN.AI.Entity.LogLevel GetModel(Int32 id)
		{
			return DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_GetModel(id);
		}

		public static List<LogLevel> GetList(string strWhere = "")
        {
            return LogLevelDataSet_ToList(DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_GetList(strWhere).Tables[0]);
        }

		public static List<LogLevel> GetList(int top, string strWhere, string filedOrder)
        {
            return LogLevelDataSet_ToList(DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_GetList(top, strWhere, filedOrder).Tables[0]);
        }
  
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_GetRecordCount(strWhere);
        }
  
		public static List<LogLevel> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return LogLevelDataSet_ToList(DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }

		public static List<LogLevel> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "Id")
		{
            return LogLevelDataSet_ToList(DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<LogLevel> LogLevelDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<LogLevel>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetLogLevel(ConnectionName).LogLevel_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
