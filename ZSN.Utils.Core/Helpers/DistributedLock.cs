using System;
using StackExchange.Redis;

namespace ZSN.Utils.Core.Helpers
{
    public class DistributedLock
    {
        private readonly IConnectionMultiplexer _mux;
        public DistributedLock()
        {
            // Reuse the same multiplexer from RedisHelper
            var redis = new RedisHelper();
            _mux = redis.GetConnectionRedisMultiplexer();
        }

        private static string AddKeyPrefix(string key)
        {
            var prefix = RedisHelper.DefaultKey ?? string.Empty;
            return string.IsNullOrEmpty(prefix) ? key : $"{prefix}:{key}";
        }

        public bool TryAcquire(string key, string value, TimeSpan expiry)
        {
            var db = _mux.GetDatabase();
            key = AddKeyPrefix(key);
            return db.StringSet(key, value, expiry, When.NotExists, CommandFlags.DemandMaster);
        }

        public bool Release(string key, string value)
        {
            var db = _mux.GetDatabase();
            key = AddKeyPrefix(key);
            const string script = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
            var result = (int)db.ScriptEvaluate(script, new RedisKey[] { key }, new RedisValue[] { value });
            return result == 1;
        }
    }
}
