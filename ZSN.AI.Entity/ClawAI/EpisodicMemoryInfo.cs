using System;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_episodic_memory 情景记忆表
    /// </summary>
    public partial class EpisodicMemoryInfo
    {
        public EpisodicMemoryInfo() { }

        #region AutoField
        /// <summary>
        /// 记忆ID
        /// </summary>
        public string MemoryID { get; set; }

        /// <summary>
        /// 应用ID
        /// </summary>
        public string AppID { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionID { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public string MemberID { get; set; }

        /// <summary>
        /// 事件类型: task_planning, agent_execution, reflection, etc.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// 事件上下文(JSON)
        /// </summary>
        public string EventContext { get; set; }

        /// <summary>
        /// 事件结果
        /// </summary>
        public string EventResult { get; set; }

        /// <summary>
        /// 记忆摘要
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// 向量嵌入(用于语义检索)
        /// </summary>
        public string Embedding { get; set; }

        /// <summary>
        /// 重要性(0-100)
        /// </summary>
        public int Importance { get; set; }

        /// <summary>
        /// 访问次数
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime? LastAccessTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        #endregion
    }
}
