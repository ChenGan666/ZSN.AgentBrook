# VoiceNode 语音处理节点

## 概述

VoiceNode 是工作流中的语音文件处理节点（NodeType = 27），负责将音频/视频文件转写为文本，支持多人发言识别标注，并通过提示词驱动 LLM 完成内容摘要、分析、会议纪要生成等后处理任务。

**实施日期**: 2026-05-18 | **状态**: 已完成（Phase 1 - FunASR Provider + 核心转写能力）

---

## 外部依赖与环境要求

### 1. FunASR Server（必需）

语音转写核心服务，阿里端到端语音识别工具链的运行时服务。

**部署方式**：Docker 容器化部署

```bash
# CPU 版本
docker run -d \
  --name funasr-server \
  -p 10095:10095 \
  -p 10096:10096 \
  registry.cn-hangzhou.aliyuncs.com/funasr_repo/funasr:funasr-runtime-sdk-online-cpu-0.1.12

# GPU 版本（推荐，推理速度更快）
docker run -d \
  --name funasr-server \
  --gpus all \
  -p 10095:10095 \
  -p 10096:10096 \
  registry.cn-hangzhou.aliyuncs.com/funasr_repo/funasr:funasr-runtime-sdk-online-gpu-0.1.12
```

**端口说明**：
- `10095`：WebSocket 服务端口（VoiceNode 使用此端口进行离线转写）
- `10096`：HTTP 服务端口

**FunASR Server 内置模型**：
| 模型 | 用途 |
|------|------|
| Paraformer-large | 离线高精度语音识别 |
| SenseVoiceSmall | 多语言 + 情感识别 + 音频事件检测 |
| FSMN-VAD | 语音端点检测 |
| CT-Transformer | 标点恢复 |
| CAM++ | 说话人分离（Speaker Diarization） |

### 2. FFmpeg（必需）

音频/视频格式转换工具，用于将非 WAV 格式的音频文件转为 16kHz 16bit mono PCM/WAV 标准格式。

**安装方式**：

```bash
# Ubuntu/Debian
sudo apt-get install ffmpeg

# CentOS/RHEL
sudo yum install ffmpeg

# macOS
brew install ffmpeg

# Windows
# 下载 https://ffmpeg.org/download.html 并添加到 PATH
```

**注意事项**：
- 如果服务器已安装 FFmpeg 并在 PATH 中，`FFmpegPath` 配置留空即可
- 如果 FFmpeg 不在 PATH 中，需在 `appsettings.json` 中指定完整路径：
  ```json
  "VoiceNodeOptions": {
    "FFmpegPath": "/usr/local/bin/ffmpeg"
  }
  ```
- FFmpeg 不可用时，仅支持 WAV/PCM 格式的音频文件，其他格式会报错

### 3. .NET 10.0 运行时（必需）

项目基于 .NET 10.0（`net10.0`），需安装对应 SDK。

---

## Docker 部署指南

### 完整部署架构

```
┌─────────────────────────────────────────────────────┐
│  Docker Compose 环境                                 │
│                                                      │
│  ┌──────────────┐    ┌──────────────┐               │
│  │ ZSN.Agent    │    │ ZSN.Agent    │               │
│  │ Brook.AutoJob│───►│ Brook.API    │               │
│  │ (后台任务)    │    │ (REST API)   │               │
│  └──────┬───────┘    └──────────────┘               │
│         │                                            │
│         │ WebSocket                                  │
│         ▼                                            │
│  ┌──────────────┐                                   │
│  │ FunASR       │   ws://funasr:10095               │
│  │ Server       │   (语音转写服务)                    │
│  └──────────────┘                                   │
│                                                      │
│  ┌──────────────┐    ┌──────────────┐               │
│  │ MySQL        │    │ PostgreSQL   │               │
│  │ (主数据库)    │    │ (pgvector)   │               │
│  └──────────────┘    └──────────────┘               │
│                                                      │
│  ┌──────────────┐                                   │
│  │ Redis        │   (任务队列 + 缓存)                │
│  └──────────────┘                                   │
└─────────────────────────────────────────────────────┘
```

### docker-compose.yml 示例（FunASR 部分）

```yaml
services:
  funasr-server:
    image: registry.cn-hangzhou.aliyuncs.com/funasr_repo/funasr:funasr-runtime-sdk-online-cpu-0.1.12
    container_name: funasr-server
    ports:
      - "10095:10095"
      - "10096:10096"
    restart: unless-stopped
    # GPU 版本需要添加：
    # deploy:
    #   resources:
    #     reservations:
    #       devices:
    #         - driver: nvidia
    #           count: 1
    #           capabilities: [gpu]
```

