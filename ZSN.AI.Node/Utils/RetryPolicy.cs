using System;
using System.Threading.Tasks;

namespace ZSN.AI.Node.Utils
{
    /// <summary>
    /// 重试策略
    /// 提供统一的重试机制，用于处理临时性错误
    /// </summary>
    public static class RetryPolicy
    {
        /// <summary>
        /// 同步执行带重试的操作
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="action">要执行的操作</param>
        /// <param name="maxRetries">最大重试次数（默认3次）</param>
        /// <param name="delayMs">重试延迟毫秒数（默认1000ms）</param>
        /// <param name="onRetry">重试时的回调（可选）</param>
        /// <returns>操作结果</returns>
        public static T Execute<T>(
            Func<T> action,
            int maxRetries = 3,
            int delayMs = 1000,
            Action<Exception, int>? onRetry = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Exception? lastException = null;

            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    return action();
                }
                catch (Exception ex) when (i < maxRetries)
                {
                    lastException = ex;
                    onRetry?.Invoke(ex, i + 1);

                    // 指数退避策略
                    int delay = delayMs * (i + 1);
                    Task.Delay(delay).Wait();
                }
            }

            throw new RetryFailedException(
                $"操作在 {maxRetries + 1} 次尝试后失败",
                lastException);
        }

        /// <summary>
        /// 异步执行带重试的操作
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="maxRetries">最大重试次数（默认3次）</param>
        /// <param name="delayMs">重试延迟毫秒数（默认1000ms）</param>
        /// <param name="onRetry">重试时的回调（可选）</param>
        /// <returns>操作结果</returns>
        public static async Task<T> ExecuteAsync<T>(
            Func<Task<T>> action,
            int maxRetries = 3,
            int delayMs = 1000,
            Action<Exception, int>? onRetry = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Exception? lastException = null;

            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (i < maxRetries)
                {
                    lastException = ex;
                    onRetry?.Invoke(ex, i + 1);

                    // 指数退避策略
                    int delay = delayMs * (i + 1);
                    await Task.Delay(delay);
                }
            }

            throw new RetryFailedException(
                $"异步操作在 {maxRetries + 1} 次尝试后失败",
                lastException);
        }

        /// <summary>
        /// 异步执行带重试的操作（无返回值）
        /// </summary>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="maxRetries">最大重试次数（默认3次）</param>
        /// <param name="delayMs">重试延迟毫秒数（默认1000ms）</param>
        /// <param name="onRetry">重试时的回调（可选）</param>
        public static async Task ExecuteAsync(
            Func<Task> action,
            int maxRetries = 3,
            int delayMs = 1000,
            Action<Exception, int>? onRetry = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Exception? lastException = null;

            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex) when (i < maxRetries)
                {
                    lastException = ex;
                    onRetry?.Invoke(ex, i + 1);

                    // 指数退避策略
                    int delay = delayMs * (i + 1);
                    await Task.Delay(delay);
                }
            }

            throw new RetryFailedException(
                $"异步操作在 {maxRetries + 1} 次尝试后失败",
                lastException);
        }

        /// <summary>
        /// 执行带特定条件重试的操作
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="action">要执行的操作</param>
        /// <param name="shouldRetry">判断是否应该重试的函数</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="delayMs">重试延迟毫秒数</param>
        /// <returns>操作结果</returns>
        public static async Task<T> ExecuteWithConditionAsync<T>(
            Func<Task<T>> action,
            Func<Exception, bool> shouldRetry,
            int maxRetries = 3,
            int delayMs = 1000)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (shouldRetry == null) throw new ArgumentNullException(nameof(shouldRetry));

            Exception? lastException = null;

            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (i < maxRetries && shouldRetry(ex))
                {
                    lastException = ex;
                    int delay = delayMs * (i + 1);
                    await Task.Delay(delay);
                }
            }

            throw new RetryFailedException(
                $"操作在 {maxRetries + 1} 次尝试后失败",
                lastException);
        }

        /// <summary>
        /// 断路器模式 - 当连续失败达到阈值后，暂停一段时间再尝试
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="action">要执行的操作</param>
        /// <param name="failureThreshold">失败阈值（默认5次）</param>
        /// <param name="resetTimeoutMs">重置超时时间（默认60000ms = 1分钟）</param>
        /// <returns>操作结果</returns>
        public static async Task<T> ExecuteCircuitBreakerAsync<T>(
            Func<Task<T>> action,
            int failureThreshold = 5,
            int resetTimeoutMs = 60000)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var circuitBreaker = new CircuitBreakerState(failureThreshold, resetTimeoutMs);

            if (circuitBreaker.IsOpen)
            {
                if (circuitBreaker.ShouldAttemptReset)
                {
                    circuitBreaker.Reset();
                }
                else
                {
                    throw new CircuitBreakerOpenException(
                        $"断路器已打开，请在 {circuitBreaker.RemainingTimeMs}ms 后重试");
                }
            }

            try
            {
                var result = await action();
                circuitBreaker.RecordSuccess();
                return result;
            }
            catch (Exception ex)
            {
                circuitBreaker.RecordFailure();
                throw;
            }
        }

        /// <summary>
        /// 超时重试 - 在指定时间内尽可能多次重试
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="action">要执行的操作</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="minDelayMs">最小延迟（毫秒）</param>
        /// <returns>操作结果</returns>
        public static async Task<T> ExecuteWithTimeoutAsync<T>(
            Func<Task<T>> action,
            int timeoutMs,
            int minDelayMs = 100)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var startTime = DateTime.UtcNow;
            Exception? lastException = null;

            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    await Task.Delay(minDelayMs);
                }
            }

            throw new TimeoutException(
                $"操作在 {timeoutMs}ms 内未成功完成",
                lastException);
        }

        #region 内部类

        /// <summary>
        /// 断路器状态
        /// </summary>
        private class CircuitBreakerState
        {
            private readonly int _failureThreshold;
            private readonly int _resetTimeoutMs;
            private int _failureCount;
            private DateTime _lastFailureTime;

            public CircuitBreakerState(int failureThreshold, int resetTimeoutMs)
            {
                _failureThreshold = failureThreshold;
                _resetTimeoutMs = resetTimeoutMs;
                _failureCount = 0;
                _lastFailureTime = DateTime.MinValue;
            }

            public bool IsOpen => _failureCount >= _failureThreshold;

            public bool ShouldAttemptReset =>
                IsOpen && (DateTime.UtcNow - _lastFailureTime).TotalMilliseconds >= _resetTimeoutMs;

            public long RemainingTimeMs =>
                IsOpen ? Math.Max(0, _resetTimeoutMs - (long)(DateTime.UtcNow - _lastFailureTime).TotalMilliseconds) : 0;

            public void RecordSuccess()
            {
                _failureCount = 0;
            }

            public void RecordFailure()
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;
            }

            public void Reset()
            {
                _failureCount = 0;
                _lastFailureTime = DateTime.MinValue;
            }
        }

        #endregion
    }

    /// <summary>
    /// 重试失败异常
    /// </summary>
    public class RetryFailedException : Exception
    {
        public RetryFailedException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 断路器打开异常
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message)
        {
        }
    }
}
