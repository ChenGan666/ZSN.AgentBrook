using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Attributes;
using ZSN.AI.Service.WebHelpers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.Utils.Core.Helpers;
using ZSN.Utils.Core.Extensions;
using ZSN.AI.Service.Controllers;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class DictionaryController : AdminBaseController
    {
        public IActionResult DictionaryList(int pid = 0, int pageIndex = 1, int pageSize = 10, string keyword = "")
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            pageSize = pageSize < 1 ? 1 : pageSize;
            var sql = $" Pid =" + pid;
            string ShowFieldName = "*";
            var dicList = BaseDictionaryInfoBussiness.GetListByPage(pageSize, pageIndex, sql, out int pagetotal, out int total, 1, ShowFieldName);
            ViewBag.PageIndex = pageIndex;
            ViewBag.PageTitle = "字典列表";
            ViewBag.PageSize = pageSize;
            ViewBag.PageTotal = pagetotal;
            ViewBag.Total = total;
            ViewBag.pid = pid;
            ViewBag.DicList = dicList;
            ViewBag.Pid = pid;
            return View();
        }
        public IActionResult DictionaryEdit(int did = 0, int pid = 0)
        {
            var dic = did == 0 ? new BaseDictionaryInfo
            {
                Pid = pid,
                CreateTime = DateTime.Now,
                Status = 0
            } :
            BaseDictionaryInfoBussiness.GetModel(did);
            ViewBag.Dic = dic;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> DictionaryStatus(int did, bool status)
        {
            var dictionary = BaseDictionaryInfoBussiness.GetModel(did);
            dictionary.Status = status ? 0 : 1;
            dictionary.UpdateTime = DateTime.Now;
            BaseDictionaryInfoBussiness.Update(dictionary);
            UpdateDic();
            return JsonMsg<string>.OK("更新成功");
        }

        [HttpPost]
        public JsonMsg<string> DictionarySort(int did, int sort)
        {
            BaseDictionaryInfo dictionary = BaseDictionaryInfoBussiness.GetModel(did);
            dictionary.Sort = sort;
            dictionary.UpdateTime = DateTime.Now;
            BaseDictionaryInfoBussiness.Update(dictionary);
            UpdateDic();
            return JsonMsg<string>.OK("更新成功");
        }

        [HttpPost]
        public JsonMsg<string> DictionarySave(BaseDictionaryInfo dic)
        {
            dic.UpdateTime = DateTime.Now;
            if (dic.DicId == 0)
            {
                dic.CreateTime = DateTime.Now;
                dic.UpdateTime = DateTime.Now;
                if (dic.Pid != 0)
                {
                    var p = BaseDictionaryInfoBussiness.GetModel(dic.Pid??0);
                    dic.Cid = (p == null || p.Pid == 0) ? 0 : p.Cid;
                }
                BaseDictionaryInfoBussiness.Add(dic);
            }
            else
            {
                BaseDictionaryInfoBussiness.Update(dic);
            }

            UpdateDic();
            return JsonMsg<string>.OK("保存成功");
        }

        [HttpPost]
        public JsonMsg<string> DictionaryDel(int id)
        {
            var d = DictionarySessionHelper.DictionaryList.FirstOrDefault(t => t.DicId == id);
            DelDic(d);
            UpdateDic();
            return JsonMsg<string>.OK("删除成功");
        }

        private void DelDic(BaseDictionaryInfo d)
        {
            if (d.ChildrenList.Any())
            {
                foreach (var cd in d.ChildrenList)
                {
                    DelDic(cd);
                }
            }
            BaseDictionaryInfoBussiness.Delete(d.DicId);
        }

        private void UpdateDic()
        {
            DictionarySessionHelper.InitDictionaryList();
        }
    }
}