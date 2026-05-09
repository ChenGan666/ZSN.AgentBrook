using System.Collections.Generic;

namespace ZSN.AI.Node.Claw.Configuration
{
    /// <summary>
    /// Claw AI 配置选项
    /// </summary>
    public class ClawAIOptions
    {
        /// <summary>
        /// 问候语检测配置
        /// </summary>
        public GreetingDetectionOptions GreetingDetection { get; set; } = new GreetingDetectionOptions();

        /// <summary>
        /// 记忆配置
        /// </summary>
        public MemoryOptions Memory { get; set; } = new MemoryOptions();

        /// <summary>
        /// 规划配置
        /// </summary>
        public PlanningOptions Planning { get; set; } = new PlanningOptions();

        /// <summary>
        /// 相似度阈值配置
        /// </summary>
        public SimilarityThresholds SimilarityThresholds { get; set; } = new SimilarityThresholds();

        /// <summary>
        /// 任务复杂度配置
        /// </summary>
        public TaskComplexityOptions TaskComplexity { get; set; } = new TaskComplexityOptions();

        /// <summary>
        /// 反思配置
        /// </summary>
        public ReflectionOptions Reflection { get; set; } = new ReflectionOptions();

        /// <summary>
        /// 主控配置
        /// </summary>
        public MasterControlOptions MasterControl { get; set; } = new MasterControlOptions();

        /// <summary>
        /// 记忆整理配置
        /// </summary>
        public MemoryConsolidationOptions MemoryConsolidation { get; set; } = new MemoryConsolidationOptions();
    }

    /// <summary>
    /// 问候语检测配置
    /// </summary>
    public class GreetingDetectionOptions
    {
        /// <summary>
        /// 问候语最大长度
        /// </summary>
        public int MaxLength { get; set; } = 20;

        /// <summary>
        /// 问候语模式列表
        /// </summary>
        public List<string> Patterns { get; set; } = new List<string>
        {
            "你好", "嗨", "hello", "hi", "早上好", "晚上好", "下午好",
            "在吗", "在不在", "哈喽", "嗨", "嘿"
        };

        /// <summary>
        /// 简单对话模式
        /// </summary>
        public List<string> SimpleConversationPatterns { get; set; } = new List<string>
        {
            "谢谢", "感谢", "再见", "拜拜", "好的", "行", "可以"
        };
    }

    /// <summary>
    /// 记忆配置
    /// </summary>
    public class MemoryOptions
    {
        /// <summary>
        /// 短期记忆（工作记忆）最大条数
        /// </summary>
        public int WorkingMemoryLimit { get; set; } = 10;

        /// <summary>
        /// 情景记忆最大条数
        /// </summary>
        public int EpisodicMemoryLimit { get; set; } = 5;

        /// <summary>
        /// 长期记忆检索最大条数
        /// </summary>
        public int LongTermMemoryLimit { get; set; } = 3;

        /// <summary>
        /// 记忆归档重要性阈值
        /// </summary>
        public int ArchiveImportanceThreshold { get; set; } = 60;

        /// <summary>
        /// 用户画像最大长度
        /// </summary>
        public int UserProfileMaxLength { get; set; } = 500;

        /// <summary>
        /// 交互模式最大长度
        /// </summary>
        public int InteractionPatternMaxLength { get; set; } = 500;

        /// <summary>
        /// 记忆处理模型(用于知识提炼和压缩)
        /// </summary>
        public string MemoryModel { get; set; }

        /// <summary>
        /// 启用自动知识提炼
        /// </summary>
        public bool EnableAutoKnowledgeExtraction { get; set; } = true;

        /// <summary>
        /// 启用记忆优先回答
        /// </summary>
        public bool EnableMemoryPriorityAnswer { get; set; } = true;

        /// <summary>
        /// 知识提炼最小对话长度
        /// </summary>
        public int MinDialogueLengthForExtraction { get; set; } = 50;

        /// <summary>
        /// 自动去重合并周期(小时)
        /// </summary>
        public int AutoMergePeriodHours { get; set; } = 24;

        /// <summary>
        /// 是否归档失败任务（作为经验教训）
        /// </summary>
        public bool ArchiveFailedTasks { get; set; } = true;

