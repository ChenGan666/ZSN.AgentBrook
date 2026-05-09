
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.Core.Service;
using ZSN.AI.Core.Repositories;
using ZSN.AI.Core.Interface;

namespace ZSN.AI.Core.Common.DependencyInjection
{
    public static class InitExtensions
    {
        private static ILogger _logger;

        public static void InitLog(ILogger logger)
        {
            _logger = logger;
        }

        public static IApplicationBuilder Init(this IApplicationBuilder app)
        {
            try
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    //IServiceProvider _serviceProvider = scope.ServiceProvider.GetService<IServiceProvider>();
                    //IKernelService _kernelService = scope.ServiceProvider.GetService<IKernelService>();

                }
            }
            catch
            {

            }
            return app;
        }

        /// <summary>
        /// 加载数据库的插件
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static WebApplication LoadFun(this WebApplication app)
        {
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    //codefirst 创建表
                    var funRep = scope.ServiceProvider.GetRequiredService<IFuns_Repositories>();
                    var functionService = scope.ServiceProvider.GetRequiredService<FunctionService>();
                    /*
                    var funs = funRep.GetList();
                    foreach (var fun in funs)
                    {
                        functionService.FuncLoad(fun.Path);
                    }
                    */
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " ---- " + ex.StackTrace);
            }
            return app;
        }
    }
}
