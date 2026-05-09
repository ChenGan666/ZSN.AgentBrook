using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;

namespace ZSN.AI.BLL
{
    public partial class EpisodicMemoryBusiness
    {
        #region 基础信息
        private const string ConnectionName = "KnowledgeBaseDb";
        #endregion

        #region tb_episodic_memory
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(EpisodicMemoryInfo model)
        {
            return DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_Add(model);
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(EpisodicMemoryInfo model)
        {
            return DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_Update(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public static bool Delete(string MemoryID)
        {
            return DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_Delete(MemoryID);
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public static bool DeleteList(string MemoryIDlist)
        {
            MemoryIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(MemoryIDlist, ',', '\'');
            return DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_DeleteList(MemoryIDlist);
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public static EpisodicMemoryInfo GetModel(string MemoryID)
        {
            return DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_GetModel(MemoryID);
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<EpisodicMemoryInfo> GetList(string strWhere = "")
        {
            return EpisodicMemoryDataSet_ToList(DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_GetList(strWhere).Tables[0]);
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public static List<EpisodicMemoryInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return EpisodicMemoryDataSet_ToList(DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_GetList(top, strWhere, filedOrder).Tables[0]);
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_GetRecordCount(strWhere);
        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public static List<EpisodicMemoryInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return EpisodicMemoryDataSet_ToList(DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }

        /// <summary>
        /// 增加访问次数
        /// </summary>
        public static bool IncrementAccessCount(string MemoryID)
        {
            return DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_IncrementAccessCount(MemoryID);
        }

        /// <summary>
        /// 根据会话ID获取记忆列表
        /// </summary>
        public static List<EpisodicMemoryInfo> GetBySessionID(string SessionID)
        {
            return GetList("session_id='" + SessionID + "'");
        }

        /// <summary>
        /// 根据用户ID和应用ID获取记忆列表
        /// </summary>
        public static List<EpisodicMemoryInfo> GetByMemberAndApp(string MemberID, string AppID, int limit)
        {
            return GetList(limit, "member_id='" + MemberID + "' AND app_id='" + AppID + "'", "importance DESC, create_time DESC");
        }

        /// <summary>
        /// 根据事件类型获取记忆列表
        /// </summary>
        public static List<EpisodicMemoryInfo> GetByEventType(string EventType, int limit)
        {
            return GetList(limit, "event_type='" + EventType + "'", "create_time DESC");
        }

        private static List<EpisodicMemoryInfo> EpisodicMemoryDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<EpisodicMemoryInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetEpisodicMemory(ConnectionName).EpisodicMemory_DataRowToModel(r));
            }
            return list;
        }
        #endregion
    }
}
