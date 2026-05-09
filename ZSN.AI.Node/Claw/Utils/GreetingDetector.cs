using System;
using System.Linq;

namespace ZSN.AI.Node.Claw.Utils
{
    /// <summary>
    /// 问候语检测工具类
    /// 统一管理所有问候语识别逻辑，避免重复代码
    /// </summary>
    public static class GreetingDetector
    {
        /// <summary>
        /// 问候语模式（增强版）
        /// 包含中英文问候、感谢、告别、确认等常见对话场景
        /// </summary>
        private static readonly string[] GreetingPatterns = new[]
        {
            // 基础问候
            "你好", "您好", "hi", "hello", "嗨", "hey",
            "早上好", "下午好", "晚上好", "午安", "早安", "晚安",
            
            // 感谢
            "谢谢", "感谢", "多谢", "thank", "thanks",
            
            // 告别
            "再见", "拜拜", "bye", "goodbye",
            
            // 确认/回应
            "好的", "ok", "okay", "知道了", "明白了", "收到", "嗯", "哦",
            
            // 询问状态
            "怎么样", "如何", "最近", "近况",
            "在吗", "在不在", "在么", "在嘛", "有空吗"
        };

        /// <summary>
        /// 常见尾部标点符号，匹配前去除以支持 "你好！" 等变体
        /// </summary>
        private static readonly char[] TrailingPunctuation = new[] { '!', '！', '?', '？', ',', '，', '.', '。', '~', '～', ' ', ';', '；', ':', '：' };

        /// <summary>
        /// 判断是否为简单问候语（快速路径）
        /// 用于ExcutionClaw的超快速路径判断
        /// </summary>
        /// <param name="task">用户输入的任务</param>
        /// <returns>true表示是简单问候语</returns>
        public static bool IsSimpleGreeting(string task)
        {
            if (string.IsNullOrWhiteSpace(task) || task.Length > 20)
                return false;

            var stripped = task.ToLower().Trim().TrimEnd(TrailingPunctuation);
            return GreetingPatterns.Any(pattern => stripped == pattern);
        }

        /// <summary>
        /// 判断是否应该使用问候语快速路径（增强版）
        /// 用于TaskPlanningService的任务复杂度分析
        /// 包含更严格的检测逻辑：长度限制 + 模式匹配 + 符号检测
        /// </summary>
        /// <param name="task">用户输入的任务</param>
        /// <returns>true表示应该使用快速路径</returns>
        public static bool ShouldUseGreetingFastPath(string task)
        {
            if (string.IsNullOrWhiteSpace(task))
                return false;

            // 检查1: 精确匹配问候语模式（去除尾部标点后等于模式）
            // 防止子串误匹配（如 "agentbrook" 包含 "ok"）
            var stripped = task.ToLower().Trim().TrimEnd(TrailingPunctuation);
            if (GreetingPatterns.Any(pattern => stripped == pattern))
                return true;

            // 检查2: 纯标点符号或emoji（长度<=10）
            if (task.Length <= 10 && task.All(c => char.IsPunctuation(c) || char.IsSymbol(c) || c == ' '))
                return true;

            return false;
        }

        /// <summary>
        /// 获取问候语类型（用于日志和调试）
        /// </summary>
        /// <param name="task">用户输入的任务</param>
        /// <returns>问候语类型描述</returns>
        public static string GetGreetingType(string task)
        {
            if (string.IsNullOrWhiteSpace(task))
                return "空输入";

            var stripped = task.ToLower().Trim().TrimEnd(TrailingPunctuation);

            // 基础问候
            if (new[] { "你好", "您好", "hi", "hello", "嗨", "hey" }.Any(p => stripped == p))
                return "基础问候";

            // 时间问候
            if (new[] { "早上好", "下午好", "晚上好", "午安", "早安", "晚安" }.Any(p => stripped == p))
                return "时间问候";

            // 感谢
            if (new[] { "谢谢", "感谢", "多谢", "thank", "thanks" }.Any(p => stripped == p))
                return "感谢";

            // 告别
            if (new[] { "再见", "拜拜", "bye", "goodbye" }.Any(p => stripped == p))
                return "告别";

            // 确认
            if (new[] { "好的", "ok", "okay", "知道了", "明白了", "收到", "嗯", "哦" }.Any(p => stripped == p))
                return "确认回应";

            // 询问状态
            if (new[] { "怎么样", "如何", "最近", "近况", "在吗", "在不在" }.Any(p => stripped == p))
                return "询问状态";

            // 纯符号
            if (task.Length <= 10 && task.All(c => char.IsPunctuation(c) || char.IsSymbol(c) || c == ' '))
                return "纯符号/表情";

            return "非问候语";
        }

        /// <summary>
        /// 获取所有支持的问候语模式（用于文档和测试）
        /// </summary>
        /// <returns>问候语模式数组</returns>
        public static string[] GetSupportedPatterns()
        {
            return (string[])GreetingPatterns.Clone();
        }
    }
}
