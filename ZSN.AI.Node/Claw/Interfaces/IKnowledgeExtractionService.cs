using System.Collections.Generic;
using System.Threading.Tasks;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.Node.Claw.Interfaces
{
    /// <summary>
    /// 知识提炼服务接口
    /// 从对话中自动提取和提炼知识,更新到长期记忆
    /// </summary>
    public interface IKnowledgeExtractionService
    {
        /// <summary>
        /// 从对话中提取知识点
        /// </summary>
        /// <param name="userQuestion">用户提问</param>
        /// <param name="aiAnswer">AI回答</param>
        /// <param name="context">对话上下文</param>
        /// <returns>提取的知识点列表</returns>
        Task<List<ExtractedKnowledge>> ExtractKnowledgeFromDialogueAsync(
            string userQuestion,
            string aiAnswer,
            DialogueContext context);

        /// <summary>
        /// 将提取的知识更新到长期记忆
        /// </summary>
        /// <param name="knowledge">提取的知识</param>
        /// <param name="AppID">应用ID</param>
        /// <param name="ClawID">ClawAI节点ID</param>
        /// <param name="SessionID">会话ID</param>
        /// <param name="MemberID">用户ID</param>
        /// <param name="embeddingModelConfig">向量模型配置（可选）</param>
        /// <returns>更新的记忆ID</returns>
        Task<string> UpdateLongTermMemoryAsync(
            ExtractedKnowledge knowledge,
            string AppID,
            string ClawID,
            string SessionID,
            string MemberID,
            LargeModelConfig embeddingModelConfig = null);

        /// <summary>
        /// 批量处理对话历史,提取知识
        /// </summary>
        /// <param name="chatHistory">对话历史</param>
        /// <param name="AppID">应用ID</param>
        /// <param name="ClawID">ClawAI节点ID</param>
        /// <param name="SessionID">会话ID</param>
        /// <param name="MemberID">用户ID</param>
        /// <param name="embeddingModelConfig">向量模型配置（可选）</param>
        /// <returns>提取的知识数量</returns>
        Task<int> ProcessChatHistoryAsync(
            List<AppChatLogInfo> chatHistory,
            string AppID,
            string ClawID,
            string SessionID,
            string MemberID,
            LargeModelConfig embeddingModelConfig = null);

        /// <summary>
        /// 合并和去重相似知识
        /// </summary>
        /// <param name="AppID">应用ID</param>
        /// <param name="MemberID">用户ID</param>
        /// <returns>合并的知识数量</returns>
        Task<int> MergeAndDeduplicateKnowledgeAsync(
            string AppID,
            string MemberID);
    }

    /// <summary>
    /// 提取的知识点
    /// </summary>
    public class ExtractedKnowledge
    {
        /// <summary>
        /// 知识类型
        /// </summary>
        public KnowledgeType Type { get; set; }

        /// <summary>
        /// 主题/标签
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// 知识摘要
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// 知识详细内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 关键词列表
        /// </summary>
        public List<string> Keywords { get; set; } = new List<string>();

        /// <summary>
        /// 重要性评分(0-100)
        /// </summary>
        public int Importance { get; set; } = 50;

        /// <summary>
        /// 置信度(0-1)
        /// </summary>
        public double Confidence { get; set; } = 0.8;

        /// <summary>
        /// 来源对话
        /// </summary>
        public string SourceDialogue { get; set; }
    }

    /// <summary>
    /// 知识类型
    /// </summary>
    public enum KnowledgeType
    {
        /// <summary>
        /// 概念定义
        /// </summary>
        Concept,

        /// <summary>
        /// 事实信息
        /// </summary>
        Fact,

        /// <summary>
        /// 操作流程
        /// </summary>
        Procedure,

        /// <summary>
        /// 经验总结
        /// </summary>
        Experience,

        /// <summary>
        /// 问答对
        /// </summary>
        QA,

        /// <summary>
        /// 用户偏好
        /// </summary>
        Preference
    }

    /// <summary>
    /// 对话上下文
    /// </summary>
    public class DialogueContext
    {
        /// <summary>
        /// 对话历史
        /// </summary>
        public List<AppChatLogInfo> ChatHistory { get; set; } = new List<AppChatLogInfo>();

        /// <summary>
        /// 任务规划信息
        /// </summary>
        public TaskPlanning TaskPlanning { get; set; }

        /// <summary>
        /// 用户画像
        /// </summary>
        public UserProfile UserProfile { get; set; }

        /// <summary>
        /// 相关记忆
        /// </summary>
        public List<EpisodicMemory> RelevantMemories { get; set; } = new List<EpisodicMemory>();
    }
}
