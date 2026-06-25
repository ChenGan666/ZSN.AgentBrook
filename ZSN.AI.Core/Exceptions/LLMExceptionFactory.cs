using System;

namespace ZSN.AI.Core.Exceptions
{
    /// <summary>
    /// 从底层异常（Semantic Kernel / OpenAI SDK / HttpRequestException 等）
    /// 构造 <see cref="LLMException"/>，解析 HTTP 状态码与是否致命。
    /// 使用反射探测不同 SDK 的异常属性，避免对具体类型的硬依赖。
    /// </summary>
    public static class LLMExceptionFactory
    {
        /// <summary>
        /// 从任意异常构造 LLMException。
        /// 若本身就是 LLMException 则原样返回。
        /// </summary>
        public static LLMException FromException(Exception ex)
        {
            if (ex is LLMException llm) return llm;

            string message = ex?.Message ?? "未知 LLM 错误";
            int statusCode = TryGetStatusCode(ex);
            bool isFatal = LLMException.IsFatalErrorMessage(message)
                || statusCode == 401
                || statusCode == 403;

            return new LLMException(message, statusCode, isFatal, ex);
        }

        /// <summary>
        /// 反射探测异常中的 HTTP 状态码。
        /// 兼容 HttpRequestException.StatusCode、HttpOperationException (SemanticKernel) 等。
        /// </summary>
        private static int TryGetStatusCode(Exception ex)
        {
            if (ex == null) return 0;
            try
            {
                var type = ex.GetType();

                // 1) HttpRequestException.StatusCode (int?)
                if (type.GetProperty("StatusCode") != null)
                {
                    var val = type.GetProperty("StatusCode").GetValue(ex);
                    if (val != null)
                    {
                        var codeObj = val.GetType().GetProperty("Value")?.GetValue(val) ?? val;
                        return ToInt(codeObj);
                    }
                }

                // 2) SemanticKernel HttpOperationException.ResponseStatusCode (HttpStatusCode)
                if (type.GetProperty("ResponseStatusCode") != null)
                {
                    var val = type.GetProperty("ResponseStatusCode").GetValue(ex);
                    return ToInt(val);
                }

                // 3) 递归 InnerException
                if (ex.InnerException != null)
                {
                    return TryGetStatusCode(ex.InnerException);
                }
            }
            catch
            {
                // 反射失败时忽略，返回 0
            }
            return 0;
        }

        /// <summary>
        /// 将反射取到的状态码对象（int / 可空 int / 枚举）转换为 int。
        /// 使用 Convert.ToInt32 兼容枚举与数值类型。
        /// </summary>
        private static int ToInt(object val)
        {
            if (val == null) return 0;
            if (val is int i) return i;
            try { return Convert.ToInt32(val); }
            catch { return 0; }
        }
    }
}
