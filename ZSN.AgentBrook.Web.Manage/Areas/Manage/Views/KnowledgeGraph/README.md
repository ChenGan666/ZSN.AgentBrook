# 知识图谱管理系统

## 概述

知识图谱管理系统提供了完整的功能来管理和可视化 Claw AI 的知识图谱数据，包括知识节点（长期记忆）和知识关系的查看、删除和可视化。

## 功能特性

### 1. 知识节点列表（Index）
- **路径**: `/Manage/KnowledgeGraph/Index`
- **功能**:
  - 显示所有知识节点（长期记忆）
  - 支持按应用ID、节点ID筛选
  - 分页显示
  - 显示知识类型、主题、重要性、访问次数等
  - 操作按钮：可视化、详情、删除

### 2. 知识图谱可视化（Visualize）
- **路径**: `/Manage/KnowledgeGraph/Visualize?memoryId={id}`
- **功能**:
  - 基于 AntV G6 v4.8.24 引擎
  - 动态可视化知识图谱
  - 支持多种布局方式：
    - 力导向布局（Force）
    - 辐射布局（Radial）
    - 同心圆布局（Concentric）
    - 层次结构布局（Dagre）
  - 可调节图谱深度（1-3层）
  - 交互功能：
    - 节点拖拽
    - 画布缩放
    - 节点点击查看详情
    - 适应屏幕
    - 放大/缩小
  - 节点着色：
    - 中心节点：橙红色（#ff5722）
    - 按知识类型着色：
      - concept（概念）：蓝色
      - fact（事实）：绿色
      - procedure（流程）：橙色
      - experience（经验）：紫色
  - 关系类型：
    - related（相关）：灰色
    - prerequisite（前置）：绿色
    - derived（派生）：橙色
    - conflict（冲突）：红色
    - example（示例）：蓝色
    - category（分类）：紫色

### 3. 知识详情（Details）
- **路径**: `/Manage/KnowledgeGraph/Details?memoryId={id}`
- **功能**:
  - 显示知识节点的完整信息
  - 显示所有入度关系（指向该节点的关系）
  - 显示所有出度关系（从该节点出发的关系）
  - 可查看关系的元数据
  - 可删除关系
  - 可跳转到相关节点的可视化

### 4. API 接口

#### 4.1 获取图谱数据
```
GET /Manage/KnowledgeGraph/GetGraphData
参数:
  - memoryId: 知识节点ID（必需）
  - maxDepth: 最大深度（默认2，可选）
  - maxNodes: 最大节点数（默认50，可选）

返回: G6格式的图数据
{
  "nodes": [
    {
      "id": "节点ID",
      "label": "显示名称",
      "type": "center|normal",
      "knowledgeType": "知识类型",
      "topic": "主题",
      "importance": 重要性,
      "fullLabel": "完整标签",
      "content": "内容"
    }
  ],
  "edges": [
    {
      "source": "源节点ID",
      "target": "目标节点ID",
      "label": "关系标签",
      "type": "关系类型",
      "strength": 关系强度
    }
  ]
}
```

#### 4.2 删除知识节点
```
POST /Manage/KnowledgeGraph/DeleteMemory
参数:
  - memoryId: 知识节点ID

注意: 将同时删除所有相关的关系
```

#### 4.3 删除关系
```
POST /Manage/KnowledgeGraph/DeleteRelation
参数:
  - relationId: 关系ID
```

#### 4.4 获取统计数据
```
GET /Manage/KnowledgeGraph/GetStatistics
参数:
  - appId: 应用ID（可选，用于过滤）

返回: 统计数据
{
  "totalMemories": 总知识数,
  "totalRelations": 总关系数,
  "typeStats": 按类型统计,
  "topicStats": 按主题统计,
  "relationStats": 按关系类型统计
}
```

## 使用指南

### 访问知识图谱管理

1. **登录管理系统**
   - 访问管理后台
   - 使用管理员账号登录

2. **导航到知识图谱**
   - 在菜单中找到"知识图谱"或"Knowledge Graph"
   - 点击进入知识节点列表

3. **查看知识节点**
   - 列表显示所有知识节点
   - 可使用搜索框筛选特定应用或节点
   - 点击"可视化"按钮查看知识图谱

