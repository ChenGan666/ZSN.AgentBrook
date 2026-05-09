using System;

namespace ZSN.AI.Node
{
    /// <summary>
    /// 全局 ServiceProvider 持有者
    /// 供后台任务（如 ClawAI 异步恢复）通过 DI 获取服务实例
    /// 在应用启动时通过 Configure 方法注入
    /// </summary>
    public static class ServiceProviderHolder
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider ServiceProvider
        {
            get => _serviceProvider ?? throw new InvalidOperationException(
                "ServiceProviderHolder 未初始化，请在应用启动时调用 ServiceProviderHolder.Initialize(provider)");
        }

        /// <summary>
        /// 初始化（在 DI 容器构建完成后调用一次）
        /// </summary>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
