-- Apache AGE 初始化脚本
-- 知识图谱功能初始化

-- 1. 安装Apache AGE扩展（如果尚未安装）
CREATE EXTENSION IF NOT EXISTS age;

-- 2. 加载AGE扩展
LOAD 'age';

-- 3. 设置search_path以包含AGE
SET search_path = ag_catalog, "$user", public;

-- 4. 创建主知识图谱图（如果不存在）
SELECT ag_catalog.create_graph('knowledge_graph');

-- 注释：
-- - 此脚本应在 PostgreSQL 数据库中执行
-- - 执行前确保已安装 Apache AGE 扩展
-- - 如果数据库中已有 'knowledge_graph' 图，此操作将报错，可以忽略
