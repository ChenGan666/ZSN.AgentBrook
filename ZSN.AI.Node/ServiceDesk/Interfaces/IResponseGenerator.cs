using ZSN.AI.Entity;
using ZSN.AI.Node.ServiceDesk.Models;
using ZSN.AI.Node.Utils.Pipeline;

namespace ZSN.AI.Node.ServiceDesk.Interfaces
{
    /// <summary>响应生成器接口</summary>
    public interface IResponseGenerator
    {
        /// <summary>
        /// 生成用户回复（支持流式输出）
        /// </summary>
        Task<ServiceDeskResponse> GenerateResponseAsync(
            string userQuery,
            ClassificationResult classification,
            KnowledgeRetrievalResult retrievalResult,
            MemoryContext memoryContext,
            ServiceDeskData config,
            StreamBatchWriter streamWriter);

        /// <summary>
        /// 通过 FunctionCall 生成回复（LLM 自主调用知识库检索工具）
        /// </summary>
        Task<ServiceDeskResponse> GenerateFunctionCallResponseAsync(
            string userQuery,
            ServiceDeskData config,
            StreamBatchWriter streamWriter,
            List<ChatMessageRecord> chatHistory = null);

        /// <summary>
        /// 直接回复（问候/闲聊）
        /// </summary>
        ServiceDeskResponse GenerateDirectReply(
            string userQuery,
            ClassificationResult classification,
            StreamBatchWriter streamWriter);
    }
}
