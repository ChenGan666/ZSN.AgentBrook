using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ZSN.AI.Core.Interface;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Models;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Service.Helpers;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// 后台后处理队列接口
    /// </summary>
    public interface IBackgroundPostProcessingQueue
    {
        /// <summary>
        /// 将后处理任务加入队列
        /// </summary>
        void QueuePostProcessing(PostProcessingSnapshot snapshot);
    }

    /// <summary>
    /// 后台后处理队列服务
    /// 使用 Channel 实现线程安全的后台队列,避免 Task.Run 直接复用作用域服务
    /// </summary>
    public class BackgroundPostProcessingQueue : BackgroundService, IBackgroundPostProcessingQueue
    {
        private readonly Channel<PostProcessingSnapshot> _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundPostProcessingQueue> _logger;

        public BackgroundPostProcessingQueue(
            IServiceProvider serviceProvider,
            ILogger<BackgroundPostProcessingQueue> logger)
        {
            // 创建无界队列
            _queue = Channel.CreateUnbounded<PostProcessingSnapshot>(new UnboundedChannelOptions
            {
                SingleReader = true, // 只有一个后台线程消费
                SingleWriter = false // 可能有多个请求同时入队
            });
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// 将后处理任务加入队列
        /// </summary>
        public void QueuePostProcessing(PostProcessingSnapshot snapshot)
        {
            if (!_queue.Writer.TryWrite(snapshot))
            {
                _logger.LogError("[PostProcessingQueue] 入队失败 - SessionID: {SessionID}", snapshot.SessionID);
            }
            else
            {
                _logger.LogDebug("[PostProcessingQueue] 入队成功 - SessionID: {SessionID}", snapshot.SessionID);
            }
        }

        /// <summary>
        /// 后台服务执行方法
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[PostProcessingQueue] 后台队列服务已启动");

            await foreach (var snapshot in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    // 为每个后处理任务创建独立的作用域
                    using var scope = _serviceProvider.CreateScope();
                    
                    // 从独立作用域中获取服务实例
                    var memoryService = scope.ServiceProvider.GetRequiredService<IMemoryService>();
                    var personalityService = scope.ServiceProvider.GetRequiredService<IPersonalityService>();
                    var streamSync = scope.ServiceProvider.GetRequiredService<RedisStreamSync>();
                    
                    // 执行后处理
                    await ProcessPostProcessingAsync(snapshot, memoryService, personalityService, streamSync);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PostProcessingQueue] 后处理失败 - SessionID: {SessionID}", snapshot.SessionID);
                }
            }

            _logger.LogInformation("[PostProcessingQueue] 后台队列服务已停止");
        }

        /// <summary>
        /// 执行后处理逻辑
        /// </summary>
        private async Task ProcessPostProcessingAsync(
            PostProcessingSnapshot snapshot,
            IMemoryService memoryService,
            IPersonalityService personalityService,
            RedisStreamSync streamSync)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.EXECUTION, 
                    $"[PostProcessing] 开始后处理 - SessionID: {snapshot.SessionID}");

                // 1. 更新记忆
                await memoryService.UpdateMemoriesAsync(
                    snapshot.MemoryContext, 
                    snapshot.OriginalTask, 
                    snapshot.FinalResult, 
                    snapshot.TaskPlanning,
                    snapshot.AppID, 
                    snapshot.SessionID, 
                    snapshot.MemberID, 
                    snapshot.ClawID,
                    snapshot.EmbeddingModelConfig);
                
                var memoryUpdateMsg = "✓ 记忆已更新";
                _ = streamSync.AppendDeltaAsync(
                    snapshot.StreamKey, snapshot.SessionID, snapshot.ProcessesID, 
                    snapshot.TaskID, snapshot.NodeID, memoryUpdateMsg + "\n");

                // 2. 更新 AI 个性状态 (如果启用)
                if (snapshot.PersonalityConfig.enabled && snapshot.MemoryContext.AIState != null)
                {
                    var personalityUpdateMsg = "\n更新 AI 个性状态...";
                    _ = streamSync.AppendDeltaAsync(
                        snapshot.StreamKey, snapshot.SessionID, snapshot.ProcessesID, 
                        snapshot.TaskID, snapshot.NodeID, personalityUpdateMsg + "\n");

                    // 更新目标
                    if (snapshot.PersonalityConfig.enableGoalOriented)
                    {
                        bool taskCompleted = snapshot.TaskPlanning.PlanningStatus == ZSN.AI.Entity.ClawAI.PlanningStatus.Completed;
                        snapshot.MemoryContext.AIState = await personalityService.UpdateGoalsAsync(
                            snapshot.MemoryContext.AIState,
                            taskCompleted,
                            snapshot.OriginalTask,
                            snapshot.PersonalityConfig);
                    }

                    // 更新成功率
                    int qualityScore = snapshot.TaskPlanning.Steps.Any() 
                        ? (int)snapshot.TaskPlanning.Steps.Average(s => s.QualityScore) 
                        : 0;
                    bool taskSuccess = snapshot.TaskPlanning.PlanningStatus == ZSN.AI.Entity.ClawAI.PlanningStatus.Completed;
                    var successRate = await personalityService.UpdateSuccessRateAsync(
                        snapshot.SessionID, taskSuccess, qualityScore);

                    var personalityUpdatedMsg = $"✓ AI 个性状态已更新 (成功率: {successRate:F2}%)";
                    _ = streamSync.AppendDeltaAsync(
                        snapshot.StreamKey, snapshot.SessionID, snapshot.ProcessesID, 
                        snapshot.TaskID, snapshot.NodeID, personalityUpdatedMsg + "\n");
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.EXECUTION, 
                    $"[PostProcessing] 后处理完成 - SessionID: {snapshot.SessionID}");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.EXECUTION, 
                    $"[PostProcessing] 后处理异常 - SessionID: {snapshot.SessionID}", ex);
            }
        }
    }
}
