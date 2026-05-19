[中文](README.md) | **English**

<div align="center">

# ZSN.AgentBrook

**Enterprise AI Agent Orchestration Platform**

Visual Workflow Orchestration · Multi-Model Agents · RAG Knowledge Base · MCP Tool Integration

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-1.74-0078D4?logo=microsoft)](https://github.com/microsoft/semantic-kernel)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

</div>

---

## Platform Highlights

- **Visual DAG Workflow Engine** — Drag-and-drop designer, 20+ node types, supporting conditional branching, parallel execution, human approval, and sub-workflow nesting
- **Voice Transcription Node** — Integrated speech recognition + LLM post-processing, FunASR local deployment, speaker diarization, multi-format output (SRT/VTT/JSON), automatic long-audio segmentation, hotword boosting
- **ClawAI Agent** — Plan-Execute-Reflect loop architecture, multi-layer memory system (short-term/long-term/episodic/personality/user profile), task decomposition and dynamic replanning
- **ServiceDesk Customer Service Node** — FunctionCall-driven integrated knowledge base retrieval + generation, supporting multi-turn dialogue, intent recognition, and information collection
- **Research Node** — Autonomous web research based on SearXNG search + Playwright web scraping, multi-round search-analyze-reflect iterations, automatic research report generation
- **RAG Knowledge Base** — Hybrid search combining vector retrieval + full-text search, supporting PDF/Word/Markdown and other document formats, automatic chunking and indexing, image recognition and image output support
- **MCP Tool Protocol** — Built-in MCP Server/Client for quick integration of external tools and data sources
- **Multi-Model Support** — OpenAI / Claude / DeepSeek / Ollama / Zhipu / Baidu and other mainstream models, configurable per node
- **Real-time Streaming Output** — Streaming response based on Redis Stream, real-time display of LLM generation process on the frontend
- **Browser Automation** — Playwright-based agent browser, supporting web page operations and data collection

---

## Architecture Overview

[![Architecture Overview](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/Platform.png)](https://agentbrook.com/)

### Data Flow

[![Data Flow](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/DataFlow.png)](https://agentbrook.com/)

### Core Module Dependencies

```
AI.Entity ────────────────────────────────────── (Base data models, zero dependencies)
   │
   ├── AI.DAL ──── AI.DAL.MySql / AI.DAL.Postgres (Data access abstraction)
   │      │
   │      └── AI.BLL ──────────────────────────── (Business logic layer)
   │             │
   │             ├── AI.Service ───────────────── (Application service layer)
   │             │
   │             └── AI.Node ◄── AI.Core ───────── (Workflow engine + AI core)
   │                  │              │
   │                  │              ├── AI.Plugins (Function plugins)
   │                  │              ├── AI.MCPClient (MCP client)
   │                  │              └── AI.Functions (Built-in functions)
   │                  │
   │                  └── AI.KnowledgeBase ──────── (Knowledge base service)
   │
   ├── AgentBrook.API ────────────────────────── (API gateway, connecting all modules)
   │
   ├── AgentBrook.AutoJob ────────────────────── (Background task scheduling)
   │
   ├── AgentBrook.Web / Web.Manage ───────────── (Frontend interfaces)
   │
   └── Utils.Core / LitJSON / Cache ──────────── (Common utility libraries)
```

---

## Project Structure

| Project | Description | Key Technologies |
|---|---|---|
| **ZSN.AI.Core** | AI core, Semantic Kernel wrapper, multi-model routing, streaming output | Semantic Kernel, Extensions.AI |
| **ZSN.AI.Node** | Workflow node execution engine, ClawAI / ServiceDesk / LLM and all nodes | SK FunctionCall, Pipeline |
| **ZSN.AI.Entity** | Data models, DTOs, enum definitions | SqlSugar annotations |
| **ZSN.AI.BLL** | Business logic layer: workflow management, task scheduling, knowledge base operations | |
| **ZSN.AI.DAL** | Data access abstraction interfaces | SqlSugar ORM |
| **ZSN.AI.DAL.MySql** | MySQL data access implementation | SqlSugar + MySQL |
| **ZSN.AI.DAL.Postgres** | PostgreSQL implementation (vector retrieval + knowledge graph) | Npgsql, pgvector, Apache AGE |
| **ZSN.AI.KnowledgeBase** | Knowledge base service: document import, chunking, indexing, semantic retrieval, image recognition | Npgsql, pgvector, Apache AGE |
| **ZSN.AI.MCPServer** | MCP tool server, exposing platform capabilities as MCP tools | ModelContextProtocol |
| **ZSN.AI.MCPClient** | MCP client, connecting to external MCP services | ModelContextProtocol |
| **ZSN.AI.Plugins** | Semantic Kernel function plugin collection | |
| **ZSN.AI.Functions** | Built-in function library | |
| **ZSN.AgentBrook.API** | REST API gateway, Swagger documentation | ASP.NET Core, SignalR |
| **ZSN.AgentBrook.Web** | Frontend interface (React + Ant Design Pro) | React, Ant Design Pro |
| **ZSN.AgentBrook.Web.Manage** | Admin dashboard (LayUI) | LayUI, jQuery |
| **ZSN.AgentBrook.AutoJob** | Background task scheduler, polling and executing workflow tasks | Quartz.NET |
| **ZSN.AgentBrook.Plugins** | Application-level plugins | |
| **ZSN.AgentBrowser** | AI browser automation | Playwright |
| **ZSN.Cache** | Distributed caching service | Redis, MemoryCache |
| **ZSN.Utils.Core** | Common utility library | log4net, NPOI |
| **LitJSON** | Lightweight JSON library | |

---

## Core Capabilities

### 1. Visual Workflow Engine

DAG (Directed Acyclic Graph) based visual workflow editor with drag-and-drop design:

[![Visual Workflow Engine](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/WorkFlow.png)](https://agentbrook.com/)


**Supports 20+ node types:**

| Category | Nodes |
|---|---|
| Flow Control | Start, End, AgentStart, AgentEnd |
| AI Reasoning | MainAI, LargeModel, ClawAI, ServiceDesk, Research, Voice |
| Knowledge Retrieval | KnowledgeBase, FileToMarkdown |
| Logic Routing | Selector (conditional branching), Merge (convergence), IntentionRecognition (intent recognition) |
| Tool Integration | MCP, Plugins, Agent (sub-workflow) |
| Human-Machine Collaboration | HumanInTheLoop (human approval), Reporter (report generation) |
| Triggers | TimeTrigger (scheduled trigger) |

### 2. ClawAI — Advanced Agent

ClawAI is the platform's core agent node, implementing a complete **Plan-Execute-Reflect** loop:

[![ClawAI Architecture Overview](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/ClawAI.png)](https://agentbrook.com/)


**Multi-layer Memory System:**

| Memory Layer | Purpose |
|---|---|
| Short-term Memory | Current conversation context, maintaining dialogue coherence |
| Episodic Memory | Historical event records, cross-session experience accumulation |
| Long-term Memory | Knowledge base retrieval results, persistent knowledge storage |
| User Profile | User preferences and behavioral characteristics, personalized service |
| AI Personality | AI role state, maintaining consistent interaction style |

**Multi-model Collaboration:** Each stage can be independently configured with a model (main model, planning model, reflection model, memory model, personality model), achieving flexible balance between cost and performance.

### 3. ServiceDesk — Customer Service Agent

A quick-response node for customer service scenarios, using FunctionCall to let the LLM autonomously call knowledge base retrieval:

[![ServiceDesk](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/ServiceDesk.png)](https://agentbrook.com/)


**Core Features:**
- FunctionCall-driven: LLM autonomously decides whether to retrieve from the knowledge base
- Multi-turn dialogue: Automatically maintains conversation context
- Information collection: Detects intent and automatically follows up on missing fields
- Confidence grading: High/Medium/Low confidence levels correspond to different processing strategies
- Source citation: Answers include knowledge base source annotations

### 4. RAG Knowledge Base

[![Knowledge Base Architecture Overview](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/KnowledgeBase.png)](https://agentbrook.com/)


- **Hybrid Retrieval**: Vector semantic search + full-text keyword search, fused ranking
- **Knowledge Graph**: Entity relationship graph based on Apache AGE
- **Multi-format Support**: PDF, Word, Markdown, TXT, HTML, etc.
- **Intelligent Chunking**: Semantic-aware document chunking strategy
- **Image Recognition**: Automatically extracts images from documents, generates image descriptions through VLM (Vision Language Model) with OCR text recognition, supports PDF/Word/PPT documents
- **Image Output**: Knowledge base search results support returning associated images, automatic image-text chunk association, hybrid image-text retrieval

**Image Processing Pipeline:**

```
Document Upload → Image Extraction (PDF/Word/PPT)
    → Content Deduplication (SHA256 hash)
    → VLM Description Generation (image description + OCR + tags)
    → Image Storage + Metadata Persistence
    → Automatic Image-Chunk Association
```

### 4.1 Research — Autonomous Research Node

The Research node is an autonomous web research engine that performs multi-round search, web scraping, analysis, and reflection based on research goals:

[![Research Architecture Overview](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/Research.png)](https://agentbrook.com/)


**Key Features:**
- **Dual-mode Scraping**: Playwright web scraping prioritized, automatic fallback to search snippet mode when unavailable
- **Multi-round Iteration**: Up to 3 search-analysis cycles, LLM autonomously plans keywords
- **Completeness Assessment**: Evaluates information coverage (0.0-1.0) after each analysis round, auto-stops when threshold is reached
- **LLM Call Budget**: Configurable maximum LLM call count to prevent cost overruns
- **Content Caching**: Redis-based web content caching to avoid redundant scraping
- **Timeout Protection**: Global timeout protection, returns collected content on timeout
- **Streaming Output**: Real-time streaming of research progress

### 4.2 Voice — Speech Transcription Node

The Voice node is an intelligent speech processing node that integrates speech recognition with LLM post-processing, supporting automatic structured text generation from audio files:

**Core Capabilities:**
- **Speech Transcription**: Localized speech recognition based on FunASR (WebSocket offline mode), data stays on your server
- **Speaker Diarization**: Automatically identifies different speakers, supports custom speaker label mapping
- **Multi-format Output**: Plain text, timestamped segment JSON, SRT subtitles, WebVTT subtitles
- **LLM Post-processing**: Transcription results are automatically fed to LLM, supporting custom prompts for text refinement, summarization, etc.
- **Long Audio Segmentation**: Audio exceeding the threshold (default 300s) is automatically segmented by silence detection, transcribed in parallel, then merged
- **Hotword Boosting**: Configurable hotword list to improve recognition accuracy for domain-specific vocabulary
- **Multi-format Input**: WAV, MP3, M4A, OGG, FLAC, AAC and 12 other audio/video formats, automatic FFmpeg conversion

**Processing Pipeline:**

```
Audio Input → Format Conversion (FFmpeg)
    → Long Audio VAD Segmentation (silencedetect)
    → FunASR WebSocket Transcription (chunked sending)
    → Speaker Label Mapping + Output Formatting
    → LLM Post-processing (optional)
    → Structured Result Output
```

**Configuration Example (appsettings.json):**

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

### 5. MCP Tool Integration

[![MCP Architecture Overview](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/MCP.png)](https://agentbrook.com/)

Built-in MCP Server and Client, supporting:
- Exposing platform capabilities (knowledge base retrieval, workflow triggering, etc.) as MCP tools
- Connecting to external MCP services to extend LLM tool capabilities
- Supporting bidirectional client/server invocation mode

### 6. Multi-Model Support

Connecting to multiple AI providers through a unified `IChatService` interface:

| Provider | Model Examples | Integration Method |
|---|---|---|
| OpenAI | GPT-4o, GPT-4, GPT-3.5 | OpenAI API |
| Anthropic | Claude series | OpenAI-compatible interface |
| DeepSeek | DeepSeek-V3/R1 | OpenAI-compatible interface |
| Ollama | Qwen, Llama, Mistral and other local models | Ollama API |
| Zhipu AI | GLM-4 | Zhipu API |
| Baidu | ERNIE Bot | Baidu API |
| Others | Any OpenAI-compatible interface | Custom EndPoint |

Each workflow node can be independently configured with model, Temperature, TopP, and other parameters.

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Runtime** | .NET 10 |
| **AI Framework** | Microsoft Semantic Kernel 1.74, Microsoft.Extensions.AI 10.4 |
| **MCP** | ModelContextProtocol 0.3 |
| **ORM** | SqlSugar 5.1 |
| **Database** | MySQL (primary), PostgreSQL + pgvector + Apache AGE (knowledge base) |
| **Cache** | Redis (StackExchange.Redis) |
| **Document Processing** | PdfPig, OpenXml, Markdig, ImageSharp |
| **Image Processing** | VLM image description, OCR text recognition, image-chunk association |
| **Task Scheduling** | Quartz.NET |
| **Frontend** | React + Ant Design Pro (user-facing), LayUI (admin) |
| **Browser Automation** | Playwright |
| **API Documentation** | Swagger / OpenAPI |

---

## Quick Start

### Prerequisites

- .NET 10 SDK
- MySQL 8.0+
- PostgreSQL 16+ (for knowledge base features, requires pgvector extension)
- Redis 7.0+

### Installation

```bash
# Clone the repository
git clone https://github.com/your-org/ZSN.AgentBrook.git
cd ZSN.AgentBrook

# Restore dependencies
dotnet restore ZSN.AI.sln
```

### Configuration

Edit `ZSN.AgentBrook.API/appsettings.json`:

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

### Running

```bash
# Start the API service
dotnet run --project ZSN.AgentBrook.API

# Start the background task scheduler (workflow execution)
dotnet run --project ZSN.AgentBrook.AutoJob

# Start the admin dashboard (optional)
dotnet run --project ZSN.AgentBrook.Web.Manage
```

---

## Demo

### Multi-Image Tweet Generation

[![Multi-Image Tweet Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E5%A4%9A%E5%9B%BE%E6%8E%A8%E6%96%87%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE1.png)](https://agentbrook.com/)

[![Multi-Image Tweet Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E5%A4%9A%E5%9B%BE%E6%8E%A8%E6%96%87%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE2.png)](https://agentbrook.com/)

[![Multi-Image Tweet Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E5%A4%9A%E5%9B%BE%E6%8E%A8%E6%96%87%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE3.png)](https://agentbrook.com/)

[![Multi-Image Tweet Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E5%A4%9A%E5%9B%BE%E6%8E%A8%E6%96%87%E7%94%9F%E6%88%90/%E5%B7%A5%E4%BD%9C%E6%B5%81.png)](https://agentbrook.com/)


### Picture Book Generation

[![Picture Book Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE1.png)](https://agentbrook.com/)

[![Picture Book Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE2.png)](https://agentbrook.com/)

[![Picture Book Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E5%B1%95%E7%A4%BA%E5%9B%BE3.png)](https://agentbrook.com/)

[![Picture Book Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E5%B7%A5%E4%BD%9C%E6%B5%81%E9%9B%86.png)](https://agentbrook.com/)

[![Picture Book Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E7%BB%98%E6%9C%AC%E6%96%87%E6%A1%88%E7%94%9F%E6%88%90%E5%B7%A5%E4%BD%9C%E6%B5%81.png)](https://agentbrook.com/)

[![Picture Book Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E8%8B%B1%E8%AF%ADAI%E6%8F%90%E7%A4%BA%E8%AF%8D%E7%94%9F%E6%88%90%E5%B7%A5%E4%BD%9C%E6%B5%81.png)](https://agentbrook.com/)

[![Picture Book Generation](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/%E7%BB%98%E6%9C%AC%E7%94%9F%E6%88%90/%E5%9B%BE%E7%89%87%E7%94%9F%E6%88%90%E5%B7%A5%E4%BD%9C%E6%B5%81.png)](https://agentbrook.com/)

### AI Customer Service - Knowledge Base

[![AI Customer Service](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/AI%E5%AE%A2%E6%9C%8D-%E7%9F%A5%E8%AF%86%E5%BA%93/%E5%B1%95%E7%A4%BA%E5%9B%BE1.png)](https://agentbrook.com/)

[![AI Customer Service](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/AI%E5%AE%A2%E6%9C%8D-%E7%9F%A5%E8%AF%86%E5%BA%93/%E5%B1%95%E7%A4%BA%E5%9B%BE2.png)](https://agentbrook.com/)

[![AI Customer Service](https://github.com/ChenGan666/ZSN.AgentBrook/blob/main/README/demo/AI%E5%AE%A2%E6%9C%8D-%E7%9F%A5%E8%AF%86%E5%BA%93/%E5%B7%A5%E4%BD%9C%E6%B5%81.png)](https://agentbrook.com/)

---

## License

This project is licensed under the [MIT License](LICENSE).
