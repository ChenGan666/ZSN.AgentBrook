-- ============================================================================
-- ZSN.AI.KnowledgeBase 数据库初始化脚本
-- 用于 Docker 部署时的数据库初始化
--
-- 功能：
-- 1. 启用必要的 PostgreSQL 扩展（pgvector, Apache AGE）
-- 2. 创建向量存储表（文档块向量、实体向量）
-- 3. 创建触发器和索引
-- 4. 初始化 Apache AGE 图数据库环境
--
-- 使用方法：
-- 在 Docker 启动时将此脚本挂载到 /docker-entrypoint-initdb.d/ 目录
-- PostgreSQL 会自动执行此脚本
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. 启用必要的 PostgreSQL 扩展
-- ----------------------------------------------------------------------------

-- 启用 pgvector 扩展（用于向量相似度搜索）
CREATE EXTENSION IF NOT EXISTS vector;

-- 启用 Apache AGE 扩展（用于知识图谱存储）
CREATE EXTENSION IF NOT EXISTS age;

COMMENT ON EXTENSION vector IS '向量相似度搜索扩展，支持高维向量存储和检索';
COMMENT ON EXTENSION age IS 'Apache AGE - PostgreSQL 的图数据库扩展';

-- ----------------------------------------------------------------------------
-- 2. 初始化 Apache AGE 环境
-- ----------------------------------------------------------------------------

-- 加载 AGE 扩展
LOAD 'age';

-- 设置 search_path 包含 ag_catalog
SET search_path = ag_catalog, "$user", public;

-- 创建默认的知识图谱（如果不存在）
-- 注意：这里创建一个全局的图，实际使用时会按知识库ID创建独立的图
DO $$
BEGIN
    -- 检查图是否已存在
    IF NOT EXISTS (SELECT 1 FROM ag_catalog.ag_graph WHERE name = 'knowledge_graph') THEN
        PERFORM ag_catalog.create_graph('knowledge_graph');
        RAISE NOTICE '已创建知识图谱: knowledge_graph';
    ELSE
        RAISE NOTICE '知识图谱已存在: knowledge_graph';
    END IF;
END $$;

COMMENT ON GRAPH knowledge_graph IS '默认的知识图谱，用于存储实体和关系';

-- ----------------------------------------------------------------------------
-- 3. 创建向量存储表
-- ----------------------------------------------------------------------------

