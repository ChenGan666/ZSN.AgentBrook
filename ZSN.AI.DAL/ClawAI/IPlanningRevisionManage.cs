using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial interface IPlanningRevisionManage
    {
        string SetConnectionName(string connName);

        #region tb_planning_revision
        /// <summary>
        /// 增加一条数据
        /// </summary>
        string PlanningRevision_Add(PlanningRevisionInfo model);

        /// <summary>
        /// 更新一条数据
        /// </summary>
        bool PlanningRevision_Update(PlanningRevisionInfo model);

        /// <summary>
        /// 删除一条数据
        /// </summary>
        bool PlanningRevision_Delete(string RevisionID);

        /// <summary>
        /// 批量删除数据
        /// </summary>
        bool PlanningRevision_DeleteList(string RevisionIDlist);

        /// <summary>
        /// 根据规划ID删除所有修订
        /// </summary>
        bool PlanningRevision_DeleteByPlanningID(string PlanningID);

        /// <summary>
        /// DataRow转Model
        /// </summary>
        PlanningRevisionInfo PlanningRevision_DataRowToModel(DataRow row);

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        PlanningRevisionInfo PlanningRevision_GetModel(string RevisionID);

        /// <summary>
        /// 获得数据列表
        /// </summary>
        DataSet PlanningRevision_GetList(string strWhere);

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        DataSet PlanningRevision_GetList(int top, string strWhere, string filedOrder);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        int PlanningRevision_GetRecordCount(string strWhere);
        #endregion
    }
}
