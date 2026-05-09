using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial interface IEpisodicMemoryManage
    {
        string SetConnectionName(string connName);

        #region tb_episodic_memory
        /// <summary>
        /// 增加一条数据
        /// </summary>
        string EpisodicMemory_Add(EpisodicMemoryInfo model);

        /// <summary>
        /// 更新一条数据
        /// </summary>
        bool EpisodicMemory_Update(EpisodicMemoryInfo model);

        /// <summary>
        /// 删除一条数据
        /// </summary>
        bool EpisodicMemory_Delete(string MemoryID);

        /// <summary>
        /// 批量删除数据
        /// </summary>
        bool EpisodicMemory_DeleteList(string MemoryIDlist);

        /// <summary>
        /// DataRow转Model
        /// </summary>
        EpisodicMemoryInfo EpisodicMemory_DataRowToModel(DataRow row);

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        EpisodicMemoryInfo EpisodicMemory_GetModel(string MemoryID);

        /// <summary>
        /// 获得数据列表
        /// </summary>
        DataSet EpisodicMemory_GetList(string strWhere);

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        DataSet EpisodicMemory_GetList(int top, string strWhere, string filedOrder);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        int EpisodicMemory_GetRecordCount(string strWhere);

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        DataSet EpisodicMemory_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);

        /// <summary>
        /// 增加访问次数
        /// </summary>
        bool EpisodicMemory_IncrementAccessCount(string MemoryID);
        #endregion
    }
}
