# ZSN.AI.Node ClawAI 功能迭代记录

> 本文档整合了 ClawAI 节点所有已完成的主要功能迭代，作为项目开发历程的参考。

---

## 一、核心架构

### 1.1 ExcutionClaw Pipeline 重构

将 `ExcutionClaw.cs`（原1185行）拆分为多个专门的 Pipeline 处理器：

| 处理器 | 文件 | 职责 |
|--------|------|------|
| `ModelInitializer` | `Claw/Pipeline/ModelInitializer.cs` | 初始化所有模型配置，集成重试机制 |
| `ContextLoader` | `Claw/Pipeline/ContextLoader.cs` | 并行加载记忆上下文、AI状态和WorkFlow配置 |
| `GreetingFastPath` | `Claw/Pipeline/GreetingFastPath.cs` | 问候语快速路径响应，跳过规划阶段 |
| `PlanningHandler` | `Claw/Pipeline/PlanningHandler.cs` | 创建和管理任务规划 |

### 1.2 配置管理系统

使用 IOptions Pattern 实现配置驱动：

- `Claw/Configuration/ClawAIOptions.cs` - 配置类（303行）
- `appsettings.ClawAI.example.json` - 配置示例

配置模块包括：GreetingDetection、Memory、Planning、SimilarityThresholds、TaskComplexity、Reflection。

### 1.3 基础设施

- **正则表达式缓存**：`Claw/Utils/ClawAIRegexPatterns.cs`，预编译15+个正则表达式，性能提升5-10%
- **重试策略**：`Utils/RetryPolicy.cs`，指数退避、断路器模式、超时控制
- **Result类型**：`Utils/Result.cs`，函数式错误处理（Map/Bind/Fold/Match）
- **批量操作**：`Utils/BatchOperationExtensions.cs`，并行数据库访问，性能提升20-30%
- **统一日志**：`Claw/Utils/LoggerHelper.cs`，模块化日志管理

---

## 二、智能主控服务

**实施日期**: 2026-04-01 | **状态**: 已完成

使用 LLM 替代原有 `GreetingDetector` 关键词匹配，通过理解上下文和系统提示词智能判断用户输入应该**直接回复**还是**进行任务规划**。

### 核心设计

- **判断维度**：用户输入 + 系统提示词 + 对话历史 + 可用WorkFlow + 用户画像
- **DirectResponse**：问候、确认、知识问答、简单对话 → 直接生成回复（避免二次LLM调用）
- **TaskPlanning**：明确任务、需要调用WorkFlow、多步骤任务 → 进入规划流程

### 性能优势

- 问候语响应从2-3秒降至1-1.5秒（节省40-60%时间）
- 缓存机制（5分钟内相同输入返回缓存）
- 超时控制（默认5秒）
- 回退策略（判断失败时回退到关键词匹配）

### 关键文件

- 配置：`ZSN.AI.Entity/ClawAI/ClawAIConfig.cs` (MasterControlConfig)
- 服务：`Claw/Services/MasterControlService.cs`
- 提示词：`md/ClawAI/MasterControlPrompt.md`

---

## 三、AI 个性系统

**实施日期**: 2026-03-24 | **状态**: 已完成

为 ClawAI 提供情绪模拟、个性化响应和目标导向行为能力。

### 核心功能

| 模块 | 功能 | 说明 |
|------|------|------|
| 个性特征 | friendliness/professionalism/creativity/patience/humor | 控制AI的交互风格 |
| 情绪状态 | confidence/satisfaction/engagement/energy (0-100) | 根据任务成功率动态调整 |
| 目标导向 | 目标列表 + 自动更新 | 任务完成后自动更新目标 |
| 成功率追踪 | 加权计算 | 影响AI的自信度 |

### 关键文件

- 服务接口：`Claw/Interfaces/IPersonalityService.cs`
- 服务实现：`Claw/Services/PersonalityService.cs`
- 数据表：`tb_ai_personality_state`

---

## 四、动态任务规划

**实施日期**: 2026-03-29 | **状态**: 已完成

