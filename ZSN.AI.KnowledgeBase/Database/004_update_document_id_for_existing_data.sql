-- 更新现有数据的 source_document_id
-- 此脚本用于为之前创建的实体和关系添加 source_document_id 属性
-- 注意：此脚本需要根据实际情况修改 documentId 的值

-- 为实体添加 source_document_id（需要根据实际情况指定 documentId）
-- 示例：将所有没有 source_document_id 的实体设置为某个文档ID
-- MATCH (e:Entity) WHERE e.source_document_id IS NULL
-- SET e.source_document_id = 'your_document_id_here';

-- 为关系添加 source_document_id（需要根据实际情况指定 documentId）
-- 示例：将所有没有 source_document_id 的关系设置为某个文档ID
-- MATCH ()-[r:RELATION]->() WHERE r.source_document_id IS NULL
-- SET r.source_document_id = 'your_document_id_here';

-- 或者，如果你想删除所有旧数据（重新导入）：
-- MATCH (e:Entity) DELETE e;
-- MATCH ()-[r:RELATION]->() DELETE r;

-- 查询当前有多少实体没有 source_document_id
MATCH (e:Entity) WHERE e.source_document_id IS NULL OR e.source_document_id = ''
RETURN count(e) as entities_without_document;

-- 查询当前有多少关系没有 source_document_id
MATCH ()-[r:RELATION]->() WHERE r.source_document_id IS NULL OR r.source_document_id = ''
RETURN count(r) as relations_without_document;

-- 查看所有 source_document_id 的分布情况
MATCH (e:Entity)
RETURN e.source_document_id, count(e) as entity_count
ORDER BY entity_count DESC;