        /// <summary>
        /// 启用动态重要性阈值（根据任务状态调整阈值）
        /// </summary>
        public bool UseDynamicImportanceThreshold { get; set; } = true;

        /// <summary>
        /// 自动去重触发频率（每N次对话触发一次）
        /// </summary>
        public int AutoMergeFrequency { get; set; } = 10;

        /// <summary>
        /// 启用规则降级（LLM失败时使用规则提取）
        /// </summary>
        public bool EnableRuleFallback { get; set; } = true;
    }

    /// <summary>
    /// 规划配置
    /// </summary>
    public class PlanningOptions
    {
        /// <summary>
        /// 简单任务最大步骤数
        /// </summary>
        public int SimpleTaskMaxSteps { get; set; } = 2;

        /// <summary>
        /// 中等任务最大步骤数
        /// </summary>
        public int MediumTaskMaxSteps { get; set; } = 5;

        /// <summary>
        /// 历史规划最大条数
        /// </summary>
        public int HistoricalPlansLimit { get; set; } = 5;

        /// <summary>
        /// 知识问答模式
        /// </summary>
        public List<string> KnowledgeQueryPatterns { get; set; } = new List<string>
        {
            "是什么", "什么是", "介绍", "解释", "说明", "定义",
            "为什么", "怎么样", "如何理解", "什么意思", "告诉我", "我想知道", "请问"
        };

        /// <summary>
        /// 简单任务模式
        /// </summary>
        public List<string> SimpleTaskPatterns { get; set; } = new List<string>
        {
            "查询", "搜索", "找", "查找", "显示", "列出", "帮我看", "看看"
        };

        /// <summary>
        /// 复杂任务模式
        /// </summary>
        public List<string> ComplexTaskPatterns { get; set; } = new List<string>
        {
            "分析并", "首先.*然后", "生成.*并", "处理.*再", "对比", "比较",
            "制定", "设计", "规划", "优化", "改进", "评估.*并",
            "创建.*并", "编写.*并", "制作.*并"
        };

        /// <summary>
        /// 纯分析模式（不分配WorkFlow）
        /// </summary>
        public List<string> PureAnalysisPatterns { get; set; } = new List<string>
        {
            "分析用户", "明确目标", "理解意图", "制定策略",
            "仅分析", "仅明确", "仅理解", "仅制定"
        };

        /// <summary>
        /// WorkFlow匹配阈值
        /// </summary>
        public double WorkflowMatchThreshold { get; set; } = 0.02;

        /// <summary>
        /// WorkFlow关键词提取数量
        /// </summary>
        public int WorkflowKeywordsCount { get; set; } = 5;
    }

    /// <summary>
    /// 相似度阈值配置
    /// </summary>
    public class SimilarityThresholds
    {
        /// <summary>
        /// 记忆快速路径相似度阈值
        /// </summary>
        public double MemoryFastPath { get; set; } = 0.3;

        /// <summary>
        /// 情景记忆相似度阈值
        /// </summary>
        public double EpisodicMemory { get; set; } = 0.25;

        /// <summary>
        /// WorkFlow匹配相似度阈值
        /// </summary>
        public double WorkflowMatch { get; set; } = 0.02;
    }

    /// <summary>
    /// 任务复杂度配置
    /// </summary>
    public class TaskComplexityOptions
    {
        /// <summary>
        /// 简单任务最大长度
        /// </summary>
        public int SimpleTaskMaxLength { get; set; } = 50;

        /// <summary>
        /// 复杂任务最小长度
        /// </summary>
        public int ComplexTaskMinLength { get; set; } = 200;

        /// <summary>
        /// 知识问答最大长度
        /// </summary>
        public int KnowledgeQueryMaxLength { get; set; } = 100;

        /// <summary>
        /// 任务连接词（用于判断是否为多步骤任务）
        /// </summary>
        public List<string> TaskConnectors { get; set; } = new List<string>
        {
            "并", "然", "再", "接"
        };
    }

    /// <summary>
    /// 反思配置
    /// </summary>
    public class ReflectionOptions
    {
        /// <summary>
        /// 简单任务最大步骤数
        /// </summary>
        public int SimpleTaskMaxSteps { get; set; } = 2;

        /// <summary>
        /// 简单任务快速完成的整体质量分数
        /// </summary>
        public int SimpleTaskOverallQuality { get; set; } = 85;

