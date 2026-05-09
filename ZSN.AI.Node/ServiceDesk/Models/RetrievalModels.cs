namespace ZSN.AI.Node.ServiceDesk.Models
{
    /// <summary>知识检索结果</summary>
    public class KnowledgeRetrievalResult
    {
        /// <summary>原始查询</summary>
        public string Query { get; set; }

        /// <summary>重写后的查询</summary>
        public string RewrittenQuery { get; set; }

        /// <summary>检索结果列表</summary>
        public List<RetrievalItem> Items { get; set; } = new List<RetrievalItem>();

        /// <summary>总结果数（去重前）</summary>
        public int TotalCount { get; set; }

        /// <summary>整体置信度 (0-1)</summary>
        public double Confidence { get; set; }

        /// <summary>检索耗时（毫秒）</summary>
        public long ElapsedMs { get; set; }

        /// <summary>涉及的知识源</summary>
        public List<KnowledgeSource> Sources { get; set; } = new List<KnowledgeSource>();
    }

    /// <summary>检索结果条目</summary>
    public class RetrievalItem
    {
        /// <summary>内容</summary>
        public string Content { get; set; }

        /// <summary>原始检索分数</summary>
        public double Score { get; set; }

        /// <summary>重排序后的最终分数</summary>
        public double FinalScore { get; set; }

        /// <summary>知识来源</summary>
        public KnowledgeSource Source { get; set; } = new KnowledgeSource();

        /// <summary>元数据</summary>
        public Dictionary<string, string> Metadata { get; set; }
    }

    /// <summary>知识来源</summary>
    public class KnowledgeSource
    {
        /// <summary>知识库 ID</summary>
        public string KnowledgeBaseId { get; set; }

        /// <summary>知识库名称</summary>
        public string KnowledgeBaseName { get; set; }

        /// <summary>文档 ID</summary>
        public string DocumentId { get; set; }

        /// <summary>文档标题</summary>
        public string DocumentTitle { get; set; }

        /// <summary>分块 ID</summary>
        public string ChunkId { get; set; }
    }
}
