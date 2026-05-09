using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.DAL;

namespace ZSN.AI.BLL
{
    /// <summary>
    /// 知识关系业务逻辑类
    /// </summary>
    public partial class KnowledgeRelationBusiness
    {
        #region 基础信息
        private const string ConnectionName = "KnowledgeBaseDb";
        #endregion

        #region 基础CRUD操作

        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(KnowledgeRelationInfo model)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_Add(model);
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(KnowledgeRelationInfo model)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_Update(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public static bool Delete(string RelationID)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_Delete(RelationID);
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public static bool DeleteList(string RelationIDlist)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_DeleteList(RelationIDlist);
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public static KnowledgeRelationInfo GetModel(string RelationID)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetModel(RelationID);
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<KnowledgeRelationInfo> GetList(string strWhere = "")
        {
            return KnowledgeRelationDataSet_ToList(DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetList(strWhere).Tables[0]);
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public static List<KnowledgeRelationInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return KnowledgeRelationDataSet_ToList(DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetList(top, strWhere, filedOrder).Tables[0]);
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetRecordCount(strWhere);
        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public static List<KnowledgeRelationInfo> GetListByPage(int size, int index, string where, out int pagetotal, out int total)
        {
            return KnowledgeRelationDataSet_ToList(DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetListByPage(size, index, where, out pagetotal, out total));
        }

        #endregion

        #region 扩展方法

        /// <summary>
        /// 根据源知识ID获取关系列表
        /// </summary>
        public static List<KnowledgeRelationInfo> GetBySourceId(string sourceMemoryId)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetBySourceId(sourceMemoryId);
        }

        /// <summary>
        /// 根据目标知识ID获取关系列表
        /// </summary>
        public static List<KnowledgeRelationInfo> GetByTargetId(string targetMemoryId)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetByTargetId(targetMemoryId);
        }

        /// <summary>
        /// 根据应用ID和关系类型获取关系列表
        /// </summary>
        public static List<KnowledgeRelationInfo> GetByAppAndType(string appId, string relationType, int limit = 10)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetByAppAndType(appId, relationType, limit);
        }

