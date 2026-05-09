# ZSN.AI.Node — 项目说明

> 快速导航: [`READ_ME_FIRST.md`](./READ_ME_FIRST.md) | ClawAI 模块: [`Claw/README.md`](./Claw/README.md) | 功能迭代记录: [`MD/README.md`](./MD/README.md)

## 项目概览

- **定位**：工作流节点编排与执行核心库，负责节点模型定义、默认工作流初始化、节点实例化与执行记录写入等。
- **被谁使用**：
  - `ZSN.AgentBrook.AutoJob` 中的 `Job/NodeJob.cs` 调用本库完成各节点类型的实际执行。
  - `ZSN.AgentBrook.API` 的对话/节点调试接口创建任务，由 AutoJob 执行，底层依赖本库。

## 技术栈与依赖

- **框架**：.NET 8（`net8.0`）
- **项目引用**（见 `ZSN.AI.Node.csproj`）：
  - `ZSN.AI.Core`（核心类型、接口、DTO）
  - `ZSN.AI.BLL`（数据访问）
  - `ZSN.Utils.Core`（通用工具）
- **第三方**：`SharpZipLib`

## 目录结构

```
ZSN.AI.Node/
├── Execution.cs               ← 节点执行入口（各节点类型分发）
├── ExecutionClaw.cs           ← ClawAI 核心执行器（规划-执行-反思循环）
├── BaseExecution.cs           ← 执行器基类
├── Utils.cs                   ← 工作流/节点构造、执行记录写入
├── ServiceProviderHolder.cs   ← 全局 ServiceProvider 持有
├── Claw/                      ← ClawAI 智能体模块
│   ├── README.md              ← Claw 模块详细文档（流程图/配置/数据库）
│   ├── Configuration/         ← IOptions 配置类
│   ├── Pipeline/              ← 执行管线处理器（模型初始化/上下文加载/快速路径/规划）
│   ├── Services/              ← 业务服务（13个：规划/反思/记忆/主控/个性/知识图谱等）
│   ├── Interfaces/            ← 服务接口（9个）
│   ├── Models/                ← 数据模型
│   ├── Utils/                 ← 工具类（日志/正则/问候语检测）
│   ├── Helpers/               ← 辅助类（记忆去重/记忆工具）
│   └── Analysis/              ← 分析器（任务复杂度/文本相似度/WorkFlow匹配）
└── MD/
    └── README.md              ← 功能迭代完整记录
```

## 核心类型与概念（源于 `ZSN.AI.Entity`，本库进行使用）

- **工作流结构**：`WorkFlow`（`Info` + `Nodes` + `Edges` + `Config`）、`WorkflowNodeInfo`、`WorkflowEdgeInfo`、`NodeConfig`（`id/mainid/workflowid/type/data/position...`）
- **常见节点数据结构**：`StartData`、`EndData`、`MainAIData`、`LargeModelData`、`AgentData`、`ReporterData`、`KnowledgeBaseData`、`SelectorData`、`MergeData`、`MCPData`、`TimeTriggerData`、`FileToMarkdownData`、`HumanInTheLoopData`、`IntentionRecognitionData`、`Inputs`、`Output`
- **任务与执行**：`TaskInfo`、`TaskConfig`（`NodeConfig` 或 `NotNodeConfig` + `TaskData`）、`LoopType/TaskState` 等；执行记录：`WorkflowNodeExecutionRecordInfo`

## 默认工作流与节点构造（`Utils.cs`）

- **`Utils.initWorkFlow(string MainID, MainType MainType)`**
  - 初始化默认工作流骨架：
    - APP：`MainAI`、`TimeTrigger`、`Agent`、`Reporter`、`Start`、`End`。
    - Agent：`AgentStart`、`AgentEnd`。
  - 自动填充 `NodeConfig`、默认 `Inputs/Output` 与模型参数。

