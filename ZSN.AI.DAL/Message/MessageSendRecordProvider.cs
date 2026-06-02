using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IMessageSendRecordManage _messageSendRecordInstance;
        private static readonly object _messageSendRecordLockObj = new object();
        public static IMessageSendRecordManage GetMessageSendRecord(string connectionName)
        {
            if (_messageSendRecordInstance == null)
            {
                lock (_messageSendRecordLockObj)
                {
                    if (_messageSendRecordInstance == null)
                    {
                        var db = DbConfig.GetDbInfo(connectionName);
                        var type = Type.GetType(
                            $"ZSN.AI.DAL." + db.DbType + ".MessageSendRecordManage, ZSN.AI.DAL." + db.DbType,
                            false, true);
                        var provider = (IMessageSendRecordManage)Activator.CreateInstance(type);
                        provider.SetConnectionName(connectionName);
                        _messageSendRecordInstance = provider;
                    }
                }
            }
            return _messageSendRecordInstance;
        }
    }
}
