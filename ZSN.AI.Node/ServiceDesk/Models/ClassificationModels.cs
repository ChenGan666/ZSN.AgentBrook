namespace ZSN.AI.Node.ServiceDesk.Models
{
    /// <summary>消息类型</summary>
    public enum MessageType
    {
        Greeting,       // 问候语
        SmallTalk,      // 闲聊
        SimpleQA,       // 简单问答
        ComplexQuery,   // 复杂查询
        Complaint,      // 投诉
        Unknown         // 未知
    }

    /// <summary>消息复杂度</summary>
    public enum MessageComplexity
    {
        Simple,   // 简单
        Medium,   // 中等
        Complex   // 复杂
    }

    /// <summary>处理策略</summary>
    public enum ProcessingStrategy
    {
        DirectReply,         // 直接回复（模板回复）
        KnowledgeRetrieval,  // 知识库检索
        RAGEnhanced,         // RAG 增强（检索 + LLM）
        EscalateToClawAI     // 升级到 ClawAI
    }

    /// <summary>分类结果</summary>
    public class ClassificationResult
    {
        /// <summary>消息类型</summary>
        public MessageType Type { get; set; }

        /// <summary>检测到的意图</summary>
        public string Intent { get; set; }

        /// <summary>置信度 (0-1)</summary>
        public double Confidence { get; set; }

        /// <summary>消息复杂度</summary>
        public MessageComplexity Complexity { get; set; }

        /// <summary>处理策略</summary>
        public ProcessingStrategy Strategy { get; set; }

        /// <summary>推理追踪（用于调试）</summary>
        public string ReasoningTrace { get; set; }

        /// <summary>分类耗时（毫秒）</summary>
        public long ElapsedMs { get; set; }
    }

    /// <summary>意图检测结果</summary>
    public class IntentDetectionResult
    {
        public string IntentName { get; set; } = "Unknown";
        public double Confidence { get; set; }
        public List<string> RequiredFields { get; set; } = new List<string>();
        public bool RequiresConfirmation { get; set; }
    }
}
