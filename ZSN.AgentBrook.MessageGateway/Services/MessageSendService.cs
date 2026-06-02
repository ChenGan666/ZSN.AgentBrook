using Microsoft.Extensions.Options;
using ZSN.AgentBrook.MessageGateway.Configuration;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.AI.BLL;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.MessageGateway.Services
{
    public class MessageSendService : IMessageSendService
    {
        private readonly IMessageProviderFactory _providerFactory;
        private readonly IOptions<GatewayOptions> _options;
        private readonly ILogger<MessageSendService> _logger;

        public MessageSendService(
            IMessageProviderFactory providerFactory,
            IOptions<GatewayOptions> options,
            ILogger<MessageSendService> logger)
        {
            _providerFactory = providerFactory;
            _options = options;
            _logger = logger;
        }

        public async Task<SendResult> SendAsync(string channelId, SendMessageRequest request, CancellationToken ct = default)
        {
            var channelConfig = ChannelConfigBussiness.GetModel(channelId);
            if (channelConfig == null || channelConfig.Enabled != 1)
                return new SendResult { Success = false, ErrorMessage = "渠道不存在或已禁用" };

            var provider = await _providerFactory.GetAvailableProviderAsync(channelConfig.ProviderType, ct);

            SendResult result = null;
            for (int i = 0; i <= _options.Value.RetryCount; i++)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(_options.Value.SendTimeoutSeconds));

                    result = await provider.SendAsync(request, channelConfig, cts.Token);
                    if (result.Success)
                    {
                        _providerFactory.RecordSuccess(channelConfig.ProviderType);
                        result.RetryCount = i;
                        break;
                    }

                    _logger.LogWarning("[MessageSend] 第{Attempt}次发送失败: {Error}", i + 1, result.ErrorMessage);
                }
                catch (OperationCanceledException)
                {
                    result = new SendResult { Success = false, ErrorMessage = "发送超时" };
                }
                catch (Exception ex)
                {
                    result = new SendResult { Success = false, ErrorMessage = ex.Message };
                    _logger.LogError(ex, "[MessageSend] 发送异常");
                }

                if (i < _options.Value.RetryCount)
                    await Task.Delay(_options.Value.RetryIntervalSeconds * 1000, ct);
            }

            if (result != null && !result.Success)
                _providerFactory.RecordFailure(channelConfig.ProviderType);

            if (_options.Value.EnableSendLog)
                SaveSendRecord(channelId, request, result);

            return result ?? new SendResult { Success = false, ErrorMessage = "未知错误" };
        }

        private void SaveSendRecord(string channelId, SendMessageRequest request, SendResult result)
        {
            try
            {
                MessageSendRecordBussiness.Add(new MessageSendRecordInfo
                {
                    RecordID = Guid.NewGuid().ToString(),
                    ChannelID = channelId,
                    MessageType = request.MessageType,
                    Content = request.Content,
                    TargetUser = request.TargetUser,
                    SendStatus = result.Success ? 1 : -1,
                    PlatformMessageId = result.PlatformMessageId ?? "",
                    RetryCount = result.RetryCount,
                    ErrorMessage = result.ErrorMessage ?? "",
                    SendTime = result.SendTime,
                    CreateTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MessageSend] 保存发送记录失败");
            }
        }
    }
}
