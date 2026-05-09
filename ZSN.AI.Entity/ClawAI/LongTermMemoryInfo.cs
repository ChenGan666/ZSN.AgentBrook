using System;

namespace ZSN.AI.Entity.ClawAI
{
    /// <summary>
    /// tb_claw_long_term_memory - Claw AI长期记忆表
    /// 存储经过压缩和提炼的长期知识,支持语义检索
    /// </summary>
    public partial class LongTermMemoryInfo
    {
        public LongTermMemoryInfo() { }

        #region AutoField
        
        /// <summary>
        /// 记忆ID
        /// </summary>
        public string MemoryID { get; set; } = string.Empty;

        /// <summary>
        /// 应用ID
        /// </summary>
        public string AppID { get; set; } = string.Empty;

        /// <summary>
        /// ClawAI节点ID(用于区分同一APP下的多个ClawAI节点)
        /// </summary>
        public string ClawID { get; set; } = string.Empty;

        /// <summary>
        /// 会话ID(可选,用于关联特定会话)
        /// </summary>
        public string SessionID { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID(可选,用于关联特定用户)
        /// </summary>
        public string MemberID { get; set; } = string.Empty;

        /// <summary>
        /// 知识类型: concept(概念), fact(事实), procedure(流程), experience(经验)
        /// </summary>
        public string KnowledgeType { get; set; } = string.Empty;

        /// <summary>
        /// 主题/标签(用于分类)
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// 知识内容摘要
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 知识详细内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 向量嵌入(用于语义检索,存储为JSON数组字符串)
        /// </summary>
        public string Embedding { get; set; } = string.Empty;

        /// <summary>
        /// 重要性评分(0-100)
        /// </summary>
        public int Importance { get; set; } = 50;

        /// <summary>
        /// 访问次数(用于评估知识的使用频率)
        /// </summary>
        public int AccessCount { get; set; } = 0;

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime? LastAccessTime { get; set; }

        /// <summary>
        /// 来源类型: episodic(来自情景记忆), user_input(用户输入), system(系统生成)
        /// </summary>
        public string SourceType { get; set; } = string.Empty;

        /// <summary>
        /// 来源ID(关联的情景记忆ID或其他来源)
        /// </summary>
        public string SourceID { get; set; } = string.Empty;

        /// <summary>
        /// 元数据(JSON格式,存储额外信息)
        /// </summary>
        public string Metadata { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;

        #endregion
    }
}
