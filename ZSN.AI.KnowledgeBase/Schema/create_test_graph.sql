-- 创建测试用图数据库
-- 用于单元测试和开发

-- 1. 创建测试图
SELECT ag_catalog.create_graph('test_knowledge_graph');

-- 2. 在测试图中创建一些示例数据
SELECT * FROM cypher('test_knowledge_graph', $$
    CREATE (p1:Person {name: '张三', role: '工程师', department: '研发部'})
    RETURN p1
$$) as (p1 agtype);

SELECT * FROM cypher('test_knowledge_graph', $$
    CREATE (p2:Person {name: '李四', role: '产品经理', department: '产品部'})
    RETURN p2
$$) as (p2 agtype);

SELECT * FROM cypher('test_knowledge_graph', $$
    CREATE (c1:Company {name: '某某科技公司', type: '私营'})
    RETURN c1
$$) as (c1 agtype);

-- 3. 创建关系
SELECT * FROM cypher('test_knowledge_graph', $$
    MATCH (p1:Person {name: '张三'}), (c1:Company {name: '某某科技公司'})
    CREATE (p1)-[r:WORKS_FOR {since: 2020}]->(c1)
    RETURN r
$$) as (r agtype);

SELECT * FROM cypher('test_knowledge_graph', $$
    MATCH (p1:Person {name: '张三'}), (p2:Person {name: '李四'})
    CREATE (p1)-[r:COLLEAGUE {project: 'AI知识库'}]->(p2)
    RETURN r
$$) as (r agtype);

-- 4. 验证数据
SELECT * FROM cypher('test_knowledge_graph', $$
    MATCH (n) RETURN n
$$) as (n agtype);

SELECT * FROM cypher('test_knowledge_graph', $$
    MATCH ()-[r]->() RETURN r
$$) as (r agtype);
