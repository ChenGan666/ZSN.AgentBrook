-- ============================================================
-- Apache AGE 扩展安装脚本
-- ============================================================
-- 说明: 此脚本用于在 PostgreSQL 数据库中安装和配置 Apache AGE
-- 数据库: zsn_agentbrook_base
-- 使用方法: 在 PostgreSQL 数据库中执行此脚本
-- ============================================================

-- 1. 检查并安装 Apache AGE 扩展
CREATE EXTENSION IF NOT EXISTS age;

-- 2. 加载 AGE 扩展到当前会话
LOAD 'age';

-- 3. 设置 search_path 以包含 AGE 函数
SET search_path = ag_catalog, "$user", public;

-- 4. 创建主知识图谱图（如果不存在）
-- 注意: 如果图已存在，会报错，可以忽略
SELECT ag_catalog.create_graph('knowledge_graph');

-- 5. 创建测试用图（可选）
SELECT ag_catalog.create_graph('test_knowledge_graph');

-- 6. 验证安装
-- 应该返回 agtype 类型的结果
SELECT * FROM ag_catalog.cypher('knowledge_graph', $$
    CREATE (p:Person {name: '测试用户', role: '测试者'})
    RETURN p
$$) as (p agtype);

-- 7. 查询验证
SELECT * FROM ag_catalog.cypher('knowledge_graph', $$
    MATCH (n) RETURN n
$$) as (n agtype);

-- ============================================================
-- 安装完成提示
-- ============================================================
-- 如果看到上述查询返回了结果，说明 Apache AGE 安装成功！
-- ============================================================
