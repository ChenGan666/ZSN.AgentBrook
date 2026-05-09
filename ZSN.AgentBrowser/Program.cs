using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ZSN.AgentBrowser;

var builder = WebApplication.CreateBuilder(args);

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 添加 OpenAPI 支持（.NET 10.0 原生）
builder.Services.AddOpenApi();

// 注册 AgentBrowserService
builder.Services.AddSingleton<AgentBrowserService>();

// 添加 CORS 支持
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 配置 HTTP 请求管道

// 启用 OpenAPI 文档（.NET 10.0 原生）
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // 重定向 /swagger 到 /openapi/v1.json
    app.MapGet("/swagger", () => Results.Redirect("/openapi/v1.json")).WithName("Swagger UI Redirect").ExcludeFromDescription();
}

// 配置中间件顺序
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

// 映射端点
app.MapControllers();

// 添加根路由处理
app.MapGet("/", () => new 
{ 
    message = "Agent-Browser API Service", 
    version = "1.0.0", 
    docs = new 
    { 
        swagger = "/swagger",
        openapi = "/openapi/v1.json"
    }
});

app.Run();
