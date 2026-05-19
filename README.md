**中文** | [English](README_EN.md)

<div align="center">

# ZSN.AgentBrook

**企业级 AI 智能体编排平台**

可视化工作流编排 · 多模型智能体 · RAG 知识库 · MCP 工具集成

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Semantic Kernel](https://img.shields.io/badge/Semantic_Kernel-1.74-0078D4?logo=microsoft)](https://github.com/microsoft/semantic-kernel)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![pgvector](https://img.shields.io/badge/pgvector-0.7-4169E1?logo=postgresql)](https://github.com/pgvector/pgvector)
[![Apache AGE](https://img.shields.io/badge/Apache_AGE-1.1-4169E1?logo=apache)](https://github.com/apache/age)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?logo=mysql)](https://www.mysql.com/)
[![Redis](https://img.shields.io/badge/Redis-5.0-DC382D?logo=redis)](https://redis.io/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)](https://react.dev/)
[![Playwright](https://img.shields.io/badge/Playwright-1.x-2EAD33?logo=playwright)](https://playwright.dev/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)
[![FunASR](https://img.shields.io/badge/FunASR-0.4.7-FF6A00)](https://github.com/modelscope/FunASR)
[![SearXNG](https://img.shields.io/badge/SearXNG-Latest-1D4ED8)](https://github.com/searxng/searxng)
[![Quartz.NET](https://img.shields.io/badge/Quartz.NET-3.x-512BD4)](https://www.quartz-scheduler.net/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

</div>

---

## 平台亮点

- **可视化 DAG 工作流引擎** — 拖拽式设计器，20+ 节点类型，支持条件分支、并行执行、人工审批、子工作流嵌套
- **Voice 语音转写节点** — 语音识别 + LLM 后处理一体化，支持 FunASR 本地部署，说话人分离、多格式输出（SRT/VTT/JSON）、长音频自动分段、热词增强
- **ClawAI 智能体** — Plan-Execute-Reflect 循环架构，多层记忆系统（短期/长期/情景/人格/用户画像），支持任务分解与动态重规划
- **ServiceDesk 客服节点** — FunctionCall 驱动的知识库检索 + 生成一体化，支持多轮对话、意图识别、信息收集
- **Research 研究节点** — 自主网络研究，基于 SearXNG 搜索 + Playwright 网页抓取，多轮搜索-分析-反思迭代，自动生成研究报告
- **RAG 知识库** — 向量检索 + 全文检索混合搜索，支持 PDF/Word/Markdown 等多种文档格式，自动分块与索引，支持图片识别与图片输出
- **MCP 工具协议** — 内置 MCP Server/Client，快速接入外部工具和数据源
- **多模型支持** — OpenAI / Claude / DeepSeek / Ollama / 智谱 / 百度 等主流模型，可按节点独立配置
- **实时流式输出** — 基于 Redis Stream 的流式响应，前端实时展示 LLM 生成过程
- **浏览器自动化** — 基于 Playwright 的 Agent 浏览器，支持网页操作与数据采集

---

## 架构总览

[![架构总览](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/Platform.png)](https://agentbrook.com/)

### 数据流

[![数据流](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/DataFlow.png)](https://agentbrook.com/)

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
| **ZSN.AI.KnowledgeBase** | 知识库服务：文档导入、分块、索引、语义检索、图片识别 | Npgsql, pgvector, Apache AGE |
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
| AI 推理 | MainAI, LargeModel, ClawAI, ServiceDesk, Research, Voice |
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
- **图片识别**：自动提取文档中的图片，通过 VLM（视觉语言模型）生成图片描述、OCR 文字识别，支持 PDF/Word/PPT 文档
- **图片输出**：知识库检索结果支持返回关联图片，图片与文本分块自动关联，支持混合图文检索

**图片处理管线：**

```
文档上传 → 图片提取（PDF/Word/PPT）
    → 内容去重（SHA256 哈希）
    → VLM 描述生成（图片描述 + OCR + 标签）
    → 图片存储 + 元数据入库
    → 图片-分块自动关联
```

### 4.1 Research — 自主研究节点

Research 节点是一个自主网络研究引擎，能够根据研究目标自动进行多轮搜索、网页抓取、分析和反思：

[![Research架构总览](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/Research.png)](https://agentbrook.com/)


**关键特性：**
- **双模式抓取**：Playwright 网页抓取优先，不可用时自动降级为搜索摘要模式
- **多轮迭代**：最多 3 轮搜索-分析循环，LLM 自动规划关键词
- **完整度评估**：每轮分析后评估信息覆盖度（0.0-1.0），达到阈值后自动停止
- **LLM 调用预算**：可配置最大 LLM 调用次数，防止成本失控
- **内容缓存**：基于 Redis 的网页内容缓存，避免重复抓取
- **超时保护**：全局超时保护，超时返回已获取内容
- **流式输出**：实时流式推送研究进度

### 4.2 Voice — 语音转写节点

Voice 节点是集语音识别与 LLM 后处理于一体的智能语音处理节点，支持从音频文件自动生成结构化文本：

**核心能力：**
- **语音转写**：基于 FunASR（WebSocket 离线模式）的本地化语音识别，数据不出服务器
- **说话人分离**：自动识别不同发言人，支持自定义说话人标签映射
- **多格式输出**：纯文本、带时间戳分段 JSON、SRT 字幕、WebVTT 字幕
- **LLM 后处理**：转写结果自动接入 LLM，支持自定义提示词进行文本整理、摘要生成等
- **长音频分段**：超过阈值（默认 300 秒）的音频自动按静音检测分段，并行转写后合并
- **热词增强**：支持配置热词列表，提升特定领域词汇识别率
- **多格式输入**：WAV、MP3、M4A、OGG、FLAC、AAC 等 12 种音频/视频格式，FFmpeg 自动转换

**处理流程：**

```
音频输入 → 格式转换（FFmpeg）
    → 长音频 VAD 分段（silencedetect）
    → FunASR WebSocket 转写（分片发送）
    → 说话人标签映射 + 输出格式化
    → LLM 后处理（可选）
    → 结构化结果输出
```

**配置示例（appsettings.json）：**

```json
{
  "VoiceNodeOptions": {
    "DefaultProvider": "FunASR",
    "MaxConcurrentSegments": 4,
    "MaxFileSizeMb": 500,
    "AutoSegmentThresholdSeconds": 300,
    "TempFileDirectory": "",
    "FFmpegPath": ""
  },
  "FunASROptions": {
    "ServerUrl": "ws://127.0.0.1:10095",
    "ChunkSize": 9600,
    "ConnectTimeoutSeconds": 5,
    "TranscribeTimeoutMinutes": 10
  }
}
```

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
| **文档处理** | PdfPig, OpenXml, Markdig, ImageSharp |
| **图片处理** | VLM 图片描述, OCR 文字识别, 图片-分块关联 |
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

---

## Demo

### 多图推文生成

[![多图推文生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E5%A4%9A%E5%9B%BE%E6%8E%A8%E6%96%87%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE1.png)](https://agentbrook.com/)

[![多图推文生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E5%A4%9A%E5%9B%BE%E6%8E%A8%E6%96%87%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE2.png)](https://agentbrook.com/)

[![多图推文生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E5%A4%9A%E5%9B%BE%E6%8E%A8%E6%96%87%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE3.png)](https://agentbrook.com/)

[![多图推文生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E5%A4%9A%E5%9B%BE%E6%8E%A8%E6%96%87%E7%94%9F%E6%88%90/%E5%B7%A5%E4%BD%9C%E6%B5%81.png)](https://agentbrook.com/)


### 绘本生成

[![绘本生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE1.png)](https://agentbrook.com/)

[![绘本生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE2.png)](https://agentbrook.com/)

[![绘本生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE3.png)](https://agentbrook.com/)

[![绘本生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E5%B7%A5%E4%BD%9C%E6%B5%81%E9%9B%86.png)](https://agentbrook.com/)

[![绘本生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E7%BB%98%E6%9C%AC%E6%96%87%E6%A1%88%E7%94%9F%E6%88%90%E5%B7%A5%E4%BD%9C%E6%B5%81.png)](https://agentbrook.com/)

[![绘本生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E8%8B%B1%E8%AF%ADAI%E6%8F%90%E7%A4%BA%E8%AF%8D%E7%94%9F%E6%88%90%E5%B7%A5%E4%BD%9C%E6%B5%81.png)](https://agentbrook.com/)

[![绘本生成](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E5%9B%BE%E7%89%87%E7%94%9F%E6%88%90%E5%B7%A5%E4%BD%9C%E6%B5%81.png)](https://agentbrook.com/)

### AI客服-知识库

[![AI客服](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/AI%E5%AE%A2%E6%9C%8D-%E7%9F%A5%E8%AF%86%E5%BA%93/%E5%B1%95%E7%A4%BA%E5%9B%BE1.png)](https://agentbrook.com/)

[![AI客服](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/AI%E5%AE%A2%E6%9C%8D-%E7%9F%A5%E8%AF%86%E5%BA%93/%E5%B1%95%E7%A4%BA%E5%9B%BE2.png)](https://agentbrook.com/)

[![AI客服](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/AI%E5%AE%A2%E6%9C%8D-%E7%9F%A5%E8%AF%86%E5%BA%93/%E5%B7%A5%E4%BD%9C%E6%B5%81.png)](https://agentbrook.com/)

---

## 许可证

本项目基于 [MIT License](LICENSE) 开源。