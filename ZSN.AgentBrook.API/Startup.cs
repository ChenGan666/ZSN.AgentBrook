using ZSN.AI.Service.Base;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using ZSN.AgentBrook.API.ConfigureSwagger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Microsoft.AspNetCore.Rewrite;
using System;
using ZSN.AI.Service.Helpers;
using ZSN.AI.Core.Common.DependencyInjection;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using ZSN.AI.Core.Utils;
using ZSN.AI.Core.Service;

namespace ZSN.AgentBrook.API
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
            // 添加CORS服务
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

            System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.Name = "ZSNAppSession"; // Session的Cookie名称
                options.IdleTimeout = TimeSpan.FromSeconds(3600); // Session过期时间
                options.Cookie.HttpOnly = true; // 只通过HTTP访问Session Cookie
            });
            //services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "DataProtection"));

            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddControllersWithViews();

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
            services.AddScoped<ZSN.AI.Node.ExecutionServiceDesk>();

            services.AddSingleton(sp => new FunctionService(sp, [typeof(ZSN.AI.Plugins.BasePlugin).Assembly]));
            services.AddSingleton<TaskManager>();

            services.AddSignalR();
            //注册Swagger服务
            services.ConfigureSwaggerUp();
            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressConsumesConstraintForFormFileParameters = false;
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
            app.UseDeveloperExceptionPage();

            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint($"/swagger/V1-Public/swagger.json", "V1-Public");
                c.SwaggerEndpoint($"/swagger/V1-Member/swagger.json", "V1-Member");
                c.SwaggerEndpoint($"/swagger/V1-User/swagger.json", "V1-User");
                c.SwaggerEndpoint($"/swagger/V1-Manage/swagger.json", "V1-Manage");
                c.RoutePrefix = "doc";
            });
            app.UseStaticFiles();

            // 添加URL重写中间件//api/File/Get?fileCode=66&w=0&h=0
            app.UseRewriter(new RewriteOptions()
                .AddRewrite(@"^api/File/Get/([^/]+)/(\d+)/(\d+)$", "api/File/Get?filecode=$1&w=$2&h=$3", skipRemainingRules: true)
            );

            // 启用请求体缓冲，允许多次读取请求体
            // 解决基类构造函数中读取 BodyParams 导致 [FromBody] 参数为 null 的问题
            app.Use(async (context, next) =>
            {
                context.Request.EnableBuffering();
                await next();
            });

            app.UseRouting();

            app.UseCors();

            app.UseSession();

            app.UseAuthorization();

            StartupHelper.ConfigureInit(app, env);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
