using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ZSN.AI.BLL;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Utils;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.Node.Helpers
{
    /// <summary>
    /// 记忆去重辅助类 - P3优化
    /// 功能：检查并避免保存重复的记忆
    /// </summary>
    public static class MemoryDeduplicationHelper
    {
        /// <summary>
        /// 检查是否为重复记忆
        /// </summary>
        /// <param name="summary">记忆摘要</param>
        /// <param name="content">记忆内容</param>
        /// <param name="appID">应用ID</param>
        /// <param name="clawID">ClawAI实例ID</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>是否为重复记忆</returns>
        public static bool IsDuplicateMemory(
            string summary,
            string content,
            string appID,
            string clawID,
            ILogger logger = null)
        {
            try
            {
                // 1. 查找相似的记忆（关键词搜索）
                var existingMemories = LongTermMemoryBusiness.SearchByKeywords(
                    appID,
                    summary,
                    limit: 10);

                if (existingMemories == null || existingMemories.Count == 0)
                {
                    LoggerHelper.LogInfo(logger, ClawLogModules.MEMORY,
                        $"去重检查：未找到相关记忆，继续保存");
                    return false;
                }

                // 2. 检查是否有高度相似的记忆
                foreach (var existing in existingMemories)
                {
                    // 计算摘要相似度
                    float summarySimilarity = MemoryHelper.CalculateTextSimilarity(
                        summary,
                        existing.Summary ?? "");

                    // 计算内容相似度
                    float contentSimilarity = MemoryHelper.CalculateTextSimilarity(
                        content,
                        existing.Content ?? "");

                    LoggerHelper.LogDebug(logger, ClawLogModules.MEMORY,
                        $"去重检查：与记忆 [{existing.MemoryID}] 相似度 - 摘要:{summarySimilarity:P2}, 内容:{contentSimilarity:P2}");

                    // 3. 如果摘要或内容相似度 > 90%，认为是重复
                    if (summarySimilarity > 0.9f || contentSimilarity > 0.9f)
                    {
                        LoggerHelper.LogInfo(logger, ClawLogModules.MEMORY,
                            $"发现重复记忆：与 [{existing.MemoryID}] 相似度过高（摘要:{summarySimilarity:P2}, 内容:{contentSimilarity:P2}），跳过保存");
                        return true;
                    }
                }

                LoggerHelper.LogInfo(logger, ClawLogModules.MEMORY,
                    $"去重检查：未发现重复记忆，继续保存");
                return false;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(logger, ClawLogModules.MEMORY,
                    $"去重检查失败：{ex.Message}");
                // 出错时不阻止保存，返回false
                return false;
            }
        }

        /// <summary>
        /// 批量检查重复记忆
        /// </summary>
        /// <param name="memories">待检查的记忆列表</param>
        /// <param name="appID">应用ID</param>
        /// <param name="clawID">ClawAI实例ID</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>去重后的记忆列表</returns>
        public static List<(LongTermMemoryInfo memory, bool isDuplicate)> BatchCheckDuplicates(
            List<LongTermMemoryInfo> memories,
            string appID,
            string clawID,
            ILogger logger = null)
        {
            var results = new List<(LongTermMemoryInfo, bool)>();

            foreach (var memory in memories)
            {
                bool isDup = IsDuplicateMemory(
                    memory.Summary ?? "",
                    memory.Content ?? "",
                    appID,
                    clawID,
                    logger
                );

                results.Add((memory, isDup));
            }

            return results;
        }

        /// <summary>
        /// 查找相似的记忆
        /// </summary>
        /// <param name="summary">摘要</param>
        /// <param name="content">内容</param>
        /// <param name="appID">应用ID</param>
        /// <param name="clawID">ClawAI实例ID</param>
        /// <param name="threshold">相似度阈值（默认0.9）</param>
        /// <param name="limit">返回数量限制</param>
        /// <returns>相似的记忆列表</returns>
        public static List<LongTermMemoryInfo> FindSimilarMemories(
            string summary,
            string content,
            string appID,
            string clawID,
            float threshold = 0.9f,
            int limit = 10)
        {
            try
            {
                var existingMemories = LongTermMemoryBusiness.SearchByKeywords(
                    appID,
                    summary,
                    limit: limit);

                if (existingMemories == null || existingMemories.Count == 0)
                {
                    return new List<LongTermMemoryInfo>();
                }

                var similarMemories = new List<LongTermMemoryInfo>();

                foreach (var existing in existingMemories)
                {
                    float summarySimilarity = MemoryHelper.CalculateTextSimilarity(
                        summary,
                        existing.Summary ?? "");

                    float contentSimilarity = MemoryHelper.CalculateTextSimilarity(
                        content,
                        existing.Content ?? "");

                    // 只要摘要或内容相似度超过阈值就认为相似
                    if (summarySimilarity > threshold || contentSimilarity > threshold)
                    {
                        similarMemories.Add(existing);
                    }
                }

                return similarMemories;
            }
            catch (Exception)
            {
                return new List<LongTermMemoryInfo>();
            }
        }
    }
}