        /// <summary>
        /// 简单任务快速完成的完整度分数
        /// </summary>
        public int SimpleTaskCompletenessScore { get; set; } = 90;

        /// <summary>
        /// 简单任务快速完成的准确度分数
        /// </summary>
        public int SimpleTaskAccuracyScore { get; set; } = 80;

        /// <summary>
        /// 高质量步骤质量阈值
        /// </summary>
        public int HighQualityStepThreshold { get; set; } = 80;

        /// <summary>
        /// 大部分完成比例阈值（0-1）
        /// </summary>
        public double MostlyCompletedRatio { get; set; } = 0.8;

        /// <summary>
        /// 大部分完成的整体质量分数
        /// </summary>
        public int MostlyCompletedOverallQuality { get; set; } = 90;

        /// <summary>
        /// 大部分完成的完整度分数
        /// </summary>
        public int MostlyCompletedCompletenessScore { get; set; } = 95;

        /// <summary>
        /// 全部完成的质量分数
        /// </summary>
        public int AllCompletedOverallQuality { get; set; } = 95;

        /// <summary>
        /// 全部完成的完整度分数
        /// </summary>
        public int AllCompletedCompletenessScore { get; set; } = 98;

        /// <summary>
        /// 全部完成的准确度分数
        /// </summary>
        public int AllCompletedAccuracyScore { get; set; } = 95;

        /// <summary>
        /// 最大迭代前返回的整体质量分数
        /// </summary>
        public int MaxIterationOverallQuality { get; set; } = 70;

        /// <summary>
        /// 最大迭代前的完整度分数
        /// </summary>
        public int MaxIterationCompletenessScore { get; set; } = 75;

        /// <summary>
        /// 最大迭代前的准确度分数
        /// </summary>
        public int MaxIterationAccuracyScore { get; set; } = 70;

        /// <summary>
        /// 禁止重新规划的迭代次数
        /// </summary>
        public int NoReplanIterationThreshold { get; set; } = 2;

        /// <summary>
        /// 禁止重新规划的步骤数
        /// </summary>
        public int NoReplanStepThreshold { get; set; } = 2;
    }

    /// <summary>
    /// 主控配置
    /// </summary>
    public class MasterControlOptions
    {
        /// <summary>
        /// 是否启用主控判断
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCache { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// 简单问候语模式
        /// </summary>
        public List<string> SimpleGreetingPatterns { get; set; } = new List<string>
        {
            "你好", "嗨", "hello", "hi", "早上好", "晚上好", "下午好",
            "在吗", "在不在", "哈喽", "嘿"
        };

        /// <summary>
        /// 简单感谢模式
        /// </summary>
        public List<string> SimpleThanksPatterns { get; set; } = new List<string>
        {
            "谢谢", "感谢", "多谢", "thanks", "thank you"
        };

        /// <summary>
        /// 简单告别模式
        /// </summary>
        public List<string> SimpleGoodbyePatterns { get; set; } = new List<string>
        {
            "再见", "拜拜", "bye", "goodbye", "回见"
        };

        /// <summary>
        /// 知识问答模式
        /// </summary>
        public List<string> KnowledgeQueryPatterns { get; set; } = new List<string>
        {
            "是什么", "什么是", "介绍", "解释", "说明", "定义",
            "为什么", "怎么样", "如何理解", "什么意思"
        };

        /// <summary>
        /// 需要规划的任务模式
        /// </summary>
        public List<string> NeedPlanningPatterns { get; set; } = new List<string>
        {
            "帮我", "请", "生成", "创建", "制作", "编写", "分析",
            "处理", "执行", "完成", "做", "搞定"
        };

        /// <summary>
        /// 主控判断失败时的回退策略: fallback_to_planning(默认规划), fallback_to_direct(直接回复)
        /// </summary>
        public string FallbackStrategy { get; set; } = "fallback_to_planning";
    }

    /// <summary>
    /// 记忆整理配置
    /// 用于定时记忆整理服务 MemoryConsolidationJob 的参数配置
    /// </summary>
    public class MemoryConsolidationOptions
    {
        /// <summary>
        /// 是否启用记忆整理
        /// </summary>
        public bool Enabled { get; set; } = true;

        // ===== 清理规则 =====