### 注意事项

- FunASR Server 首次启动需要下载模型文件（约 1-2GB），启动时间较长（3-5 分钟）
- CPU 模式下单次转写耗时约为音频时长的 0.3-0.5 倍；GPU 模式约为 0.05-0.1 倍
- FunASR 容器重启后模型需要重新加载，建议设置 `restart: unless-stopped`
- 如果 FunASR 与应用部署在不同机器，修改 `FunASROptions.ServerUrl` 为实际地址

---

## 配置说明

### appsettings.json 配置项

#### VoiceNodeOptions（全局配置）

```json
"VoiceNodeOptions": {
  "DefaultProvider": "FunASR",
  "MaxConcurrentSegments": 4,
  "MaxFileSizeMb": 500,
  "MaxProcessingTimeMinutes": 15,
  "AutoSegmentThresholdSeconds": 300,
  "SupportedFormats": [".wav", ".mp3", ".pcm", ".m4a", ".ogg", ".flac", ".aac", ".wma", ".mp4", ".avi", ".mkv", ".mov"],
  "DefaultSystemPrompt": "请对以下语音转写文本进行整理，修正标点符号和明显错误，并生成简要摘要。",
  "TempFileDirectory": "",
  "FFmpegPath": "",
  "KeepRawResponse": false,
  "CircuitBreakerThreshold": 3,
  "CircuitBreakerRecoverySeconds": 60
}
```

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| DefaultProvider | string | "FunASR" | 默认转写服务商 |
| MaxConcurrentSegments | int | 4 | 长音频分段后最大并发转写段数 |
| MaxFileSizeMb | int | 500 | 音频文件最大大小（MB） |
| MaxProcessingTimeMinutes | int | 15 | 单次任务最大处理时间（分钟） |
| AutoSegmentThresholdSeconds | int | 300 | 长音频自动分段阈值（秒），超过此值自动分段并行转写 |
| SupportedFormats | string[] | 12种格式 | 支持的音频/视频格式 |
| DefaultSystemPrompt | string | 见上文 | LLM 后处理默认提示词 |
| TempFileDirectory | string | "" | 临时文件目录（空则使用系统临时目录） |
| FFmpegPath | string | "" | FFmpeg 路径（空则在 PATH 中查找） |
| KeepRawResponse | bool | false | 是否保留 FunASR 原始 JSON 响应 |
| CircuitBreakerThreshold | int | 3 | 熔断器连续失败阈值 |
| CircuitBreakerRecoverySeconds | int | 60 | 熔断器恢复时间（秒） |

#### FunASROptions（FunASR 配置）

```json
"FunASROptions": {
  "ServerUrl": "ws://10.10.10.2:10095",
  "ChunkSize": 9600,
  "ConnectTimeoutSeconds": 5,
  "TranscribeTimeoutMinutes": 10
}
```

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| ServerUrl | string | ws://127.0.0.1:10095 | FunASR Server WebSocket 地址 |
| ChunkSize | int | 9600 | 音频分片大小（字节），9600 ≈ 600ms @16kHz 16bit |
| ConnectTimeoutSeconds | int | 5 | WebSocket 连接超时（秒） |
| TranscribeTimeoutMinutes | int | 10 | 单次转写超时（分钟） |

---

## 节点配置（VoiceNodeData）

前端节点编辑器中配置，序列化存储在 `NodeConfig.data` 字段。

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| AudioSource | string | null | 音频来源 URL（支持占位符 `{{上游节点ID_fileUrl}}`） |
| Provider | string | null | 指定服务商（空则使用全局默认） |
| EnablePostProcessing | bool | true | 是否启用 LLM 后处理 |
| SystemPrompt | string | null | LLM 后处理提示词（支持 `{{transcription}}` 等占位符） |
| ModelId | string | null | 后处理 LLM 模型 ID |
| OutputFormat | enum | PlainText | 输出格式：PlainText / SegmentsJson / SRT / VTT |
| Language | string | "auto" | 语言提示（zh/en/auto） |
| EnableSpeakerDiarization | bool | true | 是否启用说话人分离 |
| ExpectedSpeakerCount | int | 0 | 预期说话人数（0 = 自动检测） |
| SpeakerLabelMap | Dict | null | 说话人标签映射（Speaker_0 → 张经理） |
| EnableEmotionDetection | bool | false | 情感识别开关 |
| EnableAudioEventDetection | bool | false | 音频事件检测开关 |
| Hotwords | Dict | null | 热词列表（词 → 权重） |
| MaxAudioDurationSeconds | int | 0 | 最大音频时长（秒，0 = 不限制） |

