using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public partial class KnowledgeBaseTagInfoBussiness
    {
        #region 基础信息
        private const string ConnectionName = "ModelDb";
        #endregion
        #region tb_knowledge_base_tag_info
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static int Add(KnowledgeBaseTagInfo model)
        {
            return DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_Add(model);
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(KnowledgeBaseTagInfo model)
        {
            return DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_Update(model);
        }
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 knowledgeBaseTagID)
        {
            return DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_Delete(knowledgeBaseTagID);
        }
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string knowledgeBaseTagIDlist)
        {
            return DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_DeleteList(knowledgeBaseTagIDlist);
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.KnowledgeBaseTagInfo GetModel(Int32 knowledgeBaseTagID)
        {
            return DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_GetModel(knowledgeBaseTagID);
        }
        public static ZSN.AI.Entity.KnowledgeBaseTagInfo GetModel(string Tag)
        {
            return DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_GetModel(Tag);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<KnowledgeBaseTagInfo> GetList(string strWhere = "")
        {
            return KnowledgeBaseTagInfoDataSet_ToList(DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<KnowledgeBaseTagInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return KnowledgeBaseTagInfoDataSet_ToList(DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<KnowledgeBaseTagInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return KnowledgeBaseTagInfoDataSet_ToList(DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        /// <param name="pageSize">每页大小</param>
        /// <param name="pageIndex">页标</param>
        /// <param name="strWhere">查询条件</param>
        /// <param name="pagetotal">总页数</param>
        /// <param name="total">总数</param>
        /// <param name="orderType">排序规则， 默认降序，1降序，0升序</param>
        /// <param name="showName">显示字段，默认全部</param>
        /// <param name="orderKey">排序key，默认主键</param>
        /// <returns></returns>
        public static List<KnowledgeBaseTagInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "KnowledgeBaseTagID")
        {
            return KnowledgeBaseTagInfoDataSet_ToList(DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
        private static List<KnowledgeBaseTagInfo> KnowledgeBaseTagInfoDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<KnowledgeBaseTagInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetKnowledgeBaseTagInfo(ConnectionName).KnowledgeBaseTagInfo_DataRowToModel(r));
            }
            return list;
        }

        /// <summary>
        /// 获取指定分类及其所有子分类下的标签
        /// </summary>
        /// <param name="tagClassID">分类ID</param>
        /// <returns>标签列表</returns>
        public static List<KnowledgeBaseTagInfo> GetAllTagByTagClassID(int tagClassID)
        {
            // 获取指定分类及其所有子分类
            var categories = BaseDictionaryInfoBussiness.GetAllChildList(tagClassID, false, false);

            // 提取所有分类ID
            var categoryIds = new List<int> { tagClassID }; // 包含主分类ID
            categoryIds.AddRange(categories.Select(c => c.DicId)); // 添加所有子分类ID

            // 获取所有标签
            var allTags = GetList();

            // 筛选出匹配的标签
            return allTags.Where(tag => categoryIds.Contains(tag.TagClassID)).ToList();
        }
        #endregion
    }
}
