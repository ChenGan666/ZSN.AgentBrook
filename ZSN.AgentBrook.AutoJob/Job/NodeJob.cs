using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.AutoJob
{
    /// <summary>
    /// NodeJob - 补偿扫描：任务创建时已由 TaskInfoBussiness.Add 直推 Redis 队列（事件驱动），
    /// 本 Job 仅作为兜底：扫描直推失败回退/异常恢复后处于 Waiting 状态的任务，低频入队 Redis
    /// </summary>
    public class NodeJob : JobBase, IJob
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private const int NodeExcutionErrorLogID = 308;

        /// <summary>
        /// Redis 队列 Key，NodeTaskQueueConsumer 从此队列消费（与 BLL 直推队列共用）
        /// </summary>
        public const string QUEUE_KEY = TaskInfoBussiness.NodeTaskQueueKey;

        Task IJob.Execute(IJobExecutionContext context)
        {
            return Auto();
        }

        public async Task<int> Auto()
        {
            int num = 0;
            try
            {
                List<NodeType> nodeTypes = new List<NodeType>() {
                    NodeType.Start,
                    NodeType.AgentStart,
                    NodeType.End,
                    NodeType.AgentEnd,
                    NodeType.LargeModel,
                    NodeType.Agent,
                    NodeType.Plugins,
                    NodeType.MainAI,
                    NodeType.Selector,
                    NodeType.KnowledgeBase,
                    NodeType.Merge,
                    NodeType.MCP,
                    NodeType.FileToMarkdown,
                    NodeType.HumanInTheLoop,
                    NodeType.IntentionRecognition,
                    NodeType.HumanInTheLoopInput,
                    NodeType.SkillAgent,
                    NodeType.ImageGeneration,
                    NodeType.VideoGeneration,
                    NodeType.ClawAI,
                    NodeType.ServiceDesk,
                    NodeType.Research,
                    NodeType.Voice
                };

                List<TaskInfo> tasks = null;

                // 1. 原子读取并更新状态（FOR UPDATE SKIP LOCKED + UPDATE State）
                await _semaphore.WaitAsync();
                try
                {
                    var queryTime = DateTime.Now;
                    tasks = TaskInfoBussiness.GetList(0, nodeTypes, queryTime, 1, 100);

                    if (tasks != null && tasks.Count > 0)
                    {
                        Console.WriteLine($"[NodeJob] 本轮查询到 {tasks.Count} 个任务 (QueryTime: {queryTime:HH:mm:ss.fff})");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[NodeJob-Error] GetList 异常: {e.Message}");
                    Console.WriteLine(e);
                }
                finally
                {
                    _semaphore.Release();
                }

                // 2. 写入 Redis 队列，快速返回
                if (tasks != null && tasks.Count > 0)
                {
                    try
                    {
                        var redis = new RedisHelper().GetConnectionRedisMultiplexer().GetDatabase();
                        int successCount = 0;
                        List<string> failedTaskIds = new List<string>();
                        
                        foreach (var task in tasks)
                        {
                            if (task != null)
                            {
                                try
                                {
                                    string taskJson = JsonConvert.SerializeObject(task);
                                    redis.ListLeftPush(QUEUE_KEY, taskJson);
                                    successCount++;
                                    Console.WriteLine($"[NodeJob] 任务入队 - TaskID: {task.TaskID}, Type: {task.TaskType}, FromMainTaskID: {task.FromMainTaskID}");
                                }
                                catch (Exception itemEx)
                                {
                                    // 单个任务入队失败，记录失败ID
                                    failedTaskIds.Add(task.TaskID);
                                    Console.WriteLine($"[NodeJob-Error] 单个任务入队失败 - TaskID: {task.TaskID}: {itemEx.Message}");
                                }
                            }
                        }
                        
                        Console.WriteLine($"[NodeJob] 本轮入队完成，成功: {successCount}/{tasks.Count}");
                        
                        // ✅ 修改: 如果有失败任务，回退状态到Waiting
                        if (failedTaskIds.Count > 0)
                        {
                            try
                            {
                                TaskInfoBussiness.ResetTasksToWaiting(failedTaskIds);
                                Console.WriteLine($"[NodeJob] 已回退 {failedTaskIds.Count} 个失败任务到Waiting状态");
                                DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, 
                                    $"Redis入队失败，已回退{failedTaskIds.Count}个任务: {string.Join(",", failedTaskIds)}");
                            }
                            catch (Exception resetEx)
                            {
                                Console.WriteLine($"[NodeJob-Error] 回退任务状态失败: {resetEx.Message}");
                                DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"回退失败: {resetEx.Message}");
                            }
                        }
                        
                        num = successCount;
                    }
                    catch (Exception redisEx)
                    {
                        // 整体Redis异常，回退所有任务
                        Console.WriteLine($"[NodeJob-Error] Redis异常，回退所有任务: {redisEx.Message}");
                        DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"Redis异常: {redisEx.Message}");
                        
                        try
                        {
                            List<string> allTaskIds = tasks.Select(t => t.TaskID).ToList();
                            TaskInfoBussiness.ResetTasksToWaiting(allTaskIds);
                            Console.WriteLine($"[NodeJob] 已回退所有 {allTaskIds.Count} 个任务到Waiting状态");
                        }
                        catch (Exception resetEx)
                        {
                            Console.WriteLine($"[NodeJob-Error] 回退任务状态失败: {resetEx.Message}");
                        }
                        
                        num = 0;
                    }
                }
            }
            catch (Exception e)
            {
                num = -1;
                DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, e.Message);
            }
            return await Task.FromResult(num);
        }
    }
}
