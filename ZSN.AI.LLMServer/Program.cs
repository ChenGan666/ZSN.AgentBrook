using System.Net;
using System.Runtime.InteropServices;
using ZSN.Utils.Core.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZSN.AI.LLMServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                host = host.UseWindowsService();
            }
            return host.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole(options =>
                {
                    options.LogToStandardErrorThreshold = LogLevel.Error;
                });
            }).ConfigureWebHostDefaults(webBuilder =>
            {
                var port = ConfigHelper.GetInt("ServicePort");//���÷���˿�
                webBuilder.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.Listen(IPAddress.Any, port);
                    serverOptions.Limits.MaxRequestBodySize = null;
                });
                webBuilder.UseStartup<Startup>();
            });
        }
    }
}
