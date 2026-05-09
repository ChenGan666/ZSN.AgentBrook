using System;
using ZSN.Utils.Core.DI;
using ZSN.AI.Service.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ZSN.AI.Service.Base
{
    public class StartupHelper
    {
        public static void ServicesInit(IServiceCollection services)
        {
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.AddMvc().AddNewtonsoftJson();

            // cors
            //services.AddCors(o => o.AddPolicy("FilePolicy", builder =>
            //{
            //    builder.AllowAnyOrigin()
            //        .AllowAnyMethod()
            //        .AllowAnyHeader();
            //}));

            services.AddMvc(option => { option.Filters.Add(new GlobalExceptionFilter()); });
            // session
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                // Make the session cookie essential
                //options.Cookie.IsEssential = true;
            });

            services.AddHttpContextAccessor();

            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromHours(1);
            });

            ServiceLocator.SetService(services);
        }

        public static void ConfigureInit(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors();
            // done
            app.UseSession();
        }

    }
}