---

## 支持的音频/视频格式

| 格式 | 扩展名 | 说明 |
|------|--------|------|
| WAV | .wav | 推荐，无需转换直接处理 |
| PCM | .pcm | 原始音频，无需转换 |
| MP3 | .mp3 | 常见音频格式，FFmpeg 转换 |
| M4A | .m4a | Apple 音频格式，FFmpeg 转换 |
| OGG | .ogg | 开源音频格式，FFmpeg 转换 |
| FLAC | .flac | 无损音频，FFmpeg 转换 |
| AAC | .aac | 高级音频编码，FFmpeg 转换 |
| WMA | .wma | Windows 音频格式，FFmpeg 转换 |
| MP4 | .mp4 | 视频文件，FFmpeg 提取音轨 |
| AVI | .avi | 视频文件，FFmpeg 提取音轨 |
| MKV | .mkv | 视频文件，FFmpeg 提取音轨 |
| MOV | .mov | Apple 视频格式，FFmpeg 提取音轨 |

---

## 输出变量

VoiceNode 执行完成后输出以下变量，下游节点可通过占位符引用：

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `results` | string | 最终结果（LLM 后处理后的文本，未启用后处理则等于 transcription） |
| `transcription` | string | 格式化后的转写文本（纯文本/SRT/VTT/JSON） |
| `duration` | string | 音频时长（秒，保留 1 位小数） |
| `speakerCount` | string | 检测到的说话人数量 |
| `provider` | string | 实际使用的转写服务商名称 |

**下游引用示例**：
- `{{Voice节点ID_results}}` — 获取最终结果
- `{{Voice节点ID_transcription}}` — 获取原始转写文本
- `{{Voice节点ID_speakerCount}}` — 获取说话人数量

---

## LLM 后处理提示词

节点配置的 `SystemPrompt` 支持以下内置占位符：

| 占位符 | 说明 |
|--------|------|
| `{{transcription}}` | 转写后的格式化文本 |
| `{{duration}}` | 音频总时长（秒） |
| `{{speakerCount}}` | 说话人数量 |
| `{{speakers}}` | 说话人列表（JSON 格式） |

默认提示词模板见 [DefaultPrompt.md](./DefaultPrompt.md)。

**典型场景**：

| 场景 | 提示词要点 |
|------|-----------|
| 会议纪要 | 指定结构（主题/参会人/议题/决议/待办） |
| 内容摘要 | 限定字数，引用 `{{transcription}}` |
| 情感分析 | 分析客户情绪、问题点、改进建议 |
| 关键信息提取 | 提取人名、时间、地点、数字、关键事项 |
| 多语言翻译 | 将转写文本翻译为指定语言 |

---

## 核心执行流程

```
音频文件输入（上游节点传入 URL / 占位符变量）
       │
       ▼
ExecutionVoice（继承 BaseExecution）
       │
       ├── 1. 解析 VoiceNodeData 配置 + 占位符替换
       ├── 2. 创建执行记录 + 流式输出（RecordUpdateThrottler + StreamBatchWriter）
       ├── 3. 音频预处理（AudioPreprocessor）
       │       ├── 格式检测与转换（FFmpeg）
       │       ├── 采样率标准化（16kHz 16bit mono）
       │       └── 文件大小/时长校验
       ├── 4. 获取可用 Provider（VoiceProviderFactory，含熔断器降级）
       ├── 5. 执行转写（FunASR WebSocket offline 模式）
       │       ├── 单段直接转写
       │       └── 长音频分段并行转写（SemaphoreSlim 控制并发）
       ├── 6. 后处理管道
       │       ├── 说话人标签规范化（SpeakerLabelNormalizer）
       │       ├── 输出格式化（OutputFormatter）
       │       └── LLM 后处理（可选）
       └── 7. 写入 Output → 触发下游节点
```

---

## 容错与降级策略

```
主服务商（FunASR Server）
       │
       ├── 连接超时（>5s）→ 自动切换降级服务商
       ├── 转写超时（>MaxProcessingTimeMinutes）→ 终止并标记失败
       └── 连续 3 次失败 → 熔断主服务商 60s
```

- 每个 Provider 独立维护熔断器状态
- 熔断期间所有请求直接走降级服务商
- 半开状态下单次成功即重置计数器
- LLM 后处理失败不影响转写结果输出

---

## 目录结构

