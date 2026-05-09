# ZSN.AI.Service — 项目说明

> 路径：`w:\AI\ZSN.Knowbase\ZSN.Knowbase.Core\ZSN.AI.Service`

## 项目概览

- **定位**：Web 层的通用服务库，提供 Controller 基类、签名/会话/日志等 Web 助手、过滤器与属性，用于 API/Web 端统一接入规范（参数读取、Token 解析、统一返回、签名校验、记录日志等）。
- **被依赖**：`ZSN.AgentBrook.API`、`ZSN.AgentBrook.Web.Manage`、`ZSN.AI.MCPServer` 等。

## 技术栈与依赖

- **框架**：.NET 8（`net8.0`）
- **包**（见 `ZSN.AI.Service.csproj`）：
  - `Microsoft.AspNetCore.Mvc.NewtonsoftJson`、`Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation`
  - `Swashbuckle.AspNetCore.SwaggerGen`
  - `NLog`、`SharpZipLib`、`System.Drawing.Common`、`System.Text.RegularExpressions`
- **项目引用**：`ZSN.AI.BLL`、`ZSN.AI.DAL`、`ZSN.AI.Entity`、`ZSN.Utils.Core`

## 目录结构

- `Controllers/`
  - `CommonBaseController.cs`：MVC 基类，提供 `BuildSuccessResult(...)`、`BuildFailResult(...)` 统一 JSON 返回。
  - `CommonApiBaseController.cs`：API 基类，负责：
    - 请求体 `BodyParams` 读取（支持多次读取，配合上层 `EnableBuffering()`）。
    - 解析请求头 `bearer/memberbearer` 为 `Token/MemberToken`，并尝试写入 `Session`。
    - 生成/解析 `AccessToken`、`MemberToken`（基于 `DesEncrypt/DesDecrypt` 与时间戳）。
    - 提供 `GetErrorResult(...)` 标准错误 JSON。
- `Attributes/`：常用过滤/属性
  - `SqlRecoder.cs`：SQL 记录器特性（用于基类上，便于记录行为）。
  - `HiddenApiFilter.cs`：隐藏/跳过某些 API 的特性。
  - `RunTime.cs`：方法运行时长统计等。
- `Helpers/`：辅助工具
  - `ApiSignHelper.cs`：签名与参数校验相关。
  - `FileHelper.cs`、`NpoiHelper.cs`：文件/NPOI 相关工具。
- `WebHelpers/`：Web 辅助
  - `DefaultLogService.cs`：统一日志写入。
  - `UserService.cs`：用户上下文/登录态封装（`Session/Cookies`）。
  - `DictionaryHelper.cs`、`DictionarySessionHelper.cs`：字典/缓存辅助。
- `Base/StartupHelper.cs`：在上层应用中统一注册/初始化帮助方法（`ServicesInit(...)`、`ConfigureInit(...)`）。
- 其它：`Common/`、`Filters/`、`Token/`、`Expander/`（按需扩展）。

## 关键能力

- **统一参数读取**：`CommonApiBaseController.BodyParams` 在构造阶段即读取请求体并放入 `Session`，解决基类/过滤器提前读取导致 `[FromBody]` 为空的问题。
- **统一鉴权上下文**：自动从请求头提取 `Token/MemberToken`，并提供 `GetUserIdByToken(...)` / `GetMemberIdByToken(...)` 解析方法与过期策略（基于配置 `AccessTokenTimeOut`）。
- **统一返回格式**：
  - 成功：`BuildSuccessResult(data, type)` → `{ status:true, success:true, data, ... }`
  - 失败：`BuildFailResult(...)`、`GetErrorResult(...)`。
- **签名/日志**：配合 `ApiSignHelper` 与若干过滤器实现签名校验与请求/SQL 记录。

## 在上层项目中的用法

- 控制器继承：
  - API 层：`class XxxController : CommonApiBaseController`
  - Web 层：`class XxxController : CommonBaseController`
- Program/Startup：
  - 启用请求体缓冲：`context.Request.EnableBuffering()`，保证多次读取 Body。
  - 通过 `StartupHelper.ServicesInit(...)` 与 `StartupHelper.ConfigureInit(...)` 完成统一注册与中间件初始化。

## 示例引用

- **统一错误 JSON**：`CommonApiBaseController.GetErrorResult(ErrorCode.DataEmpty)`
- **Token 解析**：`CommonApiBaseController.GetUserIdByToken(token, out userID, out ts)`
- **设置登录态**：`UserService.SetLoginRemember(userId)`、`HttpContext.Session.SetString(...)`

## 建议

- 生产环境建议将签名/Token 超时、日志级别放入配置中心或环境变量。
- 若新增统一行为（追踪/灰度/限流），优先通过过滤器或基类扩展实现，保持入口一致。
