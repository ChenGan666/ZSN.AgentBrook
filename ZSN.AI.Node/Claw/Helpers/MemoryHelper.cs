using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZSN.AI.BLL;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Models;

namespace ZSN.AI.Node.Helpers
{
    /// <summary>
    /// 记忆辅助类 - P3优化
    /// 功能：文本相似度计算、分词、主题提取、对话摘要、重要性计算等
    /// </summary>
    public static class MemoryHelper
    {
        /// <summary>
        /// 计算文本的Jaccard相似度
        /// Jaccard相似度 = |A ∩ B| / |A ∪ B|
        /// </summary>
        /// <param name="text1">文本1</param>
        /// <param name="text2">文本2</param>
        /// <returns>相似度 (0-1)</returns>
        public static float CalculateTextSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            {
                return 0f;
            }

            var words1 = TokenizeText(text1);
            var words2 = TokenizeText(text2);

            var intersection = words1.Intersect(words2).ToList();
            var union = words1.Union(words2).ToList();

            return union.Count == 0 ? 0f : (float)intersection.Count / union.Count;
        }

        /// <summary>
        /// 文本分词
        /// </summary>
        /// <param name="text">待分词的文本</param>
        /// <returns>词集合（去重）</returns>
        public static HashSet<string> TokenizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new HashSet<string>();
            }

            // 分隔符：空格、标点符号、换行等
            var separators = new char[] { ' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':', '"', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '|', '-', '_', '+', '=', '*', '&', '^', '%', '$', '#', '@', '~', '`' };

            var words = text.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                           .Select(word => word.Trim().ToLower())
                           .Where(word => word.Length >= 2) // 只保留长度>=2的词
                           .Distinct();

            return new HashSet<string>(words);
        }

        /// <summary>
        /// 从查询中提取主题
        /// </summary>
        /// <param name="query">查询文本</param>
        /// <returns>主题关键词</returns>
        public static string ExtractTopic(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return "通用";
            }

            // 简单提取：取前50个字符作为主题
            var topic = query.Length > 50 ? query.Substring(0, 50) : query;

            // 移除特殊字符
            var separators = new char[] { '\n', '\r', '\t', '?', '!', '.', ',', ';', ':', '"', '\'' };
            var cleanTopic = topic.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                                   .FirstOrDefault()?.Trim();

            return !string.IsNullOrEmpty(cleanTopic) ? cleanTopic : "通用";
        }

        /// <summary>
        /// 生成对话摘要
        /// </summary>
        /// <param name="question">用户问题</param>
        /// <param name="answer">AI回答</param>
        /// <returns>对话摘要</returns>
        public static string SummarizeConversation(string question, string answer)
        {
            if (string.IsNullOrEmpty(question))
            {
                return string.IsNullOrEmpty(answer) ? "" : answer.Substring(0, Math.Min(200, answer.Length));
            }

            // 摘要格式：Q: [问题的前100字符] A: [回答的前100字符]
            var q = question.Length > 100 ? question.Substring(0, 100) + "..." : question;
            var a = string.IsNullOrEmpty(answer) ? "" : (answer.Length > 100 ? answer.Substring(0, 100) + "..." : answer);

            return $"Q: {q}\nA: {a}";
        }

        /// <summary>
        /// 计算初始重要性分数
        /// </summary>
        /// <param name="planning">任务规划信息</param>
        /// <returns>重要性分数 (0-100)</returns>
        public static int CalculateInitialImportance(TaskPlanning planning)
        {
            if (planning == null)
            {
                return 50; // 默认中等重要性
            }

            int score = 50; // 基础分

            // 根据置信度调整 (±20分)
            score += (int)((planning.Metadata.Confidence - 50) * 0.4);

            // 根据任务步骤数调整 (±10分)
            if (planning.TotalSteps > 0)
            {
                score += Math.Min(10, planning.TotalSteps / 2);
            }

            // 根据策略类型调整
            if (!string.IsNullOrEmpty(planning.Metadata.Strategy))
            {
                if (planning.Metadata.Strategy.Contains("complex", StringComparison.OrdinalIgnoreCase))
                {
                    score += 10; // 复杂任务更重要的
                }
                else if (planning.Metadata.Strategy.Contains("simple", StringComparison.OrdinalIgnoreCase))
                {
                    score -= 5; // 简单任务相对不重要
                }
            }

            // 限制在0-100范围内
            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// 判断是否应该保存到长期记忆
        /// </summary>
        /// <param name="param">后处理参数</param>
        /// <returns>是否应该保存</returns>
        public static bool ShouldSaveToLongTermMemory(PostProcessingParams param)
        {
            // 1. 必须有最终结果
            if (string.IsNullOrEmpty(param.FinalResult))
            {
                return false;
            }

            // 2. 最终结果长度必须足够（至少50个字符）
            if (param.FinalResult.Length < 50)
            {
                return false;
            }

            // 3. 检查任务规划质量
            if (param.TaskPlanning != null)
            {
                // 如果置信度太低（<30），不保存
                if (param.TaskPlanning.Metadata.Confidence < 30)
                {
                    return false;
                }

                // 如果所有步骤质量都很低，不保存
                if (param.TaskPlanning.Steps != null && param.TaskPlanning.Steps.Count > 0)
                {
                    var avgQuality = param.TaskPlanning.Steps.Average(s => s.QualityScore ?? 0);
                    if (avgQuality < 30)
                    {
                        return false;
                    }
                }
            }

            // 4. 反思质量检查
            if (param.ReflectionResult != null)
            {
                // 只有反思质量>=60才保存
                if (param.ReflectionResult.OverallQuality < 60)
                {
                    return false;
                }
            }

            // 5. 检查是否有交互历史
            if (param.MemoryContext.WorkingMemory != null && param.MemoryContext.WorkingMemory.Count > 0)
            {
                // 如果交互次数太少（<2轮），可能不重要的对话
                if (param.MemoryContext.WorkingMemory.Count < 2)
                {
                    return false;
                }
            }

            // 6. 检查是否是错误状态
            if (param.ReflectionResult != null)
            {
                var action = param.ReflectionResult.Action.ToString();
                if (action.Contains("Fail", StringComparison.OrdinalIgnoreCase) ||
                    action.Contains("Error", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
