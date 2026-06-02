using ZSN.AgentBrook.MessageGateway.Models;

namespace ZSN.AgentBrook.MessageGateway.Interfaces
{
    public interface IMessageSendService
    {
        Task<SendResult> SendAsync(string channelId, SendMessageRequest request, CancellationToken ct = default);
    }
}
