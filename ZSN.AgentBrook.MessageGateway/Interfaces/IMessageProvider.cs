using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.MessageGateway.Interfaces
{
    public interface IMessageProvider
    {
        string ProviderType { get; }

        Task<SendResult> SendAsync(SendMessageRequest request, ChannelConfigInfo channelConfig, CancellationToken ct = default);

        Task<bool> ValidateWebhookAsync(WebhookContext context, ChannelConfigInfo channelConfig);

        ReceiveMessageEvent ParseWebhookEvent(WebhookContext context, ChannelConfigInfo channelConfig);

        Task<bool> IsHealthyAsync(CancellationToken ct = default);
    }
}
