-- ============================================================
-- 图片支持表结构 (IMPL_01)
-- 执行前确保已启用 pgvector 扩展:
--   CREATE EXTENSION IF NOT EXISTS vector;
-- ============================================================

-- 图片信息主表
CREATE TABLE IF NOT EXISTS document_images (
    id BIGSERIAL PRIMARY KEY,
    document_id VARCHAR(255) NOT NULL,
    image_id VARCHAR(255) NOT NULL UNIQUE,
    page_number INTEGER,
    sequence_number INTEGER DEFAULT 0,
    original_filename VARCHAR(500),
    storage_path TEXT NOT NULL,
    storage_type VARCHAR(20) DEFAULT 'file',
    mime_type VARCHAR(100),
    file_size BIGINT,
    width INTEGER,
    height INTEGER,
    content_hash VARCHAR(64),
    description TEXT,
    ocr_text TEXT,
    description_status VARCHAR(20) DEFAULT 'pending',
    is_decorative BOOLEAN DEFAULT FALSE,
    metadata JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_document_images_document_id ON document_images(document_id);
CREATE INDEX IF NOT EXISTS idx_document_images_hash ON document_images(content_hash);
CREATE INDEX IF NOT EXISTS idx_document_images_status ON document_images(description_status);

-- 分块-图片关联表
CREATE TABLE IF NOT EXISTS chunk_image_relations (
    id BIGSERIAL PRIMARY KEY,
    chunk_id VARCHAR(255) NOT NULL,
    image_id VARCHAR(255) NOT NULL,
    relation_type VARCHAR(20) DEFAULT 'nearby',
    UNIQUE(chunk_id, image_id)
);

CREATE INDEX IF NOT EXISTS idx_cir_chunk_id ON chunk_image_relations(chunk_id);
CREATE INDEX IF NOT EXISTS idx_cir_image_id ON chunk_image_relations(image_id);

-- CLIP向量表（第三阶段用，可先建空表）
CREATE TABLE IF NOT EXISTS image_embeddings (
    id BIGSERIAL PRIMARY KEY,
    image_id VARCHAR(255) NOT NULL UNIQUE,
    embedding vector,
    model_name VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
