using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;

namespace ZSN.AI.BLL
{
    public partial class AIPersonalityStateBusiness
    {
        #region 基础信息
        private const string ConnectionName = "KnowledgeBaseDb";
        #endregion

        #region tb_ai_personality_state
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(AIPersonalityStateInfo model)
        {
            return DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_Add(model);
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(AIPersonalityStateInfo model)
        {
            return DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_Update(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public static bool Delete(string StateID)
        {
            return DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_Delete(StateID);
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public static bool DeleteList(string StateIDlist)
        {
            StateIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(StateIDlist, ',', '\'');
            return DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_DeleteList(StateIDlist);
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public static AIPersonalityStateInfo GetModel(string StateID)
        {
            return DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_GetModel(StateID);
        }

        /// <summary>
        /// 根据会话ID获取状态
        /// </summary>
        public static AIPersonalityStateInfo GetBySessionID(string SessionID)
        {
            return DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_GetBySessionID(SessionID);
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<AIPersonalityStateInfo> GetList(string strWhere = "")
        {
            return AIPersonalityStateDataSet_ToList(DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_GetList(strWhere).Tables[0]);
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public static List<AIPersonalityStateInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return AIPersonalityStateDataSet_ToList(DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_GetList(top, strWhere, filedOrder).Tables[0]);
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_GetRecordCount(strWhere);
        }

        /// <summary>
        /// 增加交互次数
        /// </summary>
        public static bool IncrementInteractions(string StateID)
        {
            return DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_IncrementInteractions(StateID);
        }

        /// <summary>
        /// 更新成功率
        /// </summary>
        public static bool UpdateSuccessRate(string StateID, decimal successRate)
        {
            return DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_UpdateSuccessRate(StateID, successRate);
        }

        private static List<AIPersonalityStateInfo> AIPersonalityStateDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<AIPersonalityStateInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetAIPersonalityState(ConnectionName).AIPersonalityState_DataRowToModel(r));
            }
            return list;
        }
        #endregion
    }
}
