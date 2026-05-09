using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Node.Utils;
using ZSN.AI.Node.Claw.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Node.Claw.Pipeline
{
    /// <summary>
    /// 问候语快速路径处理器 - 为问候语提供超快速响应，跳过规划阶段
    /// </summary>
    public class GreetingFastPath
    {
        private readonly IChatService _chatService;
        private readonly ILogger _logger;

        public GreetingFastPath(IChatService chatService, ILogger logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// 检测是否应该使用快速路径
        /// </summary>
        public bool ShouldUseFastPath(string originalTask)
        {
            return GreetingDetector.IsSimpleGreeting(originalTask);
        }

        /// <summary>
        /// 执行问候语快速路径
        /// </summary>
        public async Task<FastPathResult> ExecuteAsync(
            string originalTask,
            string appId,
            string sessionId,
            string processesId,
            string taskId,
            string fromMainTaskId,
            string recordId,
            NodeConfig config,
            LargeModelConfig mainModelConfig,
            IProgress<string> progress)
        {
            var result = new FastPathResult
            {
                Logs = new List<string>(),
                Outputs = new List<Output>(),
                ShouldUseFastPath = true
            };

            var greetingType = GreetingDetector.GetGreetingType(originalTask);
            LoggerHelper.LogInfo(_logger, ClawLogModules.GREETING_FAST_PATH, $"检测到简单问候语({greetingType}),启用超快速路径");

            result.Logs.Add("\n⚡ 检测到问候语,启用超快速响应");

            // 直接使用主模型快速响应,不创建规划
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(originalTask);

            var responseBuilder = new StringBuilder();
            await foreach (var chunk in _chatService.SendChatAsync(
                mainModelConfig,
                chatHistory,
                Function: null,
                responseFormat: "text",
                enableStreamingObservation: true,
                progress: progress,
                ct: System.Threading.CancellationToken.None))
            {
                responseBuilder.Append(chunk);
            }

            string greetingResult = responseBuilder.ToString();
            result.Response = greetingResult;

            // ChatHistory 由 EndNodeAsync 统一保存，此处不再重复写入

            // 创建输出
            result.Outputs.Add(new Output
            {
                varname = "results",
                value = greetingResult,
                nodeId = config.id,
                sourceId = $"{config.id}_results"
            });

            // 添加统计信息输出
            result.Outputs.Add(new Output
            {
                varname = "totalSteps",
                value = "0",
                nodeId = config.id,
                sourceId = $"{config.id}_totalSteps"
            });

            result.Outputs.Add(new Output
            {
                varname = "completedSteps",
                value = "0",
                nodeId = config.id,
                sourceId = $"{config.id}_completedSteps"
            });

            result.Outputs.Add(new Output
            {
                varname = "iterations",
                value = "0",
                nodeId = config.id,
                sourceId = $"{config.id}_iterations"
            });

            result.Outputs.Add(new Output
            {
                varname = "planningStatus",
                value = "FastPath",
                nodeId = config.id,
                sourceId = $"{config.id}_planningStatus"
            });

            result.Logs.Add($"✓ 问候语快速响应完成: {greetingResult}");

            // 触发下游节点
            await TriggerDownstreamNodesAsync(config, result.Outputs, appId, sessionId, processesId, taskId, fromMainTaskId, result.Logs);

            result.Logs.Add("=== 超快速路径执行完成 ===");

            return result;
        }

        /// <summary>
        /// 触发下游节点
        /// </summary>
        private async Task TriggerDownstreamNodesAsync(
            NodeConfig config,
            List<Output> outputs,
            string appId,
            string sessionId,
            string processesId,
            string taskId,
            string fromMainTaskId,
            List<string> logs)
        {
            List<WorkflowEdgeInfo> greetingEdgeList = WorkflowEdgeInfoBussiness.GetListBySourceNodeId(config.id);
            if (greetingEdgeList != null && greetingEdgeList.Count > 0)
            {
                List<string> TargetNodeId = new();

                foreach (var edge in greetingEdgeList)
                {
                    var cfg = edge.Config as Newtonsoft.Json.Linq.JObject ?? Newtonsoft.Json.Linq.JObject.FromObject(edge.Config);
                    if ((string?)cfg["sourceHandle"] == "output_to_next")
                    {
                        TargetNodeId.Add(edge.TargetNodeId);
                    }
                }

                if (TargetNodeId.Count > 0)
                {
                    List<WorkflowNodeInfo> targetNodeList = WorkflowNodeInfoBussiness.GetListByNodeID(
                        string.Join(",", TargetNodeId.Select(id => $"'{id}'")));

                    if (targetNodeList != null)
                    {
                        foreach (var node in targetNodeList)
                        {
                            NodeConfig targetNode = new NodeConfig()
                            {
                                id = node.NodeID,
                                mainid = config.mainid,
                                workflowid = node.WorkflowID,
                                type = node.NodeType,
                                data = node.Config
                            };

                            string newTaskID = TaskInfoBussiness.toTask(
                                config, outputs, targetNode, appId, sessionId,
                                processesId, taskId, fromMainTaskId, ""
                            );

                            logs.Add($"触发下游节点: {newTaskID}");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 快速路径执行结果
    /// </summary>
    public class FastPathResult
    {
        public bool ShouldUseFastPath { get; set; }
        public string Response { get; set; }
        public List<Output> Outputs { get; set; }
        public List<string> Logs { get; set; }
    }
}