- **`Utils.newNode(string WorkflowID, NodeType nodeType, string MainID)`**
  - 按 `nodeType` 生成 `WorkflowNodeInfo` 与默认 `NodeConfig.data`。
  - 支持：`Start/AgentStart/End/AgentEnd/MainAI/LargeModel/Agent/Reporter/KnowledgeBase/Plugins/Selector/Merge/MCP/Notepaper/FileToMarkdown/HumanInTheLoop/IntentionRecognition/TimeTrigger`。

- **执行记录与附件辅助**
  - `newExcutionRecord(sessionId, currentNode, processesId, taskId, nextNodeId)`: 新建执行记录，返回 `RecordID`。
  - `updateExcutionRecord(recordId, status, outputs, logs)`: 更新执行记录状态/输出/日志。
  - `updateAttachmentItemsFilePath(attachments)`: 由 `FileCode` 补全本地 `FilePath`。
  - `AttachmentToChatHistoryAsync(attachments, history)`: 将附件加入 `SemanticKernel.ChatHistory`（图片/二进制）。

## 节点执行器（`Execution.cs`）

外部主要通过以下方法驱动节点（在 `ZSN.AgentBrook.AutoJob/Job/NodeJob.cs` 中按类型分发）：

- `StartNode`、`AgentStartNode`
- `EndNodeAsync`、`AgentEndNodeAsync`
- `LargeModelNodeAsync`、`MainAINodeAsync`
- `ClawAINodeAsync`（ClawAI 智能体节点，详见 [`Claw/README.md`](./Claw/README.md)）
- `AgentNodeAsync`、`PluginsNodeAsync`、`KnowledgeBaseNodeAsync`
- `SelectorNodeAsync`、`MergeNodeAsync`、`MCPNodeAsync`
- `FileToMarkdownNode`、`HumanInTheLoopNode`、`IntentionRecognitionNodeAsync`

## 执行链路（与 AutoJob 配合）

```
ZSN.AgentBrook.API 创建任务（写 TaskInfo）
  → ZSN.AgentBrook.AutoJob 的 NodeJob 轮询任务
    → 依据 TaskInfo.TaskConfig.NodeConfig.type 调用 Execution 对应方法
      → 写回 TaskInfo.Results/State 与执行记录
```

- 流式/回调由上层负责，本库专注节点执行与记录写入

## 常见节点行为提示

- **Start/AgentStart**：标准化输入（`input/attachments/additionalOptions/currentTime` 等）。
- **MainAI/LargeModel**：根据模型与提示词执行对话/补全。
- **ClawAI**：智能体节点 — 智能主控判断 → 任务规划 → 并行执行 → 反思评估 → 动态重规划（详见 [`Claw/README.md`](./Claw/README.md)）。
- **Agent**：运行 Agent 能力（输出 `agentName/currentTime` 等）。
- **Reporter**：会话摘要抽取（与 `AutoJob.AIDispatcher` 配合）。
- **FileToMarkdown**：配合 `AutoJob.MarkdownJob` 或 `MCPServer` 做文件→Markdown 与图片抽取。
- **MCP**：通过 MCP 工具调用外部服务（文件转换、知识库等）。
- **TimeTrigger**：时间事件驱动（与 `AutoJob.TimeTrigger` 协同）。
- **HumanInTheLoop**：人工介入（选项驱动不同分支）。
- **IntentionRecognition/Selector/Merge**：意图识别与路由/聚合。

## 协同与依赖

- **API**：任务投递、SSE、人工回调。
- **AutoJob**：调度并发与执行控制。
- **MCPServer**：外部工具能力（文件转换/知识库入库）。
- **Web.Manage**：工作流编辑/复制/发布（调用 `Utils.initWorkFlow/newNode`）。

## 运行与测试建议

- 单元/集成测试建议围绕：
  - `Utils.newNode` 输出结构完整性
  - `initWorkFlow` 默认骨架与坐标位置
  - 执行记录增改（`newExcutionRecord/updateExcutionRecord`）
- 若引入新节点：在 `Utils.newNode` 与 `Execution` 同步扩展，保证编辑器/执行器一致。
