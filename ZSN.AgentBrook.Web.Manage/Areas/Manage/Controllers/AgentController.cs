using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

using ZSN.Utils.Core.Helpers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using Newtonsoft.Json;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Entity.Model.Enum;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class AgentController: AdminBaseController
    {
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = AgentInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.AppList = lst;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> Status(string mid, bool status)
        {
            var App = AgentInfoBussiness.GetModel(mid);
            App.SystemStatus = status ? AgentStatus.Normal : AgentStatus.Disable;

            AgentInfoBussiness.Update(App);
            return JsonMsg<string>.OK("更新成功");
        }

        public IActionResult Edit(string mid = "")
        {
            var Agent = mid == "" ? new AgentInfo() : AgentInfoBussiness.GetModel(mid);
            var ModelList = LargeModelInfoBussiness.GetList(" SystemStatus = 0 ");

            ViewBag.AgentTypeList = BaseDictionaryInfoBussiness.GetChildList("智能体类型");
            ViewBag.SessionModelList = ModelList.FindAll(x => x.TypeCode == AIModelType.Chat);
            //ViewBag.VectorModelList = ModelList.FindAll(x => x.TypeCode == 5);

            ViewBag.Agent = Agent;
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");

            //ViewBag.KnowledgeBases = KnowledgeBaseInfoBussiness.GetList(" SystemStatus = 0 ");
            return View();
        }

        [HttpPost]
        public JsonMsg<string> Save(AgentInfo Agent, string KnowledgeBases)
        {
            if (!KnowledgeBases.IsNullOrEmpty())
            {
                //Agent.KnowledgeBases = JsonConvert.DeserializeObject<List<AgentKnowledgeBaseInfo>>(KnowledgeBases);
            }

            if (Agent.AgentID.IsNullOrEmpty())
            {
                Agent.AgentID = hashEncrypt.MD5System(Guid.NewGuid().ToString());
                Agent.CreateTime = DateTime.Now;
                AgentInfoBussiness.Add(Agent);
            }
            else
            {

                AgentInfoBussiness.Update(Agent);
            }
            return JsonMsg<string>.OK("保存成功");
        }

        public JsonMsg<string> Del(string mid)
        {
            AgentInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功");
        }
    }
}
