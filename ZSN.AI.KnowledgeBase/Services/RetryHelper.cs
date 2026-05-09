using Microsoft.Extensions.Logging;
using System.Threading;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 重试策略配置
    /// </summary>
    public class RetryOptions
    {
        /// <summary>
        /// 最大重试次数（默认3次）
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 初始重试延迟（毫秒，默认1000ms）
        /// </summary>
        public int InitialDelayMs { get; set; } = 1000;

        /// <summary>
        /// 重试延迟倍数（默认2，即指数退避）
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// 最大重试延迟（毫秒，默认10000ms）
        /// </summary>
        public int MaxDelayMs { get; set; } = 10000;

        /// <summary>
        /// 是否在重试时逐步提高Temperature
        /// </summary>
        public bool IncreaseTemperatureOnRetry { get; set; } = true;

        /// <summary>
        /// Temperature增量（每次重试增加0.1）
        /// </summary>
        public double TemperatureIncrement { get; set; } = 0.1;
    }

    /// <summary>
    /// 重试结果
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    public class RetryResult<T>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 结果数据
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 尝试次数
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 最后一次异常
        /// </summary>
        public Exception? LastException { get; set; }
    }

    /// <summary>
    /// 重试辅助类
    /// 提供带重试机制的异步执行功能
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// 执行带重试机制的异步操作
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="operation">要执行的异步操作</param>
        /// <param name="options">重试选项</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重试结果</returns>
        public static async Task<RetryResult<T>> ExecuteWithRetryAsync<T>(
            Func<int, double, Task<T>> operation,
            RetryOptions? options = null,
            ILogger? logger = null,
            string? operationName = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new RetryOptions();
            var attempts = 0;
            var currentTemperature = 0.0;
            Exception? lastException = null;
            string? errorMessage = null;

            for (int attempt = 0; attempt <= options.MaxRetries; attempt++)
            {
                attempts = attempt + 1;
                var delayMs = CalculateDelay(attempt, options);

                try
                {
                    if (attempt > 0)
                    {
                        logger?.LogWarning("{OperationName} 第 {Attempt} 次重试，延迟 {DelayMs}ms，Temperature: {Temperature}",
                            operationName ?? "操作",
                            attempt,
                            delayMs,
                            currentTemperature.ToString("F1"));

                        // 等待后再重试
                        await Task.Delay(delayMs, cancellationToken);
                    }

                    // 执行操作（传入尝试次数和当前Temperature）
                    var result = await operation(attempt, currentTemperature);

                    // 成功执行
                    logger?.LogInformation("{OperationName} 成功完成，尝试次数: {Attempts}",
                        operationName ?? "操作", attempts);

                    return new RetryResult<T>
                    {
                        IsSuccess = true,
                        Data = result,
                        Attempts = attempts
                    };
                }
                catch (OperationCanceledException)
                {
                    // 取消操作，不重试
                    logger?.LogWarning("{OperationName} 被取消", operationName ?? "操作");
                    return new RetryResult<T>
                    {
                        IsSuccess = false,
                        Attempts = attempts,
                        ErrorMessage = "操作被取消",
                        LastException = new OperationCanceledException()
                    };
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    errorMessage = ex.Message;

                    logger?.LogError(ex, "{OperationName} 第 {Attempt} 次尝试失败: {Message}",
                        operationName ?? "操作", attempts, ex.Message);

                    // 如果是最后一次尝试，不再继续
                    if (attempt >= options.MaxRetries)
                    {
                        logger?.LogError("{OperationName} 已达到最大重试次数 {MaxRetries}，放弃重试",
                            operationName ?? "操作", options.MaxRetries);
                        break;
                    }

                    // 逐步提高Temperature（如果启用）
                    if (options.IncreaseTemperatureOnRetry)
                    {
                        currentTemperature += options.TemperatureIncrement;
                        // 限制最大Temperature为1.0
                        if (currentTemperature > 1.0)
                            currentTemperature = 1.0;
                    }
                }
            }

            // 所有尝试都失败
            return new RetryResult<T>
            {
                IsSuccess = false,
                Attempts = attempts,
                ErrorMessage = $"操作在{attempts}次尝试后失败: {errorMessage}",
                LastException = lastException
            };
        }

        /// <summary>
        /// 执行带重试机制的异步操作（无返回值）
        /// </summary>
        /// <param name="operation">要执行的异步操作</param>
        /// <param name="options">重试选项</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重试结果</returns>
        public static async Task<RetryResult<bool>> ExecuteWithRetryAsync(
            Func<int, double, Task> operation,
            RetryOptions? options = null,
            ILogger? logger = null,
            string? operationName = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new RetryOptions();

            var result = await ExecuteWithRetryAsync(async (attempt, temp) =>
            {
                await operation(attempt, temp);
                return true;
            }, options, logger, operationName, cancellationToken);

            return new RetryResult<bool>
            {
                IsSuccess = result.IsSuccess,
                Data = result.IsSuccess,
                Attempts = result.Attempts,
                ErrorMessage = result.ErrorMessage,
                LastException = result.LastException
            };
        }

        /// <summary>
        /// 计算重试延迟时间（指数退避）
        /// </summary>
        /// <param name="attempt">尝试次数</param>
        /// <param name="options">重试选项</param>
        /// <returns>延迟时间（毫秒）</returns>
        private static int CalculateDelay(int attempt, RetryOptions options)
        {
            if (attempt == 0)
                return 0;

            // 指数退避：delay = initialDelay * (backoffMultiplier ^ (attempt - 1))
            var delay = (int)(options.InitialDelayMs * Math.Pow(options.BackoffMultiplier, attempt - 1));

            // 限制最大延迟
            return Math.Min(delay, options.MaxDelayMs);
        }

        /// <summary>
        /// 创建默认的实体提取重试选项
        /// </summary>
        public static RetryOptions CreateEntityExtractionRetryOptions()
        {
            return new RetryOptions
            {
                MaxRetries = 3,
                InitialDelayMs = 1000,
                BackoffMultiplier = 2.0,
                MaxDelayMs = 8000,
                IncreaseTemperatureOnRetry = true,
                TemperatureIncrement = 0.1
            };
        }

        /// <summary>
        /// 创建默认的关系抽取重试选项
        /// </summary>
        public static RetryOptions CreateRelationExtractionRetryOptions()
        {
            return new RetryOptions
            {
                MaxRetries = 3,
                InitialDelayMs = 1500,
                BackoffMultiplier = 2.0,
                MaxDelayMs = 10000,
                IncreaseTemperatureOnRetry = true,
                TemperatureIncrement = 0.15
            };
        }
    }
}
