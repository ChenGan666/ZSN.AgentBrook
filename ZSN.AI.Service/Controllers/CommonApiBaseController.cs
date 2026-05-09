using ZSN.Utils.Core;
using ZSN.Utils.Core.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using ZSN.Utils.Core.Utils;
using ZSN.Utils.Core.Extensions;
using ZSN.AI.Entity;

namespace ZSN.AI.Service.Controllers
{
    public class CommonApiBaseController : CommonBaseController
    {

        public static HashEncrypt hashEncrypt = new HashEncrypt();
        /// <summary>
        /// url
        /// </summary>
        private string _url { get; set; } = null;
        /// <summary>
        /// bodyParams
        /// </summary>
        private string _bodyParams { get; set; } = null;
        /// <summary>
        /// AccessToken
        /// </summary>
        private string _token { get; set; } = null;
        /// <summary>
        /// MemberToken
        /// </summary>
        private string _memberToken { get; set; } = null;

        
        public static DateTime UnixTimeStampStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long DateTimeToTimeStamp(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - UnixTimeStampStart).TotalSeconds;
        }
        public CommonApiBaseController()
        {

            if (HttpContextHelper.Current != null)
            {
                // 已启用 EnableBuffering，现在可以安全地多次读取请求体
                // 在构造函数中读取 BodyParams 并设置到 Session，供 SettingsService 使用
                var _x = this.BodyParams;

                try
                {
                    _token = HttpContextHelper.Current.Request.Headers["bearer"];
                    _memberToken = HttpContextHelper.Current.Request.Headers["memberbearer"];
                }
                catch
                {
                    _token = "";
                    _memberToken = "";
                }
                
                // 尝试设置Session，如果失败（如MCP调用时响应已开始）则忽略
                try
                {
                    HttpContextHelper.Session.SetString("Token", _token == null ? "" : _token);
                    HttpContextHelper.Session.SetString("MemberToken", _memberToken == null ? "" : _memberToken);
                }
                catch (System.InvalidOperationException)
                {
                    // MCP调用时响应已开始，无法设置Session，忽略此异常
                }
            }
        }


        public string BodyParams
        {
            get
            {
                if (this.PostFile != null)
                {
                    _bodyParams = HttpContextHelper.Current.Request.Form["Data"];
                    if (_bodyParams != null)
                    {
                        // 尝试设置Session，如果失败则忽略
                        try
                        {
                            HttpContextHelper.Session.SetString("BodyParams", _bodyParams);
                        }
                        catch (System.InvalidOperationException)
                        {
                            // MCP调用时响应已开始，无法设置Session，忽略此异常
                        }
                    }
                }
                else
                {
                    if (_bodyParams.IsNullOrEmpty())
                    {
                        _bodyParams = HttpContextHelper.GetBodyParams(HttpContextHelper.Current);
                        if(_bodyParams != null)
                        {
                            // 尝试设置Session，如果失败则忽略
                            try
                            {
                                HttpContextHelper.Session.SetString("BodyParams", _bodyParams);
                            }
                            catch (System.InvalidOperationException)
                            {
                                // MCP调用时响应已开始，无法设置Session，忽略此异常
                            }
                        }
                    }
                }
                return _bodyParams;
            }
        }

        public string URL
        {
            get
            {
                _url = HttpContextHelper.Current.Request.AbsoluteUri();
                return _url;
            }
        }

