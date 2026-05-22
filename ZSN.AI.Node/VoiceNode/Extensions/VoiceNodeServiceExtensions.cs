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
            // 配置绑定
            services.Configure<VoiceNodeOptions>(configuration.GetSection("VoiceNodeOptions"));
            services.Configure<FunASROptions>(configuration.GetSection("FunASROptions"));

            // 核心服务
            services.AddSingleton<IVoiceProviderFactory, VoiceProviderFactory>();
            services.AddSingleton<IAudioPreprocessor, AudioPreprocessor>();

            // Provider 注册
            services.AddSingleton<IVoiceTranscriptionProvider, FunASRProvider>();

            // 执行器
            services.AddTransient<ExecutionVoice>();

            return services;
        }
    }
}
