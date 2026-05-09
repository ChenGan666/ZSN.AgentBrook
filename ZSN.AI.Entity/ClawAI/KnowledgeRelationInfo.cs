using System;

namespace ZSN.AI.Entity.ClawAI
{
    /// <summary>
    /// tb_claw_knowledge_relation - Claw AI知识关系表
    /// 用于构建知识图谱，存储知识之间的关联关系
    /// </summary>
    public partial class KnowledgeRelationInfo
    {
        public KnowledgeRelationInfo() { }

        #region AutoField

        /// <summary>
        /// 关系ID
        /// </summary>
        public string RelationID { get; set; } = string.Empty;

        /// <summary>
        /// 应用ID
        /// </summary>
        public string AppID { get; set; } = string.Empty;

        /// <summary>
        /// 源知识ID
        /// </summary>
        public string SourceMemoryID { get; set; } = string.Empty;

        /// <summary>
        /// 目标知识ID
        /// </summary>
        public string TargetMemoryID { get; set; } = string.Empty;

        /// <summary>
        /// 关系类型: related(相关), prerequisite(前置), derived(派生), conflict(冲突), example(示例), category(分类)
        /// </summary>
        public string RelationType { get; set; } = string.Empty;

        /// <summary>
        /// 关系强度(0-1)
        /// </summary>
        public float Strength { get; set; } = 0.5f;

        /// <summary>
        /// 元数据(JSON格式,存储额外信息)
        /// </summary>
        public string Metadata { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;

        #endregion
    }

    /// <summary>
    /// 关系类型枚举
    /// </summary>
    public enum KnowledgeRelationType
    {
        /// <summary>
        /// 相关知识
        /// </summary>
        Related,

        /// <summary>
        /// 前置知识
        /// </summary>
        Prerequisite,

        /// <summary>
        /// 派生知识
        /// </summary>
        Derived,

        /// <summary>
        /// 冲突知识
        /// </summary>
        Conflict,

        /// <summary>
        /// 示例关系
        /// </summary>
        Example,

        /// <summary>
        /// 分类关系
        /// </summary>
        Category
    }
}
