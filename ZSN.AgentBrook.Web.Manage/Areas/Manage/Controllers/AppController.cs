﻿using ZSN.AI.BLL;
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
    public class AppController:AdminBaseController
    {
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = AppInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.AppList = lst;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> AppStatus(string mid, bool status)
        {
            var App = AppInfoBussiness.GetModel(mid);
            App.SystemStatus = status ? 2 : 1;

            AppInfoBussiness.Update(App);
            return JsonMsg<string>.OK("更新成功");
        }

        public IActionResult Edit(string mid = "")
        {
            var App = mid == "" ? new AppInfo() : AppInfoBussiness.GetModel(mid);
            var ModelList = LargeModelInfoBussiness.GetList(" SystemStatus = 0 ");

            if (!mid.IsNullOrEmpty()) {
                App.SecretKey = ApisettingsInfoBussiness.GetModelByAppID(mid)?.SecretKey;
            }

            ViewBag.AppTypeList = BaseDictionaryInfoBussiness.GetChildList("应用类型");
            ViewBag.SessionModelList = ModelList.FindAll(x => x.TypeCode == AIModelType.Chat);
            ViewBag.VectorModelList = ModelList.FindAll(x => x.TypeCode == AIModelType.Embedding);

            ViewBag.App = App;
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");

            ViewBag.KnowledgeBases = KnowledgeBaseInfoBussiness.GetList(" SystemStatus = 0 ");
            return View();
        }
        [HttpPost]
        public JsonMsg<string> AppSave(AppInfo App)
        {
            App.SecretKey = App.SecretKey.IsNullOrEmpty()? hashEncrypt.MD5System(Guid.NewGuid().ToString()): App.SecretKey;

            if (App.AppID.IsNullOrEmpty())
            { 
                App.AppID = Guid.NewGuid().ToString();
                
                App.CreateTime = DateTime.Now;
                AppInfoBussiness.Add(App);
            }
            else
            {

                AppInfoBussiness.Update(App);
            }
            return JsonMsg<string>.OK("保存成功");
        }

        public JsonMsg<string> AppDel(string mid)
        {
            AppInfoBussiness.DeleteList(mid);

            ApisettingsInfoBussiness.DeleteListByAppID(mid);

            return JsonMsg<string>.OK("删除成功");
        }
    }
}