        /// <summary>
        /// 清理规则：问候语内容匹配模式列表
        /// </summary>
        public List<string> GreetingPatterns { get; set; } = new List<string>
        {
            "你好", "嗨", "hello", "hi", "早上好", "晚上好", "下午好",
            "在吗", "在不在", "哈喽", "嘿"
        };

        /// <summary>
        /// 清理规则：简单感谢/告别匹配模式
        /// </summary>
        public List<string> TrivialPatterns { get; set; } = new List<string>
        {
            "谢谢", "感谢", "再见", "拜拜", "好的", "行", "可以",
            "bye", "goodbye", "thanks", "thank you"
        };

        /// <summary>
        /// 清理规则：最小内容长度（低于此长度且重要性低则清理）
        /// </summary>
        public int MinContentLength { get; set; } = 20;

        /// <summary>
        /// 清理规则：低重要性阈值（ClawAI级）
        /// </summary>
        public int LowImportanceThresholdClawAI { get; set; } = 30;

        /// <summary>
        /// 清理规则：低重要性阈值（APP级，更保守）
        /// </summary>
        public int LowImportanceThresholdApp { get; set; } = 20;

        /// <summary>
        /// 清理规则：长期未访问天数（ClawAI级）
        /// </summary>
        public int LongUnusedDaysClawAI { get; set; } = 30;

        /// <summary>
        /// 清理规则：长期未访问天数（APP级，更宽容）
        /// </summary>
        public int LongUnusedDaysApp { get; set; } = 60;

        // ===== 知识提炼 =====

        /// <summary>
        /// 深度知识提炼：每批处理数量
        /// </summary>
        public int KnowledgeExtractionBatchSize { get; set; } = 20;

        // ===== 知识图谱 =====

        /// <summary>
        /// 知识图谱构建：每批处理数量
        /// </summary>
        public int GraphBuildBatchSize { get; set; } = 20;

        // ===== 层级提升 =====

        /// <summary>
        /// 知识提升：通用性阈值（0-100）
        /// 只有 GeneralityScore >= 此值的知识才会被提升到APP级
        /// </summary>
        public int PromotionGeneralityThreshold { get; set; } = 70;

        /// <summary>
        /// 知识提升：最低重要性评分
        /// </summary>
        public int PromotionMinImportance { get; set; } = 60;

        /// <summary>
        /// 知识提升：每次最多提升的知识数量
        /// </summary>
        public int PromotionMaxPerRun { get; set; } = 50;

        // ===== 重评分 =====

        /// <summary>
        /// 记忆重评分：时间衰减天数
        /// </summary>
        public int TimeDecayDays { get; set; } = 30;

        /// <summary>
        /// 记忆重评分：ClawAI级时间衰减分数
        /// </summary>
        public int TimeDecayScoreClawAI { get; set; } = 5;

        /// <summary>
        /// 记忆重评分：APP级时间衰减分数（衰减更慢）
        /// </summary>
        public int TimeDecayScoreApp { get; set; } = 3;

        /// <summary>
        /// 记忆重评分：高访问量阈值
        /// </summary>
        public int HighAccessThreshold { get; set; } = 10;

        /// <summary>
        /// 记忆重评分：高访问量加分
        /// </summary>
        public int HighAccessBonus { get; set; } = 10;

        // ===== 时间窗口（增量处理） =====

        /// <summary>
        /// Cron定时模式下的回查时间范围（分钟）
        /// 当 Job 使用 Cron 表达式调度时（如每天凌晨3点），使用此值作为回查窗口
        /// 默认1440分钟（24小时），确保覆盖上一轮以来的全部数据
        /// </summary>
        public int CronLookbackMinutes { get; set; } = 1440;

        /// <summary>
        /// 周期模式下的回查时间倍数
        /// 当 Job 使用固定间隔调度时（如每60分钟），实际回查分钟数 = LoopTimerSeconds / 60 * IntervalLookbackMultiplier
        /// 默认1.5倍，例如每60分钟执行 → 回查90分钟
        /// </summary>
        public double IntervalLookbackMultiplier { get; set; } = 1.5;

        // ===== 通用 =====

        /// <summary>
        /// 每次执行最大处理的App数量（0=不限）
        /// </summary>
        public int MaxAppsPerRun { get; set; } = 0;
    }
}
