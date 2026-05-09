using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class MemberOtherAuthInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "MemberDb";
        #endregion
		#region tb_member_other_auth_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(MemberOtherAuthInfo model)
		{
			return DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(MemberOtherAuthInfo model)
		{
			return DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 memberOtherAuthID)
		{
			return DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_Delete(memberOtherAuthID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string memberOtherAuthIDlist)
		{
			return DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_DeleteList(memberOtherAuthIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.MemberOtherAuthInfo GetModel(Int32 memberOtherAuthID)
		{
			return DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_GetModel(memberOtherAuthID);
		}
        public static ZSN.AI.Entity.MemberOtherAuthInfo GetModel(string memberID)
        {
            return DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_GetModel(memberID);
        }
        public static ZSN.AI.Entity.MemberOtherAuthInfo GetModelByOpenid(string OpenID)
        {
            return DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_GetModelByOpenid(OpenID);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<MemberOtherAuthInfo> GetList(string strWhere = "")
        {
            return MemberOtherAuthInfoDataSet_ToList(DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<MemberOtherAuthInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return MemberOtherAuthInfoDataSet_ToList(DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<MemberOtherAuthInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return MemberOtherAuthInfoDataSet_ToList(DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<MemberOtherAuthInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "MemberOtherAuthID")
		{
            return MemberOtherAuthInfoDataSet_ToList(DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<MemberOtherAuthInfo> MemberOtherAuthInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<MemberOtherAuthInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetMemberOtherAuthInfo(ConnectionName).MemberOtherAuthInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
