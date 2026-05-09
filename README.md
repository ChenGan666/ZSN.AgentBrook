<div align="center">

# ZSN.AgentBrook

**企业级 AI 智能体编排平台**

可视化工作流编排 · 多模型智能体 · RAG 知识库 · MCP 工具集成

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-1.74-0078D4?logo=microsoft)](https://github.com/microsoft/semantic-kernel)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

</div>

---

## 平台亮点

- **可视化 DAG 工作流引擎** — 拖拽式设计器，20+ 节点类型，支持条件分支、并行执行、人工审批、子工作流嵌套
- **ClawAI 智能体** — Plan-Execute-Reflect 循环架构，多层记忆系统（短期/长期/情景/人格/用户画像），支持任务分解与动态重规划
- **ServiceDesk 客服节点** — FunctionCall 驱动的知识库检索 + 生成一体化，支持多轮对话、意图识别、信息收集
- **RAG 知识库** — 向量检索 + 全文检索混合搜索，支持 PDF/Word/Markdown 等多种文档格式，自动分块与索引
- **MCP 工具协议** — 内置 MCP Server/Client，快速接入外部工具和数据源
- **多模型支持** — OpenAI / Claude / DeepSeek / Ollama / 智谱 / 百度 等主流模型，可按节点独立配置
- **实时流式输出** — 基于 Redis Stream 的流式响应，前端实时展示 LLM 生成过程
- **浏览器自动化** — 基于 Playwright 的 Agent 浏览器，支持网页操作与数据采集

---

## 架构总览

[![架构总览](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/Platform.png)](https://agentbrook.com/)


### 核心模块依赖关系

```
AI.Entity ────────────────────────────────────── (基础数据模型，零依赖)
   │
   ├── AI.DAL ──── AI.DAL.MySql / AI.DAL.Postgres (数据访问抽象)
   │      │
   │      └── AI.BLL ──────────────────────────── (业务逻辑层)
   │             │
   │             ├── AI.Service ───────────────── (应用服务层)
   │             │
   │             └── AI.Node ◄── AI.Core ───────── (工作流引擎 + AI 内核)
   │                  │              │
   │                  │              ├── AI.Plugins (函数插件)
   │                  │              ├── AI.MCPClient (MCP 客户端)
   │                  │              └── AI.Functions (内置函数)
   │                  │
   │                  └── AI.KnowledgeBase ──────── (知识库服务)
   │
   ├── AgentBrook.API ────────────────────────── (API 网关，串联所有模块)
   │
   ├── AgentBrook.AutoJob ────────────────────── (后台任务调度)
   │
   ├── AgentBrook.Web / Web.Manage ───────────── (前端界面)
   │
   └── Utils.Core / LitJSON / Cache ──────────── (通用工具库)
```

---

## 项目结构

| 项目 | 说明 | 关键技术 |
|---|---|---|
| **ZSN.AI.Core** | AI 内核，Semantic Kernel 封装，多模型路由，流式输出 | Semantic Kernel, Extensions.AI |
| **ZSN.AI.Node** | 工作流节点执行引擎，ClawAI / ServiceDesk / LLM 等全部节点 | SK FunctionCall, Pipeline |
| **ZSN.AI.Entity** | 数据模型、DTO、枚举定义 | SqlSugar 注解 |
| **ZSN.AI.BLL** | 业务逻辑层：工作流管理、任务调度、知识库操作 | |
| **ZSN.AI.DAL** | 数据访问抽象接口 | SqlSugar ORM |
| **ZSN.AI.DAL.MySql** | MySQL 数据访问实现 | SqlSugar + MySQL |
| **ZSN.AI.DAL.Postgres** | PostgreSQL 实现（向量检索 + 知识图谱） | Npgsql, pgvector, Apache AGE |
| **ZSN.AI.KnowledgeBase** | 知识库服务：文档导入、分块、索引、语义检索 | Kernel Memory, pgvector |
| **ZSN.AI.MCPServer** | MCP 工具服务器，将平台能力暴露为 MCP 工具 | ModelContextProtocol |
| **ZSN.AI.MCPClient** | MCP 客户端，连接外部 MCP 服务 | ModelContextProtocol |
| **ZSN.AI.Plugins** | Semantic Kernel 函数插件集合 | |
| **ZSN.AI.Functions** | 内置函数库 | |
| **ZSN.AgentBrook.API** | REST API 网关，Swagger 文档 | ASP.NET Core, SignalR |
| **ZSN.AgentBrook.Web** | 前端界面（React + Ant Design Pro） | React, Ant Design Pro |
| **ZSN.AgentBrook.Web.Manage** | 管理后台（LayUI） | LayUI, jQuery |
| **ZSN.AgentBrook.AutoJob** | 后台任务调度器，轮询执行工作流任务 | Quartz.NET |
| **ZSN.AgentBrook.Plugins** | 应用级插件 | |
| **ZSN.AgentBrowser** | AI 浏览器自动化 | Playwright |
| **ZSN.Cache** | 分布式缓存服务 | Redis, MemoryCache |
| **ZSN.Utils.Core** | 通用工具库 | log4net, NPOI |
| **LitJSON** | 轻量 JSON 库 | |

---

## 核心能力

### 1. 可视化工作流引擎

基于 DAG（有向无环图）的可视化工作流编辑器，支持拖拽设计：

[![可视化工作流引擎](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/WorkFlow.png)](https://agentbrook.com/)


**支持 20+ 节点类型：**

| 类别 | 节点 |
|---|---|
| 流程控制 | Start, End, AgentStart, AgentEnd |
| AI 推理 | MainAI, LargeModel, ClawAI, ServiceDesk |
| 知识检索 | KnowledgeBase, FileToMarkdown |
| 逻辑路由 | Selector（条件分支）, Merge（汇聚）, IntentionRecognition（意图识别） |
| 工具集成 | MCP, Plugins, Agent（子工作流） |
| 人机协作 | HumanInTheLoop（人工审批）, Reporter（报告生成） |
| 触发器 | TimeTrigger（定时触发） |

### 2. ClawAI — 高级智能体

ClawAI 是平台的核心智能体节点，实现了完整的 **Plan-Execute-Reflect** 循环：

[![ClawAI架构总览](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/ClawAI.png)](https://agentbrook.com/)


**多层记忆系统：**

| 记忆层 | 作用 |
|---|---|
| 短期记忆 | 当前会话上下文，保持对话连贯性 |
| 情景记忆 | 历史事件记录，跨会话经验积累 |
| 长期记忆 | 知识库检索结果，持久化知识存储 |
| 用户画像 | 用户偏好与行为特征，个性化服务 |
| AI 人格 | AI 角色状态，维持一致的交互风格 |

**多模型协作：** 每个环节可独立配置模型（主模型、规划模型、反思模型、记忆模型、人格模型），实现成本与效果的灵活平衡。

### 3. ServiceDesk — 客服智能体

面向客服场景的快速响应节点，通过 FunctionCall 让 LLM 自主调用知识库检索：

[![ServiceDesk](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/ServiceDesk.png)](https://agentbrook.com/)


**核心特性：**
- FunctionCall 驱动：LLM 自主决定是否检索知识库
- 多轮对话：自动维护会话上下文
- 信息收集：检测意图后自动追问缺失字段
- 置信度分级：高/中/低置信度对应不同处理策略
- 来源引用：回答附带知识库来源标注

### 4. RAG 知识库

[![知识库架构总览](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/KnowledgeBase.png)](https://agentbrook.com/)


- **混合检索**：向量语义搜索 + 全文关键词搜索，融合排序
- **知识图谱**：基于 Apache AGE 的实体关系图谱
- **多格式支持**：PDF、Word、Markdown、TXT、HTML 等
- **智能分块**：语义感知的文档分块策略

### 5. MCP 工具集成

[![MCP架构总览](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/MCP.png)](https://agentbrook.com/)

内置 MCP Server 和 Client，支持：
- 将平台能力（知识库检索、工作流触发等）暴露为 MCP 工具
- 连接外部 MCP 服务，扩展 LLM 工具能力
- 支持客户端/服务端双向调用模式

### 6. 多模型支持

通过统一的 `IChatService` 接口对接多种 AI 提供商：

| 提供商 | 模型示例 | 接入方式 |
|---|---|---|
| OpenAI | GPT-4o, GPT-4, GPT-3.5 | OpenAI API |
| Anthropic | Claude 系列 | OpenAI 兼容接口 |
| DeepSeek | DeepSeek-V3/R1 | OpenAI 兼容接口 |
| Ollama | Qwen, Llama, Mistral 等本地模型 | Ollama API |
| 智谱 AI | GLM-4 | 智谱 API |
| 百度 | 文心一言 | 百度 API |
| 其他 | 任何 OpenAI 兼容接口 | 自定义 EndPoint |

每个工作流节点可独立配置模型、Temperature、TopP 等参数。

---

## 技术栈

| 层级 | 技术 |
|---|---|
| **运行时** | .NET 10 |
| **AI 框架** | Microsoft Semantic Kernel 1.74, Microsoft.Extensions.AI 10.4 |
| **MCP** | ModelContextProtocol 0.3 |
| **ORM** | SqlSugar 5.1 |
| **数据库** | MySQL（主库）, PostgreSQL + pgvector + Apache AGE（知识库） |
| **缓存** | Redis (StackExchange.Redis) |
| **文档处理** | Kernel Memory, PdfPig, OpenXml, Markdig |
| **任务调度** | Quartz.NET |
| **前端** | React + Ant Design Pro（用户端）, LayUI（管理端） |
| **浏览器自动化** | Playwright |
| **API 文档** | Swagger / OpenAPI |

---

## 快速开始

### 环境要求

- .NET 10 SDK
- MySQL 8.0+
- PostgreSQL 16+（知识库功能，需安装 pgvector 扩展）
- Redis 7.0+

### 安装步骤

```bash
# 克隆仓库
git clone https://github.com/your-org/ZSN.AgentBrook.git
cd ZSN.AgentBrook

# 还原依赖
dotnet restore ZSN.AI.sln
```

### 配置

编辑 `ZSN.AgentBrook.API/appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=zsn_ai;Uid=root;Pwd=your_password;",
    "PostgresConnection": "Host=localhost;Database=zsn_kb;Username=postgres;Password=your_password;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "LLM": {
    "DefaultModelId": 1
  }
}
```

### 运行

```bash
# 启动 API 服务
dotnet run --project ZSN.AgentBrook.API

# 启动后台任务调度（工作流执行）
dotnet run --project ZSN.AgentBrook.AutoJob

# 启动管理后台（可选）
dotnet run --project ZSN.AgentBrook.Web.Manage
```

### 数据流

[![数据流](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/DataFlow.png)](https://agentbrook.com/)


---

## 许可证

本项目基于 [MIT License](LICENSE) 开源。
