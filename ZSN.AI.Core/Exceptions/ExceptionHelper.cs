using System;

namespace ZSN.AI.Core.Exceptions
{
    /// <summary>
    /// 异常处理辅助工具。
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// 从异常链中提取致命的 <see cref="LLMException"/>。
        /// 用于穿透重试策略（如 RetryFailedException）的包装，检测底层 LLM 错误。
        /// 返回 null 表示异常链中无致命 LLM 错误。
        /// </summary>
        public static LLMException ExtractFatalLLMException(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current is LLMException llm && llm.IsFatal)
                {
                    return llm;
                }
            }
            return null;
        }

        /// <summary>
        /// 从异常链中提取任意 <see cref="LLMException"/>（不区分是否致命）。
        /// </summary>
        public static LLMException ExtractLLMException(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current is LLMException llm)
                {
                    return llm;
                }
            }
            return null;
        }
    }
}
