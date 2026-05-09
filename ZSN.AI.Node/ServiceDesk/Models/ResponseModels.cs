namespace ZSN.AI.Node.ServiceDesk.Models
{
    /// <summary>ServiceDesk 回复结果</summary>
    public class ServiceDeskResponse
    {
        /// <summary>回复内容</summary>
        public string Content { get; set; }

        /// <summary>使用的处理策略</summary>
        public ProcessingStrategy Strategy { get; set; }

        /// <summary>置信度 (0-1)</summary>
        public double Confidence { get; set; }

        /// <summary>知识来源列表</summary>
        public List<KnowledgeSource> Sources { get; set; } = new List<KnowledgeSource>();

        /// <summary>检索结果数量</summary>
        public int RetrievalCount { get; set; }

        /// <summary>引用标注</summary>
        public string Citations { get; set; }

        /// <summary>是否需要升级到 ClawAI</summary>
        public bool NeedsEscalation { get; set; }

        /// <summary>升级原因</summary>
        public string EscalationReason { get; set; }

        /// <summary>是否为兜底回复</summary>
        public bool IsFallback { get; set; }

        /// <summary>生成耗时（毫秒）</summary>
        public long ElapsedMs { get; set; }

        /// <summary>相关建议</summary>
        public List<string> Suggestions { get; set; } = new List<string>();
    }

    /// <summary>记忆上下文（简化版，用于分类和检索时传递对话历史）</summary>
    public class MemoryContext
    {
        /// <summary>短期记忆（最近对话记录）</summary>
        public List<ChatMessageRecord> ShortTermMemory { get; set; } = new List<ChatMessageRecord>();

        /// <summary>最近话题</summary>
        public string LastTopic { get; set; }
    }

    /// <summary>对话记录</summary>
    public class ChatMessageRecord
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