ClawAI 的渐进式智能规划系统，用户只需说明目标，系统自动分解并动态调整。

### 核心流程

```
用户输入："春节推文"
  → 初始规划：步骤1（文案生成）
  → 执行步骤1 → 动态反思："缺少图片"
  → 自动添加：步骤2（图片生成）
  → 执行步骤2 → 动态反思："需要排版"
  → 自动添加：步骤3（排版设计）
  → 执行步骤3 → 反思："已完成" → 返回结果
```

### 关键能力

- **渐进式规划**：只规划第一步，根据结果动态添加后续步骤
- **智能WorkFlow匹配**：精确匹配 + 模糊匹配
- **参数智能提取**：从前置步骤结果提取数据（JSON Path / LLM提取）
- **动态反思**：`AnalyzeTaskDynamicallyAsync()` 分析完成度、缺失能力、改进建议

### 关键文件

- 数据模型：`ZSN.AI.Entity/ClawAI/CommonModels.cs` (TaskAnalysis, SuggestedStep, InputSuggestion)
- 反思服务：`Claw/Services/ReflectionService.cs`
- 结果解析：`Claw/Services/ResultParserService.cs`
- 规划服务：`Claw/Services/TaskPlanningService.cs` (ApplySuggestedStepsAsync)
- 提示词：`md/ClawAI/DynamicReflectionPrompt.md`

### 配置

```json
{
  "reflectionConfig": {
    "enableDynamicTaskAnalysis": true,
    "enableSmartStepSuggestion": true,
    "maxSuggestedSteps": 3
  },
  "taskPlanningConfig": {
    "allowDynamicReplanning": true
  }
}
```

---

## 五、记忆系统

### 5.1 V1 深度优化（2026-03-30）

- **自动知识提炼**：从对话中自动提取和提炼知识点
- **动态记忆更新**：实时更新长期记忆知识库
- **记忆优先回答**：优先使用记忆内容回答用户问题
- **智能去重合并**：自动合并相似知识，避免冗余
- 基于规则的快速提取 + LLM深度提取（可选）
- 自动检测知识类型（概念/事实/流程/经验/问答/偏好）

### 5.2 V2 优化（2026-04-01）

改进7项核心问题：
1. 知识提取触发条件（改为检查对话总长度）
2. 知识类型判断（多维度判断替代简单关键词）
3. 长期记忆归档条件（失败经验也可积累）
4. LLM降级方案（失败时不丢失知识）
5. 自动去重机制
6. 记忆去重优化（Jaccard相似度）
7. 用户反馈学习

### 5.3 P3 PostgreSQL 优化（2026-04-03/04）

基于 PostgreSQL 16 + pgvector 的深度优化：

| 优化项 | 内容 | 说明 |
|--------|------|------|
| 数据库设计 | 10个核心表 + 3个视图 + 3个函数 | `PostgreSQL_Initialization.sql` |
| 语义相似度 | pgvector向量检索 + HNSW索引 | `LongTermMemoryManage.cs` |
| 知识图谱 | 规则方法 + LLM方法双策略 | 见下方知识图谱章节 |
| 用户反馈学习 | 反馈收集 + 偏好调整 | `tb_claw_user_feedback` |
| 上下文加载增强 | 长期记忆关键词+向量检索 | `ContextLoader.cs` |

### 关键文件

- 服务：`Claw/Services/MemoryService.cs`, `Claw/Services/MemoryPersistenceService.cs`
- DAL：`ZSN.AI.DAL.Postgres/ClawAI/LongTermMemoryManage.cs`
- 数据表：`tb_claw_long_term_memory`, `tb_claw_knowledge_relation`, `tb_claw_user_feedback`

---

## 六、知识图谱

**实施日期**: 2026-04-04 | **状态**: 已完成

### 双策略架构

```
知识保存 → 判断重要性
  ├─ 重要性>=80 → LLM方法（深度语义分析，准确率~80%，2-5秒/次）
  └─ 重要性<80  → 规则方法（文本相似度，准确率~53%，<10ms）
    → 保存到 tb_claw_knowledge_relation
```

