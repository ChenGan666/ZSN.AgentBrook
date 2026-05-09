using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.DAL
{
    /// <summary>
    /// 用户反馈数据访问接口
    /// </summary>
    public partial interface IUserFeedbackManage
    {
        string SetConnectionName(string connName);

        #region 基础CRUD操作

        /// <summary>
        /// 增加一条数据
        /// </summary>
        string UserFeedback_Add(UserFeedbackInfo model);

        /// <summary>
        /// 更新一条数据
        /// </summary>
        bool UserFeedback_Update(UserFeedbackInfo model);

        /// <summary>
        /// 删除一条数据
        /// </summary>
        bool UserFeedback_Delete(string FeedbackID);

        /// <summary>
        /// 批量删除数据
        /// </summary>
        bool UserFeedback_DeleteList(string FeedbackIDlist);

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        UserFeedbackInfo UserFeedback_GetModel(string FeedbackID);

        /// <summary>
        /// 获得数据列表
        /// </summary>
        DataSet UserFeedback_GetList(string strWhere);

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        DataSet UserFeedback_GetList(int top, string strWhere, string filedOrder);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        int UserFeedback_GetRecordCount(string strWhere);

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        DataTable UserFeedback_GetListByPage(int size, int index, string where, out int pagetotal, out int total);

        /// <summary>
        /// DataRow转Model
        /// </summary>
        UserFeedbackInfo UserFeedback_DataRowToModel(DataRow row);

        #endregion

        #region 扩展方法

        /// <summary>
        /// 根据记忆ID获取反馈列表
        /// </summary>
        List<UserFeedbackInfo> UserFeedback_GetByMemoryId(string memoryId, int limit);

        /// <summary>
        /// 根据用户ID和应用ID获取反馈列表
        /// </summary>
        List<UserFeedbackInfo> UserFeedback_GetByMemberAndApp(string memberId, string appId, int limit);

        /// <summary>
        /// 根据会话ID获取反馈列表
        /// </summary>
        List<UserFeedbackInfo> UserFeedback_GetBySessionId(string sessionId);

        /// <summary>
        /// 根据反馈类型获取统计信息
        /// </summary>
        Dictionary<string, int> UserFeedback_GetStatsByType(string appId, DateTime? startTime, DateTime? endTime);

        /// <summary>
        /// 获取最近的反馈列表
        /// </summary>
        List<UserFeedbackInfo> UserFeedback_GetRecentFeedbacks(string appId, int days, int limit);

        /// <summary>
        /// 批量插入用户反馈
        /// </summary>
        int UserFeedback_AddBatch(List<UserFeedbackInfo> feedbacks);

        /// <summary>
        /// 获取知识的反馈统计信息
        /// </summary>
        KnowledgeFeedbackStats UserFeedback_GetKnowledgeStats(string memoryId, int recentDays);

        /// <summary>
        /// 根据应用和时间范围获取反馈统计
        /// </summary>
        DataSet UserFeedback_GetStatsByAppAndTime(string appId, DateTime startTime, DateTime endTime);

        #endregion
    }
}
