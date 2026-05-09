# ZSN.AgentBrook.Plugins — 项目说明

> 路径：`w:\AI\ZSN.Knowbase\ZSN.Knowbase.Core\ZSN.AgentBrook.Plugins`

## 项目概览

- **定位**：面向 Agent/工作流的功能插件库。
- **当前实现**：`KnowledgeBasePlugin` 提供将文本/长文本分段写入知识库的能力，可被大模型/工作流节点/外部服务调用。
- **集成方式**：与解决方案其他项目（如 `ZSN.AgentBrook.AutoJob`、`ZSN.AgentBrook.API`）通过依赖注入与函数服务协作。

## 技术栈与依赖

- **框架**：.NET 8（`net8.0`）
- **项目引用**（见 `ZSN.AgentBrook.Plugins.csproj`）：
  - `ZSN.AI.Core`
  - `ZSN.AI.Entity`
  - `ZSN.Utils.Core`

## 目录结构

- **`Functions/KnowledgeBasePlugin.cs`**
  - 命名空间：`ZSN.AgentBrook.Plugins.Functions`
  - 类：`KnowledgeBasePlugin`
  - 标注：`[Description("知识库能力插件")]`，方法使用 `[KernelFunction]` 可被 Semantic Kernel 工具/函数调用。

## KnowledgeBasePlugin 说明

- **依赖注入**
  - `ILogger<KnowledgeBasePlugin>`
  - `IConfiguration`
  - `IImportKMSService`

- **核心方法**
  - `Task<object> Save(string knowledgeBaseId, string text, string? fileName, bool isQAValue = false, string delimiter = "")`
    - 标注：`[KernelFunction]`、`[Description("ZSN.AI.Plugins:将数据保存到知识库")]`、返回标注 `[return: Description("数据ID")]`
    - 入参：
      - `knowledgeBaseId`：目标知识库 ID（必填）
      - `text`：要写入的文本内容
      - `fileName`：原始文件名（可空；长文本拆分保存时用于生成片段文件名）
      - `isQAValue`：是否以 QA 语料方式入库
      - `delimiter`：分隔符；提供时按分隔符拆分 `text`，逐段入库
    - 行为：
      - 构造 `ImportKMSTaskReq`，默认 `ImportType=Text`，附带 `KnowledgeBaseFileInfo`。
      - 若提供 `delimiter`，按分隔符拆分；否则整体作为一个条目保存。
      - 对每段文本：
        - 长度 > 1000 时，自动落盘为临时 `.txt` 文件并改为 `ImportType=File`。
          - 目录：`Path.GetTempPath()/[FileConversion:TempDirectory]/TextMDOutputs/text_file/`（默认 TempDirectory=`ZSN.Knowbase.FileConversions`）。
        - 填充 `KnowledgeBaseFileInfo` 基本信息。
        - 调用 `_importKMSService.ImportKMSTask(request)` 完成入库。
        - `KnowledgeBaseFileInfoBussiness.Add(...)` 落库文件记录。
      - 返回：`{ Success, Message, Data: { KmsId, ImportType, DataCount } }`

- **返回示例**
```json
{
  "Success": true,
  "Message": "内容已成功保存到知识库",
  "Data": {
    "KmsId": "your_kms_id",
    "ImportType": "Text",
    "DataCount": 1
  }
}
```

## 配置项（由宿主提供）

- `FileConversion:TempDirectory`：长文本自动落盘时的临时目录名，默认 `ZSN.Knowbase.FileConversions`。
- 其它知识库导入相关配置（数据库、服务地址等）由 `IImportKMSService` 的实现与 BLL 层处理。

## 宿主集成与调用

- 宿主项目需在 DI 中注册：`IImportKMSService`、`ILogger<>`、`IConfiguration`。
- 典型加载方式：
  - 通过 `services.AddServicesFromAssemblies("ZSN.AgentBrook.Plugins")` 扫描注册程序集类型。
  - 若使用 Semantic Kernel，可通过 `FunctionService` 或 SK 插件机制暴露 `[KernelFunction]` 方法给 Agent/工作流。

## 与其他模块协作

- 与 `ZSN.AgentBrook.AutoJob` 的 `FileChunkJob`/知识库导入链路共用 `IImportKMSService` 与 `KnowledgeBaseFileInfoBussiness`。
- 可与 API 项目联动回调（如转换结果经 `TaskController.ReCall` 回流）。

## 使用建议

- 长文本建议配合 `delimiter` 分段导入，减少单次处理耗时与内存压力。
- `isQAValue` 可作为知识问答语料导入的标记，便于后续检索/召回策略差异化处理。
