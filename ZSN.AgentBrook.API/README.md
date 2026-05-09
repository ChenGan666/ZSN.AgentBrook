# ZSN.AgentBrook.API — 项目说明

> 路径：`w:\AI\ZSN.Knowbase\ZSN.Knowbase.Core\ZSN.AgentBrook.API`

## 项目概览

- **定位**：知识库/智能体后端 API 与简单页面（Razor Pages），包含认证、会话、聊天、文件、成员/组织、知识库等接口。
- **框架**：ASP.NET Core (`net8.0`)。
- **服务端口**：读取配置 `ServicePort`（默认 `5003`），Kestrel 监听 `IPAddress.Any`。
- **接口文档**：集成 Swagger，访问路径：`/doc`（分组：V1-Public、V1-Member、V1-User、V1-Manage）。
- **运行模式**：支持控制台运行与 Windows Service（Windows 环境）。

## 技术栈与依赖

- **运行时/框架**：.NET 8、ASP.NET Core MVC、Razor Pages、Session、CORS、Rewrite
- **包依赖**（节选，见 `ZSN.AgentBrook.API.csproj`）：
  - `Swashbuckle.AspNetCore`（Swagger）
  - `Magick.NET-Q8-AnyCPU`（图像处理）
  - `SharpZipLib`（压缩）
  - `Microsoft.Extensions.Hosting.WindowsServices`（Windows 服务托管）
- **项目依赖**（同解决方案内其他项目，需一并编译）：
  - `ZSN.AI.Core`、`ZSN.AI.Service`、`ZSN.AI.BLL`、`ZSN.AI.DAL*`、`ZSN.AI.Entity`、`ZSN.AI.Functions`、`ZSN.AI.Node`、`ZSN.AI.Plugins`、`ZSN.AgentBrook.Plugins`、`ZSN.Utils.Core`

## 目录结构

- **`Program.cs`**：主机构建。根据 `ServicePort` 启动 Kestrel；在 Windows 上启用 WindowsService；控制台日志仅输出 Error 级别至标准错误。
- **`Startup.cs`**：
  - 注册 CORS、Session、Razor Pages、Controllers
  - 扫描并注册服务：`services.AddServicesFromAssemblies("ZSN.AI.Core")`、`"ZSN.AI.Plugins"`
  - 单例：`FunctionService`、`TaskManager`
  - `AddSignalR()`（当前未见 Hub 映射）
  - JSON 序列化：Newtonsoft 忽略循环引用、非驼峰；System.Text.Json 禁用命名策略、全量 Unicode；注册 `DateTimeConverter`/`DateTimeNullableConvert`
  - 中间件：异常页、静态资源、Swagger、URL Rewrite、请求体重复读取缓冲、Routing、CORS、Session、Authorization、自定义初始化 `StartupHelper.ConfigureInit`
  - 终结点：默认路由 `{controller=Home}/{action=Index}/{id?}`
- **`Controllers/`**：主要 API 控制器
  - `ApiBaseController.cs`、`BaseController.cs`
  - `TokenController.cs`、`SessionController.cs`
  - `ChatController.cs`（聊天功能，文件较大）
  - `KnowledgeBaseController.cs`、`FileController.cs`
  - `MemberController.cs`、`ManageController.cs`、`AppController.cs`
  - `CompanyController.cs`、`DepartmentController.cs`、`StaffController.cs`
  - `TaskController.cs`
- **`Attributes/`**：自定义过滤器/特性：`APIChecker.cs`、`ApiRecoder.cs`、`MemberCheck.cs`
- **`ConfigureSwagger/`**：Swagger 配置与文件上传操作过滤器
- **`Helpers/`**：`SettingsService.cs`、`TaskManager.cs`
- **`Pages/`**：Razor Pages（`Index`、`Privacy`、`Error` 等）
- **`wwwroot/`**：静态资源
- **`Properties/launchSettings.json`**：本地调试配置
- **`appsettings.json`**：应用配置（数据库、Redis、端口、文件路径等）

## 配置说明（`appsettings.json` 摘要）

