using System;
using System.Collections.Generic;

namespace ZSN.AI.Entity.ClawAI
{
    /// <summary>
    /// Agent 节点信息
    /// </summary>
    public class AgentNodeInfo
    {
        public string NodeID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Capabilities { get; set; }
        public string WorkflowID { get; set; }
    }

    /// <summary>
    /// 记忆上下文
    /// </summary>
    public class MemoryContext
    {
        /// <summary>
        /// 用户画像
        /// </summary>
        public UserProfile UserProfile { get; set; }

        /// <summary>
        /// AI 个性状态
        /// </summary>
        public AIPersonalityState AIState { get; set; }

        /// <summary>
        /// 短期记忆数量
        /// </summary>
        public int WorkingMemoryCount { get; set; }

        /// <summary>
        /// 短期工作记忆(ChatHistory实际内容)
        /// </summary>
        public List<ZSN.AI.Entity.AppChatLogInfo> WorkingMemory { get; set; } = new List<ZSN.AI.Entity.AppChatLogInfo>();

        /// <summary>
        /// 相关记忆列表
        /// </summary>
        public List<EpisodicMemory> RelevantMemories { get; set; } = new List<EpisodicMemory>();

        // ==================== P3优化: 长期记忆字段 ====================

        /// <summary>
        /// 关键词检索的长期记忆 (P3优化)
        /// </summary>
        public List<LongTermMemoryInfo> LongTermMemories { get; set; } = new List<LongTermMemoryInfo>();

        /// <summary>
        /// 语义相似度检索的长期记忆 (P3优化)
        /// </summary>
        public List<LongTermMemoryInfo> SemanticMemories { get; set; } = new List<LongTermMemoryInfo>();

        /// <summary>
        /// 知识图谱关联记忆 (P3优化)
        /// </summary>
        public List<LongTermMemoryInfo> RelatedKnowledge { get; set; } = new List<LongTermMemoryInfo>();
    }

    /// <summary>
    /// 用户画像
    /// </summary>
    public class UserProfile
    {
        public string ProfileID { get; set; } = Guid.NewGuid().ToString();
        public string MemberID { get; set; }
        public string AppID { get; set; }

        /// <summary>
        /// 用户偏好摘要
        /// </summary>
        public string PreferencesSummary { get; set; }

        /// <summary>
        /// 交互模式摘要
        /// </summary>
        public string InteractionPatternSummary { get; set; }

        /// <summary>
        /// 偏好详情(JSON)
        /// </summary>
        public string PreferencesDetail { get; set; }

        /// <summary>
        /// 交互模式详情(JSON)
        /// </summary>
        public string InteractionPatternsDetail { get; set; }

        /// <summary>
        /// 总交互次数
        /// </summary>
        public int TotalInteractions { get; set; } = 0;

        /// <summary>
        /// 最后交互时间
        /// </summary>
        public DateTime LastInteractionTime { get; set; } = DateTime.Now;

        public DateTime LastUpdateTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// AI 个性状态
    /// </summary>
    public class AIPersonalityState
    {
        public string StateID { get; set; } = Guid.NewGuid().ToString();
        public string SessionID { get; set; }
        public string AppID { get; set; }

