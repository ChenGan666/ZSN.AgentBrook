using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AI.Node.Utils
{
    /// <summary>
    /// 工作流自动生成器 — 三阶段流水线编排引擎
    /// Phase 1: 规划工作流结构 → Phase 2: 并行生成节点详情 → Phase 3: 工程组装
    /// 通过 Func<StreamEvent, Task> 回调推送 SSE 事件到 Controller
    /// </summary>
    public class WorkflowAutoGenerator
    {
        private readonly IChatService _chatService;
        private readonly ILogger<WorkflowAutoGenerator> _logger;

        public WorkflowAutoGenerator(IChatService chatService, ILogger<WorkflowAutoGenerator> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        // === 入口方法 ===

        /// <summary>
        /// 三阶段流水线入口，通过 callback 推送 SSE 事件
        /// </summary>
        public async Task GenerateAsync(
            string sourceNodeId,
            string sourceNodeType,
            string upstreamContextJson,
            string userRequirement,
            string workflowId,
            string mainId,
            Func<StreamEvent, Task> onEvent,
            CancellationToken ct = default)
        {
            // 1. 解析上游上下文
            JObject upstreamCtx = null;
            if (!string.IsNullOrEmpty(upstreamContextJson))
            {
                try { upstreamCtx = JObject.Parse(upstreamContextJson); }
                catch { _logger.LogWarning("解析 upstreamContext 失败"); }
            }

            // 2. 构建变量清单文本
            var variableCatalog = BuildUpstreamContextDescription(upstreamCtx, sourceNodeId);

            // 3. 加载当前工作流
            var currentWorkflow = LoadWorkflow(workflowId);
            if (currentWorkflow == null)
            {
                await onEvent(new StreamEvent
                {
                    EventType = "error",
                    Data = new { message = "工作流不存在" }
                });
                return;
            }

            var keepNodeIds = GetUpstreamNodeIds(currentWorkflow, sourceNodeId);
            keepNodeIds.Add(sourceNodeId);

            // === Phase 1: 规划 ===
            await onEvent(new StreamEvent
            {
                EventType = "progress",
                Data = new ProgressData
                {
                    Phase = "planning",
                    Message = "正在规划工作流结构..."
                }
            });

            WorkflowPlan plan;
            try
            {
                plan = await GeneratePlanAsync(variableCatalog, userRequirement, sourceNodeId, sourceNodeType, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 1 规划失败");
                await onEvent(new StreamEvent
                {
                    EventType = "error",
                    Data = new { phase = "planning", message = $"规划失败: {ex.Message}" }
                });
                return;
            }

            if (plan == null || plan.Steps.Count == 0)
            {
                await onEvent(new StreamEvent
                {
                    EventType = "error",
                    Data = new { phase = "planning", message = "AI 未能生成有效规划" }
                });
                return;
            }

            // 推送规划结果到前端
            await onEvent(new StreamEvent
            {
                EventType = "plan",
                Data = new { steps = plan.Steps, edges = plan.Edges }
            });

            // === Phase 2: 并行生成节点详情 ===
            await onEvent(new StreamEvent
            {
                EventType = "progress",
                Data = new ProgressData
                {
                    Phase = "generating",
                    Message = $"正在生成节点详情，共 {plan.Steps.Count} 个节点"
                }
            });

            Dictionary<int, JObject> nodeConfigs;
            try
            {
                nodeConfigs = await GenerateNodeDetailsAsync(
                    plan, variableCatalog, sourceNodeId, sourceNodeType, workflowId, mainId, onEvent, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 2 节点生成失败");
                await onEvent(new StreamEvent
                {
                    EventType = "error",
                    Data = new { phase = "generating", message = $"节点详情生成失败: {ex.Message}" }
                });
                return;
            }

            if (nodeConfigs == null || nodeConfigs.Count == 0)
            {
                await onEvent(new StreamEvent
                {
                    EventType = "error",
                    Data = new { phase = "generating", message = "所有节点生成均失败" }
                });
                return;
            }

            // === Phase 3: 组装 ===
            await onEvent(new StreamEvent
            {
                EventType = "progress",
                Data = new ProgressData
                {
                    Phase = "assembling",
                    Message = "正在组装工作流..."
                }
            });

            try
            {
                _logger.LogInformation("Phase 3 开始组装: plan.Steps={Steps}, nodeConfigs={Configs}",
                    plan.Steps.Count, nodeConfigs.Count);

                var result = AssembleWorkflow(currentWorkflow, keepNodeIds,
                    sourceNodeId, plan, nodeConfigs);

                _logger.LogInformation("Phase 3 组装完成, 开始序列化 complete 数据...");

                // 推送完成结果
                var completeData = new
                {
                    workflow = new
                    {
                        WorkflowID = result.WorkflowID,
                        MainID = result.MainID,
                        MainType = (int)result.MainType,
                        Nodes = result.Nodes.Select(n => new
                        {
                            NodeID = n.NodeID,
                            NodeType = n.NodeType.ToString(),
                            NodeName = n.NodeName,
                            Description = n.Description,
                            Config = n.Config
                        }).ToList(),
                        Edges = result.Edges.Select(e => new
                        {
                            EdgeID = e.EdgeID,
                            SourceNodeId = e.SourceNodeId,
                            TargetNodeId = e.TargetNodeId,
                            Config = e.Config
                        }).ToList()
                    }
                };

                // 测试序列化是否成功
                var testJson = JsonConvert.SerializeObject(completeData);
                _logger.LogInformation("Phase 3 complete 数据序列化成功, 长度={Len}", testJson.Length);

                await onEvent(new StreamEvent
                {
                    EventType = "complete",
                    Data = completeData
                });
                _logger.LogInformation("Phase 3 complete 事件已推送");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 3 组装/序列化失败");
                await onEvent(new StreamEvent
                {
                    EventType = "error",
                    Data = new { phase = "assembling", message = $"组装失败: {ex.Message}" }
                });
            }
        }

        // === Phase 1: 工作流规划 ===

        /// <summary>
        /// Phase 1: 生成工作流规划（轻量 LLM 调用，仅输出步骤列表 + 连线）
        /// </summary>
        private async Task<WorkflowPlan> GeneratePlanAsync(
            string variableCatalog, string userRequirement, string sourceNodeId,
            string sourceNodeType, CancellationToken ct)
        {
            var modelConfig = GetDefaultModelConfig(temperature: 0.3);
            var planPrompt = Node.Utils.Utils.LoadPromptTemplate("AutoGeneratePlan");

            // 若提示词模板加载失败，使用内置兜底提示词
            if (string.IsNullOrEmpty(planPrompt))
            {
                _logger.LogWarning("AutoGeneratePlan 提示词模板为空，使用内置提示词");
                planPrompt = BuildFallbackPlanPrompt();
            }

            var nodeTypeCatalog = BuildNodeTypeCatalog();

            var history = new ChatHistory();
            history.AddSystemMessage(planPrompt
                .Replace("{{NODE_TYPE_CATALOG}}", nodeTypeCatalog));
            history.AddUserMessage(
                $"## 源节点信息\n" +
                $"- 节点ID: {sourceNodeId}\n" +
                $"- 节点类型: {sourceNodeType}\n\n" +
                $"{variableCatalog}\n\n## 用户需求\n{userRequirement}");

            var rawContent = new StringBuilder();
            var chatResult = _chatService.SendChatAsync(modelConfig, history, ct: ct);
            await foreach (var content in chatResult.WithCancellation(ct))
                rawContent.Append(content.ConvertToString());

            var rawText = rawContent.ToString();
            _logger.LogInformation("Phase 1 LLM 原始输出(前500字符): {Raw}",
                rawText.Length > 500 ? rawText.Substring(0, 500) : rawText);

            // 多策略解析规划 JSON
            var plan = TryParsePlanJson(rawText);
            if (plan == null)
                throw new InvalidOperationException($"无法从 AI 响应中解析规划 JSON。原始输出: {rawText.Substring(0, Math.Min(rawText.Length, 300))}");

            // 自动追加 End/AgentEnd 节点（如果规划中没有）
            if (!plan.Steps.Any(s =>
                s.NodeType == "End" || s.NodeType == "AgentEnd"))
            {
                int lastIdx = plan.Steps.Count;
                plan.Steps.Add(new PlanStep
                {
                    StepIndex = lastIdx + 1,
                    NodeType = "End",
                    NodeName = "结束",
                    Description = "流程终点",
                    Inputs = new List<PlanIO>
                    {
                        new PlanIO
                        {
                            Varname = "input",
                            SourceRef = $"{{{{STEP{lastIdx}}}_results}}",
                            Type = "string"
                        }
                    },
                    Outputs = new List<PlanIO>()
                });
                plan.Edges.Add(new PlanEdgeDef
                {
                    FromStepIndex = lastIdx,
                    ToStepIndex = lastIdx + 1
                });
            }

            return plan;
        }

        // === Phase 2: 并行节点详情生成 ===

        /// <summary>
        /// Phase 2: "基座 + AI 修改"模式并行生成节点详情
        /// 每完成一个节点即通过 onEvent 推送 event:node
        /// </summary>
        private async Task<Dictionary<int, JObject>> GenerateNodeDetailsAsync(
            WorkflowPlan plan, string variableCatalog, string sourceNodeId,
            string sourceNodeType,
            string workflowId, string mainId,
            Func<StreamEvent, Task> onEvent, CancellationToken ct)
        {
            const int MAX_CONCURRENCY = 5;
            var semaphore = new SemaphoreSlim(MAX_CONCURRENCY);
            var results = new ConcurrentDictionary<int, JObject>();
            var modifyPrompt = Node.Utils.Utils.LoadPromptTemplate("ModifyNodeDetail");

            var tasks = plan.Steps.Select(async step =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    // 推送进度
                    await onEvent(new StreamEvent
                    {
                        EventType = "progress",
                        Data = new ProgressData
                        {
                            Phase = "generating",
                            Message = $"正在生成节点 {step.StepIndex}/{plan.Steps.Count}: {step.NodeName}",
                            StepIndex = step.StepIndex,
                            TotalSteps = plan.Steps.Count
                        }
                    });

                    // 2.1: 获取标准基础 Config
                    var nodeType = (NodeType)Enum.Parse(typeof(NodeType), step.NodeType);
                    var baseNodeInfo = Node.Utils.Utils.newNode(workflowId, nodeType, mainId);
                    var baseConfig = JObject.FromObject(baseNodeInfo.Config);
                    var baseData = baseConfig["data"] as JObject;

                    if (baseData == null)
                    {
                        _logger.LogWarning($"节点 {step.StepIndex} 基础 Config 无 data 字段");
                        return;
                    }

                    // 2.2: LLM 仅输出需要修改的字段
                    var modelConfig = GetDefaultModelConfig(temperature: 0.5);
                    var history = new ChatHistory();
                    history.AddSystemMessage(modifyPrompt);
                    history.AddUserMessage(
                        $"## 基础节点 JSON（结构完整，请只输出需要修改的字段）\n"
                        + $"{JsonConvert.SerializeObject(baseData, Formatting.Indented)}\n\n"
                        + $"## 源节点信息\n"
                        + $"- 节点ID: {sourceNodeId}\n"
                        + $"- 节点类型: {sourceNodeType}\n\n"
                        + $"## 规划信息\n"
                        + $"- 节点类型: {step.NodeType}\n"
                        + $"- 节点名称(label): {step.NodeName}\n"
                        + $"- 功能描述: {step.Description}\n\n"
                        + $"## 输入变量定义\n{PlanIOListToText(step.Inputs)}\n\n"
                        + $"## 输出变量定义\n{PlanIOListToText(step.Outputs)}\n\n"
                        + $"## 上游可用变量清单\n{variableCatalog}");

                    var rawContent = new StringBuilder();
                    var chatResult = _chatService.SendChatAsync(modelConfig, history, ct: ct);
                    await foreach (var content in chatResult.WithCancellation(ct))
                        rawContent.Append(content.ConvertToString());

                    // 2.3: 多策略解析 LLM 修改字段，合并到基础 Config
                    var modifications = TryParseModificationsJson(rawContent.ToString(), step.StepIndex);
                    if (modifications != null)
                    {
                        foreach (var prop in modifications.Properties())
                        {
                            // output/inputs 数组按 varname 匹配合并，避免 LLM 输出不完整覆盖基础字段
                            if ((prop.Name == "output" || prop.Name == "inputs") &&
                                prop.Value is JArray llmArray &&
                                baseData[prop.Name] is JArray baseArray)
                            {
                                SmartMergeArray(baseArray, llmArray);
                            }
                            else
                            {
                                baseData[prop.Name] = prop.Value;
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"节点 {step.StepIndex} ({step.NodeName}) LLM 修改解析全部失败，使用基础配置");
                    }

                    // 确保 label 字段被设置
                    if (string.IsNullOrEmpty(baseData["label"]?.ToString()))
                        baseData["label"] = step.NodeName;

                    results[step.StepIndex] = baseConfig;

                    // 2.4: 使用临时 ID 流式推送单个节点到前端
                    var tempNodeId = $"temp-{step.StepIndex}";
                    baseConfig["id"] = tempNodeId;

                    await onEvent(new StreamEvent
                    {
                        EventType = "node",
                        Data = new
                        {
                            stepIndex = step.StepIndex,
                            nodeInfo = new
                            {
                                NodeID = tempNodeId,
                                NodeType = step.NodeType,
                                NodeName = step.NodeName,
                                Description = step.Description,
                                Config = (object)baseConfig
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"节点 {step.StepIndex} ({step.NodeName}) 生成失败");
                    // 单个节点失败不阻断其他节点
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return new Dictionary<int, JObject>(results);
        }

        // === Phase 3: 工程组装 ===

        /// <summary>
        /// Phase 3: 将规划 + 各节点详情组装为完整 WorkFlow（纯代码，无 LLM 调用）
        /// </summary>
        private WorkFlow AssembleWorkflow(
            WorkFlow currentWorkflow,
            HashSet<string> keepNodeIds,
            string sourceNodeId,
            WorkflowPlan plan,
            Dictionary<int, JObject> nodeConfigs)
        {
            // 1. 建立 stepIndex → 真实 nodeId 映射
            var idMap = new Dictionary<int, string>();
            foreach (var step in plan.Steps)
            {
                idMap[step.StepIndex] = Guid.NewGuid().ToString();
            }

            // 2. 为每个节点替换 ID 和 sourceId 引用
            var assembledNodes = new List<WorkflowNodeInfo>();
            foreach (var step in plan.Steps)
            {
                if (!nodeConfigs.TryGetValue(step.StepIndex, out var config))
                {
                    _logger.LogWarning($"节点 stepIndex={step.StepIndex} 无对应 Config，跳过");
                    continue;
                }

                var newNodeId = idMap[step.StepIndex];

                // 替换 Config 中的临时 ID
                config["id"] = newNodeId;

                // 替换 inputs 中的占位符 sourceId
                var inputs = config["data"]?["inputs"] as JArray;
                if (inputs != null)
                {
                    foreach (var input in inputs)
                    {
                        var sourceRef = input["sourceId"]?.ToString();
                        if (!string.IsNullOrEmpty(sourceRef))
                        {
                            input["sourceId"] = ResolveSourceRef(sourceRef, sourceNodeId, idMap);
                        }
                    }
                }

                // 替换 prompt 中的占位符
                var prompt = config["data"]?["prompt"]?.ToString();
                if (!string.IsNullOrEmpty(prompt))
                {
                    config["data"]["prompt"] = ResolveSourceRef(prompt, sourceNodeId, idMap);
                }

                assembledNodes.Add(new WorkflowNodeInfo
                {
                    NodeID = newNodeId,
                    WorkflowID = currentWorkflow.Info.WorkflowID,
                    NodeType = (NodeType)Enum.Parse(typeof(NodeType), step.NodeType),
                    NodeName = step.NodeName,
                    Description = step.Description,
                    Config = config
                });
            }

            // 3. 构建 Edges
            var assembledEdges = new List<WorkflowEdgeInfo>();
            foreach (var planEdge in plan.Edges)
            {
                string sourceId, targetId;

                if (planEdge.FromStepIndex == 0)
                {
                    sourceId = sourceNodeId; // 从源节点出发
                }
                else
                {
                    if (!idMap.TryGetValue(planEdge.FromStepIndex, out sourceId)) continue;
                }

                if (!idMap.TryGetValue(planEdge.ToStepIndex, out targetId)) continue;

                var edgeId = Guid.NewGuid().ToString();
                assembledEdges.Add(new WorkflowEdgeInfo
                {
                    EdgeID = edgeId,
                    WorkflowID = currentWorkflow.Info.WorkflowID,
                    SourceNodeId = sourceId,
                    TargetNodeId = targetId,
                    Config = new
                    {
                        id = edgeId,
                        source = sourceId,
                        target = targetId,
                        markerEnd = "arrowclosed",
                        style = "stroke-width: 3;stroke:rgba(62, 71, 222, 0.38);"
                    }
                });
            }

            // 4. 自动布局
            AutoLayoutNodes(assembledNodes, assembledEdges, sourceNodeId);

            // 5. 校验 DAG
            if (HasCycle(assembledNodes, assembledEdges))
                throw new InvalidOperationException("组装后的工作流存在环路");

            // 6. 合并到保留节点
            var merged = new WorkFlow
            {
                WorkflowID = currentWorkflow.Info.WorkflowID,
                MainID = currentWorkflow.Info.MainID,
                MainType = currentWorkflow.Info.MainType,
                Info = currentWorkflow.Info,
                Config = currentWorkflow.Config,
                Nodes = currentWorkflow.Nodes.Where(n => keepNodeIds.Contains(n.NodeID)).ToList(),
                Edges = currentWorkflow.Edges
                    .Where(e => keepNodeIds.Contains(e.SourceNodeId)
                             && keepNodeIds.Contains(e.TargetNodeId)).ToList()
            };
            merged.Nodes.AddRange(assembledNodes);
            merged.Edges.AddRange(assembledEdges);

            return merged;
        }

        // === 辅助方法 ===

        /// <summary>
        /// 加载工作流
        /// </summary>
        private WorkFlow LoadWorkflow(string workflowId)
        {
            var info = WorkflowInfoBussiness.GetModel(workflowId);
            if (info == null) return null;

            var workflow = new WorkFlow
            {
                WorkflowID = info.WorkflowID,
                MainID = info.MainID,
                MainType = info.MainType,
                Info = info,
                Config = (WorkFlowConfig)info.Config,
                Nodes = WorkflowNodeInfoBussiness.GetList($"WorkflowID='{workflowId}'"),
                Edges = WorkflowEdgeInfoBussiness.GetList($"WorkflowID='{workflowId}'")
            };
            return workflow;
        }

        /// <summary>
        /// BFS 反向遍历，获取指定节点的所有上游节点ID
        /// </summary>
        private HashSet<string> GetUpstreamNodeIds(WorkFlow workflow, string nodeId)
        {
            var result = new HashSet<string>();
            var queue = new Queue<string>();
            var visited = new HashSet<string>();

            // 构建反向邻接表 (target -> sources)
            var reverseAdj = new Dictionary<string, List<string>>();
            foreach (var edge in workflow.Edges)
            {
                if (!reverseAdj.ContainsKey(edge.TargetNodeId))
                    reverseAdj[edge.TargetNodeId] = new List<string>();
                reverseAdj[edge.TargetNodeId].Add(edge.SourceNodeId);
            }

            queue.Enqueue(nodeId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current)) continue;

                if (reverseAdj.TryGetValue(current, out var parents))
                {
                    foreach (var parent in parents)
                    {
                        result.Add(parent);
                        queue.Enqueue(parent);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 将上游上下文（sourceNodes 结构）格式化为 LLM 可精确引用的变量清单
        /// </summary>
        private string BuildUpstreamContextDescription(
            JObject upstreamContext, string sourceNodeId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## 可用变量清单（下游节点 inputs 可引用的全部变量）");
            sb.AppendLine();
            sb.AppendLine("以下列出了所有上游节点提供的输出变量。");
            sb.AppendLine("生成下游节点时，每个节点 `data.inputs` 中需要引用上游变量时，");
            sb.AppendLine("其 `sourceId` 字段必须严格使用下表中的 **可用 sourceId** 值。");
            sb.AppendLine("**下游节点可以直接引用任意深度的祖先节点变量，无需逐级传递。**");
            sb.AppendLine();

            if (upstreamContext == null || !upstreamContext.HasValues)
            {
                sb.AppendLine("（无上游节点上下文）");
                return sb.ToString();
            }

            // 按 depth 排序，直接父节点在前
            var sortedEntries = upstreamContext.Properties()
                .OrderBy(p => (int?)(p.Value["depth"] ?? 999))
                .ThenBy(p => p.Name)
                .ToList();

            foreach (var property in sortedEntries)
            {
                var nodeId = property.Name;
                var nodeInfo = property.Value;
                var label = nodeInfo["label"]?.ToString() ?? nodeId;
                var depth = (int?)(nodeInfo["depth"] ?? 0);
                var isDirect = (bool?)(nodeInfo["isDirect"] ?? false) ?? false;
                var depthLabel = isDirect ? $"直接上游(depth={depth})" : $"祖先节点(depth={depth})";

                sb.AppendLine($"### 节点: {nodeId} - {label} ({depthLabel})");
                sb.AppendLine($"| varname | type | txt | 可用 sourceId |");
                sb.AppendLine($"|---------|------|-----|---------------|");

                var outputs = nodeInfo["output"] as JArray;
                if (outputs != null)
                {
                    foreach (var output in outputs)
                    {
                        var varname = output["varname"]?.ToString() ?? "unknown";
                        var type = output["type"]?.ToString() ?? "string";
                        var txt = output["txt"]?.ToString() ?? output["displayText"]?.ToString() ?? "";
                        var sourceId = $"{nodeId}_{varname}";
                        sb.AppendLine($"| {varname} | {type} | {txt} | `{sourceId}` |");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("> 提示：上述变量包括源节点自身的输出和更上游祖先节点的输出。");
            sb.AppendLine("> 生成的下游节点 inputs.sourceId 必须从上表的\"可用 sourceId\"列中选取。");

            return sb.ToString();
        }

        /// <summary>
        /// 动态构建节点类型目录文本，供 LLM 理解可用节点
        /// </summary>
        private string BuildNodeTypeCatalog()
        {
            var catalog = new StringBuilder();

            var nodeDescriptions = new Dictionary<string, (string desc, string inputs, string outputs, string config, string scenarios)>
            {
                ["LargeModel"] = (
                    "调用大语言模型进行文本处理、生成、分析",
                    "prompt (string): 提示词模板，可使用 {{变量名}} 引用上游输出",
                    "results (string): 模型响应文本",
                    "model (LargeModelInfo): 模型选择; temperature (int): 温度0-100; topp (int): TopP采样0-100",
                    "文本生成、翻译、摘要、分析、改写、分类等"
                ),
                ["MainAI"] = (
                    "应用主控AI节点，调用大语言模型进行文本处理",
                    "prompt (string): 提示词模板",
                    "results (string): 模型响应文本; complete_type (string): 完成类型",
                    "model (LargeModelInfo): 模型选择; temperature (int): 温度0-100; topp (int): TopP采样0-100",
                    "应用主控流程"
                ),
                ["ImageGeneration"] = (
                    "根据文本描述生成图像",
                    "prompt (string): 图像描述提示词; imageInput (string): 参考图URL，可选",
                    "imageUrl (string): 生成的图像URL; prompt (string): 实际使用的提示词",
                    "model (LargeModelInfo): 图像模型; width (int): 宽度; height (int): 高度; quality (string): standard/hd; style (string): vivid/natural",
                    "插图生成、海报制作、视觉内容创作"
                ),
                ["VideoGeneration"] = (
                    "根据文本描述生成视频",
                    "prompt (string): 视频生成提示词; imageInput (string): 输入图像（图生视频，可选）; referenceImages (List<string>): 参考图片列表",
                    "videoUrl (string): 生成的视频URL; taskId (string): 任务ID; duration (int): 视频时长",
                    "model (LargeModelInfo): 视频模型; duration (int): 时长; size (string): 尺寸; aspectRatio (string): 宽高比; resolution (string): 分辨率",
                    "视频生成、短视频创作"
                ),
                ["KnowledgeBase"] = (
                    "从知识库中检索相关信息",
                    "prompt (string): 检索查询",
                    "results (string): 检索结果",
                    "knowledgeBase (List<KnowledgeBaseInfo>): 绑定知识库; relevance (int): 相关度阈值",
                    "知识检索、RAG问答、文档查询"
                ),
                ["Selector"] = (
                    "条件分支节点，根据上游输出选择不同路径",
                    "prompt (string): 选择依据",
                    "results (string): 选择结果",
                    "selector (List<Selector>): 条件分支定义，每个分支包含 varname/comparison/value",
                    "条件路由、多分支决策"
                ),
                ["Merge"] = (
                    "合并多个上游节点的输出",
                    "input (string): 合并输入",
                    "results (string): 合并结果",
                    "allowFailure (bool): 是否允许上游失败",
                    "多路合并、汇聚节点"
                ),
                ["End"] = (
                    "流程结束节点，输出最终结果",
                    "input (string): 最终结果内容",
                    "无（流程终点）",
                    "无特殊配置",
                    "流程终点"
                ),
                ["AgentEnd"] = (
                    "Agent流程结束节点，输出最终结果",
                    "input (string): 最终结果内容",
                    "results (string): 输出结果; agentName (String): Agent名称",
                    "无特殊配置",
                    "Agent流程终点"
                ),
                ["ClawAI"] = (
                    "高级AI智能体，具备规划-执行-反思循环能力",
                    "prompt (string): 用户任务描述",
                    "results (string): 最终答案; taskPlanning (string): 规划JSON",
                    "model/planningModel/reflectionModel: 多模型配置; taskPlanningConfig/memoryConfig等",
                    "复杂多步任务、自主规划执行"
                ),
                ["ServiceDesk"] = (
                    "客服节点，支持知识库检索增强的快速响应",
                    "prompt (string): 用户消息",
                    "response (string): 回复; confidence (string): 置信度; strategy (string): 处理策略",
                    "knowledgeBase: 知识库; personaPrompt: 人设提示词",
                    "客服对话、FAQ自动回复"
                ),
                ["Research"] = (
                    "深度研究节点，迭代搜索-抓取-分析循环",
                    "prompt (string): 研究目标",
                    "results (string): 研究结果(Markdown); sources (string): 来源JSON",
                    "MaxIterations: 最大迭代次数; MaxSearchResults: 搜索结果数; OutputFormat: 输出格式",
                    "深度调研、多源信息聚合"
                ),
                ["Voice"] = (
                    "语音转写节点，音频/视频文件转文本",
                    "prompt (string): LLM后处理提示词; audioSource (string): 音频URL",
                    "results (string): 结果; transcription (string): 转写文本; duration (string): 时长",
                    "Provider: 转写服务商; EnableSpeakerDiarization: 说话人分离",
                    "会议记录、语音转文字"
                ),
                ["Plugins"] = (
                    "插件节点，调用外部API或函数",
                    "prompt (string): 插件调用参数",
                    "results (string): 插件返回结果",
                    "plugins (PluginsInfo): 插件选择和配置",
                    "外部API调用、数据获取"
                ),
                ["Agent"] = (
                    "子工作流Agent节点，触发另一个工作流执行",
                    "prompt (string): 传递给子工作流的输入",
                    "results (string): 子工作流输出; agentName (string): Agent名称",
                    "agent (AgentInfo): Agent选择",
                    "子流程调用、模块化编排"
                ),
                ["HumanInTheLoop"] = (
                    "人机交互节点，暂停执行等待用户选择",
                    "prompt (string): 询问内容",
                    "results (string): 用户选择结果",
                    "askContent: 提问内容; options: 可选项列表",
                    "需要用户确认/选择的场景"
                ),
                ["Message"] = (
                    "消息推送节点，向指定用户发送消息",
                    "input (string): 消息内容",
                    "sendSuccess (string): 发送状态; results (string): 综合结果",
                    "MessageType: 消息类型; TargetUserConfig: 目标用户配置",
                    "通知推送、消息分发"
                ),
                ["SkillAgent"] = (
                    "技能执行节点，运行外部脚本(Python/PowerShell等)",
                    "prompt (string): 脚本输入",
                    "results (string): 脚本输出",
                    "skill (Skill): 技能选择; skillsToolsOptions: 执行配置",
                    "代码执行、数据处理"
                ),
                ["MCP"] = (
                    "MCP工具节点，调用Model Context Protocol工具",
                    "prompt (string): 工具调用参数",
                    "results (string): 工具返回结果",
                    "mcp (McpInfo): MCP服务选择; config: 连接配置",
                    "MCP工具调用"
                ),
                ["FileToMarkdown"] = (
                    "文件转Markdown节点，将文件内容转换为Markdown格式",
                    "prompt (string): 转换指令",
                    "markdownFiles (List<ConvertToMarkdownFiles>): 转换结果",
                    "prompt: 转换提示词",
                    "文档格式转换"
                ),
                ["IntentionRecognition"] = (
                    "意图识别节点，根据上游输入匹配不同的下游路由",
                    "prompt (string): 待识别文本",
                    "results (string): 识别结果",
                    "intentions (List<Intention>): 意图规则列表",
                    "意图分类、智能路由"
                ),
                ["Reporter"] = (
                    "记录员节点，负责记录和整理对话内容",
                    "input (string): 对话内容",
                    "results (string): 整理后的记录",
                    "recordslength (int): 摘要记录条数; enable (bool): 是否启用",
                    "对话记录、内容摘要"
                ),
            };

            foreach (var (type, (desc, inputs, outputs, config, scenarios)) in nodeDescriptions)
            {
                catalog.AppendLine($"### {type}");
                catalog.AppendLine($"- 功能: {desc}");
                catalog.AppendLine($"- 典型输入: {inputs}");
                catalog.AppendLine($"- 典型输出: {outputs}");
                catalog.AppendLine($"- 特殊配置: {config}");
                catalog.AppendLine($"- 适用场景: {scenarios}");
                catalog.AppendLine();
            }

            return catalog.ToString();
        }

        /// <summary>
        /// 将规划 IO 列表格式化为文本
        /// </summary>
        private string PlanIOListToText(List<PlanIO> ios)
        {
            if (ios == null || ios.Count == 0) return "（无）";
            var sb = new StringBuilder();
            foreach (var io in ios)
            {
                sb.AppendLine($"- varname: {io.Varname}");
                sb.AppendLine($"  sourceRef: {io.SourceRef}");
                sb.AppendLine($"  type: {io.Type}");
                sb.AppendLine($"  txt: {io.Txt}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 将占位符 sourceRef 替换为真实 sourceId
        /// {S}_prompt  → {sourceNodeGuid}_prompt
        /// {STEP1}_results → {nodeIdOfStep1}_results
        /// </summary>
        private string ResolveSourceRef(string text, string sourceNodeId, Dictionary<int, string> idMap)
        {
            // 替换 {S}_xxx → sourceNodeId_xxx
            text = Regex.Replace(text, @"\{S\}_(\w+)", $"{sourceNodeId}_$1");

            // 替换 {STEPn}_xxx 或 {{STEPn}}_xxx → realNodeId_xxx
            // 兼容 LLM 输出的单花括号和提示词模板的双花括号格式
            foreach (var (stepIndex, nodeId) in idMap)
            {
                // 双花括号: {{STEP2}}_results
                text = Regex.Replace(text,
                    $@"\{{{{STEP{stepIndex}\}}}}_(\w+)",
                    $"{nodeId}_$1");
                // 单花括号: {STEP2}_results
                text = Regex.Replace(text,
                    $@"\{{STEP{stepIndex}\}}_(\w+)",
                    $"{nodeId}_$1");
            }

            return text;
        }

        /// <summary>
        /// DAG 环路检测（拓扑排序），Phase 3 组装后调用
        /// </summary>
        private bool HasCycle(List<WorkflowNodeInfo> nodes, List<WorkflowEdgeInfo> edges)
        {
            var nodeIds = new HashSet<string>(nodes.Select(n => n.NodeID));
            var inDegree = nodeIds.ToDictionary(id => id, _ => 0);
            var adjacency = nodeIds.ToDictionary(id => id, _ => new List<string>());

            foreach (var edge in edges)
            {
                if (nodeIds.Contains(edge.SourceNodeId) && nodeIds.Contains(edge.TargetNodeId))
                {
                    adjacency[edge.SourceNodeId].Add(edge.TargetNodeId);
                    inDegree[edge.TargetNodeId]++;
                }
            }

            var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            int visited = 0;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                visited++;
                foreach (var neighbor in adjacency[current])
                    if (--inDegree[neighbor] == 0) queue.Enqueue(neighbor);
            }
            return visited != nodeIds.Count;
        }

        /// <summary>
        /// BFS 分层自动布局（Phase 3 调用）
        /// </summary>
        private void AutoLayoutNodes(List<WorkflowNodeInfo> nodes, List<WorkflowEdgeInfo> edges, string sourceNodeId)
        {
            const int nodeWidth = 300, horizontalGap = 80, nodeHeight = 50, verticalGap = 80;
            var levels = new Dictionary<string, int>();
            var adjacency = nodes.ToDictionary(n => n.NodeID, _ => new List<string>());
            var inDegree = nodes.ToDictionary(n => n.NodeID, _ => 0);

            foreach (var edge in edges)
            {
                if (adjacency.ContainsKey(edge.SourceNodeId))
                    adjacency[edge.SourceNodeId].Add(edge.TargetNodeId);
                if (inDegree.ContainsKey(edge.TargetNodeId))
                    inDegree[edge.TargetNodeId]++;
            }

            var queue = new Queue<string>();
            foreach (var edge in edges.Where(e => e.SourceNodeId == sourceNodeId))
            {
                if (levels.ContainsKey(edge.TargetNodeId)) continue;
                levels[edge.TargetNodeId] = 1;
                queue.Enqueue(edge.TargetNodeId);
            }
            foreach (var node in nodes)
                if (!levels.ContainsKey(node.NodeID) && inDegree.TryGetValue(node.NodeID, out var deg) && deg == 0)
                { levels[node.NodeID] = 1; queue.Enqueue(node.NodeID); }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!adjacency.TryGetValue(current, out var neighbors)) continue;
                foreach (var neighbor in neighbors)
                {
                    var nl = levels[current] + 1;
                    if (!levels.ContainsKey(neighbor) || levels[neighbor] < nl)
                    { levels[neighbor] = nl; queue.Enqueue(neighbor); }
                }
            }

            foreach (var node in nodes)
                if (!levels.ContainsKey(node.NodeID)) levels[node.NodeID] = 1;

            var buckets = levels.GroupBy(kv => kv.Value)
                .ToDictionary(g => g.Key, g => g.Select(kv => nodes.First(n => n.NodeID == kv.Key)).ToList());

            foreach (var (level, bucket) in buckets.OrderBy(kv => kv.Key))
            {
                int x = level * (nodeWidth + horizontalGap);
                int totalH = bucket.Count * nodeHeight + (bucket.Count - 1) * verticalGap;
                int startY = -totalH / 2;
                for (int i = 0; i < bucket.Count; i++)
                {
                    int y = startY + i * (nodeHeight + verticalGap);
                    if (bucket[i].Config is JObject jObj)
                        jObj["position"] = JObject.FromObject(new { x, y });
                }
            }
        }

        /// <summary>
        /// 获取默认 LLM 模型配置
        /// </summary>
        /// <summary>
        /// 多策略解析 Phase 1 规划 JSON，容错 LLM 输出格式不完美的情况
        /// </summary>
        private WorkflowPlan TryParsePlanJson(string rawText)
        {
            // 策略1：直接提取 JSON 候选并解析
            var jsonText = Utils.ExtractJsonCandidate(rawText);
            if (!string.IsNullOrEmpty(jsonText))
            {
                try { return JsonConvert.DeserializeObject<WorkflowPlan>(jsonText); }
                catch (Exception ex) { _logger.LogWarning(ex, "策略1解析规划失败"); }
            }

            // 策略2：NormalizeJsonFormat 修复常见格式问题后再解析
            var normalized = Utils.NormalizeJsonFormat(rawText);
            jsonText = Utils.ExtractJsonCandidate(normalized);
            if (!string.IsNullOrEmpty(jsonText))
            {
                try { return JsonConvert.DeserializeObject<WorkflowPlan>(jsonText); }
                catch (Exception ex) { _logger.LogWarning(ex, "策略2解析规划失败"); }
            }

            // 策略3：尝试先解析为 JToken 验证有效性，再转换
            try
            {
                jsonText = Utils.ExtractJsonCandidate(rawText);
                if (!string.IsNullOrEmpty(jsonText))
                {
                    var token = JToken.Parse(jsonText);
                    return token.ToObject<WorkflowPlan>();
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "策略3解析规划失败"); }

            // 策略4：尝试使用修复常见 JSON 错误（双花括号、尾部逗号等）
            try
            {
                var fixed1 = rawText
                    .Replace("{{", "{")
                    .Replace("}}", "}")
                    .Replace("```json", "")
                    .Replace("```", "");
                jsonText = Utils.ExtractJsonCandidate(fixed1);
                if (!string.IsNullOrEmpty(jsonText))
                {
                    return JsonConvert.DeserializeObject<WorkflowPlan>(jsonText);
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "策略4解析规划失败"); }

            return null;
        }

        /// <summary>
        /// 多策略解析 Phase 2 节点修改 JSON
        /// </summary>
        private JObject TryParseModificationsJson(string rawText, int stepIndex)
        {
            // 策略1：直接提取并解析
            var jsonText = Utils.ExtractJsonCandidate(rawText);
            if (!string.IsNullOrEmpty(jsonText))
            {
                try { return JObject.Parse(jsonText); }
                catch (Exception ex) { _logger.LogWarning(ex, $"节点{stepIndex} 策略1解析修改失败"); }
            }

            // 策略2：NormalizeJsonFormat 修复后解析
            var normalized = Utils.NormalizeJsonFormat(rawText);
            jsonText = Utils.ExtractJsonCandidate(normalized);
            if (!string.IsNullOrEmpty(jsonText))
            {
                try { return JObject.Parse(jsonText); }
                catch (Exception ex) { _logger.LogWarning(ex, $"节点{stepIndex} 策略2解析修改失败"); }
            }

            // 策略3：修复双花括号
            try
            {
                var fixed1 = rawText
                    .Replace("{{", "{")
                    .Replace("}}", "}")
                    .Replace("```json", "")
                    .Replace("```", "");
                jsonText = Utils.ExtractJsonCandidate(fixed1);
                if (!string.IsNullOrEmpty(jsonText))
                {
                    return JObject.Parse(jsonText);
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, $"节点{stepIndex} 策略3解析修改失败"); }

            return null;
        }

        /// <summary>
        /// 内置兜底规划提示词（当 md 文件未部署时使用）
        /// </summary>
        private string BuildFallbackPlanPrompt()
        {
            return @"你是一个【工作流结构规划器】。

你的任务：根据可用变量清单和用户需求，规划出需要生成的下游节点列表及连线关系。
**只输出步骤列表和连线，不要输出每个节点的详细 prompt 文本。**

核心规则：
1. 分析用户需求，拆解为可执行的步骤序列
2. 每个步骤指定 nodeType、nodeName、description、inputs、outputs
3. inputs 中的 sourceRef 使用占位符：{S}_varname 引用源节点变量，{STEPn}_varname 引用第 n 个生成节点的输出
4. 步骤从 stepIndex=1 开始（0 代表源节点）
5. 连线用 fromStepIndex → toStepIndex 表示

" + BuildNodeTypeCatalog() + @"

输出格式（严格的JSON，不要输出其他内容）：
{
  ""steps"": [
    {
      ""stepIndex"": 1,
      ""nodeType"": ""LargeModel"",
      ""nodeName"": ""节点名称"",
      ""description"": ""功能描述"",
      ""inputs"": [
        { ""varname"": ""prompt"", ""sourceRef"": ""{S}_prompt"", ""type"": ""string"", ""txt"": ""说明"" }
      ],
      ""outputs"": [
        { ""varname"": ""results"", ""type"": ""string"", ""txt"": ""说明"" }
      ]
    }
  ],
  ""edges"": [
    { ""fromStepIndex"": 0, ""toStepIndex"": 1 }
  ]
}

约束：只输出 JSON，不输出解释；步骤数 2~8 个；最后一步建议用 End；所有 sourceRef 用 {S}_xxx 或 {STEPn}_xxx 格式";
        }

        /// <summary>
        /// 智能合并数组：按 varname 匹配，LLM 输出的字段覆盖基础字段
        /// 避免 LLM 输出不完整的 output/inputs 时覆盖掉基础 JSON 中的完整字段
        /// </summary>
        private void SmartMergeArray(JArray baseArray, JArray llmArray)
        {
            foreach (var llmItem in llmArray)
            {
                if (llmItem is not JObject llmObj) continue;
                var llmVarname = llmObj["varname"]?.ToString()
                              ?? llmObj["Varname"]?.ToString();
                if (string.IsNullOrEmpty(llmVarname)) continue;

                // 在基础数组中查找同名项
                JObject matchedItem = null;
                foreach (var baseItem in baseArray)
                {
                    if (baseItem is not JObject baseObj) continue;
                    var baseVarname = baseObj["varname"]?.ToString()
                                   ?? baseObj["Varname"]?.ToString();
                    if (string.Equals(baseVarname, llmVarname, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedItem = baseObj;
                        break;
                    }
                }

                if (matchedItem != null)
                {
                    // 合并：LLM 字段覆盖基础字段（不删除基础中 LLM 未提供的字段）
                    foreach (var field in llmObj.Properties())
                    {
                        matchedItem[field.Name] = field.Value;
                    }
                }
                else
                {
                    // 无匹配项，追加新项
                    baseArray.Add(llmItem);
                }
            }
        }

        private LargeModelConfig GetDefaultModelConfig(double temperature = 0.3)
        {
            return new LargeModelConfig
            {
                Model = LargeModelInfoBussiness.GetDefaultModel(),
                Temperature = temperature,
                TopPCoefficient = 0.8
            };
        }
    }
}
