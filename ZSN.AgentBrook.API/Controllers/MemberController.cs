using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ZSN.AgentBrook.API.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Attributes;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AgentBrook.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-Member")]
    [Route("api/[controller]/[action]")]
    public class MemberController: ApiBaseController
    {
        public MemberController()
        {

        }

        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        /// <param name="paramValue">{}</param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public JsonMsg<MemberInfo> Get([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                return JsonMsg<MemberInfo>.OK(this.MemberSetting.FullMember.Member);
            }
            else
            {
                return JsonMsg<MemberInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 修改当前用户信息
        /// </summary>
        /// <param name="paramValue">{}</param>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public JsonMsg<MemberInfo> Save([FromBody] PostData paramValue) 
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string MNickName = jObject.JsonGetValue<string>("MNickName");

                string OldPassword = jObject.JsonGetValue<string>("OldPassword");
                string NewPassword = jObject.JsonGetValue<string>("NewPassword");

                if (!OldPassword.IsNullOrEmpty() && this.MemberSetting.FullMember.Member.MPWD == OldPassword)
                {

                    MemberInfo memberInfo = MemberInfoBussiness.GetModel(this.MemberSetting.FullMember.Member.MemberID);
                    if (memberInfo != null)
                    {
                        if (!NewPassword.IsNullOrEmpty() && OldPassword != NewPassword)
                        {
                            memberInfo.MNickName = MNickName;
                            memberInfo.MPWD = NewPassword;
                            MemberInfoBussiness.Update(memberInfo);
                        }
                        else
                        {
                            memberInfo.MNickName = MNickName;
                            MemberInfoBussiness.Update(memberInfo);
                        }

                        return JsonMsg<MemberInfo>.OK(memberInfo);
                    }
                    else
                    {
                         return JsonMsg<MemberInfo>.Error(null, ErrorCode.DataEmpty); 
                    }
                }
                else
                {
                    return JsonMsg<MemberInfo>.Error(null, ErrorCode.PasswordError);
                }

            }
            else
            {
                return JsonMsg<MemberInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }
    }
}
