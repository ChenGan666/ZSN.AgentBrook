using System;
using System.Text;
using System.Threading;
using ZSN.AI.Service.Helpers;

namespace ZSN.AI.Node.Utils.Pipeline
{
    /// <summary>
    /// 流式输出批量写入器 (P2优化)
    /// 将多条 delta 合并为一次 Redis 写入，减少网络往返
    /// </summary>
    public class StreamBatchWriter : IDisposable
    {
        private readonly RedisStreamSync _streamSync;
        private readonly string _streamKey;
        private readonly string _sessionId;
        private readonly string _processesId;
        private readonly string _taskId;
        private readonly string _nodeId;
        private readonly Timer _timer;
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly object _lock = new object();
        private bool _disposed;

        /// <summary>
        ///
        /// </summary>
        /// <param name="streamSync">Redis流同步器</param>
        /// <param name="streamKey">流Key</param>
        /// <param name="sessionId">会话ID</param>
        /// <param name="processesId">流程ID</param>
        /// <param name="taskId">任务ID</param>
        /// <param name="nodeId">节点ID</param>
        /// <param name="intervalMs">定时刷新间隔(毫秒)，默认200ms</param>
        public StreamBatchWriter(
            RedisStreamSync streamSync,
            string streamKey,
            string sessionId,
            string processesId,
            string taskId,
            string nodeId,
            int intervalMs = 200)
        {
            _streamSync = streamSync;
            _streamKey = streamKey;
            _sessionId = sessionId;
            _processesId = processesId;
            _taskId = taskId;
            _nodeId = nodeId;
            _timer = new Timer(OnTimerTick, null, intervalMs, intervalMs);
        }

        /// <summary>
        /// 追加一条消息到缓冲区 (线程安全)
        /// </summary>
        public void Append(string message)
        {
            if (_disposed || string.IsNullOrEmpty(message)) return;
            lock (_lock)
            {
                _buffer.Append(message);
            }
        }

        /// <summary>
        /// 强制刷新缓冲区到 Redis
        /// </summary>
        public void Flush()
        {
            string content;
            lock (_lock)
            {
                if (_buffer.Length == 0) return;
                content = _buffer.ToString();
                _buffer.Clear();
            }

            try
            {
                _ = _streamSync.AppendDeltaAsync(
                    _streamKey, _sessionId, _processesId, _taskId, _nodeId, content);
            }
            catch
            {
                // Redis写入失败不影响主流程
            }
        }

        private void OnTimerTick(object state)
        {
            if (!_disposed)
            {
                Flush();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
            Flush(); // 最后刷新一次
        }
    }
}
