namespace ZSN.AgentBrook.Web.Manage.Middleware
{
    /// <summary>
    /// 首次运行拦截中间件
    /// </summary>
    public class FirstRunMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FirstRunMiddleware> _logger;

        public FirstRunMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<FirstRunMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var isFirstRun = _configuration.GetValue<bool>("Welcome:FirstRun");
            if (!isFirstRun)
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // 允许访问欢迎页面和静态资源
            var allowedPaths = new[]
            {
                "/manage/welcome/",
                "/layui/",
                "/layuiadmin/",
                "/css/",
                "/js/",
                "/lib/",
                "/ueditor/",
                "/favicon.ico",
                "/openapi/",
                "/swagger/"
            };

            if (allowedPaths.Any(p => path.StartsWith(p)))
            {
                await _next(context);
                return;
            }

            _logger.LogInformation("[FirstRun] 首次运行，重定向到欢迎页面");
            context.Response.Redirect("/Manage/Welcome/Index");
        }
    }

    /// <summary>
    /// 中间件扩展方法
    /// </summary>
    public static class FirstRunMiddlewareExtensions
    {
        public static IApplicationBuilder UseFirstRunMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FirstRunMiddleware>();
        }
    }
}
