using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class MessageSendRecordBussiness
    {
        private const string ConnectionName = "MessageDb";
        public static string Add(MessageSendRecordInfo model)
        {
            return DatabaseProvider.GetMessageSendRecord(ConnectionName).MessageSendRecord_Add(model);
        }
        public static bool Update(MessageSendRecordInfo model)
        {
            return DatabaseProvider.GetMessageSendRecord(ConnectionName).MessageSendRecord_Update(model);
        }
        public static MessageSendRecordInfo GetModel(string recordID)
        {
            return DatabaseProvider.GetMessageSendRecord(ConnectionName).MessageSendRecord_GetModel(recordID);
        }
        public static List<MessageSendRecordInfo> GetList(string strWhere = "")
        {
            return MessageSendRecordDataSet_ToList(DatabaseProvider.GetMessageSendRecord(ConnectionName).MessageSendRecord_GetList(strWhere).Tables[0]);
        }
        public static List<MessageSendRecordInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total)
        {
            var dt = DatabaseProvider.GetMessageSendRecord(ConnectionName).MessageSendRecord_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total);
            if (dt == null) return new List<MessageSendRecordInfo>();
            return MessageSendRecordDataSet_ToList(dt);
        }
        private static List<MessageSendRecordInfo> MessageSendRecordDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<MessageSendRecordInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetMessageSendRecord(ConnectionName).MessageSendRecord_DataRowToModel(r));
            }
            return list;
        }
    }
}
