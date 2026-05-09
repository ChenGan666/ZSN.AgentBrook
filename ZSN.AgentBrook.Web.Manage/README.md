# ZSN.AgentBrook.Web.Manage — 项目说明

> 路径：`w:\AI\ZSN.Knowbase\ZSN.Knowbase.Core\ZSN.AgentBrook.Web.Manage`
> 主要功能位于：`Areas/Manage/`

## 项目概览

- **定位**：后台管理站点（Razor Views + Areas），用于应用/Agent/工作流、知识库、菜单、用户与权限等管理。
- **框架**：ASP.NET Core MVC (`net8.0`) + Razor Views + Areas。
- **入口路由**：
  - 默认：`/{controller=Index}/{action=Index}/{id?}`
  - 区域：`/{area:exists}/{controller=Index}/{action=Index}/{id?}`
- **富文本资源**：内置 `ueditor/` 静态目录映射。

## 技术栈与依赖

- **包**（见 `ZSN.AgentBrook.Web.Manage.csproj`）：
  - `Microsoft.Extensions.*`（Hosting、DI、WindowsServices）
  - `Blazored.LocalStorage`
  - `SharpZipLib`
  - `System.Text.Json`
- **项目引用**：
  - `ZSN.AI.Core`、`ZSN.AI.Service`、`ZSN.AI.BLL`、`ZSN.AI.Entity`、`ZSN.AI.Node`、`ZSN.AI.Plugins`
  - `ZSN.AI.DAL.*`（MySql / Postgres）
  - `ZSN.AgentBrook.Plugins`
  - `ZSN.Utils.Core`

## 启动配置（`Startup.cs`）

- **服务注册**：
  - `AddSession()`、`AddRazorPages().AddRazorRuntimeCompilation()`、`AddControllersWithViews()`
  - CORS 默认允许任意源/方法/头
  - 自动扫描服务：`services.AddServicesFromAssemblies("ZSN.AI.Core")`、`"ZSN.AI.Plugins"`
  - `FunctionService` 单例注入（载入 `ZSN.AI.Plugins.BasePlugin`）
  - SignalR
  - 全局 JSON 格式：
    - Newtonsoft：忽略循环引用、非驼峰、日期格式 `yyyy-MM-dd HH:mm:ss`
    - System.Text.Json：禁用命名策略、全量 Unicode、注册 `DateTimeConverter`/`DateTimeNullableConvert`
- **中间件**：
  - 开发异常页或异常处理页
  - 静态文件与 ueditor 静态目录（`/ueditor`，公共缓存头）
  - `app.Init()` + `StartupHelper.ConfigureInit(...)`
  - 路由与 CORS、授权、Session

## 目录结构

- `Areas/Manage/Controllers/`：管理后台核心控制器（详见下节）
- `Areas/Manage/Views/`：对应的 Razor 视图
- `Attributes/`：后台鉴权特性
- `Controllers/`（根）：少量通用入口
- `wwwroot/`：静态资源（assets、plugs、样例文件等）

## 区域 Manage 的核心功能（`Areas/Manage/`）

- **登录与权限**
  - `AuthorizeController`：
    - `Login`/`doLogin`/`Quit`
    - 登录成功后写入 Session（`UserID`），并发放管理端 Token（`UserInfoBussiness.GetTokenByUserId`）
  - `AdminBaseController`：所有后台控制器基类，带 `[Area("Manage")]`
  - `AdminAttributes`（特性，见 `ZSN.AgentBrook.Web.Manage.Attributes`）：控制登录、URL、权限校验，可在单个 Action 上放宽

- **首页与菜单**
  - `IndexController.Index()`：
    - 拉取菜单 `MenuInfo`，按 `User.PermissionCode` 过滤权限菜单
    - 通过 `ViewBag.Menus` 渲染导航
  - `MenuController`：
    - 菜单树视图与刷新（`Tree/Tree1/TreeRefresh`）
    - 菜单项维护（`Body/AddApp/Sort/Buttons`）
    - 依赖 `MenuInfoBussiness` 进行增删改与排序

