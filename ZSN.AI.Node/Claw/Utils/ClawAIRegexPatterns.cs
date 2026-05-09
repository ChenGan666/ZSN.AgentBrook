using System.Text.RegularExpressions;

namespace ZSN.AI.Node.Claw.Utils
{
    /// <summary>
    /// Claw AI 正则表达式模式缓存
    /// 使用静态编译的正则表达式，全局复用以提升性能
    /// </summary>
    public static class ClawAIRegexPatterns
    {
        /// <summary>
        /// 复杂任务模式匹配
        /// </summary>
        public static readonly Regex ComplexTaskPattern = new Regex(
            @"分析并|首先.*然后|生成.*并|处理.*再|对比|比较|制定|设计|规划|优化|改进|评估.*并|创建.*并|编写.*并|制作.*并",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 中文词语提取（长度>=2）
        /// </summary>
        public static readonly Regex ChineseWordExtractor = new Regex(
            @"[\u4e00-\u9fa5]{2,}",
            RegexOptions.Compiled);

        /// <summary>
        /// 英文词语提取（长度>=2）
        /// </summary>
        public static readonly Regex EnglishWordExtractor = new Regex(
            @"[a-zA-Z]{2,}",
            RegexOptions.Compiled);

        /// <summary>
        /// 数字提取（包括整数和小数）
        /// </summary>
        public static readonly Regex NumberExtractor = new Regex(
            @"\d+\.?\d*",
            RegexOptions.Compiled);

        /// <summary>
        /// JSON代码块提取（带语言标记）
        /// </summary>
        public static readonly Regex JsonCodeBlockWithLanguage = new Regex(
            @"```json\s*(\{[\s\S]*?\})\s*```",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 通用代码块提取
        /// </summary>
        public static readonly Regex GenericCodeBlock = new Regex(
            @"```\s*(\{[\s\S]*?\})\s*```",
            RegexOptions.Compiled);

        /// <summary>
        /// JSON对象提取
        /// </summary>
        public static readonly Regex JsonObjectExtractor = new Regex(
            @"(\{[\s\S]*\})",
            RegexOptions.Compiled);

        /// <summary>
        /// 占位符提取 (格式: {{ key }} 或 {{ key(jsonpath) }})
        /// </summary>
        public static readonly Regex PlaceholderExtractor = new Regex(
            @"\{\{\s*([^\{\}]+?)\s*\}\}",
            RegexOptions.Compiled);

        /// <summary>
        /// 带路径的键提取 (格式: key(path))
        /// </summary>
        public static readonly Regex KeyWithPathExtractor = new Regex(
            @"^([^()\s]+?)(?:\((.*?)\))?$",
            RegexOptions.Compiled);

        /// <summary>
        /// WorkFlow ID格式验证
        /// </summary>
        public static readonly Regex WorkflowIdValidator = new Regex(
            @"^[a-zA-Z0-9\-_]+$",
            RegexOptions.Compiled);

        /// <summary>
        /// GUID格式验证
        /// </summary>
        public static readonly Regex GuidValidator = new Regex(
            @"^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$",
            RegexOptions.Compiled);

        /// <summary>
        /// 数字提取
        /// </summary>
        public static readonly Regex DigitExtractor = new Regex(
            @"\d+",
            RegexOptions.Compiled);

        /// <summary>
        /// 标点符号分隔符（用于分词）
        /// </summary>
        public static readonly Regex PunctuationSplitter = new Regex(
            @"[ ,.,;,:!,?、。!?;:""'''《》\-_\\/\\|\n\r\t]+",
            RegexOptions.Compiled);

        /// <summary>
        /// 常见动作动词提取
        /// </summary>
        public static readonly string[] ActionVerbs = new[]
        {
            "查询", "搜索", "检索", "查找", "获取",
            "生成", "创建", "制作", "编写", "撰写",
            "分析", "统计", "计算", "处理", "转换",
            "介绍", "说明", "解释", "描述"
        };
    }
}
