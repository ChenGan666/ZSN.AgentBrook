using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZSN.AI.Node.VoiceNode.Interfaces;
using ZSN.AI.Node.VoiceNode.Providers.FunASR;
using ZSN.AI.Node.VoiceNode.Services;

namespace ZSN.AI.Node.VoiceNode.Extensions
{
    /// <summary>
    /// VoiceNode DI 注册扩展方法
    /// </summary>
    public static class VoiceNodeServiceExtensions
    {
        public static IServiceCollection AddVoiceNodeServices(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<VoiceNodeOptions>(configuration.GetSection("VoiceNodeOptions"));
            services.Configure<FunASROptions>(configuration.GetSection("FunASROptions"));

            services.AddSingleton<IVoiceProviderFactory, VoiceProviderFactory>();
            services.AddSingleton<IAudioPreprocessor, AudioPreprocessor>();

            services.AddSingleton<IVoiceTranscriptionProvider, FunASRProvider>();

            services.AddTransient<ExecutionVoice>();

            return services;
        }
    }
}
