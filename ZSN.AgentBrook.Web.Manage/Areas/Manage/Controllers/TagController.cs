using Microsoft.AspNetCore.Mvc;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity.Model.Enum;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Helpers;
using Newtonsoft.Json;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class TagController: AdminBaseController
    {
        public IActionResult index(int TagClassID,string Tag,int index = 1, int size = 10)
        {
            string _sql = " 1=1 ";
            if (TagClassID > 0)
            {
                _sql += " and TagClassID = " + TagClassID;
            }
            if (!string.IsNullOrEmpty(Tag))
            {
                _sql += " and Tag like '%" + Tag + "%'";
            }

            var lst = KnowledgeBaseTagInfoBussiness.GetListByPage(size, index, _sql, out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.List = lst;
            ViewBag.TagClassID = TagClassID;
            ViewBag.Tag = Tag;
            return View();
        }
        public IActionResult Edit(int mid = 0)
        {
            var Tag = mid == 0 ? new KnowledgeBaseTagInfo() : KnowledgeBaseTagInfoBussiness.GetModel(mid);

            ViewBag.AgentTypeList = BaseDictionaryInfoBussiness.GetAllChildList("标签分类", false, true);

            var tree = BaseDictionaryInfoBussiness.BuildTree(ViewBag.AgentTypeList, 24);

            ViewBag.TagClass = tree;
            ViewBag.Tag = Tag;

            return View();
        }

        [HttpPost]
        public JsonMsg<string> Save(int KnowledgeBaseTagID,int TagClassID,string TagClassName,string Tag,int TCount,string TSummary)
        {
            KnowledgeBaseTagInfo tag = new KnowledgeBaseTagInfo();
            tag.KnowledgeBaseTagID = KnowledgeBaseTagID;
            tag.TagClassID = TagClassID;
            tag.TagClassName = TagClassName;
            tag.Tag = Tag;
            tag.TAppendTime = DateTime.Now;
            tag.TCount = TCount;
            tag.TSummary = TSummary;

            if (tag.KnowledgeBaseTagID>0)
            {
                string _tag = KnowledgeBaseTagInfoBussiness.GetModel(tag.KnowledgeBaseTagID).Tag;
                if (_tag.Equals(tag.Tag))
                {
                    KnowledgeBaseTagInfoBussiness.Update(tag);
                }
                else
                {
                    if (KnowledgeBaseTagInfoBussiness.GetModel(tag.Tag) == null)
                    {
                        KnowledgeBaseTagInfoBussiness.Update(tag);
                    }
                    else
                    {
                        return JsonMsg<string>.Error("标签已存在",ErrorCode.DataAlreadyExists);
                    }
                }
                
            }
            else
            {
                if (KnowledgeBaseTagInfoBussiness.GetModel(tag.Tag) == null)
                {
                    tag.KnowledgeBaseTagID = KnowledgeBaseTagInfoBussiness.Add(tag);
                }
                else
                {
                    return JsonMsg<string>.Error("标签已存在", ErrorCode.DataAlreadyExists);
                }
                   
            }
            return JsonMsg<string>.OK("保存成功");
        }

        public JsonMsg<string> Del(string mid)
        {
            KnowledgeBaseTagInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功");
        }
    }
}
