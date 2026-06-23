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
	    #region 基础信息
        private const string ConnectionName = "MessageDb";
        #endregion
		#region tb_message_receive_record
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(MessageReceiveRecordInfo model)
		{
			return DatabaseProvider.GetMessageReceiveRecord(ConnectionName).MessageReceiveRecord_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(MessageReceiveRecordInfo model)
		{
			return DatabaseProvider.GetMessageReceiveRecord(ConnectionName).MessageReceiveRecord_Update(model);
		}
        /// <summary>
        /// 根据EventId获取对象实体
        /// </summary>
		public static MessageReceiveRecordInfo GetByEventId(string eventId)
		{
			return DatabaseProvider.GetMessageReceiveRecord(ConnectionName).MessageReceiveRecord_GetByEventId(eventId);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<MessageReceiveRecordInfo> GetList(string strWhere = "")
		{
            return MessageReceiveRecordDataSet_ToList(DatabaseProvider.GetMessageReceiveRecord(ConnectionName).MessageReceiveRecord_GetList(strWhere).Tables[0]);
		}
		/// <summary>
        /// 分页获取数据列表
        /// </summary>
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
		#endregion
	}
}
