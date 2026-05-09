using Microsoft.AspNetCore.Mvc;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity.ClawAI;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class KnowledgeGraphController : AdminBaseController
    {
        /// <summary>
        /// 知识图谱列表页面（显示所有记忆节点）
        /// </summary>
        public IActionResult Index(int index = 1, int size = 10, string appId = "", string clawId = "")
        {
            // 获取记忆列表
            var memories = LongTermMemoryBusiness.GetListByPage(size, index, "", out int pageTotal, out int total);

            // 如果指定了appId或clawId，进行过滤
            if (!string.IsNullOrEmpty(appId) || !string.IsNullOrEmpty(clawId))
            {
                memories = memories.Where(m =>
                    (string.IsNullOrEmpty(appId) || m.AppID == appId) &&
                    (string.IsNullOrEmpty(clawId) || m.ClawID == clawId)
                ).ToList();
            }

            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.MemoryList = memories;
            ViewBag.AppId = appId;
            ViewBag.ClawId = clawId;

            return View();
        }

        /// <summary>
        /// 关系列表页面（显示所有关系）
        /// </summary>
        public IActionResult Relations(int index = 1, int size = 10, string appId = "")
        {
            // 获取关系列表
            var relations = KnowledgeRelationBusiness.GetListByPage(size, index, "", out int pageTotal, out int total);

            // 如果指定了appId，进行过滤
            if (!string.IsNullOrEmpty(appId))
            {
                relations = relations.Where(r => r.AppID == appId).ToList();
            }

            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.RelationList = relations;
            ViewBag.AppId = appId;

            return View();
        }

        /// <summary>
        /// 知识图谱可视化页面（基于AntV G6）
        /// </summary>
        public IActionResult Visualize(string memoryId)
        {
            if (string.IsNullOrEmpty(memoryId))
            {
                return Content("请指定知识节点ID");
            }

            var memory = LongTermMemoryBusiness.GetModel(memoryId);
            if (memory == null)
            {
                return Content("知识节点不存在");
            }

            ViewBag.Memory = memory;
            ViewBag.MemoryId = memoryId;

            return View();
        }

        /// <summary>
        /// 获取知识图谱数据（用于可视化）
        /// </summary>
        public JsonMsg<object> GetGraphData(string memoryId, int maxDepth = 2, int maxNodes = 50)
        {
            if (string.IsNullOrEmpty(memoryId))
            {
                return JsonMsg<object>.Error("知识节点ID不能为空",ErrorCode.DataNoMemoryId);
            }

            try
            {
                // 获取中心节点
                var centerMemory = LongTermMemoryBusiness.GetModel(memoryId);
                if (centerMemory == null)
                {
                    return JsonMsg<object>.Error("知识节点不存在", ErrorCode.DataLongTermMemoryNull);
                }

                // 获取相关知识
                var relatedMemories = KnowledgeRelationBusiness.GetRelatedKnowledge(
                    memoryId,
                    maxDepth: maxDepth,
                    maxResults: maxNodes);

                // 获取所有关系
                var allRelations = new List<KnowledgeRelationInfo>();

                // 添加直接关系
                var directRelations = KnowledgeRelationBusiness.GetBySourceId(memoryId);
                allRelations.AddRange(directRelations);

                // 构建G6格式的数据
                var nodes = new List<object>();
                var edges = new List<object>();

                // 添加中心节点
                nodes.Add(new
                {
                    id = centerMemory.MemoryID,
                    label = TruncateText(centerMemory.Summary, 20),
                    type = "center",
                    knowledgeType = centerMemory.KnowledgeType,
                    topic = centerMemory.Topic,
                    importance = centerMemory.Importance,
                    fullLabel = centerMemory.Summary,
                    content = centerMemory.Content
                });

                // 添加相关节点和边
                foreach (var mem in relatedMemories)
                {
                    if (mem.MemoryID != memoryId)
                    {
                        nodes.Add(new
                        {
                            id = mem.MemoryID,
                            label = TruncateText(mem.Summary, 20),
                            type = "normal",
                            knowledgeType = mem.KnowledgeType,
                            topic = mem.Topic,
                            importance = mem.Importance,
                            fullLabel = mem.Summary,
                            content = mem.Content
                        });
                    }
                }

                // 添加关系边
                foreach (var rel in allRelations)
                {
                    edges.Add(new
                    {
                        source = rel.SourceMemoryID,
                        target = rel.TargetMemoryID,
                        label = GetRelationTypeLabel(rel.RelationType),
                        type = rel.RelationType,
                        strength = rel.Strength
                    });
                }

                var graphData = new
                {
                    nodes = nodes,
                    edges = edges
                };

                return JsonMsg<object>.OK(graphData, "获取成功");
            }
            catch (System.Exception ex)
            {
                return JsonMsg<object>.Error($"获取失败: {ex.Message}",ErrorCode.Error);
            }
        }

        /// <summary>
        /// 查看知识详情
        /// </summary>
        public IActionResult Details(string memoryId)
        {
            if (string.IsNullOrEmpty(memoryId))
            {
                return Content("请指定知识节点ID");
            }

            var memory = LongTermMemoryBusiness.GetModel(memoryId);
            if (memory == null)
            {
                return Content("知识节点不存在");
            }

            // 获取相关关系
            var incomingRelations = KnowledgeRelationBusiness.GetByTargetId(memoryId);
            var outgoingRelations = KnowledgeRelationBusiness.GetBySourceId(memoryId);

            ViewBag.Memory = memory;
            ViewBag.IncomingRelations = incomingRelations;
            ViewBag.OutgoingRelations = outgoingRelations;

            return View();
        }

        /// <summary>
        /// 删除知识节点
        /// </summary>
        public JsonMsg<string> DeleteMemory(string memoryId)
        {
            if (string.IsNullOrEmpty(memoryId))
            {
                return JsonMsg<string>.Error("知识节点ID不能为空",ErrorCode.DataNoMemoryId);
            }

            try
            {
                // 删除相关的关系
                var relations = KnowledgeRelationBusiness.GetBySourceId(memoryId);
                foreach (var rel in relations)
                {
                    KnowledgeRelationBusiness.Delete(rel.RelationID);
                }

                var targetRelations = KnowledgeRelationBusiness.GetByTargetId(memoryId);
                foreach (var rel in targetRelations)
                {
                    KnowledgeRelationBusiness.Delete(rel.RelationID);
                }

                // 删除记忆节点
                LongTermMemoryBusiness.Delete(memoryId);

                return JsonMsg<string>.OK("删除成功");
            }
            catch (System.Exception ex)
            {
                return JsonMsg<string>.Error($"删除失败: {ex.Message}",ErrorCode.Error);
            }
        }

        /// <summary>
        /// 删除关系
        /// </summary>
        public JsonMsg<string> DeleteRelation(string relationId)
        {
            if (string.IsNullOrEmpty(relationId))
            {
                return JsonMsg<string>.Error("关系ID不能为空",ErrorCode.DataRelationIdNotFound);
            }

            try
            {
                KnowledgeRelationBusiness.Delete(relationId);
                return JsonMsg<string>.OK("删除成功");
            }
            catch (System.Exception ex)
            {
                return JsonMsg<string>.Error($"删除失败: {ex.Message}",ErrorCode.Error);
            }
        }

        /// <summary>
        /// 获取统计数据
        /// </summary>
        public JsonMsg<object> GetStatistics(string appId = "")
        {
            try
            {
                // 使用数据库层面的聚合统计（性能优化）
                var memoryStats = LongTermMemoryBusiness.GetStatistics(appId);
                var relationStats = KnowledgeRelationBusiness.GetStatistics(appId);

                // 获取总计数
                int totalMemories = 0;
                int totalRelations = 0;

                if (memoryStats.Rows.Count > 0)
                {
                    totalMemories = Convert.ToInt32(memoryStats.Rows[0]["total_count"]);
                }

                if (relationStats.Rows.Count > 0)
                {
                    totalRelations = Convert.ToInt32(relationStats.Rows[0]["total_count"]);
                }

                // 按知识类型统计
                var typeStatsTable = LongTermMemoryBusiness.GetCountByKnowledgeType(appId);
                var typeStats = typeStatsTable.AsEnumerable()
                    .Select(row => new { type = row["type"], count = row["count"] });

                // 按主题统计（Top 10）
                var topicStatsTable = LongTermMemoryBusiness.GetCountByTopic(appId, 10);
                var topicStats = topicStatsTable.AsEnumerable()
                    .Select(row => new { topic = row["topic"], count = row["count"] });

                // 按关系类型统计
                var relationTypeStatsTable = KnowledgeRelationBusiness.GetCountByType(appId);
                var relationTypeStats = relationTypeStatsTable.AsEnumerable()
                    .Select(row => new { type = row["type"], count = row["count"] });

                var stats = new
                {
                    totalMemories = totalMemories,
                    totalRelations = totalRelations,
                    typeStats = typeStats,
                    topicStats = topicStats,
                    relationStats = relationTypeStats
                };

                return JsonMsg<object>.OK(stats, "获取成功");
            }
            catch (System.Exception ex)
            {
                return JsonMsg<object>.Error($"获取失败: {ex.Message}", ErrorCode.Error);
            }
        }

        #region 辅助方法

        /// <summary>
        /// 截断文本
        /// </summary>
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// 获取关系类型标签
        /// </summary>
        private string GetRelationTypeLabel(string relationType)
        {
            return relationType switch
            {
                "related" => "相关",
                "prerequisite" => "前置",
                "derived" => "派生",
                "conflict" => "冲突",
                "example" => "示例",
                "category" => "分类",
                _ => "相关"
            };
        }

        #endregion
    }
}
