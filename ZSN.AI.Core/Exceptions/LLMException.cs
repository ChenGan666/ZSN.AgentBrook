using System;

namespace ZSN.AI.Core.Exceptions
{
    /// <summary>
    /// LLM 调用异常（HTTP 错误、权限、限流、账户欠费等）。
    /// 区别于"模型返回了格式错误的 JSON"（可重试 / 可降级），
    /// 致命异常（<see cref="IsFatal"/>) 通常不可恢复，不应盲目重试或降级。
    /// </summary>
    public class LLMException : Exception
    {
        /// <summary>HTTP 状态码（0 表示非 HTTP 错误或无法解析）</summary>
        public int StatusCode { get; }

        /// <summary>
        /// 是否为不可恢复错误（401/403/账户欠费/key 无效等）。
        /// 致命错误时调用方应快速失败并通知用户，而非退化为重试 / 工作流计划。
        /// </summary>
        public bool IsFatal { get; }

        public LLMException(string message, int statusCode = 0, bool isFatal = false, Exception inner = null)
            : base(message, inner)
        {
            StatusCode = statusCode;
            IsFatal = isFatal;
        }

        /// <summary>
        /// 判断一段响应文本是否为 ChatService 历史遗留的错误包装字符串
        /// （形如 "生成回答时发生错误：HTTP 403 ..."）。
        /// 用于对未走 throw 路径的残留调用方做防御性检测。
        /// </summary>
        public static bool IsWrappedErrorResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return false;
            return response.StartsWith("生成回答时发生错误", StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断错误信息是否代表不可恢复（致命）错误。
        /// 检测常见的权限 / 鉴权 / 欠费关键词。
        /// </summary>
        public static bool IsFatalErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;
            // 统一小写比较英文关键词
            var lower = message.ToLowerInvariant();
            string[] fatalKeywords =
            {
                "permission_error",
                "forbidden",
                "account overdue",
                "unpaid order",
                "invalid_api_key",
                "invalid api key",
                "incorrect api key",
                "authentication",
                "unauthorized",
                "401",
                "403",
                "over quota",
                "exceeded your current quota",
                "access denied"
            };
            foreach (var kw in fatalKeywords)
            {
                if (lower.Contains(kw.ToLowerInvariant())) return true;
            }
            return false;
        }
    }
}