### 规则方法（基础）

- 语义相似度搜索（SearchBySimilarity）
- 多维度相似度计算（主题+摘要+类型）
- Jaccard相似度算法
- 6种关系类型自动判断（related/prerequisite/derived/conflict/example/category）
- BFS遍历和最短路径查找

**位置**: `ZSN.AI.BLL/ClawAI/KnowledgeRelationBusiness.cs`

### LLM方法（增强）

- 深度语义理解
- 复杂关系识别（前置、派生、冲突等）
- 关系原因说明（可解释）
- 自动降级（失败时使用规则方法）

**位置**: `Claw/Services/KnowledgeGraphLLMService.cs`

### 自动化流程

每次对话后自动执行：
1. 提取对话中的重要知识
2. 保存到长期记忆（`tb_claw_long_term_memory`）
3. 根据重要性选择策略发现知识关系
4. 自动保存到知识图谱

**触发位置**: `MemoryPersistenceService.cs:209`

---

## 七、知识库混合检索

**实施日期**: 2026-04-09 | **状态**: 已完成

将知识库节点（KnowledgeBaseNode）从旧的 LLM 直接问答方式改造为向量检索+图谱检索的融合策略。

- 检索结果传递给下游节点处理，不在节点内直接调用LLM
- 支持向量相似度检索
- 支持知识图谱关联检索

---

## 八、性能优化

### 8.1 P0+P1 性能优化（2026-03-30）

综合性能提升 **45-60%**：

| 场景 | 优化前 | 优化后 | 提升 |
|------|--------|--------|------|
| 问候语响应 | 173秒 | 2-3秒 | 98% |
| 正则匹配 | 基准 | +5-10% | 5-10% |
| 数据库访问 | 串行 | 并行 | 20-30% |

### 8.2 后处理异步化

将阶段4后处理（记忆更新、ChatHistory保存、AI状态更新）在新线程中异步执行，主流程立即返回结果，用户等待时间减少2-5秒。

### 8.3 WorkFlow执行优化

- **动态超时机制**：根据WorkFlow复杂度智能调整超时时间
- **并发执行优化**：基于依赖关系的智能并行执行
- **配置化超时参数**：通过配置文件灵活调整，无需修改代码

### 8.4 WorkFlow状态检测增强

- 新增 `IsWorkflowCompletedAsync()` 方法
- 检查实际执行结果数据（不依赖 `NodeStatus` 字段）
- 解决已完成但检测不到一直轮询到超时（10分钟）的问题

---

## 九、任务规划优化

- **提示词模板优化**：减少LLM臆造WorkFlow，增强WorkFlow能力展示
- **记忆利用增强**：充分利用对话历史和历史成功案例
- **WorkFlow能力描述**：清晰展示每个WorkFlow的实际能力，辅助LLM匹配

---

## 十、BUG修复记录

### P0级（已完成）

| 问题 | 描述 | 修复日期 |
|------|------|---------|
| 异步回调注册时序 | ClawAI异步触发子WorkFlow后永久停留在等待状态 | 2026-04-23 |
| 异步触发后续步骤未执行 | 步骤1异步触发后主流程退出，步骤2-8不执行 | 2026-04-24 |
| AgentEndNodeAsync缺少await | TryResumeClawAIStepAsync被Fire-and-Forget | 2026-04-24 |
| 并行上下文丢失 | clawai:ctx key被误删，所有层共用同一个key | 2026-04-24 |
| NodeJob队列堵塞 | DisallowConcurrentExecution导致任务无法及时处理 | 2026-04-24 |

### P1级（已完成）

| 问题 | 描述 | 修复日期 |
|------|------|---------|
| 并行日志线程安全 | 并行执行时日志集合非线程安全 | 2026-04-24 |
| WorkFlow多步骤截断 | 多步骤执行结果被截断 | 2026-04-24 |
| Task.Run作用域服务共享 | 后台Task.Run中共享Scoped服务 | 2026-04-24 |
| 任务未完成就停止 | AllStepsCompleted判断过于宽松 | 2026-04-22 |
| 并行步骤恢复失败 | 并行执行后未继续执行反思和完成逻辑 | 2026-04-22 |

