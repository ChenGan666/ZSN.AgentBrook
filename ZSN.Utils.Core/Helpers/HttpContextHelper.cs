using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using ZSN.Utils.Core.DI;
using Microsoft.AspNetCore.Hosting;
using Senparc.CO2NET.HttpUtility;

namespace ZSN.Utils.Core.Helpers
{
    public class HttpContextHelper
    {
        public static IHttpContextAccessor ContextAccessor => ServiceLocator.GetInstance<IHttpContextAccessor>();

        public static IWebHostEnvironment Environment => ServiceLocator.GetInstance<IWebHostEnvironment>();

        public static HttpContext Current => ContextAccessor?.HttpContext;

        public static ISession Session => Current?.Session;

        public static HttpRequest Request => Current?.Request;

        public static IQueryCollection Query => Request?.Query;

        public static QueryString QueryString => Request?.QueryString ?? new QueryString("");

        public static IRequestCookieCollection Cookie => Request?.Cookies;

        public static HttpResponse Response => Current?.Response;

        public static IResponseCookies ResponseCookie => Response?.Cookies;

        public static string GetBodyParams(HttpContext context)
        {
            try
            {
                System.IO.Stream s = context.Request.GetRequestMemoryStream();
                // 启用 EnableBuffering 后需要重置流位置，以便多次读取
                if (s.CanSeek)
                {
                    s.Position = 0;
                }
                
                int count = 0;
                byte[] buffer = new byte[1024];
                StringBuilder builder = new StringBuilder();
                while ((count = s.Read(buffer, 0, 1024)) > 0)
                {
                    builder.Append(Encoding.UTF8.GetString(buffer, 0, count));
                }
                
                // 读取完成后重置流位置，允许后续再次读取（例如 [FromBody] 参数绑定）
                if (s.CanSeek)
                {
                    s.Position = 0;
                }
                
                // 不要关闭和释放流，因为后续还需要使用
                // s.Flush();
                // s.Close();
                // s.Dispose();
                
                return builder.ToString();
            }
            catch (Exception ex)
            {
                throw ex; 
            }
        }
        /// <summary>
        /// 获取 QueryString 参数
        /// </summary>
        public static string? GetQuery(string key)
        {
            return ContextAccessor?.HttpContext?.Request?.Query[key].ToString();
        }
        public static bool IsLocalIP()
        {
            var ip = Current?.GetClientUserIp() ?? "127.0.0.1";
            return ip == "::1" || ip == "127.0.0.1";
        }
    }
}
