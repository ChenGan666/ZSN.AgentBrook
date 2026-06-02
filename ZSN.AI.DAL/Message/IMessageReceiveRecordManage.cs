using System;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial interface IMessageReceiveRecordManage
    {
        string SetConnectionName(string connName);
        string MessageReceiveRecord_Add(MessageReceiveRecordInfo model);
        bool MessageReceiveRecord_Update(MessageReceiveRecordInfo model);
        MessageReceiveRecordInfo MessageReceiveRecord_DataRowToModel(DataRow row);
        MessageReceiveRecordInfo MessageReceiveRecord_GetByEventId(string eventId);
        DataSet MessageReceiveRecord_GetList(string strWhere);
        DataTable MessageReceiveRecord_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime");
    }
}
