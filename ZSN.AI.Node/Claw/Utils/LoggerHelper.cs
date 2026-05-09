using Microsoft.Extensions.Logging;

namespace ZSN.AI.Node.Claw.Utils
{
    /// <summary>
    /// 日志助手类 - 提供统一的日志格式和输出
    /// </summary>
    public static class LoggerHelper
    {
        /// <summary>
        /// 获取带时间戳的日志消息
        /// </summary>
        /// <param name="module">模块名称</param>
        /// <param name="message">日志消息</param>
        /// <returns>格式化后的日志消息</returns>
        public static string FormatLogMessage(string module, string message)
        {
            return $"[{DateTime.Now:HH:mm:ss.fff}] [{module}] {message}";
        }

        /// <summary>
        /// 记录信息日志（带时间戳）
        /// </summary>
        public static void LogInfo(ILogger logger, string module, string message)
        {
            logger.LogInformation(FormatLogMessage(module, message));
        }

        /// <summary>
        /// 记录信息日志（带时间戳和参数）
        /// </summary>
        public static void LogInfo(ILogger logger, string module, string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            logger.LogInformation(FormatLogMessage(module, formattedMessage));
        }

        /// <summary>
        /// 记录警告日志（带时间戳）
        /// </summary>
        public static void LogWarning(ILogger logger, string module, string message)
        {
            logger.LogWarning(FormatLogMessage(module, message));
        }

        /// <summary>
        /// 记录警告日志（带时间戳和异常）
        /// </summary>
        public static void LogWarning(ILogger logger, string module, string message, Exception exception)
        {
            logger.LogWarning(exception, FormatLogMessage(module, message));
        }

        /// <summary>
        /// 记录错误日志（带时间戳）
        /// </summary>
        public static void LogError(ILogger logger, string module, string message)
        {
            logger.LogError(FormatLogMessage(module, message));
        }

        /// <summary>
        /// 记录错误日志（带时间戳和异常）
        /// </summary>
        public static void LogError(ILogger logger, string module, string message, Exception exception)
        {
            logger.LogError(exception, FormatLogMessage(module, message));
        }

        /// <summary>
        /// 记录调试日志（带时间戳）
        /// </summary>
        public static void LogDebug(ILogger logger, string module, string message)
        {
            logger.LogDebug(FormatLogMessage(module, message));
        }

        /// <summary>
        /// 记录调试日志（带时间戳和参数）
        /// </summary>
        public static void LogDebug(ILogger logger, string module, string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            logger.LogDebug(FormatLogMessage(module, formattedMessage));
        }

        /// <summary>
        /// 记录性能日志（带时间戳和执行时间）
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="module">模块名称</param>
        /// <param name="operation">操作名称</param>
        /// <param name="durationMs">执行耗时（毫秒）</param>
        public static void LogPerformance(ILogger logger, string module, string operation, long durationMs)
        {
            var message = $"{operation} 完成，耗时: {durationMs}ms";
            logger.LogInformation(FormatLogMessage(module, message));
        }

        /// <summary>
        /// 记录带时间差的开始日志
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="module">模块名称</param>
        /// <param name="operation">操作名称</param>
        /// <returns>用于计算时间差的DateTime</returns>
        public static DateTime LogStart(ILogger logger, string module, string operation)
        {
            var message = $"开始 {operation}";
            logger.LogInformation(FormatLogMessage(module, message));
            return DateTime.Now;
        }

        /// <summary>
        /// 记录带时间差的结束日志
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="module">模块名称</param>
        /// <param name="operation">操作名称</param>
        /// <param name="startTime">开始时间</param>
        public static void LogEnd(ILogger logger, string module, string operation, DateTime startTime)
        {
            var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
            LogPerformance(logger, module, operation, duration);
        }

        /// <summary>
        /// Claw AI 专用日志方法 - 记录带阶段标识的日志
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="module">模块名称</param>
        /// <param name="phase">阶段标识</param>
        /// <param name="message">日志消息</param>
        public static void LogClawPhase(ILogger logger, string module, string phase, string message)
        {
            var formattedMessage = FormatLogMessage(module, $"=== {phase} ===");
            logger.LogInformation(formattedMessage);
            logger.LogInformation(FormatLogMessage(module, message));
        }

        /// <summary>
        /// Claw AI 专用日志方法 - 记录步骤日志
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="module">模块名称</param>
        /// <param name="step">步骤编号</param>
        /// <param name="totalSteps">总步骤数</param>
        /// <param name="message">日志消息</param>
        public static void LogClawStep(ILogger logger, string module, int step, int totalSteps, string message)
        {
            var formattedMessage = $"[{step}/{totalSteps}] {message}";
            logger.LogInformation(FormatLogMessage(module, formattedMessage));
        }
    }

    /// <summary>
    /// Claw AI 日志模块名称常量
    /// </summary>
    public static class ClawLogModules
    {
        public const string CLAW = "ClawAI";
        public const string MODEL_INIT = "ModelInitializer";
        public const string CONTEXT_LOADER = "ContextLoader";
        public const string GREETING_FAST_PATH = "GreetingFastPath";
        public const string PLANNING_HANDLER = "PlanningHandler";
        public const string TASK_COMPLEXITY = "TaskComplexity";
        public const string TEXT_SIMILARITY = "TextSimilarity";
        public const string WORKFLOW_MATCHER = "WorkflowMatcher";
        public const string EXECUTION = "Execution";
        public const string REFLECTION = "Reflection";
        public const string MEMORY = "Memory";
        public const string AGENT_ORCHESTRATION = "AgentOrchestration";
        public const string PERSONALITY = "Personality";
        public const string RESULT_PARSER = "ResultParser";
        public const string TASK_PLANNING = "TaskPlanning";
        public const string MASTER_CONTROL = "MasterControl";
    }
}
