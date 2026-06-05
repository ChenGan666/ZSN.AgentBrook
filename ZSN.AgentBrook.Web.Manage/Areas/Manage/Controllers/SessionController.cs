using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Workflow;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Service.Token;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using ZSN.AgentBrook.Web.Manage.Attributes;
using Newtonsoft.Json;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class SessionController : AdminBaseController
    {
        /// <summary>
        /// 会话列表页（分页 + 搜索）
        /// </summary>
        public IActionResult index(int index = 1, int size = 15, string keyword = "", int? sessionStatus = null)
        {
            string strWhere = " 1=1 ";

            // 关键词搜索：TopicSummary、AppID、MemberID
            if (!keyword.IsNullOrEmpty())
            {
                strWhere += $" AND (TopicSummary LIKE '%{keyword.SecureSQL()}%' OR AppID LIKE '%{keyword.SecureSQL()}%' OR MemberID LIKE '%{keyword.SecureSQL()}%')";
            }

            // 会话状态筛选
            if (sessionStatus.HasValue)
            {
                strWhere += $" AND SessionStatus = {sessionStatus.Value}";
            }

            var lst = AppChatSessionInfoBussiness.GetListByPage(size, index, strWhere, out int pagetotal, out int total, 1, "*", "CreateTime");

            // 批量查询 AppName 和 UserName，避免 N+1 查询
            var appNameDict = new Dictionary<string, string>();
            var memberNameDict = new Dictionary<string, string>();

            foreach (var session in lst)
            {
                if (!session.AppID.IsNullOrEmpty() && !appNameDict.ContainsKey(session.AppID))
                {
                    var app = AppInfoBussiness.GetModel(session.AppID);
                    appNameDict[session.AppID] = app?.Name ?? session.AppID;
                }
                if (!session.MemberID.IsNullOrEmpty() && !memberNameDict.ContainsKey(session.MemberID))
                {
                    var member = MemberInfoBussiness.GetModel(session.MemberID);
                    memberNameDict[session.MemberID] = member?.MNickName ?? session.MemberID;
                }
            }

            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.PageTotal = pagetotal;
            ViewBag.SessionList = lst;
            ViewBag.AppNameDict = appNameDict;
            ViewBag.MemberNameDict = memberNameDict;
            ViewBag.Keyword = keyword;
            ViewBag.SessionStatus = sessionStatus;
            ViewBag.APIBaseURL = ConfigHelper.GetString("APIBaseURL");

            return View();
        }

        /// <summary>
        /// 会话详情/对话查看页
        /// </summary>
        public IActionResult Detail(string sessionID)
        {
            if (sessionID.IsNullOrEmpty())
            {
                return RedirectToAction("index");
            }

            var session = AppChatSessionInfoBussiness.GetModel(sessionID);
            if (session == null)
            {
                return RedirectToAction("index");
            }

            // 获取应用名称和用户名
            string appName = session.AppID;
            if (!session.AppID.IsNullOrEmpty())
            {
                var app = AppInfoBussiness.GetModel(session.AppID);
                appName = app?.Name ?? session.AppID;
            }

            string userName = session.MemberID;
            if (!session.MemberID.IsNullOrEmpty())
            {
                var member = MemberInfoBussiness.GetModel(session.MemberID);
                userName = member?.MNickName ?? session.MemberID;
            }

            ViewBag.SessionInfo = session;
            ViewBag.AppName = appName;
            ViewBag.UserName = userName;
            ViewBag.APIBaseURL = ConfigHelper.GetString("APIBaseURL");

            return View();
        }

        /// <summary>
        /// 删除会话（支持批量删除，逗号分隔ID）
        /// </summary>
        [HttpPost]
        public JsonMsg<string> Del(string mid)
        {
            if (mid.IsNullOrEmpty())
            {
                return JsonMsg<string>.Error("参数错误", ErrorCode.DataFormatError);
            }

            AppChatSessionInfoBussiness.DeleteList(mid);
            return JsonMsg<string>.OK("删除成功");
        }

        /// <summary>
        /// 获取会话列表（JSON 接口，供 Detail 页面左侧列表使用）
        /// </summary>
        [HttpPost]
        public JsonMsg<object> GetSessionList(int index = 1, int size = 20, string keyword = "")
        {
            string strWhere = " 1=1 ";
            if (!keyword.IsNullOrEmpty())
            {
                strWhere += $" AND (TopicSummary LIKE '%{keyword.SecureSQL()}%' OR AppID LIKE '%{keyword.SecureSQL()}%' OR MemberID LIKE '%{keyword.SecureSQL()}%')";
            }

            var lst = AppChatSessionInfoBussiness.GetListByPage(size, index, strWhere, out int pagetotal, out int total, 1, "*", "CreateTime");

            // 批量查询名称
            var appNameDict = new Dictionary<string, string>();
            var memberNameDict = new Dictionary<string, string>();
            foreach (var s in lst)
            {
                if (!s.AppID.IsNullOrEmpty() && !appNameDict.ContainsKey(s.AppID))
                {
                    var app = AppInfoBussiness.GetModel(s.AppID);
                    appNameDict[s.AppID] = app?.Name ?? s.AppID;
                }
                if (!s.MemberID.IsNullOrEmpty() && !memberNameDict.ContainsKey(s.MemberID))
                {
                    var member = MemberInfoBussiness.GetModel(s.MemberID);
                    memberNameDict[s.MemberID] = member?.MNickName ?? s.MemberID;
                }
            }

            var result = lst.Select(s => (object)new
            {
                s.ChatSessionID,
                s.AppID,
                s.MemberID,
                s.TopicSummary,
                s.SessionStatus,
                s.CreateTime,
                AppName = appNameDict.ContainsKey(s.AppID ?? "") ? appNameDict[s.AppID] : s.AppID,
                UserName = memberNameDict.ContainsKey(s.MemberID ?? "") ? memberNameDict[s.MemberID] : s.MemberID
            }).ToList();

            return JsonMsg<object>.OK(new { list = result, total = total });
        }

        /// <summary>
        /// 获取会话的对话消息列表（JSON 接口，供前端 ChatBox 加载历史消息）
        /// </summary>
        [HttpPost]
        public JsonMsg<List<object>> GetChatMessages(string sessionID)
        {
            if (sessionID.IsNullOrEmpty())
            {
                return JsonMsg<List<object>>.Error(null, ErrorCode.DataFormatError);
            }

            var session = AppChatSessionInfoBussiness.GetModel(sessionID);
            if (session == null)
            {
                return JsonMsg<List<object>>.Error(null, ErrorCode.DataFormatError);
            }

            // 获取对话消息列表
            var chatLogs = AppChatLogInfoBussiness.GetListBySessionID(session.AppID, sessionID);

            // 转换为前端 ChatBox 可用的格式
            var messages = new List<object>();
            foreach (var log in chatLogs)
            {
                var gptMsg = log.ContentToGptMsg;
                string content = "";
                if (gptMsg != null)
                {
                    content = gptMsg.content ?? "";
                }
                else if (log.Content != null)
                {
                    content = log.Content.ToString();
                }

                // 跳过空消息
                if (content.IsNullOrEmpty() && (gptMsg?.Attachments == null || gptMsg.Attachments.Count == 0))
                {
                    continue;
                }

                messages.Add(new
                {
                    id = log.ChatLogID,
                    sessionID = log.ChatSessionID,
                    role = log.Role.ToLower(),
                    content = content,
                    timestamp = log.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    createdAt = log.CreateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    attachments = gptMsg?.Attachments ?? new List<ZSN.AI.Entity.Chat.AttachmentItem>(),
                    logOrder = log.LogOrder
                });
            }

            return JsonMsg<List<object>>.OK(messages, "Success", sessionID);
        }

        /// <summary>
        /// 获取会话对应 App 的 API 配置（供 ChatBox 初始化）
        /// </summary>
        [HttpPost]
        public JsonMsg<object> GetSessionApiConfig(string appID, string memberID)
        {
            try
            {
                WorkflowTester workflowTester = WorkflowTester.Config;

                string TesterAppID = workflowTester.APIAppID;
                string SecretKey = workflowTester.SecretKey;
                string AccessToken = "";
                string MemberToken = "";
                string RefreshToken = "";

                DateTime expirationDate = DateTime.Now.AddMilliseconds(ConfigHelper.GetInt("AccessTokenTimeOut"));

                // 使用会话的 MemberID 生成 MemberToken
                if (!memberID.IsNullOrEmpty())
                {
                    MemberTokenHelper.Set(memberID, 0, null, out MemberToken, out RefreshToken);
                }
                else if (!workflowTester.MemberID.IsNullOrEmpty())
                {
                    MemberTokenHelper.Set(workflowTester.MemberID, 0, null, out MemberToken, out RefreshToken);
                }

                // 获取 AccessToken
                AccessToken = CommonApiBaseController.GetTokenByAPPID(TesterAppID);

                // 确定使用的 AppID
                string targetAppID = appID ?? workflowTester.AppID;

                var config = new
                {
                    apiAppID = TesterAppID,
                    apiSecretKey = SecretKey,
                    appID = targetAppID,
                    accessToken = AccessToken,
                    memberToken = MemberToken,
                    refreshToken = RefreshToken,
                    expirationDate = expirationDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    memberId = memberID ?? workflowTester.MemberID,
                    baseURL = ConfigHelper.GetString("APIBaseURL")
                };

                return JsonMsg<object>.OK(config);
            }
            catch (System.Exception ex)
            {
                return JsonMsg<object>.Error(new { error = ex.Message }, ErrorCode.DataFormatError);
            }
        }
    }
}
