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
        private const string ConnectionName = "MessageDb";
        public static string Add(ChannelConfigInfo model)
        {
            return DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_Add(model);
        }
        public static bool Update(ChannelConfigInfo model)
        {
            return DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_Update(model);
        }
        public static bool Delete(string channelID)
        {
            return DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_Delete(channelID);
        }
        public static ChannelConfigInfo GetModel(string channelID)
        {
            return DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_GetModel(channelID);
        }
        public static List<ChannelConfigInfo> GetList(string strWhere = "")
        {
            return ChannelConfigDataSet_ToList(DatabaseProvider.GetChannelConfig(ConnectionName).ChannelConfig_GetList(strWhere).Tables[0]);
        }
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
    }
}
