using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AgentBrook.AutoJob
{
    /// <summary>
    /// 支持 Scoped 服务的 JobFactory
    /// 每次执行 Job 时创建一个 Scope，确保能正确解析 Scoped 服务
    /// </summary>
    public class JobFactory: IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public JobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            // 创建一个 scope 来支持 Scoped 服务
            var scope = _serviceProvider.CreateScope();

            // 从 scope 中解析 Job
            var job = (IJob)scope.ServiceProvider.GetRequiredService(bundle.JobDetail.JobType);

            // 包装 job 以在 dispose 时释放 scope
            return new ScopedJobWrapper(job, scope);
        }

        public void ReturnJob(IJob job)
        {
            // ScopedJobWrapper 会自动处理 scope 的释放
            if (job is ScopedJobWrapper wrapper)
            {
                wrapper.Dispose();
            }
            else if (job is IDisposable disposableJob)
            {
                disposableJob.Dispose();
            }
        }

        /// <summary>
        /// 包装 Job 并管理 Scope 的生命周期
        /// </summary>
        private class ScopedJobWrapper : IJob, IDisposable
        {
            private readonly IJob _innerJob;
            private readonly IServiceScope _scope;
            private bool _disposed;

            public ScopedJobWrapper(IJob innerJob, IServiceScope scope)
            {
                _innerJob = innerJob;
                _scope = scope;
            }

            public Task Execute(IJobExecutionContext context)
            {
                return _innerJob.Execute(context);
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _scope?.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}
