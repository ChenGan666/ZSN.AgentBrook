using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IMessageReceiveRecordManage _messageReceiveRecordInstance;
        private static readonly object _messageReceiveRecordLockObj = new object();
        public static IMessageReceiveRecordManage GetMessageReceiveRecord(string connectionName)
        {
            if (_messageReceiveRecordInstance == null)
            {
                lock (_messageReceiveRecordLockObj)
                {
                    if (_messageReceiveRecordInstance == null)
                    {
                        var db = DbConfig.GetDbInfo(connectionName);
                        var type = Type.GetType(
                            $"ZSN.AI.DAL." + db.DbType + ".MessageReceiveRecordManage, ZSN.AI.DAL." + db.DbType,
                            false, true);
                        var provider = (IMessageReceiveRecordManage)Activator.CreateInstance(type);
                        provider.SetConnectionName(connectionName);
                        _messageReceiveRecordInstance = provider;
                    }
                }
            }
            return _messageReceiveRecordInstance;
        }
    }
}
