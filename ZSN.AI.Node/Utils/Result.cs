using System;

namespace ZSN.AI.Node.Utils
{
    /// <summary>
    /// Result 类型 - 用于函数式错误处理
    /// 避免异常抛出，使错误处理更加显式和可控
    /// </summary>
    /// <typeparam name="T">成功值的类型</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// 是否失败
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// 成功时的值
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// 失败时的错误信息
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// 失败时的异常对象
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private Result(bool isSuccess, T? value, string? error, Exception? exception)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            Exception = exception;
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, null, null);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static Result<T> Failure(string error, Exception? exception = null)
        {
            return new Result<T>(false, default, error, exception);
        }

        /// <summary>
        /// 从异常创建失败结果
        /// </summary>
        public static Result<T> FromException(Exception ex)
        {
            return new Result<T>(false, default, ex.Message, ex);
        }

        /// <summary>
        /// Map 操作 - 转换成功值
        /// </summary>
        public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        {
            return IsSuccess
                ? Result<TOut>.Success(mapper(Value!))
                : Result<TOut>.Failure(Error!, Exception);
        }

        /// <summary>
        /// Bind 操作 - 链接可能失败的操作
        /// </summary>
        public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
        {
            return IsSuccess
                ? binder(Value!)
                : Result<TOut>.Failure(Error!, Exception);
        }

        /// <summary>
        /// Fold 操作 - 无论成功失败都返回同一类型
        /// </summary>
        public TOut Fold<TOut>(
            Func<T, TOut> onSuccess,
            Func<string, Exception?, TOut> onFailure)
        {
            return IsSuccess
                ? onSuccess(Value!)
                : onFailure(Error!, Exception);
        }

        /// <summary>
        /// 获取值或默认值
        /// </summary>
        public T GetValueOrDefault(T defaultValue)
        {
            return IsSuccess ? Value! : defaultValue;
        }

        /// <summary>
        /// 获取值或执行函数生成默认值
        /// </summary>
        public T GetValueOrDefault(Func<T> defaultValueFactory)
        {
            return IsSuccess ? Value! : defaultValueFactory();
        }

        /// <summary>
        /// 匹配操作
        /// </summary>
        public void Match(
            Action<T> onSuccess,
            Action<string, Exception?> onFailure)
        {
            if (IsSuccess)
            {
                onSuccess(Value!);
            }
            else
            {
                onFailure(Error!, Exception);
            }
        }

        /// <summary>
        /// 隐式转换 - 从 T 到 Result<T>
        /// </summary>
        public static implicit operator Result<T>(T value)
        {
            return Success(value);
        }

        /// <summary>
        /// 重写 ToString
        /// </summary>
        public override string ToString()
        {
            return IsSuccess
                ? $"Success({Value})"
                : $"Failure({Error})";
        }
    }

    /// <summary>
    /// 无返回值的 Result
    /// </summary>
    public class Result
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// 是否失败
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// 失败时的错误信息
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// 失败时的异常对象
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private Result(bool isSuccess, string? error, Exception? exception)
        {
            IsSuccess = isSuccess;
            Error = error;
            Exception = exception;
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static Result Success()
        {
            return new Result(true, null, null);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static Result Failure(string error, Exception? exception = null)
        {
            return new Result(false, error, exception);
        }

        /// <summary>
        /// 从异常创建失败结果
        /// </summary>
        public static Result FromException(Exception ex)
        {
            return new Result(false, ex.Message, ex);
        }

        /// <summary>
        /// Fold 操作
        /// </summary>
        public T Fold<T>(
            Func<T> onSuccess,
            Func<string, Exception?, T> onFailure)
        {
            return IsSuccess
                ? onSuccess()
                : onFailure(Error!, Exception);
        }

        /// <summary>
        /// 匹配操作
        /// </summary>
        public void Match(
            Action onSuccess,
            Action<string, Exception?> onFailure)
        {
            if (IsSuccess)
            {
                onSuccess();
            }
            else
            {
                onFailure(Error!, Exception);
            }
        }

        /// <summary>
        /// 重写 ToString
        /// </summary>
        public override string ToString()
        {
            return IsSuccess ? "Success" : $"Failure({Error})";
        }
    }

    /// <summary>
    /// Result 扩展方法
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// 将多个 Result 组合，只有全部成功才成功
        /// </summary>
        public static Result<(T1, T2)> Combine<T1, T2>(
            this Result<T1> result1,
            Result<T2> result2)
        {
            if (result1.IsFailure)
                return Result<(T1, T2)>.Failure(result1.Error!, result1.Exception);

            if (result2.IsFailure)
                return Result<(T1, T2)>.Failure(result2.Error!, result2.Exception);

            return Result<(T1, T2)>.Success((result1.Value!, result2.Value!));
        }

        /// <summary>
        /// 将多个 Result 组合，只有全部成功才成功
        /// </summary>
        public static Result<(T1, T2, T3)> Combine<T1, T2, T3>(
            this Result<T1> result1,
            Result<T2> result2,
            Result<T3> result3)
        {
            if (result1.IsFailure)
                return Result<(T1, T2, T3)>.Failure(result1.Error!, result1.Exception);

            if (result2.IsFailure)
                return Result<(T1, T2, T3)>.Failure(result2.Error!, result2.Exception);

            if (result3.IsFailure)
                return Result<(T1, T2, T3)>.Failure(result3.Error!, result3.Exception);

            return Result<(T1, T2, T3)>.Success((result1.Value!, result2.Value!, result3.Value!));
        }

        /// <summary>
        /// 序列中的第一个成功结果，或全部失败
        /// </summary>
        public static Result<T> FirstSuccess<T>(this Result<T>[] results)
        {
            foreach (var result in results)
            {
                if (result.IsSuccess)
                    return result;
            }

            return Result<T>.Failure(
                "所有操作都失败了",
                results[0].Exception);
        }

        /// <summary>
        /// 将 Result 转换为异步 Result
        /// </summary>
        public static async Task<Result<T>> AsTask<T>(this Result<T> result)
        {
            return await Task.FromResult(result);
        }
    }
}
