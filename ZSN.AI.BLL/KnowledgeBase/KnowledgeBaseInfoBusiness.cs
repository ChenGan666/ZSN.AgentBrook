using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.DAL;
using ZSN.AI.Entity;
namespace ZSN.AI.BLL
{
    public partial class KnowledgeBaseInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "ModelDb";
        #endregion
		#region tb_knowledge_base_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(KnowledgeBaseInfo model)
		{
			return DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(KnowledgeBaseInfo model)
		{
			return DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string knowledgeBaseID)
		{
			return DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_Delete(knowledgeBaseID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string knowledgeBaseIDlist)
		{
            knowledgeBaseIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(knowledgeBaseIDlist, ',', '\'');
            return DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_DeleteList(knowledgeBaseIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static KnowledgeBaseInfo GetModel(string knowledgeBaseID)
		{
			return DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_GetModel(knowledgeBaseID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<KnowledgeBaseInfo> GetList(string strWhere = "")
        {
            return KnowledgeBaseInfoDataSet_ToList(DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<KnowledgeBaseInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return KnowledgeBaseInfoDataSet_ToList(DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<KnowledgeBaseInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return KnowledgeBaseInfoDataSet_ToList(DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<KnowledgeBaseInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "LastUpdateTime")
		{
            return KnowledgeBaseInfoDataSet_ToList(DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<KnowledgeBaseInfo> KnowledgeBaseInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<KnowledgeBaseInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetKnowledgeBaseInfo(ConnectionName).KnowledgeBaseInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
