using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Repositories;
using ZSN.AI.Core.Service;
using ZSN.AI.Core.Services;
using ZSN.AI.Core.Utils;
using ZSN.AI.KnowledgeBase.Interface;
using ZSN.AI.KnowledgeBase.Repositories;
using ZSN.AI.KnowledgeBase.Services;
using ZSN.AI.Node.ServiceDesk;
using ZSN.AI.Service.Base;
using ZSN.AgentBrook.Web.Manage.Middleware;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
            //第一个参数为配置文件路径，默认为项目目录下config.json
            //第二个参数为是否缓存配置文件，默认false
            //services.AddUEditorService();
            services.AddSession();
            //services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "DataProtection"));

            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddControllersWithViews();
            services.AddHttpClient(); // 注册 HttpClient 工厂服务
            services.AddHttpContextAccessor();

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

            services.AddServicesFromAssemblies("ZSN.AI.Core");
            services.AddServicesFromAssemblies("ZSN.AI.Plugins");

            // 注册操作日志服务
            services.AddScoped<ZSN.AI.BLL.IOperationLogService, ZSN.AI.Service.WebHelpers.OperationLogService>();

            // 注册 Claw AI 服务
            services.AddScoped<ZSN.AI.Node.Claw.Interfaces.ITaskPlanningService, ZSN.AI.Node.Claw.Services.TaskPlanningService>();
            services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IMemoryService, ZSN.AI.Node.Claw.Services.MemoryService>();
            services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IReflectionService, ZSN.AI.Node.Claw.Services.ReflectionService>();
            services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IAgentOrchestrationService, ZSN.AI.Node.Claw.Services.AgentOrchestrationService>();
            services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IPersonalityService, ZSN.AI.Node.Claw.Services.PersonalityService>();
            services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IResultParserService, ZSN.AI.Node.Claw.Services.ResultParserService>();
            services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IKnowledgeExtractionService, ZSN.AI.Node.Claw.Services.KnowledgeExtractionService>();
            services.AddScoped<ZSN.AI.Node.Claw.Interfaces.IMasterControlService, ZSN.AI.Node.Claw.Services.MasterControlService>();

            // ServiceDesk 服务注册
            services.AddScoped<ZSN.AI.Node.ServiceDesk.Interfaces.IRequestClassifier, ZSN.AI.Node.ServiceDesk.Services.RequestClassifier>();
            services.AddScoped<ZSN.AI.Node.ServiceDesk.Interfaces.IKnowledgeRetriever, ZSN.AI.Node.ServiceDesk.Services.KnowledgeRetriever>();
            services.AddScoped<ZSN.AI.Node.ServiceDesk.Interfaces.IResponseGenerator, ZSN.AI.Node.ServiceDesk.Services.ResponseGenerator>();
            services.AddScoped<ZSN.AI.Node.ServiceDesk.Interfaces.ISessionStateManager, ZSN.AI.Node.ServiceDesk.Services.SessionStateManager>();
            services.AddScoped<ExecutionServiceDesk>();

            // 首次运行欢迎向导服务
            services.AddScoped<ZSN.AgentBrook.Web.Manage.Services.IWelcomeEnvironmentService, ZSN.AgentBrook.Web.Manage.Services.WelcomeEnvironmentService>();
            services.AddScoped<ZSN.AgentBrook.Web.Manage.Services.IWelcomeStartInfoService, ZSN.AgentBrook.Web.Manage.Services.WelcomeStartInfoService>();

            // 注册工作流自动生成器
            services.AddScoped<ZSN.AI.Node.Utils.WorkflowAutoGenerator>();

            services.AddScoped<ISemanticChunkerService, SemanticChunkerService>();

            // 知识库服务改为 Scoped，因为依赖 IChatService（Scoped）
            services.AddScoped<IGraphRepository, AgeGraphRepository>();
            services.AddScoped<IKnowledgeGraphService, KnowledgeGraphService>();
            services.AddScoped<IHybridSearchService, HybridSearchService>();
            services.AddScoped<IEmbeddingService, EmbeddingService>();
            services.AddScoped<IVectorRepository, VectorRepository>();
            services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

            // 图片处理服务
            services.AddScoped<IImageExtractionService, ImageExtractionService>();
            services.AddScoped<IImageStorageService, FileImageStorageService>();
            services.AddScoped<IImageDescriptionService, VLMImageDescriptionService>();
            services.AddScoped<IImageRepository, ImageRepository>();
            services.AddScoped<IImageProcessingPipeline, ImageProcessingPipeline>();

            services.AddSingleton(sp => new FunctionService(sp, [typeof(ZSN.AI.Plugins.BasePlugin).Assembly]));

            services.AddSignalR();

            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressConsumesConstraintForFormFileParameters = true;
                options.SuppressInferBindingSourcesForParameters = true;
                options.SuppressModelStateInvalidFilter = true;
                options.SuppressMapClientErrors = true;
                //options.ClientErrorMapping[StatusCodes.Status404NotFound].Link = "https://httpstatuses.com/404";
            });

            //全局配置Json序列化处理
            services.AddMvc().AddNewtonsoftJson(options =>
            {
                //忽略循环引用
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                //不使用驼峰样式的key
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                //设置时间格式
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";

            });
            services.AddControllersWithViews().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            });
            services.AddMvc().AddJsonOptions(options =>
            {
                //JSON首字母小写解决
                options.JsonSerializerOptions.PropertyNamingPolicy = null;

                //此设定解决JsonResult中文被编码的问题
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

                options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
                options.JsonSerializerOptions.Converters.Add(new DateTimeNullableConvert());
            });

            StartupHelper.ServicesInit(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                //app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            var ueditorDir = "ueditor";
            var ueditorPath = PathHelper.Combine(Directory.GetCurrentDirectory(), ueditorDir);
            if (!Directory.Exists(ueditorPath))
            {
                Directory.CreateDirectory(ueditorPath);
            }
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(ueditorPath),
                RequestPath = $"/{ueditorDir}",
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=36000");
                }
            });

            app.UseStaticFiles();

            // 统一为文本/脚本类响应补充 charset=utf-8
            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.HasStarted)
                {
                    return;
                }
                var contentType = context.Response.ContentType;
                if (!string.IsNullOrEmpty(contentType))
                {
                    if ((contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
                         contentType.StartsWith("application/javascript", StringComparison.OrdinalIgnoreCase) ||
                         contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                        && !contentType.Contains("charset=", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.ContentType = contentType + "; charset=utf-8";
                    }
                }
            });

            app.Init();

            // 初始化知识库服务
            Task.Run(async () =>
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var graphRepository = scope.ServiceProvider.GetService<IGraphRepository>();
                    var vectorRepository = scope.ServiceProvider.GetService<IVectorRepository>();

                    try
                    {
                        if (graphRepository != null)
                        {
                            await graphRepository.InitializeAsync();
                            Console.WriteLine("✓ Apache AGE 图数据库初始化成功");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Apache AGE 图数据库初始化失败: {ex.Message}");
                    }

                    try
                    {
                        if (vectorRepository != null)
                        {
                            await vectorRepository.InitializeAsync();
                            Console.WriteLine("✓ 向量数据库初始化成功");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ 向量数据库初始化失败: {ex.Message}");
                    }
                }
            }).Wait();

            app.UseRouting();

            // 首次运行拦截中间件
            app.UseFirstRunMiddleware();

            app.UseCors();
            
            app.UseAuthorization();

            StartupHelper.ConfigureInit(app, env);

            app.UseEndpoints(endpoints =>
            {

                endpoints.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Index}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Index}/{action=Index}/{id?}");
            });

            app.UseSession();

        }
    }
}
