using System;
using System.Collections.Generic;
using ZSN.AI.Entity.Model;

namespace ZSN.AI.Entity.ClawAI
{
    /// <summary>
    /// 任务规划配置
    /// </summary>
    public class TaskPlanningConfig
    {
        /// <summary>
        /// 启用任务规划
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// 使用专用规划模型(如果配置了 planningModel)
        /// </summary>
        public bool useDedicatedModel { get; set; } = true;

        /// <summary>
        /// 规划策略: sequential(顺序), parallel(并行), adaptive(自适应)
        /// </summary>
        public string planningStrategy { get; set; } = "adaptive";

        /// <summary>
        /// 最大规划步骤数(优化后默认值降低,配合动态步骤数调整)
        /// </summary>
        public int maxSteps { get; set; } = 15;

        /// <summary>
        /// 是否允许动态调整规划
        /// </summary>
        public bool allowDynamicReplanning { get; set; } = true;

        /// <summary>
        /// 重新规划的触发条件
        /// </summary>
        public ReplanningTrigger replanningTrigger { get; set; } = new ReplanningTrigger();

        /// <summary>
        /// 规划提示词模板
        /// </summary>
        public string planningPromptTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 统一规划提示词模板 (P0优化: 合并主控判断与任务规划)
        /// 如果非空，优先使用此模板进行单次LLM调用，同时完成判断和规划
        /// </summary>
        public string unifiedPlanPromptTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 统一规划 JSON 解析失败时的最大重试次数（默认 2 次）
        /// </summary>
        public int maxParseRetries { get; set; } = 2;
    }

    /// <summary>
    /// 重新规划触发条件
    /// </summary>
    public class ReplanningTrigger
    {
        /// <summary>
        /// 步骤失败次数阈值
        /// </summary>
        public int stepFailureThreshold { get; set; } = 2;

        /// <summary>
        /// 质量分数低于此值触发重新规划
        /// </summary>
        public int qualityThreshold { get; set; } = 50;

        /// <summary>
        /// 执行时间超过预估的倍数
        /// </summary>
        public double timeOverrunMultiplier { get; set; } = 2.0;
    }

    /// <summary>
    /// WorkFlow 循环配置
    /// </summary>
    public class WorkFlowLoopConfig
    {
        /// <summary>
        /// 启用 WorkFlow 循环
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// 最大循环次数(优化后默认值降低,减少不必要的迭代)
        /// </summary>
        public int maxIterations { get; set; } = 2;

        /// <summary>
        /// WorkFlow 选择策略: auto(自动选择), manual(手动指定), all(全部执行)
        /// </summary>
        public string selectionStrategy { get; set; } = "auto";


        /// <summary>
        /// WorkFlow 执行模式: sequential(顺序), parallel(并行)
        /// </summary>
        public string executionMode { get; set; } = "sequential";

        /// <summary>
        /// 是否允许 WorkFlow 失败后继续
        /// </summary>
        public bool continueOnWorkFlowFailure { get; set; } = true;

        /// <summary>
        /// 质量阈值(0-100,优化后降低阈值,更容易通过)
        /// </summary>
        public int qualityThreshold { get; set; } = 60;

        /// <summary>
        /// WorkFlow 执行超时(分钟)
        /// </summary>
        public int workflowExecutionTimeoutMinutes { get; set; } = 5;

        /// <summary>
        /// WorkFlow 轮询间隔(毫秒)
        /// </summary>
        public int workflowPollingIntervalMs { get; set; } = 500;

        /// <summary>
        /// P2改进: 快速轮询持续时间(秒) - 前N秒使用快速轮询
        /// </summary>
        public int fastPollingDurationSeconds { get; set; } = 30;

        /// <summary>
        /// P2改进: 快速轮询间隔(毫秒) - 前N秒的轮询间隔
        /// </summary>
        public int fastPollingIntervalMs { get; set; } = 100;

        /// <summary>
        /// P2改进: 慢速轮询间隔(毫秒) - N秒后的轮询间隔
        /// </summary>
        public int slowPollingIntervalMs { get; set; } = 500;

        /// <summary>
        /// P2改进: 最小超时时间(秒) - 动态超时的最小值
        /// </summary>
        public int minTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// P2改进: 超时缓冲倍数 - 预估时间的缓冲倍数
        /// </summary>
        public double timeoutBufferMultiplier { get; set; } = 1.5;

        /// <summary>
        /// P2改进: 日志输出频率 - 每N次轮询输出一次日志
        /// </summary>
        public int logOutputFrequency { get; set; } = 10;

        /// <summary>
        /// 是否启用异步触发模式（true: 异步回调, false: 同步轮询）
        /// </summary>
        public bool useAsyncTrigger { get; set; } = true;

        /// <summary>
        /// 异步触发的全局最大等待时间（分钟），防止永远挂起
        /// 0 表示使用默认值 120 分钟
        /// </summary>
        public int asyncTriggerMaxWaitMinutes { get; set; } = 120;
    }

