using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Node;
using ZSN.AI.Node.Claw;
using ZSN.AI.Node.ServiceDesk;
using ZSN.AI.Node.VoiceNode;
using ZSN.AI.Node.VoiceNode.Interfaces;
using Microsoft.Extensions.Options;
using ZSN.AI.Node.VoiceNode.Extensions;
using ZSN.AI.Node.MessageNode;
using StackExchange.Redis;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.AutoJob
{
    /// <summary>
    /// 节点任务队列消费者 - BackgroundService
    /// 持续从 Redis 队列消费任务并多线程执行
    /// 与 NodeJob（生产者）配合，实现任务获取与执行的解耦
    /// </summary>
    public class NodeTaskQueueConsumer : BackgroundService
    {
        private const int MAX_CONCURRENT = 20;
        private const int POLL_INTERVAL_MS = 500;
        private const int NodeExcutionLogID = 307;
        private const int NodeExcutionErrorLogID = 308;

        private readonly IServiceProvider _rootProvider;

        public NodeTaskQueueConsumer(IServiceProvider provider)
        {
            _rootProvider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("[NodeTaskConsumer] 消费者启动，监听队列: " + NodeJob.QUEUE_KEY);

            var semaphore = new SemaphoreSlim(MAX_CONCURRENT, MAX_CONCURRENT);
            var activeTasks = new List<Task>();

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    bool semaphoreAcquired = false;
                    try
                    {
                        await semaphore.WaitAsync(stoppingToken);
                        semaphoreAcquired = true;
                        
                        var redis = new RedisHelper().GetConnectionRedisMultiplexer().GetDatabase();
                        var item = redis.ListRightPop(NodeJob.QUEUE_KEY);

                        if (item.IsNullOrEmpty)
                        {
                            semaphore.Release();
                            semaphoreAcquired = false;
                            await Task.Delay(POLL_INTERVAL_MS, stoppingToken);
                            continue;
                        }

                        var taskInfo = JsonConvert.DeserializeObject<TaskInfo>(item.ToString());
                        if (taskInfo == null)
                        {
                            Console.WriteLine("[NodeTaskConsumer-Error] 反序列化任务失败，重新入队 - JSON: " + item.ToString().Substring(0, Math.Min(100, item.ToString().Length)));
                            DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"反序列化失败: {item.ToString().Substring(0, Math.Min(100, item.ToString().Length))}");
                            
                            redis.ListLeftPush(NodeJob.QUEUE_KEY, item);
                            semaphore.Release();
                            semaphoreAcquired = false;
                            continue;
                        }

                        // 使用 CancellationToken.None 确保任务已从Redis弹出后lambda必定执行
                        // 避免因stoppingToken取消导致delegate不执行、信号量永久丢失
                        var processingTask = Task.Run(async () =>
                        {
                            try
                            {
                                Console.WriteLine($"[NodeTaskConsumer] 开始处理任务 - TaskID: {taskInfo.TaskID}, Type: {taskInfo.TaskType}");
                                using var scope = _rootProvider.CreateScope();
                                await ProcessTaskAsync(taskInfo, scope.ServiceProvider);
                                Console.WriteLine($"[NodeTaskConsumer] 任务处理完成 - TaskID: {taskInfo.TaskID}, State: {taskInfo.State}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[NodeTaskConsumer-Error] 处理任务异常 - TaskID: {taskInfo.TaskID}: {ex.Message}\n{ex.StackTrace}");
                                DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"处理异常 - TaskID: {taskInfo.TaskID}: {ex.Message}");
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, CancellationToken.None);

                        semaphoreAcquired = false; // 信号量所有权已转移给Task.Run的finally

                        // 追踪活跃任务
                        lock (activeTasks)
                        {
                            activeTasks.Add(processingTask);
                        }

                        // 清理已完成的任务
                        _ = processingTask.ContinueWith(t =>
                        {
                            lock (activeTasks)
                            {
                                activeTasks.Remove(t);
                            }
                        });
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        if (semaphoreAcquired) semaphore.Release();
                        Console.WriteLine("[NodeTaskConsumer] 收到停止信号");
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (semaphoreAcquired) semaphore.Release();
                        Console.WriteLine($"[NodeTaskConsumer-Error] 消费循环异常: {ex.Message}");
                        try { await Task.Delay(1000, stoppingToken); } catch { break; }
                    }
                }
            }
            finally
            {
                // 等待所有活跃任务完成
                Console.WriteLine("[NodeTaskConsumer] 等待活跃任务完成...");
                try
                {
                    lock (activeTasks)
                    {
                        if (activeTasks.Count > 0)
                        {
                            Console.WriteLine($"[NodeTaskConsumer] 还有 {activeTasks.Count} 个任务在处理");
                            Task.WaitAll(activeTasks.ToArray(), TimeSpan.FromSeconds(30));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NodeTaskConsumer-Error] 等待任务超时: {ex.Message}");
                }

                Console.WriteLine("[NodeTaskConsumer] 消费者已停止");
            }
        }

        /// <summary>
        /// 处理单个节点任务（与原 AIWorkerAsync_Node 逻辑一致）
        /// </summary>
        private async Task ProcessTaskAsync(TaskInfo task, IServiceProvider scopeProvider)
        {
            DefaultLogService.AddOperationLog(NodeExcutionLogID, JsonConvert.SerializeObject(task));
            TaskConfig taskConfig = task.TaskConfig;
            task.Results = new Results();
            
            IChatService chatService = null;

            try
            {
                // ✅ 修改1: 将DI操作移到try内，捕获DI异常
                try
                {
                    chatService = scopeProvider.GetRequiredService<IChatService>();
                }
                catch (Exception diEx)
                {
                    // DI解析失败，设置失败状态
                    task.State = TaskState.Failure;
                    task.Results.Data = $"[DI-Error] 服务解析失败: {diEx.Message}";
                    Console.WriteLine($"[NodeTaskConsumer-Error] DI异常 - TaskID: {task.TaskID}: {diEx.Message}");
                    DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"DI异常 - TaskID: {task.TaskID}: {diEx.Message}");

                    // 更新会话状态为失败
                    UpdateSessionStatusSafe(taskConfig?.Data?.SessionID, -1);

                    // 更新任务状态并返回
                    try
                    {
                        task.UpdateTime = DateTime.Now;
                        TaskInfoBussiness.Update(task);
                    }
                    catch (Exception updateEx)
                    {
                        Console.WriteLine($"[NodeTaskConsumer-Error] Update失败 - TaskID: {task.TaskID}: {updateEx.Message}");
                    }
                    return;
                }

                // ✅ 修改2: 检查NodeConfig有效性，无效时设置失败状态
                if (taskConfig == null || taskConfig.NodeConfig == null || taskConfig.NodeConfig.data == null)
                {
                    task.State = TaskState.Failure;
                    task.Results.Data = "[Config-Error] NodeConfig为null或无效";
                    Console.WriteLine($"[NodeTaskConsumer-Error] 无效NodeConfig - TaskID: {task.TaskID}");
                    DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"无效NodeConfig - TaskID: {task.TaskID}");

                    // 更新会话状态为失败
                    UpdateSessionStatusSafe(taskConfig?.Data?.SessionID, -1);

                    try
                    {
                        task.UpdateTime = DateTime.Now;
                        TaskInfoBussiness.Update(task);
                    }
                    catch (Exception updateEx)
                    {
                        Console.WriteLine($"[NodeTaskConsumer-Error] Update失败 - TaskID: {task.TaskID}: {updateEx.Message}");
                    }
                    return;
                }

                // 正常执行分支
                TaskData taskData = taskConfig.Data;
                taskData.TaskID = task.TaskID;
                string re = "";
                NodeType nodeType = taskConfig.NodeConfig.type;

                // 更新会话状态为运行中
                UpdateSessionStatusSafe(taskData.SessionID, 1);

                var logger = scopeProvider.GetRequiredService<ILogger<Execution>>();
                Execution excutionNode = new Execution(chatService, scopeProvider, logger);

                    switch (nodeType)
                    {
                        case NodeType.Start:
                            re = await excutionNode.StartNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.AgentStart:
                            re = await excutionNode.AgentStartNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.LargeModel:
                            re = await excutionNode.LargeModelNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.End:
                            re = await excutionNode.EndNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.AgentEnd:
                            re = await excutionNode.AgentEndNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.Agent:
                            re = await excutionNode.AgentNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.Plugins:
                            re = await excutionNode.PluginsNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.KnowledgeBase:
                            re = await excutionNode.KnowledgeBaseNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.MainAI:
                            re = await excutionNode.MainAINodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.Selector:
                            re = await excutionNode.SelectorNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.Merge:
                            re = await excutionNode.MergeNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.MCP:
                            re = await excutionNode.MCPNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.FileToMarkdown:
                            re = excutionNode.FileToMarkdownNode(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.HumanInTheLoop:
                            re = await excutionNode.HumanInTheLoopNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.IntentionRecognition:
                            re = await excutionNode.IntentionRecognitionNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.HumanInTheLoopInput:
                            re = await excutionNode.HumanInTheLoopInputNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.SkillAgent:
                            re = await excutionNode.SkillAgentNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.ImageGeneration:
                            re = await excutionNode.ImageGenerationNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.VideoGeneration:
                            re = await excutionNode.VideoGenerationNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.ClawAI:
                            var clawLogger = scopeProvider.GetRequiredService<ILogger<ExecutionClaw>>();
                            var taskPlanningService = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Interfaces.ITaskPlanningService>();
                            var memoryService = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Interfaces.IMemoryService>();
                            var reflectionService = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Interfaces.IReflectionService>();
                            var agentOrchestrationService = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Interfaces.IAgentOrchestrationService>();
                            var personalityService = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Interfaces.IPersonalityService>();
                            var masterControlService = scopeProvider.GetService<ZSN.AI.Node.Claw.Interfaces.IMasterControlService>();
                            var postProcessingQueue = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Services.IBackgroundPostProcessingQueue>();

                            ExecutionClaw excutionClaw = new ZSN.AI.Node.Claw.ExecutionClaw(
                                chatService, scopeProvider, clawLogger,
                                taskPlanningService, memoryService, reflectionService, agentOrchestrationService, personalityService, masterControlService, postProcessingQueue);
                            re = await excutionClaw.ClawAINodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.ServiceDesk:
                            var sdLogger = scopeProvider.GetRequiredService<ILogger<ExecutionServiceDesk>>();
                            var sdClassifier = scopeProvider.GetRequiredService<ZSN.AI.Node.ServiceDesk.Interfaces.IRequestClassifier>();
                            var sdGenerator = scopeProvider.GetRequiredService<ZSN.AI.Node.ServiceDesk.Interfaces.IResponseGenerator>();
                            var sdStateManager = scopeProvider.GetRequiredService<ZSN.AI.Node.ServiceDesk.Interfaces.ISessionStateManager>();

                            var executionServiceDesk = new ZSN.AI.Node.ServiceDesk.ExecutionServiceDesk(
                                chatService, scopeProvider, sdLogger,
                                sdClassifier, sdGenerator, sdStateManager);
                            re = await executionServiceDesk.ServiceDeskNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.Research:
                            var researchLogger = scopeProvider.GetRequiredService<ILogger<ZSN.AI.Node.ResearchNode.ExecutionResearch>>();
                            var researchSearch = scopeProvider.GetRequiredService<ZSN.AI.Node.ResearchNode.Services.IWebSearchService>();
                            var researchFetcher = scopeProvider.GetRequiredService<ZSN.AI.Node.ResearchNode.Services.IContentFetcherService>();
                            var researchEngine = scopeProvider.GetRequiredService<ZSN.AI.Node.ResearchNode.Services.IResearchEngineService>();
                            var researchOptions = scopeProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ZSN.AI.Node.ResearchNode.ResearchNodeOptions>>();

                            var executionResearch = new ZSN.AI.Node.ResearchNode.ExecutionResearch(
                                chatService, scopeProvider, researchLogger,
                                researchSearch, researchFetcher, researchEngine, researchOptions);
                            re = await executionResearch.ResearchNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.Voice:
                            var voiceLogger = scopeProvider.GetRequiredService<ILogger<ExecutionVoice>>();
                            var voiceProviderFactory = scopeProvider.GetRequiredService<IVoiceProviderFactory>();
                            var voicePreprocessor = scopeProvider.GetRequiredService<IAudioPreprocessor>();
                            var voiceNodeOptions = scopeProvider.GetRequiredService<IOptions<ZSN.AI.Node.VoiceNode.VoiceNodeOptions>>();

                            var executionVoice = new ExecutionVoice(
                                chatService, scopeProvider, voiceLogger,
                                voiceProviderFactory, voicePreprocessor, voiceNodeOptions);
                            re = await executionVoice.VoiceNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.Message:
                            var messageLogger = scopeProvider.GetRequiredService<ILogger<ExecutionMessage>>();
                            var redis = scopeProvider.GetRequiredService<IConnectionMultiplexer>();
                            var messageNodeOptions = scopeProvider.GetRequiredService<IOptions<ZSN.AI.Node.MessageNode.MessageNodeOptions>>();

                            var executionMessage = new ExecutionMessage(
                                chatService, scopeProvider, messageLogger,
                                redis, messageNodeOptions);
                            re = await executionMessage.MessageNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                    }

                    // ✅ 修改3: 保护Results.Data，避免覆盖业务结果
                    // 检查是否已有业务结果（EndNodeAsync/AgentEndNodeAsync已设置）
                    if (task.Results.Data == null || (task.Results.Data is string && string.IsNullOrEmpty(task.Results.Data.ToString())))
                    {
                        task.Results.Data = re;
                    }
                    else
                    {
                        // 业务结果已存在，保留原值
                        Console.WriteLine($"[NodeTaskConsumer] Results.Data已有业务结果，保留原值 - TaskID: {task.TaskID}");
                    }
                    
                    task.State = TaskState.Completed;

                    // ClawAI 异步触发场景：主 ClawAI 任务提前退出（IsAsyncTriggered），
                    // 子工作流仍在运行。此时不应将 SessionStatus 置 0，否则心跳会在
                    // 子工作流执行期间看到状态反复闪烁（0→1→0...）。
                    // 标记通过 ClawAINodeAsync 的返回值传递（__ASYNC_TRIGGERED__:前缀）。
                    bool isClawAIAsyncTriggered = nodeType == NodeType.ClawAI
                        && re != null
                        && re.Contains("__ASYNC_TRIGGERED__");

                    if (!isClawAIAsyncTriggered)
                    {
                        // 更新会话状态为完成
                        UpdateSessionStatusSafe(taskConfig?.Data?.SessionID, 0);
                    }

            }
            catch (Exception ex)
            {
                task.Results.Data = ex;
                task.State = TaskState.Failure;
                DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, ex.Message);

                // 更新会话状态为失败
                UpdateSessionStatusSafe(taskConfig?.Data?.SessionID, -1);
            }

            // 更新任务状态
            try
            {
                task.UpdateTime = DateTime.Now;
                TaskInfoBussiness.Update(task);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NodeTaskConsumer-Error] Update 任务状态失败 - TaskID: {task.TaskID}, State: {task.State}, Error: {ex.Message}");
                DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"Update 任务状态失败 - TaskID: {task.TaskID}: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全更新会话状态
        /// </summary>
        private static void UpdateSessionStatusSafe(string sessionID, int status)
        {
            if (string.IsNullOrEmpty(sessionID)) return;
            try
            {
                AppChatSessionInfoBussiness.UpdateSessionStatus(sessionID, status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NodeTaskConsumer-Warning] 更新会话状态失败 - SessionID: {sessionID}, Status: {status}: {ex.Message}");
            }
        }
    }
}
