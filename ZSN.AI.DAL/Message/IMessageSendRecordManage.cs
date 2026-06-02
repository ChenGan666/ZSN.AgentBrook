using System;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial interface IMessageSendRecordManage
    {
        string SetConnectionName(string connName);
        string MessageSendRecord_Add(MessageSendRecordInfo model);
        bool MessageSendRecord_Update(MessageSendRecordInfo model);
        MessageSendRecordInfo MessageSendRecord_DataRowToModel(DataRow row);
        MessageSendRecordInfo MessageSendRecord_GetModel(string recordID);
        DataSet MessageSendRecord_GetList(string strWhere);
        DataTable MessageSendRecord_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime");
    }
}
