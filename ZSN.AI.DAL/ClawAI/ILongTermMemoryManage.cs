using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.DAL
{
    /// <summary>
    /// 长期记忆数据访问接口
    /// </summary>
    public partial interface ILongTermMemoryManage
    {
        string SetConnectionName(string connName);

        #region 基础CRUD操作

        /// <summary>
        /// 增加一条数据
        /// </summary>
        string LongTermMemory_Add(LongTermMemoryInfo model);

        /// <summary>
        /// 更新一条数据
        /// </summary>
        bool LongTermMemory_Update(LongTermMemoryInfo model);

        /// <summary>
        /// 删除一条数据
        /// </summary>
        bool LongTermMemory_Delete(string MemoryID);

        /// <summary>
        /// 批量删除数据
        /// </summary>
        bool LongTermMemory_DeleteList(string MemoryIDlist);

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        LongTermMemoryInfo LongTermMemory_GetModel(string MemoryID);

        /// <summary>
        /// 获得数据列表
        /// </summary>
        DataSet LongTermMemory_GetList(string strWhere);

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        DataSet LongTermMemory_GetList(int top, string strWhere, string filedOrder);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        int LongTermMemory_GetRecordCount(string strWhere);

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        DataTable LongTermMemory_GetListByPage(int size, int index, string where, out int pagetotal, out int total);

        /// <summary>
        /// DataRow转Model
        /// </summary>
        LongTermMemoryInfo LongTermMemory_DataRowToModel(DataRow row);

        #endregion

        #region 扩展方法

        /// <summary>
        /// 根据AppID获取长期记忆列表
        /// </summary>
        List<LongTermMemoryInfo> LongTermMemory_GetByApp(string AppID, int limit);

        /// <summary>
        /// 根据AppID和主题获取长期记忆
        /// </summary>
        List<LongTermMemoryInfo> LongTermMemory_GetByTopic(string AppID, string Topic, int limit);

        /// <summary>
        /// 根据知识类型获取长期记忆
        /// </summary>
        List<LongTermMemoryInfo> LongTermMemory_GetByKnowledgeType(string AppID, string KnowledgeType, int limit);

        /// <summary>
        /// 增加访问次数
        /// </summary>
        bool LongTermMemory_IncrementAccessCount(string MemoryID);

        /// <summary>
        /// 更新向量嵌入
        /// </summary>
        bool LongTermMemory_UpdateEmbedding(string MemoryID, string Embedding);

        /// <summary>
        /// 批量插入长期记忆
        /// </summary>
        int LongTermMemory_AddBatch(List<LongTermMemoryInfo> memories);

        /// <summary>
        /// 根据重要性和访问频率获取热门知识
        /// </summary>
        List<LongTermMemoryInfo> LongTermMemory_GetHotKnowledge(string AppID, int limit);

        #endregion

        #region P3 优化 - PostgreSQL 向量搜索

        /// <summary>
        /// 语义相似度检索（使用pgvector）
        /// </summary>
        /// <param name="query">查询文本</param>
        /// <param name="appId">应用ID</param>
        /// <param name="memberId">用户ID（可选）</param>
        /// <param name="clawId">ClawAI节点ID（可选）</param>
        /// <param name="topK">返回结果数量</param>
        /// <param name="minSimilarity">最小相似度阈值(0-1)</param>
        /// <returns>相似记忆列表</returns>
        List<LongTermMemoryInfo> LongTermMemory_SearchBySimilarity(
            string query, string appId, string memberId, string clawId,
            int topK, float minSimilarity);

        /// <summary>
        /// 批量更新向量嵌入
        /// </summary>
        /// <param name="memoryIds">记忆ID列表</param>
        /// <param name="embeddings">向量嵌入列表（JSON格式）</param>
        /// <returns>更新的数量</returns>
        int LongTermMemory_UpdateEmbeddingBatch(string[] memoryIds, string[] embeddings);

        /// <summary>
        /// 根据向量相似度和重要性获取记忆（混合排序）
        /// </summary>
        List<LongTermMemoryInfo> LongTermMemory_GetByVectorAndImportance(
            string query, string appId, string clawId, int limit);

        #endregion

        #region 统计方法

        /// <summary>
        /// 按知识类型统计数量
        /// </summary>
        DataTable LongTermMemory_GetCountByKnowledgeType(string appId);

        /// <summary>
        /// 按主题统计数量（Top N）
        /// </summary>
        DataTable LongTermMemory_GetCountByTopic(string appId, int topN);

        /// <summary>
        /// 获取基础统计数据
        /// </summary>
        DataTable LongTermMemory_GetStatistics(string appId);

        /// <summary>
        /// 获取重要性分布统计
        /// </summary>
        DataTable LongTermMemory_GetImportanceDistribution(string appId);

        #endregion
    }
}
