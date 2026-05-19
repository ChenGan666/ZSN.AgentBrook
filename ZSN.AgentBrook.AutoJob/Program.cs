using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;
using ZSN.AgentBrook.AutoJob;
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Repositories;
using ZSN.AI.Core.Service;
using ZSN.AI.Core.Services;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.KnowledgeBase.Interface;
using ZSN.AI.KnowledgeBase.Repositories;
using ZSN.AI.KnowledgeBase.Services;
using ZSN.AI.Node.Claw;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.Node;
using ZSN.AI.Node.ResearchNode;
using ZSN.AI.Node.ResearchNode.Services;
using ZSN.AI.Node.VoiceNode;
using ZSN.AI.Node.VoiceNode.Interfaces;
using ZSN.AI.Node.VoiceNode.Providers.FunASR;
using ZSN.AI.Node.VoiceNode.Services;
using ZSN.AI.Service.Base;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.AutoJob
{
    //xx.exe install
    //xx.exe uninstall
    public class Program
    {
        public static void Main(string[] args)
        {
            var rc = HostFactory.Run(x =>
            {
                x.Service<MainService>(s =>
                {
                    s.ConstructUsing(name => new MainService(args));
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("ZSN.AgentBrook.Job");
                x.SetDisplayName("ZSN.AgentBrook.Job");
                x.SetServiceName("ZSN.AgentBrook.Job");
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;

        }
    }
    public class MainService
    {
        private string[] args;
        public MainService(string[] vs)
        {
            args = vs;
        }
        public  void Start()
        {
            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // 注册自定义 JobFactory
                services.AddSingleton<IJobFactory, JobFactory>();

                // 注册 Quartz 调度器
                services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
                services.AddSingleton(provider =>
                {
                    var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
                    var scheduler = schedulerFactory.GetScheduler().Result;

                    // 使用自定义 JobFactory
                    scheduler.JobFactory = provider.GetRequiredService<IJobFactory>();
                    return scheduler;
                });

                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(
                        builder =>
                        {
                            builder.AllowAnyOrigin()
                                   .AllowAnyMethod()
                                   .AllowAnyHeader();
                        });
                });

                // 从程序集加载类型并注册到容器
                services.AddServicesFromAssemblies("ZSN.AI.Core");
                services.AddServicesFromAssemblies("ZSN.AI.Node");

                services.AddServicesFromAssemblies("ZSN.AI.Plugins");
                services.AddServicesFromAssemblies("ZSN.AI.Functions");

                // 注册操作日志服务
                services.AddScoped<ZSN.AI.BLL.IOperationLogService, ZSN.AI.Service.WebHelpers.OperationLogService>();


                services.AddServicesFromAssemblies("ZSN.AgentBrook.Plugins");

                services.AddServicesFromAssemblies("ZSN.AgentBrook.AutoJob");

                services.AddTransient<AIDispatcher>();
                services.AddTransient<TimeTrigger>();
                services.AddTransient<FileChunkJob>();
                services.AddTransient<NodeJob>();
                services.AddTransient<SessionTopicJob>();
                services.AddTransient<MarkdownJob>();
                services.AddTransient<CleanerJob>();
                services.AddTransient<FileToKnowledgeBaseJob>();
                services.AddTransient<MemoryConsolidationJob>();
                services.AddTransient<ClawAIStepTimeoutJob>();

                // 配置 Claw AI 选项
                services.Configure<ClawAIOptions>(context.Configuration.GetSection("ClawAI"));

                // 注册 Claw AI 服务
                services.AddScoped<ZSN.AI.Node.Claw.Interfaces.ITaskPlanningService, ZSN.AI.Node.Claw.Services.TaskPlanningService>();
                services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IMemoryService, ZSN.AI.Node.Claw.Services.MemoryService>();
                services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IReflectionService, ZSN.AI.Node.Claw.Services.ReflectionService>();
                services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IAgentOrchestrationService, ZSN.AI.Node.Claw.Services.AgentOrchestrationService>();
                services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IPersonalityService, ZSN.AI.Node.Claw.Services.PersonalityService>();
                services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IResultParserService, ZSN.AI.Node.Claw.Services.ResultParserService>();
                services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IKnowledgeExtractionService, ZSN.AI.Node.Claw.Services.KnowledgeExtractionService>();
                services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IMasterControlService, ZSN.AI.Node.Claw.Services.MasterControlService>();
                
                // P1修复: 注册后台队列服务 (Singleton + HostedService)
                services.AddSingleton<ZSN.AI.Node.Claw.Services.IBackgroundPostProcessingQueue, ZSN.AI.Node.Claw.Services.BackgroundPostProcessingQueue>();
                services.AddHostedService(sp => (ZSN.AI.Node.Claw.Services.BackgroundPostProcessingQueue)sp.GetRequiredService<ZSN.AI.Node.Claw.Services.IBackgroundPostProcessingQueue>());
                
                services.AddScoped<ZSN.AI.Node.ExecutionClaw>();

                // ServiceDesk 服务注册
                services.AddScoped<ZSN.AI.Node.ServiceDesk.Interfaces.IRequestClassifier, ZSN.AI.Node.ServiceDesk.Services.RequestClassifier>();
                services.AddScoped<ZSN.AI.Node.ServiceDesk.Interfaces.IResponseGenerator, ZSN.AI.Node.ServiceDesk.Services.ResponseGenerator>();
                services.AddScoped<ZSN.AI.Node.ServiceDesk.Interfaces.ISessionStateManager, ZSN.AI.Node.ServiceDesk.Services.SessionStateManager>();
                services.AddScoped<ZSN.AI.Node.ExecutionServiceDesk>();

                // 注册记忆整理服务
                services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IMemoryConsolidationService, ZSN.AI.Node.Claw.Services.MemoryConsolidationService>();
                services.AddScoped<ZSN.AI.Node.Services.KnowledgeGraphLLMService>();

                // 知识库服务使用 Scoped，因为依赖 IChatService（Scoped）
                services.AddScoped<ISemanticChunkerService, SemanticChunkerService>();
                services.AddScoped<IGraphRepository, AgeGraphRepository>();
                services.AddScoped<IKnowledgeGraphService, KnowledgeGraphService>();
                services.AddScoped<IHybridSearchService, HybridSearchService>();
                services.AddScoped<IEmbeddingService, EmbeddingService>();
                services.AddScoped<IVectorRepository, VectorRepository>();
                services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

                // Research 节点服务注册
                services.Configure<ResearchNodeOptions>(context.Configuration.GetSection("ResearchNode"));
                services.AddSingleton<PlaywrightBrowserPool>();
                services.AddHttpClient<IWebSearchService, WebSearchService>();
                services.AddScoped<IContentFetcherService, ContentFetcherService>();
                services.AddScoped<IResearchEngineService, ResearchEngineService>();
                services.AddScoped<IContentCache, RedisContentCache>();

                // Voice 节点服务注册
                services.Configure<VoiceNodeOptions>(context.Configuration.GetSection("VoiceNodeOptions"));
                services.Configure<FunASROptions>(context.Configuration.GetSection("FunASROptions"));
                services.AddSingleton<IVoiceProviderFactory, VoiceProviderFactory>();
                services.AddSingleton<IAudioPreprocessor, AudioPreprocessor>();
                services.AddSingleton<IVoiceTranscriptionProvider, FunASRProvider>();
                services.AddTransient<ExecutionVoice>();

                // 知识库图片处理服务注册
                services.AddScoped<IImageExtractionService, ImageExtractionService>();
                services.AddScoped<IImageDescriptionService, VLMImageDescriptionService>();
                services.AddScoped<IImageStorageService, FileImageStorageService>();
                services.AddScoped<IImageProcessingPipeline, ImageProcessingPipeline>();
                services.AddScoped<IImageRepository, ImageRepository>();

                services.AddSingleton(sp => new FunctionService(sp, [typeof(ZSN.AI.Plugins.BasePlugin).Assembly]));
                services.AddSingleton(sp => new FunctionService(sp, [typeof(ZSN.AI.Plugins.Functions.HttpPlugin).Assembly]));

                services.AddSignalR();

                // 节点任务队列消费者（持续从 Redis 队列消费任务并执行）
                services.AddHostedService<NodeTaskQueueConsumer>();

                // 启动调度服务
                services.AddHostedService<QuartzHostedService>();


            })
            .Build();

            // 初始化全局 ServiceProvider（供后台异步恢复任务使用）
            ServiceProviderHolder.Initialize(host.Services);

            // ✅ 关键修复：在 host.RunAsync() 之前注册回调，确保 NodeJob 启动前回调已就绪
            // 注册 ClawAI 异步恢复回调（BLL → Node 跨层调用）
            ClawAIResumeCallback.Register(async (asyncTaskID, mergedResult, allStepResults) =>
            {
                var sp = ServiceProviderHolder.ServiceProvider;
                using var scope = sp.CreateScope();
                var executionClaw = scope.ServiceProvider.GetRequiredService<ExecutionClaw>();
                return await executionClaw.ContinueFromStepAsync(asyncTaskID, mergedResult, allStepResults);
            });


            // ✅ 回调注册完成后再启动 Quartz 定时任务
            host.RunAsync();

            NLogHelper.WriteInfo("定时任务启动！");

            // 初始化知识库服务
            Task.Run(async () =>
            {
                var serviceProvider = host.Services;
                var graphRepository = serviceProvider.GetService<IGraphRepository>();
                var vectorRepository = serviceProvider.GetService<IVectorRepository>();

                try
                {
                    if (graphRepository != null)
                    {
                        await graphRepository.InitializeAsync();
                        NLogHelper.WriteInfo("Apache AGE 图数据库初始化成功");
                    }
                }
                catch (Exception ex)
                {
                    NLogHelper.WriteError($"Apache AGE 图数据库初始化失败: {ex.Message}");
                }

                try
                {
                    if (vectorRepository != null)
                    {
                        await vectorRepository.InitializeAsync();
                        NLogHelper.WriteInfo("向量数据库初始化成功");
                    }
                }
                catch (Exception ex)
                {
                    NLogHelper.WriteError($"向量数据库初始化失败: {ex.Message}");
                }
            }).Wait();

            Console.Read();
        }

        public void Stop()
        {
        }

    }

}
