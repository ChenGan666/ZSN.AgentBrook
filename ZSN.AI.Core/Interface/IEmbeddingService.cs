using System.Threading.Tasks;

namespace ZSN.AI.Core.Interface
{
    /// <summary>
    /// 文本向量化服务接口
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// 生成单个文本的向量
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>向量数组</returns>
        Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量生成文本向量
        /// </summary>
        /// <param name="texts">文本列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>向量数组列表</returns>
        Task<float[][]> GetEmbeddingsAsync(string[] texts, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取向量维度
        /// </summary>
        int VectorDimension { get; }
    }
}
