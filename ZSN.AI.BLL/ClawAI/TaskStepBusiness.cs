using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;

namespace ZSN.AI.BLL
{
    public partial class TaskStepBusiness
    {
        #region 基础信息
        private const string ConnectionName = "KnowledgeBaseDb";
        #endregion

        #region tb_task_step
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(TaskStepInfo model)
        {
            return DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_Add(model);
        }

        /// <summary>
        /// 批量增加数据
        /// </summary>
        public static int AddBatch(List<TaskStepInfo> models)
        {
            return DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_AddBatch(models);
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(TaskStepInfo model)
        {
            return DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_Update(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public static bool Delete(string StepID)
        {
            return DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_Delete(StepID);
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public static bool DeleteList(string StepIDlist)
        {
            StepIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(StepIDlist, ',', '\'');
            return DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_DeleteList(StepIDlist);
        }

        /// <summary>
        /// 根据规划ID删除所有步骤
        /// </summary>
        public static bool DeleteByPlanningID(string PlanningID)
        {
            return DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_DeleteByPlanningID(PlanningID);
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public static TaskStepInfo GetModel(string StepID)
        {
            return DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_GetModel(StepID);
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<TaskStepInfo> GetList(string strWhere = "")
        {
            return TaskStepDataSet_ToList(DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_GetList(strWhere).Tables[0]);
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public static List<TaskStepInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return TaskStepDataSet_ToList(DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_GetList(top, strWhere, filedOrder).Tables[0]);
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_GetRecordCount(strWhere);
        }

        /// <summary>
        /// 更新步骤状态
        /// </summary>
        public static bool UpdateStatus(string StepID, string status)
        {
            return DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_UpdateStatus(StepID, status);
        }

        /// <summary>
        /// 更新步骤执行结果
        /// </summary>
        public static bool UpdateExecutionResult(string StepID, string actualOutput, string executionResult, int qualityScore)
        {
            return DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_UpdateExecutionResult(StepID, actualOutput, executionResult, qualityScore);
        }

        /// <summary>
        /// 根据规划ID获取所有步骤
        /// </summary>
        public static List<TaskStepInfo> GetByPlanningID(string PlanningID)
        {
            return GetList("planning_id='" + PlanningID + "'");
        }

        private static List<TaskStepInfo> TaskStepDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<TaskStepInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetTaskStep(ConnectionName).TaskStep_DataRowToModel(r));
            }
            return list;
        }
        #endregion
    }
}
