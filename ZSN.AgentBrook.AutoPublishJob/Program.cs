using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AgentBrook.AutoPublishJob.Pipeline;

namespace ZSN.AgentBrook.AutoPublishJob
{
    /// <summary>
    /// 应用工厂发布服务入口。
    /// 独立于 ZSN.AgentBrook.AutoJob，专司「把 App 编译成独立应用」，
    /// 可单独部署到打包服务器(需预装 Node/Rust/MSVC/Git)。
    /// 安装为 Windows 服务：sc create ZSN.AgentBrook.AutoPublishJob binPath= "...\ZSN.AgentBrook.AutoPublishJob.exe" start= auto
    ///
    /// 命令行工具模式(不走轮询，执行后即退出)：
    ///   dotnet run -- reset &lt;taskId&gt;       将指定任务重置为 Failed(可被后台重试)
    ///   dotnet run -- show &lt;taskId&gt;        查看任务状态
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // 命令行工具模式
            if (args != null && args.Length >= 2)
            {
                string cmd = args[0].ToLowerInvariant();
                string taskId = args[1];
                if (cmd == "reset")
                {
                    CliResetTask(taskId);
                    return;
                }
                if (cmd == "rerun")
                {
                    CliRerunTask(taskId);
                    return;
                }
                if (cmd == "show")
                {
                    CliShowTask(taskId);
                    return;
                }
                if (cmd == "setweb")
                {
                    CliSetTarget(taskId, "web");
                    return;
                }
                if (cmd == "setdesktop")
                {
                    CliSetTarget(taskId, "desktop");
                    return;
                }
            }

            var builder = Host.CreateApplicationBuilder(args);

            // 配置绑定
            builder.Services.Configure<PublishJobOptions>(builder.Configuration.GetSection("PublishJob"));
            builder.Services.Configure<BuildToolsOptions>(builder.Configuration.GetSection("BuildTools"));

            // Pipeline 组件(单例，无状态)
            builder.Services.AddSingleton<ProcessRunner>();
            builder.Services.AddSingleton<GitTemplateFetcher>();
            builder.Services.AddSingleton<AppCustomizer>();
            builder.Services.AddSingleton<BuildVerifier>();
            builder.Services.AddSingleton<ArtifactPublisher>();

            // 单任务编排器(Scoped，每任务一个 scope)
            builder.Services.AddScoped<PublishJob>();

            // 长轮询消费者(HostedService)
            builder.Services.AddHostedService<PublishHostedService>();

            // 支持以 Windows 服务方式运行
            builder.Services.AddWindowsService(opts => opts.ServiceName = "ZSN.AgentBrook.AutoPublishJob");

            var host = builder.Build();
            await host.RunAsync();
        }

        /// <summary>CLI: 重置任务为 Failed。</summary>
        private static void CliResetTask(string taskId)
        {
            var t = PublishTaskInfoBusiness.GetModel(taskId);
            if (t == null) { Console.WriteLine($"任务不存在: {taskId}"); return; }
            Console.WriteLine($"重置前: State={t.State} Progress={t.Progress} Stage={t.Stage}");
            t.State = PublishTaskState.Failed;
            t.ErrorMsg = "手动重置: 准备重新测试";
            t.Progress = 0;
            t.Stage = "";
            PublishTaskInfoBusiness.Update(t);
            Console.WriteLine($"重置后: State=Failed，可在后台点「重试」重新入队");
        }

        /// <summary>CLI: 直接把任务设为 Pending 并清空日志/进度，供运行中的服务立即拾取重新构建。</summary>
        private static void CliRerunTask(string taskId)
        {
            var t = PublishTaskInfoBusiness.GetModel(taskId);
            if (t == null) { Console.WriteLine($"任务不存在: {taskId}"); return; }
            Console.WriteLine($"重跑前: State={t.State} Progress={t.Progress} Stage={t.Stage}");
            t.State = PublishTaskState.Pending;
            t.ErrorMsg = "";
            t.Progress = 0;
            t.Stage = "";
            t.Logs = "";
            t.ArtifactPath = "";
            t.ArtifactFileCode = "";
            t.StartTime = new DateTime(2000, 1, 1);
            t.FinishTime = new DateTime(2000, 1, 1);
            t.UpdateTime = DateTime.Now;
            PublishTaskInfoBusiness.Update(t);
            Console.WriteLine($"重跑后: State=Pending，运行中的服务会在下个轮询周期拾取");
        }

        /// <summary>CLI: 显示任务状态。</summary>
        private static void CliShowTask(string taskId)
        {
            var t = PublishTaskInfoBusiness.GetModel(taskId);
            if (t == null) { Console.WriteLine($"任务不存在: {taskId}"); return; }
            Console.WriteLine($"TaskID:       {t.TaskID}");
            Console.WriteLine($"AppID:        {t.AppID}");
            Console.WriteLine($"State:        {t.State}");
            Console.WriteLine($"Progress:     {t.Progress}");
            Console.WriteLine($"Stage:        {t.Stage}");
            Console.WriteLine($"Template:     {t.TemplateName} {t.TemplateGitUrl} (ref={t.TemplateRef}, sub={t.PublishConfig?.templateSubPath})");
            Console.WriteLine($"Targets:      {t.TargetPlatforms}");
            Console.WriteLine($"ArtifactPath: {t.ArtifactPath}");
            Console.WriteLine($"Error:        {(string.IsNullOrEmpty(t.ErrorMsg) ? "(无)" : t.ErrorMsg)}");
            Console.WriteLine($"Logs(尾):     {(string.IsNullOrEmpty(t.Logs) ? "(无)" : (t.Logs.Length > 1000 ? "..." + t.Logs.Substring(t.Logs.Length - 1000) : t.Logs))}");
        }

        /// <summary>CLI: 把任务目标改为 web 或 desktop，并重置为 Pending。</summary>
        private static void CliSetTarget(string taskId, string target)
        {
            var t = PublishTaskInfoBusiness.GetModel(taskId);
            if (t == null) { Console.WriteLine($"任务不存在: {taskId}"); return; }
            Console.WriteLine($"改前: Targets={t.TargetPlatforms} State={t.State}");
            if (t.PublishConfig?.build?.targets != null)
            {
                t.PublishConfig.build.targets.Clear();
                if (target == "web") { t.PublishConfig.build.targets.Add("web"); t.TargetPlatforms = "Web"; }
                else { t.PublishConfig.build.targets.Add("nsis"); t.TargetPlatforms = "WinX64"; }
            }
            t.State = PublishTaskState.Pending;
            t.ErrorMsg = "";
            t.Progress = 0;
            t.Stage = "";
            t.Logs = "";
            t.ArtifactPath = "";
            t.ArtifactFileCode = "";
            t.StartTime = new DateTime(2000, 1, 1);
            t.FinishTime = new DateTime(2000, 1, 1);
            t.UpdateTime = DateTime.Now;
            PublishTaskInfoBusiness.Update(t);
            Console.WriteLine($"改后: Targets={t.TargetPlatforms} State=Pending(服务会拾取)");
        }
    }
}
