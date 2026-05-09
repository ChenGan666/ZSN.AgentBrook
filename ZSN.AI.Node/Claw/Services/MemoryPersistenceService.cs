using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZSN.AI.BLL;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.Node.Claw.Models;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Node.Helpers;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.Node.Services
{
    /// <summary>
    /// 记忆持久化服务 - P3优化
    /// 功能：保存重要对话到长期记忆、保存反思到记忆、构建知识图谱关系等
    /// </summary>
    public static class MemoryPersistenceService
    {
        /// <summary>
        /// 保存重要对话到长期记忆
        /// </summary>
        /// <param name="param">后处理参数</param>
        /// <param name="logger">日志记录器</param>
        public static async Task SaveImportantConversationToLongTermMemoryAsync(
            PostProcessingParams param,
            ILogger logger = null)
        {
            try
            {
                // 1. 检查是否应该保存
                if (!MemoryHelper.ShouldSaveToLongTermMemory(param))
                {
                    LoggerHelper.LogDebug(logger, ClawLogModules.MEMORY,
                        "记忆保存检查：未满足保存条件，跳过保存");
                    return;
                }

                // 2. 提取记忆信息
                var topic = MemoryHelper.ExtractTopic(param.OriginalTask);
                var summary = MemoryHelper.SummarizeConversation(param.OriginalTask, param.FinalResult);
                var content = $"Q: {param.OriginalTask}\n\nA: {param.FinalResult}";

                // 3. P3优化: 检查重复记忆
                bool isDuplicate = MemoryDeduplicationHelper.IsDuplicateMemory(
                    summary, content, param.AppID, param.ClawID, logger);

                if (isDuplicate)
                {
                    param.Logs.Add("⚠ 检测到重复记忆，已跳过保存");
                    return;
                }

                // 4. 计算重要性
                var importance = param.TaskPlanning != null
                    ? MemoryHelper.CalculateInitialImportance(param.TaskPlanning)
                    : 50;

                // 5. 创建记忆对象
                var memory = new LongTermMemoryInfo
                {
                    MemoryID = Guid.NewGuid().ToString(),
                    AppID = param.AppID,
                    ClawID = param.ClawID,
                    SessionID = param.SessionID,
                    MemberID = param.MemberID,
                    KnowledgeType = "conversation",
                    Topic = topic,
                    Summary = summary,
                    Content = content,
                    Importance = importance,
                    AccessCount = 0,
                    LastAccessTime = DateTime.Now,
                    SourceType = "claw_conversation",
                    SourceID = param.ProcessesID,
                    CreateTime = DateTime.Now,
                    LastUpdateTime = DateTime.Now
                };

                // 6. 保存到数据库
                LongTermMemoryBusiness.Add(memory);

                LoggerHelper.LogInfo(logger, ClawLogModules.MEMORY,
                    $"✓ 已保存对话到长期记忆 - ID: {memory.MemoryID}, 重要性: {importance}, 主题: {topic}");

                param.Logs.Add($"✓ 已保存对话到长期记忆 (重要性: {importance})");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(logger, ClawLogModules.MEMORY,
                    $"保存对话到长期记忆失败: {ex.Message}");
                param.Logs.Add($"⚠ 保存对话到长期记忆失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存反思到长期记忆
        /// </summary>
        /// <param name="appID">应用ID</param>
        /// <param name="sessionID">会话ID</param>
        /// <param name="memberID">用户ID</param>
        /// <param name="clawID">ClawAI实例ID</param>
        /// <param name="task">原始任务</param>
        /// <param name="reflection">反思结果</param>
        /// <param name="planning">任务规划</param>
        /// <param name="logger">日志记录器</param>
        public static async Task SaveReflectionToMemoryAsync(
            string appID,
            string sessionID,
            string memberID,
            string clawID,
            string task,
            ReflectionResult reflection,
            TaskPlanning planning,
            ILogger logger = null)
        {
            try
            {
                // 只保存高质量的反思（质量>=80）
                if (reflection.OverallQuality < 80)
                {
                    LoggerHelper.LogDebug(logger, ClawLogModules.MEMORY,
                        $"反思质量不足（{reflection.OverallQuality}/100），跳过保存");
                    return;
                }

                // 创建反思记忆
                var memory = new LongTermMemoryInfo
                {
                    MemoryID = Guid.NewGuid().ToString(),
                    AppID = appID,
                    ClawID = clawID,
                    SessionID = sessionID,
                    MemberID = memberID,
                    KnowledgeType = "reflection",
                    Topic = $"任务反思: {MemoryHelper.ExtractTopic(task)}",
                    Summary = $"质量评分: {reflection.OverallQuality}/100 - {reflection.Reason}",
                    Content = FormatReflectionContent(task, reflection, planning),
                    Importance = Math.Min(100, reflection.OverallQuality + 10), // 高质量反思额外加分
                    AccessCount = 0,
                    LastAccessTime = DateTime.Now,
                    SourceType = "claw_reflection",
                    SourceID = planning?.PlanningID,
                    CreateTime = DateTime.Now,
                    LastUpdateTime = DateTime.Now,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        action = reflection.Action.ToString(),
                        overallQuality = reflection.OverallQuality,
                        completenessScore = reflection.CompletenessScore,
                        accuracyScore = reflection.AccuracyScore,
                        taskAnalysis = reflection.TaskAnalysis
                    })
                };

                // 保存到数据库
                LongTermMemoryBusiness.Add(memory);

                LoggerHelper.LogInfo(logger, ClawLogModules.MEMORY,
                    $"✓ 已保存反思到长期记忆 - ID: {memory.MemoryID}, 质量: {reflection.OverallQuality}/100");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(logger, ClawLogModules.MEMORY,
                    $"保存反思到长期记忆失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 构建知识图谱关系
        /// </summary>
        /// <param name="param">后处理参数</param>
        /// <param name="logger">日志记录器</param>
        public static async Task BuildKnowledgeGraphRelationsAsync(
            PostProcessingParams param,
            ILogger logger = null)
        {
            try
            {
                // 1. 获取最近保存的记忆
                var recentMemories = LongTermMemoryBusiness.SearchByKeywords(
                    param.AppID,
                    param.OriginalTask,
                    limit: 5);

                if (recentMemories == null || recentMemories.Count == 0)
                {
                    LoggerHelper.LogDebug(logger, ClawLogModules.MEMORY,
                        "知识图谱构建：未找到相关记忆，跳过图谱构建");
                    return;
                }

                // 2. 为每个记忆发现关联
                int totalNewRelations = 0;
                var processedMemoryIds = new HashSet<string>();

                foreach (var memory in recentMemories.Take(5))
                {
                    // 跳过已处理的记忆
                    if (processedMemoryIds.Contains(memory.MemoryID))
                    {
                        continue;
                    }
                    processedMemoryIds.Add(memory.MemoryID);

                    // 发现新的关联关系（增强版：支持memberId和clawId过滤）
                    var newRelations = KnowledgeRelationBusiness.DiscoverRelations(
                        memory.MemoryID,
                        param.AppID,
                        param.MemberID,      // 传递MemberID
                        param.ClawID,        // 传递ClawID
                        similarityThreshold: 0.75f,
                        maxRelations: 5);

                    if (newRelations.Count > 0)
                    {
                        totalNewRelations += newRelations.Count;
                        LoggerHelper.LogDebug(logger, ClawLogModules.MEMORY,
                            $"知识图谱构建：为记忆 [{memory.MemoryID}] 发现 {newRelations.Count} 个新关联");
                    }
                }

                if (totalNewRelations > 0)
                {
                    LoggerHelper.LogInfo(logger, ClawLogModules.MEMORY,
                        $"✓ 已构建知识图谱 - 发现 {totalNewRelations} 个新关联");
                    param.Logs.Add($"✓ 知识图谱已更新（新增 {totalNewRelations} 个关联）");
                }
                else
                {
                    LoggerHelper.LogDebug(logger, ClawLogModules.MEMORY,
                        "知识图谱构建：未发现新的关联关系");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(logger, ClawLogModules.MEMORY,
                    $"构建知识图谱失败: {ex.Message}");
                param.Logs.Add($"⚠ 构建知识图谱失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 格式化反思内容
        /// </summary>
        private static string FormatReflectionContent(
            string task,
            ReflectionResult reflection,
            TaskPlanning planning)
        {
            var content = new List<string>
            {
                $"任务描述: {task}",
                $"完成度评分: {reflection.CompletenessScore}/100",
                $"准确性评分: {reflection.AccuracyScore}/100",
                $"行动类型: {reflection.Action}",
                $"原因: {reflection.Reason}",
                $"最终答案: {reflection.FinalAnswer ?? "无"}"
            };

            if (planning != null)
            {
                content.Add($"任务分析: {planning.Steps?.Count ?? 0} 个步骤");
                content.Add($"策略: {planning.Metadata.Strategy ?? "未指定"}");
                content.Add($"置信度: {planning.Metadata.Confidence}/100");
            }

            return string.Join("\n", content);
        }

        /// <summary>
        /// 批量保存记忆
        /// </summary>
        /// <param name="memories">记忆列表</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>成功保存的数量</returns>
        public static int BatchSaveMemories(
            List<LongTermMemoryInfo> memories,
            ILogger logger = null)
        {
            if (memories == null || memories.Count == 0)
            {
                return 0;
            }

            int successCount = 0;

            foreach (var memory in memories)
            {
                try
                {
                    LongTermMemoryBusiness.Add(memory);
                    successCount++;
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError(logger, ClawLogModules.MEMORY,
                        $"批量保存记忆失败 [{memory.MemoryID}]: {ex.Message}");
                }
            }

            LoggerHelper.LogInfo(logger, ClawLogModules.MEMORY,
                $"批量保存记忆完成: {successCount}/{memories.Count} 成功");

            return successCount;
        }

        /// <summary>
        /// 清理低质量记忆
        /// </summary>
        /// <param name="appID">应用ID</param>
        /// <param name="days">天数</param>
        /// <param name="minImportance">最小重要性</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>清理的记忆数量</returns>
        public static int CleanupLowQualityMemories(
            string appID,
            int days = 90,
            int minImportance = 30,
            ILogger logger = null)
        {
            try
            {
                // TODO: 实现清理逻辑
                // 目前只是框架，实际需要从数据库查询并删除

                LoggerHelper.LogInfo(logger, ClawLogModules.MEMORY,
                    $"清理低质量记忆: 应用={appID}, 天数={days}, 最小重要性={minImportance}");

                return 0;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(logger, ClawLogModules.MEMORY,
                    $"清理低质量记忆失败: {ex.Message}");
                return 0;
            }
        }
    }
}
