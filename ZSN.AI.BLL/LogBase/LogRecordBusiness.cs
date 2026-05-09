using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class LogRecordBusiness
    {
	    #region 
        private const string ConnectionName = "LogBaseDb";
        #endregion
		#region log_record

		public static int Add(LogRecord model)
		{
			return DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_Add(model);
		}

		public static bool Update(LogRecord model)
		{
			return DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_Update(model);
		}

		public static bool Delete(Int64 id)
		{
			return DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_Delete(id);
		}
        public static bool DeleteByWhere(string where)
        {
            return DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_DeleteByWhere(where);
        }
        public static bool DeleteList(string idlist)
		{
			return DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_DeleteList(idlist);
		}

		public static ZSN.AI.Entity.LogRecord GetModel(Int64 id)
		{
			return DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_GetModel(id);
		}

		public static List<LogRecord> GetList(string strWhere = "")
        {
            return LogRecordDataSet_ToList(DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_GetList(strWhere).Tables[0]);
        }

		public static List<LogRecord> GetList(int top, string strWhere, string filedOrder)
        {
            return LogRecordDataSet_ToList(DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_GetList(top, strWhere, filedOrder).Tables[0]);
        }

		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_GetRecordCount(strWhere);
        }

		public static List<LogRecord> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return LogRecordDataSet_ToList(DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }

		public static List<LogRecord> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "Id")
		{
            return LogRecordDataSet_ToList(DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<LogRecord> LogRecordDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<LogRecord>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetLogRecord(ConnectionName).LogRecord_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
