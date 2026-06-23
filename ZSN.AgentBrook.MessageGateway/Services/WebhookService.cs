using Microsoft.Extensions.Options;
using ZSN.AgentBrook.MessageGateway.Configuration;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.AI.BLL;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.MessageGateway.Services
{
    public class WebhookService : IWebhookService
    {
        private readonly IMessageProviderFactory _providerFactory;
        private readonly IMessageRouter _router;
        private readonly IOptions<GatewayOptions> _options;
        private readonly ILogger<WebhookService> _logger;

        public WebhookService(
            IMessageProviderFactory providerFactory,
            IMessageRouter router,
            IOptions<GatewayOptions> options,
            ILogger<WebhookService> logger)
        {
            _providerFactory = providerFactory;
            _router = router;
            _options = options;
            _logger = logger;
        }

        public async Task<WebhookHandleResult> HandleWebhookAsync(
            string providerType, WebhookContext context, CancellationToken ct = default)
        {
            try
            {
                if (!int.TryParse(providerType, out int ptInt))
                {
                    if (Enum.TryParse<MessageProviderType>(providerType, true, out var ptEnum))
                        ptInt = (int)ptEnum;
                    else
                        return new WebhookHandleResult { Success = false, StatusCode = 400, Message = "Unknown provider type" };
                }

                var channels = ChannelConfigBussiness.GetList(
                    $"ProviderType={ptInt} AND Enabled=1 AND (FlowDirection=2 OR FlowDirection=3)");

                if (channels == null || channels.Count == 0)
                {
                    _logger.LogWarning("[Webhook] 未找到接收渠道配置: ProviderType={Type}", providerType);
                    return new WebhookHandleResult { Success = false, Message = "No channel configured" };
                }

                ChannelConfigInfo matchedChannel = null;
                IMessageProvider matchedProvider = null;

                var provider = _providerFactory.GetProvider(ptInt);

                foreach (var channel in channels)
                {
                    if (await provider.ValidateWebhookAsync(context, channel))
                    {
                        matchedChannel = channel;
                        matchedProvider = provider;
                        break;
                    }
                }

                if (matchedChannel == null)
                {
                    _logger.LogWarning("[Webhook] 验签失败: ProviderType={Type}", providerType);
                    return new WebhookHandleResult { Success = false, StatusCode = 401, Message = "Signature validation failed" };
                }

                var msgEvent = matchedProvider.ParseWebhookEvent(context, matchedChannel);
                if (msgEvent == null)
                {
                    _logger.LogDebug("[Webhook] 非消息事件，跳过");
                    return new WebhookHandleResult { Success = true, Message = "Non-message event ignored" };
                }
                msgEvent.ChannelID = matchedChannel.ChannelID;

                var existingRecord = MessageReceiveRecordBussiness.GetByEventId(msgEvent.EventId);
                if (existingRecord != null)
                {
                    _logger.LogDebug("[Webhook] 重复事件跳过: EventId={EventId}", msgEvent.EventId);
                    return new WebhookHandleResult { Success = true, Message = "Duplicate event" };
                }

                var receiveRecord = new MessageReceiveRecordInfo
                {
                    RecordID = Guid.NewGuid().ToString(),
                    ChannelID = msgEvent.ChannelID,
                    EventId = msgEvent.EventId,
                    ProviderType = msgEvent.ProviderType,
                    FromUser = msgEvent.FromUser,
                    FromUserName = msgEvent.FromUserName,
                    MessageType = msgEvent.MessageType,
                    Content = msgEvent.Content,
                    RawPayload = msgEvent.RawData,
                    RouteStatus = 0,
                    ReceiveTime = msgEvent.ReceiveTime,
                    CreateTime = DateTime.Now
                };
                MessageReceiveRecordBussiness.Add(receiveRecord);

                if (_options.Value.EnableMessageRouting)
                {
                    var routeResult = await _router.RouteAsync(msgEvent, matchedChannel);

                    receiveRecord.RouteStatus = routeResult.Matched ? 1 : -1;
                    receiveRecord.RoutedWorkflowID = routeResult.Matched ? routeResult.MatchedRuleID : "";
                    receiveRecord.RoutedTaskID = routeResult.CreatedTaskID ?? "";
                    MessageReceiveRecordBussiness.Update(receiveRecord);

                    _logger.LogInformation("[Webhook] 路由完成: EventId={EventId}, Matched={Matched}, TaskID={TaskID}",
                        msgEvent.EventId, routeResult.Matched, routeResult.CreatedTaskID);
                }

                return new WebhookHandleResult { Success = true, Message = "OK" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Webhook] 处理异常: ProviderType={Type}", providerType);
                return new WebhookHandleResult { Success = false, Message = ex.Message };
            }
        }
    }
}
