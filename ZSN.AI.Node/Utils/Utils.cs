using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Entity.Model;
using ZSN.AI.Entity.Workflow;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.Node.Utils
{
    public class Utils
    {
        /// <summary>
        /// 从配置文件加载提示词模板
        /// </summary>
        /// <param name="configKey">配置键，如 "ReporterPrompt"、"FileToMarkdownPrompt" 等</param>
        /// <returns>提示词内容，如果文件不存在返回空字符串</returns>
        public static string LoadPromptTemplate(string configKey)
        {
            try
            {
                string promptFilePath = ConfigHelper.GetString($"PromptTemplates:{configKey}");
                if (string.IsNullOrEmpty(promptFilePath))
                {
                    return string.Empty;
                }

                return LoadPromptTemplateByPath(promptFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载提示词模板失败 [{configKey}]: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 从文件路径加载提示词模板
        /// </summary>
        /// <param name="filePath">相对于应用基目录的文件路径，如 "md/ReporterPrompt.md"</param>
        /// <returns>文件内容，如果文件不存在返回空字符串</returns>
        public static string LoadPromptTemplateByPath(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return string.Empty;
                }

                // 构建完整的文件路径
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(basePath, filePath);

                // 读取文件内容
                if (System.IO.File.Exists(fullPath))
                {
                    return System.IO.File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"提示词文件不存在: {fullPath}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取提示词文件失败 [{filePath}]: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 初始化新工作流，组织初始化节点
        /// </summary>
        /// <param name="MainID"></param>
        /// <param name="MainType"></param>
        /// <returns></returns>
        public static WorkFlow initWorkFlow(string MainID, MainType MainType)
        {

            WorkFlow workflow = new WorkFlow();
            workflow.Info = new WorkflowInfo();
            workflow.Info.MainID = MainID;
            workflow.Info.MainType = MainType;
            workflow.Info.WorkflowName = MainType == MainType.APP ? "应用 工作流" : "助理 工作流";

            workflow.Nodes = new List<WorkflowNodeInfo>();
            Inputs inputs = new Inputs();
            Output output = new Output();

            if (MainType == MainType.APP)
            {
                
                AppInfo appInfo = AppInfoBussiness.GetModel(MainID);

                #region MainAI
                WorkflowNodeInfo MainAINode = new WorkflowNodeInfo();
                MainAINode.NodeID = Guid.NewGuid().ToString();
                MainAINode.WorkflowID = workflow.Info.WorkflowID;
                MainAINode.NodeType = NodeType.MainAI;
                MainAINode.NodeName = "主控AI";

                NodeConfig MainAINodeConfig = new NodeConfig();
                MainAINodeConfig.id = MainAINode.NodeID;
                MainAINodeConfig.mainid = MainID;
                MainAINodeConfig.workflowid = workflow.Info.WorkflowID;
                MainAINodeConfig.type = NodeType.MainAI;
                MainAINodeConfig.position.x = 16;
                MainAINodeConfig.position.y = 80;

                MainAIData mainAIData = new MainAIData();

                mainAIData.prompt = appInfo.Prompt;
                mainAIData.topp = appInfo.TopPCoefficient;
                mainAIData.temperature = appInfo.TemperatureCoefficient;

                LargeModelInfo largeModelInfo = new LargeModelInfo();
                largeModelInfo.LargeModelID = appInfo.SessionModelID;

                inputs = new Inputs();
                inputs.varname = "input";
                mainAIData.inputs.Add(inputs);

                output = new Output();
                output.varname = "results";
                mainAIData.output.Add(output);

                mainAIData.model = largeModelInfo;

                MainAINodeConfig.data = mainAIData;

                MainAINode.Config = MainAINodeConfig;
                workflow.Nodes.Add(MainAINode);
                #endregion

                #region TimeTrigger
                WorkflowNodeInfo TimeTriggerNode = new WorkflowNodeInfo();
                TimeTriggerNode.NodeID = Guid.NewGuid().ToString();
                TimeTriggerNode.WorkflowID = workflow.Info.WorkflowID;
                TimeTriggerNode.NodeType = NodeType.TimeTrigger;
                TimeTriggerNode.NodeName = "时间触发器";

                NodeConfig TimeTriggerNodeConfig = new NodeConfig();
                TimeTriggerNodeConfig.id = TimeTriggerNode.NodeID;
                TimeTriggerNodeConfig.mainid = MainID;
                TimeTriggerNodeConfig.workflowid = workflow.Info.WorkflowID;
                TimeTriggerNodeConfig.type = NodeType.TimeTrigger;
                TimeTriggerNodeConfig.position.x = -808;
                TimeTriggerNodeConfig.position.y = 240;

                TimeTriggerData timeTriggerData = new TimeTriggerData();
                TimeTrigger timeTrigger = new TimeTrigger();

                timeTriggerData.timeTrigger = timeTrigger;
                timeTriggerData.output = new List<Output>();
                timeTriggerData.output.Add(new Output() { varname= "currentTime", value= "{{currentTime}}",type="DateTime",txt="当前时间" });

                TimeTriggerNodeConfig.data = timeTriggerData;

                TimeTriggerNode.Config = TimeTriggerNodeConfig;
                workflow.Nodes.Add(TimeTriggerNode);
                #endregion

                #region Agent
                WorkflowNodeInfo AgentNode = new WorkflowNodeInfo();
                AgentNode.NodeID = Guid.NewGuid().ToString();
                AgentNode.WorkflowID = workflow.Info.WorkflowID;
                AgentNode.NodeType = NodeType.Agent;
                AgentNode.NodeName = "Agent";

                NodeConfig AgentNodeConfig = new NodeConfig();
                AgentNodeConfig.id = AgentNode.NodeID;
                AgentNodeConfig.mainid = MainID;
                AgentNodeConfig.workflowid = workflow.Info.WorkflowID;
                AgentNodeConfig.type = NodeType.Agent;
                AgentNodeConfig.position.x = 16;
                AgentNodeConfig.position.y = 160;

                AgentData AgentData = new AgentData();
                inputs = new Inputs();
                inputs.varname = "input";
                AgentData.inputs.Add(inputs);

                output = new Output();
                output.varname = "results";
                AgentData.output.Add(output);

                AgentData.agent = new AgentInfo();

                AgentNodeConfig.data = AgentData;

                AgentNode.Config = AgentNodeConfig;
                workflow.Nodes.Add(AgentNode);
                #endregion

                #region Reporter
                WorkflowNodeInfo ReporterNode = new WorkflowNodeInfo();
                ReporterNode.NodeID = Guid.NewGuid().ToString();
                ReporterNode.WorkflowID = workflow.Info.WorkflowID;
                ReporterNode.NodeType = NodeType.Reporter;
                ReporterNode.NodeName = "记录员";

                NodeConfig ReporterNodeConfig = new NodeConfig();
                ReporterNodeConfig.id = ReporterNode.NodeID;
                ReporterNodeConfig.mainid = MainID;
                ReporterNodeConfig.workflowid = workflow.Info.WorkflowID;
                ReporterNodeConfig.type = NodeType.Reporter;
                ReporterNodeConfig.position.x = -808;
                ReporterNodeConfig.position.y = 160;

                ReporterData reporterData = new ReporterData();

                reporterData.prompt = LoadPromptTemplate("ReporterPrompt");
                if (string.IsNullOrEmpty(reporterData.prompt))
                {
                    reporterData.prompt = "你是一个谈话记录员,负责将对话内容进行整理,按角色分别提取关键点,并有条理得整理成Json格式。";
                }
                reporterData.topp = appInfo.TopPCoefficient;
                reporterData.temperature = appInfo.TemperatureCoefficient;

                LargeModelInfo reporterLargeModelInfo = new LargeModelInfo();
                reporterLargeModelInfo.LargeModelID = appInfo.SessionModelID;

                reporterData.model = reporterLargeModelInfo;
                reporterData.recordslength = 10;

                ReporterNodeConfig.data = reporterData;

                ReporterNode.Config = ReporterNodeConfig;
                workflow.Nodes.Add(ReporterNode);
                #endregion

                #region Start
                WorkflowNodeInfo StartNode = new WorkflowNodeInfo();
                StartNode.NodeID = Guid.NewGuid().ToString();
                StartNode.WorkflowID = workflow.Info.WorkflowID;
                StartNode.NodeType = NodeType.Start;
                StartNode.NodeName = "开始";

                NodeConfig StartNodeConfig = new NodeConfig();
                StartNodeConfig.id = StartNode.NodeID;
                StartNodeConfig.mainid = MainID;
                StartNodeConfig.workflowid = workflow.Info.WorkflowID;
                StartNodeConfig.type = NodeType.Start;
                StartNodeConfig.position.x = -808;
                StartNodeConfig.position.y = 80;

                StartData startData = new StartData();
                startData.inputs.Add(new Inputs() { varname = "input",txt="输入信息" });
                startData.inputs.Add(new Inputs() { varname = "attachments",type= "List<AttachmentItem>",txt="附件" });
                startData.inputs.Add(new Inputs() { varname = "additionalOptions",type= "dynamic",txt = "附加配置项" });

                startData.output.Add(new Output() { varname = "prompt" });
                startData.output.Add(new Output { varname = "currentTime", value = "{{currentTime}}", type = "DateTime", txt = "当前时间" });

                startData.output.Add(new Output() { varname = "attachments", type = "List<AttachmentItem>" });
                startData.output.Add(new Output() { varname = "additionalOptions", type = "dynamic" });

                StartNodeConfig.data = startData;

                StartNode.Config = StartNodeConfig;

                StartNode.CreateTime = DateTime.Now;
                StartNode.LastUpdateTime = DateTime.Now;
                workflow.Nodes.Add(StartNode);

                #endregion

                #region End
                WorkflowNodeInfo EndNode = new WorkflowNodeInfo();
                EndNode.NodeID = Guid.NewGuid().ToString();
                EndNode.WorkflowID = workflow.Info.WorkflowID;
                EndNode.NodeType = NodeType.End;
                EndNode.NodeName = "结束";

                NodeConfig EndNodeConfig = new NodeConfig();
                EndNodeConfig.id = EndNode.NodeID;
                EndNodeConfig.mainid = MainID;
                EndNodeConfig.workflowid = workflow.Info.WorkflowID;
                EndNodeConfig.type = NodeType.End;
                EndNodeConfig.position.x = 808;
                EndNodeConfig.position.y = 80;

                EndData endData = new EndData();
                inputs = new Inputs();
                inputs.varname = "input";
                endData.inputs.Add(inputs);

                EndNodeConfig.data = endData;

                EndNode.Config = EndNodeConfig;

                EndNode.CreateTime = DateTime.Now;
                EndNode.LastUpdateTime = DateTime.Now;
                workflow.Nodes.Add(EndNode);
                #endregion
            }
            else
            {
                #region Start
                WorkflowNodeInfo StartNode = new WorkflowNodeInfo();
                StartNode.NodeID = Guid.NewGuid().ToString();
                StartNode.WorkflowID = workflow.Info.WorkflowID;
                StartNode.NodeType = NodeType.AgentStart;
                StartNode.NodeName = "开始";

                NodeConfig StartNodeConfig = new NodeConfig();
                StartNodeConfig.id = StartNode.NodeID;
                StartNodeConfig.mainid = MainID;
                StartNodeConfig.workflowid = workflow.Info.WorkflowID;
                StartNodeConfig.type = NodeType.AgentStart;
                StartNodeConfig.position.x = -808;
                StartNodeConfig.position.y = 80;

                AgentStartData startData = new AgentStartData();
                startData.inputs.Add(new Inputs() { varname = "input", txt = "输入信息" });
                startData.inputs.Add(new Inputs() { varname = "attachments", type = "List<AttachmentItem>", txt = "附件" });
                startData.inputs.Add(new Inputs() { varname = "additionalOptions", type = "dynamic", txt = "附加配置项" });
                startData.inputs.Add(new Inputs() { varname = "context", type = "string", txt = "上游传递的上下文" });

                startData.output.Add(new Output() { varname = "prompt" });
                startData.output.Add(new Output { varname = "currentTime", value = "{{currentTime}}", type = "DateTime", txt = "当前时间" });

                startData.output.Add(new Output() { varname = "attachments", type = "List<AttachmentItem>" });
                startData.output.Add(new Output() { varname = "additionalOptions", type = "dynamic" });

                StartNodeConfig.data = startData;

                StartNode.Config = StartNodeConfig;

                StartNode.CreateTime = DateTime.Now;
                StartNode.LastUpdateTime = DateTime.Now;
                workflow.Nodes.Add(StartNode);
                #endregion

                #region EndNode
                WorkflowNodeInfo EndNode = new WorkflowNodeInfo();
                EndNode.NodeID = Guid.NewGuid().ToString();
                EndNode.WorkflowID = workflow.Info.WorkflowID;
                EndNode.NodeType = NodeType.AgentEnd;
                EndNode.NodeName = "结束";

                NodeConfig EndNodeConfig = new NodeConfig();
                EndNodeConfig.id = EndNode.NodeID;
                EndNodeConfig.mainid = MainID;
                EndNodeConfig.workflowid = workflow.Info.WorkflowID;
                EndNodeConfig.type = NodeType.AgentEnd;
                EndNodeConfig.position.x = 808;
                EndNodeConfig.position.y = 80;

                AgentEndData endData = new AgentEndData();
                endData.inputs.Add(new Inputs());

                endData.output.Add(new Output() { varname = "results" });
                endData.output.Add(new Output { varname = "currentTime", value = "{{currentTime}}", type = "DateTime", txt = "当前时间" });
                endData.output.Add(new Output { varname = "agentName", value = "{{agentName}}", type = "String" });

                EndNodeConfig.data = endData;

                EndNode.Config = EndNodeConfig;

                EndNode.CreateTime = DateTime.Now;
                EndNode.LastUpdateTime = DateTime.Now;
                workflow.Nodes.Add(EndNode);
                #endregion
            }

            return workflow;
        }

        /// <summary>
        /// 初始化节点
        /// </summary>
        /// <param name="WorkflowID"></param>
        /// <param name="NodeType"></param>
        /// <param name="MainID"></param>
        /// <returns></returns>
        public static WorkflowNodeInfo newNode(string WorkflowID, NodeType nodeType, string MainID)
        {

            WorkflowNodeInfo nodeInfo = new WorkflowNodeInfo();
            nodeInfo.WorkflowID = WorkflowID;
            nodeInfo.NodeType = nodeType;
            nodeInfo.NodeID = Guid.NewGuid().ToString();

            NodeConfig nodeConfig = new NodeConfig();
            nodeConfig.id = nodeInfo.NodeID;
            nodeConfig.mainid = MainID;
            nodeConfig.workflowid = WorkflowID;
            nodeConfig.type = nodeType;

            switch (nodeType)
            {
                case NodeType.Start:
                case NodeType.AgentStart:

                    StartData startData = new StartData();
                    startData.inputs.Add(new Inputs { varname = "input" });
                    startData.inputs.Add(new Inputs() { varname = "attachments", type = "List<AttachmentItem>", txt = "附件" });
                    startData.inputs.Add(new Inputs() { varname = "additionalOptions", type = "dynamic", txt = "附加配置项" });
                    startData.inputs.Add(new Inputs() { varname = "context", type = "string", txt = "上游传递的上下文" });

                    startData.output.Add(new Output { varname = "prompt" });
                    startData.output.Add(new Output { varname = "currentTime", value = "{{currentTime}}", type = "DateTime", txt = "当前时间" });
                    startData.output.Add(new Output() { varname = "attachments", type = "List<AttachmentItem>" });
                    startData.output.Add(new Output() { varname = "additionalOptions", type = "dynamic" });
                    startData.output.Add(new Output() { varname = "sessionId", type = "string" });

                    nodeConfig.data = startData;
                    break;

                case NodeType.End:
                case NodeType.AgentEnd:

                    EndData endData = new EndData();
                    endData.inputs.Add(new Inputs { varname = "input" });

                    nodeConfig.data = endData;
                    break;

                case NodeType.LargeModel:

                    LargeModelData largeModelData = new LargeModelData();

                    largeModelData.inputs.Add(new Inputs { varname = "input" });

                    largeModelData.output.Add(new Output { varname = "results" });

                    nodeConfig.data = largeModelData;
                    break;
                case NodeType.MainAI:

                    MainAIData mainAIData = new MainAIData();
                    mainAIData.inputs.Add(new Inputs { varname = "input" });
                    mainAIData.output.Add(new Output { varname = "results" });
                    mainAIData.output.Add(new Output { varname = "complete_type" });

                    nodeConfig.data = mainAIData;
                    break;
                case NodeType.Reporter:

                    ReporterData reporterData = new ReporterData();
                    reporterData.inputs.Add(new Inputs { varname = "input" });

                    reporterData.output.Add(new Output { varname = "results" });

                    nodeConfig.data = reporterData;
                    break;
                case NodeType.KnowledgeBase:
                    KnowledgeBaseData knowledgeBaseData = new KnowledgeBaseData();
                    knowledgeBaseData.searchOptions?.VectorWeight = 60;
                    knowledgeBaseData.searchOptions?.GraphWeight = 40;
                    knowledgeBaseData.inputs.Add(new Inputs { varname = "input" });
                    knowledgeBaseData.output.Add(new Output { varname = "results" });

                    nodeConfig.data = knowledgeBaseData;
                    break;
                

                case NodeType.Plugins:
                    PluginsData pluginsData = new PluginsData();

                    pluginsData.inputs.Add(new Inputs { varname = "input" });

                    pluginsData.output.Add(new Output { varname = "results" });

                    nodeConfig.data = pluginsData;
                    break;

                case NodeType.Selector:
                    SelectorData selectorData = new SelectorData();
                    selectorData.inputs.Add(new Inputs { varname = "input" });

                    selectorData.output.Add(new Output { varname = "results" });

                    nodeConfig.data = selectorData;
                    break;
                case NodeType.TimeTrigger:

                    TimeTriggerData timeTriggerData = new TimeTriggerData();
                    TimeTrigger timeTrigger = new TimeTrigger();

                    timeTriggerData.timeTrigger = timeTrigger;
                    timeTriggerData.output.Add(new Output() { varname = "prompt", value = "{{prompt}}", type = "string", txt = "" });
                    timeTriggerData.output.Add(new Output() { varname = "currentTime", value = "{{currentTime}}", type = "DateTime", txt = "当前时间" });

                    nodeConfig.data = timeTriggerData;
                    break;
                case NodeType.Agent:

                    AgentData AgentData = new AgentData();

                    AgentData.inputs.Add(new Inputs { varname = "input" });

                    AgentData.output.Add(new Output { varname = "results" });
                    AgentData.output.Add(new Output { varname = "currentTime", type = "DateTime" });
                    AgentData.output.Add(new Output { varname = "agentName" });

                    AgentData.agent = new AgentInfo();

                    nodeConfig.data = AgentData;
                    break;
                case NodeType.Merge:
                    MergeData mergeData = new MergeData();
                    mergeData.inputs.Add(new Inputs { varname = "input" });

                    mergeData.output.Add(new Output { varname = "results" });

                    nodeConfig.data = mergeData;
                    break;
                case NodeType.MCP:
                    MCPData mcpData = new MCPData();
                    mcpData.inputs.Add(new Inputs { varname = "input" });

                    mcpData.output.Add(new Output { varname = "results" });

                    nodeConfig.data = mcpData;
                    break;
                case NodeType.Notepaper:
                    NotepaperData notepaperData = new NotepaperData();

                    nodeConfig.data = notepaperData;
                    break;
                case NodeType.FileToMarkdown:
                    FileToMarkdownData fileToMarkdownData = new FileToMarkdownData();
                    fileToMarkdownData.inputs.Add(new Inputs { varname = "input" });
                    //fileToMarkdownData.inputs.Add(new Inputs { varname = "attachments", type = "List<AttachmentItem>", txt = "附件" });

                    fileToMarkdownData.prompt = LoadPromptTemplate("FileToMarkdownPrompt");
                    if (string.IsNullOrEmpty(fileToMarkdownData.prompt))
                    {
                        fileToMarkdownData.prompt = "请将图片内容转写为 Markdown 文本。";
                    }

                    fileToMarkdownData.output.Add(new Output { varname = "currentTime", type = "DateTime" });
                    fileToMarkdownData.output.Add(new Output { varname = "markdownFiles", type = "List<ConvertToMarkdownFiles>",txt="转换为Markdown的数据" });


                    nodeConfig.data = fileToMarkdownData;
                    break;
                case NodeType.HumanInTheLoop:
                    HumanInTheLoopData humanInTheLoopData = new HumanInTheLoopData();

                    humanInTheLoopData.inputs.Add(new Inputs { varname = "input" });

                    humanInTheLoopData.output.Add(new Output { varname = "results" });

                    nodeConfig.data = humanInTheLoopData;
                    break;
                case NodeType.IntentionRecognition:
                    IntentionRecognitionData intentionRecognitionData = new IntentionRecognitionData();
                    intentionRecognitionData.inputs.Add(new Inputs { varname = "input" });

                    intentionRecognitionData.output.Add(new Output { varname = "results" });
                    nodeConfig.data = intentionRecognitionData;
                    break;
                case NodeType.HumanInTheLoopInput:
                    HumanInTheLoopInputData humanInTheLoopInputData = new HumanInTheLoopInputData();

                    humanInTheLoopInputData.inputs.Add(new Inputs { varname = "input" });

                    humanInTheLoopInputData.output.Add(new Output { varname = "results" });

                    nodeConfig.data = humanInTheLoopInputData;
                    break;
                case NodeType.SkillAgent:
                    SkillAgentData skillAgentData = new SkillAgentData();
                    skillAgentData.inputs.Add(new Inputs { varname = "input" });
                    skillAgentData.output.Add(new Output { varname = "results" });
                    nodeConfig.data = skillAgentData;
                    break;
                case NodeType.ImageGeneration:
                    ImageGenerationData imageGenerationData = new ImageGenerationData();
                    
                    // 输入参数
                    imageGenerationData.inputs.Add(new Inputs { varname = "prompt", type = "string", txt = "图像生成提示词" });
                    imageGenerationData.inputs.Add(new Inputs { varname = "imageInput", type = "string", txt = "输入图像（图生图，可选）" });
                    
                    // 输出参数
                    imageGenerationData.output.Add(new Output { varname = "imageUrl", type = "string", txt = "生成的图像URL" });
                    imageGenerationData.output.Add(new Output { varname = "prompt", type = "string", txt = "使用的提示词" });
                    imageGenerationData.output.Add(new Output { varname = "width", type = "int", txt = "图像宽度" });
                    imageGenerationData.output.Add(new Output { varname = "height", type = "int", txt = "图像高度" });
                    
                    // 设置默认提示词
                    imageGenerationData.prompt = "一只可爱的小猫在花园里玩耍，阳光明媚，鲜花盛开";
                    
                    nodeConfig.data = imageGenerationData;
                    break;
                case NodeType.VideoGeneration:
                    VideoGenerationData videoGenerationData = new VideoGenerationData();
                    
                    // 输入参数
                    videoGenerationData.inputs.Add(new Inputs { varname = "prompt", type = "string", txt = "视频生成提示词" });
                    videoGenerationData.inputs.Add(new Inputs { varname = "imageInput", type = "string", txt = "输入图像（图生视频，可选）" });
                    videoGenerationData.inputs.Add(new Inputs { varname = "referenceImages", type = "List<string>", txt = "参考图片列表（1-7张，可选）" });
                    videoGenerationData.inputs.Add(new Inputs { varname = "firstFrameUrl", type = "string", txt = "首帧图片（首尾帧生成，可选）" });
                    videoGenerationData.inputs.Add(new Inputs { varname = "lastFrameUrl", type = "string", txt = "尾帧图片（首尾帧生成，可选）" });
                    
                    // 输出参数
                    videoGenerationData.output.Add(new Output { varname = "videoUrl", type = "string", txt = "生成的视频URL" });
                    videoGenerationData.output.Add(new Output { varname = "taskId", type = "string", txt = "任务ID" });
                    videoGenerationData.output.Add(new Output { varname = "prompt", type = "string", txt = "使用的提示词" });
                    videoGenerationData.output.Add(new Output { varname = "duration", type = "int", txt = "视频时长（秒）" });
                    videoGenerationData.output.Add(new Output { varname = "resolution", type = "string", txt = "视频分辨率" });
                    
                    // 设置默认值
                    videoGenerationData.prompt = "一只可爱的小猫在花园里玩耍，阳光明媚，鲜花盛开，镜头缓慢推进";
                    videoGenerationData.generationType = 1; // TextToVideo
                    videoGenerationData.duration = 5;
                    videoGenerationData.size = "720x1280";
                    videoGenerationData.aspectRatio = "9:16";
                    videoGenerationData.resolution = "720p";
                    
                    nodeConfig.data = videoGenerationData;
                    break;
                case NodeType.ClawAI:
                    ClawAIData clawAIData = new ClawAIData();
                    
                    // 输入参数
                    clawAIData.inputs.Add(new Inputs { varname = "prompt", type = "string", txt = "用户任务" });
                    
                    // 输出参数
                    clawAIData.output.Add(new Output { varname = "results", type = "string", txt = "最终答案" });
                    clawAIData.output.Add(new Output { varname = "taskPlanning", type = "string", txt = "完整规划JSON" });
                    clawAIData.output.Add(new Output { varname = "totalSteps", type = "int", txt = "总步骤数" });
                    clawAIData.output.Add(new Output { varname = "completedSteps", type = "int", txt = "已完成步骤数" });
                    clawAIData.output.Add(new Output { varname = "iterations", type = "int", txt = "迭代次数" });
                    clawAIData.output.Add(new Output { varname = "planningStatus", type = "string", txt = "规划状态" });

                    //主AI设置
                    clawAIData.masterControlConfig = new MasterControlConfig
                    {
                        enabled = true,
                    };
                    

                    // 设置默认配置
                    clawAIData.taskPlanningConfig = new TaskPlanningConfig
                    {
                        enabled = true,
                        useDedicatedModel = true,
                        planningStrategy = "adaptive",
                        maxSteps = 5,
                        allowDynamicReplanning = true
                    };

                    
                    // 加载统一规划提示词 (P0优化: 合并主控判断与任务规划)
                    string unifiedPlanPrompt = LoadPromptTemplate("ClawAIUnifiedPlanPrompt");
                    if (!string.IsNullOrEmpty(unifiedPlanPrompt))
                    {
                        clawAIData.taskPlanningConfig.unifiedPlanPromptTemplate = unifiedPlanPrompt;
                    }
                    
                    clawAIData.workFlowLoopConfig = new WorkFlowLoopConfig
                    {
                        enabled = true,
                        maxIterations = 5,
                        selectionStrategy = "auto",
                        executionMode = "sequential",
                        continueOnWorkFlowFailure = true,
                        qualityThreshold = 70
                    };
                    
                    clawAIData.reflectionConfig = new ReflectionConfig
                    {
                        enabled = true,
                        useDedicatedModel = true
                    };
                    
                    // 加载反思提示词
                    string reflectionPrompt = LoadPromptTemplate("ClawAIReflectionPrompt");
                    if (!string.IsNullOrEmpty(reflectionPrompt))
                    {
                        clawAIData.reflectionConfig.reflectionPromptTemplate = reflectionPrompt;
                    }
                    
                    clawAIData.memoryConfig = new MemoryConfig
                    {
                        useDedicatedModel = true,
                        enableWorkingMemory = true,
                        enableLongTermMemory = true,
                        enableEpisodicMemory = true,
                        compressionStrategy = "summary",
                        relevanceThreshold = 70
                    };
                    
                    clawAIData.userProfileConfig = new UserProfileConfig
                    {
                        useDedicatedModel = true,
                        enabled = true,
                        trackPreferences = true,
                        trackInteractionPatterns = true,
                        personalizationStrength = 50
                    };
                    
                    clawAIData.personalityConfig = new PersonalityConfig
                    {
                        useDedicatedModel = true,
                        enabled = true,
                        enableEmotionalState = false,
                        enableGoalOriented = true
                    };
                    
                    nodeConfig.data = clawAIData;
                    break;
                case NodeType.ServiceDesk:
                    ServiceDeskData serviceDeskData = new ServiceDeskData();

                    // 输入参数
                    serviceDeskData.inputs.Add(new Inputs { varname = "prompt", type = "string", txt = "用户消息" });

                    // 输出参数
                    serviceDeskData.output.Add(new Output { varname = "response", type = "string", txt = "回复内容" });
                    serviceDeskData.output.Add(new Output { varname = "confidence", type = "string", txt = "置信度" });
                    serviceDeskData.output.Add(new Output { varname = "strategy", type = "string", txt = "处理策略" });
                    serviceDeskData.output.Add(new Output { varname = "needsEscalation", type = "string", txt = "是否需要升级" });

                    // 默认提示词
                    string sdRAGPrompt = LoadPromptTemplate("ServiceDeskRAGResponsePrompt");
                    if (!string.IsNullOrEmpty(sdRAGPrompt))
                    {
                        serviceDeskData.PersonaPrompt = sdRAGPrompt; // 使用模板文件中的提示词
                    }

                    nodeConfig.data = serviceDeskData;
                    break;
                case NodeType.Research:
                    ResearchNodeData researchData = new ResearchNodeData();
                    researchData.inputs.Add(new Inputs { varname = "prompt", type = "string", txt = "研究目标" });
                    researchData.output.Add(new Output { varname = "results", type = "string", txt = "研究结果(Markdown)" });
                    researchData.output.Add(new Output { varname = "summary", type = "string", txt = "研究摘要" });
                    researchData.output.Add(new Output { varname = "sources", type = "string", txt = "信息来源(JSON)" });
                    researchData.output.Add(new Output { varname = "key_findings", type = "string", txt = "关键发现(JSON)" });
                    nodeConfig.data = researchData;
                    break;
                case NodeType.Voice:
                    VoiceNodeData voiceData = new VoiceNodeData();

                    // 输入参数
                    voiceData.inputs.Add(new Inputs { varname = "prompt", type = "string", txt = "LLM后处理提示词" });
                    voiceData.inputs.Add(new Inputs { varname = "audioSource", type = "string", txt = "音频来源(URL/路径)" });

                    // 输出参数
                    voiceData.output.Add(new Output { varname = "results", type = "string", txt = "最终结果(LLM处理后)" });
                    voiceData.output.Add(new Output { varname = "transcription", type = "string", txt = "转写文本" });
                    voiceData.output.Add(new Output { varname = "duration", type = "string", txt = "音频时长(秒)" });
                    voiceData.output.Add(new Output { varname = "speakerCount", type = "string", txt = "说话人数量" });
                    voiceData.output.Add(new Output { varname = "provider", type = "string", txt = "转写服务商" });

                    // 默认提示词
                    voiceData.prompt = LoadPromptTemplate("VoiceDefaultPrompt");
                    if (string.IsNullOrEmpty(voiceData.prompt))
                    {
                        voiceData.prompt = "请对以下语音转写文本进行整理，修正标点符号和明显错误，并生成简要摘要。";
                    }

                    nodeConfig.data = voiceData;
                    break;
            }
            nodeInfo.Config = nodeConfig;

            return nodeInfo;
        }

        

        /// <summary>
        /// 添加节点执行记录
        /// </summary>
        /// <param name="SessionID"></param>
        /// <param name="CurrentNode"></param>
        /// <param name="NextNodeID"></param>
        /// <returns>RecordID</returns>
        public static string newExcutionRecord(string SessionID, NodeConfig CurrentNode,string ProcessesID,string TaskID, string NextNodeID="", string FromMainTaskID="", List<Inputs> inputs=null)
        {
            WorkflowNodeExecutionRecordInfo recordInfo = new WorkflowNodeExecutionRecordInfo();
            recordInfo.SessionID = SessionID;
            recordInfo.ProcessesID = ProcessesID;
            recordInfo.NextNodeID = NextNodeID;
            recordInfo.WorkflowID = CurrentNode.workflowid;
            recordInfo.TaskID = TaskID;
            recordInfo.NodeID = CurrentNode.id;
            recordInfo.StartTime = DateTime.Now;
            recordInfo.EndTime = DateTime.Now;
            recordInfo.Status = ExecutionRecordStatus.Running;
            recordInfo.Inputs = inputs != null && inputs.Count > 0 ? inputs : CurrentNode.data;
            recordInfo.Outputs = null;
            recordInfo.Logs = null;
            // 从 data 中提取用户命名的 label，提升可读性（如 "End:结束节点" 而非 "End:End"）
            string nodeLabel = CurrentNode.name;
            try
            {
                if (CurrentNode.data != null)
                {
                    var dataObj = CurrentNode.data as Newtonsoft.Json.Linq.JObject
                        ?? Newtonsoft.Json.Linq.JObject.FromObject(CurrentNode.data);
                    var labelToken = dataObj?.Property("label")?.Value;
                    if (labelToken != null && !string.IsNullOrEmpty(labelToken.ToString()))
                        nodeLabel = labelToken.ToString();
                }
            }
            catch { /* 解析失败时回退到 CurrentNode.name */ }
            recordInfo.NodeName = Equals(CurrentNode.type.ToString(), nodeLabel) ? CurrentNode.type.ToString() : $"{CurrentNode.type.ToString()}:{nodeLabel}";
            recordInfo.FromMainTaskID = FromMainTaskID;
            
            switch (CurrentNode.type)
            {
                case NodeType.End:
                    recordInfo.NextNodeID = NodeType.End.ToString();
                    break;
                case NodeType.AgentEnd:
                    recordInfo.NextNodeID = NodeType.AgentEnd.ToString();
                    break;
            }

            WorkflowNodeExecutionRecordInfoBussiness.Add(recordInfo);

            return recordInfo.RecordID;
        }

        /// <summary>
        /// 记录更新执行结果
        /// </summary>
        /// <param name="RecordID"></param>
        /// <param name="Status"></param>
        /// <param name="Outputs"></param>
        /// <param name="Logs"></param>
        public static void updateExcutionRecord(string RecordID, ExecutionRecordStatus Status, object Outputs,object Logs) {

            WorkflowNodeExecutionRecordInfoBussiness.Update( RecordID,  Status,  Outputs,  Logs);
        }

        /// <summary>
        /// 只更新 Outputs 和 Logs，不修改 Status（避免后台后处理覆盖已写入的最终状态）
        /// </summary>
        public static void updateExcutionRecordLogs(string RecordID, object Outputs, object Logs) {

            WorkflowNodeExecutionRecordInfoBussiness.UpdateLogs(RecordID, Outputs, Logs);
        }

        /// <summary>
        /// 填充附件文件路径
        /// </summary>
        /// <param name="AttachmentItems"></param>
        /// <returns></returns>
        public static List<AttachmentItem> updateAttachmentItemsFilePath(List<AttachmentItem> AttachmentItems)
        {
            if (AttachmentItems != null && AttachmentItems.Count > 0)
            {
                foreach (var item in AttachmentItems)
                {
                    FilesInfo fileInfo = FilesInfoBussiness.GetModel(item.FileCode);
                    string sourcePath = "";
                    if (fileInfo != null)
                    {
                        sourcePath = fileInfo.FFilePath + "/" + fileInfo.FName;
                    }
                    if (System.IO.File.Exists(sourcePath))
                    {
                        item.FilePath = sourcePath;
                    }
                }
            }
            return AttachmentItems;
        }

        public static async Task<ChatHistory> AttachmentToChatHistoryAsync(List<AttachmentItem> AttachmentItems, ChatHistory history)
        {
            var _ChatMessage = new ChatMessageContentItemCollection();

            foreach (var attachment in AttachmentItems)
            {
                // 智能选择文件来源：优先使用本地文件，不存在则从URI获取
                byte[] bytes;
                if (File.Exists(attachment.FilePath))
                {
                    bytes = File.ReadAllBytes(attachment.FilePath);
                }
                else if (!string.IsNullOrEmpty(attachment.FileURI))
                {
                    using (var httpClient = new HttpClient())
                    {
                        bytes = await httpClient.GetByteArrayAsync(attachment.FileURI);
                    }
                }
                else
                {
                    continue;
                }

                // 从文件名提取真正的文件扩展名(不使用Type字段,因为Type存储的是附件类型分类如"Image"、"Document")
                string extension = null;
                if (!string.IsNullOrEmpty(attachment.Name))
                {
                    extension = Path.GetExtension(attachment.Name)?.TrimStart('.').ToLower();
                }
                else if (!string.IsNullOrEmpty(attachment.FilePath))
                {
                    extension = Path.GetExtension(attachment.FilePath)?.TrimStart('.').ToLower();
                }

                // 容错处理:如果无法提取扩展名,跳过该附件
                if (string.IsNullOrEmpty(extension))
                {
                    Console.WriteLine($"警告: 无法从附件中提取文件扩展名,跳过该附件。Name={attachment.Name}, FilePath={attachment.FilePath}");
                    continue;
                }

                // 根据扩展名判断是图片还是其他文件类型
                if (FilesExtension.ImageExtensionMimeTypes.ContainsKey(extension))
                {
                    _ChatMessage.Add(new ImageContent(bytes, FilesExtension.ImageExtensionMimeTypes[extension]));
                }
                else if (FilesExtension.FilesExtensionMimeTypes.ContainsKey(extension))
                {
#pragma warning disable SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                    _ChatMessage.Add(new BinaryContent(bytes, FilesExtension.FilesExtensionMimeTypes[extension]));
#pragma warning restore SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                }
                else
                {
                    // 扩展名不在字典中,使用默认MIME类型
                    Console.WriteLine($"警告: 未知的文件扩展名 '{extension}',使用默认MIME类型 'application/octet-stream'");
#pragma warning disable SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                    _ChatMessage.Add(new BinaryContent(bytes, "application/octet-stream"));
#pragma warning restore SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                }
            }
            history.AddUserMessage(_ChatMessage);
            return history;
        }

        

        /// <summary>
        /// 预处理文本:去除markdown代码块、替换特殊引号
        /// </summary>
        private static string PreprocessText(string raw)
        {
            string text = raw.Trim();

            // 去除markdown代码块 ```json ... ```
            int firstFence = text.IndexOf("```", StringComparison.Ordinal);
            if (firstFence >= 0)
            {
                int secondFence = text.IndexOf("```", firstFence + 3, StringComparison.Ordinal);
                if (secondFence > firstFence)
                {
                    text = text.Substring(firstFence + 3, secondFence - (firstFence + 3)).Trim();
                    // 去除 "json" 标记
                    if (text.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                    {
                        int newline = text.IndexOfAny(new[] { '\n', '\r' });
                        text = newline >= 0 ? text.Substring(newline + 1).Trim() : text.Substring(4).Trim();
                    }
                }
            }

            // 替换中文引号和其他特殊引号为标准引号
            text = text.Replace('\u201C', '"')  // "
                       .Replace('\u201D', '"')  // "
                       .Replace('\u2018', '\'') // '
                       .Replace('\u2019', '\''); // '

            return text.Trim();
        }

        /// <summary>
        /// 提取JSON候选文本(通过平衡括号)
        /// </summary>
        private static string ExtractJsonCandidate(string text)
        {
            // 优先查找对象 {}
            int objStart = text.IndexOf('{');
            if (objStart >= 0 && TryExtractBalancedBraces(text, objStart, '{', '}', out string objJson))
            {
                return objJson;
            }

            // 其次查找数组 []
            int arrStart = text.IndexOf('[');
            if (arrStart >= 0 && TryExtractBalancedBraces(text, arrStart, '[', ']', out string arrJson))
            {
                return arrJson;
            }

            return string.Empty;
        }

        /// <summary>
        /// 规范化JSON格式,修复常见问题
        /// </summary>
        private static string NormalizeJsonFormat(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            try
            {
                string normalized = text;

                // 1. 为裸属性名添加引号 {name: "value"} => {"name": "value"}
                normalized = Regex.Replace(normalized,
                    @"(?<=[{,]\s*)([A-Za-z_][A-Za-z0-9_]*)(?=\s*:)",
                    "\"$1\"",
                    RegexOptions.Compiled);

                // 2. 为空值填充空字符串 {"key":,} => {"key":"",}
                normalized = Regex.Replace(normalized,
                    @":\s*(?=[,}])",
                    ": \"\"",
                    RegexOptions.Compiled);

                // 3. 为裸字符串值添加引号(排除数字、布尔值、null、对象、数组)
                normalized = Regex.Replace(normalized,
                    @"(:\s*)(?![""[\{\-\d]|true\b|false\b|null\b)([A-Za-z_][A-Za-z0-9_]*)(?=\s*[,}\]])",
                    "$1\"$2\"",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

                // 4. 移除尾随逗号 {"key":"value",} => {"key":"value"}
                normalized = Regex.Replace(normalized,
                    @",\s*([}\]])",
                    "$1",
                    RegexOptions.Compiled);

                // 5. 将单引号替换为双引号
                normalized = Regex.Replace(normalized,
                    @"'([^']*?)'",
                    "\"$1\"",
                    RegexOptions.Compiled);

                return normalized.Trim();
            }
            catch
            {
                return text;
            }
        }

        private static bool TryParseJson(string text, out string compact)
        {
            compact = string.Empty;
            try
            {
                var token = Newtonsoft.Json.Linq.JToken.Parse(text);
                compact = token.ToString(Newtonsoft.Json.Formatting.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 提取平衡的括号内容(支持对象{}和数组[])
        /// </summary>
        private static bool TryExtractBalancedBraces(string text, int startIndex, char openChar, char closeChar, out string result)
        {
            result = string.Empty;
            if (startIndex < 0 || startIndex >= text.Length || text[startIndex] != openChar)
                return false;

            int depth = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = startIndex; i < text.Length; i++)
            {
                char c = text[i];

                if (inString)
                {
                    escaped = escaped ? false : (c == '\\');
                    if (!escaped && c == '"') inString = false;
                }
                else
                {
                    if (c == '"')
                    {
                        inString = true;
                    }
                    else if (c == openChar)
                    {
                        depth++;
                    }
                    else if (c == closeChar)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            result = text.Substring(startIndex, i - startIndex + 1).Trim();
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
