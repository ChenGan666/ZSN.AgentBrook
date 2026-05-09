-- 添加唯一约束到已存在的表
-- 添加唯一约束到 document_chunks.chunk_id
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'document_chunks_chunk_id_key'
    ) THEN
        ALTER TABLE document_chunks ADD CONSTRAINT document_chunks_chunk_id_key UNIQUE (chunk_id);
    END IF;
END $$;

-- entity_embeddings 表已经有 entity_id 的 UNIQUE 约束（定义中已包含），无需添加
