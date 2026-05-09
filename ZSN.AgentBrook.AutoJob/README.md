# ZSN.AgentBrook.AutoJob — 项目说明

> 路径：`w:\AI\ZSN.Knowbase\ZSN.Knowbase.Core\ZSN.AgentBrook.AutoJob`

## 项目概览

- **定位**：基于 Quartz 的后台任务调度服务，承载 Agent/工作流执行、文件转换与知识库入库、会话主题识别、系统清理等作业。
- **宿主模式**：使用 Topshelf 作为 Windows 服务宿主，可安装/卸载为系统服务。
- **调度框架**：Quartz（支持固定间隔与 Cron 表达式）。
- **依赖生态**：与 API 项目共享 BLL/Entity/Core/Service/Node/Plugins 等域模型与服务。

## 技术栈与依赖

- **框架**：.NET 8（`net8.0`）
- **关键包**（见 `ZSN.AgentBrook.AutoJob.csproj`）：
  - `Quartz`、`Quartz.Extensions.DependencyInjection`
  - `Microsoft.Extensions.Hosting`
  - `Topshelf`（Windows 服务宿主）
  - `Magick.NET-Q8-AnyCPU`、`SharpZipLib`
- **项目引用**：
  - `ZSN.AI.Core`、`ZSN.AI.Service`、`ZSN.AI.BLL`、`ZSN.AI.Entity`、`ZSN.AI.Functions`、`ZSN.AI.Node`
  - `ZSN.AI.DAL*`（MySql/Postgres）、`ZSN.AI.Plugins`、`ZSN.AgentBrook.Plugins`
  - `ZSN.Utils.Core`

## 目录结构

- **`Program.cs`**：Topshelf 入口，注册 DI 与 Quartz，启动宿主服务 `QuartzHostedService`。
- **`QuartzHostedService.cs`**：读取配置 `Job` 数组，映射 Job 类型，注册 Trigger 并启动 Scheduler。
- **`JobFactory.cs`**：自定义 `IJobFactory`，从 DI 容器解析 Job 实例。
- **`Job/`**：各类任务实现（详见下文“Job 说明”）。
- **`appsettings.json`**：数据库、Redis、文件转换、回调地址与 Jobs 调度配置。
- **`plugins/`**：预留插件目录（如 `plugins/SemanticFunction/`）。

## 启动与运行

- **本地运行**：
```powershell
dotnet restore
dotnet build -c Debug
dotnet run --project .\ZSN.AgentBrook.AutoJob.csproj
```
- **Windows 服务**（在产物目录执行，需管理员权限）：
```powershell
ZSN.AgentBrook.AutoJob.exe install
ZSN.AgentBrook.AutoJob.exe start
# 停止/卸载
ZSN.AgentBrook.AutoJob.exe stop
ZSN.AgentBrook.AutoJob.exe uninstall
```
- 启动日志：`NLogHelper.WriteInfo("定时任务启动！")`（控制台/日志）。

## 调度机制

- **配置来源**：`appsettings.json` → `Job` 数组，每项形如：
```json
{
  "JobName": "Node",
  "LoopTimerSeconds": 1
}
```
- **两种调度方式**（见 `QuartzHostedService.StartAsync()`）：
  - 固定间隔：`LoopTimerSeconds > 0` → `WithSimpleSchedule().WithIntervalInSeconds(...).RepeatForever()`
  - Cron 表达式：`LoopTimerSeconds <= 0` 且提供 `WithCronSchedule`（如每日 3 点：`"0 0 3 * * ?"`）
- **Job 名称与类型映射**（`QuartzHostedService.JobMappings`）：
  - `TimeTrigger` → `TimeTrigger`
  - `AIDispatcher` → `AIDispatcher`
  - `FileChunk` → `FileChunkJob`
  - `Node` → `NodeJob`
  - `SessionTopic` → `SessionTopicJob`
  - `Markdown` → `MarkdownJob`
  - `Cleaner` → `CleanerJob`

## 配置说明（`appsettings.json` 摘要）

- **数据库**：`DbConnectionStrings`（多库 MySQL + `KnowledgeBaseDb` Postgres，`TableNamePrefix: "km-"`）
- **Redis/Garnet**：`RedisConnectionString`、`GarnetConnectionString`
- **文件转换**：`FileConversion`（`MediaDir`、`TempDirectory`、`PandocPath`）
- **多模态模型**：`VLLLMConfig`（示例为 Ollama `qwen2.5vl:7b`）
- **回调地址**：
  - `FileToMarkdownReCallUrl`
  - `HumanOperationReCallUrl`
- **Jobs 配置**：`Job` 数组（默认启用 `Node`，每 1 秒轮询）

> 注意：该配置文件包含真实连接信息，建议在生产中改为环境变量或机密管理并在仓库中使用占位值。

## 依赖注入与服务注册（`Program.cs`）

- **自动扫描注册**：`services.AddServicesFromAssemblies("ZSN.AI.Core" | "ZSN.AI.Plugins" | "ZSN.AI.Functions" | "ZSN.AgentBrook.Plugins" | "ZSN.AgentBrook.AutoJob")`
- **函数服务**：两次注入 `FunctionService`，分别载入 `ZSN.AI.Plugins.BasePlugin` 与 `ZSN.AI.Plugins.Functions.HttpPlugin` 程序集
- **注册 Jobs 到 DI**：`AIDispatcher`、`TimeTrigger`、`FileChunkJob`、`NodeJob`、`SessionTopicJob`、`MarkdownJob`、`CleanerJob`
- **启动 HostedService**：`QuartzHostedService`

