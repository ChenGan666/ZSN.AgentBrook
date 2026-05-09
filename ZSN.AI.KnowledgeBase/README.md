# ZSN.AI 知识库升级项目文档

> **项目名称**: 知识图谱功能升级
> **项目周期**: 16周（6个阶段）
> **当前状态**: 🔄 规划阶段
> **创建日期**: 2026-04-07

---

## 📚 文档结构

```
ZSN.AI.KnowledgeBase/
├── 知识图谱功能规划文档.md       # 总体规划文档
├── README.md                      # 本文档
│
└── 实施阶段/
    ├── 总体进度跟踪.md             # 📊 整体进度和里程碑
    ├── 阶段1-基础设施准备.md       # 🏗️ 阶段1详细文档
    ├── 阶段2-语义感知分块实现.md   # 🔪 阶段2详细文档
    └── 阶段3-6实施计划.md          # 📝 阶段3-6精简文档
```

---

## 🎯 项目目标

### 核心目标
1. **语义感知的智能分块** - 替换硬切块，语义完整性从60%提升到90%
2. **知识图谱构建** - 实现实体识别（召回率>80%）和关系抽取（准确率>75%）
3. **混合检索系统** - 向量检索+图谱检索，准确率从70%提升到85%
4. **PostgreSQL + Apache AGE** - 统一数据库方案

### 技术栈
- **向量存储**: PostgreSQL + pgvector
- **图数据库**: PostgreSQL + Apache AGE
- **LLM**: OpenAI / Ollama（多模型支持）
- **框架**: .NET 10, Microsoft.KernelMemory

---

## 📅 阶段概览

| 阶段 | 名称 | 周期 | 状态 | 完成度 | 里程碑 |
|------|------|------|------|--------|--------|
| 📋 **阶段0** | 规划准备 | - | ✅ 完成 | 100% | 规划文档完成 |
| 🏗️ **阶段1** | 基础设施准备 | 2周 | ⏸️ 未开始 | 0% | M1: 基础架构完成 |
| 🔪 **阶段2** | 语义感知分块 | 3周 | ⏸️ 未开始 | 0% | M2: 智能分块完成 |
| 🕸️ **阶段3** | 知识图谱构建 | 4周 | ⏸️ 未开始 | 0% | M3: 图谱系统完成 |
| 🔍 **阶段4** | 混合检索系统 | 3周 | ⏸️ 未开始 | 0% | M4: 检索系统完成 |
| 🔧 **阶段5** | 集成与优化 | 2周 | ⏸️ 未开始 | 0% | M5: 集成完成 |
| 🚀 **阶段6** | 测试与部署 | 2周 | ⏸️ 未开始 | 0% | M6: 上线发布 |
| **总计** | **6个阶段** | **16周** | **⏸️ 未开始** | **0%** | **6个里程碑** |

---

## 📖 快速导航

