using System;
using ZSN.AI.Service.Attributes;
using Microsoft.AspNetCore.Mvc;
using ZSN.Utils.Core.Helpers;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Entity;

namespace ZSN.AI.Service.Controllers
{
    [SqlRecoder]
    public class CommonBaseController : Controller
    {
        public CommonBaseController()
        {
            ViewBag.Title = ConfigHelper.GetString("SystemTitle");

        }
       
        /// <summary>
        /// 生成成功Json返回
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected JsonResult BuildSuccessResult(Object data, ResultType type = ResultType.Object)
        {
            return Json(new { status = true, success = true, data = data, type = type.ToString(), errorCode = 0, errorDetail = "" });
        }


        /// <summary>
        /// 生成失败Json返回
        /// </summary>
        /// <param name="code"></param>
        /// <param name="detail"></param>
        /// <returns></returns>
        protected JsonResult BuildFailResult(int code = -1, string detail = "")
        {
            return Json(new
            {
                status = false,
                success = false,
                errorCode = code,
                data = new { },
                type = ResultType.Object.ToString(),
                errorDetail = detail
            });
        }
        protected JsonResult BuildFailResult(ErrorCode errorCode)
        {
            return BuildFailResult((int)errorCode, errorCode.ToString());
        }

        protected JsonResult BuildFailResult(ErrorCode errorCode, string detail = "")
        {
            return BuildFailResult((int)errorCode, detail != "" ? detail : errorCode.ToString());
        }

        protected enum ResultType
        {
            Object,
            Array
        }
    }
}