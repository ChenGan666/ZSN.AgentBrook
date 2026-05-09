using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial interface ITaskStepManage
    {
        string SetConnectionName(string connName);

        #region tb_task_step
        /// <summary>
        /// 增加一条数据
        /// </summary>
        string TaskStep_Add(TaskStepInfo model);

        /// <summary>
        /// 批量增加数据
        /// </summary>
        int TaskStep_AddBatch(List<TaskStepInfo> models);

        /// <summary>
        /// 更新一条数据
        /// </summary>
        bool TaskStep_Update(TaskStepInfo model);

        /// <summary>
        /// 删除一条数据
        /// </summary>
        bool TaskStep_Delete(string StepID);

        /// <summary>
        /// 批量删除数据
        /// </summary>
        bool TaskStep_DeleteList(string StepIDlist);

        /// <summary>
        /// 根据规划ID删除所有步骤
        /// </summary>
        bool TaskStep_DeleteByPlanningID(string PlanningID);

        /// <summary>
        /// DataRow转Model
        /// </summary>
        TaskStepInfo TaskStep_DataRowToModel(DataRow row);

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        TaskStepInfo TaskStep_GetModel(string StepID);

        /// <summary>
        /// 获得数据列表
        /// </summary>
        DataSet TaskStep_GetList(string strWhere);

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        DataSet TaskStep_GetList(int top, string strWhere, string filedOrder);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        int TaskStep_GetRecordCount(string strWhere);

        /// <summary>
        /// 更新步骤状态
        /// </summary>
        bool TaskStep_UpdateStatus(string StepID, string status);

        /// <summary>
        /// 更新步骤执行结果
        /// </summary>
        bool TaskStep_UpdateExecutionResult(string StepID, string actualOutput, string executionResult, int qualityScore);
        #endregion
    }
}
