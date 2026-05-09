using System;

namespace ZSN.AI.Node.Claw.Models
{
    /// <summary>
    /// 记忆作用域/层级
    /// </summary>
    public enum MemoryScope
    {
        /// <summary>
        /// 会话级：仅在单个会话内可见
        /// </summary>
        Session = 1,

        /// <summary>
        /// ClawAI级：该ClawAI节点的所有会话可见
        /// </summary>
        ClawAI = 2,

        /// <summary>
        /// APP级：该APP下所有ClawAI节点可见
        /// </summary>
        App = 3
    }

    /// <summary>
    /// 记忆层级上下文 - 描述一次整理操作的作用层级
    /// </summary>
    public class MemoryScopeContext
    {
        /// <summary>
        /// 当前整理层级
        /// </summary>
        public MemoryScope Scope { get; set; }

        /// <summary>
        /// APP ID（APP级和ClawAI级必须有值）
        /// </summary>
        public string AppID { get; set; }

        /// <summary>
        /// ClawAI节点ID（ClawAI级必须有值，APP级为空）
        /// </summary>
        public string ClawID { get; set; }

        /// <summary>
        /// 会话ID（仅会话级有值）
        /// </summary>
        public string SessionID { get; set; }

        /// <summary>
        /// 获取层级的描述标签
        /// </summary>
        public string ScopeLabel =>
            Scope switch
            {
                MemoryScope.App => $"APP:{AppID}",
                MemoryScope.ClawAI => $"APP:{AppID}/Claw:{ClawID}",
                MemoryScope.Session => $"APP:{AppID}/Claw:{ClawID}/Session:{SessionID}",
                _ => "Unknown"
            };
    }

    /// <summary>
    /// 知识提升请求：从低层级提升到高层级
    /// </summary>
    public class KnowledgePromotionRequest
    {
        /// <summary>
        /// 源记忆ID
        /// </summary>
        public string SourceMemoryID { get; set; }

        /// <summary>
        /// 源层级
        /// </summary>
        public MemoryScope SourceScope { get; set; }

        /// <summary>
        /// 目标层级
        /// </summary>
        public MemoryScope TargetScope { get; set; }

        /// <summary>
        /// 目标AppID
        /// </summary>
        public string TargetAppID { get; set; }

        /// <summary>
        /// 目标ClawID（提升到APP级时为空）
        /// </summary>
        public string TargetClawID { get; set; }

        /// <summary>
        /// LLM判定的通用性评分（0-100，越高越通用）
        /// </summary>
        public int GeneralityScore { get; set; }

        /// <summary>
        /// 提升原因
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// 知识提升结果
    /// </summary>
    public class KnowledgePromotionResult
    {
        /// <summary>
        /// 新创建的目标记忆ID
        /// </summary>
        public string TargetMemoryID { get; set; }

        /// <summary>
        /// 源记忆ID
        /// </summary>
        public string SourceMemoryID { get; set; }

        /// <summary>
        /// 提升到的层级
        /// </summary>
        public MemoryScope TargetScope { get; set; }

        /// <summary>
        /// 通用性评分
        /// </summary>
        public int GeneralityScore { get; set; }
    }
}
