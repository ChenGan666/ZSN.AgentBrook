using Microsoft.AspNetCore.Mvc;
using ZSN.AI.BLL;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Entity;
using System;
using System.Collections.Generic;
using ZSN.AgentBrook.API.Attributes;
using Newtonsoft.Json;

namespace ZSN.AgentBrook.API.Controllers
{
    /// <summary>
    /// ClawAI用户反馈API控制器 (P3优化 - 优化点4)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ApiRecoder]
    public class FeedbackController : ApiBaseController
    {
        /// <summary>
        /// 记录用户反馈
        /// </summary>
        [HttpPost("record")]
        public JsonMsg<string> RecordFeedback()
        {
            try
            {
                // 从JsonObj获取参数
                var appId = JsonObj["appId"]?.ToString();
                var sessionId = JsonObj["sessionId"]?.ToString();
                var memberId = JsonObj["memberId"]?.ToString();
                var memoryId = JsonObj["memoryId"]?.ToString();
                var userQuery = JsonObj["userQuery"]?.ToString();
                var aiResponse = JsonObj["aiResponse"]?.ToString();
                var feedbackType = JsonObj["feedbackType"]?.ToString();
                var feedbackScore = Convert.ToInt32(JsonObj["feedbackScore"]);
                var comment = JsonObj["comment"]?.ToString();

                // 获取使用的记忆ID列表
                var usedMemoriesList = new List<string>();
                if (JsonObj["usedMemories"] != null)
                {
                    var usedMemoriesJsonStr = JsonObj["usedMemories"].ToString();
                    if (!string.IsNullOrEmpty(usedMemoriesJsonStr))
                    {
                        usedMemoriesList = JsonConvert.DeserializeObject<List<string>>(usedMemoriesJsonStr);
                    }
                }

                // 参数验证
                if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(memberId))
                {
                    return JsonMsg<string>.Error(null, ErrorCode.ParameterError);
                }

                if (feedbackScore < 1 || feedbackScore > 5)
                {
                    return JsonMsg<string>.Error(null, ErrorCode.ParameterError);
                }

                // 转换反馈类型
                FeedbackType type = feedbackType?.ToLower() switch
                {
                    "positive" => FeedbackType.Positive,
                    "negative" => FeedbackType.Negative,
                    "neutral" => FeedbackType.Neutral,
                    _ => feedbackScore >= 4 ? FeedbackType.Positive : (feedbackScore <= 2 ? FeedbackType.Negative : FeedbackType.Neutral)
                };

                // 转换使用的记忆ID列表为JSON字符串
                // string usedMemoriesJson = usedMemoriesList.Count > 0
                //     ? JsonConvert.SerializeObject(usedMemoriesList)
                //     : "[]";

                // 记录反馈（自动调整知识重要性）
                UserFeedbackBusiness.RecordFeedback(
                    appId,
                    sessionId,
                    memberId,
                    userQuery,
                    aiResponse,
                    usedMemoriesList,
                    type,
                    feedbackScore,
                    comment
                );

                // 获取反馈分析结果（如果提供了MemoryID）
                object stats = null;
                if (!string.IsNullOrEmpty(memoryId))
                {
                    var feedbackStats = UserFeedbackBusiness.AnalyzeKnowledgeFeedback(memoryId, recentDays: 30);

                    stats = new
                    {
                        totalFeedbacks = feedbackStats.TotalFeedbacks,
                        positiveCount = feedbackStats.PositiveCount,
                        negativeCount = feedbackStats.NegativeCount,
                        neutralCount = feedbackStats.NeutralCount,
                        positiveRate = feedbackStats.PositiveRate,
                        averageScore = feedbackStats.AverageScore
                    };
                }

                return JsonMsg<string>.OK(JsonConvert.SerializeObject(new
                {
                    feedbackType = type.ToString(),
                    feedbackScore = feedbackScore,
                    stats = stats
                }), "反馈已记录");
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error(null, ErrorCode.ServerError);
            }
        }

        /// <summary>
        /// 获取知识反馈统计
        /// </summary>
        [HttpGet("stats/{memoryId}")]
        public JsonMsg<string> GetFeedbackStats(string memoryId, int recentDays = 30)
        {
            try
            {
                if (string.IsNullOrEmpty(memoryId))
                {
                    return JsonMsg<string>.Error(null, ErrorCode.ParameterError);
                }

                // 分析知识反馈
                var stats = UserFeedbackBusiness.AnalyzeKnowledgeFeedback(memoryId, recentDays);

                return JsonMsg<string>.OK(JsonConvert.SerializeObject(new
                {
                    memoryId = memoryId,
                    recentDays = recentDays,
                    totalFeedbacks = stats.TotalFeedbacks,
                    positiveCount = stats.PositiveCount,
                    negativeCount = stats.NegativeCount,
                    neutralCount = stats.NeutralCount,
                    positiveRate = stats.PositiveRate,
                    averageScore = stats.AverageScore
                }));
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error(null, ErrorCode.ServerError);
            }
        }

