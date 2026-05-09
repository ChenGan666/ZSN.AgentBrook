using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.Json.Nodes;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Entity.KnowledgeBase;
using static Org.BouncyCastle.Math.Primes;
namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_workflow_node_info
    /// </summary>
    public partial class WorkflowNodeInfo
    {
        public WorkflowNodeInfo() { }
        #region AutoField
        /// <summary>
        /// NodeID
        /// </summary>
        public string NodeID { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// WorkflowID
        /// </summary>
        public string WorkflowID { get; set; } = string.Empty;
        /// <summary>
        /// NodeType
        /// </summary>
        public NodeType NodeType { get; set; }
        /// <summary>
        /// NodeName
        /// </summary>
        public string NodeName { get; set; } = string.Empty;
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Config
        /// </summary>
        public object? Config { get; set; }

        /// <summary>
        /// CreateTime
        /// </summary>
        public DateTime? CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// LastUpdateTime
        /// </summary>
        public DateTime? LastUpdateTime { get; set; } = DateTime.Now;
        #endregion
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeType
    {
        Start = 1,
        End = 2,
        LargeModel = 3,
        MainAI = 4,
        KnowledgeBase = 5,
        Selector = 6,
        Reporter = 7,
        TimeTrigger = 8,
        Agent = 9,
        Plugins = 10,
        AgentStart = 11,
        AgentEnd = 12,
        Merge = 13,
        MCP = 14,
        Notepaper = 15,
        FileToMarkdown = 16,
        HumanInTheLoop = 17,
        IntentionRecognition = 18,
        HumanInTheLoopInput = 19,
        SkillAgent = 20,
        ImageGeneration = 21,
        VideoGeneration = 22,
        ClawAI = 23,
        ClawAIWorkflowStep = 24,  // ClawAI 异步等待中的子 WorkFlow 步骤
        ServiceDesk = 25,

        NotNode_FileChunk = 90,
        NotNode_Markdown = 91,
        NotNode_HumanOperation = 92,
        NotNode_FileToKnowledgeBase = 93
    }
    public partial class NodeConfig
    {
        public NodeConfig() { }
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string name { get; set; }
        public string mainid { get; set; } = string.Empty;
        public string workflowid { get; set; } = string.Empty;
        public NodeType type { get; set; }
        public object data { get; set; }
        public Position position { get; set; } = new Position();
        //上一节点类型
        public NodeType fromNodeType { get;set; }
        public string fromNodeId { get; set; } = string.Empty;
    }

    public partial class Position
    {
        public Position() { }
        public decimal x { get; set; } = 0;
        public decimal y { get; set; } = 80;
    }

    public partial class Inputs
    {
        public Inputs() { }
        public string id { get; set;}
        public string sourceId { get; set; }
        public string varname { get; set; } = "input";
        public string value { get; set; } = string.Empty;
        public string type { get; set; } = "string";
        public string txt { get; set; } = "";
        // 插件函数参数定义字段
        public string paramName { get; set; } = string.Empty;
        public string paramType { get; set; } = string.Empty;
        public string defaultValue { get; set; } = string.Empty;
    }

    public partial class Output
    {
        public Output() { }
        public string id { get; set; }
        public string nodeId { get; set; }
        public string sourceId { get; set; }
        public string varname { get; set; } = "output";
        public string value { get; set; } = string.Empty;
        public string type { get; set; } = "string";
        public string txt { get; set; } = "";
        public string displayText { get; set;} = string.Empty;
        public string originalSourceId { get; set; } = string.Empty;
        public string originalNodeId { get; set; } = string.Empty;
    }

    public partial class Option
    {
        public Option() { }
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string name { get; set; }
        public string value { get; set; }
    }
    public partial class InputOption
    {
        public InputOption() { }
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string name { get; set; }
        public string value { get; set; }
        public string valueType { get; set; } = "string";
        /// <summary>
        /// 是否必填
        /// </summary>
        public bool isRequired { get; set; } = false;
    }

    public partial class Selector
    {
        public Selector() { }
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string varname { get; set; } = "input";
        public string comparison { get; set; }
        public string value { get; set; } = "";
        public int top { get; set; }
    }

    public partial class Intention
    {
        public Intention() { }
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string reognitionRules { get; set; } = string.Empty;
    }

    public partial class IntervalConfig
    {
        public IntervalConfig() { }
        /// <summary>
        /// 周期类型
        /// 每隔n秒=s
        /// 每隔n天=d
        /// 每周星期几=w
        /// 每月第几日=m
        /// </summary>
        public string LoopType { get; set; }
        /// <summary>
        /// 执行周期
        /// </summary>
        public int Interval { get; set; } = 3600;
        /// <summary>
        /// 首次执行开始时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 执行次数，0=无限
        /// </summary>
        public int Repeat { get; set; } = 0;
        /// <summary>
        /// 最后一次执行时间
        /// </summary>
        public DateTime LastRunTime { get; set; } = DateTime.Now;
    }

    public partial class Skill
    {
        public Skill() { }
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SkillID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Skill主目录
        /// </summary>
        public string SkillDirectory { get; set; } = string.Empty;

    }
    public class SkillsToolsOptions
    {
        public List<string> AllowedScriptExtensions { get; set; } = [".py", ".ps1", ".sh", ".cs"];
        public int ScriptTimeoutSeconds { get; set; } = 30;
        public int MaxOutputSizeBytes { get; set; } = 50 * 1024; // 50KB

        public List<string> AllowedCommands { get; set; } = ["git", "npm", "dotnet", "powershell"];
    }

    public partial class TimeTrigger: IntervalConfig
    {
        public TimeTrigger() { }
        public string id { get; set; } = Guid.NewGuid().ToString();
        public int top { get; set; }
    }

    public partial class NodeData
    {
        public NodeData() { }
        public string label { get; set; } = string.Empty;
        public List<Inputs> inputs { get; set; } = new List<Inputs>();
        public List<Output> output { get; set; } = new List<Output>();
        public string prompt { get; set; } = string.Empty;
    }

    public partial class StartData : NodeData
    {
        public StartData()
        {
            label = "Start";
        }
    }

    public partial class AgentStartData : StartData
    {
        public AgentStartData() { }
    }

    public partial class EndData
    {
        public EndData() { }
        public string label { get; set; } = "End";
        public List<Inputs> inputs { get; set; } = new List<Inputs>();
        public string prompt { get; set; } = string.Empty;
    }
    public partial class AgentEndData : EndData
    {
        public AgentEndData() { }
        public List<Output> output { get; set; } = new List<Output>();
    }

    public partial class LargeModelData: NodeData
    {
        public LargeModelData() { }
        public string label { get; set; } = "Large Model";
        public LargeModelInfo model { get; set; } = new LargeModelInfo();
        public List<Inputs> inputs { get; set; } = new List<Inputs>();
        public string prompt { get; set; } = string.Empty;
        public int temperature { get; set; } = 30;
        public int topp { get; set; } = 80;
        public List<Output> output { get; set; } = new List<Output>();

        public List<PluginsInfo> SemanticFunction { get; set; } = new List<PluginsInfo>();
        public List<PluginsInfo> NativeFunction { get; set; } = new List<PluginsInfo>();

        /// <summary>
        /// 思考模式
        /// </summary>
        public bool Thinking { get; set; } = false;

        /// <summary>
        /// 输出类型json_object，text
        /// </summary>
        public string ResponseFormat { get; set; } = "text";

        /// <summary>
        /// 具备图片读取能力
        /// </summary>
        public bool CanReadPic { get; set; } = false;
        /// <summary>
        /// 具备文档读取能力
        /// </summary>
        public bool CanReadDoc { get; set; } = false;
    }

    public partial class MainAIData : LargeModelData
    {

        public MainAIData() { }
        public string label { get; set; } = "Main AI";
    }

    public partial class ReporterData : LargeModelData
    {
        public ReporterData() { }
        public string label { get; set; } = "Reporter";

        /// <summary>
        /// 一次摘要记录条数
        /// </summary>
        public int recordslength { get; set; } = 10;
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool enable { get; set; } = true;
    }

    public partial class KnowledgeBaseData: LargeModelData
    {
        public KnowledgeBaseData() { }
        public string label { get; set; } = "Knowledge Base";

        public HybridSearchOptions searchOptions = new HybridSearchOptions();
        public List<KnowledgeBaseInfo> knowledgeBase { get; set; } = new List<KnowledgeBaseInfo>();
        public List<Inputs> inputs { get; set; } = new List<Inputs>();
        public int relevance { get; set; } = 70;
        public List<Output> output { get; set; } = new List<Output>();
    }

    
    public partial class PluginsData
    {
        public PluginsData() { }
        public string label { get; set; } = "Plugins";
        public PluginsInfo plugins { get; set; } = new PluginsInfo();
        public List<Inputs> inputs { get; set; } = new List<Inputs>();
        public List<Output> output { get; set; } = new List<Output>();
    }

    public partial class SelectorData
    {
        public SelectorData() { }
        public string label { get; set; } = "Selector";
        public List<Inputs> inputs { get; set; } = new List<Inputs>();
        public List<Output> output { get; set; } = new List<Output>();
        public List<Selector> selector { get; set; } = new List<Selector>();
    }

    public partial class TimeTriggerData
    {
        public TimeTriggerData() { }
        public string label { get; set; } = "TimeTrigger";
        public string prompt { get; set; } = string.Empty;
        public bool enable { get; set; } = true;
        public TimeTrigger timeTrigger { get; set; } = new TimeTrigger();
        public List<Output> output { get; set; } = new List<Output>();
    }

    public partial class AgentData
    {
        public AgentData() { }
        public string label { get; set; } = "Agent";

        public AgentInfo agent { get; set; } = new AgentInfo();

        public List<Inputs> inputs { get; set; } = new List<Inputs>();
        public List<Output> output { get; set; } = new List<Output>();
    }

    public partial class MergeData
    { 
        public MergeData() { }
        public string label { get; set; } = "Merge";
        public bool allowFailure { get; set; } = true;
        public List<Inputs> inputs { get; set; } = new List<Inputs>();
        public List<Output> output { get; set; } = new List<Output>();
    }

    public partial class MCPData: LargeModelData
    {
        public MCPData() { }
        public string label { get; set; } = "MCP";
        public McpInfo mcp { get; set; } = new McpInfo();
        public List<Inputs> inputs { get; set; } = new List<Inputs>();
        public string config { get; set; } = string.Empty;
        public List<Output> output { get; set; } = new List<Output>();
    }
    public partial class NotepaperData
    {
        public NotepaperData() { }
        public string label { get; set; } = "Notepaper";
        public string content { get; set; } = string.Empty;
        public string color { get; set; } = string.Empty;
        public decimal width  { get; set; } = 0;
        public decimal height { get; set; } = 0;
    }

    public partial class FileToMarkdownData: NodeData
    {
        public FileToMarkdownData() { 
            label = "File to Markdown";
        }
    }


    public partial class HumanInTheLoopData : NodeData
    {
        public HumanInTheLoopData()
        {
            label = "Human In The Loop";
        }
        /// <summary>
        /// 向用户提问内容
        /// </summary>
        public string askContent { get; set; } = string.Empty;
        public string optionMode { get; set; } = "fixed";//dynamic
        public string dynamicOptionsVar { get; set; } = string.Empty;
        /// <summary>
        /// 可选项列表
        /// </summary>
        public List<Option> options { get; set; } = new List<Option>();
    }
    public partial class HumanInTheLoopInputData : LargeModelData
    {
        public HumanInTheLoopInputData()
        {
            label = "Human In The Loop Input";
        }
        /// <summary>
        /// 向用户提问内容
        /// </summary>
        public string askContent { get; set; } = string.Empty;
        public string optionMode { get; set; } = "fixed";//dynamic
        public string dynamicOptionsVar { get; set; } = string.Empty;
        /// <summary>
        /// 可选项列表
        /// </summary>
        public List<InputOption> inputOptions { get; set; } = new List<InputOption>();
        /// <summary>
        /// 当为用户提交输入时，开始执行解析用户输入内容的开关
        /// </summary>
        public bool toExecSwitch { get; set; } = false;
        /// <summary>
        /// 用户输入的对话内容
        /// </summary>
        public List<string> userInputContent { get; set; } = new List<string>();
    }
    public class HumanInputParseResult
    {
        public List<InputOption> options { get; set; } = new List<InputOption>();
        public List<string> missing { get; set; } = new List<string>();
        public string ask { get; set; } = string.Empty;
        public bool valid { get; set; } = false;
    }

    public partial class IntentionRecognitionData : LargeModelData
    {
        public IntentionRecognitionData()
        {
            label = "Intention Recognition";
            
        }
        public List<Intention> intentions { get; set; } = new List<Intention>();
    }

    public partial class SkillAgentData : LargeModelData {
        public SkillAgentData() {
            label = "Skill Agent";
        }

        public Skill skill { get; set; } = new Skill();

        public SkillsToolsOptions skillsToolsOptions { get; set; } =new SkillsToolsOptions();
    }

    /// <summary>
    /// 图像生成节点数据
    /// </summary>
    public partial class ImageGenerationData : NodeData
    {
        public ImageGenerationData()
        {
            label = "Image Generation";
        }

        /// <summary>
        /// 图像模型信息
        /// </summary>
        public LargeModelInfo model { get; set; } = new LargeModelInfo();

        /// <summary>
        /// 图像生成提示词
        /// </summary>
        public string prompt { get; set; } = string.Empty;

        /// <summary>
        /// 输入图像（用于图生图，可选）
        /// 可以是变量引用，如 {{previousNode_imageUrl}}
        /// </summary>
        public string imageInput { get; set; } = string.Empty;

        /// <summary>
        /// 图像宽度
        /// </summary>
        public int width { get; set; } = 1024;

        /// <summary>
        /// 图像高度
        /// </summary>
        public int height { get; set; } = 1024;

        /// <summary>
        /// 图像质量（OpenAI DALL-E 3）
        /// standard 或 hd
        /// </summary>
        public string quality { get; set; } = "standard";

        /// <summary>
        /// 图像风格（OpenAI DALL-E 3）
        /// vivid 或 natural
        /// </summary>
        public string style { get; set; } = "vivid";

        /// <summary>
        /// 输入参数
        /// </summary>
        public List<Inputs> inputs { get; set; } = new List<Inputs>();

        /// <summary>
        /// 输出参数
        /// </summary>
        public List<Output> output { get; set; } = new List<Output>();
    }

    /// <summary>
    /// 视频生成节点数据
    /// </summary>
    public partial class VideoGenerationData : NodeData
    {
        public VideoGenerationData()
        {
            label = "Video Generation";
        }

        /// <summary>
        /// 视频模型信息
        /// </summary>
        public LargeModelInfo model { get; set; } = new LargeModelInfo();

        /// <summary>
        /// 视频生成提示词
        /// </summary>
        public string prompt { get; set; } = string.Empty;

        /// <summary>
        /// 负面提示词（可选）
        /// </summary>
        public string negativePrompt { get; set; } = string.Empty;

        /// <summary>
        /// 视频生成类型
        /// TextToVideo=1, ImageToVideo=2, ReferenceToVideo=3, StartEndToVideo=4
        /// </summary>
        public int generationType { get; set; } = 1;

        /// <summary>
        /// 输入图像（用于图生视频，可选）
        /// 可以是URL或Base64格式，也可以是变量引用，如 {{previousNode_imageUrl}}
        /// </summary>
        public string imageInput { get; set; } = string.Empty;

        /// <summary>
        /// 参考图片列表（用于参考图生成视频，1-7张）
        /// </summary>
        public List<string> referenceImages { get; set; } = new List<string>();

        /// <summary>
        /// 首帧图片URL或Base64（用于首尾帧生成视频）
        /// </summary>
        public string firstFrameUrl { get; set; } = string.Empty;

        /// <summary>
        /// 尾帧图片URL或Base64（用于首尾帧生成视频）
        /// </summary>
        public string lastFrameUrl { get; set; } = string.Empty;

        /// <summary>
        /// 视频时长（秒）
        /// 不同模型支持不同时长，如Sora支持4/8/12秒，Vidu支持5秒等
        /// </summary>
        public int duration { get; set; } = 5;

        /// <summary>
        /// 视频尺寸（如 "720x1280", "1280x720"）
        /// </summary>
        public string size { get; set; } = "720x1280";

        /// <summary>
        /// 宽高比（如 "16:9", "9:16", "1:1"）
        /// </summary>
        public string aspectRatio { get; set; } = "9:16";

        /// <summary>
        /// 分辨率（如 "720p", "1080p", "1080P"）
        /// </summary>
        public string resolution { get; set; } = "720p";

        /// <summary>
        /// 随机种子（用于可重现的生成）
        /// </summary>
        public int seed { get; set; } = 0;

        /// <summary>
        /// 运动幅度（Vidu模型）
        /// auto, small, medium, large
        /// </summary>
        public string movementAmplitude { get; set; } = "auto";

        /// <summary>
        /// 是否添加背景音乐（Vidu模型）
        /// </summary>
        public bool bgm { get; set; } = false;

        /// <summary>
        /// 是否启用提示词优化（Hailuo模型）
        /// </summary>
        public bool promptOptimizer { get; set; } = true;

        /// <summary>
        /// 输入参数
        /// </summary>
        public List<Inputs> inputs { get; set; } = new List<Inputs>();

        /// <summary>
        /// 输出参数
        /// </summary>
        public List<Output> output { get; set; } = new List<Output>();
    }

    /// <summary>
    /// Claw AI 节点数据配置
    /// </summary>
    public partial class ClawAIData : LargeModelData
    {
        public ClawAIData()
        {
            label = "Claw AI";
        }

        /// <summary>
        /// 输入参数
        /// </summary>
        public new List<Inputs> inputs { get; set; } = new List<Inputs>();

        /// <summary>
        /// 输出到下游节点
        /// </summary>
        public List<Output> outputToNext { get; set; } = new List<Output>();

        /// <summary>
        /// 输出到 Agent 循环
        /// </summary>
        public List<Output> outputToAgent { get; set; } = new List<Output>();

        // ============ 多模型配置 ============
        
        /// <summary>
        /// 主 AI 模型 - 用于处理用户请求和最终响应生成
        /// 继承自 LargeModelData.model
        /// </summary>
        // public LargeModelInfo model { get; set; } // 已在基类中定义

        /// <summary>
        /// 任务规划模型 - 用于分析任务并生成执行计划
        /// 如果为 null,则使用主模型
        /// </summary>
        public LargeModelInfo planningModel { get; set; } = null;

        /// <summary>
        /// 反思评估模型 - 用于评估执行质量和决定下一步行动
        /// 如果为 null,则使用主模型
        /// </summary>
        public LargeModelInfo reflectionModel { get; set; } = null;

        /// <summary>
        /// 记忆处理模型 - 用于记忆压缩、摘要、检索等
        /// 如果为 null,则使用主模型
        /// </summary>
        public LargeModelInfo memoryModel { get; set; } = null;

        /// <summary>
        /// 用户画像模型 - 用于分析用户偏好和交互模式
        /// 如果为 null,则使用记忆模型或主模型
        /// </summary>
        public LargeModelInfo profileModel { get; set; } = null;

        /// <summary>
        /// AI 个性模型 - 用于生成个性化响应和情绪模拟
        /// 如果为 null,则使用主模型
        /// </summary>
        public LargeModelInfo personalityModel { get; set; } = null;

        /// <summary>
        /// 向量模型 - 用于生成文本向量嵌入
        /// </summary>
        public LargeModelInfo embeddingModel { get; set; } = null;

        /// <summary>
        /// 任务规划配置
        /// </summary>
        public TaskPlanningConfig taskPlanningConfig { get; set; } = new TaskPlanningConfig();

        /// <summary>
        /// 智能主控配置（使用主模型进行判断）
        /// </summary>
        public MasterControlConfig masterControlConfig { get; set; } = new MasterControlConfig();

        /// <summary>
        /// WorkFlow 循环配置
        /// </summary>
        public WorkFlowLoopConfig workFlowLoopConfig { get; set; } = new WorkFlowLoopConfig();

        /// <summary>
        /// 反思配置
        /// </summary>
        public ReflectionConfig reflectionConfig { get; set; } = new ReflectionConfig();

        /// <summary>
        /// 记忆配置
        /// </summary>
        public MemoryConfig memoryConfig { get; set; } = new MemoryConfig();

        /// <summary>
        /// 用户画像配置
        /// </summary>
        public UserProfileConfig userProfileConfig { get; set; } = new UserProfileConfig();

        /// <summary>
        /// AI 个性配置
        /// </summary>
        public PersonalityConfig personalityConfig { get; set; } = new PersonalityConfig();

        /// <summary>
        /// 预设的 WorkFlow 配置列表
        /// </summary>
        public List<WorkflowConfigInfo> workflowConfigs { get; set; } = new List<WorkflowConfigInfo>();
    }

    /// <summary>
    /// ServiceDesk 节点配置 — 面向客服场景的快速响应节点
    /// </summary>
    public partial class ServiceDeskData : LargeModelData
    {
        public ServiceDeskData()
        {
            label = "ServiceDesk";
        }

        public new List<Inputs> inputs { get; set; } = new List<Inputs>();
        public new List<Output> output { get; set; } = new List<Output>();


        /// <summary>绑定的知识库列表</summary>
        public List<KnowledgeBaseInfo> knowledgeBase { get; set; } = new List<KnowledgeBaseInfo>();

        /// <summary>每个知识库返回的 Top-K 结果数</summary>
        public int TopK { get; set; } = 5;

        /// <summary>融合后的最终结果数</summary>
        public int FusedResultTopN { get; set; } = 5;

        /// <summary>相似度阈值</summary>
        public float SimilarityThreshold { get; set; } = 0.3f;

        /// <summary>向量检索权重</summary>
        public float VectorSearchWeight { get; set; } = 0.7f;

        /// <summary>全文检索权重</summary>
        public float FullTextSearchWeight { get; set; } = 0.3f;

        /// <summary>最低检索分数阈值</summary>
        public float MinRetrievalScore { get; set; } = 0.3f;

        /// <summary>最大上下文分块数（RAG Prompt 中使用的检索结果数）</summary>
        public int? MaxContextChunks { get; set; }


        /// <summary>高置信度阈值（直接回复）</summary>
        public float HighConfidenceThreshold { get; set; } = 0.85f;

        /// <summary>中置信度阈值（RAG 增强）</summary>
        public float MediumConfidenceThreshold { get; set; } = 0.5f;

        /// <summary>低置信度阈值（兜底/升级）</summary>
        public float LowConfidenceThreshold { get; set; } = 0.3f;

        /// <summary>置信度计算权重配置</summary>
        public ConfidenceWeightConfig ConfidenceWeights { get; set; } = new ConfidenceWeightConfig();


        /// <summary>人设 Prompt（覆盖默认）</summary>
        public string PersonaPrompt { get; set; }


        /// <summary>是否显示知识来源标注</summary>
        public bool ShowSourceCitation { get; set; } = true;


        /// <summary>问候语模式列表</summary>
        public List<string> GreetingPatterns { get; set; } = new List<string>
        {
            "你好", "您好", "嗨", "hello", "hi", "在吗"
        };

        /// <summary>简单对话模式列表（感谢/告别等）</summary>
        public List<string> SimpleConversationPatterns { get; set; } = new List<string>
        {
            "谢谢", "感谢", "再见", "拜拜", "好的", "知道了", "嗯"
        };


        /// <summary>意图规则列表（可配置的业务意图）</summary>
        public List<IntentRule> IntentRules { get; set; } = new List<IntentRule>();


        /// <summary>是否允许升级到 ClawAI</summary>
        public bool EnableEscalation { get; set; } = false;

        /// <summary>升级目标 ClawAI 节点 ID</summary>
        public string EscalationNodeID { get; set; }

        /// <summary>兜底话术</summary>
        public string FallbackMessage { get; set; } = "抱歉，我暂时无法回答您的问题，请稍后再试或联系人工客服。";


        /// <summary>最大并发数</summary>
        public int MaxConcurrency { get; set; } = 15;


        /// <summary>Embedding 模型配置</summary>
        public LargeModelInfo EmbeddingModel { get; set; }
    }

    /// <summary>置信度计算权重配置</summary>
    public class ConfidenceWeightConfig
    {
        public float SimilarityWeight { get; set; } = 0.35f;
        public float SourceWeight { get; set; } = 0.25f;
        public float QualityWeight { get; set; } = 0.15f;
        public float RecencyWeight { get; set; } = 0.15f;
        public float FeedbackWeight { get; set; } = 0.10f;
    }

    /// <summary>意图规则配置</summary>
    public class IntentRule
    {
        /// <summary>意图名称</summary>
        public string IntentName { get; set; }

        /// <summary>匹配关键词列表（支持 AND/OR 逻辑：&amp; 表示 AND，| 表示 OR）</summary>
        public List<string> Keywords { get; set; } = new List<string>();

        /// <summary>必填字段列表</summary>
        public List<string> RequiredFields { get; set; } = new List<string>();

        /// <summary>字段提取规则（字段名 → 正则表达式）</summary>
        public Dictionary<string, string> FieldExtractionPatterns { get; set; } = new Dictionary<string, string>();

        /// <summary>是否需要确认</summary>
        public bool RequiresConfirmation { get; set; } = false;

        /// <summary>优先级（数字越大越优先）</summary>
        public int Priority { get; set; } = 0;
    }
}