- **Logging**：日志级别
- **AllowedHosts**：主机限制
- **DbConnectionStrings**：
  - 多库：`BaseDb`、`MemberDb`、`LogBaseDb`、`ModelDb`、`AppDb`、`AgentDb`、`WorkflowDb`、`ChatDb`、`JobDb`、`ObjectDb`（MySQL）
  - `KnowledgeBaseDb`（Postgres，含 `TableNamePrefix: "km-"`）
- **应用参数**：
  - `AppID`、`SystemTitle`、`BaseUrl`
  - `ServicePort`（默认 `5003`）
  - `CheckSign`、`CheckTimestamp`、`AccessTokenTimeOut`、`TimestampTimeOut`、`SignInStepTimeOut`、`AccessStepTimeOut`
  - `SSE_TimeOut`
  - `RedisConnectionString`、`Redis.DefaultKey`
  - `FilePath`（文件上传根目录，例如 `W:/AI/ZSN.AI/publish/file`）
  - `UploadFileType`、`previewHost`
- 注意：`appsettings.json` 中含真实连接字符串与凭据，请勿在公共环境泄露。建议使用环境变量或 User Secrets 覆盖，并在仓库中改为占位值。

## 中间件与行为

- **CORS**：默认策略允许任意源、方法、头
- **Session**：Cookie 名 `ZSNAppSession`，过期 3600 秒，`HttpOnly=true`
- **URL Rewrite**：将 `api/File/Get/{filecode}/{w}/{h}` 重写为 `api/File/Get?filecode=$1&w=$2&h=$3`
- **请求体缓冲**：启用 `Request.EnableBuffering()` 以支持多次读取（解决基类构造函数预读导致 `[FromBody]` 为 null 的问题）
- **Swagger**：`/doc` 下多分组

## 运行与调试

- **前置要求**：
  - .NET SDK 8.0+
  - MySQL、Postgres、Redis 可用并与配置匹配
  - 同解决方案内被引用项目需可编译
- **本地运行**：
```powershell
dotnet restore
dotnet build -c Debug
dotnet run --project .\ZSN.AgentBrook.API.csproj
```
- **访问**：
  - Swagger 文档：`http://localhost:5003/doc`
  - 默认路由首页：`http://localhost:5003/`
- **端口**：修改 `appsettings.json` 的 `ServicePort` 或使用环境变量覆盖。
- **Windows Service**：在 Windows 环境下可作为服务托管（通过 `UseWindowsService()`）。发布与安装需按企业部署规范执行。

## 部署建议

- **配置外置**：将敏感连接串改为环境变量/密钥管理（如 Azure Key Vault），仓库内使用占位值。
- **发布**：使用 `dotnet publish` 或 `Properties/PublishProfiles/` 下配置。
- **安全**：生产环境为 Swagger `/doc` 添加访问控制。

## 常见问题

- **Swagger 打不开**：确认应用已启动且访问路径为 `/doc`，检查反向代理是否转发静态资源。
- **[FromBody] 解析为 null**：项目已启用请求体缓冲；若仍出现，检查基类读取逻辑与控制器绑定源。
- **跨域**：默认允许全部来源，生产建议改为白名单。

## 相关代码引用

- **主机构建**：`Program.cs` → `CreateHostBuilder()`、`ConfigHelper.GetInt("ServicePort")`
- **服务注册与中间件**：`Startup.cs` → `ConfigureServices()`、`Configure()`
- **Swagger 配置**：`ConfigureSwagger/ConfigureSwagger.cs`
- **控制器**：`Controllers/` 下各文件
- **自定义特性**：`Attributes/`（`APIChecker`、`ApiRecoder`、`MemberCheck`）
- **任务管理**：`Helpers/TaskManager.cs`
- **设置服务**：`Helpers/SettingsService.cs`

## 开发约定与扩展点

- **依赖注入**：`services.AddServicesFromAssemblies("ZSN.AI.Core"|"ZSN.AI.Plugins")` 自动扫描注册，新增服务请注意命名空间与可发现性。
- **JSON 约定**：系统范围内禁用驼峰、全量 Unicode 编码、时间格式 `yyyy-MM-dd HH:mm:ss`。
- **API 版本/分组**：Swagger 已分组；新增控制器时按现有分组约定更新配置。
- **文件上传/预览**：受 `FilePath`、`previewHost` 控制；上传类型在 `UploadFileType` 中配置。

