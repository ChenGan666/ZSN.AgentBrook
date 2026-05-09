using ZSN.AgentBrook.API.Attributes;
using ZSN.AgentBrook.API.Controllers;
using ZSN.Utils.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using System.Net;
using ZSN.Utils.Core.Extensions;
using ZSN.AI.Service.Attributes;
using ZSN.AI.Service.Token;

namespace ZSN.AgentBrook.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class TokenController : ApiBaseController
    {
        public static new DateTime UnixTimeStampStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static new long DateTimeToTimeStamp(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - UnixTimeStampStart).TotalSeconds;
        }

        public TokenController()
        {
            
        }
        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }


        /// <summary>
        /// Get token using account password
        /// </summary>
        /// <param name="paramValue">{'AppID':'Y=String'}</param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker(Token = false)]
        public JsonMsg<TokenResponse> Get([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string AppID = jObject.JsonGetValue<string>("AppID");
                
                string AccessToken = GetTokenByAPPID(AppID);
                TokenResponse token = new TokenResponse() { AccessToken = AccessToken, Expirein = DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut"))) };
                return JsonMsg<TokenResponse>.OK(token);// BuildSuccessResult(new { AccessToken = AccessToken, Expirein = DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut"))) });
            }
            else
            {
                return JsonMsg<TokenResponse>.Error(null, ErrorCode.DataFormatError);// BuildFailResult(ErrorCode.DataFormatError);
            }
        }
        /// <summary>
        /// 小程序Code换Accesstoken
        /// </summary>
        /// <param name="paramValue">
        /// {'code':'Y=String'}
        /// </param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker(Token = true)]
        public async Task<JsonMsg<TokenResponse>> GetOpenID([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string code = jObject.JsonGetValue<string>("code");

                string MemberID = "";
                string MemberToken = "";
                string RefreshToken = "";
                string AppID = ConfigHelper.GetSection("WeixinMiniApp").GetSection("AppID").Value ?? "";
                string AppSecret = ConfigHelper.GetSection("WeixinMiniApp").GetSection("AppSecret").Value ?? "";
                string code2Session_url = ConfigHelper.GetSection("WeixinMiniApp").GetSection("code2Session_url").Value ?? "";

                string url = string.Format(code2Session_url, AppID, AppSecret, code);

                (HttpStatusCode, string) _re = await HttpRequestHelper.HttpGetAsync(url);
                if(_re.Item1 == HttpStatusCode.OK && !string.IsNullOrEmpty(_re.Item2)){
                    JObject jObject2 = JObject.Parse(_re.Item2);
                    if(jObject2.JsonGetValue<int>("errcode") == 0)
                    {
                        string openid = jObject2.JsonGetValue<string>("openid");
                        string session_key = jObject2.JsonGetValue<string>("session_key");
                        string unionid = jObject2.JsonGetValue<string>("unionid");

                        if(this.GetMemberAccessTokenByOpenID(openid, session_key, unionid,out MemberID,out MemberToken, out RefreshToken))
                        {
                            TokenResponse token = new TokenResponse() { MemberToken = MemberToken, MemberRefreshToken = RefreshToken, Expirein = DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut"))) };
                            return JsonMsg<TokenResponse>.OK(token);// BuildSuccessResult(new { MemberToken = MemberToken, MemberRefreshToken = RefreshToken, Expirein = DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut"))) });
                        }
                        else
                        {
                            return JsonMsg<TokenResponse>.Error(null, ErrorCode.WeixinMiniAppMemberAcctokenError);// BuildFailResult(ErrorCode.WeixinMiniAppMemberAcctokenError);
                        }
                       
                    }
                    else
                    {
                        return JsonMsg<TokenResponse>.Error(null, ErrorCode.WeixinMiniAppError); //BuildFailResult(ErrorCode.WeixinMiniAppError);
                    }
                }
                else
                {
                    return JsonMsg<TokenResponse>.Error(null, ErrorCode.WeixinMiniAppRequestError); //BuildFailResult(ErrorCode.WeixinMiniAppRequestError);
                }

                
            }
            else
            {
                return JsonMsg<TokenResponse>.Error(null, ErrorCode.DataFormatError); //BuildFailResult(ErrorCode.DataFormatError);
            }
        }
        /// <summary>
        /// Get MemberToken using account password
        /// </summary>
        /// <param name="paramValue">
        /// {
        /// 'PhoneNumber':'Y=String',
        /// 'PWD':'Y=String',
        /// 'Device':{
        /// 'DeviceName':'String',
        /// 'DeviceID':'String,FCM DeviceID',
        /// 'IMEI':'String',
        /// 'ICCID':'String',
        /// 'UDID':'String',
        /// 'UUID':'String',
        /// 'IP':'String',
        /// 'Lat':'Number',
        /// 'Lon':'Number'
        /// }
        /// }</param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker(Token = true)]
        public JsonMsg<TokenResponse> GetMemberToken([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string PhoneNumber = jObject.JsonGetValue<string>("PhoneNumber").SecureSQL();
                string PWD = jObject.JsonGetValue<string>("PWD").SecureSQL();
                JObject deviceJson = jObject.JsonGetValue<JObject>("Device");

                MemberInfo member = MemberInfoBussiness.GetModelByPhoneNumber(PhoneNumber);
                if (member == null || string.Compare(member.MPWD, PWD.Trim(), false) != 0)
                {
                    return JsonMsg<TokenResponse>.Error(null, ErrorCode.AccountError);//BuildFailResult(ErrorCode.AccountError);
                }
                else if (member.MState == 1)
                {
                    return JsonMsg<TokenResponse>.Error(null, ErrorCode.AccountLock);//BuildFailResult(ErrorCode.AccountLock);
                }
                else
                {
                    string MemberToken = "";
                    string RefreshToken = "";
                    string memberAuthInfoJson = redis.StringGet(member.MemberID);
                    MemberAuthInfo memberAuthInfo = null;

                    
                    if (memberAuthInfoJson.IsNullOrEmpty())
                    {
                        MemberOtherAuthInfo memberOtherAuthInfo = MemberOtherAuthInfoBussiness.GetModel(member.MemberID);
                        if (memberOtherAuthInfo != null)
                        {
                            MemberTokenHelper.Set(member.MemberID, memberOtherAuthInfo.MemberOtherAuthID,redis, out MemberToken, out RefreshToken);
                        }
                        else
                        {
                            MemberTokenHelper.Set(member.MemberID, 0, redis, out MemberToken, out RefreshToken);
                        }
                        
                    }
                    else
                    {
                        memberAuthInfo = JsonConvert.DeserializeObject<MemberAuthInfo>(memberAuthInfoJson);
                        if (memberAuthInfo != null)
                        {
                            MemberToken = memberAuthInfo.AccessToken;
                            RefreshToken = memberAuthInfo.RefreshToken;
                        }
                    }
                    TokenResponse token = new TokenResponse() { MemberToken = MemberToken, MemberRefreshToken = RefreshToken, Expirein = DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut"))) };
                    return JsonMsg<TokenResponse>.OK(token);//return BuildSuccessResult(new { MemberToken = MemberToken, MemberRefreshToken = RefreshToken, Expirein = DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut"))) });
                }
            }
            else
            {
                return JsonMsg<TokenResponse>.Error(null, ErrorCode.DataFormatError);// BuildFailResult(ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// RefreshMemberToken
        /// </summary>
        /// <param name="paramValue">
        /// {'RefreshToken':'Y=String'}
        /// </param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true,MemberToken = true)]
        public JsonMsg<TokenResponse> RefreshMemberToken([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string MemberAccessToken = this.MemberToken;
                string RefreshToken = jObject.JsonGetValue<string>("RefreshToken").SecureSQL();
                string _MemberID = "";

               MemberAuthInfo memberAuthInfo =  MemberAuthInfoBussiness.GetModelByRefreshToken(RefreshToken);

                if (memberAuthInfo != null)
                {
                    if (MemberAccessToken.Equals(memberAuthInfo.AccessToken))
                    {
                        int _MemberOtherAuthID = 0;
                        string _timestamp_FormRefreshToken_MemberAccessToken = "";
                        GetMemberIdByToken(memberAuthInfo.AccessToken, out _MemberID, out _MemberOtherAuthID, out _timestamp_FormRefreshToken_MemberAccessToken);
                        if (_MemberOtherAuthID>0)
                        {
                            MemberTokenHelper.Set(_MemberID, _MemberOtherAuthID,redis, out MemberAccessToken, out RefreshToken);

                            TokenResponse token = new TokenResponse() { MemberToken = MemberToken, MemberRefreshToken = RefreshToken, Expirein = DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut"))) };
                            return JsonMsg<TokenResponse>.OK(token);//return BuildSuccessResult(new { MemberToken = MemberAccessToken, MemberRefreshToken = RefreshToken, Expirein = DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut"))) });

                        }
                        else
                        {
                            return JsonMsg<TokenResponse>.Error(null, ErrorCode.RefreshTokenError); //BuildFailResult(ErrorCode.RefreshTokenError);
                        }
                    }
                    else
                    {
                        return JsonMsg<TokenResponse>.Error(null, ErrorCode.RefreshTokenError); //BuildFailResult(ErrorCode.RefreshTokenError);
                    }
                }
                else
                {
                    return JsonMsg<TokenResponse>.Error(null, ErrorCode.RefreshTokenError); //BuildFailResult(ErrorCode.RefreshTokenError);
                }
            }
            else
            {
                return JsonMsg<TokenResponse>.Error(null, ErrorCode.DataFormatError); //BuildFailResult(ErrorCode.DataFormatError, jObject.JsonGetValue<string>("msg", ErrorCode.DataFormatError.ToString()));
            }
        }

        private bool GetMemberAccessTokenByOpenID(string OpenID, string session_key, string unionid, out string MemberID, out string MemberAccessToken, out string MemberRefreshToken)
        {
            MemberID = "";
            MemberAccessToken = "";
            MemberRefreshToken = "";

            //判断是否已经注册
            MemberOtherAuthInfo  memberOtherAuth = MemberOtherAuthInfoBussiness.GetModelByOpenid(OpenID);
            if(memberOtherAuth != null)
            {
                MemberID = memberOtherAuth.MemberID;
                MemberTokenHelper.Set(MemberID, memberOtherAuth.MemberOtherAuthID,redis, out MemberAccessToken, out MemberRefreshToken);

                memberOtherAuth.SessionKey = session_key;
                memberOtherAuth.UnionID = unionid;
                MemberOtherAuthInfoBussiness.Update(memberOtherAuth);

                return true;
            }
            else
            {
                //未注册生成一套会员信息

                MemberID = hashEncrypt.MD5System(Guid.NewGuid().ToString());
                MemberInfo memberInfo = new MemberInfo();
                memberInfo.MemberID = MemberID;
                memberInfo.MPhoneNumber = "";
                memberInfo.MNickName = "";
                memberInfo.MPWD = "";
                memberInfo.MIcon = "";
                memberInfo.MBirthday = DateTime.Now;
                memberInfo.MState = 0;
                memberInfo.MPoints = 0;
                memberInfo.MLevel = 0;
                memberInfo.MIntroducer = "";
                memberInfo.MAppendTime = DateTime.Now;

                if (MemberInfoBussiness.Add(memberInfo)!= String.Empty)
                {
                    memberOtherAuth = new MemberOtherAuthInfo();
                    memberOtherAuth.OpenID = OpenID;
                    memberOtherAuth.SessionKey = session_key;
                    memberOtherAuth.UnionID = unionid;
                    memberOtherAuth.MemberID = MemberID;

                    memberOtherAuth.MemberOtherAuthID = MemberOtherAuthInfoBussiness.Add(memberOtherAuth);
                    if (memberOtherAuth.MemberOtherAuthID > 0)
                    {
                        MemberTokenHelper.Set(MemberID, memberOtherAuth.MemberOtherAuthID,redis, out MemberAccessToken, out MemberRefreshToken);
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

    }
}