4. **使用知识图谱可视化**
   - 图谱自动加载选中节点及其相关知识
   - 可调整深度、布局方式
   - 点击节点查看详细信息
   - 使用工具栏进行缩放和导航

5. **查看知识详情**
   - 点击"详情"按钮查看完整信息
   - 查看所有相关关系（入度和出度）
   - 可删除单个关系
   - 可跳转到相关节点

6. **删除知识节点**
   - 在列表中点击"删除"
   - 确认删除操作
   - **注意**：将同时删除所有相关关系

## 技术实现

### 后端技术
- **框架**: ASP.NET Core MVC
- **架构**: Area + Controller + View
- **数据层**: ZSN.AI.BLL 业务逻辑层

### 前端技术
- **UI框架**: Layui
- **可视化**: AntV G6 v4.8.24
- **图表**: 自定义力导向图

### 数据结构

#### LongTermMemoryInfo（知识节点）
```csharp
public class LongTermMemoryInfo
{
    public string MemoryID { get; set; }
    public string AppID { get; set; }
    public string ClawID { get; set; }
    public string KnowledgeType { get; set; }
    public string Topic { get; set; }
    public string Summary { get; set; }
    public string Content { get; set; }
    public int Importance { get; set; }
    public int AccessCount { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
}
```

#### KnowledgeRelationInfo（关系）
```csharp
public class KnowledgeRelationInfo
{
    public string RelationID { get; set; }
    public string AppID { get; set; }
    public string SourceMemoryID { get; set; }
    public string TargetMemoryID { get; set; }
    public string RelationType { get; set; }
    public float Strength { get; set; }
    public string Metadata { get; set; }
    public DateTime CreateTime { get; set; }
}
```

## 文件结构

```
ZSN.AgentBrook.Web.Manage/
└── Areas/
    └── Manage/
        ├── Controllers/
        │   └── KnowledgeGraphController.cs    # 知识图谱控制器
        └── Views/
            └── KnowledgeGraph/
                ├── Index.cshtml               # 知识节点列表
                ├── Visualize.cshtml           # 知识图谱可视化
                └── Details.cshtml             # 知识详情页面
```

## 扩展功能建议

### 1. 高级搜索
- 按关键词搜索知识内容
- 按重要性范围筛选
- 按时间范围筛选

### 2. 批量操作
- 批量删除知识节点
- 批量导出知识图谱
- 批量修改元数据

### 3. 图谱分析
- 显示最短路径
- 查找孤立节点
- 计算中心度指标
- 社区检测

### 4. 数据导出
- 导出为 JSON 格式
- 导出为 GraphML
- 导出为图片

### 5. 实时更新
- WebSocket 实时推送新增知识
- 动态更新图谱

## 注意事项

1. **性能考虑**
   - 建议图谱深度不超过3层
   - 节点数量建议控制在100以内
   - 大型图谱建议使用分片加载

2. **浏览器兼容性**
   - 推荐使用现代浏览器（Chrome、Firefox、Edge）
   - 需要支持 Canvas 和 ES6

3. **数据安全**
   - 删除操作不可恢复
   - 建议在测试环境先验证
   - 重要数据建议先备份

## 常见问题

### Q1: 图谱显示空白？
**A**: 检查以下几点：
1. 确认知识节点ID有效
2. 检查是否有相关知识
3. 查看浏览器控制台是否有错误

### Q2: 图谱节点重叠？
**A**:
1. 点击"适应屏幕"按钮
2. 尝试切换不同的布局方式
3. 调整图谱深度

### Q3: 如何删除关系？
**A**:
1. 进入知识详情页面
2. 找到要删除的关系
3. 点击"删除关系"按钮

### Q4: 图谱加载慢？
**A**:
1. 减少图谱深度
2. 减少最大节点数
3. 检查网络连接

## 更新日志

### v1.0.0 (2026-04-04)
- ✅ 创建知识节点列表页面
- ✅ 创建知识图谱可视化页面（基于 AntV G6）
- ✅ 创建知识详情页面
- ✅ 实现删除功能（知识节点和关系）
- ✅ 支持多种图谱布局
- ✅ 支持交互式操作（缩放、拖拽、点击）

---

**创建日期**: 2026-04-04
**版本**: v1.0.0
**作者**: Claude Code Assistant
