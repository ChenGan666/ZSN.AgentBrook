using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.DAL;

namespace ZSN.AI.BLL
{
    /// <summary>
    /// 用户反馈业务逻辑类
    /// </summary>
    public partial class UserFeedbackBusiness
    {
        #region 基础信息
        private const string ConnectionName = "KnowledgeBaseDb";
        #endregion

        #region 基础CRUD操作

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(UserFeedbackInfo model)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_Add(model);
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(UserFeedbackInfo model)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_Update(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public static bool Delete(string FeedbackID)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_Delete(FeedbackID);
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public static bool DeleteList(string FeedbackIDlist)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_DeleteList(FeedbackIDlist);
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public static UserFeedbackInfo GetModel(string FeedbackID)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetModel(FeedbackID);
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<UserFeedbackInfo> GetList(string strWhere = "")
        {
            return UserFeedbackDataSet_ToList(DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetList(strWhere).Tables[0]);
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public static List<UserFeedbackInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return UserFeedbackDataSet_ToList(DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetList(top, strWhere, filedOrder).Tables[0]);
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetRecordCount(strWhere);
        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public static List<UserFeedbackInfo> GetListByPage(int size, int index, string where, out int pagetotal, out int total)
        {
            return UserFeedbackDataSet_ToList(DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetListByPage(size, index, where, out pagetotal, out total));
        }

        #endregion

        #region 扩展方法

        /// <summary>
        /// 根据记忆ID获取反馈列表
        /// </summary>
        public static List<UserFeedbackInfo> GetByMemoryId(string memoryId, int limit = 10)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetByMemoryId(memoryId, limit);
        }

        /// <summary>
        /// 根据用户ID和应用ID获取反馈列表
        /// </summary>
        public static List<UserFeedbackInfo> GetByMemberAndApp(string memberId, string appId, int limit = 10)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetByMemberAndApp(memberId, appId, limit);
        }

        /// <summary>
        /// 根据会话ID获取反馈列表
        /// </summary>
        public static List<UserFeedbackInfo> GetBySessionId(string sessionId)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetBySessionId(sessionId);
        }

