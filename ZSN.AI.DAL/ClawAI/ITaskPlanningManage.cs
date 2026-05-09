using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial interface ITaskPlanningManage
    {
        string SetConnectionName(string connName);

        #region tb_task_planning
        /// <summary>
        /// 增加一条数据
        /// </summary>
        string TaskPlanning_Add(TaskPlanningInfo model);

        /// <summary>
        /// 更新一条数据
        /// </summary>
        bool TaskPlanning_Update(TaskPlanningInfo model);

        /// <summary>
        /// 删除一条数据
        /// </summary>
        bool TaskPlanning_Delete(string PlanningID);

        /// <summary>
        /// 批量删除数据
        /// </summary>
        bool TaskPlanning_DeleteList(string PlanningIDlist);

        /// <summary>
        /// DataRow转Model
        /// </summary>
        TaskPlanningInfo TaskPlanning_DataRowToModel(DataRow row);

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        TaskPlanningInfo TaskPlanning_GetModel(string PlanningID);

        /// <summary>
        /// 获得数据列表
        /// </summary>
        DataSet TaskPlanning_GetList(string strWhere);

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        DataSet TaskPlanning_GetList(int top, string strWhere, string filedOrder);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        int TaskPlanning_GetRecordCount(string strWhere);

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        DataSet TaskPlanning_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        DataTable TaskPlanning_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);

        /// <summary>
        /// 更新规划状态
        /// </summary>
        bool TaskPlanning_UpdateStatus(string PlanningID, string status);

        /// <summary>
        /// 增加修订次数
        /// </summary>
        bool TaskPlanning_IncrementRevisionCount(string PlanningID);
        #endregion
    }
}