## Job 说明（`Job/`）

- **通用基类**：`Job/Job.cs` → `JobBase`（`ErrorId = 308` 用于统一日志编号）

- **TimeTrigger（时间触发）** — `Job/TimeTrigger.cs`
  - 拉取 `NodeType.TimeTrigger` 任务，按 `LoopType/IntervalValue` 判定触发（支持秒/日/周/月以及一次性）。
  - 命中后构造输出 `prompt/currentTime`，根据出边创建下一节点任务：`TaskInfoBussiness.toTask(...)`。
  - 正确处理 `RepeatValue` 与 `RedoCount` 以决定下次等待/完成。

- **AIDispatcher（AI 记录员/摘要）** — `Job/AIDispatcher.cs`
  - 拉取 `NodeType.Reporter` 的任务。
  - 聚合会话历史 `AppChatLogInfo` → `ChatHistory`，加入系统提示 `reporter.prompt`，调用 `_chatService.HistorySummarize(...)` 产出摘要。
  - 生成 `AppChatSummaryInfo`（含关联的 `ChatLogIDList`），任务置为 `Completed`。

- **FileChunk（文件分块入库）** — `Job/FileChunkJob.cs`
  - 拉取 `NotNode_FileChunk` 任务，解析 `FileChunkConfig`。
  - 调用 `IImportKMSService.ImportKMSTask(importKMSTask)` 执行知识库分块入库，更新 `KnowledgeBaseFileInfo`，任务置 `Completed`。

- **Markdown（文件转 Markdown）** — `Job/MarkdownJob.cs`
  - 拉取 `NotNode_Markdown` 任务，解析 `MarkdownConfig`。
  - 构造 `FileConverts`，对 `sourceFile[]` 调用 `ToMarkdownFilesAsync(...)`。
  - 结果集合写入 `task.Results` 并置 `Completed`；如配置 `reCallUrl`，以 HTTP POST 回调结果（含日志记录）。

- **Node（工作流节点执行）** — `Job/NodeJob.cs`
  - 拉取多种节点类型任务：`Start/AgentStart/End/AgentEnd/LargeModel/Agent/Plugins/MainAI/Selector/KnowledgeBase/Merge/MCP/FileToMarkdown/HumanInTheLoop/IntentionRecognition`
  - 并发控制：全局 `_semaphore` 控制取任务，内部 `SemaphoreSlim(100)` 控制并行处理上限。
  - 调用 `ZSN.AI.Node.Excution` 各节点处理方法，成功置 `Completed`，失败写 `Results=Exception` 并日志。
  - **并发安全（2026-04-11 修复）**：同一 Session 下并发执行多个相同工作流时，各流程实例通过 `ProcessesID` 隔离数据。ClawAI 步骤级 WorkFlow 调用使用 `$"{ProcessesID}_{step.StepID}"` 子任务 ID，防止步骤间数据污染。详见 `ZSN.AI.Node/KNOWLEDGE_BASE_HYBRID_SEARCH_SUMMARY.md` 中的并发修复章节。

- **SessionTopic（会话主题生成）** — `Job/SessionTopicJob.cs`
  - 拉取近 3 分钟内活跃会话（`SystemStatus=0`）作为任务。
  - 获取会话历史，提示词要求 “15 字内主题”，调用 `_chatService.HistorySummarize(...)`，将结果写回 `AppChatSessionInfo.TopicSummary`。

- **Cleaner（清理任务）** — `Job/CleanerJob.cs`
  - 定时清理历史数据：
    - 30 天前系统日志 `LogRecordBusiness`
    - 30 天前节点执行记录 `WorkflowNodeExecutionRecordInfoBussiness`
    - 30 天前已完成的非循环任务 `TaskInfoBussiness`（`LoopType=0`）

## 与 API 项目的协同

- `FileToMarkdownReCallUrl`、`HumanOperationReCallUrl` 指向 API 的 `TaskController.ReCall` 接口（见 `ZSN.AgentBrook.API`），完成节点异步回调联动。
- `previewHost` 用于拼接文件预览 URI，和 API 的 `FileController.Get` 配合。

## 常见问题

- **Job 不执行**：检查 `appsettings.json` 的 `Job` 条目是否启用、`LoopTimerSeconds` 或 Cron 是否合理；确认 Quartz 启动成功（控制台输出）。
- **任务找不到依赖**：AutoJob 依赖其他项目（Core/BLL/Entity/Node 等），需确保解决方案整体可编译。
- **文件转换失败**：检查 `FileConversion.PandocPath` 与多模态模型配置 `VLLLMConfig` 可用性；查看 `MarkdownJob` 的回调日志。
- **回调不通**：确认 API 项目已启动，`TaskController.ReCall` 路径与查询字符串参数正确。

## 安全与配置建议

- 将 `appsettings.json` 中的数据库、Redis、模型与回调地址改为环境变量/机密管理；仓库中使用占位符。
- 按需限制 Job 并发（`NodeJob` 的并发上限可调整），避免对下游服务造成压力。
