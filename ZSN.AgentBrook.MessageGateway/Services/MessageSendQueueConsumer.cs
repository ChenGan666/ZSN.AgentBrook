using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using ZSN.AI.BLL;
using ZSN.AgentBrook.MessageGateway.Configuration;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.MessageGateway.Services
{
    public class MessageSendQueueConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<GatewayOptions> _options;
        private readonly ILogger<MessageSendQueueConsumer> _logger;
        private readonly SemaphoreSlim _concurrencyLimiter;

        public MessageSendQueueConsumer(
            IServiceScopeFactory scopeFactory,
            IOptions<GatewayOptions> options,
            ILogger<MessageSendQueueConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
            _concurrencyLimiter = new SemaphoreSlim(options.Value.MaxConcurrentSends);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[MessageSendQueue] 消费者启动，队列: {Queue}, 并发: {Max}",
                _options.Value.SendQueueName, _options.Value.MaxConcurrentSends);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _concurrencyLimiter.WaitAsync(stoppingToken);

                    var redis = new RedisHelper();
                    var taskJson = await redis.ListRightPopAsync(_options.Value.SendQueueName);

                    if (string.IsNullOrEmpty(taskJson))
                    {
                        _concurrencyLimiter.Release();
                        await Task.Delay(200, stoppingToken);
                        continue;
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessSendTaskAsync(taskJson, stoppingToken);
                        }
                        finally
                        {
                            _concurrencyLimiter.Release();
                        }
                    }, stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _concurrencyLimiter.Release();
                    _logger.LogError(ex, "[MessageSendQueue] 消费循环异常");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private async Task ProcessSendTaskAsync(string taskJson, CancellationToken ct)
        {
            MessageSendTask task = null;
            try
            {
                task = JsonConvert.DeserializeObject<MessageSendTask>(taskJson);
                if (task == null)
                {
                    _logger.LogWarning("[MessageSendQueue] 反序列化失败: {Json}", taskJson);
                    return;
                }

                _logger.LogInformation("[MessageSendQueue] 处理任务 RecordID={RecordID}, Channel={Channel}",
                    task.RecordID, task.ChannelID);

                var request = new SendMessageRequest
                {
                    ChannelID = task.ChannelID,
                    MessageType = task.MessageType,
                    Content = task.Content,
                    TargetUser = task.TargetUser,
                    TargetName = task.TargetName,
                    ExtraParams = task.ExtraParams
                };

                using var scope = _scopeFactory.CreateScope();
                var sendService = scope.ServiceProvider.GetRequiredService<IMessageSendService>();
                var result = await sendService.SendAsync(task.ChannelID, request, ct);

                var record = MessageSendRecordBussiness.GetModel(task.RecordID);
                if (record != null)
                {
                    record.SendStatus = result.Success ? 1 : -1;
                    record.PlatformMessageId = result.PlatformMessageId ?? "";
                    record.ErrorMessage = result.ErrorMessage ?? "";
                    record.SendTime = DateTime.Now;
                    record.RetryCount = result.RetryCount;
                    MessageSendRecordBussiness.Update(record);
                }

                _logger.LogInformation("[MessageSendQueue] 任务完成 RecordID={RecordID}, Success={Success}",
                    task.RecordID, result.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MessageSendQueue] 处理失败 RecordID={RecordID}", task?.RecordID);

                if (task != null)
                {
                    try
                    {
                        var record = MessageSendRecordBussiness.GetModel(task.RecordID);
                        if (record != null)
                        {
                            record.SendStatus = -1;
                            record.ErrorMessage = ex.Message;
                            record.SendTime = DateTime.Now;
                            MessageSendRecordBussiness.Update(record);
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
