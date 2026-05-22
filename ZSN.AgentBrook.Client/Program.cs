var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// SPA 静态文件服务 (Vue3 构建产物)
app.UseStaticFiles();

// SPA fallback — 所有未匹配路由返回 index.html，交给前端路由处理
app.MapFallbackToFile("index.html");

// 健康检查端点
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
