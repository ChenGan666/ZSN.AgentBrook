# ZSN.AI.Plugins — 项目说明

> 路径：`w:\AI\ZSN.Knowbase\ZSN.Knowbase.Core\ZSN.AI.Plugins`

## 项目概览

- **定位**：面向 Agent/工作流与大模型（Semantic Kernel）的功能插件库。
- **当前实现**：
  - `Functions/BasePlugin.cs`：基础能力（时间/农历、触发 Agent/Workflow 执行）。
  - `Functions/HttpPlugin.cs`：HTTP 能力（GET/POST/文件上传 POST）。
- **集成方式**：可被 `FunctionService` 或 SK 插件机制加载，在节点/对话/任务执行中被调用。

## 技术栈与依赖

- **框架**：.NET 8（`net8.0`）
- **包**（见 `ZSN.AI.Plugins.csproj`）：
  - `Microsoft.SemanticKernel`
  - `SharpZipLib`、`System.Drawing.Common`、`System.Text.RegularExpressions`
- **项目引用**：
  - `ZSN.AI.BLL`、`ZSN.Utils.Core`

## 目录结构

- `Functions/BasePlugin.cs`
- `Functions/HttpPlugin.cs`

## BasePlugin（`Functions/BasePlugin.cs`）

- **标注**：`[Description("基础能力插件")]`，方法均使用 `[KernelFunction]`，可被 SK 调用。
- **能力**：
  - `get_date_time()`：返回当前日期时间（格式化）。
  - `date_to_chinese_traditional_calendar(date)`：公历转农历（参数格式 `yyyy-MM-dd`）。
  - `excution_agent(AppID, TaskID, FromMainTaskID, SessionID, ProcessesID, AgentNodeID, Inputs)`：
    - 读取 `AgentNodeID` 的节点配置，定位 Agent 的 `Start` 节点，构造 `outputs`，通过 `TaskInfoBussiness.toTask(...)` 投递新任务，返回 `NewTaskID`。
  - `excution_workflow(AppID, SessionID, ProcessesID, WorkFlowID, inputs)`：
    - 按 `WorkFlowID` 定位起始节点并投递任务，返回 `TaskID`。

## HttpPlugin（`Functions/HttpPlugin.cs`）

- **标注**：`[Description("Http能力插件")]`，方法均使用 `[KernelFunction]`。
- **通用能力**：
  - `HttpGet(url, headers?)`：发送 GET，可传入 JSON 字典的自定义请求头。
  - `HttpPost(url, postData, headers?)`：发送 POST，`postData` 为 JSON 文本。
  - `HttpFilePost(url, postData?, files?, headers?)`：发送 multipart/form-data，支持 JSON 数组 `{ fieldName, filePath }` 的文件上传。
- **实现要点**：
  - 共享 `HttpClient`（启用 Cookie，默认 30s 超时）。
  - 非 2xx 将抛出 `EnsureSuccessStatusCode()` 异常并序列化为 `{ success:false, error, type }`。

## 宿主集成

- 通过 `services.AddServicesFromAssemblies("ZSN.AI.Plugins")` 扫描注册。
- `FunctionService` 可注入该程序集，将 `[KernelFunction]` 能力暴露给 Agent/节点。
- 与下游配合：
  - `AutoJob.NodeJob` 执行时可间接触发插件（经节点/服务）。
  - `API/ChatController` → 工作流执行链路中亦可使用。

## 使用建议

- 以 SK 工具/函数方式调用，构造最小输入即可复用。
- `excution_agent/excution_workflow` 需确保传入 ID/上下文参数完整。
- `HttpFilePost` 请提供正确的文件绝对路径与字段名。
