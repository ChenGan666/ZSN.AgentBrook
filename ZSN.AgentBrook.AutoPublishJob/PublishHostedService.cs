using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.AutoPublishJob
{
    /// <summary>
    /// 长轮询消费 tb_publish_task，拾取 Pending 任务并交给 PublishJob 编排执行。
    /// 并发受 MaxConcurrency 限制(用 SemaphoreSlim 控制)。
    /// </summary>
    public class PublishHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<PublishJobOptions> _options;
        private readonly ILogger<PublishHostedService> _logger;
        private SemaphoreSlim? _concurrencyGate;

        public PublishHostedService(IServiceProvider serviceProvider, IOptions<PublishJobOptions> options, ILogger<PublishHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int concurrency = Math.Max(1, _options.Value.MaxConcurrency);
            _concurrencyGate = new SemaphoreSlim(concurrency, concurrency);
            int intervalMs = Math.Max(1, _options.Value.PullIntervalSeconds) * 1000;

            _logger.LogInformation("[PublishHostedService] 启动");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 有空闲槽位才尝试取任务，避免无谓的 DB 轮询与状态翻转
                    if (_concurrencyGate.CurrentCount > 0)
                    {
                        PublishTaskInfo? task = PublishTaskInfoBusiness.GetPending();
                        if (task != null)
                        {
                            await _concurrencyGate.WaitAsync(stoppingToken);
                            // 用 fire-and-forget 方式启动单任务执行，立即释放轮询循环
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using var scope = _serviceProvider.CreateScope();
                                    var job = scope.ServiceProvider.GetRequiredService<Pipeline.PublishJob>();
                                    await job.RunAsync(task.TaskID, stoppingToken);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "[PublishHostedService] 任务执行异常 TaskID={TaskID}", task.TaskID);
                                    try { PublishTaskInfoBusiness.MarkFailed(task.TaskID, "任务执行异常: " + ex.Message); } catch { }
                                }
                                finally
                                {
                                    _concurrencyGate.Release();
                                }
                            }, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException) { /* 正常关闭 */ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PublishHostedService] 轮询异常");
                }

                try
                {
                    await Task.Delay(intervalMs, stoppingToken);
                }
                catch (OperationCanceledException) { break; }
            }

            _logger.LogInformation("[PublishHostedService] 已停止");
        }
    }
}
