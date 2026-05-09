# ClawAI 记忆系统 - PostgreSQL 部署指南

**版本**: 2.0  
**日期**: 2026-04-01  
**数据库**: PostgreSQL 16.6 + pgvector 0.8.0 + Apache AGE (可选)

---

## 📋 目录

1. [概述](#概述)
2. [系统要求](#系统要求)
3. [快速开始](#快速开始)
4. [表结构说明](#表结构说明)
5. [性能优化](#性能优化)
6. [常见问题](#常见问题)

---

## 概述

ClawAI记忆系统基于PostgreSQL构建，提供以下核心功能：

- ✅ **语义相似度检索** - 使用pgvector扩展实现向量搜索
- ✅ **知识图谱** - 使用Apache AGE扩展实现图数据库
- ✅ **用户反馈学习** - 动态调整知识重要性

### 架构图

```
┌─────────────────────────────────────────────────────────┐
│              PostgreSQL 16.6 数据库                      │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  关系数据     │  │  向量数据     │  │  图数据       │  │
│  │  (标准表)     │  │  (pgvector)  │  │  (Apache AGE)│  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│         │                 │                  │          │
│         └─────────────────┴──────────────────┘          │
│                    统一SQL查询                           │
└─────────────────────────────────────────────────────────┘
```

---

## 系统要求

### 必需组件

- **PostgreSQL**: 16.6 或更高版本
- **pgvector**: 0.8.0 或更高版本（已包含在Dockerfile中）

### 可选组件

- **Apache AGE**: 1.5.0 或更高版本（用于图数据库功能）

### Docker环境

- **Docker**: 20.10+ 
- **Docker Compose**: 2.0+

---

## 快速开始

### 方法1: 使用现有Dockerfile（推荐）

您的Dockerfile已经包含了pgvector，可以直接使用：

```bash
# 1. 进入docker目录
cd W:\AI\ZSN.Knowbase\docker\postgres

# 2. 构建镜像
docker build -t zsn-postgres:16.6 .

# 3. 启动容器
docker run -d \
  --name zsn-postgres \
  -p 5432:5432 \
  -e POSTGRES_PASSWORD=your_password \
  -v $(pwd)/data:/var/lib/postgresql/data \
  zsn-postgres:16.6

# 4. 等待PostgreSQL启动（约10秒）
docker logs -f zsn-postgres

# 5. 验证扩展安装
docker exec -it zsn-postgres psql -U postgres -c "SELECT extname, extversion FROM pg_extension WHERE extname = 'vector';"
```

### 方法2: 添加Apache AGE支持（可选）

如果需要图数据库功能，修改Dockerfile：

```dockerfile
FROM postgres:16.6

ENV PGVECTOR_VERSION=0.8.0
ENV AGE_VERSION=1.5.0

# 安装依赖
RUN apt-get update && \
    apt-get install -y \
    postgresql-16-pgvector \
    build-essential \
    postgresql-server-dev-16 \
    git \
    && rm -rf /var/lib/apt/lists/*

# 安装Apache AGE
RUN cd /tmp && \
    git clone --branch PG16/v${AGE_VERSION} https://github.com/apache/age.git && \
    cd age && \
    make && \
    make install && \
    cd / && \
    rm -rf /tmp/age

COPY ./init.sh /docker-entrypoint-initdb.d/
COPY ./0_init.sql /docker-entrypoint-initdb.d/

EXPOSE 5432
```

然后重新构建：

```bash
docker build -t zsn-postgres:16.6-age .
```

### 初始化数据库

容器启动后，会自动执行以下脚本：

1. **init.sh** - 加载扩展
2. **0_init.sql** - 创建表结构

验证初始化：

```bash
# 连接数据库
docker exec -it zsn-postgres psql -U postgres -d zsn_knowledge_base

# 查看表
\dt

# 查看索引
\di

# 查看扩展
\dx
```

---

## 表结构说明

### 1. tb_claw_long_term_memory（长期记忆表）

**用途**: 存储知识点，支持向量语义检索

**关键字段**:
- `embedding vector(1536)` - 向量嵌入（OpenAI text-embedding-3）
- `importance INT` - 重要性评分（0-100）
- `metadata JSONB` - JSON元数据

**索引**:
- `idx_ltm_embedding_hnsw` - HNSW向量索引（最优性能）
- `idx_ltm_app_member` - 应用和用户复合索引
- `idx_ltm_metadata` - GIN索引（JSON查询）

**示例查询**:

```sql
-- 语义相似度检索（余弦距离）
SELECT 
    memory_id, 
    summary, 
    1 - (embedding <=> '[0.1, 0.2, ...]'::vector) AS similarity
FROM tb_claw_long_term_memory
WHERE app_id = 'app_001' 
  AND member_id = 'user_001'
ORDER BY embedding <=> '[0.1, 0.2, ...]'::vector
LIMIT 5;

-- 混合搜索（向量 + 全文）
WITH vector_results AS (
    SELECT memory_id, 
           1 - (embedding <=> @query_vector::vector) AS score
    FROM tb_claw_long_term_memory
    ORDER BY embedding <=> @query_vector::vector
    LIMIT 20
)
SELECT m.*, v.score
FROM tb_claw_long_term_memory m
JOIN vector_results v ON m.memory_id = v.memory_id
WHERE to_tsvector('english', m.summary || ' ' || m.content) 
      @@ plainto_tsquery('english', 'search query')
ORDER BY v.score DESC
LIMIT 5;
```

### 2. tb_claw_knowledge_relation（知识关系表）

**用途**: 存储知识之间的关系，支持图查询

**关键字段**:
- `source_memory_id` - 源知识ID
- `target_memory_id` - 目标知识ID
- `relation_type` - 关系类型（related/prerequisite/derived/conflict等）
- `strength FLOAT` - 关系强度（0-1）

**关系类型**:
- `related` - 相关知识
- `prerequisite` - 前置知识
- `derived` - 派生知识
- `conflict` - 冲突知识
- `example` - 示例关系
- `category` - 分类关系

**示例查询**:

```sql
-- 查找直接关联的知识
SELECT 
    m.memory_id,
    m.summary,
    r.relation_type,
    r.strength
FROM tb_claw_knowledge_relation r
JOIN tb_claw_long_term_memory m ON r.target_memory_id = m.memory_id
WHERE r.source_memory_id = 'memory_001'
ORDER BY r.strength DESC;

-- 递归查询（最多3层）
WITH RECURSIVE knowledge_path AS (
    -- 初始节点
    SELECT 
        source_memory_id,
        target_memory_id,
        relation_type,
        strength,
        1 AS depth,
        ARRAY[source_memory_id] AS path
    FROM tb_claw_knowledge_relation
    WHERE source_memory_id = 'memory_001'
    
    UNION ALL
    
    -- 递归部分
    SELECT 
        r.source_memory_id,
        r.target_memory_id,
        r.relation_type,
        r.strength,
        kp.depth + 1,
        kp.path || r.source_memory_id
    FROM tb_claw_knowledge_relation r
    JOIN knowledge_path kp ON r.source_memory_id = kp.target_memory_id
    WHERE kp.depth < 3
      AND NOT (r.source_memory_id = ANY(kp.path))  -- 避免循环
)
SELECT DISTINCT
    kp.target_memory_id,
    m.summary,
    kp.depth,
    kp.relation_type,
    kp.strength
FROM knowledge_path kp
JOIN tb_claw_long_term_memory m ON kp.target_memory_id = m.memory_id
ORDER BY kp.depth, kp.strength DESC
LIMIT 10;
```

### 3. tb_claw_user_feedback（用户反馈表）

**用途**: 收集用户反馈，动态调整知识重要性

**关键字段**:
- `feedback_type` - 反馈类型（positive/negative/neutral）
- `feedback_score INT` - 评分（1-5）
- `used_memories JSONB` - 使用的记忆ID列表

**示例查询**:

```sql
-- 统计知识的反馈情况
SELECT 
    memory_id,
    COUNT(*) AS total_feedbacks,
    COUNT(*) FILTER (WHERE feedback_type = 'positive') AS positive_count,
    COUNT(*) FILTER (WHERE feedback_type = 'negative') AS negative_count,
    AVG(feedback_score) AS avg_score
FROM tb_claw_user_feedback
WHERE create_time >= CURRENT_TIMESTAMP - INTERVAL '30 days'
GROUP BY memory_id
HAVING COUNT(*) >= 5
ORDER BY avg_score DESC;

-- 查找低质量知识（负面率>70%）
SELECT 
    f.memory_id,
    m.summary,
    COUNT(*) AS total_feedbacks,
    COUNT(*) FILTER (WHERE f.feedback_type = 'negative') AS negative_count,
    ROUND(
        COUNT(*) FILTER (WHERE f.feedback_type = 'negative')::NUMERIC / 
        COUNT(*)::NUMERIC * 100, 
        2
    ) AS negative_rate
FROM tb_claw_user_feedback f
JOIN tb_claw_long_term_memory m ON f.memory_id = m.memory_id
WHERE f.create_time >= CURRENT_TIMESTAMP - INTERVAL '30 days'
GROUP BY f.memory_id, m.summary
HAVING COUNT(*) >= 5
   AND COUNT(*) FILTER (WHERE f.feedback_type = 'negative')::NUMERIC / COUNT(*)::NUMERIC > 0.7
ORDER BY negative_rate DESC;
```

---

## 性能优化

### HNSW索引参数调优

当前配置（默认）:
```sql
CREATE INDEX idx_ltm_embedding_hnsw ON tb_claw_long_term_memory 
USING hnsw (embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);
```

**参数说明**:
- `m` (默认16): 每层最大连接数
  - 范围: 2-100
  - 越大索引越大，查询越快
  - 推荐: 16-32
  
- `ef_construction` (默认64): 构建时搜索深度
  - 范围: 4-1000
  - 越大构建越慢，索引质量越高
  - 推荐: 64-128

**查询时动态调整**:
```sql
-- 设置查询时搜索深度（默认40）
SET hnsw.ef_search = 100;

-- 执行查询
SELECT * FROM tb_claw_long_term_memory
ORDER BY embedding <=> '[...]'::vector
LIMIT 5;
```

### 性能基准

| 数据量 | HNSW索引 | IVFFlat索引 | 无索引 |
|--------|---------|------------|--------|
| 1万条 | <10ms | <20ms | <100ms |
| 10万条 | <20ms | <50ms | <1s |
| 100万条 | <50ms | <200ms | <10s |
| 1000万条 | <100ms | <500ms | >60s |

### 监控查询

```sql
-- 查看索引使用情况
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes
WHERE tablename LIKE 'tb_claw_%'
ORDER BY idx_scan DESC;

-- 查看表大小
SELECT 
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE tablename LIKE 'tb_claw_%'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- 查看慢查询
SELECT 
    query,
    calls,
    total_exec_time,
    mean_exec_time,
    max_exec_time
FROM pg_stat_statements
WHERE query LIKE '%tb_claw_%'
ORDER BY mean_exec_time DESC
LIMIT 10;
```

---

## 常见问题

### Q1: 如何验证pgvector是否正常工作？

```bash
docker exec -it zsn-postgres psql -U postgres -d zsn_knowledge_base -c "
SELECT 
    '[1,2,3]'::vector <-> '[4,5,6]'::vector AS l2_distance,
    '[1,2,3]'::vector <=> '[4,5,6]'::vector AS cosine_distance;
"
```

预期输出：
```
 l2_distance | cosine_distance 
-------------+-----------------
    5.196152 |       0.0253681
```

### Q2: 如何检查向量索引是否被使用？

```sql
EXPLAIN ANALYZE
SELECT * FROM tb_claw_long_term_memory
ORDER BY embedding <=> '[0.1, 0.2, ...]'::vector
LIMIT 5;
```

应该看到 `Index Scan using idx_ltm_embedding_hnsw`

### Q3: 向量检索速度慢怎么办？

1. **调整ef_search参数**:
```sql
SET hnsw.ef_search = 40;  -- 降低精度，提升速度
```

2. **使用预过滤**:
```sql
-- 先用其他条件过滤，再做向量检索
SELECT * FROM tb_claw_long_term_memory
WHERE app_id = 'app_001' 
  AND importance >= 60
ORDER BY embedding <=> '[...]'::vector
LIMIT 5;
```

3. **考虑使用IVFFlat索引**（构建更快）:
```sql
DROP INDEX idx_ltm_embedding_hnsw;
CREATE INDEX idx_ltm_embedding_ivfflat ON tb_claw_long_term_memory 
USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);
```

### Q4: 如何备份和恢复数据？

**备份**:
```bash
# 备份整个数据库
docker exec zsn-postgres pg_dump -U postgres zsn_knowledge_base > backup.sql

# 只备份ClawAI表
docker exec zsn-postgres pg_dump -U postgres -t 'tb_claw_*' zsn_knowledge_base > clawai_backup.sql
```

**恢复**:
```bash
# 恢复数据库
docker exec -i zsn-postgres psql -U postgres zsn_knowledge_base < backup.sql

# 恢复特定表
docker exec -i zsn-postgres psql -U postgres zsn_knowledge_base < clawai_backup.sql
```

### Q5: 如何清理测试数据？

```sql
-- 清空所有ClawAI表（保留结构）
TRUNCATE TABLE tb_claw_user_feedback CASCADE;
TRUNCATE TABLE tb_claw_knowledge_relation CASCADE;
TRUNCATE TABLE tb_claw_long_term_memory CASCADE;

-- 重置序列（如果有）
-- ALTER SEQUENCE xxx_seq RESTART WITH 1;
```

### Q6: Apache AGE如何使用？

如果已安装AGE扩展：

```sql
-- 1. 创建图
SELECT create_graph('knowledge_graph');

-- 2. 创建节点
SELECT * FROM cypher('knowledge_graph', $$
    CREATE (m:Memory {
        id: 'memory_001',
        topic: 'PostgreSQL',
        summary: 'PostgreSQL是强大的开源数据库'
    })
    RETURN m
$$) AS (memory agtype);

-- 3. 创建关系
SELECT * FROM cypher('knowledge_graph', $$
    MATCH (source:Memory {id: 'memory_001'})
    MATCH (target:Memory {id: 'memory_002'})
    CREATE (source)-[r:RELATES_TO {strength: 0.8}]->(target)
    RETURN r
$$) AS (relation agtype);

-- 4. 图查询
SELECT * FROM cypher('knowledge_graph', $$
    MATCH path = (source:Memory {id: 'memory_001'})-[r:RELATES_TO*1..3]->(target:Memory)
    RETURN target.id, target.summary, length(path)
    ORDER BY length(path)
    LIMIT 10
$$) AS (id agtype, summary agtype, depth agtype);
```

---

## 相关文档

- **完整实施方案**: `MEMORY_OPTIMIZATION_P3_POSTGRESQL_PLAN.md`
- **pgvector官方文档**: https://github.com/pgvector/pgvector
- **Apache AGE官方文档**: https://age.apache.org/
- **PostgreSQL文档**: https://www.postgresql.org/docs/16/

---

## 支持

如有问题，请查看：
1. Docker容器日志: `docker logs zsn-postgres`
2. PostgreSQL日志: `docker exec zsn-postgres tail -f /var/lib/postgresql/data/log/postgresql-*.log`
3. 扩展状态: `docker exec zsn-postgres psql -U postgres -c "\dx"`

---

**文档版本**: 2.0  
**最后更新**: 2026-04-01  
**维护者**: Cascade AI
