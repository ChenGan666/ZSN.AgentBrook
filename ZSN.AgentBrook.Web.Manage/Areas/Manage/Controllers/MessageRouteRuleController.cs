using Microsoft.AspNetCore.Mvc;
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
    public class MessageRouteRuleController : AdminBaseController
    {
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = MessageRouteRuleBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.RuleList = lst;
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            return View();
        }

        public IActionResult Edit(string mid = "")
        {
            var model = string.IsNullOrEmpty(mid)
                ? new MessageRouteRuleInfo()
                : MessageRouteRuleBussiness.GetModel(mid);

            // 渠道列表
            var channelList = ChannelConfigBussiness.GetList("Enabled=1");
            ViewBag.ChannelList = channelList;

            // APP 列表
            var appList = AppInfoBussiness.GetList("SystemStatus=2");
            ViewBag.AppList = appList;

            ViewBag.Rule = model;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> RuleSave(string RuleID, string ChannelID, string RuleName,
            string MatchType, string MatchCondition, string TargetAppID,
            string InputMapping, int SessionTimeoutMinutes,
            int EnableAutoReply, string AutoReplyContent, int Priority, int Enabled)
        {
            try
            {
                var model = string.IsNullOrEmpty(RuleID)
                    ? new MessageRouteRuleInfo { RuleID = Guid.NewGuid().ToString() }
                    : MessageRouteRuleBussiness.GetModel(RuleID);

                if (model == null)
                    return JsonMsg<string>.Error("未找到记录", ErrorCode.DataEmpty);

                model.ChannelID = string.IsNullOrEmpty(ChannelID) ? "" : ChannelID;
                model.RuleName = RuleName;
                model.MatchType = MatchType;
                model.MatchCondition = MatchCondition;
                model.TargetAppID = TargetAppID;
                model.InputMapping = InputMapping;
                model.SessionTimeoutMinutes = SessionTimeoutMinutes;
                model.EnableAutoReply = EnableAutoReply;
                model.AutoReplyContent = AutoReplyContent;
                model.Priority = Priority;
                model.Enabled = Enabled;

                if (string.IsNullOrEmpty(RuleID))
                {
                    MessageRouteRuleBussiness.Add(model);
                }
                else
                {
                    MessageRouteRuleBussiness.Update(model);
                }

                return JsonMsg<string>.OK("保存成功");
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error(ex.Message, ErrorCode.Error);
            }
        }

        [HttpPost]
        public JsonMsg<string> RuleDel(string mid)
        {
            try
            {
                if (string.IsNullOrEmpty(mid))
                    return JsonMsg<string>.Error("参数错误", ErrorCode.DataEmpty);

                var ids = mid.Split(',');
                foreach (var id in ids)
                {
                    MessageRouteRuleBussiness.Delete(id.Trim());
                }
                return JsonMsg<string>.OK("删除成功");
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error(ex.Message, ErrorCode.Error);
            }
        }
    }
}
