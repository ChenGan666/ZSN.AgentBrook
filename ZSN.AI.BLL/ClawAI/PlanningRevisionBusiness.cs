using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;

namespace ZSN.AI.BLL
{
    public partial class PlanningRevisionBusiness
    {
        #region 基础信息
        private const string ConnectionName = "KnowledgeBaseDb";
        #endregion

        #region tb_planning_revision
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(PlanningRevisionInfo model)
        {
            return DatabaseProvider.GetPlanningRevision(ConnectionName).PlanningRevision_Add(model);
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(PlanningRevisionInfo model)
        {
            return DatabaseProvider.GetPlanningRevision(ConnectionName).PlanningRevision_Update(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public static bool Delete(string RevisionID)
        {
            return DatabaseProvider.GetPlanningRevision(ConnectionName).PlanningRevision_Delete(RevisionID);
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public static bool DeleteList(string RevisionIDlist)
        {
            RevisionIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(RevisionIDlist, ',', '\'');
            return DatabaseProvider.GetPlanningRevision(ConnectionName).PlanningRevision_DeleteList(RevisionIDlist);
        }

        /// <summary>
        /// 根据规划ID删除所有修订
        /// </summary>
        public static bool DeleteByPlanningID(string PlanningID)
        {
            return DatabaseProvider.GetPlanningRevision(ConnectionName).PlanningRevision_DeleteByPlanningID(PlanningID);
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public static PlanningRevisionInfo GetModel(string RevisionID)
        {
            return DatabaseProvider.GetPlanningRevision(ConnectionName).PlanningRevision_GetModel(RevisionID);
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<PlanningRevisionInfo> GetList(string strWhere = "")
        {
            return PlanningRevisionDataSet_ToList(DatabaseProvider.GetPlanningRevision(ConnectionName).PlanningRevision_GetList(strWhere).Tables[0]);
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public static List<PlanningRevisionInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return PlanningRevisionDataSet_ToList(DatabaseProvider.GetPlanningRevision(ConnectionName).PlanningRevision_GetList(top, strWhere, filedOrder).Tables[0]);
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetPlanningRevision(ConnectionName).PlanningRevision_GetRecordCount(strWhere);
        }

        /// <summary>
        /// 根据规划ID获取修订历史
        /// </summary>
        public static List<PlanningRevisionInfo> GetByPlanningID(string PlanningID)
        {
            return GetList("planning_id='" + PlanningID + "'");
        }

        private static List<PlanningRevisionInfo> PlanningRevisionDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<PlanningRevisionInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetPlanningRevision(ConnectionName).PlanningRevision_DataRowToModel(r));
            }
            return list;
        }
        #endregion
    }
}
