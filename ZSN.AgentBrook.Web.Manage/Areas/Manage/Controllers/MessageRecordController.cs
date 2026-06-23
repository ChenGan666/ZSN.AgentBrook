using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Controllers;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [Area("Manage")]
    [AdminAttributes]
    public class MessageRecordController : AdminBaseController
    {
        public IActionResult SendList(int index = 1, int size = 10, string keyword = "", string sessionID = "", string channelID = "")
        {
            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(keyword))
                conditions.Add($"(Content LIKE '%{keyword}%' OR TargetUser LIKE '%{keyword}%')");
            if (!string.IsNullOrEmpty(sessionID))
                conditions.Add($"SessionID='{sessionID}'");
            if (!string.IsNullOrEmpty(channelID))
                conditions.Add($"ChannelID='{channelID}'");

            var strWhere = string.Join(" AND ", conditions);
            var lst = MessageSendRecordBussiness.GetListByPage(size, index, strWhere, out int pagetotal, out int total);
            ViewBag.RecordList = lst;
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.Keyword = keyword;
            ViewBag.SessionID = sessionID;
            ViewBag.ChannelID = channelID;
            return View();
        }

        public IActionResult ReceiveList(int index = 1, int size = 10, string keyword = "", string channelID = "")
        {
            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(keyword))
                conditions.Add($"(Content LIKE '%{keyword}%' OR FromUser LIKE '%{keyword}%' OR FromUserName LIKE '%{keyword}%')");
            if (!string.IsNullOrEmpty(channelID))
                conditions.Add($"ChannelID='{channelID}'");

            var strWhere = string.Join(" AND ", conditions);
            var lst = MessageReceiveRecordBussiness.GetListByPage(size, index, strWhere, out int pagetotal, out int total);
            ViewBag.RecordList = lst;
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.Keyword = keyword;
            ViewBag.ChannelID = channelID;
            return View();
        }

        public IActionResult SendDetail(string id)
        {
            var model = MessageSendRecordBussiness.GetModel(id);
            return Json(model);
        }

        public IActionResult ReceiveDetail(string id)
        {
            var list = MessageReceiveRecordBussiness.GetList($"RecordID='{id}'");
            var model = list?.FirstOrDefault();
            return Json(model);
        }
    }
}
