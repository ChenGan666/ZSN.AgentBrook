using ZSN.AgentBrook.API.Controllers;
using ZSN.AI.Service.Helpers;
using ZSN.Utils.Core;
using ZSN.Utils.Core.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.WebHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ZSN.AI.Service.Controllers.CommonApiBaseController;

namespace ZSN.AgentBrook.API.Attributes
{
    /// <summary>
    /// 会员过滤器
    /// </summary>
    public class MemberCheck: APIChecker
    {
        public int MarkId = 305;
        /// <summary>
        /// 是否进行MemberToken认证
        /// </summary>
        public bool MemberToken = true;
        /// <summary>
        /// 是否管理员认证
        /// </summary>
        public bool CheckManage = false;
        /// <summary>
        /// 是否进行Member签名认证
        /// </summary>
        public bool MemberSignin = ConfigHelper.GetBool("CheckSign", true);
        /// <summary>
        /// 当前请求的MemberID，通过MemberToken解析
        /// </summary>
        public string MemberID = "";
        /// <summary>
        /// 会员信息
        /// </summary>
        public MemberInfo memberInfo = null;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (MemberToken)
            {
                var controller = context.Controller as ApiBaseController;
                
                var token_checked = CheckToken(context);
                if (!token_checked)
                {
                    context.Result = GetErrorResult(ErrorCode.MemberTokenCheckError);
                }
                if (MemberSignin)
                {
                    this.Sign = false;//接口只允许一种签名方式，丢弃公共签名验证
                    var rst = CheckSign(context);
                    if (!rst)
                    {
                        context.Result = GetErrorResult(ErrorCode.MemberSignError);
                    }
                }
                if (CheckManage)
                {
                    if (controller != null)
                    {
                        var MemberSetting = controller.MemberSetting;
                        if (MemberSetting.UserInfo != null)
                        {

                        }
                    }
                }
            }
            base.OnActionExecuting(context);
        }
        /// <summary>
        /// 校验MemberToken
        /// </summary>
        /// <param name="actionContext"></param>
        /// <returns></returns>
        public new bool CheckToken(ActionExecutingContext actionContext)
        {
            try
            {
                string _Token = (actionContext.Controller as ApiBaseController)?.MemberToken;

                if (string.IsNullOrEmpty(_Token))
                {
                    return false;
                }
                else
                {
                    this.MemberID = "";
                    int MemberOtherAuthID = 0;
                    string timestamp = "";
                    string now_timestamp = TimeHelper.GetCurrentTimestamp();

                    GetMemberIdByToken(_Token, out  MemberID,out MemberOtherAuthID, out timestamp);

                    if (MemberID != "")
                    {
                        if (TimeHelper.SubtractTimestam(now_timestamp, timestamp) <= ConfigHelper.GetInt("AccessTokenTimeOut"))//Token有效时间3600秒
                        {
                            this.memberInfo = MemberInfoBussiness.GetModel(MemberID);

                            if (memberInfo != null)
                            {
                                if (memberInfo.MState == 0)
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
                DefaultLogService.AddOperationLog(5, e, "", ErrorCode.MemberTokenCheckError.ToString());
                NLogHelper.WriteException("MemberToken校验失败", e);
                return false;
            }
        }

        /// <summary>
        /// 校验Member数据包签名
        /// </summary>
        /// <param name="actionContext"></param>
        /// <returns></returns>
        public new bool CheckSign(ActionExecutingContext actionContext)
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
                    Dictionary<string, object> tmpDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(obj);
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    foreach (var p in tmpDic)
                    {
                        var val = p.Value is bool b ? b.ToString().ToLower(): p.Value.ToString() ?? "";
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

                    var rst = false;
                    string sign = "";
                    if (this.memberInfo != null)
                    {
                        string _Token = (actionContext.Controller as ApiBaseController)?.MemberToken;
                        var api_secret = _Token;
                        sign = ApiSignHelper.GetSign(dic, api_secret);
                        rst = oldSign.ToUpper() == sign.ToUpper();
                    }
                    //ConsoleLogHelper.WriteLine("old: " + oldSign);
                    //ConsoleLogHelper.WriteLine("new: " + sign);
                    if (!rst)
                    {
                        DefaultLogService.AddOperationLog(5, $"验签失败<br/>oldSign:{oldSign}<br/>sign:{sign}<br/>{JsonConvert.SerializeObject(dic, Formatting.Indented)}");
                    }
                    return rst;
                }
            }
            catch (Exception e)
            {
                DefaultLogService.AddOperationLog(5, e, "", "验签错误2");
                NLogHelper.WriteException("验签错误2", e);
                return false;
            }
        }
    }
}
