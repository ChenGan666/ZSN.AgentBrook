# 提示词管理系统

## 概述

本系统实现了统一的提示词管理架构，将所有提示词从代码中分离出来，存放在独立的Markdown文件中，便于维护和版本控制。

## 目录结构

```
md/
├── README.md                           # 本文件
├── WorkFlowAIOptimizePrompts.md        # WorkFlow优化提示词
├── SkillsAIOptimizePrompts.md          # Skills优化提示词
├── ReporterPrompt.md                   # Reporter节点提示词
├── FileToMarkdownPrompt.md             # FileToMarkdown节点提示词
└── ClawAI/
    ├── TaskPlanningPrompt.md           # ClawAI任务规划提示词
    └── ReflectionPrompt.md             # ClawAI反思评估提示词
```

## 配置文件

在 `appsettings.json` 中配置提示词文件路径：

```json
"PromptTemplates": {
  "WorkFlowAIOptimizePrompts": "md/WorkFlowAIOptimizePrompts.md",
  "SkillsAIOptimizePrompts": "md/SkillsAIOptimizePrompts.md",
  "ReporterPrompt": "md/ReporterPrompt.md",
  "FileToMarkdownPrompt": "md/FileToMarkdownPrompt.md",
  "ClawAITaskPlanningPrompt": "md/ClawAI/TaskPlanningPrompt.md",
  "ClawAIReflectionPrompt": "md/ClawAI/ReflectionPrompt.md"
}
```

## 使用方法

### 1. 在Utils类中加载提示词

```csharp
// 从配置文件加载
string prompt = Utils.LoadPromptTemplate("ReporterPrompt");

// 从文件路径加载
string prompt = Utils.LoadPromptTemplateByPath("md/ReporterPrompt.md");
```

### 2. 在节点初始化中使用

**Reporter节点示例：**
```csharp
ReporterData reporterData = new ReporterData();
reporterData.prompt = Utils.LoadPromptTemplate("ReporterPrompt");
if (string.IsNullOrEmpty(reporterData.prompt))
{
    reporterData.prompt = "默认提示词";
}
```

**FileToMarkdown节点示例：**
```csharp
FileToMarkdownData fileToMarkdownData = new FileToMarkdownData();
fileToMarkdownData.prompt = Utils.LoadPromptTemplate("FileToMarkdownPrompt");
if (string.IsNullOrEmpty(fileToMarkdownData.prompt))
{
    fileToMarkdownData.prompt = "默认提示词";
}
```

### 3. 在ClawAI配置中使用

```csharp
// TaskPlanningConfig
var config = new TaskPlanningConfig();
config.InitializeFromFile();  // 从md文件加载提示词

// ReflectionConfig
var reflectionConfig = new ReflectionConfig();
reflectionConfig.InitializeFromFile();  // 从md文件加载提示词
```

### 4. 在WorkflowController中使用

```csharp
string configKey = "WorkFlowAIOptimizePrompts";
string prompt = Utils.LoadPromptTemplate(configKey);
if (string.IsNullOrEmpty(prompt))
{
    // 处理文件不存在的情况
    return Json(JsonMsg<string>.Error(null, ErrorCode.FileNotExist));
}
```

## 提示词文件说明

### ReporterPrompt.md
- **用途**：Reporter节点的系统提示词
- **功能**：将对话内容整理成JSON格式，提取关键点
- **应用场景**：会话记录和总结

### FileToMarkdownPrompt.md
- **用途**：FileToMarkdown节点的系统提示词
- **功能**：将图片内容转写为Markdown文本
- **应用场景**：文档转换和格式化

### TaskPlanningPrompt.md
- **用途**：ClawAI任务规划的提示词模板
- **功能**：指导LLM进行任务规划和步骤分解
- **应用场景**：复杂任务的自动规划

### ReflectionPrompt.md
- **用途**：ClawAI反思评估的提示词模板
- **功能**：指导LLM评估执行质量和决定下一步行动
- **应用场景**：任务执行过程中的质量评估和决策

## 优势

1. **集中管理**：所有提示词在一个目录中，便于查找和维护
2. **版本控制**：提示词文件可以纳入Git版本控制
3. **动态更新**：无需重新编译即可更新提示词
4. **易于维护**：提示词与代码分离，降低维护成本
5. **可读性强**：Markdown格式便于阅读和编辑
6. **灵活配置**：支持多个提示词版本，可通过配置切换

## 错误处理

- 如果文件不存在，`LoadPromptTemplate()` 返回空字符串
- 如果读取文件失败，会记录调试信息并返回空字符串
- 建议在使用时检查返回值，如果为空则使用默认提示词

## 最佳实践

1. **命名规范**：使用清晰的文件名，如 `NodeNamePrompt.md`
2. **文档注释**：在md文件开头添加说明，描述提示词的用途
3. **版本管理**：重要的提示词修改应该记录在Git commit中
4. **测试验证**：修改提示词后应该进行充分的测试
5. **备份保存**：保留旧版本的提示词，便于回滚

## 相关文件

- **配置文件**：`appsettings.json`
- **加载方法**：`ZSN.AI.Node.Utils.cs` (LoadPromptTemplate, LoadPromptTemplateByPath)
- **配置类**：`ZSN.AI.Entity.ClawAI.ClawAIConfig.cs` (TaskPlanningConfig, ReflectionConfig)
- **使用示例**：`ZSN.AI.Node.Utils.cs` (newNode方法中的Reporter和FileToMarkdown节点初始化)
- **控制器**：`ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers.WorkflowController.cs`