        public IFormFileCollection PostFile
        {
            get
            {
                try
                {
                    // 确保当前上下文为HTTP请求
                    if (HttpContextHelper.Request != null && HttpContextHelper.Request.HasFormContentType)
                    {
                        if (HttpContextHelper.Current != null && HttpContextHelper.Current.Request.Form.Files.Count > 0)
                        {
                            return HttpContextHelper.Current.Request.Form.Files;
                        }
                    }
                }
                catch
                {
                    // 对于JSON请求体或其他无法读取Form的情况，返回null
                }
                
                return null;
            }
        }
        /// <summary>
        /// AccessToken
        /// </summary>
        public string Token
        {
            get
            {
                if (_token.IsNullOrEmpty())
                {
                    try
                    {
                        _token = HttpContextHelper.Current.Request.Headers["bearer"];
                    }
                    catch
                    {
                        _token = "";
                    }
                    HttpContextHelper.Session.SetString("Token", _token == null ? "" : _token);
                }
                return _token;
            }
        }
        /// <summary>
        /// MemberToken
        /// </summary>
        public string MemberToken
        {
            get
            {
                if (_memberToken.IsNullOrEmpty())
                {
                    try
                    {
                        _memberToken = HttpContextHelper.Current.Request.Headers["memberbearer"];
                    }
                    catch
                    {
                        _memberToken = "";
                    }
                    HttpContextHelper.Session.SetString("MemberToken", _memberToken == null ? "" : _memberToken);
                }
                return _memberToken;
            }
        }
        /// <summary>
        /// 生成AccessToken
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public static string GetTokenByUserId(Guid userID)
        {
            return (userID.ToString() + "|" + DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut")))).DesEncrypt();//TimeHelper.GetCurrentTimestamp()
        }
        public static string GetTokenByAPPID(string AppID) {
            return (AppID + "|" + DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut")))).DesEncrypt();
        }
        public static void GetAPPIDIdByToken(string Token, out string AppID, out string timestamp)
        {
            Token = Token.DesDecrypt();

            string[] _obj = Token.Split("|");
            AppID = _obj[0];
            timestamp = _obj[1];
        }
        /// <summary>
        /// 解析AccessToken
        /// </summary>
        /// <param name="Token"></param>
        /// <param name="userID"></param>
        /// <param name="timestamp"></param>
        public static void GetUserIdByToken(string Token, out string userID, out string timestamp)
        {
            Token = Token.DesDecrypt();

            string[] _obj = Token.Split("|");
            userID = _obj[0];
            timestamp = _obj[1];
        }
        /// <summary>
        /// 生成MemberToken
        /// </summary>
        /// <param name="memberID"></param>
        /// <returns></returns>
        public static string GetMemberTokenByUserId(string memberID, int MemberOtherAuthID)
        {
            return (memberID + "|" + MemberOtherAuthID + "|" + DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut")))).DesEncrypt();//TimeHelper.GetCurrentTimestamp()
        }
        public static string GetMemberRefreshToken(string memberID, string MemberAccessToken)
        {
            return hashEncrypt.MD5System((memberID + "|" + MemberAccessToken));//.DesEncrypt();
        }
        /// <summary>
        /// 解析MemberToken
        /// </summary>
        /// <param name="MemberToken"></param>
        /// <param name="memberID"></param>
        /// <param name="timestamp"></param>
        public static void GetMemberIdByToken(string MemberToken, out string memberID, out int MemberOtherAuthID, out string timestamp)
        {
            MemberToken = MemberToken.DesDecrypt();
            memberID = "";
            MemberOtherAuthID = 0;
            timestamp = "";

            string[] _obj = MemberToken.Split("|");
            if (_obj.Length == 3)
            {
                memberID = _obj[0];
                MemberOtherAuthID = Int32.Parse( _obj[1]);
                timestamp = _obj[2];
            }
        }
        /*
        public static void GetMemberIdByRefreshToken(string MemberRefreshToken,out string memberID,out string MemberAccessToken)
        {
            
            MemberRefreshToken = MemberRefreshToken.DesDecrypt();
            memberID = "";
            MemberAccessToken = "";

            string[] _obj = MemberRefreshToken.Split("|");
            if (_obj.Length == 2)
            {
                memberID = _obj[0];
                MemberAccessToken = _obj[1];
            }
        }*/
        public static JsonResult GetErrorResult(ErrorCode errorCode)
        {
            return GetErrorResult((int)errorCode, errorCode.ToString());
        }
        public static JsonResult GetErrorResult(int code, string detail)
        {
            return new JsonResult(new
            {
                Status = false,
                Success = false,
                ErrorCode = code,
                ErrorDesc = detail
            });
        }
    }

    /// <summary>
    /// Cache Data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CacheData<T> where T : class
    {
        /// <summary>
        /// 内容
        /// </summary>
        public T Data { get; set; }
    }


    public class PageData<T> where T : class
    {
        public T Data { get; set; }
        public int pagetotal { get; set; }
        public int total { get; set; }
    }
}
