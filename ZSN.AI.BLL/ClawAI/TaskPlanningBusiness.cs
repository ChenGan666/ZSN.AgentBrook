using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;

namespace ZSN.AI.BLL
{
    public partial class TaskPlanningBusiness
    {
        #region 基础信息
        private const string ConnectionName = "KnowledgeBaseDb";
        #endregion

        #region tb_task_planning
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(TaskPlanningInfo model)
        {
            return DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_Add(model);
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(TaskPlanningInfo model)
        {
            return DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_Update(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public static bool Delete(string PlanningID)
        {
            return DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_Delete(PlanningID);
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public static bool DeleteList(string PlanningIDlist)
        {
            PlanningIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(PlanningIDlist, ',', '\'');
            return DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_DeleteList(PlanningIDlist);
        }

        /// <summary>
        /// 得到一个对象实体(含步骤)
        /// </summary>
        public static TaskPlanningInfo GetModel(string PlanningID)
        {
            TaskPlanningInfo planning = DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_GetModel(PlanningID);
            if (planning != null)
            {
                // 查询步骤列表
                planning.Steps = TaskStepBusiness.GetList("planning_id='" + PlanningID + "'");
            }
            return planning;
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<TaskPlanningInfo> GetList(string strWhere = "")
        {
            return TaskPlanningDataSet_ToList(DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_GetList(strWhere).Tables[0]);
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public static List<TaskPlanningInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return TaskPlanningDataSet_ToList(DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_GetList(top, strWhere, filedOrder).Tables[0]);
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_GetRecordCount(strWhere);
        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public static List<TaskPlanningInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return TaskPlanningDataSet_ToList(DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public static List<TaskPlanningInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CreateTime")
        {
            return TaskPlanningDataSet_ToList(DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }

        /// <summary>
        /// 更新规划状态
        /// </summary>
        public static bool UpdateStatus(string PlanningID, string status)
        {
            return DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_UpdateStatus(PlanningID, status);
        }

        /// <summary>
        /// 增加修订次数
        /// </summary>
        public static bool IncrementRevisionCount(string PlanningID)
        {
            return DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_IncrementRevisionCount(PlanningID);
        }

        /// <summary>
        /// 根据会话ID获取规划列表
        /// </summary>
        public static List<TaskPlanningInfo> GetBySessionID(string SessionID)
        {
            return GetList("session_id='" + SessionID + "'");
        }

        /// <summary>
        /// 获取历史规划(已完成的)
        /// </summary>
        public static List<TaskPlanningInfo> GetHistoricalPlans(string MemberID, string AppID, int limit)
        {
            return GetList(limit, "member_id='" + MemberID + "' AND app_id='" + AppID + "' AND planning_status='Completed'", "create_time DESC");
        }

        private static List<TaskPlanningInfo> TaskPlanningDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<TaskPlanningInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetTaskPlanning(ConnectionName).TaskPlanning_DataRowToModel(r));
            }
            return list;
        }
        #endregion
    }
}
