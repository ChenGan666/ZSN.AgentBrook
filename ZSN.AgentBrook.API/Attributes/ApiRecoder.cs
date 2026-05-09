using System;
using System.Collections.Generic;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using ZSN.AgentBrook.API.Controllers;
using static ZSN.AI.Service.Controllers.CommonApiBaseController;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.API.Attributes
{
    public class ApiRecoder : ActionFilterAttribute, IExceptionFilter
    {
        public int MarkId = 305;
        public int ErrorId = 1;
        public bool IsGetFile = false;//是否记录读取文件事件

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            IsGetFile = context.HttpContext.Request.Path.Value.IndexOf("api/File/Get") > -1;
            if (!IsGetFile)
            {
                var r = context.HttpContext.Request;
                var log = new
                {
                    url = r.Path.Value,
                    paramDic = GetRequestParams(context),
                    BodyParams = GetRequestBodyParams(context),
                    response = GetResponseValues(context)
                };

                DefaultLogService.AddOperationLog(MarkId, JsonConvert.SerializeObject(log, Formatting.Indented));
            }
            base.OnActionExecuted(context);
        }

        public void OnException(ExceptionContext context)
        {
            DefaultLogService.AddOperationLog(ErrorId, context.Exception);
            context.Result = GetErrorResult(ErrorCode.ServerError);
        }

        private static JsonResult GetErrorResult(ErrorCode errorCode)
        {
            return new JsonResult(new
            {
                success = false,
                status = false,
                errorCode = (int)errorCode,
                errorDetail = errorCode.ToString()  // DictionarySessionHelper.GetDicByName(code.ToString()).DicValue
            });
        }

        public static object GetRequestParams(ActionExecutedContext aecontext)
        {
            var context = aecontext.HttpContext;
            var pd = new Dictionary<string, object>();

            if (context.Request.Method == "POST")
            {
                var pv = GetPostValues(context);
                if (pv.Count > 0)
                {
                    pd.Add("POST", pv);
                }
            }

            if (context.Request.QueryString.HasValue)
            {
                var q = context.Request.QueryString.Value;
                if (!q.IsNullOrEmpty() && q != "?")
                {
                    pd.Add("GET", q);
                }
            }

            return pd;
        }

        public static object GetRequestBodyParams(ActionExecutedContext actionContext) {
            string obj = (actionContext.Controller as ApiBaseController)?.BodyParams;
            return obj;
        }

        /// <summary>
        /// 读取request 的提交内容
        /// </summary>
        /// <param name="HttpContext"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetPostValues(HttpContext context)
        {
            var data = new Dictionary<string, string>();
            try
            {
                if (context.Request.Form != null)
                {
                    var f1 = context.Request.Form;
                    foreach (var formKey in f1.Keys)
                    {
                        if (data.ContainsKey(formKey))
                        {
                            data[formKey] = f1[formKey];
                        }
                        else
                        {
                            data.Add(formKey, f1[formKey]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //ignored
            }
            var f2 = context.Request.Query;
            foreach (var formKey in f2.Keys)
            {
                if (data.ContainsKey(formKey))
                {
                    data[formKey] = f2[formKey];
                }
                else
                {
                    data.Add(formKey, f2[formKey]);
                }
            }
            return data;
        }

        /// <summary>
        /// 读取action返回的result
        /// </summary>
        /// <param name="actionExecutedContext"></param>
        /// <returns></returns>
        public object GetResponseValues(ActionExecutedContext actionExecutedContext)
        {
            return actionExecutedContext.Result;
        }

    }
}