```
ZSN.AI.Node/VoiceNode/
├── ExecutionVoice.cs                    # 节点执行器（主入口，继承 BaseExecution）
├── VoiceNodeData.cs                     # 节点配置数据模型（前端编辑器）
├── VoiceNodeOptions.cs                  # 全局配置（IOptions Pattern）
├── Interfaces/
│   ├── IVoiceTranscriptionProvider.cs   # 转写 Provider 抽象接口
│   ├── IVoiceProviderFactory.cs         # Provider 工厂接口（含健康检查/降级）
│   └── IAudioPreprocessor.cs            # 音频预处理器接口
├── Models/
│   ├── TranscriptionResult.cs           # 转写结果（分段 + 说话人 + 时长）
│   ├── TranscriptionSegment.cs          # 单个转写分段（文本 + 时间戳 + 说话人）
│   ├── SpeakerInfo.cs                   # 说话人信息统计
│   ├── TranscribeRequest.cs             # 转写请求封装
│   ├── VoiceTranscriptionOptions.cs     # 转写参数（说话人分离/语言/热词）
│   ├── VoiceProgress.cs                 # 进度回调模型
│   ├── AudioPreprocessResult.cs         # 预处理结果
│   ├── AudioSegmentInfo.cs              # 音频分段信息
│   ├── AudioPreprocessOptions.cs        # 预处理参数
│   └── VoiceFeature.cs                  # 功能特性枚举（Flags）
├── Services/
│   ├── VoiceProviderFactory.cs          # Provider 工厂实现（含熔断器）
│   ├── AudioPreprocessor.cs             # 音频预处理（FFmpeg 调用）
│   ├── SpeakerLabelNormalizer.cs        # 说话人标签规范化
│   └── OutputFormatter.cs              # 输出格式化（PlainText/SRT/VTT/JSON）
├── Providers/
│   └── FunASR/
│       ├── FunASRProvider.cs            # FunASR WebSocket 实现
│       └── FunASROptions.cs             # FunASR 配置类
└── Extensions/
    └── VoiceNodeServiceExtensions.cs    # DI 注册扩展方法
```

---

## 系统集成修改点

| 文件 | 修改内容 |
|------|---------|
| `ZSN.AI.Entity/Workflow/WorkflowNodeInfo.cs` | `NodeType` 枚举新增 `Voice = 27` |
| `ZSN.AgentBrook.AutoJob/Job/NodeJob.cs` | 任务入队 NodeType 列表新增 `NodeType.Voice` |
| `ZSN.AgentBrook.AutoJob/Job/NodeTaskQueueConsumer.cs` | switch 新增 `case NodeType.Voice` 调用 `ExecutionVoice.VoiceNodeAsync()` |
| `ZSN.AgentBrook.AutoJob/Program.cs` / `appsettings.json` | 注册 `AddVoiceNodeServices()` + 配置节 |
| `ZSN.AgentBrook.Web.Manage/md/Voice/DefaultPrompt.md` | 默认 LLM 后处理提示词模板 |

### DI 注册

```csharp
// Program.cs 或 ServiceCollectionExtensions.cs
services.AddVoiceNodeServices(configuration);

// 注册内容：
// - VoiceNodeOptions / FunASROptions → IOptions 配置绑定
// - IVoiceProviderFactory → VoiceProviderFactory (Singleton)
// - IAudioPreprocessor → AudioPreprocessor (Singleton)
// - IVoiceTranscriptionProvider → FunASRProvider (Singleton)
// - ExecutionVoice (Transient)
```

---

## 重要注意事项

1. **FunASR Server 必须先启动**：VoiceNode 依赖 FunASR Server 的 WebSocket 服务，如果服务不可达会导致所有语音转写任务失败
2. **FFmpeg 必须安装**：非 WAV/PCM 格式的音频文件依赖 FFmpeg 进行格式转换，未安装 FFmpeg 时仅支持 WAV/PCM 格式
3. **临时文件清理**：音频预处理过程会生成临时文件（转换后的 WAV），执行完成后自动清理，异常中断时可能残留
4. **大文件处理**：默认最大文件 500MB，超过 5 分钟的音频会自动分段并行转写，可通过 `AutoSegmentThresholdSeconds` 调整
5. **GPU 加速**：FunASR Server GPU 版本推理速度约为 CPU 版本的 5-10 倍，生产环境推荐 GPU 部署
6. **说话人分离**：依赖 FunASR 的 CAM++ 模型，需要 FunASR Server 加载对应模型
7. **超时控制**：单次任务最大处理时间默认 15 分钟，WebSocket 连接超时 5 秒，单次转写超时 10 分钟
8. **熔断器**：FunASR 连续失败 3 次后自动熔断 60 秒，防止持续请求不可用的服务

---

*最后更新: 2026-05-18*
