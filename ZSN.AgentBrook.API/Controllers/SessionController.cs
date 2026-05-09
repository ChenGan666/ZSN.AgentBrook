using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ZSN.AgentBrook.API.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Attributes;
using ZSN.AI.Service.Controllers;
using ZSN.Utils.Core.Extensions;
using Entity = ZSN.AI.Entity;

namespace ZSN.AgentBrook.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-Member")]
    [Route("api/[controller]/[action]")]
    public class SessionController: ApiBaseController
    {
        public SessionController()
        {
        }
        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }

        /// <summary>
        /// 获取会话列表
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<Entity.PageData<List<AppChatSessionInfo>>> GetList([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                int index = jObject.JsonGetValue<int>("Index", 1);
                int size = jObject.JsonGetValue<int>("Size", 10);
                string KeyWord = jObject.JsonGetValue<string>("KeyWord", "");

                string _sql = " SystemStatus = 0 and MemberID='" + memberSetting.FullMember.Member.MemberID + "' ";

                List<AppChatSessionInfo> _list = AppChatSessionInfoBussiness.GetListByPage(size, index, _sql, out int pagetotal, out int total, 1, "*", "CreateTime");

                Entity.PageData<List<AppChatSessionInfo>> _Data = new Entity.PageData<List<AppChatSessionInfo>>();
                _Data.Data = _list;
                _Data.pagetotal = pagetotal;
                _Data.total = total;
                return JsonMsg<Entity.PageData<List<AppChatSessionInfo>>>.OK(_Data);
            }
            else
            {
                return JsonMsg<Entity.PageData<List<AppChatSessionInfo>>>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 删除会话
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<string> Delete([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string SessionID = jObject.JsonGetValue<string>("sessionID", "");
                string MemberID = memberSetting.FullMember.Member.MemberID;
                string _sql = $"";

                AppChatSessionInfoBussiness.Delete(SessionID, MemberID);

                return JsonMsg<string>.OK(SessionID);
            }
            else
            {
                return JsonMsg<string>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 删除所有会话
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<string> CleanUp([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string MemberID = memberSetting.FullMember.Member.MemberID;
                string _sql = $"";

                AppChatSessionInfoBussiness.CleanUp(MemberID);

                return JsonMsg<string>.OK("");
            }
            else
            {
                return JsonMsg<string>.Error(null, ErrorCode.DataFormatError);
            }
        }


    }
}
