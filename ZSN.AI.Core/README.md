# ZSN.AI.Core — 项目说明

> 路径：`w:\AI\ZSN.Knowbase\ZSN.Knowbase.Core\ZSN.AI.Core`

## 项目概览

- **定位**：AI 核心能力与领域模型库，沉淀通用接口、服务抽象、仓储实现、通用工具与集成（Semantic Kernel、Kernel Memory、MCP 等）的基础能力。
- **被依赖**：`ZSN.AgentBrook.API`、`ZSN.AgentBrook.AutoJob`、`ZSN.AgentBrook.Web.Manage`、`ZSN.AI.MCPServer`、`ZSN.AI.Plugins` 等上层项目。

## 技术栈与依赖

- **框架**：.NET 8（`net8.0`）
- **包**（见 `ZSN.AI.Core.csproj`）：
  - 文本/Markdown：`Markdig`
  - 记忆/向量库：`Microsoft.KernelMemory.*`（Core、OpenAI、Ollama、Postgres MemoryDb）
  - 大模型/编排：`Microsoft.SemanticKernel*`
  - 协议：`ModelContextProtocol`
  - 数据/工具：`SqlSugarCore`、`Newtonsoft.Json`、`RestSharp`、`SharpZipLib`、`pythonnet`
- **项目引用**：`ZSN.AI.BLL`、`ZSN.AI.MCPClient`、`ZSN.AI.Plugins`、`ZSN.Utils.Core`

## 目录结构

- `Common/`
  - `DependencyInjection/`：依赖注入扩展与注册辅助（供上层 `services.AddServicesFromAssemblies(...)` 使用）。
  - `Excel/`：Excel 相关通用处理。
  - `Bge/`：BGE 模型/词表等资源（构建时剔除）。
- `Interface/`：核心接口定义（服务/能力抽象，供上层注入与实现）。
- `Repositories/`：仓储层实现（基于 `SqlSugarCore` 的数据访问封装）。
- `Service/`：服务层（对外暴露的核心能力与业务编排）。
- `Utils/`：通用工具（时间/JSON/文件/转换等）与扩展适配。
- `Handler/`：处理器/钩子（如事件、消息、回调相关）。

## 能力边界与协作

- **接口抽象（`Interface/`）**：定义如会话、执行、知识库、模型调用等核心接口，供 API/AutoJob/MCPServer/Plugins 统一编程。
- **仓储封装（`Repositories/`）**：对底层数据表的查询/写入进行统一封装，屏蔽 ORM 细节，配合实体层（`ZSN.AI.Entity`）。
- **服务编排（`Service/`）**：承接上层需求，组合仓储与第三方（SK、KernelMemory、MCP 等）实现可复用服务。
- **工具集合（`Utils/`）**：常用类型转换、序列化、参数约束、文件/流处理、第三方 SDK 适配等。
- **依赖注入（`Common/DependencyInjection/`）**：提供面向程序集的批量扫描/注册扩展，支持上层项目在 `Startup/Program` 中统一载入。

## 与上层项目的协同

- **API**：控制器注入并消费 Core 的接口/服务能力，完成对外 HTTP API。
- **AutoJob**：任务执行调度中使用 Core 的服务与仓储（如节点执行依赖的接口实现）。
- **Web.Manage**：后台管理调用 Core 服务进行列表/编辑/保存；工作流编辑器依赖 Core 的模型。
- **MCPServer/Plugins**：MCP 工具与 SK 插件复用 Core 提供的通用能力与抽象。

## 配置与文档

- 生成的 API 文档注释见 `ZSN.AI.Core.xml`（构建产物），便于上层引用查看注释。
- BGE 资源在构建中被排除（见 csproj 的 Remove 配置），如需启用请按需调整。

## 使用建议

- **统一注入**：在上层项目中优先通过 `services.AddServicesFromAssemblies("ZSN.AI.Core")` 等扩展进行注册，避免重复手写注册。
- **新增能力**：
  - 在 `Interface/` 定义接口 → `Service/` 提供实现 → `Repositories/` 读写数据（如需）。
  - 在 `Utils/` 补充通用工具，避免散落在上层项目。
- **跨项目复用**：尽量将通用逻辑下沉至 Core，保持 API/AutoJob/Web.Manage 薄层。

## 版本与兼容

- 注意 `SemanticKernel` 与 `KernelMemory` 的版本兼容性；升级前建议在本地联调 API/AutoJob/MCPServer 的关键链路（对话、执行、转换、知识库写入等）。

## Service 目录详细说明

- **`ChatService.cs`**
  - 职责：对话与工具调用编排。
  - 关键能力：
    - `SendChatAsync(ModelConfig, history, CallFunction?)`：与 `Semantic Kernel` 聊天补全；按需导入 `KernelFunction` 与 MCP 工具；自动或仅返回客户端 JSON 指令。
    - `SendKmsAsync(KnowledgeBaseUnits, ChatModel, questions, history)`：汇总知识库召回并组织为提示上下文。
    - `GetChatHistory(...)`：将 `AppChatLogInfo/AppChatSummaryInfo` 转为 `ChatHistory`，支持图片/二进制附件组合消息。
    - `HistorySummarize(...)`、`FunctionCall(...)`、`PromptFunctionCall(...)`、`GetMcpClientToolsAsync(...)`。

- **`KernelService.cs`**
  - 职责：集中管理 `Semantic Kernel` 与模型绑定/函数装载。
  - 关键能力：
    - `GetKernel(...)`/`GetKernelByAIModelID(...)`：按模型或 ID 构建 Kernel。
    - `ImportFunctions(...)`：装载内置/原生/提示函数或 MCP 工具至 Kernel。
    - `HistorySummarize(Kernel, history)`：对话摘要。

- **`KMService.cs`**
  - 职责：知识记忆（Kernel Memory）相关封装。
  - 关键能力：`GetRelevantSourceList(LargeModelUnit, question, knowledgeBaseId)`，为 `ChatService.SendKmsAsync(...)` 提供文档召回。

- **`ImportKMSService.cs`**
  - 职责：知识库导入流程（文件/URL/文本/QA-Excel）。
  - 典型链路：`Plugins.KnowledgeBasePlugin.Save(...)`、`AutoJob.FileChunkJob` → `ImportKMSTask(...)` → 更新 `KnowledgeBaseFileInfo/Chunk`（配合回调）。

- **`FunctionService.cs`**
  - 职责：将程序集中的 `[KernelFunction]`/插件装载为 Kernel 可调用函数，供 `KernelService` 导入使用。
  - 典型用法：在上层 `Startup/Program` 注入后，与 `KernelService.ImportFunctions(...)` 配合把 `ZSN.AI.Plugins` 等动态挂载。

- **`HttpService.cs`**
  - 职责：HTTP 调用轻量封装（内部复用）。

- **`OllamaService.cs`**
  - 职责：Ollama 推理服务适配，统一模型接入（配合 `KernelMemory.AI.Ollama`）。

### 交互关系与调用链

- **【对话链路】** `ChatService.SendChatAsync()` → `KernelService.GetKernel()` → 导入函数/MCP 工具 → `IChatCompletionService.GetChatMessageContentAsync(...)`。
- **【工具导入】** `ChatService/KernelService` → `FunctionService` 扫描并挂载 `[KernelFunction]` 或 MCP 工具。
- **【知识库】** `ChatService.SendKmsAsync()` → `KMService.GetRelevantSourceList(...)` → 拼上下文 → 补全生成。
- **【入库】** 上游（Plugins/AutoJob）→ `ImportKMSService.ImportKMSTask(...)` → 更新 `KnowledgeBaseFileInfo/Chunk`。
