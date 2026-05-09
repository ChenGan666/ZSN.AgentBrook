using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial interface IAIPersonalityStateManage
    {
        string SetConnectionName(string connName);

        #region tb_ai_personality_state
        /// <summary>
        /// 增加一条数据
        /// </summary>
        string AIPersonalityState_Add(AIPersonalityStateInfo model);

        /// <summary>
        /// 更新一条数据
        /// </summary>
        bool AIPersonalityState_Update(AIPersonalityStateInfo model);

        /// <summary>
        /// 删除一条数据
        /// </summary>
        bool AIPersonalityState_Delete(string StateID);

        /// <summary>
        /// 批量删除数据
        /// </summary>
        bool AIPersonalityState_DeleteList(string StateIDlist);

        /// <summary>
        /// DataRow转Model
        /// </summary>
        AIPersonalityStateInfo AIPersonalityState_DataRowToModel(DataRow row);

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        AIPersonalityStateInfo AIPersonalityState_GetModel(string StateID);

        /// <summary>
        /// 根据会话ID获取状态
        /// </summary>
        AIPersonalityStateInfo AIPersonalityState_GetBySessionID(string SessionID);

        /// <summary>
        /// 获得数据列表
        /// </summary>
        DataSet AIPersonalityState_GetList(string strWhere);

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        DataSet AIPersonalityState_GetList(int top, string strWhere, string filedOrder);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        int AIPersonalityState_GetRecordCount(string strWhere);

        /// <summary>
        /// 增加交互次数
        /// </summary>
        bool AIPersonalityState_IncrementInteractions(string StateID);

        /// <summary>
        /// 更新成功率
        /// </summary>
        bool AIPersonalityState_UpdateSuccessRate(string StateID, decimal successRate);
        #endregion
    }
}