    /// <summary>
    /// 反思配置
    /// </summary>
    public class ReflectionConfig
    {
        /// <summary>
        /// 启用反思
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// 反思提示词模板
        /// </summary>
        public string reflectionPromptTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 使用专用反思模型(如果配置了 reflectionModel)
        /// </summary>
        public bool useDedicatedModel { get; set; } = true;

        /// <summary>
        /// 启用动态任务分析（分析当前结果，建议下一步行动）
        /// </summary>
        public bool enableDynamicTaskAnalysis { get; set; } = true;

        /// <summary>
        /// 启用智能步骤建议（基于可用WorkFlow建议新步骤）
        /// </summary>
        public bool enableSmartStepSuggestion { get; set; } = true;

        /// <summary>
        /// 最大建议步骤数（每次反思最多建议添加的步骤数）
        /// </summary>
        public int maxSuggestedSteps { get; set; } = 3;
    }

    /// <summary>
    /// 记忆配置
    /// </summary>
    public class MemoryConfig
    {
        /// <summary>
        /// 使用专用记忆模型(如果配置了 memoryModel)
        /// </summary>
        public bool useDedicatedModel { get; set; } = true;

        /// <summary>
        /// 启用短期记忆
        /// </summary>
        public bool enableWorkingMemory { get; set; } = true;

        /// <summary>
        /// 启用长期记忆
        /// </summary>
        public bool enableLongTermMemory { get; set; } = true;

        /// <summary>
        /// 启用情景记忆
        /// </summary>
        public bool enableEpisodicMemory { get; set; } = true;

        /// <summary>
        /// 记忆压缩策略: summary, embedding
        /// </summary>
        public string compressionStrategy { get; set; } = "summary";

        /// <summary>
        /// 记忆检索相关性阈值
        /// </summary>
        public int relevanceThreshold { get; set; } = 70;
    }

    /// <summary>
    /// 用户画像配置
    /// </summary>
    public class UserProfileConfig
    {
        /// <summary>
        /// 使用专用画像模型(如果配置了 profileModel)
        /// </summary>
        public bool useDedicatedModel { get; set; } = true;

        /// <summary>
        /// 启用用户画像
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// 追踪用户偏好
        /// </summary>
        public bool trackPreferences { get; set; } = true;

        /// <summary>
        /// 追踪交互模式
        /// </summary>
        public bool trackInteractionPatterns { get; set; } = true;

        /// <summary>
        /// 个性化响应强度 (0-100)
        /// </summary>
        public int personalizationStrength { get; set; } = 50;
    }

    /// <summary>
    /// AI 个性配置
    /// </summary>
    public class PersonalityConfig
    {
        /// <summary>
        /// 使用专用个性模型(如果配置了 personalityModel)
        /// </summary>
        public bool useDedicatedModel { get; set; } = true;

        /// <summary>
        /// 启用 AI 个性
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// 个性描述
        /// </summary>
        public string personalityDescription { get; set; } = "专业、友好、有创意的AI助手";

        /// <summary>
        /// 情绪模拟
        /// </summary>
        public bool enableEmotionalState { get; set; } = false;

        /// <summary>
        /// 目标导向
        /// </summary>
        public bool enableGoalOriented { get; set; } = true;
    }

    /// <summary>
    /// 智能主控配置（使用主模型进行判断）
    /// </summary>
    public class MasterControlConfig
    {
        /// <summary>
        /// 启用智能主控判断（使用LLM理解上下文，替代简单的关键词匹配）
        /// </summary>
        public bool enabled { get; set; } = true;

        /// <summary>
        /// 主控判断提示词模板
        /// </summary>
        public string promptTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 主控判断超时时间（秒）
        /// </summary>
        public int timeoutSeconds { get; set; } = 5;

        /// <summary>
        /// 启用缓存（相同输入在短时间内返回相同结果）
        /// </summary>
        public bool enableCache { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（秒）
        /// </summary>
        public int cacheExpirationSeconds { get; set; } = 300;

        /// <summary>
        /// 主控判断失败时的回退策略: fallback_to_planning(默认规划), fallback_to_direct(直接回复)
        /// </summary>
        public string fallbackStrategy { get; set; } = "fallback_to_planning";
    }

    /// <summary>
    /// WorkFlow 配置信息
    /// </summary>
    public class WorkflowConfigInfo
    {
        /// <summary>
        /// WorkFlow ID
        /// </summary>
        public string workflowId { get; set; }
        
        /// <summary>
        /// WorkFlow 名称
        /// </summary>
        public string name { get; set; }
        
        /// <summary>
        /// WorkFlow 描述(用于AI选择)
        /// </summary>
        public string description { get; set; }
        
        /// <summary>
        /// WorkFlow 能力标签
        /// </summary>
        public List<string> capabilities { get; set; } = new List<string>();
        
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool enabled { get; set; } = true;
        
        /// <summary>
        /// WorkFlow 所属的 Agent 信息(仅用于前端保存,不参与后端逻辑)
        /// </summary>
        public AgentInfo agent { get; set; }
        
