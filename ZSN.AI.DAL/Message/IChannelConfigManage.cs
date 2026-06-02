using System;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial interface IChannelConfigManage
    {
        string SetConnectionName(string connName);
        string ChannelConfig_Add(ChannelConfigInfo model);
        bool ChannelConfig_Update(ChannelConfigInfo model);
        bool ChannelConfig_Delete(string channelID);
        ChannelConfigInfo ChannelConfig_DataRowToModel(DataRow row);
        ChannelConfigInfo ChannelConfig_GetModel(string channelID);
        DataSet ChannelConfig_GetList(string strWhere);
        DataTable ChannelConfig_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime");
    }
}
