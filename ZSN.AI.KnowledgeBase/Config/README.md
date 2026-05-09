# 配置文件说明

## entity_type_mapping.json

### 📋 用途
实体类型映射配置文件，用于知识图谱中实体类型的标准化和自动学习。

### 🔧 配置项说明

```json
{
  "version": "1.0",                    // 配置版本
  "lastUpdated": "2026-04-10T18:00:00Z", // 最后更新时间（自动维护）
  "autoLearnEnabled": true,            // 是否启用自动学习
  "minFrequencyForAutoAdd": 10,        // 自动添加映射的最小频率阈值
  
  "staticMappings": {                  // 静态映射（手动维护）
    "人物": "PERSON",
    "组织": "ORG",
    ...
  },
  
  "learnedMappings": {                 // 学习到的映射（自动添加）
    "知识库": "KNOWLEDGE_BASE",
    ...
  },
  
  "statistics": {                      // 统计信息（自动维护）
    "知识库": {
      "frequency": 156,
      "firstSeen": "2026-04-01T10:00:00Z",
      "lastSeen": "2026-04-10T17:30:00Z",
      "autoAddedAt": "2026-04-05T14:20:00Z",
      "suggestedMapping": "KNOWLEDGE_BASE"
    }
  }
}
```

### 📝 使用说明

#### 1. 默认配置
- 系统提供了默认配置文件，包含60+种常见实体类型映射
- 首次运行时会自动加载此文件

#### 2. 自动学习
- 当遇到未映射的实体类型时，系统会自动记录统计信息
- 当某个类型出现次数达到阈值（默认10次）时，系统会自动推断并添加映射
- 学习到的映射会保存到 `learnedMappings` 中

#### 3. 手动修改
- 可以直接编辑 `staticMappings` 添加新的映射
- 修改后重启服务即可生效
- 建议将高质量的学习映射迁移到静态映射中

#### 4. 配置参数调整

**启用/禁用自动学习**
```json
"autoLearnEnabled": false  // 禁用自动学习
```

**调整学习阈值**
```json
"minFrequencyForAutoAdd": 5  // 降低阈值，更快学习
```

### 🎯 标准实体类型

| 类型 | 说明 | 示例 |
|------|------|------|
| PERSON | 人物 | 张三、李四、专家 |
| ORG | 组织机构 | 公司、团队、学校 |
| LOC | 地点位置 | 北京、上海、办公室 |
| PRODUCT | 产品 | 软件、系统、平台 |
| TECH | 技术 | 算法、协议、架构 |
| LANGUAGE | 编程语言 | Python、Java、C# |
| DATABASE | 数据库 | MySQL、PostgreSQL |
| MODEL | 模型 | GPT-4、BERT |
| VERSION | 版本 | v1.0、2.0 |
| DATE | 日期时间 | 2026-04-10 |
| NUMBER | 数值 | 100、1000 |
| MONEY | 金额 | 100元、$50 |
| FEATURE | 功能特性 | 搜索、推荐 |
| CONCEPT | 概念术语 | 知识图谱、向量 |
| MODULE | 模块 | 用户模块、支付模块 |
| COMPONENT | 组件 | 按钮、表单 |
| INTERFACE | 接口 | API、REST |
| SERVICE | 服务 | 认证服务、缓存服务 |

### 🔄 配置更新流程

```
1. 手动修改配置文件
   ↓
2. 重启服务
   ↓
3. 系统加载新配置
   ↓
4. 运行过程中自动学习
   ↓
5. 定期保存（每5分钟）
   ↓
6. 配置文件自动更新
```

### ⚠️ 注意事项

1. **不要删除 `learnedMappings` 和 `statistics`**
   - 这些是系统自动维护的，删除会丢失学习成果

2. **修改 `staticMappings` 时注意格式**
   - 键：原始类型（小写，中文或英文）
   - 值：标准类型（大写英文）

3. **备份配置文件**
   - 建议定期备份此文件
   - 可以纳入版本控制

4. **配置冲突处理**
   - 如果同一个类型在 `staticMappings` 和 `learnedMappings` 中都存在
   - 优先使用 `staticMappings` 中的映射

### 📊 监控和优化

**查看学习成果**
- 检查 `learnedMappings` 中的映射是否合理
- 将高质量映射迁移到 `staticMappings`

**分析统计信息**
- 查看 `statistics` 中的高频未映射类型
- 决定是否需要添加新的静态映射

**调整学习参数**
- 如果学习过于激进，提高 `minFrequencyForAutoAdd`
- 如果学习过于保守，降低 `minFrequencyForAutoAdd`

### 🛠️ 故障排除

**配置文件损坏**
- 系统会自动创建默认配置
- 检查日志中的错误信息

**映射不生效**
- 确认配置文件格式正确（JSON格式）
- 检查是否重启了服务
- 查看日志确认配置是否加载成功

**自动学习不工作**
- 检查 `autoLearnEnabled` 是否为 `true`
- 确认类型出现次数是否达到阈值
- 查看日志中的学习信息
