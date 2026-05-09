using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class VcodeInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "BaseDb";
        #endregion
		#region tb_vcode_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(VcodeInfo model)
		{
			return DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(VcodeInfo model)
		{
			return DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 vCodeID)
		{
			return DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_Delete(vCodeID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string vCodeIDlist)
		{
			return DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_DeleteList(vCodeIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.VcodeInfo GetModel(Int32 vCodeID)
		{
			return DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_GetModel(vCodeID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<VcodeInfo> GetList(string strWhere = "")
        {
            return VcodeInfoDataSet_ToList(DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<VcodeInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return VcodeInfoDataSet_ToList(DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<VcodeInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return VcodeInfoDataSet_ToList(DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<VcodeInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "VCodeID")
		{
            return VcodeInfoDataSet_ToList(DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<VcodeInfo> VcodeInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<VcodeInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetVcodeInfo(ConnectionName).VcodeInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
