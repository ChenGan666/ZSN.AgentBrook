using System;
using System.Threading.Tasks;
using ZSN.AI.Entity;
using ZSN.AI.Node.Claw.Models;

namespace ZSN.AI.Node.Claw.Interfaces
{
    /// <summary>
    /// 记忆整理服务接口
    /// 用于定时对ClawAI的记忆进行重新组织和优化
    /// 支持三级记忆层级：会话级、ClawAI级、APP级
    /// 不影响现有的实时记忆处理逻辑
    /// </summary>
    public interface IMemoryConsolidationService
    {
        /// <summary>
        /// 执行 ClawAI 级记忆整理（包含会话级提炼）
        /// </summary>
        /// <param name="appId">应用ID</param>
        /// <param name="clawId">ClawAI节点ID</param>
        /// <param name="cutoffTime">增量时间窗口截止时间，仅处理此时间之后新增的记忆</param>
        /// <param name="modelInfo">LLM模型信息，用于知识提炼和通用性评估</param>
        /// <returns>整理结果摘要</returns>
        Task<MemoryConsolidationResult> ConsolidateClawAIAsync(
            string appId,
            string clawId,
            DateTime cutoffTime,
            LargeModelInfo modelInfo,
            LargeModelInfo embeddingModelInfo);

        /// <summary>
        /// 执行 APP 级记忆整理（跨ClawAI的知识聚合和共享）
        /// </summary>
        /// <param name="appId">应用ID</param>
        /// <param name="cutoffTime">增量时间窗口截止时间</param>
        /// <param name="modelInfo">LLM模型信息</param>
        /// <param name="embeddingModelInfo">嵌入模型信息，用于生成向量</param>
        /// <returns>整理结果摘要</returns>
        Task<MemoryConsolidationResult> ConsolidateAppAsync(
            string appId,
            DateTime cutoffTime,
            LargeModelInfo modelInfo,
            LargeModelInfo embeddingModelInfo);

        /// <summary>
        /// 清理低价值记忆（按指定层级）
        /// </summary>
        /// <param name="scope">记忆层级上下文</param>
        Task<int> CleanupLowValueMemoriesAsync(MemoryScopeContext scope);

        /// <summary>
        /// 深度知识提炼：将会话级情景记忆提炼为ClawAI级长期记忆
        /// </summary>
        /// <param name="scope">ClawAI级上下文（必须指定ClawID）</param>
        /// <param name="modelInfo">LLM模型信息</param>
        /// <param name="batchSize">每批处理数量</param>
        /// <param name="cutoffTime">增量时间窗口截止时间，仅处理此时间之后的情景记忆</param>
        Task<int> DeepKnowledgeExtractionAsync(
            MemoryScopeContext scope,
            LargeModelInfo modelInfo,
            int batchSize = 20,
            DateTime? cutoffTime = null);

        /// <summary>
        /// 层级化知识图谱构建
        /// 当scope为ClawAI时：构建该ClawAI内跨会话的知识关系
        /// 当scope为App时：构建该APP内跨ClawAI的知识关系
        /// </summary>
        Task<int> BuildScopedKnowledgeGraphAsync(
            MemoryScopeContext scope,
            LargeModelInfo modelInfo,
            int batchSize = 20);

        /// <summary>
        /// 任务规划经验整理（按ClawID分组）
        /// </summary>
        Task<int> ConsolidatePlanningExperienceAsync(
            MemoryScopeContext scope,
            LargeModelInfo modelInfo);

        /// <summary>
        /// 知识层级提升：将ClawAI级的通用知识提升到APP级
        /// </summary>
        /// <param name="appId">应用ID</param>
        /// <param name="modelInfo">LLM模型信息</param>
        /// <param name="sourceClawId">源ClawAI节点ID，为空则扫描该APP下所有ClawAI</param>
        /// <param name="generalityThreshold">通用性阈值（0-100），高于此值才提升</param>
        /// <returns>提升的知识数量</returns>
        Task<int> PromoteKnowledgeToAppLevelAsync(
            string appId,
            LargeModelInfo modelInfo,
            LargeModelInfo embeddingModelInfo,
            string sourceClawId = null,
            int generalityThreshold = 70);

        /// <summary>
        /// 记忆合并与重评分（按指定层级）
        /// </summary>
        Task<int> MergeAndRescoreMemoriesAsync(MemoryScopeContext scope);
    }

    /// <summary>
    /// 记忆整理结果
    /// </summary>
    public class MemoryConsolidationResult
    {
        /// <summary>
        /// 整理层级
        /// </summary>
        public MemoryScope Scope { get; set; }

        /// <summary>
        /// 整理范围描述
        /// </summary>
        public string ScopeLabel { get; set; }

        /// <summary>
        /// 清理的记忆数量
        /// </summary>
        public int CleanedMemories { get; set; }

        /// <summary>
        /// 提炼的知识数量
        /// </summary>
        public int ExtractedKnowledge { get; set; }

        /// <summary>
        /// 构建的图谱关系数量
        /// </summary>
        public int GraphRelationsBuilt { get; set; }

        /// <summary>
        /// 整理的规划经验数量
        /// </summary>
        public int PlanningExperienceConsolidated { get; set; }

        /// <summary>
        /// 提升到APP级的知识数量
        /// </summary>
        public int PromotedToAppLevel { get; set; }

        /// <summary>
        /// 合并的记忆数量
        /// </summary>
        public int MergedMemories { get; set; }

        /// <summary>
        /// 整理摘要
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public DateTime ExecutedAt { get; set; } = DateTime.Now;
    }
}
