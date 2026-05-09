using System;
using System.Collections.Generic;
using ZSN.AI.Service.WebHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using ZSN.Utils.Core.Helpers;
using ZSN.AgentBrook.API.Controllers;
using System.Linq;
using ZSN.AI.Service.Helpers;
using static ZSN.AI.Service.Controllers.CommonApiBaseController;
using ZSN.Utils.Core;
using Newtonsoft.Json.Linq;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AgentBrook.API.Attributes
{
    /// <summary>
    /// API过滤器
    /// </summary>
    public class APIChecker: ActionFilterAttribute, IExceptionFilter
    {

        public int MarkId = 304;
        /// <summary>
        /// 是否进行Token认证
        /// </summary>
        public bool Token = true;
        /// <summary>
        /// 是否进行签名校验
        /// </summary>
        public bool Sign = ConfigHelper.GetBool("CheckSign",true);
        /// <summary>
        /// 是否进行时间戳校验
        /// </summary>
        public bool Timestamp = ConfigHelper.GetBool("CheckTimestamp", true);
        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            bool token_checked = false;
            if (Token)
            {
                token_checked = CheckToken(context);
            }
            else
            {
                token_checked = true;
            }
            if (token_checked)
            {
                if (Sign)
                {
                    var rst = CheckSign(context);
                    if (!rst)
                    {
                        context.Result = GetErrorResult(ErrorCode.SignError);
                    }
                }
                if (Timestamp)
                {
                    var rst = CheckTimestamp(context);
                    if (!rst)
                    {
                        context.Result = GetErrorResult(ErrorCode.TimestampError);
                    }
                }
            }
            else
            {
                context.Result = GetErrorResult(ErrorCode.TokenCheckError);
            }
            base.OnActionExecuting(context);
        }

        public void OnException(ExceptionContext context)
        {
            DefaultLogService.AddOperationLog(5, context.Exception, "", "APIChecker内部错误");
            NLogHelper.WriteException(ErrorCode.Error.ToString(), context.Exception);
            context.Result = GetErrorResult(ErrorCode.Error);
        }

        public static JsonResult GetErrorResult(ErrorCode errorCode)
        {
            return GetErrorResult((int)errorCode, errorCode.ToString());
        }
        public static JsonResult GetErrorResult(int code, string detail)
        {
            return new JsonResult(new
            {
                success = false,
                errorCode = code,
                errorDesc = detail
            });
        }

        public bool CheckToken(ActionExecutingContext actionContext)
        {
            try
            {
                string _Token = (actionContext.Controller as ApiBaseController)?.Token;

                if (string.IsNullOrEmpty(_Token))
                {
                    return false;
                }
                else
                {
                    string AppID = "";
                    string timestamp = "";
                    string now_timestamp = TimeHelper.GetCurrentTimestamp();

                    GetAPPIDIdByToken(_Token,out AppID,out timestamp);
                    
                    if (AppID != ConfigHelper.GetString("AppID"))
                    {
                        if (TimeHelper.SubtractTimestam(now_timestamp, timestamp) <= ConfigHelper.GetInt("AccessTokenTimeOut"))//Token有效时间
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                DefaultLogService.AddOperationLog(MarkId, e, "", ErrorCode.TokenCheckError.ToString());
                NLogHelper.WriteException("Token校验失败", e);
                return false;
            }
        }

        public bool CheckSign(ActionExecutingContext actionContext)
        {
            try
            {
                ApiBaseController apiBaseController = actionContext.Controller as ApiBaseController;
                string obj = apiBaseController?.BodyParams;

                if (string.IsNullOrEmpty(obj))
                {
                    return false;
                }
                else
                {
                    var rst = false;

                    CompanyInfo _company = apiBaseController?.Company;
                    string AccessToken = apiBaseController?.Token;
                    string MenberToken = apiBaseController?.MemberToken;
                    if (_company != null)
                    {
                        Dictionary<string, object> tmpDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(obj);

                        Dictionary<string, object> dic = new Dictionary<string, object>();
                        foreach (var p in tmpDic)
                        {
                            var val = p.Value.ToString();
                            if (val.IndexOf('{') == 0)
                            {
                                var td = JsonConvert.DeserializeObject<Dictionary<string, string>>(val);
                                dic.Add(p.Key, JsonConvert.SerializeObject(td.OrderBy(t => t.Key, StringComparer.Ordinal).ToDictionary(k => k.Key, v => v.Value)));
                            }
                            else
                            {
                                dic.Add(p.Key, val);
                            }
                        }
                        if (!dic.ContainsKey("Sign"))
                            return false;

                        string oldSign = dic["Sign"]?.ToString();
                        dic.Remove("Sign");

                        string _SecretKey = !MenberToken.IsNullOrEmpty() ? MenberToken : !AccessToken.IsNullOrEmpty() ? AccessToken : _company.SecretKey;

                        string sign = ApiSignHelper.GetSign(dic, _SecretKey);
                        rst = oldSign.ToUpper() == sign.ToUpper();
                        if (!rst)
                        {
                            DefaultLogService.AddOperationLog(MarkId, $"RSA验签失败<br/>oldSign:{oldSign}");
                        }
                    }
                    else
                    {
                        DefaultLogService.AddOperationLog(MarkId, $"AccessToken验签失败，APPInfo为空");

                    }
                    return rst;
                }
            }
            catch (Exception e)
            {
                DefaultLogService.AddOperationLog(MarkId, e, "", "验签错误2");
                NLogHelper.WriteException("验签错误2", e);
                return false;
            }
        }

        public bool CheckTimestamp(ActionExecutingContext actionContext)
        {
            try
            {
                string obj = (actionContext.Controller as ApiBaseController)?.BodyParams;

                if (string.IsNullOrEmpty(obj))
                {
                    return false;
                }
                else
                {
                    bool rst = false;
                    Dictionary<string, object> tmpDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(obj);
                    foreach (var p in tmpDic)
                    { 
                        if(p.Key.ToLower() == "timestamp")
                        {
                            var timestamp = p.Value.ToString();
                            string now_timestamp = timestamp.Length == 13 ? TimeHelper.GetCurrentTimestamp(true) : TimeHelper.GetCurrentTimestamp();

                            if (TimeHelper.SubtractTimestam(now_timestamp, timestamp) <= ConfigHelper.GetInt("TimestampTimeOut"))//数据通信延迟有效时间3秒
                            {
                                rst = true;
                            }
                            else
                            {
                                rst = false;
                            }
                            break;
                        }
                    }
                    return rst;
                }
            }
            catch (Exception e)
            {
                DefaultLogService.AddOperationLog(MarkId, e, "", "Timestamp Error!");
                NLogHelper.WriteException("Timestamp Error!", e);
                return false;
            }
        }
        public static Dictionary<string, string> GetRequestParams(HttpContext context)
        {
            var pd = new Dictionary<string, string>();

            if (context.Request.Method == "POST")
            {
                var pv = GetPostValues(context);
                if (pv.Count > 0)
                {
                    return pv;
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

    }

}
