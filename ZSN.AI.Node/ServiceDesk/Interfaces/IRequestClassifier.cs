using ZSN.AI.Entity;
using ZSN.AI.Node.ServiceDesk.Models;

namespace ZSN.AI.Node.ServiceDesk.Interfaces
{
    /// <summary>请求分类器接口</summary>
    public interface IRequestClassifier
    {
        /// <summary>
        /// 分类用户请求，决定处理策略
        /// </summary>
        Task<ClassificationResult> ClassifyRequestAsync(
            string userMessage,
            MemoryContext memoryContext,
            ServiceDeskData config);
    }
}
