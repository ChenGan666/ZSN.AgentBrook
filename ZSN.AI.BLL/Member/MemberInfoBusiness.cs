using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class MemberInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "MemberDb";
        #endregion
		#region tb_member_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(MemberInfo model)
		{
			return DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(MemberInfo model)
		{
			return DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string memberID)
		{
			return DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_Delete(memberID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string memberIDlist)
		{
            memberIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(memberIDlist, ',', '\'');
            return DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_DeleteList(memberIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.MemberInfo GetModel(string memberID)
		{
			return DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_GetModel(memberID);
		}
        public static ZSN.AI.Entity.MemberInfo GetModelByPhoneNumber(string PhoneNumber)
        {
            return DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_GetModelByPhoneNumber(PhoneNumber);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<MemberInfo> GetList(string strWhere = "")
        {
            return MemberInfoDataSet_ToList(DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<MemberInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return MemberInfoDataSet_ToList(DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<MemberInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return MemberInfoDataSet_ToList(DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<MemberInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "MemberID")
		{
            return MemberInfoDataSet_ToList(DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<MemberInfo> MemberInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<MemberInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetMemberInfo(ConnectionName).MemberInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
