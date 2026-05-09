using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.DAL;

namespace ZSN.AI.BLL
{
    public partial class LongTermMemoryBusiness
    {
        #region 基础信息
        private const string ConnectionName = "KnowledgeBaseDb";
        #endregion

        #region tb_claw_long_term_memory

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(LongTermMemoryInfo model)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_Add(model);
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(LongTermMemoryInfo model)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_Update(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public static bool Delete(string MemoryID)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_Delete(MemoryID);
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public static bool DeleteList(string MemoryIDlist)
        {
            MemoryIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(MemoryIDlist, ',', '\'');
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_DeleteList(MemoryIDlist);
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public static LongTermMemoryInfo GetModel(string MemoryID)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetModel(MemoryID);
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<LongTermMemoryInfo> GetList(string strWhere = "")
        {
            return LongTermMemoryDataSet_ToList(DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetList(strWhere).Tables[0]);
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public static List<LongTermMemoryInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return LongTermMemoryDataSet_ToList(DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetList(top, strWhere, filedOrder).Tables[0]);
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetRecordCount(strWhere);
        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public static List<LongTermMemoryInfo> GetListByPage(int size, int index, string where, out int pagetotal, out int total)
        {
            return LongTermMemoryDataSet_ToList(DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetListByPage(size, index, where, out pagetotal, out total));
        }

        /// <summary>
        /// 根据AppID获取长期记忆列表
        /// </summary>
        public static List<LongTermMemoryInfo> GetByApp(string AppID, int limit = 10)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetByApp(AppID, limit);
        }

        /// <summary>
        /// 根据AppID和主题获取长期记忆
        /// </summary>
        public static List<LongTermMemoryInfo> GetByTopic(string AppID, string Topic, int limit = 10)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetByTopic(AppID, Topic, limit);
        }

        /// <summary>
        /// 根据知识类型获取长期记忆
        /// </summary>
        public static List<LongTermMemoryInfo> GetByKnowledgeType(string AppID, string KnowledgeType, int limit = 10)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetByKnowledgeType(AppID, KnowledgeType, limit);
        }

        /// <summary>
        /// 增加访问次数
        /// </summary>
        public static bool IncrementAccessCount(string MemoryID)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_IncrementAccessCount(MemoryID);
        }

        /// <summary>
        /// 更新向量嵌入
        /// </summary>
        public static bool UpdateEmbedding(string MemoryID, string Embedding)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_UpdateEmbedding(MemoryID, Embedding);
        }

        /// <summary>
        /// 批量插入长期记忆
        /// </summary>
        public static int AddBatch(List<LongTermMemoryInfo> memories)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_AddBatch(memories);
        }

        /// <summary>
        /// 根据重要性和访问频率获取热门知识
        /// </summary>
        public static List<LongTermMemoryInfo> GetHotKnowledge(string AppID, int limit = 10)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetHotKnowledge(AppID, limit);
        }

        /// <summary>
        /// 简单文本相似度匹配(用于无向量嵌入时的快速检索)
        /// </summary>
        public static List<LongTermMemoryInfo> SearchByKeywords(string AppID, string query, int limit = 10)
        {
            // 简单的关键词匹配
            string where = $"app_id='{AppID}' AND (summary LIKE '%{query}%' OR content LIKE '%{query}%' OR topic LIKE '%{query}%')";
            return GetList(limit, where, "importance DESC, access_count DESC");
        }

        /// <summary>
        /// 按ClawID和关键词搜索(用于区分同一APP下的多个ClawAI节点)
        /// </summary>
        public static List<LongTermMemoryInfo> SearchByClawAndKeywords(string AppID, string ClawID, string query, int limit = 10)
        {
            // 按ClawID和关键词匹配
            string where = $"app_id='{AppID}' AND claw_id='{ClawID}' AND (summary LIKE '%{query}%' OR content LIKE '%{query}%' OR topic LIKE '%{query}%')";
            return GetList(limit, where, "importance DESC, access_count DESC");
        }

        /// <summary>
        /// 按ClawID获取长期记忆
        /// </summary>
        public static List<LongTermMemoryInfo> GetByClawID(string AppID, string ClawID, int limit = 10)
        {
            string where = $"app_id='{AppID}' AND claw_id='{ClawID}'";
            return GetList(limit, where, "importance DESC, access_count DESC, create_time DESC");
        }

        /// <summary>
        /// 根据主题和用户获取长期记忆
        /// </summary>
        public static List<LongTermMemoryInfo> GetByTopicAndMember(string AppID, string MemberID, string Topic, int limit = 10)
        {
            string where = $"app_id='{AppID}' AND member_id='{MemberID}' AND topic LIKE '%{Topic}%'";
            return GetList(limit, where, "importance DESC, access_count DESC, create_time DESC");
        }

        /// <summary>
        /// 根据用户和应用获取长期记忆
        /// </summary>
        public static List<LongTermMemoryInfo> GetByMemberAndApp(string MemberID, string AppID, int limit = 10)
        {
            string where = $"app_id='{AppID}' AND member_id='{MemberID}'";
            return GetList(limit, where, "importance DESC, access_count DESC, create_time DESC");
        }

        private static List<LongTermMemoryInfo> LongTermMemoryDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<LongTermMemoryInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_DataRowToModel(r));
            }
            return list;
        }

        #endregion

        #region P3 优化 - 语义相似度匹配

        /// <summary>
        /// 语义相似度检索（使用pgvector）
        /// </summary>
        public static List<LongTermMemoryInfo> SearchBySimilarity(
            string query,
            string appId,
            string memberId = null,
            string clawId = null,
            int topK = 5,
            float minSimilarity = 0.7f)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_SearchBySimilarity(
                query, appId, memberId, clawId, topK, minSimilarity);
        }

        /// <summary>
        /// 批量更新向量嵌入
        /// </summary>
        public static int UpdateEmbeddingBatch(Dictionary<string, string> memoryEmbeddings)
        {
            if (memoryEmbeddings == null || memoryEmbeddings.Count == 0)
            {
                return 0;
            }

            var memoryIds = memoryEmbeddings.Keys.ToArray();
            var embeddings = memoryEmbeddings.Values.ToArray();

            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_UpdateEmbeddingBatch(
                memoryIds, embeddings);
        }

        /// <summary>
        /// 根据向量相似度和重要性获取记忆（混合排序）
        /// </summary>
        public static List<LongTermMemoryInfo> GetByVectorAndImportance(
            string query,
            string appId,
            string clawId,
            int limit = 10)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetByVectorAndImportance(
                query, appId, clawId, limit);
        }

        #endregion

        #region 统计方法

        /// <summary>
        /// 按知识类型统计数量
        /// </summary>
        public static DataTable GetCountByKnowledgeType(string appId = "")
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetCountByKnowledgeType(appId);
        }

        /// <summary>
        /// 按主题统计数量（Top N）
        /// </summary>
        public static DataTable GetCountByTopic(string appId = "", int topN = 10)
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetCountByTopic(appId, topN);
        }

        /// <summary>
        /// 获取基础统计数据
        /// </summary>
        public static DataTable GetStatistics(string appId = "")
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetStatistics(appId);
        }

        /// <summary>
        /// 获取重要性分布统计
        /// </summary>
        public static DataTable GetImportanceDistribution(string appId = "")
        {
            return DatabaseProvider.GetLongTermMemory(ConnectionName).LongTermMemory_GetImportanceDistribution(appId);
        }

        #endregion
    }
}
