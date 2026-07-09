using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    /// <summary>
    /// 应用工厂：在管理后台选择平台 App → 定制品牌 → 提交发布任务，
    /// 由独立的 ZSN.AgentBrook.AutoPublishJob 异步构建打包，产出可下载的独立应用。
    /// </summary>
    [Area("Manage")]
    [AdminAttributes]
    public class AppFactoryController : AdminBaseController
    {
        /// <summary>发布任务列表</summary>
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = PublishTaskInfoBusiness.GetListByPage(size, index, "", out int pagetotal, out int total, 1, "*", "CreateTime");
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.List = lst;
            return View();
        }

        /// <summary>新建/编辑发布任务入口：选 App + 选模板 + 填品牌。传 id=任务ID 时为编辑模式</summary>
        public IActionResult Create(string mid = "", string id = "")
        {
            // App 下拉：仅列出已发布的 App(编辑模式下追加当前 AppID 以防它被取消发布)
            var apps = AppInfoBussiness.GetListByPage(1000, 1, " SystemStatus = 2 ", out int _, out int _);
            ViewBag.AppList = apps;
            ViewBag.SelectedAppID = mid ?? "";
            ViewBag.Templates = GetTemplateItems();

            // 编辑模式：加载已有任务并回填表单
            PublishTaskInfo? editTask = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                editTask = PublishTaskInfoBusiness.GetModel(id);
                if (editTask == null) return Content("任务不存在");
                // 仅 Pending/Failed 可编辑，避免破坏进行中或已完成的构建一致性
                if (editTask.State != PublishTaskState.Pending && editTask.State != PublishTaskState.Failed)
                {
                    return Content($"当前状态({editTask.State})不可编辑，仅「等待中/失败」的任务允许修改。");
                }
            }
            ViewBag.EditTask = editTask;
            ViewBag.IsEdit = editTask != null;
            return View();
        }

        /// <summary>模板清单(从 appsettings 的 Templates:Items 读取，供前端下拉)</summary>
        public JsonMsg<List<TemplateItem>> TemplateList()
        {
            return JsonMsg<List<TemplateItem>>.OK(GetTemplateItems());
        }

        /// <summary>提交发布任务(新建)。taskID 非空时为更新已有任务(仅 Pending/Failed 可改)。</summary>
        [HttpPost]
        public JsonMsg<string> Submit(string taskID, string appID, string templateName, string templateGitUrl, string templateRef,
            string productName, string identifier, string version, string appTitle, string windowTitle,
            int windowWidth, int windowHeight,
            string apiBaseUrl, string connAppId, string connAppSecret,
            string lockAppId, bool hideAppPicker,
            string targetPlatforms, string reCallUrl, string templateSubPath)
        {
            bool isEdit = !string.IsNullOrWhiteSpace(taskID);
            PublishTaskInfo? task = null;
            if (isEdit)
            {
                task = PublishTaskInfoBusiness.GetModel(taskID!);
                if (task == null) return JsonMsg<string>.Error("任务不存在", ErrorCode.DataNotExists);
                if (task.State != PublishTaskState.Pending && task.State != PublishTaskState.Failed)
                    return JsonMsg<string>.Error($"当前状态({task.State})不可修改，仅「等待中/失败」的任务允许修改", ErrorCode.TaskStateError);
            }

            var app = AppInfoBussiness.GetModel(appID);
            if (app == null) return JsonMsg<string>.Error("App 不存在", ErrorCode.DataNotExists);

            // 解析模板(取默认 GitUrl/Ref/SubPath 兜底)
            var tpl = GetTemplateItems().FirstOrDefault(t => t.Name == templateName) ?? new TemplateItem();
            string gitUrl = string.IsNullOrWhiteSpace(templateGitUrl) ? tpl.GitUrl : templateGitUrl;
            string @ref = string.IsNullOrWhiteSpace(templateRef) ? tpl.DefaultRef : templateRef;
            string subPath = string.IsNullOrWhiteSpace(templateSubPath) ? (tpl.SubPath ?? "") : templateSubPath;

            // 找一个用于换 Token 的 AppID/Secret(连接凭据)：优先显式传入，否则用平台默认 App
            var apisetting = ApisettingsInfoBussiness.GetModelByAppID(appID);
            string effectiveConnAppId = string.IsNullOrWhiteSpace(connAppId) ? appID : connAppId;
            string effectiveConnSecret = string.IsNullOrWhiteSpace(connAppSecret)
                ? (apisetting?.SecretKey ?? "") : connAppSecret;

            var cfg = new PublishConfig
            {
                brand = new BrandConfig
                {
                    productName = productName,
                    identifier = identifier,
                    version = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version,
                    appTitle = string.IsNullOrWhiteSpace(appTitle) ? productName : appTitle,
                    windowTitle = string.IsNullOrWhiteSpace(windowTitle) ? productName : windowTitle,
                    windowWidth = windowWidth <= 0 ? 1200 : windowWidth,
                    windowHeight = windowHeight <= 0 ? 750 : windowHeight
                },
                lockApp = new LockAppConfig { appId = string.IsNullOrWhiteSpace(lockAppId) ? appID : lockAppId, hideAppPicker = hideAppPicker },
                connection = new ConnectionConfig
                {
                    apiBaseUrl = apiBaseUrl,
                    appId = effectiveConnAppId,
                    appSecret = effectiveConnSecret
                },
                build = new BuildConfig { targets = ParseTargetList(targetPlatforms) },
                templateSubPath = subPath ?? ""
            };

            if (isEdit)
            {
                // 修改：更新可编辑字段，重置状态为 Pending 以便重新入队
                task!.AppID = appID;
                task.TemplateName = templateName;
                task.TemplateGitUrl = gitUrl ?? "";
                task.TemplateRef = @ref ?? "";
                task.PublishConfig = cfg;
                task.TargetPlatforms = targetPlatforms ?? "WinX64";
                task.ReCallUrl = reCallUrl ?? "";
                task.State = PublishTaskState.Pending;
                task.Progress = 0;
                task.Stage = "";
                task.ErrorMsg = "";
                task.Logs = "";   // 清空旧日志
                task.UpdateTime = System.DateTime.Now;
                PublishTaskInfoBusiness.Update(task);
                return JsonMsg<string>.OK(task.TaskID);
            }

            // 新建
            var newTask = new PublishTaskInfo
            {
                AppID = appID,
                TemplateName = templateName,
                TemplateGitUrl = gitUrl ?? "",
                TemplateRef = @ref ?? "",
                PublishConfig = cfg,
                State = PublishTaskState.Pending,
                TargetPlatforms = targetPlatforms ?? "WinX64",
                ReCallUrl = reCallUrl ?? "",
                CreateMemberID = "",
                CreateUserID = ZSN.AI.Service.WebHelpers.UserService.CurrentUserID.ToString()
            };
            PublishTaskInfoBusiness.Add(newTask);
            return JsonMsg<string>.OK(newTask.TaskID);
        }

        /// <summary>任务详情(进度+日志)</summary>
        public IActionResult Detail(string id)
        {
            var task = PublishTaskInfoBusiness.GetModel(id);
            ViewBag.Task = task;
            return View();
        }

        /// <summary>轮询任务最新状态(前端定时刷新用)</summary>
        [HttpPost]
        public JsonMsg<PublishTaskInfo> Poll(string id)
        {
            var task = PublishTaskInfoBusiness.GetModel(id);
            if (task == null) return JsonMsg<PublishTaskInfo>.Error(null, ErrorCode.DataNotExists);
            // Logs 可能很大，详情页才需要全量；轮询返回尾部片段即可
            if (!string.IsNullOrEmpty(task.Logs) && task.Logs.Length > 8000)
            {
                task.Logs = "...(更早日志已省略，详见详情页)...\n" + task.Logs.Substring(task.Logs.Length - 8000);
            }
            return JsonMsg<PublishTaskInfo>.OK(task);
        }

        /// <summary>下载产物(目录内首个安装包)</summary>
        [HttpGet]
        public IActionResult Download(string id)
        {
            var task = PublishTaskInfoBusiness.GetModel(id);
            if (task == null || string.IsNullOrEmpty(task.ArtifactPath))
                return NotFound("产物不存在");

            // 产物目录下取第一个安装包/zip 文件
            var dir = task.ArtifactPath;
            if (!System.IO.Directory.Exists(dir)) return NotFound("产物目录不存在");

            // 路径越权校验：ArtifactPath 必须在配置的产物输出根目录内
            string artRoot = ConfigHelper.GetString("AppBuild:OutputDirectory");
            if (!string.IsNullOrEmpty(artRoot))
            {
                string fullRoot = Path.GetFullPath(artRoot);
                string fullDir = Path.GetFullPath(dir);
                if (!fullDir.StartsWith(fullRoot)) return BadRequest("非法路径");
            }

            var file = System.IO.Directory.GetFiles(dir)
                .FirstOrDefault(f => f.EndsWith(".exe") || f.EndsWith(".dmg") || f.EndsWith(".zip") || f.EndsWith(".msi"));
            if (file == null) return NotFound("未找到可下载的产物文件");

            return PhysicalFile(file, "application/octet-stream", Path.GetFileName(file));
        }

        /// <summary>重新生成(重置为 Pending)：成功(Done)/失败(Failed)/等待(Pending)均可，生成中禁止</summary>
        [HttpPost]
        public JsonMsg<string> Retry(string id)
        {
            var task = PublishTaskInfoBusiness.GetModel(id);
            if (task == null) return JsonMsg<string>.Error("任务不存在", ErrorCode.DataNotExists);
            // 生成中的任务(Cloning/Customizing/Building/Verifying)不允许重试，避免与正在执行的构建冲突
            bool isRunning = task.State == PublishTaskState.Cloning
                          || task.State == PublishTaskState.Customizing
                          || task.State == PublishTaskState.Building
                          || task.State == PublishTaskState.Verifying;
            if (isRunning)
                return JsonMsg<string>.Error("任务生成中，无法重新生成", ErrorCode.TaskStateError);
            task.State = PublishTaskState.Pending;
            task.Progress = 0;
            task.Stage = "";
            task.ErrorMsg = "";
            task.Logs = "";
            // 清旧产物(重生后作废)，ArtifactPath 在新构建完成时重新填
            task.ArtifactPath = "";
            task.ArtifactFileCode = "";
            task.StartTime = new System.DateTime(2000, 1, 1);
            task.FinishTime = new System.DateTime(2000, 1, 1);
            task.UpdateTime = System.DateTime.Now;
            PublishTaskInfoBusiness.Update(task);
            return JsonMsg<string>.OK("已重新入队");
        }

        /// <summary>删除任务</summary>
        [HttpPost]
        public JsonMsg<string> Delete(string id)
        {
            PublishTaskInfoBusiness.Delete(id);
            return JsonMsg<string>.OK("已删除");
        }

        #region 私有辅助
        private List<TemplateItem> GetTemplateItems()
        {
            var items = new List<TemplateItem>();
            try
            {
                string json = ConfigHelper.GetString("Templates:Items");
                if (!string.IsNullOrWhiteSpace(json))
                {
                    // ConfigHelper 可能返回单对象 JSON 片段，这里统一反序列化
                    var arr = JsonConvert.DeserializeObject<List<TemplateItem>>(json);
                    if (arr != null) items = arr;
                }
            }
            catch { }
            if (items.Count == 0)
            {
                items.Add(new TemplateItem { Name = "Base", DisplayName = "通用基座应用", DefaultRef = "main" });
                items.Add(new TemplateItem { Name = "MeetingAssistant", DisplayName = "会议助手", DefaultRef = "main" });
            }
            return items;
        }

        private static List<string> ParseTargetList(string targetPlatforms)
        {
            var result = new List<string>();
            string tp = (targetPlatforms ?? "").ToLowerInvariant();
            if (tp.Contains("win")) result.Add("nsis");
            if (tp.Contains("mac")) result.Add("dmg");
            if (tp.Contains("web")) result.Add("web");
            if (result.Count == 0) result.Add("nsis");
            return result;
        }

        public class TemplateItem
        {
            public string Name { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string GitUrl { get; set; } = "";
            public string DefaultRef { get; set; } = "main";
            /// <summary>仓库内子目录(单仓库多模板时用，如 "MeetingApp")</summary>
            public string SubPath { get; set; } = "";
            public string Description { get; set; } = "";
        }
        #endregion
    }
}
