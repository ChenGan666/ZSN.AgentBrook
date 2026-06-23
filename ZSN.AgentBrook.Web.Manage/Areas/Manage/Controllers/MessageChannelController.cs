using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Controllers;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [Area("Manage")]
    [AdminAttributes]
    public class MessageChannelController : AdminBaseController
    {
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = ChannelConfigBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.ChannelList = lst;
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            return View();
        }

        public IActionResult Edit(string mid = "")
        {
            var model = string.IsNullOrEmpty(mid)
                ? new ChannelConfigInfo()
                : ChannelConfigBussiness.GetModel(mid);

            // APP 列表（用于接收流向选择）
            var appList = AppInfoBussiness.GetList("SystemStatus=2");
            ViewBag.AppList = appList;

            ViewBag.Channel = model;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> ChannelSave(string ChannelID, string ChannelName, int ProviderType,
            string ConfigJson, int FlowDirection, string TargetAppID, int SessionTimeoutMinutes, int Enabled)
        {
            try
            {
                var model = string.IsNullOrEmpty(ChannelID)
                    ? new ChannelConfigInfo { ChannelID = Guid.NewGuid().ToString() }
                    : ChannelConfigBussiness.GetModel(ChannelID);

                if (model == null)
                    return JsonMsg<string>.Error("未找到记录", ErrorCode.ServerError);

                model.ChannelName = ChannelName;
                model.ProviderType = ProviderType;
                model.ConfigJson = ConfigJson;
                model.FlowDirection = FlowDirection;
                model.TargetAppID = TargetAppID;
                model.SessionTimeoutMinutes = SessionTimeoutMinutes;
                model.Enabled = Enabled;

                if (string.IsNullOrEmpty(ChannelID))
                {
                    ChannelConfigBussiness.Add(model);
                }
                else
                {
                    ChannelConfigBussiness.Update(model);
                }

                return JsonMsg<string>.OK("保存成功");
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error(ex.Message, ErrorCode.ServerError);
            }
        }

        [HttpPost]
        public JsonMsg<string> ChannelDel(string mid)
        {
            try
            {
                if (string.IsNullOrEmpty(mid))
                    return JsonMsg<string>.Error("参数错误", ErrorCode.DataEmpty);

                var ids = mid.Split(',');
                foreach (var id in ids)
                {
                    ChannelConfigBussiness.Delete(id.Trim());
                }
                return JsonMsg<string>.OK("删除成功");
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error(ex.Message, ErrorCode.ServerError);
            }
        }
    }
}
