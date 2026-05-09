using Microsoft.Extensions.DependencyInjection;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Services;

namespace ZSN.AI.Node.Claw
{
    /// <summary>
    /// Claw AI 服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册 Claw AI 相关服务
        /// </summary>
        public static IServiceCollection AddClawAI(this IServiceCollection services)
        {
            // 注册核心服务
            services.AddScoped<ITaskPlanningService, TaskPlanningService>();
            services.AddScoped<IMemoryService, MemoryService>();
            services.AddScoped<IReflectionService, ReflectionService>();
            services.AddScoped<IAgentOrchestrationService, AgentOrchestrationService>();

            // P2修复: 补充缺失的服务注册
            services.AddScoped<IPersonalityService, PersonalityService>();
            services.AddScoped<IMasterControlService, MasterControlService>();
            services.AddScoped<IResultParserService, ResultParserService>();
            services.AddScoped<IKnowledgeExtractionService, KnowledgeExtractionService>();
            // 注意: IMemoryPersistenceService 和 IUserProfileService 是静态类,不需要注册

            // P1修复: 注册后台队列服务 (Singleton + HostedService)
            services.AddSingleton<IBackgroundPostProcessingQueue, BackgroundPostProcessingQueue>();
            services.AddHostedService(sp => (BackgroundPostProcessingQueue)sp.GetRequiredService<IBackgroundPostProcessingQueue>());

            // 注册主执行器
            services.AddScoped<ExecutionClaw>();

            // ❌ 移除此处的回调注册：在服务注册阶段注册会导致 ServiceProviderHolder.ServiceProvider 为 null
            // ✅ 回调应该在 Program.cs 中 ServiceProviderHolder.Initialize() 之后、host.RunAsync() 之前注册
            // 参考: ZSN.AgentBrook.AutoJob/Program.cs 第166行

            return services;
        }
    }
}
