using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using I = ZSN.AI.Core.Interface;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// Mock嵌入服务 - 用于测试
    /// 生成确定性的伪随机向量
    /// </summary>
    public class MockEmbeddingService : I.IEmbeddingService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MockEmbeddingService> _logger;

        public int VectorDimension { get; } = 1536;

        public MockEmbeddingService(
            IConfiguration configuration,
            ILogger<MockEmbeddingService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _logger.LogWarning("使用MockEmbeddingService（仅用于测试）");
        }

        /// <summary>
        /// 生成单个文本的向量（基于文本哈希的确定性向量）
        /// </summary>
        public Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("文本不能为空", nameof(text));
            }

            var embedding = GenerateDeterministicVector(text);
            return Task.FromResult(embedding);
        }

        /// <summary>
        /// 批量生成文本向量
        /// </summary>
        public Task<float[][]> GetEmbeddingsAsync(string[] texts, CancellationToken cancellationToken = default)
        {
            if (texts == null || texts.Length == 0)
            {
                throw new ArgumentException("文本列表不能为空", nameof(texts));
            }

            var embeddings = texts.Select(GenerateDeterministicVector).ToArray();
            return Task.FromResult(embeddings);
        }

        /// <summary>
        /// 基于文本生成确定性向量（相同文本生成相同向量）
        /// </summary>
        private float[] GenerateDeterministicVector(string text)
        {
            var vector = new float[VectorDimension];

            // 使用文本的哈希值作为种子
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text));

            // 使用哈希值初始化随机数生成器
            var seed = BitConverter.ToInt32(hash, 0);
            var random = new Random(seed);

            // 生成单位向量（归一化）
            for (int i = 0; i < VectorDimension; i++)
            {
                vector[i] = (float)(random.NextDouble() * 2 - 1); // -1 到 1 之间
            }

            // 归一化
            var magnitude = (float)Math.Sqrt(vector.Sum(x => x * x));
            for (int i = 0; i < VectorDimension; i++)
            {
                vector[i] /= magnitude;
            }

            return vector;
        }
    }
}