---

## 十一、数据库表结构

### 核心表

| 表名 | 说明 |
|------|------|
| `tb_claw_long_term_memory` | 长期记忆（支持pgvector向量嵌入） |
| `tb_claw_knowledge_relation` | 知识关系（6种关系类型+强度） |
| `tb_claw_user_feedback` | 用户反馈（类型+评分+使用的记忆） |
| `tb_ai_personality_state` | AI个性状态（特征+情绪+目标） |

数据库初始化脚本：`Claw/PostgreSQL_Initialization.sql`

---

## 十二、配置参考

### 完整配置示例

```json
{
  "ClawAI": {
    "Memory": {
      "WorkingMemoryLimit": 10,
      "EpisodicMemoryLimit": 5
    },
    "Reflection": {
      "SimpleTaskMaxSteps": 2,
      "HighQualityStepThreshold": 80
    }
  },
  "masterControlConfig": {
    "enabled": true,
    "enableCache": true,
    "cacheExpirationSeconds": 300,
    "timeoutSeconds": 5,
    "fallbackStrategy": "fallback_to_planning"
  },
  "reflectionConfig": {
    "enabled": true,
    "enableDynamicTaskAnalysis": true,
    "enableSmartStepSuggestion": true,
    "maxSuggestedSteps": 3
  },
  "taskPlanningConfig": {
    "enabled": true,
    "planningStrategy": "adaptive",
    "maxSteps": 6,
    "allowDynamicReplanning": true
  },
  "personalityConfig": {
    "enabled": true,
    "personalityDescription": "专业、友好的AI助手",
    "enableEmotionalState": true,
    "enableGoalOriented": true
  }
}
```

---

## 十三、重要文件索引

### 服务层

| 文件 | 说明 |
|------|------|
| `ExcutionClaw.cs` | ClawAI核心执行器 |
| `Claw/Services/MasterControlService.cs` | 智能主控服务 |
| `Claw/Services/MemoryService.cs` | 记忆管理服务 |
| `Claw/Services/ReflectionService.cs` | 反思服务（含动态分析） |
| `Claw/Services/TaskPlanningService.cs` | 任务规划服务 |
| `Claw/Services/PersonalityService.cs` | AI个性服务 |
| `Claw/Services/KnowledgeGraphLLMService.cs` | LLM知识图谱服务 |
| `Claw/Services/ResultParserService.cs` | 结果解析服务 |
| `Claw/Services/MemoryPersistenceService.cs` | 记忆持久化服务 |

### Pipeline

| 文件 | 说明 |
|------|------|
| `Claw/Pipeline/ModelInitializer.cs` | 模型初始化器 |
| `Claw/Pipeline/ContextLoader.cs` | 上下文加载器 |
| `Claw/Pipeline/GreetingFastPath.cs` | 问候语快速路径 |
| `Claw/Pipeline/PlanningHandler.cs` | 规划处理器 |

### BLL层

| 文件 | 说明 |
|------|------|
| `ZSN.AI.BLL/ClawAI/KnowledgeRelationBusiness.cs` | 知识关系业务逻辑 |
| `ZSN.AI.BLL/ClawAI/LongTermMemoryBusiness.cs` | 长期记忆业务逻辑 |

### Entity层

| 文件 | 说明 |
|------|------|
| `ZSN.AI.Entity/ClawAI/ClawAIConfig.cs` | ClawAI配置类集合 |
| `ZSN.AI.Entity/ClawAI/CommonModels.cs` | 公共数据模型 |
| `ZSN.AI.Entity/ClawAI/ModelSelector.cs` | 模型选择器 |

### DAL层

| 文件 | 说明 |
|------|------|
| `ZSN.AI.DAL.Postgres/ClawAI/LongTermMemoryManage.cs` | 长期记忆数据访问（pgvector） |

---

*最后更新: 2026-04-30*
