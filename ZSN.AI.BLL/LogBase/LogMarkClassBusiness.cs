using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class LogMarkClassBusiness
    {
	    #region 
        private const string ConnectionName = "LogBaseDb";
        #endregion
		#region log_mark_class

		public static int Add(LogMarkClass model)
		{
			return DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_Add(model);
		}

		public static bool Update(LogMarkClass model)
		{
			return DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_Update(model);
		}

		public static bool Delete(Int32 id)
		{
			return DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_Delete(id);
		}

		public static bool DeleteList(string idlist)
		{
			return DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_DeleteList(idlist);
		}

		public static ZSN.AI.Entity.LogMarkClass GetModel(Int32 id)
		{
			return DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_GetModel(id);
		}

		public static List<LogMarkClass> GetList(string strWhere = "")
        {
            return LogMarkClassDataSet_ToList(DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_GetList(strWhere).Tables[0]);
        }

		public static List<LogMarkClass> GetList(int top, string strWhere, string filedOrder)
        {
            return LogMarkClassDataSet_ToList(DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_GetList(top, strWhere, filedOrder).Tables[0]);
        }

		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_GetRecordCount(strWhere);
        }

		public static List<LogMarkClass> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return LogMarkClassDataSet_ToList(DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }
		
		public static List<LogMarkClass> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "Id")
		{
            return LogMarkClassDataSet_ToList(DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<LogMarkClass> LogMarkClassDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<LogMarkClass>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetLogMarkClass(ConnectionName).LogMarkClass_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