        /// <summary>
        /// 预估执行时间(秒) - 用于动态超时计算
        /// 0表示使用默认超时时间
        /// </summary>
        public int estimatedDurationSeconds { get; set; } = 0;
    }

    /// <summary>
    /// ClawAI WorkFlow 步骤异步执行上下文
    /// 保存到 TaskInfo.TaskConfig.NotNodeConfig 中
    /// AgentEnd 完成后通过此上下文恢复 ClawAI 执行
    /// </summary>
    public class ClawAIStepContext
    {
        /// <summary>
        /// ClawAI 节点配置 ID
        /// </summary>
        public string ClawAINodeId { get; set; }

        /// <summary>
        /// ClawAI 节点的 NodeConfig JSON（用于恢复时重建 config）
        /// </summary>
        public string NodeConfigJson { get; set; }

        /// <summary>
        /// ClawAI 节点的 ClawAIData JSON
        /// </summary>
        public string NodeDataJson { get; set; }

        /// <summary>
        /// 完整的任务规划 JSON
        /// </summary>
        public string TaskPlanningJson { get; set; }

        /// <summary>
        /// 当前触发的子 WorkFlow 对应的步骤 ID
        /// </summary>
        public string TriggeredStepId { get; set; }

        /// <summary>
        /// 当前触发步骤的索引
        /// </summary>
        public int TriggeredStepIndex { get; set; }

        /// <summary>
        /// 子 WorkFlow 的 SessionID
        /// </summary>
        public string SubWorkflowSessionID { get; set; }

        /// <summary>
        /// 子 WorkFlow 的 ProcessesID（步骤级隔离）
        /// </summary>
        public string SubWorkflowProcessesID { get; set; }

        /// <summary>
        /// 子 WorkFlow 的 TaskID
        /// </summary>
        public string SubWorkflowTaskID { get; set; }

        /// <summary>
        /// 当前迭代次数
        /// </summary>
        public int CurrentIteration { get; set; }

        /// <summary>
        /// 最大迭代次数
        /// </summary>
        public int MaxIterations { get; set; }

        // ===== 运行时参数（恢复时需要） =====

        public string AppID { get; set; }
        public string SessionID { get; set; }
        public string ProcessesID { get; set; }
        public string TaskID { get; set; }
        public string FromMainTaskID { get; set; }
        public string MemberID { get; set; }
        public string OriginalTask { get; set; }
        public string RecordID { get; set; }

        /// <summary>
        /// 输入参数 JSON
        /// </summary>
        public string InputsJson { get; set; }

        /// <summary>
        /// 已完成的步骤结果 JSON: Dictionary&lt;stepId, result&gt;
        /// </summary>
        public string CompletedStepResultsJson { get; set; }

        /// <summary>
        /// 本轮剩余待执行步骤 ID 列表 JSON
        /// </summary>
        public string PendingStepIdsJson { get; set; }

        /// <summary>
        /// 本轮所有待执行步骤 ID 列表（含当前触发的）JSON
        /// </summary>
        public string CurrentLayerStepIdsJson { get; set; }

        /// <summary>
        /// 当前层级索引（并行步骤按层执行，需要知道恢复到哪层）
        /// </summary>
        public int CurrentLayerIndex { get; set; }

        /// <summary>
        /// 总层数
        /// </summary>
        public int TotalLayers { get; set; }

        /// <summary>
        /// 执行日志 JSON
        /// </summary>
        public string LogsJson { get; set; }

        /// <summary>
        /// 规划模型配置 JSON（恢复时需要重建）
        /// </summary>
        public string PlanningModelConfigJson { get; set; }

        /// <summary>
        /// 反思模型配置 JSON
        /// </summary>
        public string ReflectionModelConfigJson { get; set; }

        /// <summary>
        /// 主模型配置 JSON
        /// </summary>
        public string MainModelConfigJson { get; set; }

        /// <summary>
        /// 异步触发最大等待时间（分钟），超时 Job 使用
        /// </summary>
        public int MaxAsyncWaitMinutes { get; set; } = 120;
    }

    /// <summary>
    /// 并行层级上下文（保存到 Redis，同一层所有并行步骤共享）
    /// 供 TryResumeClawAIStep 汇聚时使用
    /// </summary>
    public class ClawAILayerContext
    {
        /// <summary>
        /// 本层步骤总数
        /// </summary>
        public int TotalStepCount { get; set; }

        /// <summary>
        /// 本层所有步骤 ID
        /// </summary>
        public List<string> StepIds { get; set; } = new List<string>();

        /// <summary>
        /// 层级索引
        /// </summary>
        public int LayerIndex { get; set; }

        /// <summary>
        /// 总层数
        /// </summary>
        public int TotalLayers { get; set; }

        /// <summary>
        /// 是否允许步骤失败后继续
        /// </summary>
        public bool ContinueOnFailure { get; set; } = true;

        /// <summary>
        /// ClawAI 主流程 ProcessesID（用于恢复）
        /// </summary>
        public string ProcessesID { get; set; }

        /// <summary>
        /// ClawAI 节点 ID
        /// </summary>
        public string ClawAINodeId { get; set; }
    }
}
