using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class McpInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "ModelDb";
        #endregion
		#region tb_mcp_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(McpInfo model)
		{
			return DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(McpInfo model)
		{
			return DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string mCPID)
		{
			return DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_Delete(mCPID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string mCPIDlist)
		{
            mCPIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(mCPIDlist, ',', '\'');
            return DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_DeleteList(mCPIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.McpInfo GetModel(string mCPID)
		{
			return DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_GetModel(mCPID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<McpInfo> GetList(string strWhere = "")
        {
            return McpInfoDataSet_ToList(DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<McpInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return McpInfoDataSet_ToList(DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<McpInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return McpInfoDataSet_ToList(DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
        /// <returns></returns>
		public static List<McpInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "MCPID")
		{
            return McpInfoDataSet_ToList(DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<McpInfo> McpInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<McpInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetMcpInfo(ConnectionName).McpInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
