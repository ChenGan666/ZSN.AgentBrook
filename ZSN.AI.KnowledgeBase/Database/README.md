# ZSN.AI.KnowledgeBase 数据库部署说明

## 📋 目录结构

```
Database/
├── 000_init_database.sql              # 数据库初始化脚本（主脚本）
├── 001_create_vector_indexes.sql      # 向量索引创建脚本（可选）
├── 002_add_unique_constraints.sql     # 添加唯一约束（迁移脚本）
├── 003_add_sequence_number.sql        # 添加序列号字段（迁移脚本）
├── 004_update_document_id_for_existing_data.sql  # 数据迁移脚本
├── docker-compose.yml                 # Docker Compose 配置
└── README.md                          # 本文件
```

---

## 🚀 快速开始

### 方式1：使用 Docker Compose（推荐）

1. **启动数据库服务**

```bash
cd ZSN.AI.KnowledgeBase/Database
docker-compose up -d
```

2. **查看初始化日志**

```bash
docker-compose logs -f postgres
```

3. **验证安装**

```bash
# 连接到数据库
docker exec -it zsn-knowledgebase-db psql -U postgres -d ClawAI

# 在 psql 中执行
\dx                    # 查看已安装的扩展
SELECT * FROM ag_graph;  # 查看 AGE 图数据库
\dt                    # 查看创建的表
\q                     # 退出
```

### 方式2：手动部署到现有 PostgreSQL

1. **确保已安装 pgvector 和 Apache AGE**

```bash
# Ubuntu/Debian
sudo apt-get install postgresql-16-age postgresql-16-pgvector

# macOS (使用 Homebrew)
brew install age
brew install pgvector
```

2. **执行初始化脚本**

```bash
psql -U postgres -d ClawAI -f 000_init_database.sql
```

3. **（可选）创建向量索引**

```bash
psql -U postgres -d ClawAI -f 001_create_vector_indexes.sql
```

---

## 📊 数据库结构

### 扩展（Extensions）

| 扩展名 | 用途 |
|--------|------|
| `vector` | 向量相似度搜索 |
| `age` | Apache AGE 图数据库 |

### 表结构（Tables）

#### 1. document_chunks - 文档块向量表

| 字段名 | 类型 | 说明 |
|--------|------|------|
| id | BIGSERIAL | 主键 |
| document_id | VARCHAR(255) | 文档ID |
| chunk_id | VARCHAR(255) | 分块ID（唯一） |
| content | TEXT | 分块内容 |
| embedding | vector | 文本向量 |
| metadata | JSONB | 元数据 |
| token_count | INTEGER | Token数量 |
| sequence_number | INTEGER | 分块序号 |
| created_at | TIMESTAMP | 创建时间 |
| updated_at | TIMESTAMP | 更新时间 |

**索引**：
- `idx_document_chunks_document_id` - 文档ID索引
- `idx_document_chunks_sequence_number` - 序号索引
- `idx_document_chunks_embedding_*` - 向量索引（可选）

#### 2. entity_embeddings - 实体向量表

| 字段名 | 类型 | 说明 |
|--------|------|------|
| id | BIGSERIAL | 主键 |
| entity_id | VARCHAR(255) | 实体ID（唯一） |
| entity_type | VARCHAR(100) | 实体类型 |
| entity_text | TEXT | 实体文本 |
| embedding | vector | 实体向量 |
| metadata | JSONB | 元数据 |
| created_at | TIMESTAMP | 创建时间 |
| updated_at | TIMESTAMP | 更新时间 |

**索引**：
- `idx_entity_embeddings_entity_id` - 实体ID索引
- `idx_entity_embeddings_entity_type` - 实体类型索引
- `idx_entity_embeddings_embedding_*` - 向量索引（可选）

### 图数据库（Apache AGE）

- **图名称**：`knowledge_graph`
- **节点标签**：`Entity`
- **边标签**：`RELATION`

---

## 🔧 配置说明

### 应用程序配置（appsettings.json）

```json
{
  "DbConnectionStrings": {
    "KnowledgeBaseDb": {
      "Connection": "Host=localhost;Port=5432;Database=ClawAI;Username=postgres;Password=Q1w2e3r4t5y6",
      "GraphName": "knowledge_graph"
    }
  },
  "LargeModel": {
    "EmbeddingModelID": 12,
    "ChatModelID": 13
  }
}
```

### 向量索引配置

根据使用的 embedding 模型维度选择创建相应的索引：

| 模型 | 维度 | 索引名称 |
|------|------|----------|
| OpenAI ada-002 | 1536 | `idx_document_chunks_embedding_1536` |
| BGE-large-zh | 1024 | `idx_document_chunks_embedding_1024` |
| BGE-base-zh | 768 | `idx_document_chunks_embedding_768` |
| BGE-small-zh | 512 | `idx_document_chunks_embedding_512` |
| all-MiniLM-L6-v2 | 384 | `idx_document_chunks_embedding_384` |

---

## 📈 性能优化

### 1. 向量索引优化

```sql
-- 创建 ivfflat 索引（根据数据量调整 lists 参数）
CREATE INDEX CONCURRENTLY idx_document_chunks_embedding_1024
    ON document_chunks USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100)
    WHERE embedding::text LIKE '[%]'
      AND array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) = 1024;
```

**lists 参数建议**：
- 10万行数据：lists = 100
- 100万行数据：lists = 1000
- 1000万行数据：lists = 10000

### 2. 查询优化

```sql
-- 使用 EXPLAIN ANALYZE 分析查询性能
EXPLAIN ANALYZE
SELECT chunk_id, content, 1 - (embedding <=> '[...]'::vector) as similarity
FROM document_chunks
WHERE embedding IS NOT NULL
ORDER BY embedding <=> '[...]'::vector
LIMIT 10;
```

### 3. 维护建议

```sql
-- 定期清理死元组
VACUUM ANALYZE document_chunks;
VACUUM ANALYZE entity_embeddings;

-- 重建索引（如果索引膨胀）
REINDEX INDEX CONCURRENTLY idx_document_chunks_embedding_1024;
```

---

## 🔍 故障排查

### 问题1：向量索引未生效

**症状**：查询性能没有提升

**解决方案**：
```sql
-- 检查索引是否存在
SELECT * FROM pg_indexes WHERE indexname LIKE '%embedding%';

-- 检查查询是否使用了索引
EXPLAIN ANALYZE SELECT ...;

-- 强制使用索引
SET enable_seqscan = off;
```

### 问题2：Apache AGE 图创建失败

**症状**：错误 "graph already exists" 或 "extension not loaded"

**解决方案**：
```sql
-- 检查 AGE 扩展是否已加载
LOAD 'age';

-- 检查图是否已存在
SELECT * FROM ag_graph;

-- 删除已存在的图
SELECT drop_graph('knowledge_graph', true);
```

### 问题3：向量维度不匹配

**症状**：错误 "vectors have different dimensions"

**解决方案**：
```sql
-- 检查向量的实际维度
SELECT
    chunk_id,
    array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) as dim
FROM document_chunks
WHERE embedding IS NOT NULL
LIMIT 10;

-- 删除错误维度的数据
DELETE FROM document_chunks
WHERE array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) != 1024;
```

---

## 📚 参考资料

- [pgvector 文档](https://github.com/pgvector/pgvector)
- [Apache AGE 文档](https://age.apache.org/)
- [PostgreSQL 文档](https://www.postgresql.org/docs/)

---

## 📝 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0.0 | 2026-04-09 | 初始版本，支持向量存储和知识图谱 |

---

## 📧 支持

如有问题，请联系项目维护者或提交 Issue。
