using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ZSN.AgentBrook.API.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Attributes;
using ZSN.AI.Service.Controllers;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Utils;

namespace ZSN.AgentBrook.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-Public")]
    [Route("api/[controller]/[action]")]
    public class BaseController : ApiBaseController
    {
        public BaseController()
        {
        }

        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }

        /// <summary>
        /// 获取基础配置信息
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<BaseInfo> Get([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                BaseInfo _baseinfo = new BaseInfo();
                _baseinfo.CompanyInfo = this.COMPANY;
                _baseinfo.CompanyInfo.SecretKey = "";

                _baseinfo.AppList = AppInfoBussiness.GetList(" SystemStatus=2 ");

                _baseinfo.TagClassList = BaseDictionaryInfoBussiness.GetAllChildList("标签分类", false,true);

                //获取个人知识库个数

                _baseinfo.TagClassList.Add(new BaseDictionaryInfo()
                {
                    Cid = -1,
                    DicId  = 0,
                    DicName = "个人知识库",
                    Icon = "profile",
                    KnowledgeBaseCount = KnowledgeBaseInfoBussiness.GetRecordCount($" MemberID='{memberSetting.FullMember.Member.MemberID}' and SystemStatus<>-1"),
                    ChildrenList = new List<BaseDictionaryInfo>()
                });

                // 查询执行中的会话状态
                string runningSessionIds = jObject.JsonGetValue<string>("runningSessionIds", "");
                if (!string.IsNullOrWhiteSpace(runningSessionIds))
                {
                    var quotedIds = StringUtil.QuoteSeparatedItems(runningSessionIds, ',', '\'');
                    var sessions = AppChatSessionInfoBussiness.GetSessionStatusList(quotedIds);
                    if (sessions != null && sessions.Count > 0)
                    {
                        foreach (var session in sessions)
                        {
                            var info = new SessionStatusInfo
                            {
                                ChatSessionID = session.ChatSessionID,
                                SessionStatus = session.SessionStatus,
                                TopicSummary = session.TopicSummary ?? "",
                                AppID = session.AppID ?? ""
                            };
                            // 已完成或失败的会话，获取最后一条assistant消息摘要
                            if (session.SessionStatus != 1)
                            {
                                info.Summary = AppChatSessionInfoBussiness.GetLastAssistantSummary(session.AppID, session.ChatSessionID);
                            }
                            _baseinfo.SessionStatusList.Add(info);
                        }
                    }
                }

                return JsonMsg<BaseInfo>.OK(_baseinfo);
            }
            else
            {
                return JsonMsg<BaseInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }
    }
}
