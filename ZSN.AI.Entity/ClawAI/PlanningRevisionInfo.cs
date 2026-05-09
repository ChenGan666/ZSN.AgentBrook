using System;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_planning_revision 规划修订历史表
    /// </summary>
    public partial class PlanningRevisionInfo
    {
        public PlanningRevisionInfo() { }

        #region AutoField
        /// <summary>
        /// 修订ID
        /// </summary>
        public string RevisionID { get; set; }

        /// <summary>
        /// 规划ID(外键)
        /// </summary>
        public string PlanningID { get; set; }

        /// <summary>
        /// 修订版本号
        /// </summary>
        public int RevisionVersion { get; set; }

        /// <summary>
        /// 修订原因
        /// </summary>
        public string RevisionReason { get; set; }

        /// <summary>
        /// 修订前内容(JSON)
        /// </summary>
        public string ContentBefore { get; set; }

        /// <summary>
        /// 修订后内容(JSON)
        /// </summary>
        public string ContentAfter { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        #endregion
    }
}
