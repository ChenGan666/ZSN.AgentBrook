﻿using ZSN.AI.BLL;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

using ZSN.Utils.Core.Helpers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using Newtonsoft.Json;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class PluginsController: AdminBaseController
    {
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = PluginsInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.PluginsList = lst;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> PluginsStatus(int mid, bool status)
        {
            var Plugins = PluginsInfoBussiness.GetModel(mid);
            Plugins.SystemStatus = status ? 0 : 1;

            PluginsInfoBussiness.Update(Plugins);
            return JsonMsg<string>.OK("更新成功");
        }

        public IActionResult Edit(int mid = 0)
        {
            var Plugins = mid == 0 ? new PluginsInfo() : PluginsInfoBussiness.GetModel(mid);
            var ModelList = LargeModelInfoBussiness.GetList(" SystemStatus = 0 ");

            ViewBag.PluginsTypeList = BaseDictionaryInfoBussiness.GetChildList("应用类型");
            ViewBag.SessionModelList = ModelList.FindAll(x => x.TypeCode == AIModelType.Chat);
            ViewBag.VectorModelList = ModelList.FindAll(x => x.TypeCode == AIModelType.Embedding);

            ViewBag.Plugins = Plugins;
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");

            ViewBag.KnowledgeBases = KnowledgeBaseInfoBussiness.GetList(" SystemStatus = 0 ");
            return View();
        }
        [HttpPost]
        public JsonMsg<string> PluginsSave(PluginsInfo Plugins, string KnowledgeBases)
        {

            if (Plugins.PluginsID <= 0)
            {
                Plugins.CreateTime = DateTime.Now;
                PluginsInfoBussiness.Add(Plugins);
            }
            else
            {

                PluginsInfoBussiness.Update(Plugins);
            }
            return JsonMsg<string>.OK("保存成功");
        }

        public JsonMsg<string> PluginsDel(string mid)
        {
            PluginsInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功");
        }
    }
}
