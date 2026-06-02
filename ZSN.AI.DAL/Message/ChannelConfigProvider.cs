using System;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IChannelConfigManage _channelConfigInstance;
        private static readonly object _channelConfigLockObj = new object();
        public static IChannelConfigManage GetChannelConfig(string connectionName)
        {
            if (_channelConfigInstance == null)
            {
                lock (_channelConfigLockObj)
                {
                    if (_channelConfigInstance == null)
                    {
                        var db = DbConfig.GetDbInfo(connectionName);
                        var type = Type.GetType(
                            $"ZSN.AI.DAL." + db.DbType + ".ChannelConfigManage, ZSN.AI.DAL." + db.DbType,
                            false, true);
                        var provider = (IChannelConfigManage)Activator.CreateInstance(type);
                        provider.SetConnectionName(connectionName);
                        _channelConfigInstance = provider;
                    }
                }
            }
            return _channelConfigInstance;
        }
    }
}
