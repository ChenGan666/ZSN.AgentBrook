using ZSN.AI.Entity;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using ZSN.AgentBrook.MessageGateway.Configuration;
using ZSN.AgentBrook.MessageGateway.Interfaces;

namespace ZSN.AgentBrook.MessageGateway.Services
{
    public class MessageProviderFactory : IMessageProviderFactory
    {
        private readonly IEnumerable<IMessageProvider> _providers;
        private readonly IOptions<GatewayOptions> _options;
        private readonly ILogger<MessageProviderFactory> _logger;
        private readonly ConcurrentDictionary<int, CircuitBreakerState> _circuitBreakers = new();

        public MessageProviderFactory(
            IEnumerable<IMessageProvider> providers,
            IOptions<GatewayOptions> options,
            ILogger<MessageProviderFactory> logger)
        {
            _providers = providers;
            _options = options;
            _logger = logger;
        }

        public IMessageProvider GetProvider(int providerType)
        {
            var provider = _providers.FirstOrDefault(p =>
            {
                if (Enum.TryParse<MessageProviderType>(p.ProviderType, out var pt))
                    return (int)pt == providerType;
                return false;
            });
            if (provider == null)
                throw new ArgumentException($"未找到消息 Provider: {providerType}");
            return provider;
        }

        public async Task<IMessageProvider> GetAvailableProviderAsync(int providerType, CancellationToken ct = default)
        {
            var state = _circuitBreakers.GetOrAdd(providerType, _ => new CircuitBreakerState());

            if (state.IsOpen)
            {
                _logger.LogWarning("[MessageProviderFactory] Provider {Type} 熔断中", providerType);
                throw new InvalidOperationException($"消息 Provider {providerType} 熔断中");
            }

            var provider = GetProvider(providerType);
            if (await provider.IsHealthyAsync(ct))
            {
                state.ConsecutiveFailures = 0;
                return provider;
            }

            state.ConsecutiveFailures++;
            if (state.ConsecutiveFailures >= _options.Value.CircuitBreakerThreshold)
            {
                state.OpenUntil = DateTime.UtcNow.AddSeconds(_options.Value.CircuitBreakerRecoverySeconds);
                _logger.LogWarning("[MessageProviderFactory] Provider {Type} 已熔断", providerType);
            }

            throw new InvalidOperationException($"消息 Provider {providerType} 不可用");
        }

        public void RecordFailure(int providerType)
        {
            var state = _circuitBreakers.GetOrAdd(providerType, _ => new CircuitBreakerState());
            state.ConsecutiveFailures++;
            if (state.ConsecutiveFailures >= _options.Value.CircuitBreakerThreshold)
                state.OpenUntil = DateTime.UtcNow.AddSeconds(_options.Value.CircuitBreakerRecoverySeconds);
        }

        public void RecordSuccess(int providerType)
        {
            var state = _circuitBreakers.GetOrAdd(providerType, _ => new CircuitBreakerState());
            state.ConsecutiveFailures = 0;
            state.OpenUntil = null;
        }

        public IEnumerable<int> GetRegisteredProviderTypes()
            => _providers.Select(p => Enum.TryParse<MessageProviderType>(p.ProviderType, out var pt) ? (int)pt : 0);
    }

    public class CircuitBreakerState
    {
        public int ConsecutiveFailures { get; set; }
        public DateTime? OpenUntil { get; set; }
        public bool IsOpen => OpenUntil.HasValue && DateTime.UtcNow < OpenUntil.Value;
    }
}
