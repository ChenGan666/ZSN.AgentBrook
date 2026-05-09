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
    /// NodeJob - 生产者：快速获取任务并入队 Redis，不执行实际任务处理
    /// 通过 Redis 队列解耦任务获取与执行，避免 [DisallowConcurrentExecution] 导致的堵塞
    /// </summary>
    public class NodeJob : JobBase, IJob
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private const int NodeExcutionErrorLogID = 308;

        /// <summary>
        /// Redis 队列 Key，NodeTaskQueueConsumer 从此队列消费
        /// </summary>
        public const string QUEUE_KEY = "nodejob:taskqueue";

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
                    NodeType.ServiceDesk
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
                    }
                }
                catch (Exception e)
                {
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
                                }
                                catch (Exception itemEx)
                                {
                                    // 单个任务入队失败，记录失败ID
                                    failedTaskIds.Add(task.TaskID);
                                }
                            }
                        }
                        
                        
                        // ✅ 修改: 如果有失败任务，回退状态到Waiting
                        if (failedTaskIds.Count > 0)
                        {
                            try
                            {
                                TaskInfoBussiness.ResetTasksToWaiting(failedTaskIds);
                                DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, 
                                    $"Redis入队失败，已回退{failedTaskIds.Count}个任务: {string.Join(",", failedTaskIds)}");
                            }
                            catch (Exception resetEx)
                            {
                                DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"回退失败: {resetEx.Message}");
                            }
                        }
                        
                        num = successCount;
                    }
                    catch (Exception redisEx)
                    {
                        // 整体Redis异常，回退所有任务
                        DefaultLogService.AddOperationLog(NodeExcutionErrorLogID, $"Redis异常: {redisEx.Message}");
                        
                        try
                        {
                            List<string> allTaskIds = tasks.Select(t => t.TaskID).ToList();
                            TaskInfoBussiness.ResetTasksToWaiting(allTaskIds);
                        }
                        catch (Exception resetEx)
                        {
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
