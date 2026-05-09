using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
namespace ZSN.AI.BLL
{
    public class TreeNode
    {
        public string name { get; set; }
        public int value { get; set; }

        public bool tag { get; set; } = false;
        public List<TreeNode> children { get; set; }
    }
    public partial class BaseDictionaryInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "BaseDb";
        #endregion
		#region base_dictionary_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(BaseDictionaryInfo model)
		{
			return DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(BaseDictionaryInfo model)
		{
			return DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 dicId)
		{
			return DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_Delete(dicId);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string dicIdlist)
		{
			return DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_DeleteList(dicIdlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.BaseDictionaryInfo GetModel(Int32 dicId)
		{
			return DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_GetModel(dicId);
		}
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<BaseDictionaryInfo> GetList(string strWhere = "")
        {
            return BaseDictionaryInfoDataSet_ToList(DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_GetList(strWhere).Tables[0]);
        }
        public static List<BaseDictionaryInfo> GetChildList(string Name = "")
        {
            return BaseDictionaryInfoDataSet_ToList(DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_GetChildList(Name).Tables[0]);
        }
        /// <summary>
        /// 获得所有子级
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="isDB">c#方法还是数据库方法，数据库方法需MySQL8.0+</param>
        /// <param name="isCount">是否统计数据</param>
        /// <returns></returns>
        public static List<BaseDictionaryInfo> GetAllChildList(string Name = "",bool isDB = false,bool isCount = false)
        {
            List<BaseDictionaryInfo> _result = new List<BaseDictionaryInfo>();
            if (isDB)
            {
                _result = GetAllChildList(Name);
            }
            else
            {
                var _allList = GetList();
                _result = GetAllChildren(_allList,Name);
            }
            if (isCount)
            {
                _result = GetCount(_result);
            }
            

            return _result;
        }
        public static List<BaseDictionaryInfo> GetAllChildList(int DicId = 0, bool isDB = false, bool isCount = false)
        {
            List<BaseDictionaryInfo> _result = new List<BaseDictionaryInfo>();
            if (isDB)
            {
                _result = GetAllChildList(DicId);
            }
            else
            {
                var _allList = GetList();
                _result = GetAllChildren(_allList, DicId);
            }
            if (isCount)
            {
                _result = GetCount(_result);
            }


            return _result;
        }

        private static List<BaseDictionaryInfo> GetCount(List<BaseDictionaryInfo>  _result) {
            // 获取所有标签数据
            var allTags = KnowledgeBaseTagInfoBussiness.GetList();

            // 获取所有节点数据，用于建立层级关系
            var allNodes = GetList();

            // 首先为每个节点计算其自身的直接统计值
            foreach (var node in _result)
            {
                // 统计与该节点DicId相同的TagClassID的KnowledgeBaseTagInfo数量
                node.TagCount = allTags.Count(tag => tag.TagClassID == node.DicId);

                // 使用标签的TCount字段统计知识库数量
                node.KnowledgeBaseCount = 0;
                var tagsForThisNode = allTags.Where(tag => tag.TagClassID == node.DicId);
                if (tagsForThisNode.Any())
                {
                    // 累加该节点下所有标签的TCount值
                    node.KnowledgeBaseCount = tagsForThisNode.Sum(tag => tag.TCount);
                }
            }

            // 构建节点父子关系的字典
            Dictionary<int, List<int>> parentToChildren = new Dictionary<int, List<int>>();
            foreach (var node in allNodes)
            {
                if (node.Pid.HasValue)
                {
                    if (!parentToChildren.ContainsKey(node.Pid.Value))
                    {
                        parentToChildren[node.Pid.Value] = new List<int>();
                    }
                    parentToChildren[node.Pid.Value].Add(node.DicId);
                }
            }

            // 得到所有叶子节点（没有子节点的节点）
            HashSet<int> leafNodes = new HashSet<int>(_result.Select(n => n.DicId));
            foreach (var children in parentToChildren.Values)
            {
                foreach (var childId in children)
                {
                    if (leafNodes.Contains(childId))
                    {
                        leafNodes.Remove(childId);
                    }
                }
            }

            // 建立节点ID到节点的字典，便于快速查找
            Dictionary<int, BaseDictionaryInfo> idToNode = _result.ToDictionary(n => n.DicId);

            // 使用递归方法计算累计的计数到父节点
            foreach (var node in _result)
            {
                AccumulateChildrenCounts(node.DicId, parentToChildren, idToNode);
            }

            return _result;
        }

        /// <summary>
        /// 递归计算父级节点的累计值
        /// </summary>
        /// <param name="nodeId">当前节点ID</param>
        /// <param name="parentToChildren">父子关系字典</param>
        /// <param name="idToNode">ID到节点的映射</param>
        /// <returns>当前节点及其所有子孙节点的统计总和</returns>
        private static (int TagCount, int KnowledgeBaseCount) AccumulateChildrenCounts(int nodeId, Dictionary<int, List<int>> parentToChildren, Dictionary<int, BaseDictionaryInfo> idToNode)
        {
            // 当前节点自身的统计值
            int tagCount = 0;
            int knowledgeBaseCount = 0;
            
            if (idToNode.TryGetValue(nodeId, out var node))
            {
                tagCount = node.TagCount;
                knowledgeBaseCount = node.KnowledgeBaseCount;
            }
            
            // 如果当前节点有子节点，递归计算并累加子节点的统计值
            if (parentToChildren.TryGetValue(nodeId, out var children))
            {
                foreach (var childId in children)
                {
                    var (childTagCount, childKnowledgeBaseCount) = AccumulateChildrenCounts(childId, parentToChildren, idToNode);
                    tagCount += childTagCount;
                    knowledgeBaseCount += childKnowledgeBaseCount;
                }
                
                // 更新当前节点的统计值（包含自身和所有子孙节点）
                if (idToNode.TryGetValue(nodeId, out var currentNode))
                {
                    currentNode.TagCount = tagCount;
                    currentNode.KnowledgeBaseCount = knowledgeBaseCount;
                }
            }
            
            return (tagCount, knowledgeBaseCount);
        }
        public static List<BaseDictionaryInfo> GetAllChildren(List<BaseDictionaryInfo> allItems, string dicName)
        {
            var root = allItems.FirstOrDefault(x => x.DicName == dicName);
            if (root == null) return new List<BaseDictionaryInfo>();

            List<BaseDictionaryInfo> result = new List<BaseDictionaryInfo>();
            GetChildrenRecursive(allItems, root.DicId, result);
            return result;
        }
        public static List<BaseDictionaryInfo> GetAllChildren(List<BaseDictionaryInfo> allItems, int DicId)
        {
            var root = allItems.FirstOrDefault(x => x.DicId == DicId);
            if (root == null) return new List<BaseDictionaryInfo>();

            List<BaseDictionaryInfo> result = new List<BaseDictionaryInfo>();
            GetChildrenRecursive(allItems, root.DicId, result);
            return result;
        }

        private static void GetChildrenRecursive(List<BaseDictionaryInfo> allItems, int parentId, List<BaseDictionaryInfo> result)
        {
            var children = allItems.Where(x => x.Pid == parentId).ToList();
            foreach (var child in children)
            {
                result.Add(child);
                GetChildrenRecursive(allItems, child.DicId, result);
            }
        }
        private static List<BaseDictionaryInfo>  GetAllChildList(string Name = "")
        {
            return BaseDictionaryInfoDataSet_ToList(DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_GetAllChildList(Name).Tables[0]);
        }
        private static List<BaseDictionaryInfo> GetAllChildList(int DicId = 0)
        {
            return BaseDictionaryInfoDataSet_ToList(DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_GetAllChildList(DicId).Tables[0]);
        }

        public static List<BaseDictionaryInfo> GetAllParentList(string Name = "", bool isDB = false)
        {
            if (isDB)
            {
                return GetAllParentList(Name);
            }
            else
            {
                var _allList = GetList();
                return GetAllParents(_allList, Name);
            }
        }
        public static List<BaseDictionaryInfo> GetAllParents(List<BaseDictionaryInfo> allItems, string dicName)
        {
            var current = allItems.FirstOrDefault(x => x.DicName == dicName);
            if (current == null) return new List<BaseDictionaryInfo>();

            List<BaseDictionaryInfo> result = new List<BaseDictionaryInfo>();
            GetParentsRecursive(allItems, current, result);
            return result;
        }

        private static void GetParentsRecursive(List<BaseDictionaryInfo> allItems, BaseDictionaryInfo current, List<BaseDictionaryInfo> result)
        {
            var parent = allItems.FirstOrDefault(x => x.DicId == current.Pid);
            if (parent != null)
            {
                result.Add(parent);
                GetParentsRecursive(allItems, parent, result);
            }
        }

        private static List<BaseDictionaryInfo>   GetAllParentList(string Name = "")
        {
            return BaseDictionaryInfoDataSet_ToList(DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_GetAllParentList(Name).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<BaseDictionaryInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return BaseDictionaryInfoDataSet_ToList(DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<BaseDictionaryInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return BaseDictionaryInfoDataSet_ToList(DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<BaseDictionaryInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "DicId")
		{
            return BaseDictionaryInfoDataSet_ToList(DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<BaseDictionaryInfo> BaseDictionaryInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<BaseDictionaryInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetBaseDictionaryInfo(ConnectionName).BaseDictionaryInfo_DataRowToModel(r));
            }
            return list;
		}


        public static List<TreeNode> BuildTree(List<BaseDictionaryInfo> allItems, int parentId = 0)
        {
            return allItems
                .Where(item => item.Pid == parentId)
                .OrderBy(item => item.Sort)
                .Select(item => new TreeNode
                {
                    name = item.DicName,
                    value = item.DicId,
                    children = BuildTree(allItems, item.DicId)
                })
                .ToList();
        }
        /// <summary>
        /// 构建树形结构,将BaseDictionaryInfo（作为枝）与KnowledgeBaseTagInfo（作为叶）合并为一棵树
        /// </summary>
        /// <param name="DictItems"></param>
        /// <param name="TagItems"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public static List<TreeNode> BuildTree(List<BaseDictionaryInfo> DictItems, List<KnowledgeBaseTagInfo> TagItems, int parentId = 0)
        {
            // 获取当前层级的DictItems
            var dictNodes = DictItems
                .Where(item => item.Pid == parentId)
                .OrderBy(item => item.Sort)
                .Select(item => new TreeNode
                {
                    name = item.DicName,
                    value = item.DicId,
                    tag = false,
                    children = BuildTree(DictItems, TagItems, item.DicId) // 递归构建子DictItems
                })
                .ToList();

            // 如果是叶节点级别（parentId > 0），添加匹配的TagItems
            if (parentId > 0)
            {
                var tagNodes = TagItems
                    .Where(tag => tag.TagClassID == parentId)
                    .Select(tag => new TreeNode
                    {
                        name = tag.Tag,
                        value = tag.KnowledgeBaseTagID,
                        tag = true,
                        children = new List<TreeNode>() // 叶节点没有子节点
                    })
                    .ToList();

                dictNodes.AddRange(tagNodes);
            }

            return dictNodes;
        }
        #endregion
    }
}
