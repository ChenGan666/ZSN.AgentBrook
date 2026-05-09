using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class MemberAuthInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "MemberDb";
        #endregion
		#region tb_member_auth_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(MemberAuthInfo model)
		{
			return DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(MemberAuthInfo model)
		{
			return DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int64 memberAuthID)
		{
			return DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_Delete(memberAuthID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string memberAuthIDlist)
		{
			return DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_DeleteList(memberAuthIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.MemberAuthInfo GetModel(Int64 memberAuthID)
		{
			return DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_GetModel(memberAuthID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<MemberAuthInfo> GetList(string strWhere = "")
        {
            return MemberAuthInfoDataSet_ToList(DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<MemberAuthInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return MemberAuthInfoDataSet_ToList(DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<MemberAuthInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return MemberAuthInfoDataSet_ToList(DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<MemberAuthInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "MemberAuthID")
		{
            return MemberAuthInfoDataSet_ToList(DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<MemberAuthInfo> MemberAuthInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<MemberAuthInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_DataRowToModel(r));
            }
            return list;
		}
        public static ZSN.AI.Entity.MemberAuthInfo GetModel(string memberID)
        {
            return DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_GetModel(memberID);
        }
        public static ZSN.AI.Entity.MemberAuthInfo GetModelByAccessToken(string AccessToken)
        {
            return DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_GetModelByAccessToken(AccessToken);
        }
        public static ZSN.AI.Entity.MemberAuthInfo GetModelByRefreshToken(string RefreshToken)
        {
            return DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_GetModelByRefreshToken(RefreshToken);
        }
        public static ZSN.AI.Entity.MemberAuthInfo GetModelByMemberID(string memberID)
        {
            return DatabaseProvider.GetMemberAuthInfo(ConnectionName).MemberAuthInfo_GetModelByMemberID(memberID);
        }
        #endregion
    }
}
