namespace ZSN.AI.Core.Utils
{
    /// <summary>
    /// Token 计数器接口
    /// </summary>
    public interface ITokenCounter
    {
        /// <summary>
        /// 计算文本的 Token 数量
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>Token 数量</returns>
        int CountTokens(string text);

        /// <summary>
        /// 批量计算多个文本的 Token 数量
        /// </summary>
        /// <param name="texts">文本集合</param>
        /// <returns>Token 数量数组</returns>
        int[] CountTokensBatch(IEnumerable<string> texts);

        /// <summary>
        /// 清空缓存
        /// </summary>
        void ClearCache();
    }

    /// <summary>
    /// Token 计数器实现
    /// </summary>
    public class TokenCounter : ITokenCounter
    {
        private readonly Dictionary<string, int> _tokenCache = new();

        /// <summary>
        /// 计算 Token 数量（简化版本）
        /// </summary>
        /// <remarks>
        /// 这是一个简化的实现，按中文字符和英文单词估算
        /// 生产环境应使用实际的 Tokenizer（如 TikTokenizer）
        /// </remarks>
        public int CountTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // 检查缓存
            if (_tokenCache.TryGetValue(text, out var count))
                return count;

            // 简化的 Token 计算逻辑
            int tokens = 0;

            foreach (char c in text)
            {
                // 中文字符、日文、韩文等 CJK 字符通常每个字算一个 Token
                if (c >= 0x4E00 && c <= 0x9FFF) // CJK 统一汉字
                {
                    tokens++;
                }
                // 其他字符按单词估算（简化处理）
                else if (char.IsWhiteSpace(c))
                {
                    // 空格不单独计 Token
                }
                else
                {
                    // 英文字母和符号约 4 个字符 = 1 Token
                    tokens++;
                }
            }

            // 调整：英文部分通常 4 个字符约等于 1 Token
            // 非CJK字符部分除以4
            int nonCJKChars = text.Count(c => c < 0x4E00 || c > 0x9FFF);
            int estimatedTokens = tokens + (nonCJKChars / 4);

            // 缓存结果（仅缓存短文本）
            if (text.Length < 1000)
            {
                _tokenCache[text] = estimatedTokens;
            }

            return estimatedTokens;
        }

        /// <summary>
        /// 批量计算 Token 数量
        /// </summary>
        public int[] CountTokensBatch(IEnumerable<string> texts)
        {
            return texts.Select(CountTokens).ToArray();
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void ClearCache()
        {
            _tokenCache.Clear();
        }
    }

    /// <summary>
    /// Token 计数扩展方法
    /// </summary>
    public static class TokenCounterExtensions
    {
        /// <summary>
        /// 计算字符串的 Token 数量
        /// </summary>
        public static int GetTokenCount(this string text)
        {
            var counter = new TokenCounter();
            return counter.CountTokens(text);
        }

        /// <summary>
        /// 估算字符串的 Token 数量（快速方法）
        /// </summary>
        /// <remarks>
        /// 使用更简单的启发式规则：中文*1 + 英文*0.25
        /// </remarks>
        public static int EstimateTokenCount(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int cjkCount = 0;
            int otherCount = 0;

            foreach (char c in text)
            {
                if (c >= 0x4E00 && c <= 0x9FFF)
                    cjkCount++;
                else if (!char.IsWhiteSpace(c))
                    otherCount++;
            }

            return cjkCount + (otherCount / 4);
        }
    }
}