        /// <summary>
        /// 获取应用整体反馈统计
        /// </summary>
        [HttpGet("app-stats/{appId}")]
        public JsonMsg<string> GetAppFeedbackStats(string appId, int recentDays = 30)
        {
            try
            {
                if (string.IsNullOrEmpty(appId))
                {
                    return JsonMsg<string>.Error(null, ErrorCode.ParameterError);
                }

                // 获取应用整体反馈统计
                var stats = UserFeedbackBusiness.GetAppFeedbackStats(appId, recentDays);

                return JsonMsg<string>.OK(JsonConvert.SerializeObject(new
                {
                    appId = appId,
                    recentDays = recentDays,
                    totalFeedbacks = stats["total_feedbacks"],
                    positiveRate = stats["positive_rate"],
                    averageScore = stats["average_score"]
                }));
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error(null, ErrorCode.ServerError);
            }
        }

        /// <summary>
        /// 清理低质量知识
        /// </summary>
        [HttpPost("clean-low-quality")]
        public JsonMsg<string> CleanLowQualityKnowledge()
        {
            try
            {
                // 从JsonObj获取参数
                var appId = JsonObj["appId"]?.ToString();
                var days = Convert.ToInt32(JsonObj["days"] ?? 30);
                var minFeedbacks = Convert.ToInt32(JsonObj["minFeedbacks"] ?? 5);
                var maxNegativeRate = Convert.ToDouble(JsonObj["maxNegativeRate"] ?? 0.7);

                // 参数验证
                if (string.IsNullOrEmpty(appId))
                {
                    return JsonMsg<string>.Error(null, ErrorCode.ParameterError);
                }

                if (days < 1 || days > 365)
                {
                    return JsonMsg<string>.Error(null, ErrorCode.ParameterError);
                }

                if (maxNegativeRate < 0 || maxNegativeRate > 1)
                {
                    return JsonMsg<string>.Error(null, ErrorCode.ParameterError);
                }

                // 执行清理
                int cleanedCount = UserFeedbackBusiness.CleanLowQualityKnowledge(
                    appId,
                    days,
                    minFeedbacks,
                    (float)maxNegativeRate
                );

                return JsonMsg<string>.OK(JsonConvert.SerializeObject(new
                {
                    cleanedCount = cleanedCount,
                    days = days,
                    minFeedbacks = minFeedbacks,
                    maxNegativeRate = maxNegativeRate
                }), $"已清理 {cleanedCount} 条低质量知识");
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error(null, ErrorCode.ServerError);
            }
        }

        /// <summary>
        /// 批量获取知识的反馈统计
        /// </summary>
        [HttpPost("batch-stats")]
        public JsonMsg<string> GetBatchFeedbackStats()
        {
            try
            {
                // 从JsonObj获取参数
                var memoryIdsJson = JsonObj["memoryIds"]?.ToString();
                var recentDays = Convert.ToInt32(JsonObj["recentDays"] ?? 30);

                if (string.IsNullOrEmpty(memoryIdsJson))
                {
                    return JsonMsg<string>.Error(null, ErrorCode.ParameterError);
                }

                var memoryIds = JsonConvert.DeserializeObject<List<string>>(memoryIdsJson);

                if (memoryIds == null || memoryIds.Count == 0)
                {
                    return JsonMsg<string>.Error(null, ErrorCode.ParameterError);
                }

                if (memoryIds.Count > 100)
                {
                    return JsonMsg<string>.Error(null, ErrorCode.ParameterError);
                }

                var results = new List<object>();

                foreach (var memoryId in memoryIds)
                {
                    try
                    {
                        var stats = UserFeedbackBusiness.AnalyzeKnowledgeFeedback(memoryId, recentDays);

                        results.Add(new
                        {
                            memoryId = memoryId,
                            totalFeedbacks = stats.TotalFeedbacks,
                            positiveRate = stats.PositiveRate,
                            averageScore = stats.AverageScore,
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            memoryId = memoryId,
                            success = false,
                            error = ex.Message
                        });
                    }
                }

                return JsonMsg<string>.OK(JsonConvert.SerializeObject(results));
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error(null, ErrorCode.ServerError);
            }
        }
    }
}