        /// <summary>
        /// 批量插入知识关系
        /// </summary>
        public static int AddBatch(List<KnowledgeRelationInfo> relations)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_AddBatch(relations);
        }

        /// <summary>
        /// 删除指定知识的所有关系
        /// </summary>
        public static bool DeleteByMemoryId(string memoryId)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_DeleteByMemoryId(memoryId);
        }

        /// <summary>
        /// 获取知识之间的关系强度
        /// </summary>
        public static float GetStrength(string sourceId, string targetId, string relationType)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetStrength(sourceId, targetId, relationType);
        }

        /// <summary>
        /// 更新关系强度
        /// </summary>
        public static bool UpdateStrength(string relationId, float newStrength)
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_UpdateStrength(relationId, newStrength);
        }

        /// <summary>
        /// 创建知识关系（便捷方法）
        /// </summary>
        public static string CreateRelation(
            string appId,
            string sourceMemoryId,
            string targetMemoryId,
            string relationType,
            float strength = 0.5f,
            string metadata = null)
        {
            var relation = new KnowledgeRelationInfo
            {
                RelationID = Guid.NewGuid().ToString(),
                AppID = appId,
                SourceMemoryID = sourceMemoryId,
                TargetMemoryID = targetMemoryId,
                RelationType = relationType,
                Strength = strength,
                Metadata = metadata ?? "{}",
                CreateTime = DateTime.Now,
                LastUpdateTime = DateTime.Now
            };

            return Add(relation);
        }

        /// <summary>
        /// 查找相关的知识（广度优先搜索，最多3层）
        /// </summary>
        public static List<LongTermMemoryInfo> GetRelatedKnowledge(
            string memoryId,
            int maxDepth = 2,
            int maxResults = 10)
        {
            var visited = new HashSet<string>();
            var results = new List<LongTermMemoryInfo>();
            var queue = new Queue<(string id, int depth)>();

            queue.Enqueue((memoryId, 0));
            visited.Add(memoryId);

            while (queue.Count > 0 && results.Count < maxResults)
            {
                var (currentId, depth) = queue.Dequeue();

                if (depth >= maxDepth) continue;

                // 查询直接关联的知识
                var relations = GetBySourceId(currentId);

                foreach (var relation in relations)
                {
                    if (visited.Contains(relation.TargetMemoryID)) continue;

                    visited.Add(relation.TargetMemoryID);

                    // 加载知识详情
                    var memory = LongTermMemoryBusiness.GetModel(relation.TargetMemoryID);
                    if (memory != null)
                    {
                        results.Add(memory);
                        queue.Enqueue((relation.TargetMemoryID, depth + 1));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 查找两个知识之间的最短路径
        /// </summary>
        public static List<string> FindShortestPath(
            string sourceId,
            string targetId,
            int maxDepth = 5)
        {
            var visited = new HashSet<string>();
            var queue = new Queue<(string id, List<string> path)>();

            queue.Enqueue((sourceId, new List<string> { sourceId }));
            visited.Add(sourceId);

            while (queue.Count > 0)
            {
                var (currentId, path) = queue.Dequeue();

                if (path.Count > maxDepth) continue;

                if (currentId == targetId)
                {
                    return path; // 找到路径
                }

                // 查询邻接节点
                var relations = GetBySourceId(currentId);

                foreach (var relation in relations)
                {
                    if (visited.Contains(relation.TargetMemoryID)) continue;

                    visited.Add(relation.TargetMemoryID);

                    var newPath = new List<string>(path) { relation.TargetMemoryID };
                    queue.Enqueue((relation.TargetMemoryID, newPath));
                }
            }

            return null; // 未找到路径
        }

        private static List<KnowledgeRelationInfo> KnowledgeRelationDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<KnowledgeRelationInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_DataRowToModel(r));
            }
            return list;
        }

        #endregion

        #region P3 优化 - 知识图谱构建

        /// <summary>
        /// 自动发现知识之间的关系（增强版：支持向量相似度）
        /// </summary>
        public static List<KnowledgeRelationInfo> DiscoverRelations(
            string memoryId,
            string appId,
            string memberId = null,
            string clawId = null,
            float similarityThreshold = 0.7f,
            int maxRelations = 10)
        {
            var relations = new List<KnowledgeRelationInfo>();

            // 1. 获取当前知识
            var currentMemory = LongTermMemoryBusiness.GetModel(memoryId);
            if (currentMemory == null) return relations;

            // 2. 使用语义相似度搜索查找相似知识
            var similarMemories = LongTermMemoryBusiness.SearchBySimilarity(
                currentMemory.Summary,
                appId,
                memberId,
                clawId,
                topK: maxRelations,
                minSimilarity: similarityThreshold);

            foreach (var similar in similarMemories)
            {
                if (similar.MemoryID == memoryId) continue;

                // 3. 检查是否已存在关系
                var existingStrength = GetStrength(memoryId, similar.MemoryID, "related");
                if (existingStrength > 0) continue;

                // 4. 计算相似度（优先使用向量相似度，降级使用文本相似度）
                float similarity = CalculateEnhancedSimilarity(currentMemory, similar);

                if (similarity >= similarityThreshold)
                {
                    // 5. 确定关系类型
                    string relationType = DetermineRelationType(currentMemory, similar, similarity);

                    var relationId = CreateRelation(
                        appId,
                        memoryId,
                        similar.MemoryID,
                        relationType,
                        similarity,
                        $"{{\"auto_discovered\":true,\"similarity\":{similarity:F3},\"discovery_method\":\"semantic_vector\"}}");

                    var relation = GetModel(relationId);
                    if (relation != null)
                    {
                        relations.Add(relation);
                    }
                }
            }

            // 6. 尝试检测前置关系（基于时间顺序）
            var prerequisiteRelations = DiscoverPrerequisiteRelations(currentMemory, appId, memberId, clawId);
            relations.AddRange(prerequisiteRelations);

            return relations;
        }

        /// <summary>
        /// 增强的相似度计算（结合向量和文本特征）
        /// </summary>
        private static float CalculateEnhancedSimilarity(LongTermMemoryInfo memory1, LongTermMemoryInfo memory2)
        {
            float similarity = 0f;

            // 1. 检查是否有向量嵌入
            if (!string.IsNullOrEmpty(memory1.Embedding) && !string.IsNullOrEmpty(memory2.Embedding))
            {
                // TODO: 实现向量余弦相似度计算
                // 这里需要解析向量并计算余弦相似度
                // 目前使用文本特征作为替代
            }

            // 2. 主题相似度
            float topicSimilarity = 0f;
            if (!string.IsNullOrEmpty(memory1.Topic) && !string.IsNullOrEmpty(memory2.Topic))
            {
                if (memory1.Topic.Equals(memory2.Topic, StringComparison.OrdinalIgnoreCase))
                {
                    topicSimilarity = 1.0f;
                }
                else if (memory1.Topic.IndexOf(memory2.Topic, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         memory2.Topic.IndexOf(memory1.Topic, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    topicSimilarity = 0.7f;
                }
            }

            // 3. 摘要相似度（简化实现）
            float summarySimilarity = CalculateJaccardSimilarity(
                memory1.Summary ?? "",
                memory2.Summary ?? "");

            // 4. 类型相似度
            float typeSimilarity = memory1.KnowledgeType == memory2.KnowledgeType ? 0.2f : 0f;

            // 5. 综合相似度
            similarity = (topicSimilarity * 0.4f) + (summarySimilarity * 0.4f) + typeSimilarity;

            return Math.Min(1.0f, similarity);
        }

        /// <summary>
        /// 计算Jaccard相似度
        /// </summary>
        private static float CalculateJaccardSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            {
                return 0f;
            }

            // 分词（简化实现，按空格和常见标点分割）
            var words1 = new HashSet<string>(text1.Split(new[] { ' ', '，', ',', '、', '的', '了', '在', '是' },
                StringSplitOptions.RemoveEmptyEntries));
            var words2 = new HashSet<string>(text2.Split(new[] { ' ', '，', ',', '、', '的', '了', '在', '是' },
                StringSplitOptions.RemoveEmptyEntries));

            if (words1.Count == 0 || words2.Count == 0)
            {
                return 0f;
            }

            // 计算Jaccard系数
            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union > 0 ? (float)intersection / union : 0f;
        }

        /// <summary>
        /// 确定关系类型
        /// </summary>
        private static string DetermineRelationType(LongTermMemoryInfo source, LongTermMemoryInfo target, float similarity)
        {
            // 1. 检查分类关系
            if (source.Topic != null && target.Summary != null &&
                target.Summary.IndexOf(source.Topic, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "category";
            }

            // 2. 检查示例关系
            if (source.KnowledgeType == "concept" && target.KnowledgeType == "fact")
            {
                return "example";
            }

            // 3. 检查时间顺序（前置关系）
            if (source.CreateTime < target.CreateTime)
            {
                // 如果源知识创建时间早，可能是前置知识
                return "prerequisite";
            }

            // 4. 默认为相关关系
            return similarity > 0.85f ? "derived" : "related";
        }

        /// <summary>
        /// 发现前置关系
        /// </summary>
        private static List<KnowledgeRelationInfo> DiscoverPrerequisiteRelations(
            LongTermMemoryInfo currentMemory,
            string appId,
            string memberId,
            string clawId)
        {
            var relations = new List<KnowledgeRelationInfo>();

            // 查找更早创建的相关知识
            var earlierMemories = LongTermMemoryBusiness.GetList(
                20,
                $"app_id='{appId}' AND claw_id='{clawId}' AND create_time < '{currentMemory.CreateTime:yyyy-MM-dd HH:mm:ss}'",
                "create_time DESC");

            foreach (var earlier in earlierMemories)
            {
                if (earlier.MemoryID == currentMemory.MemoryID) continue;

                // 检查是否已存在关系
                var existingStrength = GetStrength(earlier.MemoryID, currentMemory.MemoryID, "prerequisite");
                if (existingStrength > 0) continue;

                // 计算相似度
                float similarity = CalculateEnhancedSimilarity(earlier, currentMemory);

                if (similarity > 0.6f)
                {
                    var relationId = CreateRelation(
                        appId,
                        earlier.MemoryID,
                        currentMemory.MemoryID,
                        "prerequisite",
                        similarity,
                        $"{{\"auto_discovered\":true,\"similarity\":{similarity:F3},\"discovery_method\":\"temporal\"}}");

                    var relation = GetModel(relationId);
                    if (relation != null)
                    {
                        relations.Add(relation);
                    }
                }

                // 最多创建3个前置关系
                if (relations.Count >= 3) break;
            }

            return relations;
        }

        /// <summary>
        /// 计算两个知识之间的相似度（已弃用，请使用CalculateEnhancedSimilarity）
        /// </summary>
        [Obsolete("Please use CalculateEnhancedSimilarity instead")]
        private static float CalculateSimilarity(LongTermMemoryInfo memory1, LongTermMemoryInfo memory2)
        {
            return CalculateEnhancedSimilarity(memory1, memory2);
        }

        #endregion

        #region 统计方法

        /// <summary>
        /// 按关系类型统计数量
        /// </summary>
        public static DataTable GetCountByType(string appId = "")
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetCountByType(appId);
        }

        /// <summary>
        /// 获取基础统计数据
        /// </summary>
        public static DataTable GetStatistics(string appId = "")
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetStatistics(appId);
        }

        /// <summary>
        /// 按强度区间统计数量
        /// </summary>
        public static DataTable GetStrengthDistribution(string appId = "")
        {
            return DatabaseProvider.GetKnowledgeRelation(ConnectionName).KnowledgeRelation_GetStrengthDistribution(appId);
        }

        #endregion
    }
}
