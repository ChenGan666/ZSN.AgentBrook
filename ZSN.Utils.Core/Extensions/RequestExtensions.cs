using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace ZSN.Utils.Core.Extensions
{
    public static class RequestExtensions
    {
        public static string GetParam(this HttpRequest request, string key, string def="")
        {
            if (request.Query.ContainsKey(key))
                return request.Query[key];
            try
            {
                if (request.Form?.ContainsKey(key) ?? false)
                    return request.Form[key];
            }
            catch (Exception e)
            {
            }
            return def;
        }

        // Fields
        private const string NullIpAddress = "::1";

        // Methods
        /// <summary>返回绝对地址</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string AbsoluteUri1(this HttpRequest request)
        {
            string str = request.Scheme.ToUpper();
            string str2 = request.Host.Host;
            int? nullable = request.Host.Port;
            int num = nullable.HasValue ? nullable.GetValueOrDefault() : -1;
            string str3 = null;
            if (((num == -1) || ((str == "HTTP") && (num == 80))) || ((str == "HTTPS") && (num == 0x1bb)))
            {
                str3 = "";
            }
            else
            {
                str3 = ":" + ((int) num).ToString();
            }

            string[] textArray1 = new string[]
            {
                request.Scheme, "://", str2, str3, request.PathBase.ToUriComponent(),
                request.Path.ToUriComponent(), request.QueryString.ToUriComponent()
            };
            return string.Concat((string[]) textArray1);
        }
        /*
        /// <summary>
        /// Determines whether the specified HTTP request is an AJAX request.
        /// </summary>
        /// <returns>
        /// true if the specified HTTP request is an AJAX request; otherwise, false.
        /// </returns>
        /// <param name="request">The HTTP request.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="request" /> parameter is null (Nothing in Visual Basic).</exception>
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return ((request.Headers != null) &&
                    (request.Headers["X-Requested-With"] == "XMLHttpRequest"));
        }
        */
        /// <summary>是否是本地请求</summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static bool IsLocal(this HttpRequest req)
        {
            ConnectionInfo info = req.HttpContext.Connection;
            if (!info.RemoteIpAddress.IsSet())
            {
                return true;
            }

            if (!info.LocalIpAddress.IsSet())
            {
                return IPAddress.IsLoopback(info.RemoteIpAddress);
            }

            return info.RemoteIpAddress.Equals(info.LocalIpAddress);
        }

        private static bool IsSet(this IPAddress address)
        {
            return ((address != null) && (address.ToString() != "::1"));
        }

        /// <summary>通常是以/开头的完整路径</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string PathAndQuery2(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (request.Path + request.QueryString);
        }

        /// <summary>获取来源页面</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string UrlReferrer2(this HttpRequest request)
        {
            return request.Headers["Referer"].ToString();
        }

        /// <summary>获取客户端信息</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string UserAgent2(this HttpRequest request)
        {
            return request.Headers["User-Agent"].ToString();
        }

        /// <summary>获取客户端地址（IP）</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static IPAddress UserHostAddress(this HttpContext httpContext)
        {
            IHttpConnectionFeature local1 = httpContext.Features.Get<IHttpConnectionFeature>();
            if (local1 == null)
            {
                return null;
            }
            return local1.RemoteIpAddress;
        }

        public static BrowserType Browser(this HttpRequest request)
        {
            var userAgent = request.Headers["User-Agent"].ToString();
            var browserName = string.Empty;
            var ver = string.Empty;
            // IE
            string regexStr = @"msie (?<ver>[\d.]+)";
            Regex r = new Regex(regexStr, RegexOptions.IgnoreCase);
            Match m = r.Match(userAgent);
            if (m.Success)
            {
                browserName = "IE";
                ver = m.Groups["ver"].Value;
                return new BrowserType(browserName, ver);
            }
            // Firefox
            regexStr = @"firefox\/([\d.]+)";
            r = new Regex(regexStr, RegexOptions.IgnoreCase);
            m = r.Match(userAgent);
            if (m.Success)
            {
                browserName = "Firefox";
                ver = m.Groups["ver"].Value;
                return new BrowserType(browserName, ver);
            }
            // Chrome
            regexStr = @"chrome\/([\d.]+)";
            r = new Regex(regexStr, RegexOptions.IgnoreCase);
            m = r.Match(userAgent);
            if (m.Success)
            {
                browserName = "Chrome";
                ver = m.Groups["ver"].Value;
                return new BrowserType(browserName, ver);
            }
            // Opera
            regexStr = @"opera.([\d.]+)";
            r = new Regex(regexStr, RegexOptions.IgnoreCase);
            m = r.Match(userAgent);
            if (m.Success)
            {
                browserName = "Opera";
                ver = m.Groups["ver"].Value;
                return new BrowserType(browserName, ver);
            }
            // Safari
            regexStr = @"version\/([\d.]+).*safari";
            r = new Regex(regexStr, RegexOptions.IgnoreCase);
            m = r.Match(userAgent);
            if (m.Success)
            {
                browserName = "Safari";
                ver = m.Groups["ver"].Value;
                return new BrowserType(browserName, ver);
            }
            // Edge
            regexStr = @"edge\/([\d.]+)";
            r = new Regex(regexStr, RegexOptions.IgnoreCase);
            m = r.Match(userAgent);
            if (m.Success)
            {
                browserName = "Edge";
                ver = m.Groups["ver"].Value;
                return new BrowserType(browserName, ver);
            }
            return new BrowserType("Unknown","0");
        }
    }

    public class BrowserType
    {
        public BrowserType()
        {

        }

        public BrowserType(string n, string v)
        {
            Name = n;
            Ver = v;
        }

        public string Name { get; set; }

        public string Ver { get; set; }
    }
}