-- 3.1 创建文档块向量表
CREATE TABLE IF NOT EXISTS document_chunks (
    id BIGSERIAL PRIMARY KEY,
    document_id VARCHAR(255) NOT NULL,           -- 文档ID（对应知识库文件）
    chunk_id VARCHAR(255) NOT NULL UNIQUE,       -- 分块ID（全局唯一）
    content TEXT NOT NULL,                        -- 分块内容
    embedding vector,                             -- 文本向量（可变维度，支持不同embedding模型）
    metadata JSONB,                               -- 元数据（JSON格式）
    token_count INTEGER DEFAULT 0,                -- Token数量
    sequence_number INTEGER DEFAULT 0,            -- 分块序号（保持原文顺序）
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 创建文档块向量表的索引
CREATE INDEX IF NOT EXISTS idx_document_chunks_document_id ON document_chunks(document_id);
CREATE INDEX IF NOT EXISTS idx_document_chunks_sequence_number ON document_chunks(document_id, sequence_number);

-- 3.2 创建实体向量表
CREATE TABLE IF NOT EXISTS entity_embeddings (
    id BIGSERIAL PRIMARY KEY,
    entity_id VARCHAR(255) NOT NULL UNIQUE,      -- 实体ID（全局唯一）
    entity_type VARCHAR(100) NOT NULL,           -- 实体类型（PERSON, ORG, LOC等）
    entity_text TEXT NOT NULL,                   -- 实体文本
    embedding vector,                             -- 实体向量（可变维度）
    metadata JSONB,                               -- 元数据（JSON格式）
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 创建实体向量表的索引
CREATE INDEX IF NOT EXISTS idx_entity_embeddings_entity_id ON entity_embeddings(entity_id);
CREATE INDEX IF NOT EXISTS idx_entity_embeddings_entity_type ON entity_embeddings(entity_type);

-- ----------------------------------------------------------------------------
-- 4. 创建触发器和函数
-- ----------------------------------------------------------------------------

-- 4.1 创建更新时间触发器函数
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

COMMENT ON FUNCTION update_updated_at_column() IS '自动更新 updated_at 字段的触发器函数';

-- 4.2 为 document_chunks 表创建更新时间触发器
DROP TRIGGER IF EXISTS update_document_chunks_updated_at ON document_chunks;
CREATE TRIGGER update_document_chunks_updated_at
    BEFORE UPDATE ON document_chunks
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 4.3 为 entity_embeddings 表创建更新时间触发器
DROP TRIGGER IF EXISTS update_entity_embeddings_updated_at ON entity_embeddings;
CREATE TRIGGER update_entity_embeddings_updated_at
    BEFORE UPDATE ON entity_embeddings
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ----------------------------------------------------------------------------
-- 5. 添加表和字段注释
-- ----------------------------------------------------------------------------

-- document_chunks 表注释
COMMENT ON TABLE document_chunks IS '文档块向量存储表，用于语义检索和混合搜索';
COMMENT ON COLUMN document_chunks.id IS '自增主键';
COMMENT ON COLUMN document_chunks.document_id IS '文档ID，通常对应知识库中的文件ID';
COMMENT ON COLUMN document_chunks.chunk_id IS '分块ID，全局唯一标识符';
COMMENT ON COLUMN document_chunks.content IS '分块的文本内容';
COMMENT ON COLUMN document_chunks.embedding IS '文本向量（使用 pgvector 的 vector 类型）';
COMMENT ON COLUMN document_chunks.metadata IS '元数据，包含分块的附加信息（JSON格式）';
COMMENT ON COLUMN document_chunks.token_count IS '分块的Token数量';
COMMENT ON COLUMN document_chunks.sequence_number IS '分块序号，用于保持原文顺序';
COMMENT ON COLUMN document_chunks.created_at IS '创建时间';
COMMENT ON COLUMN document_chunks.updated_at IS '最后更新时间';

-- entity_embeddings 表注释
COMMENT ON TABLE entity_embeddings IS '实体向量存储表，用于实体相似度计算和实体链接';
COMMENT ON COLUMN entity_embeddings.id IS '自增主键';
COMMENT ON COLUMN entity_embeddings.entity_id IS '实体ID，全局唯一标识符';
COMMENT ON COLUMN entity_embeddings.entity_type IS '实体类型（PERSON, ORG, LOC, CONCEPT等）';
COMMENT ON COLUMN entity_embeddings.entity_text IS '实体文本内容';
COMMENT ON COLUMN entity_embeddings.embedding IS '实体向量（使用 pgvector 的 vector 类型）';
COMMENT ON COLUMN entity_embeddings.metadata IS '元数据，包含实体的附加信息（JSON格式）';
COMMENT ON COLUMN entity_embeddings.created_at IS '创建时间';
COMMENT ON COLUMN entity_embeddings.updated_at IS '最后更新时间';

-- ----------------------------------------------------------------------------
-- 6. 创建性能优化视图（可选）
-- ----------------------------------------------------------------------------

-- 创建文档分块统计视图
CREATE OR REPLACE VIEW v_document_chunk_stats AS
SELECT
    document_id,
    COUNT(*) as total_chunks,
    SUM(token_count) as total_tokens,
    AVG(token_count) as avg_tokens_per_chunk,
    MIN(sequence_number) as min_sequence,
    MAX(sequence_number) as max_sequence,
    MAX(updated_at) as last_updated
FROM document_chunks
GROUP BY document_id;

COMMENT ON VIEW v_document_chunk_stats IS '文档分块统计视图，提供每个文档的分块统计信息';

-- 创建实体类型分布视图
CREATE OR REPLACE VIEW v_entity_type_distribution AS
SELECT
    entity_type,
    COUNT(*) as entity_count,
    COUNT(DISTINCT entity_text) as unique_entities
FROM entity_embeddings
GROUP BY entity_type
ORDER BY entity_count DESC;

COMMENT ON VIEW v_entity_type_distribution IS '实体类型分布视图，统计每种类型的实体数量';

-- ----------------------------------------------------------------------------
-- 7. 性能优化建议（注释）
-- ----------------------------------------------------------------------------

-- 注意：ivfflat 索引不支持可变维度的向量列
-- 如果需要高性能搜索，建议在使用时为特定维度创建索引
--
-- 示例：为1536维向量创建 ivfflat 索引（OpenAI ada-002模型）
-- CREATE INDEX CONCURRENTLY idx_document_chunks_embedding_ivfflat
--     ON document_chunks USING ivfflat (embedding vector_cosine_ops)
--     WITH (lists = 100)
--     WHERE embedding::text LIKE '[%]' AND array_length(regexp_split_to_array(embedding::text, ','), 1) = 1536;
--
-- 示例：为1024维向量创建 ivfflat 索引（BGE模型）
-- CREATE INDEX CONCURRENTLY idx_document_chunks_embedding_1024
--     ON document_chunks USING ivfflat (embedding vector_cosine_ops)
--     WITH (lists = 100)
--     WHERE embedding::text LIKE '[%]' AND array_length(regexp_split_to_array(embedding::text, ','), 1) = 1024;

-- ----------------------------------------------------------------------------
-- 8. 初始化完成
-- ----------------------------------------------------------------------------

-- 记录初始化完成
DO $$
BEGIN
    RAISE NOTICE '===========================================';
    RAISE NOTICE 'ZSN.AI.KnowledgeBase 数据库初始化完成！';
    RAISE NOTICE '===========================================';
    RAISE NOTICE '已启用的扩展：pgvector, Apache AGE';
    RAISE NOTICE '已创建的表：document_chunks, entity_embeddings';
    RAISE NOTICE '已创建的图：knowledge_graph';
    RAISE NOTICE '已创建的视图：v_document_chunk_stats, v_entity_type_distribution';
    RAISE NOTICE '';
    RAISE NOTICE '下一步：';
    RAISE NOTICE '1. 根据实际使用的embedding模型维度，创建相应的向量索引';
    RAISE NOTICE '2. 通过应用程序导入文档和构建知识图谱';
    RAISE NOTICE '===========================================';
END $$;
