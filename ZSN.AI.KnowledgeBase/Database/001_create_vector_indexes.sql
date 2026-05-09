-- ============================================================================
-- ZSN.AI.KnowledgeBase 向量索引创建脚本
-- 用于提升向量相似度搜索的性能
--
-- 注意：
-- 1. 此脚本应该在初始化后，根据实际使用的 embedding 模型维度单独运行
-- 2. ivfflat 索引不支持可变维度，所以需要为每种维度创建单独的索引
-- 3. 创建索引可能需要较长时间，建议使用 CONCURRENTLY 选项避免锁表
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. 1536维向量索引（OpenAI text-embedding-ada-002）
-- ----------------------------------------------------------------------------

-- CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_document_chunks_embedding_1536
--     ON document_chunks USING ivfflat (embedding vector_cosine_ops)
--     WITH (lists = 100)
--     WHERE embedding::text LIKE '[%]'
--       AND array_length(regexp_split_to_array(embedding::text, ','), 1) = 1536;

-- CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_entity_embeddings_embedding_1536
--     ON entity_embeddings USING ivfflat (embedding vector_cosine_ops)
--     WITH (lists = 100)
--     WHERE embedding::text LIKE '[%]'
--       AND array_length(regexp_split_to_array(embedding::text, ','), 1) = 1536;

-- ----------------------------------------------------------------------------
-- 2. 1024维向量索引（BGE-large-zh 等模型）
-- ----------------------------------------------------------------------------

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_document_chunks_embedding_1024
    ON document_chunks USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100)
    WHERE embedding::text LIKE '[%]'
      AND array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) = 1024;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_entity_embeddings_embedding_1024
    ON entity_embeddings USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100)
    WHERE embedding::text LIKE '[%]'
      AND array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) = 1024;

-- ----------------------------------------------------------------------------
-- 3. 768维向量索引（BGE-base-zh 等模型）
-- ----------------------------------------------------------------------------

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_document_chunks_embedding_768
    ON document_chunks USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100)
    WHERE embedding::text LIKE '[%]'
      AND array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) = 768;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_entity_embeddings_embedding_768
    ON entity_embeddings USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100)
    WHERE embedding::text LIKE '[%]'
      AND array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) = 768;

-- ----------------------------------------------------------------------------
-- 4. 512维向量索引（BGE-small-zh 等模型）
-- ----------------------------------------------------------------------------

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_document_chunks_embedding_512
    ON document_chunks USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100)
    WHERE embedding::text LIKE '[%]'
      AND array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) = 512;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_entity_embeddings_embedding_512
    ON entity_embeddings USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100)
    WHERE embedding::text LIKE '[%]'
      AND array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) = 512;

-- ----------------------------------------------------------------------------
-- 5. 384维向量索引（all-MiniLM-L6-v2 等模型）
-- ----------------------------------------------------------------------------

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_document_chunks_embedding_384
    ON document_chunks USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100)
    WHERE embedding::text LIKE '[%]'
      AND array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) = 384;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_entity_embeddings_embedding_384
    ON entity_embeddings USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100)
    WHERE embedding::text LIKE '[%]'
      AND array_length(regexp_split_to_array(translate(embedding::text, ' []', ''), ','), 1) = 384;

-- ----------------------------------------------------------------------------
-- 6. 查看索引创建状态
-- ----------------------------------------------------------------------------

-- 查看所有向量相关索引
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename IN ('document_chunks', 'entity_embeddings')
  AND indexname LIKE '%embedding%'
ORDER BY tablename, indexname;

-- 查看索引大小
SELECT
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE tablename IN ('document_chunks', 'entity_embeddings')
  AND indexname LIKE '%embedding%'
ORDER BY pg_relation_size(indexrelid) DESC;

-- ----------------------------------------------------------------------------
-- 注意事项
-- ----------------------------------------------------------------------------

DO $$
BEGIN
    RAISE NOTICE '===========================================';
    RAISE NOTICE '向量索引创建完成！';
    RAISE NOTICE '===========================================';
    RAISE NOTICE '';
    RAISE NOTICE '索引说明：';
    RAISE NOTICE '- 使用 ivfflat 索引类型，适合大规模向量检索';
    RAISE NOTICE '- 使用 vector_cosine_ops 操作符，基于余弦相似度';
    RAISE NOTICE '- 使用 CONCURRENTLY 选项，避免锁表';
    RAISE NOTICE '- 使用部分索引（WHERE子句），只为特定维度的向量创建索引';
    RAISE NOTICE '';
    RAISE NOTICE '性能建议：';
    RAISE NOTICE '- lists 参数建议设置为：sqrt(行数)';
    RAISE NOTICE '- 例如：100万行数据，lists = 1000';
    RAISE NOTICE '';
    RAISE NOTICE '下一步：';
    RAISE NOTICE '- 使用 EXPLAIN ANALYZE 验证查询性能';
    RAISE NOTICE '- 根据实际数据量调整 lists 参数';
    RAISE NOTICE '===========================================';
END $$;
