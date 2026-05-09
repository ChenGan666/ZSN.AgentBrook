using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Tsavorite.core;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Entity.Model;
namespace ZSN.AI.Entity
{
    public enum LoopType
    {
        NOLoop = 0,
        Second = 1,
        Day = 2,
        Week = 3,
        Month = 4
    }

    public enum TaskState
    {
        Waiting = 0,
        Processing = 1,
        Completed = 2,
        Failure = -1
    }

    public enum ReCallDataType
    {
        None = 0,
        Markdown = 1
    }

    /// <summary>
    /// tb_task_info
    /// </summary>
    public partial class TaskInfo
    {
        public TaskInfo() { }
        #region AutoField
        /// <summary>
        /// TaskID
        /// </summary>
        public string TaskID { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// TaskType
        /// </summary>
        public NodeType TaskType { get; set; }
        /// <summary>
        /// TaskConfig
        /// </summary>
        public TaskConfig TaskConfig { get; set; } = new TaskConfig();
        /// <summary>
        /// CreateTime
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// UpdateTime
        /// </summary>
		public DateTime UpdateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// State
        /// </summary>
        public TaskState State { get; set; } = TaskState.Waiting;
        /// <summary>
        /// Results
        /// </summary>
        public Results Results { get; set; } = new Results();

        public LoopType LoopType { get; set; } = 0;
        public IntervalValue IntervalValue { get; set; } = new IntervalValue();
        /// <summary>
        /// 首次开始时间(用于定时触发的时间点)
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;
        public int RepeatValue { get; set; } = 0;
        public int RedoCount { get; set; } = 0;

        public string FromTaskID { get; set; } = string.Empty;
        public string FromMainTaskID {  get; set; } = string.Empty;
        public string WorkflowID { get; set; } = string.Empty;
        public string SessionID { get; set; } = string.Empty;
        public string ProcessesID { get; set; } = string.Empty;

        #endregion
    }

    public partial class Results
    {
        public Results() { }
        public object Data { get; set; }
    }
    public partial class ChatSummaryData
    {
        public ChatSummaryData() { }
        public string SummaryID { get; set; }
    }
    public partial class TaskData
    {
        public TaskData() { }
        public string AppID { get; set; }
        public string TaskID { get; set; }
        public string SessionID { get; set; }

        public string MemberID { get; set; }
        public string ProcessesID {  get; set; }
        /// <summary>
        /// 在APP工作流中定义的Agent节点的NodeID
        /// </summary>
        public string AgentNodeID { get; set; }
        public List<Inputs> Inputs { get; set; } = new List<Inputs>();

        public List<AttachmentItem> AttachmentItems { get; set; } = new List<AttachmentItem>();
        public dynamic AdditionalOptions { get; set; } = null;

        public string FromMainTaskID { get; set; } = string.Empty;
    }
    public partial class TaskConfig
    {
        public TaskConfig() { }
        public NodeConfig NodeConfig { get; set; } = new NodeConfig();

        public object? NotNodeConfig {  get; set; }

        public T NodeData<T>(T defaultValue = default(T))
        {

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(this.NodeConfig.data));
        }
        public TaskData Data { get; set; } = new TaskData();
    }
    public partial class IntervalValue
    { 
        public IntervalValue() { }

        public List<int> Value { get; set; }
    }

    public partial class FileChunkConfig
    {
        public FileChunkConfig() { }
        public string KnowledgeBaseID { get; set; }
        public string FileID { get; set; }

        public ImportKMSTaskReq ImportKMSTask { get; set; }
    }

    public partial class AgentNode_return
    {
        public List<string> AgentNodeID { get; set; }
    }

    public partial class ToMarkdownFile
    { 
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileCode { get; set; }
    }

    public partial class MarkdownConfig
    {
        public MarkdownConfig() { }
        public List<ToMarkdownFile> sourceFile { get; set; }
        public string reCallUrl { get; set; }
        public string prompt { get; set; } = string.Empty;
        public ReCallDataType reCallDataType { get; set; } = ReCallDataType.None;
    }

    public partial class HumanOperationConfig
    {
        public HumanOperationConfig() { }

        public string reCallUrl { get; set; }

    }

    /// <summary>
    /// 文件导入知识库配置
    /// </summary>
    public partial class FileToKnowledgeBaseConfig
    {
        public FileToKnowledgeBaseConfig() { }

        /// <summary>
        /// 知识库ID
        /// </summary>
        public string KnowledgeBaseId { get; set; }

        /// <summary>
        /// 待处理的文件地址
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 目标文件目录（处理后的文件存储位置，可选）
        /// </summary>
        public string TargetDirectory { get; set; }

        /// <summary>
        /// 大模型ID（用于实体提取和关系分析）
        /// </summary>
        public string LargeModelId { get; set; }

        /// <summary>
        /// 向量大模型ID（用于文本向量化）
        /// </summary>
        public string EmbeddingModelId { get; set; }

        /// <summary>
        /// 分块策略（Semantic, Fixed, Recursive）
        /// </summary>
        public string ChunkStrategy { get; set; } = "Semantic";

        /// <summary>
        /// 最大分块大小（字符数）
        /// </summary>
        public int MaxChunkSize { get; set; } = 1000;

        /// <summary>
        /// 分块重叠大小（字符数）
        /// </summary>
        public int ChunkOverlap { get; set; } = 200;

        /// <summary>
        /// 是否启用实体提取
        /// </summary>
        public bool EnableEntityExtraction { get; set; } = true;

        /// <summary>
        /// 是否启用关系提取
        /// </summary>
        public bool EnableRelationExtraction { get; set; } = true;

        /// <summary>
        /// URL地址（如果是从URL导入）
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 文本内容（如果是直接导入文本）
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 文件ID（如果文件已经在系统中）
        /// </summary>
        public string FileId { get; set; }
    }

    /// <summary>
    /// 文件导入知识库处理结果
    /// </summary>
    public partial class FileToKnowledgeBaseResult
    {
        public FileToKnowledgeBaseResult() { }

        /// <summary>
        /// 文档ID
        /// </summary>
        public string DocumentId { get; set; }

        /// <summary>
        /// 分块数量
        /// </summary>
        public int ChunkCount { get; set; }

        /// <summary>
        /// 实体数量
        /// </summary>
        public int EntityCount { get; set; }

        /// <summary>
        /// 关系数量
        /// </summary>
        public int RelationCount { get; set; }

        /// <summary>
        /// 消耗的Token数
        /// </summary>
        public long TokensConsumed { get; set; }

        /// <summary>
        /// 处理耗时（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// 知识图谱节点数量
        /// </summary>
        public int GraphNodeCount { get; set; }

        /// <summary>
        /// 知识图谱边数量
        /// </summary>
        public int GraphEdgeCount { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 详细信息（可选）
        /// </summary>
        public string Details { get; set; }
    }
}