## 接口功能说明（Controllers）

以下基于源码逐个控制器总结了路由、认证要求、请求参数与返回类型。默认前缀为 `api/[controller]/[action]`，POST 请求通常通过 `ApiBaseController.JsonObj` 解密 `PostData.Data` 后获取业务参数，返回统一包装 `JsonMsg<T>`。

- **加密入参**：请求体为 `application/json`，形如 `{ "Data": "<AES密文>" }`，服务端使用 AppKey/Token 解密。
- **统一返回**：`JsonMsg<T>`。
- **认证与签名特性**：
  - `APIChecker(...)`：控制 Token/Sign/Timestamp 校验。
  - `MemberCheck(...)`：控制成员令牌校验。

### 公用基类

- **`Controllers/ApiBaseController.cs`**
  - `JsonObj`：解析解密后的业务参数。
  - `CacheWrite`/`CacheValue`：按 `URL+Data` 的 MD5 做短期缓存。
  - `SaveFile(IFormFile)`：保存到 `FilePath/yyyy/MM/dd/HH/`，落库 `FilesInfo`，返回 `FileCode`（MD5）。
  - `GetFile(fileCode,w,h)`：图片自动缩放，其他类型原样返回。
  - `FormatFileCode(picCode)`：基于 `previewHost` 生成可预览 URL。

### AppController（应用）

- 路由：`api/App/{action}`，分组：`V1-Public`
- 接口：
  - `GET Index`：健康检查
  - `POST GetList`（`[APIChecker(Token=false)]`）：获取启用的应用列表 → `JsonMsg<List<AppInfo>>`
  - `POST Get`（`[APIChecker]`）：按 `AppID` 获取应用 → `JsonMsg<AppInfo>`

### BaseController（基础信息）

- 路由：`api/Base/{action}`，分组：`V1-Public`
- 接口：
  - `GET Index`
  - `POST Get`（`[APIChecker]`）：返回 `BaseInfo`（公司信息去除 `SecretKey`，应用列表、标签分类及个人知识库统计）

### ChatController（聊天与流程）

- 路由：`api/Chat/{action}`，分组：`V1-Member`
- 依赖：`IChatService`、`TaskManager`、`IServiceProvider`
- 接口：
  - `GET Index`
  - `POST GetList`（`[MemberCheck(Token=true, MemberToken=true)]`）：按 `sessionID` 获取会话消息 → `JsonMsg<List<AppChatLogInfo>>`
  - `POST GetSummaryList`（同上）：会话摘要 → `JsonMsg<List<AppChatSummaryInfo>>`
  - `POST completions`（同上）：生成对话，支持 SSE
    - 入参：`stream?:bool`、`messages:GptMsg`、`sessionID?:string`、`appid:string`、`SSE_TimeOut?:number`
    - 返回：`SSE text/event-stream` 或 `JsonMsg<IReadOnlyList<MessageData>>`
  - `POST ExecuteNode`（同上）：单节点调试 → `JsonMsg<MessageData>`
  - `POST ReExecuteNode`（同上）：将指定节点重置入队 → `JsonMsg<MessageData>`
  - `POST GetNodeExcutionRecord`（同上）：获取节点执行过程（SSE/非流式）
  - `POST GetMCPTools`（同上）：按 `MCPConfig` 获取 MCP 工具简化列表 → `JsonMsg<List<object>>`
  - `POST execHumanInTheLoop`（同上）：人工介入执行；Query 含 `sessionID, taskID, recordID`

### CompanyController（公司）

- 路由：`api/Company/{action}`，分组：`V1-Manage`
- 接口：
  - `GET Index`
  - `POST Save`（`[APIChecker]`）：保存/更新公司信息 → `JsonMsg<CompanyInfo>`

### DepartmentController（部门）