### 📊 进度跟踪
- **[总体进度跟踪](./实施阶段/总体进度跟踪.md)** - 查看整体进度、指标、里程碑
- **[阻塞问题](./实施阶段/总体进度跟踪.md#-当前阻塞问题)** - 当前阻碍项目的问题
- **[风险跟踪](./实施阶段/总体进度跟踪.md#-风险跟踪)** - 项目风险和缓解措施

### 📝 阶段文档

#### 阶段1：基础设施准备（2周）
**详细文档**: [阶段1-基础设施准备.md](./实施阶段/阶段1-基础设施准备.md)

**核心任务**:
- ✅ 设计并创建数据库Schema（pgvector + Apache AGE）
- ✅ 实现核心接口定义
- ✅ 集成PostgreSQL + Apache AGE
- ✅ 搭建单元测试框架

**关键交付物**:
- `schema/knowledge_chunks.sql` - 文本块向量表
- `schema/graph_schema.sql` - Apache AGE图Schema
- `IGraphRepository.cs` - 图数据库仓储接口
- `AgeGraphRepository.cs` - AGE实现

**验收标准**:
- 所有接口定义完成并通过评审
- Apache AGE连接成功，可进行基本CRUD
- 单元测试框架可运行

---

#### 阶段2：语义感知分块实现（3周）
**详细文档**: [阶段2-语义感知分块实现.md](./实施阶段/阶段2-语义感知分块实现.md)

**核心任务**:
- ✅ 实现LLM驱动的语义分块
- ✅ 实现大文件流式处理
- ✅ 性能优化（并行、缓存）
- ✅ 测试和调优

**关键交付物**:
- `SemanticChunkerService.cs` - 语义分块服务
- `StreamingSemanticChunker.cs` - 流式分块器
- `EntityIntegrityProtector.cs` - 实体完整性保护器
- `prompts/semantic-chunking.txt` - 分块Prompt

**验收标准**:
- 语义完整性 > 90%
- 支持PDF、Word、Markdown、TXT
- 处理速度 >= 原有方案的80%

---

#### 阶段3：知识图谱构建（4周）
**详细文档**: [阶段3-6实施计划.md#阶段3知识图谱构建](./实施阶段/阶段3-6实施计划.md)

**核心任务**:
- ✅ 实体识别模块（召回率 > 80%）
- ✅ 关系抽取模块（准确率 > 75%）
- ✅ Apache AGE图谱存储和查询
- ✅ 图谱质量评估

**关键交付物**:
- `LLMEntityExtractor.cs` - LLM实体抽取器
- `LLMRelationExtractor.cs` - LLM关系抽取器
- `AgeGraphStorage.cs` - AGE存储实现
- `GraphQueryService.cs` - 图谱查询服务

**验收标准**:
- 实体识别准确率 > 80%
- 关系抽取准确率 > 75%
- 图谱查询响应时间 < 500ms

---

#### 阶段4：混合检索系统（3周）
**详细文档**: [阶段3-6实施计划.md#阶段4混合检索系统](./实施阶段/阶段3-6实施计划.md)

**核心任务**:
- ✅ 向量检索优化
- ✅ 图谱检索实现
- ✅ 结果融合算法
- ✅ 重排序模块

**关键交付物**:
- `VectorSearchService.cs` - 向量检索服务
- `GraphSearchService.cs` - 图谱检索服务
- `HybridSearchService.cs` - 混合检索服务
- `WeightedFusion.cs` / `RRFusion.cs` - 融合策略

**验收标准**:
- 检索准确率提升 > 15%
- 查询响应时间 < 2s
- 支持复杂查询

---

#### 阶段5：集成与优化（2周）
**详细文档**: [阶段3-6实施计划.md#阶段5集成与优化](./实施阶段/阶段3-6实施计划.md)

**核心任务**:
- ✅ ImportKMSService升级
- ✅ KMService升级
- ✅ API扩展和文档
- ✅ 集成测试

**关键交付物**:
- 更新的 `ImportKMSService.cs`
- 更新的 `KMService.cs`
- API文档
- 集成测试套件

**验收标准**:
- 所有功能集成完成
- 向后兼容
- 通过集成测试

---

#### 阶段6：测试与部署（2周）
**详细文档**: [阶段3-6实施计划.md#阶段6测试与部署](./实施阶段/阶段3-6实施计划.md)

**核心任务**:
- ✅ 功能测试（单元、集成、E2E）
- ✅ 性能测试（压力、并发、长时间）
- ✅ 用户验收测试
- ✅ 部署上线

**关键交付物**:
- 测试报告
- 部署文档
- 监控配置
- 生产环境

**验收标准**:
- 所有测试通过
- 性能指标达标
- 用户满意度 > 85%

---

## 📊 核心指标

### 技术指标

| 指标 | 基线 | 目标 | 当前值 | 状态 |
|------|------|------|--------|------|
| 分块语义完整性 | 60% | 90% | - | 🔄 待测 |
| 检索准确率 | 70% | 85% | - | 🔄 待测 |
| 实体识别召回率 | 0% | 80% | - | 🔄 待测 |
| 关系抽取准确率 | 0% | 75% | - | 🔄 待测 |
| 查询响应时间 | ~1s | <2s | - | 🔄 待测 |
| 复杂查询支持 | ❌ | ✅ | - | 🔄 待测 |

### 业务指标

| 指标 | 目标 | 测量方法 | 当前值 | 状态 |
|------|------|----------|--------|------|
| 用户满意度 | >85% | 用户调查 | - | 🔄 待测 |
| 知识库使用率 | +20% | 使用统计 | - | 🔄 待测 |
| 查询成功率 | >90% | 日志分析 | - | 🔄 待测 |
| 功能采用率 | >60% | 功能统计 | - | 🔄 待测 |

---

## 🚀 快速开始

### 开发环境设置

1. **安装Docker**
   ```bash
   docker-compose up -d
   ```

2. **初始化数据库**
   ```bash
   psql -h localhost -U postgres -d knowledge_base_db -f schema/init.sql
   ```

3. **运行测试**
   ```bash
   dotnet test ZSN.AI.KnowledgeBase.Tests
   ```

### 代码结构

```
ZSN.AI.Core/
├── Interface/                      # 接口定义
│   ├── ISemanticChunkerService.cs
│   ├── IKnowledgeGraphService.cs
│   ├── IHybridSearchService.cs
│   └── IGraphRepository.cs
│
├── Service/                        # 服务实现
│   ├── SemanticChunkerService.cs
│   ├── KnowledgeGraphService.cs
│   └── HybridSearchService.cs
│
├── Repositories/                   # 数据访问层
│   └── AgeGraphRepository.cs
│
└── Handler/                        # Kernel Memory Handlers
    ├── SemanticChunkHandler.cs
    └── KnowledgeGraphHandler.cs
```

---

## 📝 使用指南

### 启用知识图谱功能

```csharp
// 1. 更新知识库配置
var kb = new KnowledgeBaseInfo
{
    KnowledgeBaseID = "kb_001",
    EnableKnowledgeGraph = true,
    ChunkingStrategy = ChunkingStrategy.SemanticBoundary,
    EnableHybridSearch = true,
    VectorSearchWeight = 0.6f,
    GraphSearchWeight = 0.4f
};

// 2. 导入文档（使用新流程）
await _importService.ImportKMSTask(new ImportKMSTaskReq
{
    KmsId = kb.KnowledgeBaseID,
    FilePath = documentPath,
    EnableNewPipeline = true
});

// 3. 混合检索
var results = await _kmService.GetRelevantSourceList(
    modelUnit,
    query,
    kb.KnowledgeBaseID
);
```

---

## 🔧 技术细节

### PostgreSQL + Apache AGE 配置

```sql
-- 安装Apache AGE扩展
CREATE EXTENSION IF NOT EXISTS age;
LOAD 'age';
SET search_path = ag_catalog, "$user", public;

-- 创建图数据库
SELECT create_graph('knowledge_graph');

-- 创建顶点
SELECT * FROM cypher('knowledge_graph', $$
    CREATE (p:Person {name: 'Alice', role: 'Engineer'})
    RETURN p
$$) as (p agtype);

-- 创建边
SELECT * FROM cypher('knowledge_graph', $$
    MATCH (a:Person {name: 'Alice'}), (b:Person {name: 'Bob'})
    CREATE (a)-[r:COLLEAGUE {since: 2020}]->(b)
    RETURN r
$$) as (r agtype);

-- 查询
SELECT * FROM cypher('knowledge_graph', $$
    MATCH (p:Person)-[r:COLLEAGUE]->(related)
    RETURN p.name, related.name, r.since
$$) as (name1 agtype, name2 agtype, since agtype);
```

### 向量检索配置

```sql
-- 创建向量列
ALTER TABLE knowledge_chunks
ADD COLUMN content_vector vector(1536);

-- 创建HNSW索引（更快）
CREATE INDEX idx_chunks_vector_hnsw
ON knowledge_chunks
USING hnsw (content_vector vector_cosine_ops)
WITH (m = 16, ef_construction = 64);

-- 向量相似度搜索
SELECT content, 1 - (content_vector <=> query_vector) as similarity
FROM knowledge_chunks
WHERE knowledge_base_id = 'kb_001'
ORDER BY content_vector <=> query_vector
LIMIT 20;
```

---

## 🤝 贡献指南

### 工作流程

1. **领取任务** - 从阶段文档中选择任务
2. **创建分支** - `feature/stage-N-task-name`
3. **开发实现** - 按照任务清单完成开发
4. **编写测试** - 单元测试覆盖率 > 80%
5. **提交PR** - 包含详细描述和测试结果
6. **代码审查** - 至少1人审查通过
7. **合并主分支** - 更新进度跟踪文档

### 提交规范

```
feat(stage1): 添加Apache AGE仓储实现

- 实现基础CRUD操作
- 添加单元测试
- 更新API文档

Closes #1
```

---

## 📞 联系方式

- **项目负责人**: 待定
- **技术支持**: 待定
- **问题反馈**: [GitHub Issues](待添加)

---

## 📄 许可证

待定

---

**最后更新**: 2026-04-07
**文档版本**: 1.0
