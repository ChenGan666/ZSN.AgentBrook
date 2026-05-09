using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class DepartmentInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "BaseDb";
        #endregion
		#region tb_department_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(DepartmentInfo model)
		{
			return DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(DepartmentInfo model)
		{
			return DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 departmentID)
		{
			return DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_Delete(departmentID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string departmentIDlist)
		{
			return DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_DeleteList(departmentIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.DepartmentInfo GetModel(Int32 departmentID)
		{
			return DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_GetModel(departmentID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<DepartmentInfo> GetList(string strWhere = "")
        {
            return DepartmentInfoDataSet_ToList(DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<DepartmentInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return DepartmentInfoDataSet_ToList(DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<DepartmentInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return DepartmentInfoDataSet_ToList(DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<DepartmentInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "DepartmentID")
		{
            return DepartmentInfoDataSet_ToList(DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<DepartmentInfo> DepartmentInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<DepartmentInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetDepartmentInfo(ConnectionName).DepartmentInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
