using Microsoft.Extensions.DependencyInjection;
using ZSN.AI.Node.ServiceDesk.Interfaces;
using ZSN.AI.Node.ServiceDesk.Services;

namespace ZSN.AI.Node.ServiceDesk
{
    /// <summary>
    /// ServiceDesk 服务注册扩展
    /// </summary>
    public static class ServiceDeskServiceCollectionExtensions
    {
        /// <summary>
        /// 注册 ServiceDesk 相关服务
        /// </summary>
        public static IServiceCollection AddServiceDesk(this IServiceCollection services)
        {
            services.AddScoped<IRequestClassifier, RequestClassifier>();
            services.AddScoped<IResponseGenerator, ResponseGenerator>();
            services.AddScoped<ISessionStateManager, SessionStateManager>();
            services.AddScoped<ExecutionServiceDesk>();

            return services;
        }
    }
}
