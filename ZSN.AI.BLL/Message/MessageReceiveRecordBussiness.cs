using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class MessageReceiveRecordBussiness
    {
        private const string ConnectionName = "MessageDb";
        public static string Add(MessageReceiveRecordInfo model)
        {
            return DatabaseProvider.GetMessageReceiveRecord(ConnectionName).MessageReceiveRecord_Add(model);
        }
        public static bool Update(MessageReceiveRecordInfo model)
        {
            return DatabaseProvider.GetMessageReceiveRecord(ConnectionName).MessageReceiveRecord_Update(model);
        }
        public static MessageReceiveRecordInfo GetByEventId(string eventId)
        {
            return DatabaseProvider.GetMessageReceiveRecord(ConnectionName).MessageReceiveRecord_GetByEventId(eventId);
        }
        public static List<MessageReceiveRecordInfo> GetList(string strWhere = "")
        {
            return MessageReceiveRecordDataSet_ToList(DatabaseProvider.GetMessageReceiveRecord(ConnectionName).MessageReceiveRecord_GetList(strWhere).Tables[0]);
        }
        public static List<MessageReceiveRecordInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total)
        {
            var dt = DatabaseProvider.GetMessageReceiveRecord(ConnectionName).MessageReceiveRecord_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total);
            if (dt == null) return new List<MessageReceiveRecordInfo>();
            return MessageReceiveRecordDataSet_ToList(dt);
        }
        private static List<MessageReceiveRecordInfo> MessageReceiveRecordDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<MessageReceiveRecordInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetMessageReceiveRecord(ConnectionName).MessageReceiveRecord_DataRowToModel(r));
            }
            return list;
        }
    }
}
