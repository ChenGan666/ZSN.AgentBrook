using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class SkillInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "ExpandDb";
        #endregion
		#region tb_skill_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(SkillInfo model)
		{
			return DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(SkillInfo model)
		{
			return DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string skillID)
		{
			return DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_Delete(skillID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string skillIDlist)
		{
            skillIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(skillIDlist, ',', '\'');

            return DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_DeleteList(skillIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.SkillInfo GetModel(string skillID)
		{
			return DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_GetModel(skillID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<SkillInfo> GetList(string strWhere = "")
        {
            return SkillInfoDataSet_ToList(DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<SkillInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return SkillInfoDataSet_ToList(DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<SkillInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return SkillInfoDataSet_ToList(DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<SkillInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "SkillID")
		{
            return SkillInfoDataSet_ToList(DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<SkillInfo> SkillInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<SkillInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetSkillInfo(ConnectionName).SkillInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
