using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using ZSN.Utils.Core.Extensions;

namespace ZSN.Utils.Core.Helpers
{
    /// <summary>
    ///     代理使用帮助类
    /// </summary>
    public class ProxyHelper
    {
        public static readonly string ApiUrl = ConfigHelper.GetString("ApiUrl");
        private static readonly string ProxyUrl = ApiUrl + "/Home/GetProxyIp";
        private static readonly string IdProxyUrl = ApiUrl + "/Home/GetIdProxy";
        private static readonly string MarkChangeUrl = ApiUrl + "/Home/MarkChangeIp?ip=";
        private static readonly bool IsUseProxy = ConfigHelper.GetBool("DoesUseProxy", true);

        /// <summary>
        ///     代理账户用户名
        /// </summary>
        public static string UserName = "name";

        /// <summary>
        ///     代理账户密码
        /// </summary>
        public static string Password = "pwd";

        /// <summary>
        ///     获取代理ip
        /// </summary>
        /// <param name="isNeedWait">判断是否需要等待，false则不考虑队列等待，直接返回地址</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetProxy(bool isNeedWait = true, ProxyType type = ProxyType.普通)
        {
            if (!IsUseProxy) return "";
            var proxy = "";
            var tryTimes = 10;
            while (tryTimes-- > 0)
            {
                try
                {
                    proxy = GetWebContent(ProxyUrl + $"?type={(int)type}&isWaited={isNeedWait}", timeout: 20000);
                    while (proxy.Contains("time"))
                    {
                        Console.WriteLine("代理请求频繁，需等待" + proxy);
                        Thread.Sleep(Convert.ToInt32(proxy.Split(':')[1]));
                        proxy = isNeedWait
                            ? GetWebContent(ProxyUrl + $"?type={(int)type}&isWaited=true", timeout: 20000)
                            : GetWebContent(ProxyUrl, timeout: 20000);
                    }
                    if (!string.IsNullOrEmpty(UserName + Password) && !string.IsNullOrEmpty(proxy))
                        proxy += $":{UserName}:{Password}";
                    break;
                }
                catch (Exception)
                {
                    // ignored
                    Thread.Sleep(2000);
                }
            }

            return proxy;
        }

        /// <summary>
        ///     获取特定id的代理IP
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetProxy(int id)
        {
            var proxy = "";
            var tryTimes = 10;
            while (tryTimes-- > 0)
            {
                try
                {
                    proxy = GetWebContent(IdProxyUrl + $"?id={id}", timeout: 20000);
                    if (!string.IsNullOrEmpty(UserName + Password) && !string.IsNullOrEmpty(proxy))
                        proxy += $":{UserName}:{Password}";
                    break;
                }
                catch (Exception)
                {
                    // ignored
                    Thread.Sleep(2000);
                }
            }

            return proxy;
        }

        /// <summary>
        ///     设置代理不可用
        /// </summary>
        /// <param name="proxyIp">代理ip地址</param>
        /// <param name="errorType"></param>
        /// <param name="referUrl"></param>
        public static void SetProxyError(string proxyIp, ProxyErrorType errorType = ProxyErrorType.未定义,
            string referUrl = "")
        {
            if (!IsUseProxy) return;
            if (!string.IsNullOrEmpty(UserName + Password))
            {
                var t = proxyIp.Split(':');
                if (t.Length > 1)
                {
                    proxyIp = t[0] + ":" + t[1];
                    proxyIp = HttpUtility.UrlEncode(proxyIp);
                }
                else
                {
                    return;
                }
            }
            if (ProxyErrorType.需要输入验证码 == errorType && referUrl == "") referUrl = "http://mp.weixin.qq.com/";
            referUrl = HttpUtility.UrlEncode(referUrl);
            var requestUrl = MarkChangeUrl + HttpUtility.UrlEncode(proxyIp) + $"&errortype=" + (int)errorType +
                             $"&referUrl={referUrl}";
            try
            {
                GetWebContent(requestUrl, timeout: 30000, isAsyn: true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static string GetWebContent(string url, string proxy = "", string agent = "", int timeout = 20000,
            CookieContainer cookieContainer = null, bool isAsyn = false)
        {
            if (string.IsNullOrEmpty(agent))
            {
                agent =
                    "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.80 Safari/537.36";
            }
            var request = WebRequest.Create(url) as HttpWebRequest;
            request.UserAgent = agent;
            request.Proxy = null;
            request.Timeout = timeout;
            if (cookieContainer != null)
            {
                request.CookieContainer = cookieContainer;
            }
            else
            {
                request.CookieContainer = new CookieContainer();
            }
            if (!string.IsNullOrEmpty(proxy))
            {
                var arr = proxy.Split(':');
                if (arr.Length > 0)
                {
                    var ip = arr[0];
                    var port = 80;
                    if (arr.Length > 1)
                    {
                        port = arr[1].ToInt32();
                    }
                    request.Proxy = new WebProxy(ip, port);
                    if (arr.Length == 4) request.Proxy.Credentials = new NetworkCredential(arr[2], arr[3]);
                }
            }
            var num = 3;
            while (num > 0)
            {
                try
                {
                    if (isAsyn)
                    {
                        request.BeginGetResponse(DoneRequest, new object());
                        return null;
                    }
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        using (var steam = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                    num--;
                    if (num == 0) throw;
                }
            }
            return null;
        }

        private static void DoneRequest(IAsyncResult ar)
        {
            //Console.WriteLine("done");
        }
    }

    /// <summary>
    ///     代理错误类型
    /// </summary>
    public enum ProxyErrorType
    {
        未定义 = 0,
        未知错误 = 1,
        需要输入验证码 = 2,
        操作过于频繁 = 3
    }

    public enum ProxyType
    {
        普通 = 1,
        爬取内容 = 2
    }
}