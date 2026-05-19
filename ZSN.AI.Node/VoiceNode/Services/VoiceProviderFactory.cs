using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZSN.AI.Node.VoiceNode.Interfaces;

namespace ZSN.AI.Node.VoiceNode.Services
{
    /// <summary>
    /// Provider 工厂实现（含熔断器）
    /// </summary>
    public class VoiceProviderFactory : IVoiceProviderFactory
    {
        private readonly IEnumerable<IVoiceTranscriptionProvider> _providers;
        private readonly IOptions<VoiceNodeOptions> _options;
        private readonly ILogger<VoiceProviderFactory> _logger;
        private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();

        public VoiceProviderFactory(
            IEnumerable<IVoiceTranscriptionProvider> providers,
            IOptions<VoiceNodeOptions> options,
            ILogger<VoiceProviderFactory> logger)
        {
            _providers = providers;
            _options = options;
            _logger = logger;
        }

        public IVoiceTranscriptionProvider GetProvider(string providerName)
        {
            var provider = _providers.FirstOrDefault(p =>
                p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                throw new ArgumentException($"未找到转写 Provider: {providerName}");

            return provider;
        }

        public async Task<IVoiceTranscriptionProvider> GetAvailableProviderAsync(
            string preferredProvider,
            CancellationToken cancellationToken = default)
        {
            var state = _circuitBreakers.GetOrAdd(preferredProvider, _ => new CircuitBreakerState());

            if (state.IsOpen)
            {
                throw new InvalidOperationException($"转写 Provider {preferredProvider} 熔断中，请稍后重试");
            }

            try
            {
                var provider = GetProvider(preferredProvider);
                if (await provider.IsHealthyAsync(cancellationToken))
                {
                    state.ConsecutiveFailures = 0;
                    return provider;
                }

                state.ConsecutiveFailures++;
                if (state.ConsecutiveFailures >= _options.Value.CircuitBreakerThreshold)
                {
                    state.OpenUntil = DateTime.UtcNow.AddSeconds(_options.Value.CircuitBreakerRecoverySeconds);
                    _logger.LogWarning("Provider {Name} 已熔断", preferredProvider);
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogWarning(ex, "Provider {Name} 异常", preferredProvider);
                state.ConsecutiveFailures++;
            }

            throw new InvalidOperationException($"转写 Provider {preferredProvider} 不可用");
        }

        public void RecordFailure(string providerName)
        {
            var state = _circuitBreakers.GetOrAdd(providerName, _ => new CircuitBreakerState());
            state.ConsecutiveFailures++;
            if (state.ConsecutiveFailures >= _options.Value.CircuitBreakerThreshold)
            {
                state.OpenUntil = DateTime.UtcNow.AddSeconds(_options.Value.CircuitBreakerRecoverySeconds);
            }
        }

        public void RecordSuccess(string providerName)
        {
            var state = _circuitBreakers.GetOrAdd(providerName, _ => new CircuitBreakerState());
            state.ConsecutiveFailures = 0;
            state.OpenUntil = null;
        }
    }

    /// <summary>
    /// 熔断器状态
    /// </summary>
    public class CircuitBreakerState
    {
        public int ConsecutiveFailures { get; set; }
        public DateTime? OpenUntil { get; set; }
        public bool IsOpen => OpenUntil.HasValue && DateTime.UtcNow < OpenUntil.Value;
    }
}
