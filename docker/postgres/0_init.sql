-- ============================================================================
-- PostgreSQL 初始化脚本
-- 包含: 知识库管理 + ClawAI记忆系统
-- 版本: 2.0
-- 日期: 2026-04-01
-- ============================================================================

-- 创建数据库
CREATE DATABASE zsn_agentbrook_base OWNER postgres;

-- 切换到新数据库
\c zsn_agentbrook_base;

-- ============================================================================
-- 第一部分: 扩展安装
-- ============================================================================

-- 1. 添加pgvector插件（向量搜索）
CREATE EXTENSION IF NOT EXISTS vector;

-- 2. 添加Apache AGE插件（图数据库）
CREATE EXTENSION IF NOT EXISTS age;
LOAD 'age';
SET search_path = ag_catalog, "$user", public;

-- ============================================================================
-- 第二部分: ClawAI 记忆系统表结构
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 2.1 长期记忆表（增强版 - 支持向量搜索）
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS tb_claw_long_term_memory CASCADE;

CREATE TABLE tb_claw_long_term_memory (
    -- 基础字段
    memory_id VARCHAR(50) PRIMARY KEY,
    app_id VARCHAR(50) NOT NULL,
    claw_id VARCHAR(50) NOT NULL,
    session_id VARCHAR(50),
    member_id VARCHAR(50),
    
    -- 知识内容
    knowledge_type VARCHAR(50) NOT NULL,  -- concept/fact/procedure/experience/qa/preference/failed_experience
    topic VARCHAR(200),
    summary TEXT,
    content TEXT,
    
    -- 向量嵌入（pgvector原生类型）
    embedding vector(1536),  -- OpenAI text-embedding-3-small/large (1536维)
    
    -- 评分和统计
    importance INT DEFAULT 50 CHECK (importance >= 0 AND importance <= 100),
    access_count INT DEFAULT 0,
    last_access_time TIMESTAMP,
    
    -- 来源信息
    source_type VARCHAR(50),  -- episodic/user_input/system
    source_id VARCHAR(50),
    
    -- 元数据（PostgreSQL原生JSON类型）
    metadata JSONB,
    
    -- 时间戳
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_update_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 创建索引
CREATE INDEX idx_ltm_app_member ON tb_claw_long_term_memory(app_id, member_id);
CREATE INDEX idx_ltm_topic ON tb_claw_long_term_memory(topic);
CREATE INDEX idx_ltm_type ON tb_claw_long_term_memory(knowledge_type);
CREATE INDEX idx_ltm_importance ON tb_claw_long_term_memory(importance DESC);
CREATE INDEX idx_ltm_create_time ON tb_claw_long_term_memory(create_time DESC);
CREATE INDEX idx_ltm_metadata ON tb_claw_long_term_memory USING GIN(metadata);

-- 创建HNSW向量索引（最优性能 - 推荐）
-- 参数说明:
--   m = 16: 每层最大连接数（默认16，范围2-100）
--   ef_construction = 64: 构建时搜索深度（默认64，范围4-1000）
CREATE INDEX idx_ltm_embedding_hnsw ON tb_claw_long_term_memory 
USING hnsw (embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);

-- 备选：IVFFlat索引（较快构建，但查询性能稍低）
-- CREATE INDEX idx_ltm_embedding_ivfflat ON tb_claw_long_term_memory 
-- USING ivfflat (embedding vector_cosine_ops)
-- WITH (lists = 100);

-- 创建更新时间触发器
CREATE OR REPLACE FUNCTION update_last_update_time()
RETURNS TRIGGER AS $$
BEGIN
    NEW.last_update_time = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_ltm_update_time
BEFORE UPDATE ON tb_claw_long_term_memory
FOR EACH ROW
EXECUTE FUNCTION update_last_update_time();

-- 添加表注释
COMMENT ON TABLE tb_claw_long_term_memory IS 'ClawAI长期记忆表 - 支持向量语义检索';
COMMENT ON COLUMN tb_claw_long_term_memory.embedding IS '向量嵌入(1536维) - 用于语义相似度检索';
COMMENT ON COLUMN tb_claw_long_term_memory.importance IS '重要性评分(0-100) - 用于记忆优先级排序';
COMMENT ON COLUMN tb_claw_long_term_memory.metadata IS 'JSON元数据 - 存储扩展信息';

-- ----------------------------------------------------------------------------
-- 2.2 知识关系表（图数据库 - 支持Apache AGE）
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS tb_claw_knowledge_relation CASCADE;

CREATE TABLE tb_claw_knowledge_relation (
    relation_id VARCHAR(50) PRIMARY KEY,
    app_id VARCHAR(50) NOT NULL,
    source_memory_id VARCHAR(50) NOT NULL,
    target_memory_id VARCHAR(50) NOT NULL,
    relation_type VARCHAR(50) NOT NULL,  -- related/prerequisite/derived/conflict/example/category
    strength FLOAT DEFAULT 0.5 CHECK (strength >= 0 AND strength <= 1),
    metadata JSONB,
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_update_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (source_memory_id) REFERENCES tb_claw_long_term_memory(memory_id) ON DELETE CASCADE,
    FOREIGN KEY (target_memory_id) REFERENCES tb_claw_long_term_memory(memory_id) ON DELETE CASCADE
);

-- 创建索引
CREATE INDEX idx_kr_source ON tb_claw_knowledge_relation(source_memory_id);
CREATE INDEX idx_kr_target ON tb_claw_knowledge_relation(target_memory_id);
CREATE INDEX idx_kr_app ON tb_claw_knowledge_relation(app_id);
CREATE INDEX idx_kr_type ON tb_claw_knowledge_relation(relation_type);
CREATE INDEX idx_kr_strength ON tb_claw_knowledge_relation(strength DESC);
CREATE INDEX idx_kr_metadata ON tb_claw_knowledge_relation USING GIN(metadata);

-- 创建更新时间触发器
CREATE TRIGGER trg_kr_update_time
BEFORE UPDATE ON tb_claw_knowledge_relation
FOR EACH ROW
EXECUTE FUNCTION update_last_update_time();

-- 添加表注释
COMMENT ON TABLE tb_claw_knowledge_relation IS 'ClawAI知识关系表 - 支持图数据库查询';
COMMENT ON COLUMN tb_claw_knowledge_relation.relation_type IS '关系类型: related(相关)/prerequisite(前置)/derived(派生)/conflict(冲突)/example(示例)/category(分类)';
COMMENT ON COLUMN tb_claw_knowledge_relation.strength IS '关系强度(0-1) - 用于图查询权重';

-- 创建Apache AGE图
SELECT create_graph('knowledge_graph');

-- ----------------------------------------------------------------------------
-- 2.3 用户反馈表
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS tb_claw_user_feedback CASCADE;

CREATE TABLE tb_claw_user_feedback (
    feedback_id VARCHAR(50) PRIMARY KEY,
    app_id VARCHAR(50) NOT NULL,
    session_id VARCHAR(50) NOT NULL,
    member_id VARCHAR(50) NOT NULL,
    memory_id VARCHAR(50),
    
    -- 对话内容
    user_query TEXT,
    ai_response TEXT,
    
    -- 反馈信息
    feedback_type VARCHAR(20) NOT NULL CHECK (feedback_type IN ('positive', 'negative', 'neutral')),
    feedback_score INT CHECK (feedback_score >= 1 AND feedback_score <= 5),
    feedback_comment TEXT,
    
    -- 使用的记忆（JSON数组）
    used_memories JSONB,
    
    -- 元数据
    metadata JSONB,
    
    -- 时间戳
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (memory_id) REFERENCES tb_claw_long_term_memory(memory_id) ON DELETE SET NULL
);

-- 创建索引
CREATE INDEX idx_fb_app ON tb_claw_user_feedback(app_id);
CREATE INDEX idx_fb_member ON tb_claw_user_feedback(member_id);
CREATE INDEX idx_fb_memory ON tb_claw_user_feedback(memory_id);
CREATE INDEX idx_fb_session ON tb_claw_user_feedback(session_id);
CREATE INDEX idx_fb_type ON tb_claw_user_feedback(feedback_type);
CREATE INDEX idx_fb_time ON tb_claw_user_feedback(create_time DESC);
CREATE INDEX idx_fb_used_memories ON tb_claw_user_feedback USING GIN(used_memories);

-- 添加表注释
COMMENT ON TABLE tb_claw_user_feedback IS 'ClawAI用户反馈表 - 用于知识质量评估和动态调整';
COMMENT ON COLUMN tb_claw_user_feedback.feedback_type IS '反馈类型: positive(正面)/negative(负面)/neutral(中性)';
COMMENT ON COLUMN tb_claw_user_feedback.used_memories IS 'JSON数组 - 存储使用的记忆ID列表';