using Microsoft.AspNetCore.Mvc;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Models;

namespace ZSN.AgentBrook.MessageGateway.Controllers
{
    [ApiController]
    [Route("api/webhook")]
    public class WebhookController : ControllerBase
    {
        private readonly IWebhookService _webhookService;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(IWebhookService webhookService, ILogger<WebhookController> logger)
        {
            _webhookService = webhookService;
            _logger = logger;
        }

        [HttpPost("{providerType}")]
        public async Task<IActionResult> Post([FromRoute] string providerType, CancellationToken ct)
        {
            var context = new WebhookContext
            {
                ProviderType = providerType,
                Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                QueryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()),
                Body = await new StreamReader(Request.Body).ReadToEndAsync(ct)
            };

            var result = await _webhookService.HandleWebhookAsync(providerType, context, ct);

            return StatusCode(result.StatusCode, result.Message);
        }

        [HttpGet("{providerType}")]
        public IActionResult Verify([FromRoute] string providerType)
        {
            var challenge = Request.Query["hub.challenge"].ToString();
            if (!string.IsNullOrEmpty(challenge))
                return Ok(challenge);

            var echostr = Request.Query["echostr"].ToString();
            if (!string.IsNullOrEmpty(echostr))
                return Ok(echostr);

            return Ok("OK");
        }
    }
}