        /// <summary>
        /// 根据反馈类型获取统计信息
        /// </summary>
        public static Dictionary<string, int> GetStatsByType(string appId, DateTime? startTime = null, DateTime? endTime = null)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetStatsByType(appId, startTime, endTime);
        }

        /// <summary>
        /// 获取最近的反馈列表
        /// </summary>
        public static List<UserFeedbackInfo> GetRecentFeedbacks(string appId, int days = 30, int limit = 100)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetRecentFeedbacks(appId, days, limit);
        }

        /// <summary>
        /// 批量插入用户反馈
        /// </summary>
        public static int AddBatch(List<UserFeedbackInfo> feedbacks)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_AddBatch(feedbacks);
        }

        /// <summary>
        /// 获取知识的反馈统计信息
        /// </summary>
        public static KnowledgeFeedbackStats GetKnowledgeStats(string memoryId, int recentDays = 30)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetKnowledgeStats(memoryId, recentDays);
        }

        /// <summary>
        /// 根据应用和时间范围获取反馈统计
        /// </summary>
        public static DataSet GetStatsByAppAndTime(string appId, DateTime startTime, DateTime endTime)
        {
            return DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_GetStatsByAppAndTime(appId, startTime, endTime);
        }

        private static List<UserFeedbackInfo> UserFeedbackDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<UserFeedbackInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetUserFeedback(ConnectionName).UserFeedback_DataRowToModel(r));
            }
            return list;
        }

        #endregion

        #region P3 优化 - 用户反馈学习

        /// <summary>
        /// 记录用户反馈（便捷方法）
        /// </summary>
        public static string RecordFeedback(
            string appId,
            string sessionId,
            string memberId,
            string userQuery,
            string aiResponse,
            List<string> usedMemories,
            FeedbackType feedbackType,
            int feedbackScore = 3,
            string comment = null)
        {
            var feedback = new UserFeedbackInfo
            {
                FeedbackID = Guid.NewGuid().ToString(),
                AppID = appId,
                SessionID = sessionId,
                MemberID = memberId,
                UserQuery = userQuery,
                AIResponse = aiResponse,
                FeedbackType = feedbackType.ToString().ToLower(),
                FeedbackScore = feedbackScore,
                FeedbackComment = comment ?? "",
                UsedMemories = Newtonsoft.Json.JsonConvert.SerializeObject(usedMemories ?? new List<string>()),
                Metadata = "{}",
                CreateTime = DateTime.Now
            };

            var feedbackId = Add(feedback);

            // 触发知识重要性更新
            if (usedMemories != null && usedMemories.Count > 0)
            {
                UpdateKnowledgeImportanceByFeedback(usedMemories, feedbackType, feedbackScore);
            }

            return feedbackId;
        }

        /// <summary>
        /// 根据反馈动态调整知识重要性
        /// </summary>
        private static void UpdateKnowledgeImportanceByFeedback(
            List<string> memoryIds,
            FeedbackType feedbackType,
            int feedbackScore)
        {
            foreach (var memoryId in memoryIds)
            {
                var memory = LongTermMemoryBusiness.GetModel(memoryId);
                if (memory == null) continue;

                // 计算重要性调整值
                int adjustment = CalculateImportanceAdjustment(feedbackType, feedbackScore);

                // 更新重要性（限制在0-100范围内）
                memory.Importance = Math.Min(100, Math.Max(0, memory.Importance + adjustment));

                // 更新访问次数和时间
                memory.AccessCount++;
                memory.LastAccessTime = DateTime.Now;

                LongTermMemoryBusiness.Update(memory);
            }
        }

        /// <summary>
        /// 计算重要性调整值
        /// </summary>
        private static int CalculateImportanceAdjustment(FeedbackType feedbackType, int feedbackScore)
        {
            return feedbackType switch
            {
                FeedbackType.Positive => feedbackScore * 2,      // +2 to +10
                FeedbackType.Negative => -feedbackScore * 2,     // -2 to -10
                FeedbackType.Neutral => 0,
                _ => 0
            };
        }

        /// <summary>
        /// 分析知识的反馈统计（增强版）
        /// </summary>
        public static KnowledgeFeedbackStats AnalyzeKnowledgeFeedback(
            string memoryId,
            int recentDays = 30)
        {
            return GetKnowledgeStats(memoryId, recentDays);
        }

        /// <summary>
        /// 自动清理低质量知识（定期任务）
        /// </summary>
        public static int CleanLowQualityKnowledge(
            string appId,
            int days = 30,
            int minFeedbacks = 5,
            float maxNegativeRate = 0.7f)
        {
            // 查询指定天数内有反馈的知识
            var recentFeedbacks = GetRecentFeedbacks(appId, days, int.MaxValue);

            var memoryStats = recentFeedbacks
                .Where(f => !string.IsNullOrEmpty(f.MemoryID))
                .GroupBy(f => f.MemoryID)
                .Select(g => new
                {
                    MemoryID = g.Key,
                    TotalFeedbacks = g.Count(),
                    NegativeCount = g.Count(f => f.FeedbackType == "negative"),
                    PositiveCount = g.Count(f => f.FeedbackType == "positive"),
                    AverageScore = g.Average(f => f.FeedbackScore)
                })
                .Where(s => s.TotalFeedbacks >= minFeedbacks)
                .Where(s => (float)s.NegativeCount / s.TotalFeedbacks > maxNegativeRate)
                .ToList();

            int cleanedCount = 0;
            foreach (var stat in memoryStats)
            {
                var memory = LongTermMemoryBusiness.GetModel(stat.MemoryID);
                if (memory == null) continue;

                // 降低重要性
                memory.Importance = Math.Max(0, memory.Importance - 30);

                // 标记为低质量
                var metadata = new System.Collections.Generic.Dictionary<string, object>();
                if (!string.IsNullOrEmpty(memory.Metadata))
                {
                    try
                    {
                        metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(memory.Metadata)
                            ?? new System.Collections.Generic.Dictionary<string, object>();
                    }
                    catch { }
                }

                metadata["quality"] = "low";
                metadata["negative_rate"] = (float)stat.NegativeCount / stat.TotalFeedbacks;
                metadata["last_cleaned"] = DateTime.Now.ToString("o");

                memory.Metadata = Newtonsoft.Json.JsonConvert.SerializeObject(metadata);

                LongTermMemoryBusiness.Update(memory);
                cleanedCount++;
            }

            return cleanedCount;
        }

        /// <summary>
        /// 获取应用的整体反馈统计
        /// </summary>
        public static Dictionary<string, object> GetAppFeedbackStats(
            string appId,
            int days = 30)
        {
            var feedbacks = GetRecentFeedbacks(appId, days, int.MaxValue);

            if (feedbacks.Count == 0)
            {
                return new Dictionary<string, object>
                {
                    { "total_feedbacks", 0 },
                    { "positive_rate", 0.0 },
                    { "negative_rate", 0.0 },
                    { "neutral_rate", 0.0 },
                    { "average_score", 0.0 }
                };
            }

            var stats = new Dictionary<string, object>
            {
                { "total_feedbacks", feedbacks.Count },
                { "positive_count", feedbacks.Count(f => f.FeedbackType == "positive") },
                { "negative_count", feedbacks.Count(f => f.FeedbackType == "negative") },
                { "neutral_count", feedbacks.Count(f => f.FeedbackType == "neutral") },
                { "positive_rate", (float)feedbacks.Count(f => f.FeedbackType == "positive") / feedbacks.Count },
                { "negative_rate", (float)feedbacks.Count(f => f.FeedbackType == "negative") / feedbacks.Count },
                { "neutral_rate", (float)feedbacks.Count(f => f.FeedbackType == "neutral") / feedbacks.Count },
                { "average_score", feedbacks.Average(f => f.FeedbackScore) }
            };

            return stats;
        }

        #endregion
    }
}
