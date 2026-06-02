using ZSN.AgentBrook.MessageGateway.Models;

namespace ZSN.AgentBrook.MessageGateway.Interfaces
{
    public interface IWebhookService
    {
        Task<WebhookHandleResult> HandleWebhookAsync(string providerType, WebhookContext context, CancellationToken ct = default);
    }

    public class WebhookHandleResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; } = 200;
        public string Message { get; set; }
    }
}
