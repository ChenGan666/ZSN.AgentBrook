using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.DAL
{
    /// <summary>
    /// 知识关系数据访问接口
    /// </summary>
    public partial interface IKnowledgeRelationManage
    {
        string SetConnectionName(string connName);

        #region 基础CRUD操作

        /// <summary>
        /// 增加一条数据
        /// </summary>
        string KnowledgeRelation_Add(KnowledgeRelationInfo model);

        /// <summary>
        /// 更新一条数据
        /// </summary>
        bool KnowledgeRelation_Update(KnowledgeRelationInfo model);

        /// <summary>
        /// 删除一条数据
        /// </summary>
        bool KnowledgeRelation_Delete(string RelationID);

        /// <summary>
        /// 批量删除数据
        /// </summary>
        bool KnowledgeRelation_DeleteList(string RelationIDlist);

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        KnowledgeRelationInfo KnowledgeRelation_GetModel(string RelationID);

        /// <summary>
        /// 获得数据列表
        /// </summary>
        DataSet KnowledgeRelation_GetList(string strWhere);

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        DataSet KnowledgeRelation_GetList(int top, string strWhere, string filedOrder);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        int KnowledgeRelation_GetRecordCount(string strWhere);

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        DataTable KnowledgeRelation_GetListByPage(int size, int index, string where, out int pagetotal, out int total);

        /// <summary>
        /// DataRow转Model
        /// </summary>
        KnowledgeRelationInfo KnowledgeRelation_DataRowToModel(DataRow row);

        #endregion

        #region 扩展方法

        /// <summary>
        /// 根据源知识ID获取关系列表
        /// </summary>
        List<KnowledgeRelationInfo> KnowledgeRelation_GetBySourceId(string sourceMemoryId);

        /// <summary>
        /// 根据目标知识ID获取关系列表
        /// </summary>
        List<KnowledgeRelationInfo> KnowledgeRelation_GetByTargetId(string targetMemoryId);

        /// <summary>
        /// 根据应用ID和关系类型获取关系列表
        /// </summary>
        List<KnowledgeRelationInfo> KnowledgeRelation_GetByAppAndType(string appId, string relationType, int limit);

        /// <summary>
        /// 批量插入知识关系
        /// </summary>
        int KnowledgeRelation_AddBatch(List<KnowledgeRelationInfo> relations);

        /// <summary>
        /// 删除指定知识的所有关系
        /// </summary>
        bool KnowledgeRelation_DeleteByMemoryId(string memoryId);

        /// <summary>
        /// 获取知识之间的关系强度
        /// </summary>
        float KnowledgeRelation_GetStrength(string sourceId, string targetId, string relationType);

        /// <summary>
        /// 更新关系强度
        /// </summary>
        bool KnowledgeRelation_UpdateStrength(string relationId, float newStrength);

        #endregion

        #region 统计方法

        /// <summary>
        /// 按关系类型统计数量
        /// </summary>
        DataTable KnowledgeRelation_GetCountByType(string appId);

        /// <summary>
        /// 获取基础统计数据
        /// </summary>
        DataTable KnowledgeRelation_GetStatistics(string appId);

        /// <summary>
        /// 按强度区间统计数量
        /// </summary>
        DataTable KnowledgeRelation_GetStrengthDistribution(string appId);

        #endregion
    }
}
