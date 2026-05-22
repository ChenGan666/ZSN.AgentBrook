using ZSN.AI.Service.Base;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Rewrite;
using System;
using ZSN.AI.Core.Common.DependencyInjection;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using ZSN.AI.Core.Utils;
using ZSN.AI.Core.Service;
using ZSN.AI.MCPServer.ConfigureSwagger;

namespace ZSN.AI.MCPServer
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

            // 配置 ForwardedHeaders 支持 Nginx 反向代理
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                // 允许所有代理服务器
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
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

            services.AddSingleton(sp => new FunctionService(sp, [typeof(ZSN.AI.Plugins.BasePlugin).Assembly]));

            services.AddSignalR();
            //注册Swagger服务
            services.ConfigureSwaggerUp();
            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                // 不要抑制文件参数的 Consumes 约束，以便正确处理文件上传
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
                // 添加宽松的布尔值转换器，兼容MCP协议等将布尔值序列化为字符串的场景
                options.JsonSerializerOptions.Converters.Add(new BooleanConverter());
                options.JsonSerializerOptions.Converters.Add(new NullableBooleanConverter());
            });

            services.AddMcpServer()
                    .WithHttpTransport()
                    .WithToolsFromAssembly();

            StartupHelper.ServicesInit(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 使用 ForwardedHeaders 中间件，必须在最前面
            // 支持 Nginx 反向代理的 X-Forwarded-* 头
            app.UseForwardedHeaders();

            app.UseDeveloperExceptionPage();

            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint($"/swagger/V1-Public/swagger.json", "V1-Public");
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

            app.UseAuthorization();

            StartupHelper.ConfigureInit(app, env);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                
                // 添加 MCP 服务端点支持
                endpoints.MapMcp();
            });



            app.UseSession();
        }
    }
}
