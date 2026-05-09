# ZSN.AI.MCPServer — 项目说明

> 路径：`w:\AI\ZSN.Knowbase\ZSN.Knowbase.Core\ZSN.AI.MCPServer`

## 项目概览

- **定位**：基于 Model Context Protocol (MCP) 的 HTTP 服务端，提供文件转 Markdown/图片、知识库写入等工具能力，并暴露 Swagger 文档与传统 HTTP API。
- **框架**：ASP.NET Core (`net8.0`) + MCP Server（`ModelContextProtocol.AspNetCore`）。
- **端口**：读取 `appsettings.json` 的 `ServicePort`（默认 `5008`），Kestrel 监听 `IPAddress.Any`。
- **接口文档**：Swagger 路径：`/doc`（分组：V1-Public）。
- **MCP 端点**：默认映射 `/mcp`（`Startup.Configure()` 中 `endpoints.MapMcp()`）。

## 技术栈与依赖

- **包**（见 `ZSN.AI.MCPServer.csproj`）
  - MCP：`ModelContextProtocol`、`ModelContextProtocol.AspNetCore`、`ModelContextProtocol.Core`
  - 文件转换：`Pandoc`（包装器，实际需本地安装 Pandoc/LibreOffice/Ghostscript）
  - Web：`Swashbuckle.AspNetCore`、`Microsoft.Extensions.Hosting.WindowsServices`
  - 工具：`Magick.NET-Q8-AnyCPU`、`SharpZipLib`
- **项目引用**：
  - `ZSN.AI.Core`、`ZSN.AI.Service`、`ZSN.AI.BLL`、`ZSN.AI.Entity`、`ZSN.AI.Functions`、`ZSN.AI.Node`、`ZSN.Utils.Core`
  - `ZSN.AI.DAL*`（MySql、基础 DAL）

## 启动与中间件（`Startup.cs`）

- **DI 与服务**：
  - CORS 默认允许任意源/方法/头
  - `AddDistributedMemoryCache()` + `AddSession()`（`ZSNAppSession`，3600s）
  - MVC/Razor、SignalR
  - 全局 JSON：
    - Newtonsoft：忽略循环引用、非驼峰、日期格式 `yyyy-MM-dd HH:mm:ss`
    - System.Text.Json：禁用命名策略、全量 Unicode、日期转换器，附加宽松布尔值转换器（兼容 MCP 字符串布尔）
  - 自动扫描服务：`services.AddServicesFromAssemblies("ZSN.AI.Core"|"ZSN.AI.Plugins")`
  - `FunctionService` 单例（载入 `ZSN.AI.Plugins.BasePlugin`）
  - MCP 服务：`services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly()`
- **管道**：
  - `UseForwardedHeaders()`（兼容 Nginx 反代）
  - `UseSwagger()` + `UseSwaggerUI("/doc")`
  - 静态文件 + URL Rewrite：`/api/File/Get/{filecode}/{w}/{h}` → `api/File/Get?filecode=...`
  - 请求体缓冲 `EnableBuffering()`
  - `UseRouting()`、`UseCors()`、`UseAuthorization()`、`UseSession()`
  - 端点：MVC 默认路由、`endpoints.MapMcp()`

## 配置说明（`appsettings.json` 摘要）

- **端口与对外地址**：`ServicePort:5008`、`ExternalUrl`。
- **数据库**：`LogBaseDb`、`JobDb`、`ObjectDb`、`ModelDb`（MySQL）、`KnowledgeBaseDb`（Postgres，`TableNamePrefix: "km-"`）。
- **文件转换**：`FileConversion.MediaDir`、`TempDirectory`、`PandocPath`。
- **多模态模型（VLLLMConfig）**：示例 `qwen2.5vl:7b`（Ollama）。
- 注意：包含真实连接信息，生产请改为环境变量/机密管理，仓库中使用占位值。

## MCP 配置（`mcp-config.json`）

- **服务器**：`zsn-knowbase-mcp`（HTTP、`http://localhost:5008/mcp`，tools 能力开启）。
- **工具（tools）**：
  - `MCPTest` → `GET /MCPTest`：连通性与参数测试
  - `FileToMarkdown` → `POST /FileToMarkdown`（multipart/form-data）：通用文件→Markdown（支持图片抽取）
  - `FileToImage` → `POST /FileToImage`（multipart/form-data）：文件→图片
  - `ToMarkdownSync` → `POST /ToMarkdown/sync`
  - `ToMarkdownAsync` → `POST /ToMarkdown/async`（可携带 `callbackUrl`）
  - `KnowledgeBase` → `POST /KnowledgeBase`：保存 file/url/text/qa-excel 到知识库
- **依赖声明**：
  - 必需：Pandoc 3.8+、LibreOffice 25.8+、Ghostscript 10.06+
  - 可选：PostgreSQL（知识库）、MySQL（任务管理）

## 控制器与端点（节选）

- **`Controllers/MCPTestController.cs`**：`GET /MCPTest`，参数 `message/number/flag`，返回回显与随机信息。
- **`Controllers/FileToMarkdownController.cs`**：`POST /FileToMarkdown/Convert`，调用 Pandoc 转 Markdown，收集媒体文件信息并清理临时目录，返回 `{ Content, OriginalFileName, HasMedia, MediaFiles[] }`。

## 运行与测试

- **本地运行**：
```powershell
dotnet restore
dotnet build -c Debug
dotnet run --project .\ZSN.AI.MCPServer.csproj
```
- **访问**：
  - Swagger：`http://localhost:5008/doc`
  - MCP 端点：`http://localhost:5008/mcp`
- **测试脚本**：`test-mcp.ps1`；客户端配置参考 `mcp-config.json`、`claude-mcp-config.json`。

## 环境依赖与安装提示

- 安装并在 PATH 可用：Pandoc、LibreOffice、Ghostscript。
- Windows 服务托管：支持 `UseWindowsService()`（发布为服务需另行配置）。

## 协同与依赖

- 与 `ZSN.AgentBrook.API`/`AutoJob`/`Web.Manage` 协同：共享文件预览规范与转换/入库链路，可互为能力提供方。

## 安全与建议

- 使用环境变量/机密管理替换 `appsettings.json` 中的敏感连接。
- 生产环境建议限制 Swagger `/doc` 访问，或置于内网。
