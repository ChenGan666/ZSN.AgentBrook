using Microsoft.Extensions.Logging;
using ZSN.AI.Entity;
using ZSN.AI.Node.Claw.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.Node.Claw.Analysis
{
    /// <summary>
    /// 文本相似度计算器 - 提供多种文本相似度计算方法
    /// </summary>
    public class TextSimilarityCalculator
    {
        private readonly ILogger _logger;

        // 缓存已提取的关键词，避免重复计算
        private readonly ConcurrentDictionary<string, HashSet<string>> _keywordCache = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _workflowKeywordCache = new();

        public TextSimilarityCalculator(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 计算简单文本相似度（快速方法）
        /// </summary>
        public double CalculateSimpleSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            {
                return 0.0;
            }

            if (text1.Equals(text2, StringComparison.OrdinalIgnoreCase))
            {
                return 1.0;
            }

            var words1 = ExtractWords(text1);
            var words2 = ExtractWords(text2);

            if (words1.Count == 0 || words2.Count == 0)
            {
                return 0.0;
            }

            // 计算交集
            int intersection = 0;
            foreach (var word in words1)
            {
                if (words2.Contains(word))
                {
                    intersection++;
                }
            }

            // Jaccard相似度
            int union = words1.Count + words2.Count - intersection;
            return union == 0 ? 0.0 : (double)intersection / union;
        }

        /// <summary>
        /// 计算文本相似度（完整方法，考虑缓存）
        /// </summary>
        public double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            {
                return 0.0;
            }

            if (text1.Equals(text2, StringComparison.OrdinalIgnoreCase))
            {
                return 1.0;
            }

            var words1 = GetOrExtractKeywords(text1);
            var words2 = GetOrExtractKeywords(text2);

            if (words1.Count == 0 || words2.Count == 0)
            {
                return 0.0;
            }

            // 计算交集
            int intersection = 0;
            foreach (var word in words1)
            {
                if (words2.Contains(word))
                {
                    intersection++;
                }
            }

            // Jaccard相似度
            int union = words1.Count + words2.Count - intersection;
            return union == 0 ? 0.0 : (double)intersection / union;
        }

        /// <summary>
        /// 计算WorkFlow匹配分数
        /// </summary>
        public double CalculateWorkflowMatchScore(HashSet<string> taskKeywords, WorkflowConfigInfo workflow)
        {
            var workflowKeywords = GetOrExtractWorkflowKeywords(workflow);

            if (workflowKeywords.Count == 0)
            {
                return 0.0;
            }

            // 计算交集
            int intersection = 0;
            foreach (var keyword in workflowKeywords)
            {
                if (taskKeywords.Contains(keyword))
                {
                    intersection++;
                }
            }

            // Jaccard相似度
            int union = taskKeywords.Count + workflowKeywords.Count - intersection;
            return union == 0 ? 0.0 : (double)intersection / union;
        }

        /// <summary>
        /// 提取文本中的关键词（带缓存）
        /// </summary>
        public HashSet<string> GetOrExtractKeywords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new HashSet<string>();
            }

            // 尝试从缓存获取
            if (_keywordCache.TryGetValue(text, out var cachedKeywords))
            {
                return cachedKeywords;
            }

            // 提取关键词
            var keywords = ExtractKeywords(text);

            // 存入缓存
            _keywordCache.TryAdd(text, keywords);

            return keywords;
        }

        /// <summary>
        /// 提取WorkFlow的关键词（带缓存）
        /// </summary>
        public HashSet<string> GetOrExtractWorkflowKeywords(WorkflowConfigInfo workflow)
        {
            string cacheKey = $"{workflow.workflowId}_{workflow.name}";

            // 尝试从缓存获取
            if (_workflowKeywordCache.TryGetValue(cacheKey, out var cachedKeywords))
            {
                return cachedKeywords;
            }

            // 提取关键词
            var keywords = ExtractWorkflowKeywords(workflow);

            // 存入缓存
            _workflowKeywordCache.TryAdd(cacheKey, keywords);

            return keywords;
        }

        /// <summary>
        /// 提取文本中的词语
        /// </summary>
        private HashSet<string> ExtractWords(string text)
        {
            var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 提取中文词语
            var chineseMatches = ClawAIRegexPatterns.ChineseWordExtractor.Matches(text);
            foreach (System.Text.RegularExpressions.Match match in chineseMatches)
            {
                words.Add(match.Value);
            }

            // 提取英文词语
            var englishMatches = ClawAIRegexPatterns.EnglishWordExtractor.Matches(text);
            foreach (System.Text.RegularExpressions.Match match in englishMatches)
            {
                words.Add(match.Value.ToLower());
            }

            // 提取数字
            var numberMatches = ClawAIRegexPatterns.NumberExtractor.Matches(text);
            foreach (System.Text.RegularExpressions.Match match in numberMatches)
            {
                words.Add(match.Value);
            }

            return words;
        }

        /// <summary>
        /// 提取关键词（优化版本）
        /// </summary>
        private HashSet<string> ExtractKeywords(string text)
        {
            return ExtractWords(text);
        }

        /// <summary>
        /// 提取WorkFlow的关键词
        /// </summary>
        private HashSet<string> ExtractWorkflowKeywords(WorkflowConfigInfo workflow)
        {
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 从名称提取
            if (!string.IsNullOrWhiteSpace(workflow.name))
            {
                var nameWords = ExtractWords(workflow.name);
                foreach (var word in nameWords)
                {
                    keywords.Add(word);
                }
            }

            // 从描述提取
            if (!string.IsNullOrWhiteSpace(workflow.description))
            {
                var descWords = ExtractWords(workflow.description);
                foreach (var word in descWords)
                {
                    keywords.Add(word);
                }
            }


            return keywords;
        }

        /// <summary>
        /// 检查字符串是否全为数字
        /// </summary>
        public bool IsAllDigits(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            foreach (char c in str)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _keywordCache.Clear();
            _workflowKeywordCache.Clear();
        }
    }
}
