-- 添加 sequence_number 字段到 document_chunks 表
-- 这个字段用于跟踪分块的顺序
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'document_chunks' AND column_name = 'sequence_number'
    ) THEN
        ALTER TABLE document_chunks ADD COLUMN sequence_number INTEGER DEFAULT 0;
    END IF;
END $$;
