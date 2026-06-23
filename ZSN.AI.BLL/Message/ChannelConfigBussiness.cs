using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class ChannelConfigBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "MessageDb";
        #endregion
		#region tb_channel_config
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(ChannelConfigInfo model)
		{
			return DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(ChannelConfigInfo model)
		{
			return DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string channelID)
		{
			return DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_Delete(channelID);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ChannelConfigInfo GetModel(string channelID)
		{
			return DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_GetModel(channelID);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<ChannelConfigInfo> GetList(string strWhere = "")
		{
            return ChannelConfigDataSet_ToList(DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_GetList(strWhere).Tables[0]);
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
        /// <param name="orderKey">排序key，默认CreateTime</param>
        /// <returns></returns>
		public static List<ChannelConfigInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime")
		{
            return ChannelConfigDataSet_ToList(DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
		}
		private static List<ChannelConfigInfo> ChannelConfigDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<ChannelConfigInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_DataRowToModel(r));
            }
            return list;
		}
		#endregion
	}
}
