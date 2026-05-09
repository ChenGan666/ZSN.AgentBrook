using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZSN.AI.Entity;

namespace ZSN.AI.Node.Claw.Pipeline
{
    /// <summary>
    /// 执行记录更新节流器 (P1优化)
    /// 替代每次日志变更都调用 updateExcutionRecord，改为定时/手动刷新
    /// P1修复: 支持 ConcurrentQueue<string> 日志
    /// </summary>
    public class RecordUpdateThrottler : IDisposable
    {
        private readonly string _recordId;
        private readonly List<Output> _outputs;
        private readonly ConcurrentQueue<string> _logs;
        private readonly Action<string, ExecutionRecordStatus, List<Output>, List<string>> _updateAction;
        private readonly Timer _timer;
        private readonly int _intervalMs;

        private volatile bool _isDirty;
        private ExecutionRecordStatus _currentStatus = ExecutionRecordStatus.Running;
        private int _dirtyCount;
        private bool _disposed;

        /// <summary>
        /// 每N次 MarkDirty 自动触发一次写入
        /// </summary>
        private const int AutoFlushThreshold = 3;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="recordId">记录ID</param>
        /// <param name="outputs">输出列表引用</param>
        /// <param name="logs">日志队列引用 (ConcurrentQueue)</param>
        /// <param name="updateAction">实际写入方法</param>
        /// <param name="intervalMs">定时刷新间隔(毫秒)，默认500ms</param>
        public RecordUpdateThrottler(
            string recordId,
            List<Output> outputs,
            ConcurrentQueue<string> logs,
            Action<string, ExecutionRecordStatus, List<Output>, List<string>> updateAction,
            int intervalMs = 500)
        {
            _recordId = recordId;
            _outputs = outputs;
            _logs = logs;
            _updateAction = updateAction;
            _intervalMs = intervalMs;
            _timer = new Timer(OnTimerTick, null, _intervalMs, _intervalMs);
        }

        /// <summary>
        /// 标记数据已变更，需要更新 (替代直接调用 updateExcutionRecord)
        /// </summary>
        public void MarkDirty()
        {
            if (_disposed) return;
            _isDirty = true;
            var count = Interlocked.Increment(ref _dirtyCount);

            // 累积超过阈值时立即写入
            if (count >= AutoFlushThreshold)
            {
                FlushInternal();
            }
        }

        /// <summary>
        /// 强制刷新到数据库 (阶段切换、关键节点时调用)
        /// </summary>
        public void Flush()
        {
            FlushInternal();
        }

        /// <summary>
        /// 带状态刷新 (用于最终状态如 Success/Failed)
        /// </summary>
        public void FlushWithStatus(ExecutionRecordStatus status)
        {
            _currentStatus = status;
            FlushInternal();
        }

        private void FlushInternal()
        {
            if (_disposed) return;
            _isDirty = false;
            Interlocked.Exchange(ref _dirtyCount, 0);

            try
            {
                // P1修复: 将 ConcurrentQueue 转换为 List
                var logsList = _logs.ToList();
                _updateAction(_recordId, _currentStatus, _outputs, logsList);
            }
            catch
            {
                // 写入失败不影响主流程
            }
        }

        private void OnTimerTick(object state)
        {
            if (_isDirty && !_disposed)
            {
                FlushInternal();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 停止定时器
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();

            // 最后刷新一次，确保数据不丢
            FlushInternal();
        }
    }
}
