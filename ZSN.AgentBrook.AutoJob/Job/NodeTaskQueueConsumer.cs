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
using ZSN.AI.Node.ResearchNode;
using ZSN.AI.Node.ResearchNode.Services;
using ZSN.AI.Node;
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
                                using var scope = _rootProvider.CreateScope();
                                await ProcessTaskAsync(taskInfo, scope.ServiceProvider);
                            }
                            catch (Exception ex)
                            {
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
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (semaphoreAcquired) semaphore.Release();
                        try { await Task.Delay(1000, stoppingToken); } catch { break; }
                    }
                }
            }
            finally
            {
                // 等待所有活跃任务完成
                try
                {
                    lock (activeTasks)
                    {
                        if (activeTasks.Count > 0)
                        {
                            Task.WaitAll(activeTasks.ToArray(), TimeSpan.FromSeconds(30));
                        }
                    }
                }
                catch (Exception ex)
                {
                }

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
                    DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"DI异常 - TaskID: {task.TaskID}: {diEx.Message}");
                    
                    // 更新任务状态并返回
                    try
                    {
                        task.UpdateTime = DateTime.Now;
                        TaskInfoBussiness.Update(task);
                    }
                    catch (Exception updateEx)
                    {
                    }
                    return;
                }

                // ✅ 修改2: 检查NodeConfig有效性，无效时设置失败状态
                if (taskConfig == null || taskConfig.NodeConfig == null || taskConfig.NodeConfig.data == null)
                {
                    task.State = TaskState.Failure;
                    task.Results.Data = "[Config-Error] NodeConfig为null或无效";
                    DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"无效NodeConfig - TaskID: {task.TaskID}");
                    
                    try
                    {
                        task.UpdateTime = DateTime.Now;
                        TaskInfoBussiness.Update(task);
                    }
                    catch (Exception updateEx)
                    {
                    }
                    return;
                }

                // 正常执行分支
                TaskData taskData = taskConfig.Data;
                taskData.TaskID = task.TaskID;
                string re = "";
                NodeType nodeType = taskConfig.NodeConfig.type;

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
                            var clawLogger = scopeProvider.GetRequiredService<ILogger<ZSN.AI.Node.ExecutionClaw>>();
                            var taskPlanningService = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Interfaces.ITaskPlanningService>();
                            var memoryService = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Interfaces.IMemoryService>();
                            var reflectionService = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Interfaces.IReflectionService>();
                            var agentOrchestrationService = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Interfaces.IAgentOrchestrationService>();
                            var personalityService = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Interfaces.IPersonalityService>();
                            var masterControlService = scopeProvider.GetService<ZSN.AI.Node.Claw.Interfaces.IMasterControlService>();
                            var postProcessingQueue = scopeProvider.GetRequiredService<ZSN.AI.Node.Claw.Services.IBackgroundPostProcessingQueue>();

                            ZSN.AI.Node.ExecutionClaw excutionClaw = new ZSN.AI.Node.ExecutionClaw(
                                chatService, scopeProvider, clawLogger,
                                taskPlanningService, memoryService, reflectionService, agentOrchestrationService, personalityService, masterControlService, postProcessingQueue);
                            re = await excutionClaw.ClawAINodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.ServiceDesk:
                            var sdLogger = scopeProvider.GetRequiredService<ILogger<ZSN.AI.Node.ExecutionServiceDesk>>();
                            var sdClassifier = scopeProvider.GetRequiredService<ZSN.AI.Node.ServiceDesk.Interfaces.IRequestClassifier>();
                            var sdGenerator = scopeProvider.GetRequiredService<ZSN.AI.Node.ServiceDesk.Interfaces.IResponseGenerator>();
                            var sdStateManager = scopeProvider.GetRequiredService<ZSN.AI.Node.ServiceDesk.Interfaces.ISessionStateManager>();

                            var executionServiceDesk = new ZSN.AI.Node.ExecutionServiceDesk(
                                chatService, scopeProvider, sdLogger,
                                sdClassifier, sdGenerator, sdStateManager);
                            re = await executionServiceDesk.ServiceDeskNodeAsync(taskConfig.NodeConfig, taskData);
                            break;
                        case NodeType.Research:
                            var researchLogger = scopeProvider.GetRequiredService<ILogger<ExecutionResearch>>();
                            var searchService = scopeProvider.GetRequiredService<IWebSearchService>();
                            var fetcherService = scopeProvider.GetRequiredService<IContentFetcherService>();
                            var engineService = scopeProvider.GetRequiredService<IResearchEngineService>();
                            var researchOptions = scopeProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResearchNodeOptions>>();

                            var executionResearch = new ExecutionResearch(
                                chatService, scopeProvider, researchLogger,
                                searchService, fetcherService, engineService, researchOptions);
                            re = await executionResearch.ResearchNodeAsync(taskConfig.NodeConfig, taskData);
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
                    }
                    
                    task.State = TaskState.Completed;
                
            }
            catch (Exception ex)
            {
                task.Results.Data = ex;
                task.State = TaskState.Failure;
                DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, ex.Message);
            }

            // 更新任务状态
            try
            {
                task.UpdateTime = DateTime.Now;
                TaskInfoBussiness.Update(task);
            }
            catch (Exception ex)
            {
                DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"Update 任务状态失败 - TaskID: {task.TaskID}: {ex.Message}");
            }
        }
    }
}