- **工作流管理**
  - `WorkflowController`：
    - 列表 `index(type, mid, index, size)`：按 `MainType/MainID` 查询
    - 获取编辑所需基础配置 `getBaseConfig()`：模型、知识库、插件、Agent、MCP 列表
    - 获取/初始化工作流 `getWorkFlow(WorkflowID, MainID, MainType)`：读取已存或用 `ZSN.AI.Node.Utils.initWorkFlow(...)` 初始化
    - 新增节点 `addNode(WorkflowID, NodeType, MainID)`：`ZSN.AI.Node.Utils.newNode(...)`
    - 编辑页 `Edit(id, MainID, MainType)`：注入前端测试配置 `WorkflowTester.Config`、`previewHost`
    - 保存 `Save(WorkFlow)`：`WorkflowInfoBussiness.Save(workFlow)`
    - 状态 `Status(mid, status)`：启用/禁用
    - 聊天记录 `getChatLog(ChatSessionID)`
    - 复制 `Copy(mid, name)`：深拷贝 Workflow/Nodes/Edges 并重写 ID 与配置引用
    - 前端测试凭据 `GetTesterConfig(workflowID)`：生成 AccessToken / MemberToken / RefreshToken 等

- **知识库管理**
  - `KnowledgeBaseController`：
    - 知识库列表 `index(index, size)`
    - 文件列表 `filelist(KnowledgeBaseID, index, size)`
    - 启停 `KnowledgeBaseStatus(mid, status)`
    - 新增/编辑 `Edit(mid)`、保存 `KnowledgeBaseSave(...)`、删除 `KnowledgeBaseDel(mid)`
    - 文件挂载/删除：
      - `AddFile(fileCode, fileName, KnowledgeBaseID)`：将上传文件映射为 `KnowledgeBaseFileInfo`
      - `KnowledgeBaseFileDel(mid, KnowledgeBaseID)`：删除文件与分块
    - 触发分块入库 `KnowledgeBaseFileToJob(KnowledgeBaseID, FileID)`：
      - 根据文件 MIME 推断 `ImportType`
      - 组装 `ImportKMSTaskReq` 与 `TaskInfo`（`NodeType.NotNode_FileChunk`），推入任务队列（由 AutoJob 执行）

- **其它模块（根据控制器命名）**
  - `AppController`、`AgentController`、`LargeModelController`、`PluginsController`、`MCPController`
  - `MemberManageController`、`UserManageController`、`DepartmentController`、`CompanyController`
  - `DictionaryController`（字典/标签）、`TagController`、`SystemLogController` 等
  - 以上控制器均通过 BLL 层进行数据读写，遵循相同的 View + JsonMsg 返回风格

## 运行与调试

- **本地运行**：
```powershell
dotnet restore
dotnet build -c Debug
dotnet run --project .\ZSN.AgentBrook.Web.Manage.csproj
```
- **访问**：
  - 后台登录：`/Manage/Authorize/Login`
  - 后台首页：`/Manage/Index/Index`
  - 工作流编辑：`/Manage/Workflow/Edit?id={WorkflowID}&MainID={MainID}&MainType={1|2}`
- **静态资源**：
  - `ueditor/` 目录自动映射为 `/ueditor`（若不存在会自动创建）

## 配置与安全

- `appsettings.json`：包含数据库、Redis、会话、`previewHost` 等配置项。建议：
  - 将敏感连接信息改为环境变量/机密管理，仓库中使用占位值
- Session 用于管理端登录态；必要时可调整 Cookie 策略与过期时间

## 协同与依赖

- 与 `ZSN.AgentBrook.API` 协同：
  - 文件预览使用 `previewHost` 指向 API 的文件接口
  - 工作流前端测试获取 Token/MemberToken（`CommonApiBaseController.GetTokenByAPPID`、`MemberTokenHelper.Set`）
- 与 `ZSN.AgentBrook.AutoJob` 协同：
  - 知识库文件分块入库通过 Job 触发（`NodeType.NotNode_FileChunk`）

## 常见问题

- **页面访问 404**：检查路由是否包含 `area` 前缀，如 `/Manage/Index/Index`。
- **无法登录**：确认数据库中的用户与加密逻辑（`UserInfoBussiness.GetUserEncryptionPassword`）一致，检查 Session 与 Cookie。
- **工作流保存失败**：检查提交的 `WorkFlow` 模型结构是否完整（`Info/Nodes/Edges/Config`）。