- 路由：`api/Department/{action}`，查询在 `V1-Public`，写在 `V1-Manage`
- 接口：
  - `GET Index`
  - `POST GetList`（`[APIChecker(Token=false)]`）：部门列表 → `JsonMsg<List<DepartmentInfo>>`
  - `POST Get`（`[APIChecker(Token=false)]`）：按 `DepartmentID` 获取 → `JsonMsg<DepartmentInfo>`
  - `POST Save`（`[MemberCheck(Token=true, MemberToken=true)]`）：新建/更新 → `JsonMsg<DepartmentInfo>`
  - `POST State`（`[MemberCheck]`）：更新状态 → `JsonMsg<DepartmentInfo>`
  - `POST Delete`（`[MemberCheck]`）：删除 → `JsonMsg<string>`

### FileController（文件）

- 路由：`api/File/{action}`，分组：`V1-User`
- 接口：
  - `POST Upload`（`multipart/form-data`，`[MemberCheck]`）：表单字段 `Data`（含 `FileMd5`），多文件上传；返回 `{ FileCode, Url }` → `JsonMsg<FileInfo>`
  - `GET Get`（`[MemberCheck(MemberToken=false, Token=false, Sign=false, Timestamp=false)]`）：按 `fileCode` 获取文件（图片可缩略）

### KnowledgeBaseController（知识库）

- 路由：`api/KnowledgeBase/{action}`，分组：`V1-Public`
- 接口：
  - `GET Index`
  - `POST Get`（`[APIChecker]`）：按 `KnowledgeBaseID` 获取 → `JsonMsg<KnowledgeBaseInfo>`
  - `POST GetList`（`[APIChecker]`）：分页与标签筛选 → `JsonMsg<PageData<List<KnowledgeBaseInfo>>>`
  - `POST GetTagList`（`[APIChecker]`）：标签列表 → `JsonMsg<List<KnowledgeBaseTagInfo>>`
  - `POST GetFileList`（`[APIChecker]`）：知识库文件分页 → `JsonMsg<PageData<List<KnowledgeBaseFileInfo>>>`

### ManageController（管理示例）

- 路由：`api/Manage/{action}`，分组：`V1-Manage`
- 接口：
  - `GET Index`
  - `POST CompanySave`（`[APIChecker]`）：保存公司信息（与 `CompanyController.Save` 类似）

### MemberController（会员）

- 路由：`api/Member/{action}`，分组：`V1-Member`
- 接口：
  - `GET Index`
  - `POST Get`（`[MemberCheck]`）：获取当前成员信息 → `JsonMsg<MemberInfo>`
  - `POST Save`（`[MemberCheck]`）：更新昵称和密码 → `JsonMsg<MemberInfo>`

### SessionController（会话）

- 路由：`api/Session/{action}`，分组：`V1-Member`
- 接口：
  - `GET Index`
  - `POST GetList`（`[APIChecker]`）：当前成员会话分页 → `JsonMsg<PageData<List<AppChatSessionInfo>>>`
  - `POST Delete`（`[APIChecker]`）：按 `sessionID` 删除 → `JsonMsg<string>`
  - `POST CleanUp`（`[APIChecker]`）：清空当前成员会话 → `JsonMsg<string>`

### StaffController（员工）

- 路由：`api/Staff/{action}`，分组：`V1-Manage`
- 接口：
  - `GET Index`
  - `POST Save`（`[APIChecker]`）：新建/更新员工 → `JsonMsg<StaffInfo>`
  - `POST GetList`（`[APIChecker(Token=false)]`）：员工列表 → `JsonMsg<List<StaffInfo>>`
  - `POST Get`（`[APIChecker(Token=false)]`）：按 `StaffID` 获取 → `JsonMsg<StaffInfo>`
  - `POST State`（`[APIChecker]`）：更新状态 → `JsonMsg<StaffInfo>`
  - `POST Delete`（`[APIChecker]`）：删除 → `JsonMsg<string>`

### TaskController（任务与回调）

- 路由：`api/Task/{action}`，分组：`V1-Public`
- 接口：
  - `GET Index`
  - `POST ReCall`（`[MemberCheck(Token=false, MemberToken=false, Sign=false, Timestamp=false)]`）：工作流回调；Query：`sessionID, taskID, recordID` → `JsonMsg<string>`
  - `execHumanInTheLoop(...)`：内部方法，依据人工选项驱动下一节点
