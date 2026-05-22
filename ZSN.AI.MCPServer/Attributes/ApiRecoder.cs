using System;
using System.Collections.Generic;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

using static ZSN.AI.Service.Controllers.CommonApiBaseController;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Entity;
using ZSN.AI.MCPServer.Controllers;

namespace ZSN.AI.MCPServer.Attributes
{
    public class ApiRecoder : ActionFilterAttribute, IExceptionFilter
    {
        public int MarkId = 46;
        public int ErrorId = 1;
        public bool IsGetFile = false;//是否记录读取文件事件
        
        // 保存Action参数，用于后续日志记录
        private IDictionary<string, object> _actionArguments;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 在执行前保存参数
            _actionArguments = context.ActionArguments;
            
            // 调试日志
            System.Diagnostics.Debug.WriteLine($"[ApiRecoder] OnActionExecuting - Path: {context.HttpContext.Request.Path}, Arguments Count: {_actionArguments?.Count ?? 0}");
            
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // 调试日志
            System.Diagnostics.Debug.WriteLine($"[ApiRecoder] OnActionExecuted - Path: {context.HttpContext.Request.Path}");
            
            IsGetFile = context.HttpContext.Request.Path.Value.IndexOf("api/File/Get") > -1;
            
            // 调试日志
            System.Diagnostics.Debug.WriteLine($"[ApiRecoder] IsGetFile: {IsGetFile}");
            
            if (!IsGetFile)
            {
                var r = context.HttpContext.Request;
                
                // 调试日志 - 在调用GetRequestBodyParams之前
                System.Diagnostics.Debug.WriteLine($"[ApiRecoder] 准备调用GetRequestBodyParams...");
                
                var bodyParams = this.GetRequestBodyParams(context);
                
                // 调试日志 - 查看返回结果
                System.Diagnostics.Debug.WriteLine($"[ApiRecoder] GetRequestBodyParams返回: {JsonConvert.SerializeObject(bodyParams)}");
                
                var log = new
                {
                    url = r.Path.Value,
                    paramDic = GetRequestParams(context),
                    BodyParams = bodyParams,
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

        public object GetRequestBodyParams(ActionExecutedContext actionContext) 
        {
            // 调试日志
            System.Diagnostics.Debug.WriteLine($"[ApiRecoder] GetRequestBodyParams 被调用");
            System.Diagnostics.Debug.WriteLine($"[ApiRecoder] _actionArguments is null: {_actionArguments == null}, Count: {_actionArguments?.Count ?? 0}");
            
            try
            {
                var result = new Dictionary<string, object>();
                
                // 1. 优先使用保存的Action参数（从OnActionExecuting）
                if (_actionArguments != null && _actionArguments.Count > 0)
                {
                    foreach (var arg in _actionArguments)
                    {
                        // 排除IFormFile类型（文件太大不适合记录）
                        if (arg.Value is IFormFile file)
                        {
                            result[arg.Key] = new 
                            { 
                                Type = "IFormFile",
                                FileName = file.FileName,
                                Length = file.Length,
                                ContentType = file.ContentType
                            };
                        }
                        else
                        {
                            result[arg.Key] = arg.Value;
                        }
                    }
                    return result;
                }

                // 2. 如果没有参数，尝试读取原始Body（向后兼容）
                string bodyParams = (actionContext.Controller as ApiBaseController)?.BodyParams;
                if (!string.IsNullOrEmpty(bodyParams))
                {
                    try
                    {
                        return Newtonsoft.Json.JsonConvert.DeserializeObject(bodyParams);
                    }
                    catch
                    {
                        return bodyParams;
                    }
                }

                return null;
            }
            catch (System.Exception ex)
            {
                return new { Error = "获取请求参数失败", Message = ex.Message };
            }
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
