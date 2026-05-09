using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class LogMarkBusiness
    {
        private const string ConnectionName = "LogBaseDb";

		public static int Add(LogMark model)
		{
			return DatabaseProvider.GetLogMark(ConnectionName).LogMark_Add(model);
		}

		public static bool Update(LogMark model)
		{
			return DatabaseProvider.GetLogMark(ConnectionName).LogMark_Update(model);
		}

		public static bool Delete(Int32 id)
		{
			return DatabaseProvider.GetLogMark(ConnectionName).LogMark_Delete(id);
		}
 
		public static bool DeleteList(string idlist)
		{
			return DatabaseProvider.GetLogMark(ConnectionName).LogMark_DeleteList(idlist);
		}

		public static ZSN.AI.Entity.LogMark GetModel(Int32 id)
		{
			return DatabaseProvider.GetLogMark(ConnectionName).LogMark_GetModel(id);
		}

		public static List<LogMark> GetList(string strWhere = "")
        {
            return LogMarkDataSet_ToList(DatabaseProvider.GetLogMark(ConnectionName).LogMark_GetList(strWhere).Tables[0]);
        }

		public static List<LogMark> GetList(int top, string strWhere, string filedOrder)
        {
            return LogMarkDataSet_ToList(DatabaseProvider.GetLogMark(ConnectionName).LogMark_GetList(top, strWhere, filedOrder).Tables[0]);
        }

		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetLogMark(ConnectionName).LogMark_GetRecordCount(strWhere);
        }
 
		public static List<LogMark> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return LogMarkDataSet_ToList(DatabaseProvider.GetLogMark(ConnectionName).LogMark_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }

		public static List<LogMark> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "Id")
		{
            return LogMarkDataSet_ToList(DatabaseProvider.GetLogMark(ConnectionName).LogMark_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<LogMark> LogMarkDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<LogMark>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetLogMark(ConnectionName).LogMark_DataRowToModel(r));
            }
            return list;
		}
	}
}