        /// <summary>
        /// 个性特征(字典对象,用于内存操作)
        /// </summary>
        public Dictionary<string, object> PersonalityTraits { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 情绪状态(字典对象,用于内存操作)
        /// </summary>
        public Dictionary<string, object> EmotionalState { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 当前目标(列表,用于内存操作)
        /// </summary>
        public List<string> CurrentGoals { get; set; } = new List<string>();

        public DateTime LastUpdateTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 情景记忆
    /// </summary>
    public class EpisodicMemory
    {
        public string MemoryID { get; set; } = Guid.NewGuid().ToString();
        public string AppID { get; set; }
        public string SessionID { get; set; }
        public string MemberID { get; set; }

        /// <summary>
        /// 事件类型
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// 事件上下文(字典对象,用于内存操作)
        /// </summary>
        public Dictionary<string, object> EventContext { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 事件结果
        /// </summary>
        public string EventResult { get; set; }

        /// <summary>
        /// 摘要
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// 向量嵌入
        /// </summary>
        public string Embedding { get; set; }

        /// <summary>
        /// 重要性(0-100)
        /// </summary>
        public int Importance { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 执行结果
    /// </summary>
    public class ExecutionResult
    {
        public int CompletedSteps { get; set; }
        public int FailedSteps { get; set; }
        public int SkippedSteps { get; set; }
        public bool AllStepsCompleted { get; set; }

        /// <summary>
        /// 是否已异步触发子 WorkFlow（步骤已触发但未完成，等待回调）
        /// 为 true 时调用方应立即退出循环，不再执行反思/迭代
        /// </summary>
        public bool IsAsyncTriggered { get; set; }
    }

    /// <summary>
    /// Agent 执行结果
    /// </summary>
    public class AgentExecutionResult
    {
        public string AgentNodeId { get; set; }
        public string AgentName { get; set; }
        public bool Success { get; set; }
        public string Result { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// 反思结果
    /// </summary>
    public class ReflectionResult
    {
        public int OverallQuality { get; set; }
        public int CompletenessScore { get; set; }
        public int AccuracyScore { get; set; }
        public ReflectionAction Action { get; set; }
        public string Reasoning { get; set; }
        public string FinalAnswer { get; set; }
        public int RetryStepIndex { get; set; }
        public string RefinedPrompt { get; set; }
        public List<string> SuggestedAgentIds { get; set; }
        public string Reason { get; set; }

        /// <summary>
        /// 任务分析结果（分析当前执行结果，判断是否需要额外步骤）
        /// </summary>
        public TaskAnalysis TaskAnalysis { get; set; }

        /// <summary>
        /// 建议的新步骤列表（动态添加到任务规划中）
        /// </summary>
        public List<SuggestedStep> SuggestedSteps { get; set; } = new List<SuggestedStep>();

        /// <summary>
        /// 已替换好参数的完整反思提示词（用于日志记录）
        /// </summary>
        public string ResolvedPrompt { get; set; }
    }

    /// <summary>
    /// 任务分析结果
    /// </summary>
    public class TaskAnalysis
    {
        /// <summary>
        /// 当前任务完成度（0-100）
        /// </summary>
        public int CompletionPercentage { get; set; }

        /// <summary>
        /// 是否需要额外步骤
        /// </summary>
        public bool NeedsAdditionalSteps { get; set; }

        /// <summary>
        /// 分析摘要
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// 缺失的内容或能力
        /// </summary>
        public List<string> MissingCapabilities { get; set; } = new List<string>();

        /// <summary>
        /// 建议的改进方向
        /// </summary>
        public List<string> ImprovementSuggestions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 建议的步骤
    /// </summary>
    public class SuggestedStep
    {
        /// <summary>
        /// 步骤描述
        /// </summary>
        public string StepDescription { get; set; }

        /// <summary>
        /// 步骤类型
        /// </summary>
        public StepType StepType { get; set; } = StepType.WorkflowCall;

        /// <summary>
        /// 建议的 WorkFlow ID 列表
        /// </summary>
        public List<string> SuggestedWorkflowIds { get; set; } = new List<string>();

        /// <summary>
        /// 依赖的步骤索引列表
        /// </summary>
        public List<int> DependsOnStepIndices { get; set; } = new List<int>();

        /// <summary>
        /// 输入参数建议
        /// </summary>
        public List<InputSuggestion> InputSuggestions { get; set; } = new List<InputSuggestion>();

        /// <summary>
        /// 预期输出
        /// </summary>
        public string ExpectedOutput { get; set; }

        /// <summary>
        /// 优先级（1-10，10最高）
        /// </summary>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// 建议原因
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// 输入参数建议
    /// </summary>
    public class InputSuggestion
    {
        /// <summary>
        /// 参数名
        /// </summary>
        public string VarName { get; set; }

        /// <summary>
        /// 参数值或提取路径
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 是否从前置步骤提取
        /// </summary>
        public bool ExtractFromPreviousStep { get; set; }

        /// <summary>
        /// 提取的源步骤索引
        /// </summary>
        public int? SourceStepIndex { get; set; }
    }

    /// <summary>
    /// 反思行动枚举
    /// </summary>
    public enum ReflectionAction
    {
        Complete,            // 完成
        ContinueExecution,   // 继续执行
        RetryStep,           // 重试步骤
        Replan,              // 重新规划
        Fail                 // 失败
    }
}
