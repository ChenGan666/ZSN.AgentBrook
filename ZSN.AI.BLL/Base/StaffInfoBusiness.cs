using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class StaffInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "BaseDb";
        #endregion
		#region tb_staff_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(StaffInfo model)
		{
			return DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(StaffInfo model)
		{
			return DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 staffID)
		{
			return DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_Delete(staffID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string staffIDlist)
		{
			return DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_DeleteList(staffIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.StaffInfo GetModel(Int32 staffID)
		{
			return DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_GetModel(staffID);
		}
        public static ZSN.AI.Entity.StaffInfo GetModel(string MemberID)
        {
            return DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_GetModel(MemberID);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<StaffInfo> GetList(string strWhere = "")
        {
            return StaffInfoDataSet_ToList(DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<StaffInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return StaffInfoDataSet_ToList(DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<StaffInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return StaffInfoDataSet_ToList(DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<StaffInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "StaffID")
		{
            return StaffInfoDataSet_ToList(DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<StaffInfo> StaffInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<StaffInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetStaffInfo(ConnectionName).StaffInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
