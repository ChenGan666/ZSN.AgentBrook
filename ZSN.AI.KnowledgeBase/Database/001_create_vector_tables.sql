-- 启用 pgvector 扩展
-- 使用 DO 块处理类型残留冲突：只删除残留类型，不影响已有数据
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'vector') THEN
        -- 清理残留的 vector 类型（不完整安装留下的，不带 CASCADE 避免误删表）
        DELETE FROM pg_type WHERE typname = 'vector' AND typnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'public');
        CREATE EXTENSION vector;
    END IF;
END
$$;

-- 创建文档块向量表（使用可变维度）
-- 使用 IF NOT EXISTS 避免删除已存在的数据
CREATE TABLE IF NOT EXISTS document_chunks (
    id BIGSERIAL PRIMARY KEY,
    document_id VARCHAR(255) NOT NULL,
    chunk_id VARCHAR(255) NOT NULL UNIQUE,
    content TEXT NOT NULL,
    embedding vector,  -- 可变维度，支持不同embedding模型
    metadata JSONB,
    token_count INTEGER,
    sequence_number INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 创建文档块向量表的基础索引
CREATE INDEX IF NOT EXISTS idx_document_chunks_document_id ON document_chunks(document_id);

-- 注意：ivfflat 索引不支持可变维度的向量列
-- 如果需要高性能搜索，建议在使用时为特定维度创建索引
-- 例如：CREATE INDEX ON document_chunks USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);

-- 创建实体向量表（使用可变维度）
-- 使用 IF NOT EXISTS 避免删除已存在的数据
CREATE TABLE IF NOT EXISTS entity_embeddings (
    id BIGSERIAL PRIMARY KEY,
    entity_id VARCHAR(255) NOT NULL UNIQUE,
    entity_type VARCHAR(100) NOT NULL,
    entity_text TEXT NOT NULL,
    embedding vector,  -- 可变维度，支持不同embedding模型
    metadata JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 创建实体向量表的基础索引
CREATE INDEX IF NOT EXISTS idx_entity_embeddings_entity_id ON entity_embeddings(entity_id);
CREATE INDEX IF NOT EXISTS idx_entity_embeddings_entity_type ON entity_embeddings(entity_type);

-- 创建更新时间触发器函数（使用 OR REPLACE）
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 删除已存在的触发器（以便重新创建）
DROP TRIGGER IF EXISTS update_document_chunks_updated_at ON document_chunks;
DROP TRIGGER IF EXISTS update_entity_embeddings_updated_at ON entity_embeddings;

-- 创建更新时间触发器
CREATE TRIGGER update_document_chunks_updated_at BEFORE UPDATE ON document_chunks
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_entity_embeddings_updated_at BEFORE UPDATE ON entity_embeddings
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- 添加注释
COMMENT ON TABLE document_chunks IS '文档块向量存储表，用于语义检索';
COMMENT ON TABLE entity_embeddings IS '实体向量存储表，用于实体相似度计算';
COMMENT ON COLUMN document_chunks.embedding IS '文本向量（使用 pgvector 的 vector 类型）';
COMMENT ON COLUMN entity_embeddings.embedding IS '实体向量（使用 pgvector 的 vector 类型）';
