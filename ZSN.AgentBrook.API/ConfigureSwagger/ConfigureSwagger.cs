using System;
using System.IO;
using ZSN.AgentBrook.API.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using ZSN.AI.Service.Attributes;

namespace ZSN.AgentBrook.API.ConfigureSwagger
{
    public static class ConfigureSwagger
    {
        public static void ConfigureSwaggerUp(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            //注册Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("V1-Public", new OpenApiInfo
                {
                    Title = "V1-Public",
                    Version = "V1-Public",
                    Description = $"ApiService HTTP API V1",
                });
                c.SwaggerDoc("V1-Member", new OpenApiInfo
                {
                    Title = "V1-Member",
                    Version = "V1-Member",
                    Description = $"ApiService HTTP API V1",
                });
                c.SwaggerDoc("V1-User", new OpenApiInfo
                {
                    Title = "V1-User",
                    Version = "V1-User",
                    Description = $"ApiService HTTP API V1",
                });
                c.SwaggerDoc("V1-Manage", new OpenApiInfo
                {
                    Title = "V1-Manage",
                    Version = "V1-Manage",
                    Description = $"ApiService HTTP API V1",
                });
                // 添加文件上传支持
                c.AddSecurityDefinition("multipartForm", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "multipartForm",
                    Description = "Supports 'multipart/form-data'."
                });
                //支持文件上传
                c.OperationFilter<FileUploadOperation>();
                /*// 设置要展示的接口
                c.DocInclusionPredicate((docName, apiDes) =>
                {
                    if (!apiDes.TryGetMethodInfo(out MethodInfo method))
                        return false;
                    *//* 使用ApiExplorerSettingsAttribute里面的GroupName进行特性标识
                     * DeclaringType只能获取controller上的特性
                     * 我们这里是想以action的特性为主
                     *//*
                    var actionGroup = method.GetCustomAttributes(true).OfType<ApiExplorerSettingsAttribute>().Select(m => m.GroupName);
                    if (actionGroup.Any())
                    {
                        return actionGroup.Any(v => v == docName);
                    }
                    var controllerGroup = method.DeclaringType.GetCustomAttributes(true).OfType<ApiExplorerSettingsAttribute>().Select(m => m.GroupName);
                    return controllerGroup.Any(v => v == docName);
                });*/
                // 添加授权
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Please enter a Token starting with Bearer",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
                c.AddSecurityDefinition("MemberBearer", new OpenApiSecurityScheme
                {
                    Description = "Please enter a MemberToken starting with MemberBearer",
                    Name = "MemberBearer",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
                // 认证方式，此方式为全局添加
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "MemberBearer"
                            }
                        },
                        new string[] {}
                    }
                });
                // 添加Xml说明文件
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var xmlFileName = AppDomain.CurrentDomain.FriendlyName + ".xml";
                var xmlFilePath = Path.Combine(baseDirectory, xmlFileName);
                if (File.Exists(xmlFilePath))
                {
                    c.IncludeXmlComments(xmlFilePath);
                    c.OrderActionsBy(o=>o.RelativePath);
                    c.IncludeXmlComments(xmlFilePath, true);
                }
                //c.OperationFilter<HttpHeaderOperationFilter>();
                c.DocumentFilter<HiddenApiFilter>();
                //c.OrderActionsBy(o => o.RelativePath);
            });
        }
    }
}
