using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Attributes;
using Senparc.CO2NET.Extensions;
using ZSN.AgentBrook.Web.Manage.Attributes;
using LogLevel = ZSN.AI.Entity.LogLevel;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class SystemLogController: AdminBaseController
    {
        public IActionResult LogLevelList(int pageIndex = 1, int pageSize = 10, string keyword = "")
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            pageSize = pageSize < 1 ? 1 : pageSize;
            string sql = "";
            keyword = keyword != null ? keyword.Trim() : "";
            if (keyword.Trim() != "")
            {
                sql = $" LevelName like '%{keyword}%' ";
            }
            string ShowFieldName = "*";
            var list = LogLevelBusiness.GetListByPage(pageSize, pageIndex, sql, out int pagetotal, out int total, 1, ShowFieldName);
            ViewBag.PageTitle = "日志等级";
            ViewBag.PageIndex = pageIndex;
            ViewBag.PageSize = pageSize;
            ViewBag.PageTotal = pagetotal;
            ViewBag.Total = total;
            ViewBag.keyword = keyword;
            ViewBag.LogLevelList = list;
        
            return View();
        }
        public IActionResult LogLevelEdit(int Id = 0)
        {
            var Loglv = Id == 0 ? new LogLevel
            {
                Status = true
            } :
            LogLevelBusiness.GetModel(Id);
            ViewBag.Loglv = Loglv;
            return View();
        }
        [HttpPost]
        public IActionResult LogLevelSave(LogLevel Loglv)
        {
            if (Loglv.Id == 0)
            {
                Loglv.CreateTime = DateTime.Now;
                LogLevelBusiness.Add(Loglv);
            }
            else
            {
                LogLevelBusiness.Update(Loglv);
            }

            return Json(new { status = true });
        }
        [HttpPost]
        public IActionResult LogLevelDel(int Id)
        {
            LogLevel Loglv = LogLevelBusiness.GetModel(Id);
            if (Loglv != null)
                LogLevelBusiness.Delete(Id);
            return Json(new { status = true });
        }
        [HttpPost]
        public IActionResult LogLevelStatus(int Id, bool status)
        {
            LogLevel Loglv = LogLevelBusiness.GetModel(Id);
            Loglv.Status = status;
            LogLevelBusiness.Update(Loglv);

            return Json(new { status = true });
        }

        public IActionResult LogRecordList(int pageIndex = 1, int pageSize = 30, string keyword = "", string MarkIds = "")
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            pageSize = pageSize < 1 ? 1 : pageSize;
            string sql = " 1 = 1";
            if (!MarkIds.IsNullOrEmpty())
            {
                sql += $" and MarkId in ({MarkIds})";
            }
            keyword = keyword != null ? keyword.Trim() : "";
            if (keyword.Trim() != "")
            {
                sql += $" and LogDetail like '%{keyword}%' ";
            }
            string ShowFieldName = "*";
            var list = LogRecordBusiness.GetListByPage(pageSize, pageIndex, sql, out int pagetotal, out int total, 1, ShowFieldName);

            ViewBag.PageTitle = "日志信息";
            ViewBag.PageIndex = pageIndex;
            ViewBag.PageSize = pageSize;
            ViewBag.PageTotal = pagetotal;
            ViewBag.Total = total;
            ViewBag.keyword = keyword;
            ViewBag.LogRecordList = list;
            ViewBag.LogClassList = LogMarkClassBusiness.GetList();
            ViewBag.LogMarkList = LogMarkBusiness.GetList("Status=1");
            ViewBag.LogRecordList = list;
            ViewBag.MarkIds = MarkIds;

            return View();
        }

        public IActionResult LogMarkList(int pageIndex = 1, int pageSize = 10, string keyword = "", int LevelId = 0)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            pageSize = pageSize < 1 ? 1 : pageSize;
            string sql = " 1=1 ";
            keyword = keyword != null ? keyword.Trim() : "";
            if (keyword.Trim() != "")
            {
                sql += $" and MarkName like '%{keyword}%' ";
            }
            if (LevelId > 0)
            {
                sql += $" and LevelId = {LevelId} ";
            }
            string ShowFieldName = "*";
            var list = LogMarkBusiness.GetListByPage(pageSize, pageIndex, sql, out int pagetotal, out int total, 1, ShowFieldName);
            ViewBag.PageTitle = "日志类型";
            ViewBag.PageIndex = pageIndex;
            ViewBag.PageSize = pageSize;
            ViewBag.PageTotal = pagetotal;
            ViewBag.Total = total;
            ViewBag.keyword = keyword;
            ViewBag.LogTypeList = list;
            ViewBag.LogLevelList = LogLevelBusiness.GetList("1=1");
            ViewBag.LogMarkClassList = LogMarkClassBusiness.GetList();
            ViewBag.LevelId = LevelId;
            return View();
        }

        public IActionResult LogMarkEdit(int Id = 0,int ClassId = 0)
        {
            var LogT = Id == 0 ? new LogMark
            {
                ClassId = ClassId,
                Status = true
            } :
            LogMarkBusiness.GetModel(Id);
            ViewBag.LogT = LogT;
            ViewBag.LogLevelList = LogLevelBusiness.GetList("1=1");
            ViewBag.LogMarkClassList = LogMarkClassBusiness.GetList();
            return View();
        }
        [HttpPost]
        public IActionResult LogMarkSave(LogMark LogT)
        {
            if (LogT != null)
            {
                if (LogT.Id == 0)
                {
                    LogT.CreateTime = DateTime.Now;
                    LogMarkBusiness.Add(LogT);
                }
                else
                {
                    LogMarkBusiness.Update(LogT);
                }
                return Json(new { status = true });
            }
            else
            {
                return Json(new { status = false });
            }
            
        }
        [HttpPost]
        public IActionResult LogMarkDel(int Id)
        {
            LogMark LogT = LogMarkBusiness.GetModel(Id);
            if (LogT != null)
                LogMarkBusiness.Delete(Id);
            return Json(new { status = true });
        }
        [HttpPost]
        public IActionResult LogMarkStatus(int Id, bool status)
        {
            LogMark LogT = LogMarkBusiness.GetModel(Id);
            LogT.Status = status;
            LogMarkBusiness.Update(LogT);

            return Json(new { status = true });
        }
        public IActionResult LogMarkClassList(int pageIndex = 1, int pageSize = 10, string keyword = "", int ParentId = 0)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            pageSize = pageSize < 1 ? 1 : pageSize;
            string sql = " 1 = 1 ";
            if (!keyword.IsNullOrEmpty() && keyword.Trim() != "")
            {
                sql += $" AND MarkName like '%{keyword}%' ";
            }
            if (ParentId > 0)
            {
                sql += $" AND  ParentId = {ParentId} ";
            }
            string ShowFieldName = "*";
            var list = LogMarkClassBusiness.GetListByPage(pageSize, pageIndex, sql, out int pagetotal, out int total, 1, ShowFieldName);
            ViewBag.PageTitle = "日志分类";
            ViewBag.PageIndex = pageIndex;
            ViewBag.PageSize = pageSize;
            ViewBag.PageTotal = pagetotal;
            ViewBag.Total = total;
            ViewBag.keyword = keyword;
            ViewBag.LogMarkClassList = list;
            ViewBag.ParentId = ParentId;
            return View();
        }
        public IActionResult LogMarkClassEdit(int Id = 0,int ParentId=0,int RootId=0)
        {
            var LogC = Id == 0 ? new LogMarkClass
            {
                RootId= RootId,
                ParentId = ParentId,
                CreateTime = DateTime.Now
            } :
            LogMarkClassBusiness.GetModel(Id);
            ViewBag.LogC = LogC;
            return View();
        }
        [HttpPost]
        public IActionResult LogMarkClassSave(LogMarkClass LogC)
        {
            if (LogC != null)
            {
                if (LogC.Id == 0)
                {
                    LogC.CreateTime = DateTime.Now;
                    LogMarkClassBusiness.Add(LogC);
                }
                else
                {
                    LogMarkClassBusiness.Update(LogC);
                }

                return Json(new { status = true });
            }
            else
            {
                return Json(new { status = false });
            }
        }
        [HttpPost]
        public IActionResult LogMarkClassDel(int Id)
        {
            LogMarkClass LogC = LogMarkClassBusiness.GetModel(Id);
            if (LogC != null)
                LogMarkClassBusiness.Delete(Id);
            return Json(new { status = true });
        }
    }
}
