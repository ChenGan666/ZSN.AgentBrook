using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.AutoJob
{
    /// <summary>
    /// ClawAI 异步步骤超时保护 Job
    /// 定期扫描超时的 ClawAIWorkflowStep 任务：
    /// 1. 标记超时失败
    /// 2. 处理并行层 DECR（如有计数器）
    /// 3. 触发恢复逻辑，让上层 ClawAI 继续执行
    /// </summary>
    [DisallowConcurrentExecution]
    public class ClawAIStepTimeoutJob : JobBase, IJob
    {
        Task IJob.Execute(IJobExecutionContext context)
        {
            return Auto();
        }

        public async Task Auto()
        {
            try
            {
                await CheckTimeoutStepsAsync();
            }
            catch (Exception e)
            {
                DefaultLogService.AddOperationLog(ErrorId, $"ClawAIStepTimeoutJob 异常: {e.Message}");
            }
        }

        /// <summary>
        /// 扫描并处理超时的异步等待步骤
        /// </summary>
        private async Task CheckTimeoutStepsAsync()
        {
            // 使用只读查询，避免 GetList 批量将 Waiting 改为 Processing 导致下轮查不到
            // 同时查询 Waiting(0) 和 Processing(1) 状态，覆盖被 NodeJob 或上轮超时Job改过状态的任务
            string strWhere = $" TaskType={((int)NodeType.ClawAIWorkflowStep)} " +
                              $"AND State IN (0, 1) " +
                              $"ORDER BY CreateTime ASC";
            var waitingTasks = TaskInfoBussiness.GetList(strWhere);

            if (waitingTasks == null || waitingTasks.Count == 0) return;

            int timeoutCount = 0;

            foreach (var taskInfo in waitingTasks)
            {
                try
                {
                    // 解析步骤上下文
                    var context = Newtonsoft.Json.JsonConvert.DeserializeObject<ClawAIStepContext>(
                        Newtonsoft.Json.JsonConvert.SerializeObject(taskInfo.TaskConfig.NotNodeConfig));
                    if (context == null) continue;

                    int maxWaitMinutes = context.MaxAsyncWaitMinutes > 0 ? context.MaxAsyncWaitMinutes : 120;
                    var elapsed = (DateTime.Now - taskInfo.CreateTime).TotalMinutes;

                    if (elapsed > maxWaitMinutes)
                    {
                        string timeoutMsg = $"[超时] 异步步骤等待超过 {maxWaitMinutes} 分钟，" +
                                            $"子 WorkFlow 未完成回调。StepID: {context.TriggeredStepId}";

                        timeoutCount++;

                        // 将超时结果写入 Redis，以便并行汇聚时能读取
                        var redis = new RedisHelper().GetConnectionRedisMultiplexer().GetDatabase();
                        string resultKey = $"clawai:result:{context.ProcessesID}:{context.TriggeredStepId}";
                        string failedResult = $"__FAILED__:{timeoutMsg}";
                        redis.StringSet(resultKey, failedResult, TimeSpan.FromHours(2));

                        // 注意：不在恢复前将 TaskInfo 标记为 Failure，
                        // 因为 ContinueFromStepAsync 会检查 State==Failure 而拒绝恢复。
                        // 状态将在 ContinueFromStepAsync 内部处理完成后再更新。

                        // 检查是否存在并行层计数器
                        string layerCounterKey = $"clawai:layer:{context.ProcessesID}:{context.CurrentLayerIndex}";
                        var counterExists = redis.KeyExists(layerCounterKey);

                        if (!counterExists)
                        {
                            // ===== 串行步骤（无计数器）→ 直接触发超时恢复 =====
                            DefaultLogService.AddOperationLog(ErrorId,
                                $"[ClawAI-Timeout] 串行步骤超时，触发恢复 - TaskID: {taskInfo.TaskID}");

                            await WorkflowNodeInfoBussiness.FireAndResumeClawAI(
                                taskInfo.TaskID,
                                new Dictionary<string, string>
                                {
                                    { context.TriggeredStepId, failedResult }
                                },
                                new List<string> { timeoutMsg });
                        }
                        else
                        {
                            // ===== 并行步骤 → DECR 计数器 =====
                            long remaining = redis.StringDecrement(layerCounterKey);
                            DefaultLogService.AddOperationLog(ErrorId,
                                $"[ClawAI-Timeout] 并行步骤超时 - DECR {layerCounterKey}: remaining={remaining}");

                            if (remaining <= 0)
                            {
                                // 本层全部完成（含超时），触发汇聚恢复
                                DefaultLogService.AddOperationLog(ErrorId,
                                    $"[ClawAI-Timeout] 并行层全部完成（含超时），触发汇聚恢复");

                                await TriggerParallelRecoveryAsync(context, redis, taskInfo.TaskID);
                            }
                        }

                        DefaultLogService.AddOperationLog(ErrorId,
                            $"[ClawAI-Timeout] 步骤超时 - TaskID: {taskInfo.TaskID}, " +
                            $"StepID: {context.TriggeredStepId}, 等待时间: {elapsed:F1}分钟");
                    }
                }
                catch (Exception ex)
                {
                    DefaultLogService.AddOperationLog(ErrorId,
                        $"[ClawAI-Timeout] 处理超时检查异常 - TaskID: {taskInfo.TaskID}: {ex.Message}");
                }
            }

            if (timeoutCount > 0)
            {
                DefaultLogService.AddOperationLog(ErrorId,
                    $"[ClawAI-Timeout] 本轮扫描: {waitingTasks.Count} 个等待任务, {timeoutCount} 个超时");
            }
        }

        /// <summary>
        /// 触发并行层汇聚恢复（超时场景）
        /// </summary>
        private async Task TriggerParallelRecoveryAsync(ClawAIStepContext context, StackExchange.Redis.IDatabase redis, string taskID)
        {
            try
            {
                // 获取分布式锁
                string lockKey = $"clawai:lock:{context.ProcessesID}";
                string lockValue = Guid.NewGuid().ToString();
                var distributedLock = new DistributedLock();

                if (!distributedLock.TryAcquire(lockKey, lockValue, TimeSpan.FromMinutes(5)))
                {
                    DefaultLogService.AddOperationLog(ErrorId,
                        $"[ClawAI-Timeout] 获取恢复锁失败，可能已有其他线程在恢复");
                    return;
                }

                try
                {
                    // 从 Redis 读取层级上下文
                    var layerCtxJson = redis.StringGet($"clawai:ctx:{context.ProcessesID}:{context.CurrentLayerIndex}");
                    if (layerCtxJson.IsNullOrEmpty)
                    {
                        distributedLock.Release(lockKey, lockValue);
                        return;
                    }

                    var layerCtx = Newtonsoft.Json.JsonConvert.DeserializeObject<ClawAILayerContext>(
                        layerCtxJson.ToString());

                    // 收集所有并行步骤结果（含超时的 __FAILED__）
                    var allResults = new Dictionary<string, string>();
                    foreach (var stepId in layerCtx.StepIds)
                    {
                        string stepResultKey = $"clawai:result:{context.ProcessesID}:{stepId}";
                        var stepResult = redis.StringGet(stepResultKey);
                        allResults[stepId] = stepResult.IsNullOrEmpty
                            ? "__FAILED__:步骤结果缺失（可能超时未写入）"
                            : stepResult.ToString();
                    }

                    try
                    {
                        string mergedResult = string.Join("\n\n",
                            allResults.Values.Where(v => !string.IsNullOrEmpty(v)));

                        await ClawAIResumeCallback.ResumeAsync(taskID, mergedResult, allResults);
                    }
                    catch (Exception ex)
                    {
                        DefaultLogService.AddOperationLog(ErrorId,
                            $"[ClawAI-Timeout] 超时汇聚恢复执行失败: {ex.Message}");
                        var task = TaskInfoBussiness.GetModel(taskID);
                        if (task != null)
                        {
                            task.State = TaskState.Failure;
                            task.Results = new Results { Data = ex.Message };
                            TaskInfoBussiness.Update(task);
                        }
                    }
                    finally
                    {
                        // 释放锁 + 清理 Redis 临时数据
                        distributedLock.Release(lockKey, lockValue);
                        foreach (var stepId in layerCtx.StepIds)
                        {
                            redis.KeyDelete($"clawai:result:{context.ProcessesID}:{stepId}");
                        }
                        redis.KeyDelete($"clawai:layer:{context.ProcessesID}:{context.CurrentLayerIndex}");
                        redis.KeyDelete($"clawai:ctx:{context.ProcessesID}:{context.CurrentLayerIndex}");
                    }
                }
                catch (Exception ex)
                {
                    distributedLock.Release(lockKey, lockValue);
                    DefaultLogService.AddOperationLog(ErrorId,
                        $"[ClawAI-Timeout] 并行汇聚异常: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                DefaultLogService.AddOperationLog(ErrorId,
                    $"[ClawAI-Timeout] 触发并行恢复异常: {ex.Message}");
            }
        }
    }
}
