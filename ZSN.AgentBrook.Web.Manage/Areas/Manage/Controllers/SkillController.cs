using Microsoft.AspNetCore.Mvc;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class SkillController : AdminBaseController
    {
        public IActionResult Index(string sName, int? systemStatus, int index = 1, int size = 10)
        {
            string where = " 1=1 ";
            if (!string.IsNullOrEmpty(sName))
            {
                where += " and SName like '%" + sName.Replace("'","''") + "%'";
            }
            if (systemStatus.HasValue)
            {
                where += " and SystemStatus = " + (systemStatus.Value == 0 ? "0" : "1");
            }

            var lst = SkillInfoBussiness.GetListByPage(size, index, where, out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.List = lst;
            ViewBag.SName = sName;
            ViewBag.SystemStatus = systemStatus;
            return View();
        }

        public IActionResult Edit(string mid)
        {
            var model = mid.IsNullOrEmpty() ? new SkillInfo() : SkillInfoBussiness.GetModel(mid);
            if (mid.IsNullOrEmpty())
            {
                model.CreateTime = DateTime.Now;
                model.UpdateTime = DateTime.Now;
            }
            ViewBag.Skill = model;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> Status(string mid, bool status)
        {
            var model = SkillInfoBussiness.GetModel(mid);
            if (model == null)
            {
                return JsonMsg<string>.Error("记录不存在", ErrorCode.DataNotExists);
            }
            model.SystemStatus = status ? 0 : 1; // 0: 正常, 1: 屏蔽
            model.UpdateTime = DateTime.Now;
            SkillInfoBussiness.Update(model);
            return JsonMsg<string>.OK("更新成功");
        }

        [HttpPost]
        public JsonMsg<string> Save(SkillInfo skill)
        {
            if (skill == null)
            {
                return JsonMsg<string>.Error("参数无效", ErrorCode.ParamsError);
            }

            // SName 唯一性校验
            string safeName = (skill.SName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(safeName))
            {
                return JsonMsg<string>.Error("名称不能为空", ErrorCode.ParamsError);
            }

            if (skill.SkillID.IsNullOrEmpty())
            {
                var exists = SkillInfoBussiness.GetList(" SName='" + safeName.Replace("'","''") + "' ").FirstOrDefault();
                if (exists != null)
                {
                    return JsonMsg<string>.Error("名称已存在", ErrorCode.DataAlreadyExists);
                }

                skill.SkillID = Guid.NewGuid().ToString();
                skill.SName = safeName;
                skill.CreateTime = DateTime.Now;
                skill.UpdateTime = DateTime.Now;
                SkillInfoBussiness.Add(skill);
            }
            else
            {
                var old = SkillInfoBussiness.GetModel(skill.SkillID);
                if (old == null)
                {
                    return JsonMsg<string>.Error("记录不存在", ErrorCode.DataNotExists);
                }
                if (!string.Equals(old.SName, safeName, StringComparison.Ordinal))
                {
                    var exists = SkillInfoBussiness.GetList(" SName='" + safeName.Replace("'","''") + "' ").FirstOrDefault();
                    if (exists != null)
                    {
                        return JsonMsg<string>.Error("名称已存在", ErrorCode.DataAlreadyExists);
                    }
                }
                old.SName = safeName;
                old.SDescription = skill.SDescription;
                old.SkillDirectory = skill.SkillDirectory;
                old.SystemStatus = skill.SystemStatus;
                old.UpdateTime = DateTime.Now;
                SkillInfoBussiness.Update(old);
            }
            return JsonMsg<string>.OK("保存成功");
        }

        [HttpPost]
        public JsonMsg<string> Del(string mid)
        {
            SkillInfoBussiness.DeleteList(mid);
            return JsonMsg<string>.OK("删除成功");
        }
    }
}
