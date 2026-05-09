using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.Service.Helpers
{
    public enum StreamMsgType
    {
        delta,
        done,
        error
    }

    public sealed class StreamMsg
    {
        public string type { get; set; }
        public string content { get; set; }
        public long timestamp { get; set; }
    }

    public static class StreamKey
    {
        public static string Build(string sessionId, string processesId)
            => $"stream:session:{sessionId}:{processesId}";
    }

    public sealed class RedisStreamSync
    {
        private readonly RedisHelper _redis;
        private readonly IDatabase _db;
        private readonly bool _supportStreams;

        public RedisStreamSync(RedisHelper redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _db = _redis.GetConnectionRedisMultiplexer().GetDatabase();
            _supportStreams = true;
        }

        public sealed class StreamEnvelope
        {
            public string sessionId { get; set; }
            public string processesId { get; set; }
            public string taskId { get; set; }
            public string nodeId { get; set; }
            public string type { get; set; }
            public string content { get; set; }
            public long timestamp { get; set; }

            public static string Serialize(StreamEnvelope e) => JsonConvert.SerializeObject(e);
            public static StreamEnvelope Deserialize(string json) => JsonConvert.DeserializeObject<StreamEnvelope>(json);
        }

        private static string JsonEnvelope(
            StreamMsgType type,
            string sessionId,
            string processesId,
            string taskId,
            string nodeId,
            string content = null)
            => StreamEnvelope.Serialize(new StreamEnvelope
            {
                sessionId = sessionId,
                processesId = processesId,
                taskId = taskId,
                nodeId = nodeId,
                type = type.ToString(),
                content = content,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

        public void AppendDelta(
            string key,
            string sessionId,
            string processesId,
            string taskId,
            string nodeId,
            string delta,
            int approxMaxLen = 10000)
        {
            if (string.IsNullOrEmpty(delta)) return;
            var payload = JsonEnvelope(StreamMsgType.delta, sessionId, processesId, taskId, nodeId, delta);
            if (_supportStreams)
            {
                _db.StreamAdd(key, new[] { new NameValueEntry("body", payload) }, maxLength: approxMaxLen, useApproximateMaxLength: true);
            }
            else
            {
                _db.ListRightPush(key, payload);
                _db.ListTrim(key, -approxMaxLen, -1);
            }
        }

        public void AppendDone(
            string key,
            string sessionId,
            string processesId,
            string taskId,
            string nodeId,
            TimeSpan? ttl = null,
            int approxMaxLen = 10000)
        {
            var payload = JsonEnvelope(StreamMsgType.done, sessionId, processesId, taskId, nodeId, null);
            if (_supportStreams)
            {
                _db.StreamAdd(key, new[] { new NameValueEntry("body", payload) }, maxLength: approxMaxLen, useApproximateMaxLength: true);
            }
            else
            {
                _db.ListRightPush(key, payload);
                _db.ListTrim(key, -approxMaxLen, -1);
            }
            if (ttl.HasValue) _db.KeyExpire(key, ttl.Value);
        }

        public void AppendError(
            string key,
            string sessionId,
            string processesId,
            string taskId,
            string nodeId,
            string error,
            TimeSpan? ttl = null,
            int approxMaxLen = 10000)
        {
            var payload = JsonEnvelope(StreamMsgType.error, sessionId, processesId, taskId, nodeId, error);
            if (_supportStreams)
            {
                _db.StreamAdd(key, new[] { new NameValueEntry("body", payload) }, maxLength: approxMaxLen, useApproximateMaxLength: true);
            }
            else
            {
                _db.ListRightPush(key, payload);
                _db.ListTrim(key, -approxMaxLen, -1);
            }
            if (ttl.HasValue) _db.KeyExpire(key, ttl.Value);
        }

        public async Task AppendDeltaAsync(
            string key,
            string sessionId,
            string processesId,
            string taskId,
            string nodeId,
            string delta,
            int approxMaxLen = 10000)
        {
            if (string.IsNullOrEmpty(delta)) return;
            var payload = JsonEnvelope(StreamMsgType.delta, sessionId, processesId, taskId, nodeId, delta);
            if (_supportStreams)
            {
                await _db.StreamAddAsync(key, new[] { new NameValueEntry("body", payload) }, maxLength: approxMaxLen, useApproximateMaxLength: true);
            }
            else
            {
                _db.ListRightPush(key, payload);
                _db.ListTrim(key, -approxMaxLen, -1);
            }
        }

        public async Task AppendDoneAsync(
            string key,
            string sessionId,
            string processesId,
            string taskId,
            string nodeId,
            TimeSpan? ttl = null,
            int approxMaxLen = 10000)
        {
            var payload = JsonEnvelope(StreamMsgType.done, sessionId, processesId, taskId, nodeId, null);
            if (_supportStreams)
            {
                await _db.StreamAddAsync(key, new[] { new NameValueEntry("body", payload) }, maxLength: approxMaxLen, useApproximateMaxLength: true);
            }
            else
            {
                _db.ListRightPush(key, payload);
                _db.ListTrim(key, -approxMaxLen, -1);
            }
            if (ttl.HasValue) _db.KeyExpire(key, ttl.Value);
        }

        public async Task AppendErrorAsync(
            string key,
            string sessionId,
            string processesId,
            string taskId,
            string nodeId,
            string error,
            TimeSpan? ttl = null,
            int approxMaxLen = 10000)
        {
            var payload = JsonEnvelope(StreamMsgType.error, sessionId, processesId, taskId, nodeId, error);
            if (_supportStreams)
            {
                await _db.StreamAddAsync(key, new[] { new NameValueEntry("body", payload) }, maxLength: approxMaxLen, useApproximateMaxLength: true);
            }
            else
            {
                _db.ListRightPush(key, payload);
                _db.ListTrim(key, -approxMaxLen, -1);
            }
            if (ttl.HasValue) _db.KeyExpire(key, ttl.Value);
        }

        public async IAsyncEnumerable<StreamMsg> ConsumeAsync(
            string key,
            string lastId = "0-0",
            int blockMs = 200,
            [EnumeratorCancellation] CancellationToken ct = default
        )
        {
            if (_supportStreams)
            {
                var id = lastId;
                while (!ct.IsCancellationRequested)
                {
                    var entries = _db.StreamRead(key, id == "$" ? "0-0" : id);
                    if (entries != null && entries.Length > 0)
                    {
                        foreach (var e in entries)
                        {
                            id = e.Id;
                            if (e.Values.Length > 0)
                            {
                                for (int i = 0; i < e.Values.Length; i++)
                                {
                                    if (e.Values[i].Name == "body")
                                    {
                                        var json = (string)e.Values[i].Value;
                                        StreamMsg msg = null;
                                        try { msg = JsonConvert.DeserializeObject<StreamMsg>(json); } catch { }
                                        if (msg != null) yield return msg;
                                    }
                                }
                            }
                        }
                    }
                    await Task.Delay(blockMs, ct);
                }
            }
            else
            {
                long index = 0;
                while (!ct.IsCancellationRequested)
                {
                    var len = _db.ListLength(key);
                    if (len > index)
                    {
                        var range = _db.ListRange(key, index, len - 1);
                        index = len;
                        foreach (var v in range)
                        {
                            StreamMsg msg = null;
                            try { msg = JsonConvert.DeserializeObject<StreamMsg>(v); } catch { }
                            if (msg != null) yield return msg;
                        }
                    }
                    await Task.Delay(blockMs, ct);
                }
            }
        }
    }
}
